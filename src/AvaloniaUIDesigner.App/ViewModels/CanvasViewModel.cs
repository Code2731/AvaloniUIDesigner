using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaUIDesigner.App.Designer.Contracts;
using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Designer.Services;
using AvaloniaUIDesigner.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using LineShape = Avalonia.Controls.Shapes.Line;
using PathShape = Avalonia.Controls.Shapes.Path;
using RectangleShape = Avalonia.Controls.Shapes.Rectangle;

namespace AvaloniaUIDesigner.App.ViewModels;

public partial class CanvasViewModel : ViewModelBase
{
    private readonly IComponentCatalog _componentCatalog;
    private readonly IControlRenderer _renderer;
    private readonly Dictionary<string, string> _colorResources = new(StringComparer.Ordinal);
    private readonly List<DesignerStyleDefinition> _documentStyles = new();
    private Control? _stylePreviewControl;
    private string? _stylePreviewPseudoClass;
    private bool _isReflowingContainerChildren;

    public CanvasViewModel()
        : this(new BuiltInComponentCatalog(), new DefaultControlRenderer())
    {
    }

    public CanvasViewModel(IComponentCatalog componentCatalog, IControlRenderer renderer)
    {
        _componentCatalog = componentCatalog;
        _renderer = renderer;
        Elements.CollectionChanged += OnElementsChanged;
        SelectedElements.CollectionChanged += OnSelectedElementsChanged;
    }

    public ObservableCollection<DesignElement> Elements { get; } = new();

    public ObservableCollection<DesignElement> SelectedElements { get; } = new();

    public string PlaceholderText { get; } = "Select a control in Toolbox, then click the canvas.";

    [ObservableProperty]
    private bool _hasElements;

    [ObservableProperty]
    private bool _snapToGrid = true;

    [ObservableProperty]
    private double _gridSize = 8;

    [ObservableProperty]
    private bool _isGridVisible = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ArtboardBrush))]
    private string _artboardBackground = "#FFFFFF";

    [ObservableProperty]
    private double _artboardWidth = 1280;

    [ObservableProperty]
    private double _artboardHeight = 800;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ZoomPercentage))]
    private double _zoomScale = 1;

    [ObservableProperty]
    private DesignElement? _selectedElement;

    public bool IsSelectionActive => SelectedElements.Count > 0;

    public bool HasMultipleSelection => SelectedElements.Count > 1;

    public string? ActiveStylePreviewPseudoClass => _stylePreviewPseudoClass;

    public string ZoomPercentage => $"{ZoomScale * 100:0}%";

    public IBrush ArtboardBrush => Brush.Parse(ArtboardBackground);

    public double SnapPosition(double value)
        => SnapToGrid && GridSize > 0 ? Math.Round(value / GridSize) * GridSize : value;

    public double SnapSize(double value, double minimum)
        => Math.Max(minimum, SnapPosition(value));

    public void SetGridSize(double gridSize) => GridSize = Math.Clamp(gridSize, 4, 32);

    public void SetColorResources(IReadOnlyDictionary<string, string>? resources)
    {
        _colorResources.Clear();
        if (resources is not null)
        {
            foreach (var pair in resources)
            {
                _colorResources[pair.Key] = pair.Value;
            }
        }

        RefreshDocumentStyles();
    }

    public void SetDocumentStyles(IReadOnlyList<DesignerStyleDefinition>? styles)
    {
        _documentStyles.Clear();
        if (styles is not null)
        {
            _documentStyles.AddRange(styles.Select(style => style with
            {
                Setters = new Dictionary<string, string>(style.Setters, StringComparer.Ordinal),
            }));
        }

        RefreshDocumentStyles();
    }

    public void SetStyleClasses(Control visual, IEnumerable<string> classes, bool clearNewStyleConflicts)
    {
        var normalized = classes
            .Where(className => !string.IsNullOrWhiteSpace(className))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var existing = GetUserStyleClasses(visual);
        if (clearNewStyleConflicts)
        {
            var addedClasses = normalized.Except(existing, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
            foreach (var style in _documentStyles.Where(style =>
                         addedClasses.Contains(style.ClassName)
                         && string.Equals(style.TargetType, visual.GetType().Name, StringComparison.Ordinal)))
            {
                foreach (var propertyName in style.Setters.Keys)
                {
                    DesignerStyleRuntime.ClearLocalValue(visual, propertyName);
                }
            }
        }

        foreach (var className in existing)
        {
            visual.Classes.Remove(className);
        }

        foreach (var className in normalized)
        {
            visual.Classes.Add(className);
        }

        RefreshDocumentStyles(visual);
    }

    public void RefreshDocumentStyles()
    {
        foreach (var element in Elements)
        {
            RefreshDocumentStyles(element.Visual);
        }
    }

    public void RefreshDocumentStyles(Control visual)
    {
        var previewStates = ReferenceEquals(visual, _stylePreviewControl)
            && _stylePreviewPseudoClass is not null
                ? new[] { _stylePreviewPseudoClass }
                : null;
        DesignerStyleRuntime.ApplyStyles(visual, _documentStyles, _colorResources, previewStates);
    }

    public void SetStylePreviewState(Control visual, string? pseudoClass)
    {
        var previousControl = _stylePreviewControl;
        _stylePreviewControl = null;
        _stylePreviewPseudoClass = null;
        if (previousControl is not null)
        {
            SetStylePreviewBadge(previousControl, null);
            RefreshDocumentStyles(previousControl);
        }

        if (string.IsNullOrWhiteSpace(pseudoClass))
        {
            if (previousControl is not null)
            {
                OnPropertyChanged(nameof(ActiveStylePreviewPseudoClass));
            }

            return;
        }

        _stylePreviewControl = visual;
        _stylePreviewPseudoClass = pseudoClass;
        SetStylePreviewBadge(visual, $":{pseudoClass}");
        RefreshDocumentStyles(visual);
        OnPropertyChanged(nameof(ActiveStylePreviewPseudoClass));
    }

    public void ClearStylePreviewState()
    {
        if (_stylePreviewControl is null)
        {
            return;
        }

        var previousControl = _stylePreviewControl;
        _stylePreviewControl = null;
        _stylePreviewPseudoClass = null;
        SetStylePreviewBadge(previousControl, null);
        RefreshDocumentStyles(previousControl);
        OnPropertyChanged(nameof(ActiveStylePreviewPseudoClass));
    }

    public bool IsStylePreviewTarget(Control visual)
        => ReferenceEquals(visual, _stylePreviewControl);

    public static IReadOnlyList<string> GetUserStyleClasses(Control visual)
        => visual.Classes
            .Where(className => !className.StartsWith(':'))
            .ToList();

    public void SetArtboard(double width, double height, string? background = null)
    {
        ArtboardWidth = Math.Clamp(width, 320, 3840);
        ArtboardHeight = Math.Clamp(height, 240, 2160);
        if (!string.IsNullOrWhiteSpace(background))
        {
            ArtboardBackground = background;
        }
    }

    public void ZoomIn() => SetZoom(ZoomScale + 0.1);

    public void ZoomOut() => SetZoom(ZoomScale - 0.1);

    public void ResetZoom() => SetZoom(1);

    public void FitToViewport(double viewportWidth, double viewportHeight)
    {
        if (viewportWidth <= 0 || viewportHeight <= 0 || Elements.Count == 0)
        {
            ResetZoom();
            return;
        }

        const double padding = 32;
        var contentWidth = Elements.Max(element => element.X + element.Width) + padding;
        var contentHeight = Elements.Max(element => element.Y + element.Height) + padding;
        SetZoom(Math.Min(viewportWidth / contentWidth, viewportHeight / contentHeight));
    }

    private void SetZoom(double zoom) => ZoomScale = Math.Clamp(zoom, 0.25, 2);

    public DesignElement PlaceElement(ToolboxItem item, double x, double y)
    {
        var (visual, width, height) = CreateVisualByType(item.AvaloniaTypeName, item.DisplayName);
        ApplyVisualProperties(visual, item.DefaultProperties);
        RefreshDocumentStyles(visual);
        return AddElement(
            item.NamePrefix ?? item.DisplayName,
            item.AvaloniaTypeName,
            visual,
            x,
            y,
            item.DefaultWidth ?? width,
            item.DefaultHeight ?? height,
            select: true);
    }

    public bool TrySetSelectedImageSource(string source)
    {
        if (SelectedElement?.Visual is not Image image)
        {
            return false;
        }

        return DesignerImageRuntime.TrySetSource(
            image,
            source,
            retainSourceOnFailure: false,
            out _);
    }

    public DesignElement AddElementFromSnapshot(
        DesignerElementSnapshot snapshot,
        bool select = false,
        bool deferContainerReflow = false)
    {
        var (visual, defaultWidth, defaultHeight) = CreateVisualByType(snapshot.TypeName, snapshot.DisplayName);
        var width = snapshot.Width > 0 ? snapshot.Width : defaultWidth;
        var height = snapshot.Height > 0 ? snapshot.Height : defaultHeight;

        ApplyVisualProperties(visual, snapshot.VisualProperties);
        RefreshDocumentStyles(visual);

        var element = AddElement(
            snapshot.DisplayName,
            snapshot.TypeName,
            visual,
            snapshot.X,
            snapshot.Y,
            width,
            height,
            select,
            preserveDisplayName: true);
        _isReflowingContainerChildren = true;
        try
        {
            element.IsLocked = snapshot.IsLocked;
            element.GridRow = Math.Max(0, snapshot.GridRow);
            element.GridColumn = Math.Max(0, snapshot.GridColumn);
            element.GridRowSpan = Math.Max(1, snapshot.GridRowSpan);
            element.GridColumnSpan = Math.Max(1, snapshot.GridColumnSpan);
            element.StackPanelIndex = snapshot.StackPanelIndex;
            element.StackPanelItemSize = Math.Max(10, snapshot.StackPanelItemSize);
            element.ParentLayout = snapshot.ParentLayout;
            element.DockPanelIndex = snapshot.DockPanelIndex;
            element.DockPanelDock = snapshot.DockPanelDock;
            element.DockPanelItemSize = Math.Max(10, snapshot.DockPanelItemSize);
            element.WrapPanelIndex = snapshot.WrapPanelIndex;
            element.UniformGridIndex = snapshot.UniformGridIndex;
            element.CanvasChildIndex = snapshot.CanvasChildIndex;
            element.CanvasChildLeft = snapshot.CanvasChildLeft;
            element.CanvasChildTop = snapshot.CanvasChildTop;
            element.TabIndex = snapshot.TabIndex;
            element.TabHeader = snapshot.TabHeader;
            element.SplitViewSlot = snapshot.SplitViewSlot;
            element.ParentName = snapshot.ParentName;
        }
        finally
        {
            _isReflowingContainerChildren = false;
        }

        if (!deferContainerReflow)
        {
            if (element.IsContainerChild
                && element.ParentLayout == DesignerParentLayoutKind.None)
            {
                NormalizeContainerRelationships();
            }
            else
            {
                ReflowContainerChild(element);
            }
        }

        ResolveLabelTargets();
        return element;
    }

    public IReadOnlyList<DesignElement> PlacePreset(ToolboxItem preset, double x, double y)
    {
        if (!preset.IsPreset || preset.PresetElements is null)
        {
            return Array.Empty<DesignElement>();
        }

        var placed = new List<DesignElement>();
        foreach (var snapshot in preset.PresetElements)
        {
            var (visual, defaultWidth, defaultHeight) = CreateVisualByType(snapshot.TypeName, snapshot.DisplayName);
            ApplyVisualProperties(visual, snapshot.VisualProperties);
            RefreshDocumentStyles(visual);
            placed.Add(AddElement(
                snapshot.DisplayName,
                snapshot.TypeName,
                visual,
                SnapPosition(x + snapshot.X),
                SnapPosition(y + snapshot.Y),
                snapshot.Width > 0 ? snapshot.Width : defaultWidth,
                snapshot.Height > 0 ? snapshot.Height : defaultHeight,
                select: false));
        }

        ResolveLabelTargets();
        SelectMany(placed);
        return placed;
    }

    public void ResolveLabelTargets()
    {
        var elementsByName = Elements.ToDictionary(element => element.DisplayName, StringComparer.OrdinalIgnoreCase);
        foreach (var label in Elements.Select(element => element.Visual).OfType<Label>())
        {
            var targetName = label.Tag?.ToString();
            label.Target = !string.IsNullOrWhiteSpace(targetName)
                && elementsByName.TryGetValue(targetName, out var target)
                ? target.Visual
                : null;
        }
    }

    public void Clear()
    {
        ClearStylePreviewState();
        ClearSelection();
        Elements.Clear();
    }

    public void Select(DesignElement? element, bool toggle = false)
    {
        if (_stylePreviewControl is not null
            && (element is null || !ReferenceEquals(element.Visual, _stylePreviewControl)))
        {
            ClearStylePreviewState();
        }

        if (element is null)
        {
            ClearSelection();
            return;
        }

        if (toggle)
        {
            if (SelectedElements.Remove(element))
            {
                element.IsSelected = false;
                SelectedElement = SelectedElements.LastOrDefault();
                RefreshTabChildVisibility();
                return;
            }

            SelectedElements.Add(element);
            element.IsSelected = true;
            SelectedElement = element;
            RefreshTabChildVisibility();
            return;
        }

        ReplaceSelection(new[] { element });
    }

    public void SelectMany(IEnumerable<DesignElement> elements)
        => ReplaceSelection(elements);

    public void ClearSelection()
    {
        ClearStylePreviewState();
        foreach (var element in SelectedElements)
        {
            element.IsSelected = false;
        }

        SelectedElements.Clear();
        SelectedElement = null;
        RefreshTabChildVisibility();
    }

    public bool RemoveElement(DesignElement element)
    {
        var formerParent = FindParent(element);
        if (ReferenceEquals(element.Visual, _stylePreviewControl))
        {
            _stylePreviewControl = null;
            _stylePreviewPseudoClass = null;
            element.StylePreviewStateLabel = null;
            OnPropertyChanged(nameof(ActiveStylePreviewPseudoClass));
        }

        var removed = Elements.Remove(element);
        if (!removed)
        {
            return false;
        }

        if (SelectedElements.Remove(element))
        {
            element.IsSelected = false;
            SelectedElement = SelectedElements.LastOrDefault();
        }

        foreach (var child in Elements.Where(candidate =>
                     string.Equals(candidate.ParentName, element.DisplayName, StringComparison.OrdinalIgnoreCase)))
        {
            child.ParentName = null;
            child.GridRow = 0;
            child.GridColumn = 0;
            child.GridRowSpan = 1;
            child.GridColumnSpan = 1;
            child.StackPanelIndex = -1;
            child.StackPanelItemSize = 40;
            child.ParentLayout = DesignerParentLayoutKind.None;
            child.DockPanelIndex = -1;
            child.DockPanelDock = DesignerDockSide.Left;
            child.DockPanelItemSize = 40;
            child.WrapPanelIndex = -1;
            child.UniformGridIndex = -1;
            child.CanvasChildIndex = -1;
            child.CanvasChildLeft = 0;
            child.CanvasChildTop = 0;
            child.TabIndex = -1;
            child.TabHeader = null;
            child.SplitViewSlot = DesignerSplitViewSlot.Content;
            child.IsVisibleOnArtboard = true;
        }

        element.PropertyChanged -= OnDesignElementPropertyChanged;
        if (formerParent is not null && Elements.Contains(formerParent))
        {
            ReflowContainerChildren(formerParent);
        }

        return true;
    }

    public void ReflowGridChildren(DesignElement? parent = null)
        => ReflowContainerChildren(parent);

    public void ReflowContainerChildren(DesignElement? parent = null)
    {
        if (_isReflowingContainerChildren)
        {
            return;
        }

        _isReflowingContainerChildren = true;
        try
        {
            if (parent is not null)
            {
                ReflowContainerTreeCore(parent);
                return;
            }

            foreach (var container in Elements.Where(element =>
                         IsDesignerContainer(element.Visual) && !element.IsContainerChild))
            {
                ReflowContainerTreeCore(container);
            }
        }
        finally
        {
            _isReflowingContainerChildren = false;
        }
    }

    public void NormalizeGridRelationships() => NormalizeContainerRelationships();

    public void NormalizeContainerRelationships()
    {
        if (_isReflowingContainerChildren)
        {
            return;
        }

        _isReflowingContainerChildren = true;
        try
        {
            var containers = Elements
                .Where(element => IsDesignerContainer(element.Visual))
                .ToDictionary(element => element.DisplayName, StringComparer.OrdinalIgnoreCase);
            foreach (var element in Elements.Where(element => element.ParentName is not null))
            {
                if (!containers.TryGetValue(element.ParentName!, out var parent)
                    || ReferenceEquals(parent, element))
                {
                    ResetContainerRelationship(element);
                    continue;
                }

                if (parent.Visual is not SplitView)
                {
                    element.SplitViewSlot = DesignerSplitViewSlot.Content;
                }

                if (parent.Visual is Grid)
                {
                    element.ParentLayout = DesignerParentLayoutKind.Grid;
                    element.StackPanelIndex = -1;
                    element.DockPanelIndex = -1;
                    element.WrapPanelIndex = -1;
                    element.UniformGridIndex = -1;
                    element.CanvasChildIndex = -1;
                    element.TabIndex = -1;
                    element.TabHeader = null;
                }
                else if (parent.Visual is StackPanel)
                {
                    element.ParentLayout = DesignerParentLayoutKind.StackPanel;
                    element.DockPanelIndex = -1;
                    element.WrapPanelIndex = -1;
                    element.UniformGridIndex = -1;
                    element.CanvasChildIndex = -1;
                    element.TabIndex = -1;
                    element.TabHeader = null;
                    if (element.StackPanelIndex < 0)
                    {
                        element.StackPanelIndex = GetDirectChildren(parent).Count(child =>
                            child.ParentLayout == DesignerParentLayoutKind.StackPanel
                            && child.StackPanelIndex >= 0);
                    }

                    element.StackPanelItemSize = Math.Max(10, element.StackPanelItemSize);
                }
                else if (parent.Visual is DockPanel)
                {
                    element.ParentLayout = DesignerParentLayoutKind.DockPanel;
                    element.StackPanelIndex = -1;
                    element.WrapPanelIndex = -1;
                    element.UniformGridIndex = -1;
                    element.CanvasChildIndex = -1;
                    element.TabIndex = -1;
                    element.TabHeader = null;
                    if (element.DockPanelIndex < 0)
                    {
                        element.DockPanelIndex = GetDirectChildren(parent).Count(child =>
                            child.ParentLayout == DesignerParentLayoutKind.DockPanel
                            && child.DockPanelIndex >= 0);
                    }

                    element.DockPanelItemSize = Math.Max(10, element.DockPanelItemSize);
                }
                else if (parent.Visual is WrapPanel)
                {
                    element.ParentLayout = DesignerParentLayoutKind.WrapPanel;
                    element.StackPanelIndex = -1;
                    element.DockPanelIndex = -1;
                    element.UniformGridIndex = -1;
                    element.CanvasChildIndex = -1;
                    element.TabIndex = -1;
                    element.TabHeader = null;
                    if (element.WrapPanelIndex < 0)
                    {
                        element.WrapPanelIndex = GetDirectChildren(parent).Count(child =>
                            child.ParentLayout == DesignerParentLayoutKind.WrapPanel
                            && child.WrapPanelIndex >= 0);
                    }
                }
                else if (parent.Visual is UniformGrid)
                {
                    element.ParentLayout = DesignerParentLayoutKind.UniformGrid;
                    element.StackPanelIndex = -1;
                    element.DockPanelIndex = -1;
                    element.WrapPanelIndex = -1;
                    element.CanvasChildIndex = -1;
                    element.TabIndex = -1;
                    element.TabHeader = null;
                    if (element.UniformGridIndex < 0)
                    {
                        element.UniformGridIndex = GetDirectChildren(parent).Count(child =>
                            child.ParentLayout == DesignerParentLayoutKind.UniformGrid
                            && child.UniformGridIndex >= 0);
                    }
                }
                else if (parent.Visual is Canvas)
                {
                    element.ParentLayout = DesignerParentLayoutKind.Canvas;
                    element.StackPanelIndex = -1;
                    element.DockPanelIndex = -1;
                    element.WrapPanelIndex = -1;
                    element.UniformGridIndex = -1;
                    element.TabIndex = -1;
                    element.TabHeader = null;
                    if (element.CanvasChildIndex < 0)
                    {
                        element.CanvasChildIndex = GetDirectChildren(parent).Count(child =>
                            child.ParentLayout == DesignerParentLayoutKind.Canvas
                            && child.CanvasChildIndex >= 0);
                    }
                }
                else if (parent.Visual is TabControl tabControl)
                {
                    var headers = GetTabHeaders(tabControl);
                    element.ParentLayout = DesignerParentLayoutKind.TabControl;
                    element.StackPanelIndex = -1;
                    element.DockPanelIndex = -1;
                    element.WrapPanelIndex = -1;
                    element.UniformGridIndex = -1;
                    element.CanvasChildIndex = -1;
                    element.CanvasChildLeft = 0;
                    element.CanvasChildTop = 0;
                    if (element.TabIndex < 0 || element.TabIndex >= headers.Count)
                    {
                        element.TabIndex = FindFirstAvailableTabIndex(parent, headers.Count, element);
                    }

                    element.TabHeader = element.TabIndex >= 0 && element.TabIndex < headers.Count
                        ? headers[element.TabIndex]
                        : null;
                }
                else if (parent.Visual is SplitView)
                {
                    element.ParentLayout = DesignerParentLayoutKind.SplitView;
                    element.StackPanelIndex = -1;
                    element.DockPanelIndex = -1;
                    element.WrapPanelIndex = -1;
                    element.UniformGridIndex = -1;
                    element.CanvasChildIndex = -1;
                    element.CanvasChildLeft = 0;
                    element.CanvasChildTop = 0;
                    element.TabIndex = -1;
                    element.TabHeader = null;
                }
                else
                {
                    element.ParentLayout = DesignerParentLayoutKind.Content;
                    element.StackPanelIndex = -1;
                    element.DockPanelIndex = -1;
                    element.WrapPanelIndex = -1;
                    element.UniformGridIndex = -1;
                    element.CanvasChildIndex = -1;
                    element.TabIndex = -1;
                    element.TabHeader = null;
                }
            }

            foreach (var contentParent in containers.Values.Where(parent =>
                         IsContentContainer(parent.Visual)))
            {
                var contentChildren = GetDirectChildren(contentParent);
                if (contentChildren.Count > 0)
                {
                    ClearBuiltInContent(contentParent.Visual);
                }

                foreach (var extraChild in contentChildren.Skip(1))
                {
                    ResetContainerRelationship(extraChild);
                }
            }

            foreach (var tabParent in containers.Values.Where(parent => parent.Visual is TabControl))
            {
                var headers = GetTabHeaders((TabControl)tabParent.Visual);
                var occupiedTabs = new HashSet<int>();
                foreach (var child in GetDirectChildren(tabParent)
                             .OrderBy(candidate => candidate.TabIndex)
                             .ThenBy(Elements.IndexOf))
                {
                    if (!child.IsTabControlChild
                        || child.TabIndex < 0
                        || child.TabIndex >= headers.Count
                        || !occupiedTabs.Add(child.TabIndex))
                    {
                        ResetContainerRelationship(child);
                        continue;
                    }

                    child.TabHeader = headers[child.TabIndex];
                }
            }

            foreach (var splitParent in containers.Values.Where(parent => parent.Visual is SplitView))
            {
                var occupiedSlots = new HashSet<DesignerSplitViewSlot>();
                foreach (var child in GetDirectChildren(splitParent)
                             .OrderBy(candidate => candidate.SplitViewSlot)
                             .ThenBy(Elements.IndexOf))
                {
                    if (!child.IsSplitViewChild || !occupiedSlots.Add(child.SplitViewSlot))
                    {
                        ResetContainerRelationship(child);
                        continue;
                    }

                    ClearBuiltInSplitViewSlot((SplitView)splitParent.Visual, child.SplitViewSlot);
                }
            }
        }
        finally
        {
            _isReflowingContainerChildren = false;
        }

        ReflowContainerChildren();
    }

    public void SetStackPanelChildOrder(
        DesignElement parent,
        IReadOnlyList<DesignElement> orderedChildren)
    {
        if (parent.Visual is not StackPanel)
        {
            return;
        }

        _isReflowingContainerChildren = true;
        try
        {
            for (var index = 0; index < orderedChildren.Count; index++)
            {
                var child = orderedChildren[index];
                child.GridRow = 0;
                child.GridColumn = 0;
                child.GridRowSpan = 1;
                child.GridColumnSpan = 1;
                child.StackPanelIndex = index;
                child.DockPanelIndex = -1;
                child.DockPanelDock = DesignerDockSide.Left;
                child.DockPanelItemSize = 40;
                child.WrapPanelIndex = -1;
                child.UniformGridIndex = -1;
                child.CanvasChildIndex = -1;
                child.CanvasChildLeft = 0;
                child.CanvasChildTop = 0;
                child.TabIndex = -1;
                child.TabHeader = null;
                child.ParentLayout = DesignerParentLayoutKind.StackPanel;
                child.ParentName = parent.DisplayName;
            }

            ReflowContainerTreeCore(parent);
        }
        finally
        {
            _isReflowingContainerChildren = false;
        }
    }

    public DesignElement? SetContentChild(DesignElement parent, DesignElement child)
    {
        if (!IsContentContainer(parent.Visual) || ReferenceEquals(parent, child))
        {
            return null;
        }

        var replaced = GetDirectChildren(parent).FirstOrDefault(existing => !ReferenceEquals(existing, child));
        _isReflowingContainerChildren = true;
        try
        {
            if (replaced is not null)
            {
                ResetContainerRelationship(replaced);
            }

            child.GridRow = 0;
            child.GridColumn = 0;
            child.GridRowSpan = 1;
            child.GridColumnSpan = 1;
            child.StackPanelIndex = -1;
            child.DockPanelIndex = -1;
            child.DockPanelDock = DesignerDockSide.Left;
            child.DockPanelItemSize = 40;
            child.WrapPanelIndex = -1;
            child.UniformGridIndex = -1;
            child.CanvasChildIndex = -1;
            child.CanvasChildLeft = 0;
            child.CanvasChildTop = 0;
            child.TabIndex = -1;
            child.TabHeader = null;
            child.ParentLayout = DesignerParentLayoutKind.Content;
            child.ParentName = parent.DisplayName;
            ClearBuiltInContent(parent.Visual);
            ReflowContainerTreeCore(parent);
        }
        finally
        {
            _isReflowingContainerChildren = false;
        }

        return replaced;
    }

    public void SetDockPanelChildOrder(
        DesignElement parent,
        IReadOnlyList<DesignElement> orderedChildren)
    {
        if (parent.Visual is not DockPanel)
        {
            return;
        }

        _isReflowingContainerChildren = true;
        try
        {
            for (var index = 0; index < orderedChildren.Count; index++)
            {
                var child = orderedChildren[index];
                child.GridRow = 0;
                child.GridColumn = 0;
                child.GridRowSpan = 1;
                child.GridColumnSpan = 1;
                child.StackPanelIndex = -1;
                child.StackPanelItemSize = 40;
                child.DockPanelIndex = index;
                child.WrapPanelIndex = -1;
                child.UniformGridIndex = -1;
                child.CanvasChildIndex = -1;
                child.CanvasChildLeft = 0;
                child.CanvasChildTop = 0;
                child.TabIndex = -1;
                child.TabHeader = null;
                child.ParentLayout = DesignerParentLayoutKind.DockPanel;
                child.ParentName = parent.DisplayName;
            }

            ReflowContainerTreeCore(parent);
        }
        finally
        {
            _isReflowingContainerChildren = false;
        }
    }

    public void SetWrapPanelChildOrder(
        DesignElement parent,
        IReadOnlyList<DesignElement> orderedChildren)
    {
        if (parent.Visual is not WrapPanel)
        {
            return;
        }

        _isReflowingContainerChildren = true;
        try
        {
            for (var index = 0; index < orderedChildren.Count; index++)
            {
                var child = orderedChildren[index];
                child.GridRow = 0;
                child.GridColumn = 0;
                child.GridRowSpan = 1;
                child.GridColumnSpan = 1;
                child.StackPanelIndex = -1;
                child.StackPanelItemSize = 40;
                child.DockPanelIndex = -1;
                child.DockPanelDock = DesignerDockSide.Left;
                child.DockPanelItemSize = 40;
                child.WrapPanelIndex = index;
                child.UniformGridIndex = -1;
                child.CanvasChildIndex = -1;
                child.CanvasChildLeft = 0;
                child.CanvasChildTop = 0;
                child.TabIndex = -1;
                child.TabHeader = null;
                child.ParentLayout = DesignerParentLayoutKind.WrapPanel;
                child.ParentName = parent.DisplayName;
            }

            ReflowContainerTreeCore(parent);
        }
        finally
        {
            _isReflowingContainerChildren = false;
        }
    }

    public void SetUniformGridChildOrder(
        DesignElement parent,
        IReadOnlyList<DesignElement> orderedChildren)
    {
        if (parent.Visual is not UniformGrid)
        {
            return;
        }

        _isReflowingContainerChildren = true;
        try
        {
            for (var index = 0; index < orderedChildren.Count; index++)
            {
                var child = orderedChildren[index];
                child.GridRow = 0;
                child.GridColumn = 0;
                child.GridRowSpan = 1;
                child.GridColumnSpan = 1;
                child.StackPanelIndex = -1;
                child.StackPanelItemSize = 40;
                child.DockPanelIndex = -1;
                child.DockPanelDock = DesignerDockSide.Left;
                child.DockPanelItemSize = 40;
                child.WrapPanelIndex = -1;
                child.UniformGridIndex = index;
                child.CanvasChildIndex = -1;
                child.CanvasChildLeft = 0;
                child.CanvasChildTop = 0;
                child.TabIndex = -1;
                child.TabHeader = null;
                child.ParentLayout = DesignerParentLayoutKind.UniformGrid;
                child.ParentName = parent.DisplayName;
            }

            ReflowContainerTreeCore(parent);
        }
        finally
        {
            _isReflowingContainerChildren = false;
        }
    }

    public void SetCanvasChildOrder(
        DesignElement parent,
        IReadOnlyList<DesignElement> orderedChildren)
    {
        if (parent.Visual is not Canvas)
        {
            return;
        }

        _isReflowingContainerChildren = true;
        try
        {
            for (var index = 0; index < orderedChildren.Count; index++)
            {
                var child = orderedChildren[index];
                SetCanvasChildRelationship(
                    child,
                    parent,
                    index,
                    child.CanvasChildLeft,
                    child.CanvasChildTop);
            }

            ReflowContainerTreeCore(parent);
        }
        finally
        {
            _isReflowingContainerChildren = false;
        }
    }

    public bool TryCreateCanvasGroup(
        IEnumerable<DesignElement> requested,
        out DesignElement? group,
        out string error)
    {
        group = null;
        error = string.Empty;

        var targets = requested
            .Where(Elements.Contains)
            .Distinct()
            .ToList();
        if (targets.Count < 2)
        {
            error = "Select at least two controls to group.";
            return false;
        }

        if (targets.Any(element => element.IsLocked))
        {
            error = "Locked controls cannot be grouped.";
            return false;
        }

        var parentName = targets[0].ParentName;
        if (targets.Any(element => !string.Equals(
                element.ParentName,
                parentName,
                StringComparison.OrdinalIgnoreCase)))
        {
            error = "Group controls must share the same parent.";
            return false;
        }

        var parent = parentName is null
            ? null
            : Elements.FirstOrDefault(element => string.Equals(
                element.DisplayName,
                parentName,
                StringComparison.OrdinalIgnoreCase));
        if (parentName is not null && parent?.Visual is not Canvas)
        {
            error = "Only controls on the root or inside the same Canvas can be grouped.";
            return false;
        }

        if (parent is null && targets.Any(element => element.IsContainerChild))
        {
            error = "The selected controls have an invalid parent relationship.";
            return false;
        }

        var orderedTargets = parent is null
            ? targets.OrderBy(Elements.IndexOf).ToList()
            : targets
                .OrderBy(element => element.CanvasChildIndex)
                .ThenBy(Elements.IndexOf)
                .ToList();
        var left = orderedTargets.Min(element => element.X);
        var top = orderedTargets.Min(element => element.Y);
        var right = orderedTargets.Max(element => element.X + element.Width);
        var bottom = orderedTargets.Max(element => element.Y + element.Height);
        var width = Math.Max(10, right - left);
        var height = Math.Max(10, bottom - top);
        var groupName = BuildUniqueDisplayName("Group");
        var groupVisual = new Canvas { Background = Brushes.Transparent };
        group = new DesignElement(
            groupName,
            "Avalonia.Controls.Canvas",
            groupVisual,
            left,
            top,
            width,
            height);
        group.PropertyChanged += OnDesignElementPropertyChanged;

        var insertIndex = Math.Max(0, targets.Min(Elements.IndexOf));
        Elements.Insert(Math.Min(insertIndex, Elements.Count), group);
        var originalSiblings = parent is null
            ? new List<DesignElement>()
            : GetDirectChildren(parent)
                .OrderBy(child => child.CanvasChildIndex)
                .ThenBy(Elements.IndexOf)
                .ToList();
        var originalGroupIndex = parent is null
            ? 0
            : orderedTargets
                .Select(child => originalSiblings.IndexOf(child))
                .Where(index => index >= 0)
                .DefaultIfEmpty(originalSiblings.Count)
                .Min();
        _isReflowingContainerChildren = true;
        try
        {
            foreach (var (child, index) in orderedTargets.Select((child, index) => (child, index)))
            {
                SetCanvasChildRelationship(
                    child,
                    group,
                    index,
                    child.X - left,
                    child.Y - top);
            }

            if (parent is not null)
            {
                var siblings = originalSiblings
                    .Where(child => !targets.Contains(child))
                    .ToList();
                var groupIndex = Math.Clamp(
                    originalSiblings.Take(originalGroupIndex).Count(child => !targets.Contains(child)),
                    0,
                    siblings.Count);
                siblings.Insert(groupIndex, group);
                SetCanvasChildRelationship(
                    group,
                    parent,
                    groupIndex,
                    left - parent.X,
                    top - parent.Y);
                SetCanvasChildOrder(parent, siblings);
            }
        }
        finally
        {
            _isReflowingContainerChildren = false;
        }

        NormalizeContainerRelationships();
        ReflowContainerChildren();
        ResolveLabelTargets();
        return true;
    }

    public bool TryUngroupCanvas(
        DesignElement requested,
        out IReadOnlyList<DesignElement> children,
        out string error)
    {
        children = Array.Empty<DesignElement>();
        error = string.Empty;
        if (!Elements.Contains(requested)
            || requested.IsLocked
            || requested.Visual is not Canvas)
        {
            error = "Select an unlocked Canvas group to ungroup.";
            return false;
        }

        var groupChildren = GetDirectChildren(requested)
            .Where(child => child.IsCanvasChild)
            .OrderBy(child => child.CanvasChildIndex)
            .ThenBy(Elements.IndexOf)
            .ToList();
        if (groupChildren.Count == 0)
        {
            error = "The selected Canvas has no direct children to ungroup.";
            return false;
        }

        var parent = FindParent(requested);
        if (parent is not null && parent.Visual is not Canvas)
        {
            error = "Only a root Canvas group or a group inside another Canvas can be ungrouped.";
            return false;
        }

        var groupIndex = Elements.IndexOf(requested);
        var formerParentChildren = parent is null
            ? null
            : GetDirectChildren(parent)
                .Where(child => !ReferenceEquals(child, requested))
                .OrderBy(child => child.CanvasChildIndex)
                .ThenBy(Elements.IndexOf)
                .ToList();

        _isReflowingContainerChildren = true;
        try
        {
            Elements.Remove(requested);
            if (parent is null)
            {
                foreach (var child in groupChildren)
                {
                    Elements.Remove(child);
                    ResetContainerRelationship(child);
                }

                var insertionIndex = Math.Clamp(groupIndex, 0, Elements.Count);
                foreach (var child in groupChildren)
                {
                    Elements.Insert(insertionIndex++, child);
                }
            }
            else
            {
                var parentIndex = formerParentChildren?.Count ?? 0;
                if (formerParentChildren is not null)
                {
                    parentIndex = Math.Clamp(requested.CanvasChildIndex, 0, formerParentChildren.Count);
                    foreach (var child in groupChildren)
                    {
                        SetCanvasChildRelationship(
                            child,
                            parent,
                            parentIndex++,
                            child.X - parent.X,
                            child.Y - parent.Y);
                    }

                    formerParentChildren.InsertRange(
                        Math.Clamp(requested.CanvasChildIndex, 0, formerParentChildren.Count),
                        groupChildren);
                    SetCanvasChildOrder(parent, formerParentChildren);
                }
            }
        }
        finally
        {
            _isReflowingContainerChildren = false;
        }

        if (requested.Visual is Canvas groupVisual)
        {
            groupVisual.Children.Clear();
        }
        NormalizeContainerRelationships();
        ReflowContainerChildren();
        ResolveLabelTargets();
        children = groupChildren;
        return true;
    }

    public DesignElement? SetTabControlChild(
        DesignElement parent,
        DesignElement child,
        int tabIndex)
    {
        if (parent.Visual is not TabControl tabControl || ReferenceEquals(parent, child))
        {
            return null;
        }

        var headers = GetTabHeaders(tabControl);
        if (tabIndex < 0 || tabIndex >= headers.Count)
        {
            return null;
        }

        var replaced = GetDirectChildren(parent).FirstOrDefault(existing =>
            !ReferenceEquals(existing, child)
            && existing.IsTabControlChild
            && existing.TabIndex == tabIndex);
        _isReflowingContainerChildren = true;
        try
        {
            if (replaced is not null)
            {
                ResetContainerRelationship(replaced);
            }

            child.GridRow = 0;
            child.GridColumn = 0;
            child.GridRowSpan = 1;
            child.GridColumnSpan = 1;
            child.StackPanelIndex = -1;
            child.StackPanelItemSize = 40;
            child.DockPanelIndex = -1;
            child.DockPanelDock = DesignerDockSide.Left;
            child.DockPanelItemSize = 40;
            child.WrapPanelIndex = -1;
            child.UniformGridIndex = -1;
            child.CanvasChildIndex = -1;
            child.CanvasChildLeft = 0;
            child.CanvasChildTop = 0;
            child.TabIndex = tabIndex;
            child.TabHeader = headers[tabIndex];
            child.ParentLayout = DesignerParentLayoutKind.TabControl;
            child.ParentName = parent.DisplayName;
            tabControl.SelectedIndex = tabIndex;
            ReflowContainerTreeCore(parent);
        }
        finally
        {
            _isReflowingContainerChildren = false;
        }

        return replaced;
    }

    public IReadOnlyList<DesignElement> SynchronizeTabControlChildren(
        DesignElement parent,
        IReadOnlyList<string> headers)
    {
        if (parent.Visual is not TabControl)
        {
            return Array.Empty<DesignElement>();
        }

        var promoted = new List<DesignElement>();
        _isReflowingContainerChildren = true;
        try
        {
            var occupiedTabs = new HashSet<int>();
            foreach (var child in GetDirectChildren(parent)
                         .OrderBy(candidate => candidate.TabIndex)
                         .ThenBy(Elements.IndexOf))
            {
                if (!child.IsTabControlChild
                    || child.TabIndex < 0
                    || child.TabIndex >= headers.Count
                    || !occupiedTabs.Add(child.TabIndex))
                {
                    ResetContainerRelationship(child);
                    promoted.Add(child);
                    continue;
                }

                child.TabHeader = headers[child.TabIndex];
            }

            ReflowContainerTreeCore(parent);
        }
        finally
        {
            _isReflowingContainerChildren = false;
        }

        return promoted;
    }

    public DesignElement? SetSplitViewChild(
        DesignElement parent,
        DesignElement child,
        DesignerSplitViewSlot slot)
    {
        if (parent.Visual is not SplitView splitView || ReferenceEquals(parent, child))
        {
            return null;
        }

        var replaced = GetDirectChildren(parent).FirstOrDefault(existing =>
            !ReferenceEquals(existing, child)
            && existing.IsSplitViewChild
            && existing.SplitViewSlot == slot);
        _isReflowingContainerChildren = true;
        try
        {
            if (replaced is not null)
            {
                ResetContainerRelationship(replaced);
            }

            child.GridRow = 0;
            child.GridColumn = 0;
            child.GridRowSpan = 1;
            child.GridColumnSpan = 1;
            child.StackPanelIndex = -1;
            child.StackPanelItemSize = 40;
            child.DockPanelIndex = -1;
            child.DockPanelDock = DesignerDockSide.Left;
            child.DockPanelItemSize = 40;
            child.WrapPanelIndex = -1;
            child.UniformGridIndex = -1;
            child.CanvasChildIndex = -1;
            child.CanvasChildLeft = 0;
            child.CanvasChildTop = 0;
            child.TabIndex = -1;
            child.TabHeader = null;
            child.SplitViewSlot = slot;
            child.ParentLayout = DesignerParentLayoutKind.SplitView;
            child.ParentName = parent.DisplayName;
            ClearBuiltInSplitViewSlot(splitView, slot);
            ReflowContainerTreeCore(parent);
        }
        finally
        {
            _isReflowingContainerChildren = false;
        }

        return replaced;
    }

    public bool MoveElementsToFront(IEnumerable<DesignElement> elements)
    {
        var moving = Elements.Where(elements.Contains).ToList();
        if (moving.Count == 0 || Elements.Skip(Elements.Count - moving.Count).SequenceEqual(moving))
        {
            return false;
        }

        foreach (var element in moving)
        {
            Elements.Remove(element);
        }

        foreach (var element in moving)
        {
            Elements.Add(element);
        }

        return true;
    }

    public bool MoveElementsToFrontInOrder(IEnumerable<DesignElement> elements)
    {
        var moving = elements
            .Where(Elements.Contains)
            .Distinct()
            .ToList();
        if (moving.Count == 0)
        {
            return false;
        }

        var current = Elements.Where(moving.Contains).ToList();
        var alreadyAtFront = Elements.Skip(Elements.Count - moving.Count).SequenceEqual(moving);
        if (alreadyAtFront && current.SequenceEqual(moving))
        {
            return false;
        }

        foreach (var element in moving)
        {
            Elements.Remove(element);
        }

        foreach (var element in moving)
        {
            Elements.Add(element);
        }

        return true;
    }

    public bool MoveElementsToBack(IEnumerable<DesignElement> elements)
    {
        var moving = Elements.Where(elements.Contains).ToList();
        if (moving.Count == 0 || Elements.Take(moving.Count).SequenceEqual(moving))
        {
            return false;
        }

        foreach (var element in moving)
        {
            Elements.Remove(element);
        }

        for (var index = moving.Count - 1; index >= 0; index--)
        {
            Elements.Insert(0, moving[index]);
        }

        return true;
    }

    public bool MoveElementsForward(IEnumerable<DesignElement> elements)
    {
        var moving = new HashSet<DesignElement>(elements);
        var changed = false;

        // Walk from front to back so a selected block keeps its internal order.
        for (var index = Elements.Count - 2; index >= 0; index--)
        {
            if (!moving.Contains(Elements[index]) || moving.Contains(Elements[index + 1]))
            {
                continue;
            }

            var next = Elements[index + 1];
            Elements[index + 1] = Elements[index];
            Elements[index] = next;
            changed = true;
        }

        return changed;
    }

    public bool MoveElementsBackward(IEnumerable<DesignElement> elements)
    {
        var moving = new HashSet<DesignElement>(elements);
        var changed = false;

        // Walk from back to front so a selected block keeps its internal order.
        for (var index = 1; index < Elements.Count; index++)
        {
            if (!moving.Contains(Elements[index]) || moving.Contains(Elements[index - 1]))
            {
                continue;
            }

            var previous = Elements[index - 1];
            Elements[index - 1] = Elements[index];
            Elements[index] = previous;
            changed = true;
        }

        return changed;
    }

    private void ReplaceSelection(IEnumerable<DesignElement> elements)
    {
        var next = elements
            .Where(Elements.Contains)
            .Distinct()
            .ToList();
        if (_stylePreviewControl is not null
            && (next.Count != 1 || !ReferenceEquals(next[0].Visual, _stylePreviewControl)))
        {
            ClearStylePreviewState();
        }

        foreach (var element in SelectedElements)
        {
            element.IsSelected = false;
        }

        SelectedElements.Clear();
        foreach (var element in next)
        {
            element.IsSelected = true;
            SelectedElements.Add(element);
        }

        SelectedElement = next.LastOrDefault();
        RefreshTabChildVisibility();
    }

    private void OnElementsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => HasElements = Elements.Count > 0;

    private void OnSelectedElementsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsSelectionActive));
        OnPropertyChanged(nameof(HasMultipleSelection));
    }

    private void SetStylePreviewBadge(Control visual, string? label)
    {
        var element = Elements.FirstOrDefault(candidate => ReferenceEquals(candidate.Visual, visual));
        if (element is not null)
        {
            element.StylePreviewStateLabel = label;
        }
    }

    private DesignElement AddElement(
        string displayPrefix,
        string typeName,
        Control visual,
        double x,
        double y,
        double width,
        double height,
        bool select,
        bool preserveDisplayName = false)
    {
        var element = new DesignElement(
            displayName: preserveDisplayName ? displayPrefix : BuildUniqueDisplayName(displayPrefix),
            typeName: typeName,
            visual: visual,
            x: x,
            y: y,
            width: width,
            height: height);

        element.PropertyChanged += OnDesignElementPropertyChanged;
        Elements.Add(element);
        if (select)
        {
            Select(element);
        }

        return element;
    }

    private void ReflowContainerChild(DesignElement child)
    {
        if (_isReflowingContainerChildren)
        {
            return;
        }

        _isReflowingContainerChildren = true;
        try
        {
            var parent = FindParent(child);
            if (parent is not null)
            {
                ReflowContainerTreeCore(parent);
            }
            else
            {
                ReflowContainerChildCore(child);
            }
        }
        finally
        {
            _isReflowingContainerChildren = false;
        }
    }

    private void ReflowContainerChildCore(DesignElement child)
    {
        if (!child.IsGridChild
            || FindParent(child) is not { Visual: Grid grid } parent)
        {
            return;
        }

        var rowCount = DesignerGridDefinitionRuntime.GetRowCount(grid);
        var columnCount = DesignerGridDefinitionRuntime.GetColumnCount(grid);
        child.GridRow = Math.Clamp(child.GridRow, 0, rowCount - 1);
        child.GridColumn = Math.Clamp(child.GridColumn, 0, columnCount - 1);
        child.GridRowSpan = Math.Clamp(child.GridRowSpan, 1, rowCount - child.GridRow);
        child.GridColumnSpan = Math.Clamp(child.GridColumnSpan, 1, columnCount - child.GridColumn);
        var bounds = DesignerGridDefinitionRuntime.GetCellBounds(
            grid,
            new Rect(parent.X, parent.Y, parent.Width, parent.Height),
            child.GridRow,
            child.GridColumn,
            child.GridRowSpan,
            child.GridColumnSpan);
        child.X = bounds.X;
        child.Y = bounds.Y;
        child.Width = Math.Max(10, bounds.Width);
        child.Height = Math.Max(10, bounds.Height);
    }

    private void ReflowContainerTreeCore(
        DesignElement parent,
        HashSet<DesignElement>? visited = null)
    {
        visited ??= [];
        if (!visited.Add(parent))
        {
            return;
        }

        if (parent.Visual is not TabControl)
        {
            foreach (var child in GetDirectChildren(parent))
            {
                child.IsVisibleOnArtboard = true;
            }
        }

        if (parent.Visual is StackPanel stackPanel)
        {
            ReflowStackPanelChildrenCore(parent, stackPanel);
        }
        else if (parent.Visual is DockPanel dockPanel)
        {
            ReflowDockPanelChildrenCore(parent, dockPanel);
        }
        else if (parent.Visual is WrapPanel wrapPanel)
        {
            ReflowWrapPanelChildrenCore(parent, wrapPanel);
        }
        else if (parent.Visual is UniformGrid uniformGrid)
        {
            ReflowUniformGridChildrenCore(parent, uniformGrid);
        }
        else if (parent.Visual is Canvas canvas)
        {
            ReflowCanvasChildrenCore(parent, canvas);
        }
        else if (parent.Visual is TabControl tabControl)
        {
            ReflowTabControlChildrenCore(parent, tabControl);
        }
        else if (parent.Visual is SplitView splitView)
        {
            ReflowSplitViewChildrenCore(parent, splitView);
        }
        else if (parent.Visual is Grid)
        {
            foreach (var child in GetDirectChildren(parent))
            {
                ReflowContainerChildCore(child);
            }
        }
        else if (IsContentContainer(parent.Visual)
                 && GetDirectChildren(parent).FirstOrDefault() is { } contentChild)
        {
            ReflowContentChildCore(parent, contentChild);
        }

        foreach (var childContainer in GetDirectChildren(parent).Where(child =>
                     IsDesignerContainer(child.Visual)))
        {
            ReflowContainerTreeCore(childContainer, visited);
        }
    }

    private void ReflowStackPanelChildrenCore(DesignElement parent, StackPanel stackPanel)
    {
        var children = GetDirectChildren(parent)
            .Where(child => child.IsStackPanelChild)
            .OrderBy(child => child.StackPanelIndex)
            .ThenBy(Elements.IndexOf)
            .ToList();
        if (children.Count == 0)
        {
            return;
        }

        if (stackPanel.Children.Count > 0)
        {
            stackPanel.Children.Clear();
        }

        var offset = 0d;
        for (var index = 0; index < children.Count; index++)
        {
            var child = children[index];
            child.StackPanelIndex = index;
            child.StackPanelItemSize = Math.Max(10, child.StackPanelItemSize);
            if (stackPanel.Orientation == Orientation.Vertical)
            {
                child.X = parent.X;
                child.Y = parent.Y + offset;
                child.Width = Math.Max(10, parent.Width);
                child.Height = child.StackPanelItemSize;
                offset += child.Height + stackPanel.Spacing;
            }
            else
            {
                child.X = parent.X + offset;
                child.Y = parent.Y;
                child.Width = child.StackPanelItemSize;
                child.Height = Math.Max(10, parent.Height);
                offset += child.Width + stackPanel.Spacing;
            }
        }
    }

    private static void ReflowContentChildCore(DesignElement parent, DesignElement child)
    {
        if (!child.IsContentChild)
        {
            return;
        }

        var left = 0d;
        var top = 0d;
        var right = 0d;
        var bottom = 0d;
        if (parent.Visual is Border border)
        {
            left = border.BorderThickness.Left;
            top = border.BorderThickness.Top;
            right = border.BorderThickness.Right;
            bottom = border.BorderThickness.Bottom;
        }
        else if (parent.Visual is Expander expander)
        {
            if (expander.ExpandDirection is ExpandDirection.Down or ExpandDirection.Up)
            {
                var headerHeight = Math.Min(32, Math.Max(0, parent.Height - 10));
                if (expander.ExpandDirection == ExpandDirection.Down)
                {
                    top = headerHeight;
                }
                else
                {
                    bottom = headerHeight;
                }
            }
            else
            {
                var headerWidth = Math.Min(32, Math.Max(0, parent.Width - 10));
                if (expander.ExpandDirection == ExpandDirection.Right)
                {
                    left = headerWidth;
                }
                else
                {
                    right = headerWidth;
                }
            }
        }

        child.X = parent.X + left;
        child.Y = parent.Y + top;
        child.Width = Math.Max(10, parent.Width - left - right);
        child.Height = Math.Max(10, parent.Height - top - bottom);
    }

    private void ReflowDockPanelChildrenCore(DesignElement parent, DockPanel dockPanel)
    {
        var children = GetDirectChildren(parent)
            .Where(child => child.IsDockPanelChild)
            .OrderBy(child => child.DockPanelIndex)
            .ThenBy(Elements.IndexOf)
            .ToList();
        if (children.Count == 0)
        {
            return;
        }

        dockPanel.Children.Clear();
        var remaining = new Rect(parent.X, parent.Y, parent.Width, parent.Height);
        for (var index = 0; index < children.Count; index++)
        {
            var child = children[index];
            child.DockPanelIndex = index;
            child.DockPanelItemSize = Math.Max(10, child.DockPanelItemSize);
            if (dockPanel.LastChildFill && index == children.Count - 1)
            {
                SetElementBounds(child, remaining);
                continue;
            }

            switch (child.DockPanelDock)
            {
                case DesignerDockSide.Top:
                    var topHeight = Math.Min(child.DockPanelItemSize, Math.Max(10, remaining.Height));
                    SetElementBounds(child, new Rect(remaining.X, remaining.Y, remaining.Width, topHeight));
                    remaining = new Rect(
                        remaining.X,
                        remaining.Y + topHeight,
                        remaining.Width,
                        Math.Max(0, remaining.Height - topHeight));
                    break;
                case DesignerDockSide.Right:
                    var rightWidth = Math.Min(child.DockPanelItemSize, Math.Max(10, remaining.Width));
                    SetElementBounds(child, new Rect(
                        remaining.Right - rightWidth,
                        remaining.Y,
                        rightWidth,
                        remaining.Height));
                    remaining = new Rect(
                        remaining.X,
                        remaining.Y,
                        Math.Max(0, remaining.Width - rightWidth),
                        remaining.Height);
                    break;
                case DesignerDockSide.Bottom:
                    var bottomHeight = Math.Min(child.DockPanelItemSize, Math.Max(10, remaining.Height));
                    SetElementBounds(child, new Rect(
                        remaining.X,
                        remaining.Bottom - bottomHeight,
                        remaining.Width,
                        bottomHeight));
                    remaining = new Rect(
                        remaining.X,
                        remaining.Y,
                        remaining.Width,
                        Math.Max(0, remaining.Height - bottomHeight));
                    break;
                default:
                    var leftWidth = Math.Min(child.DockPanelItemSize, Math.Max(10, remaining.Width));
                    SetElementBounds(child, new Rect(remaining.X, remaining.Y, leftWidth, remaining.Height));
                    remaining = new Rect(
                        remaining.X + leftWidth,
                        remaining.Y,
                        Math.Max(0, remaining.Width - leftWidth),
                        remaining.Height);
                    break;
            }
        }
    }

    private void ReflowWrapPanelChildrenCore(DesignElement parent, WrapPanel wrapPanel)
    {
        var children = GetDirectChildren(parent)
            .Where(child => child.IsWrapPanelChild)
            .OrderBy(child => child.WrapPanelIndex)
            .ThenBy(Elements.IndexOf)
            .ToList();
        if (children.Count == 0)
        {
            return;
        }

        wrapPanel.Children.Clear();
        var itemWidth = double.IsFinite(wrapPanel.ItemWidth) && wrapPanel.ItemWidth > 0
            ? Math.Max(10, wrapPanel.ItemWidth)
            : 96;
        var itemHeight = double.IsFinite(wrapPanel.ItemHeight) && wrapPanel.ItemHeight > 0
            ? Math.Max(10, wrapPanel.ItemHeight)
            : 36;
        var itemSpacing = double.IsFinite(wrapPanel.ItemSpacing)
            ? Math.Max(0, wrapPanel.ItemSpacing)
            : 0;
        var lineSpacing = double.IsFinite(wrapPanel.LineSpacing)
            ? Math.Max(0, wrapPanel.LineSpacing)
            : 0;

        if (wrapPanel.Orientation == Orientation.Horizontal)
        {
            var itemsPerLine = Math.Max(
                1,
                (int)Math.Floor((parent.Width + itemSpacing) / (itemWidth + itemSpacing)));
            for (var index = 0; index < children.Count; index++)
            {
                var line = index / itemsPerLine;
                var item = index % itemsPerLine;
                var lineItemCount = Math.Min(itemsPerLine, children.Count - (line * itemsPerLine));
                var lineWidth = (lineItemCount * itemWidth) + ((lineItemCount - 1) * itemSpacing);
                var offset = GetWrapAlignmentOffset(parent.Width, lineWidth, wrapPanel.ItemsAlignment);
                var child = children[index];
                child.WrapPanelIndex = index;
                SetElementBounds(
                    child,
                    new Rect(
                        parent.X + offset + (item * (itemWidth + itemSpacing)),
                        parent.Y + (line * (itemHeight + lineSpacing)),
                        itemWidth,
                        itemHeight));
            }
        }
        else
        {
            var itemsPerLine = Math.Max(
                1,
                (int)Math.Floor((parent.Height + itemSpacing) / (itemHeight + itemSpacing)));
            for (var index = 0; index < children.Count; index++)
            {
                var line = index / itemsPerLine;
                var item = index % itemsPerLine;
                var lineItemCount = Math.Min(itemsPerLine, children.Count - (line * itemsPerLine));
                var lineHeight = (lineItemCount * itemHeight) + ((lineItemCount - 1) * itemSpacing);
                var offset = GetWrapAlignmentOffset(parent.Height, lineHeight, wrapPanel.ItemsAlignment);
                var child = children[index];
                child.WrapPanelIndex = index;
                SetElementBounds(
                    child,
                    new Rect(
                        parent.X + (line * (itemWidth + lineSpacing)),
                        parent.Y + offset + (item * (itemHeight + itemSpacing)),
                        itemWidth,
                        itemHeight));
            }
        }
    }

    private static double GetWrapAlignmentOffset(
        double available,
        double occupied,
        WrapPanelItemsAlignment alignment)
        => alignment switch
        {
            WrapPanelItemsAlignment.Center => Math.Max(0, (available - occupied) / 2),
            WrapPanelItemsAlignment.End => Math.Max(0, available - occupied),
            _ => 0,
        };

    private void ReflowUniformGridChildrenCore(DesignElement parent, UniformGrid uniformGrid)
    {
        var children = GetDirectChildren(parent)
            .Where(child => child.IsUniformGridChild)
            .OrderBy(child => child.UniformGridIndex)
            .ThenBy(Elements.IndexOf)
            .ToList();
        if (children.Count == 0)
        {
            return;
        }

        uniformGrid.Children.Clear();
        var rows = Math.Max(0, uniformGrid.Rows);
        var columns = Math.Max(0, uniformGrid.Columns);
        var firstColumn = Math.Max(0, uniformGrid.FirstColumn);
        if (rows == 0 && columns == 0)
        {
            columns = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(children.Count + firstColumn)));
            firstColumn = Math.Min(firstColumn, columns - 1);
            rows = Math.Max(1, (int)Math.Ceiling((children.Count + firstColumn) / (double)columns));
        }
        else if (columns == 0)
        {
            rows = Math.Max(1, rows);
            columns = Math.Max(1, (int)Math.Ceiling((children.Count + firstColumn) / (double)rows));
            firstColumn = Math.Min(firstColumn, columns - 1);
        }
        else
        {
            columns = Math.Max(1, columns);
            firstColumn = Math.Min(firstColumn, columns - 1);
            rows = rows == 0
                ? Math.Max(1, (int)Math.Ceiling((children.Count + firstColumn) / (double)columns))
                : Math.Max(rows, (int)Math.Ceiling((children.Count + firstColumn) / (double)columns));
        }

        var rowSpacing = Math.Max(0, uniformGrid.RowSpacing);
        var columnSpacing = Math.Max(0, uniformGrid.ColumnSpacing);
        var cellWidth = Math.Max(10, (parent.Width - ((columns - 1) * columnSpacing)) / columns);
        var cellHeight = Math.Max(10, (parent.Height - ((rows - 1) * rowSpacing)) / rows);
        for (var index = 0; index < children.Count; index++)
        {
            var cellIndex = firstColumn + index;
            var row = cellIndex / columns;
            var column = cellIndex % columns;
            var child = children[index];
            child.UniformGridIndex = index;
            SetElementBounds(
                child,
                new Rect(
                    parent.X + (column * (cellWidth + columnSpacing)),
                    parent.Y + (row * (cellHeight + rowSpacing)),
                    cellWidth,
                    cellHeight));
        }
    }

    private void ReflowCanvasChildrenCore(DesignElement parent, Canvas canvas)
    {
        var children = GetDirectChildren(parent)
            .Where(child => child.IsCanvasChild)
            .OrderBy(child => child.CanvasChildIndex)
            .ThenBy(Elements.IndexOf)
            .ToList();
        canvas.Children.Clear();
        for (var index = 0; index < children.Count; index++)
        {
            var child = children[index];
            child.CanvasChildIndex = index;
            child.X = parent.X + child.CanvasChildLeft;
            child.Y = parent.Y + child.CanvasChildTop;
        }
    }

    private void ReflowTabControlChildrenCore(DesignElement parent, TabControl tabControl)
    {
        var headers = GetTabHeaders(tabControl);
        var children = GetDirectChildren(parent)
            .Where(child => child.IsTabControlChild)
            .OrderBy(child => child.TabIndex)
            .ThenBy(Elements.IndexOf)
            .ToList();
        var activeTabIndex = SelectedElement is { IsTabControlChild: true } selectedChild
            && string.Equals(
                selectedChild.ParentName,
                parent.DisplayName,
                StringComparison.OrdinalIgnoreCase)
                ? selectedChild.TabIndex
                : tabControl.SelectedIndex;
        var occupiedTabs = new HashSet<int>();
        foreach (var child in children)
        {
            if (child.TabIndex < 0
                || child.TabIndex >= headers.Count
                || !occupiedTabs.Add(child.TabIndex))
            {
                ResetContainerRelationship(child);
                continue;
            }

            child.TabHeader = headers[child.TabIndex];
            child.IsVisibleOnArtboard = child.TabIndex == activeTabIndex;
            SetElementBounds(
                child,
                new Rect(
                    parent.X + 8,
                    parent.Y + 40,
                    Math.Max(10, parent.Width - 16),
                    Math.Max(10, parent.Height - 48)));
        }
    }

    private void ReflowSplitViewChildrenCore(DesignElement parent, SplitView splitView)
    {
        var children = GetDirectChildren(parent)
            .Where(child => child.IsSplitViewChild)
            .OrderBy(child => child.SplitViewSlot)
            .ThenBy(Elements.IndexOf)
            .ToList();
        if (children.Count == 0)
        {
            return;
        }

        var isCompact = splitView.DisplayMode is
            SplitViewDisplayMode.CompactInline or SplitViewDisplayMode.CompactOverlay;
        var paneLength = splitView.IsPaneOpen
            ? splitView.OpenPaneLength
            : isCompact
                ? splitView.CompactPaneLength
                : 0;
        paneLength = double.IsFinite(paneLength) ? Math.Max(0, paneLength) : 0;
        var isInline = splitView.DisplayMode is
            SplitViewDisplayMode.Inline or SplitViewDisplayMode.CompactInline;
        var parentBounds = new Rect(parent.X, parent.Y, parent.Width, parent.Height);
        Rect paneBounds;
        Rect contentBounds;
        switch (splitView.PanePlacement)
        {
            case SplitViewPanePlacement.Right:
            {
                var width = Math.Min(paneLength, parent.Width);
                paneBounds = new Rect(parentBounds.Right - width, parent.Y, Math.Max(10, width), parent.Height);
                contentBounds = isInline
                    ? new Rect(parent.X, parent.Y, Math.Max(10, parent.Width - width), parent.Height)
                    : parentBounds;
                break;
            }
            case SplitViewPanePlacement.Top:
            {
                var height = Math.Min(paneLength, parent.Height);
                paneBounds = new Rect(parent.X, parent.Y, parent.Width, Math.Max(10, height));
                contentBounds = isInline
                    ? new Rect(parent.X, parent.Y + height, parent.Width, Math.Max(10, parent.Height - height))
                    : parentBounds;
                break;
            }
            case SplitViewPanePlacement.Bottom:
            {
                var height = Math.Min(paneLength, parent.Height);
                paneBounds = new Rect(parent.X, parentBounds.Bottom - height, parent.Width, Math.Max(10, height));
                contentBounds = isInline
                    ? new Rect(parent.X, parent.Y, parent.Width, Math.Max(10, parent.Height - height))
                    : parentBounds;
                break;
            }
            default:
            {
                var width = Math.Min(paneLength, parent.Width);
                paneBounds = new Rect(parent.X, parent.Y, Math.Max(10, width), parent.Height);
                contentBounds = isInline
                    ? new Rect(parent.X + width, parent.Y, Math.Max(10, parent.Width - width), parent.Height)
                    : parentBounds;
                break;
            }
        }

        foreach (var child in children)
        {
            SetElementBounds(
                child,
                child.SplitViewSlot == DesignerSplitViewSlot.Pane ? paneBounds : contentBounds);
        }
    }

    private static void SetElementBounds(DesignElement element, Rect bounds)
    {
        element.X = bounds.X;
        element.Y = bounds.Y;
        element.Width = Math.Max(10, bounds.Width);
        element.Height = Math.Max(10, bounds.Height);
    }

    private List<DesignElement> GetDirectChildren(DesignElement parent)
        => Elements.Where(element => string.Equals(
                element.ParentName,
                parent.DisplayName,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

    private DesignElement? FindParent(DesignElement child)
        => child.ParentName is null
            ? null
            : Elements.FirstOrDefault(element => string.Equals(
                element.DisplayName,
                child.ParentName,
                StringComparison.OrdinalIgnoreCase));

    private static void SetCanvasChildRelationship(
        DesignElement child,
        DesignElement parent,
        int index,
        double left,
        double top)
    {
        child.GridRow = 0;
        child.GridColumn = 0;
        child.GridRowSpan = 1;
        child.GridColumnSpan = 1;
        child.StackPanelIndex = -1;
        child.StackPanelItemSize = 40;
        child.DockPanelIndex = -1;
        child.DockPanelDock = DesignerDockSide.Left;
        child.DockPanelItemSize = 40;
        child.WrapPanelIndex = -1;
        child.UniformGridIndex = -1;
        child.CanvasChildIndex = index;
        child.CanvasChildLeft = left;
        child.CanvasChildTop = top;
        child.TabIndex = -1;
        child.TabHeader = null;
        child.SplitViewSlot = DesignerSplitViewSlot.Content;
        child.ParentLayout = DesignerParentLayoutKind.Canvas;
        child.ParentName = parent.DisplayName;
    }

    private static void ResetContainerRelationship(DesignElement child)
    {
        child.ParentName = null;
        child.GridRow = 0;
        child.GridColumn = 0;
        child.GridRowSpan = 1;
        child.GridColumnSpan = 1;
        child.StackPanelIndex = -1;
        child.StackPanelItemSize = 40;
        child.ParentLayout = DesignerParentLayoutKind.None;
        child.DockPanelIndex = -1;
        child.DockPanelDock = DesignerDockSide.Left;
        child.DockPanelItemSize = 40;
        child.WrapPanelIndex = -1;
        child.UniformGridIndex = -1;
        child.CanvasChildIndex = -1;
        child.CanvasChildLeft = 0;
        child.CanvasChildTop = 0;
        child.TabIndex = -1;
        child.TabHeader = null;
        child.SplitViewSlot = DesignerSplitViewSlot.Content;
        child.IsVisibleOnArtboard = true;
    }

    private void OnDesignElementPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_isReflowingContainerChildren || sender is not DesignElement element)
        {
            return;
        }

        if (e.PropertyName is nameof(DesignElement.ParentName)
            or nameof(DesignElement.GridRow)
            or nameof(DesignElement.GridColumn)
            or nameof(DesignElement.GridRowSpan)
            or nameof(DesignElement.GridColumnSpan)
            or nameof(DesignElement.StackPanelIndex)
            or nameof(DesignElement.StackPanelItemSize)
            or nameof(DesignElement.DockPanelIndex)
            or nameof(DesignElement.DockPanelDock)
            or nameof(DesignElement.DockPanelItemSize)
            or nameof(DesignElement.WrapPanelIndex)
            or nameof(DesignElement.UniformGridIndex)
            or nameof(DesignElement.CanvasChildIndex)
            or nameof(DesignElement.CanvasChildLeft)
            or nameof(DesignElement.CanvasChildTop)
            or nameof(DesignElement.TabIndex)
            or nameof(DesignElement.TabHeader)
            or nameof(DesignElement.SplitViewSlot)
            or nameof(DesignElement.ParentLayout))
        {
            ReflowContainerChild(element);
            return;
        }

        if (e.PropertyName is nameof(DesignElement.X)
            or nameof(DesignElement.Y)
            or nameof(DesignElement.Width)
            or nameof(DesignElement.Height))
        {
            if (element.IsCanvasChild
                && e.PropertyName is nameof(DesignElement.X) or nameof(DesignElement.Y)
                && FindParent(element) is { } canvasParent)
            {
                _isReflowingContainerChildren = true;
                try
                {
                    element.CanvasChildLeft = element.X - canvasParent.X;
                    element.CanvasChildTop = element.Y - canvasParent.Y;
                }
                finally
                {
                    _isReflowingContainerChildren = false;
                }

                if (IsDesignerContainer(element.Visual))
                {
                    ReflowContainerChildren(element);
                }

                return;
            }

            if (element.IsContainerChild)
            {
                ReflowContainerChild(element);
            }
            else if (IsDesignerContainer(element.Visual))
            {
                ReflowContainerChildren(element);
            }
        }
    }

    private static bool IsDesignerContainer(Control visual)
        => visual is Grid or StackPanel or DockPanel or WrapPanel or UniformGrid or Canvas
            or TabControl or SplitView or Border or ScrollViewer or Expander or UserControl
            || visual.GetType() == typeof(ContentControl);

    private static bool IsContentContainer(Control visual)
        => visual is Border or ScrollViewer or Expander or UserControl
            || visual.GetType() == typeof(ContentControl);

    private int FindFirstAvailableTabIndex(
        DesignElement parent,
        int tabCount,
        DesignElement current)
    {
        var occupied = GetDirectChildren(parent)
            .Where(child => !ReferenceEquals(child, current)
                && child.ParentLayout == DesignerParentLayoutKind.TabControl
                && child.TabIndex >= 0)
            .Select(child => child.TabIndex)
            .ToHashSet();
        for (var index = 0; index < tabCount; index++)
        {
            if (!occupied.Contains(index))
            {
                return index;
            }
        }

        return -1;
    }

    private static IReadOnlyList<string> GetTabHeaders(TabControl tabControl)
        => tabControl.Items
            .OfType<TabItem>()
            .Select((item, index) => item.Header?.ToString() ?? $"Tab {index + 1}")
            .ToList();

    private void RefreshTabChildVisibility()
    {
        if (_isReflowingContainerChildren || !Elements.Any(element => element.IsTabControlChild))
        {
            return;
        }

        ReflowContainerChildren();
    }

    private static void ClearBuiltInContent(Control visual)
    {
        switch (visual)
        {
            case Border border:
                border.Child = null;
                break;
            case ContentControl contentControl when visual.GetType() == typeof(ContentControl)
                || visual is UserControl:
                contentControl.Content = null;
                break;
            case ScrollViewer scrollViewer:
                scrollViewer.Content = null;
                break;
            case Expander expander:
                expander.Content = null;
                break;
        }
    }

    private static void ClearBuiltInSplitViewSlot(SplitView splitView, DesignerSplitViewSlot slot)
    {
        if (slot == DesignerSplitViewSlot.Pane)
        {
            splitView.Pane = null;
        }
        else
        {
            splitView.Content = null;
        }
    }

    private (Control Visual, double Width, double Height) CreateVisualByType(string typeName, string displayName)
    {
        if (_componentCatalog.TryGet(typeName, out var definition))
        {
            return (_renderer.CreateControl(definition), definition.DefaultWidth, definition.DefaultHeight);
        }

        return (
            new TextBlock { Text = $"[Unsupported: {displayName}]" },
            160,
            24);
    }

    private void ApplyVisualProperties(Control visual, IReadOnlyDictionary<string, string>? properties)
    {
        if (properties is null)
        {
            return;
        }

        if (properties.TryGetValue("__bindings", out var bindingsJson)
            && DesignerBindingRuntime.TryDeserialize(bindingsJson, out var bindings))
        {
            DesignerBindingRuntime.ReplaceBindings(visual, bindings);
        }

        if (properties.TryGetValue("Classes", out var classes))
        {
            SetUserStyleClasses(visual, classes);
        }

        DesignerLayoutRuntime.Apply(visual, properties);
        DesignerTypographyRuntime.Apply(visual, properties);
        DesignerTransformRuntime.Apply(visual, properties);
        DesignerAccessibilityRuntime.Apply(visual, properties);
        DesignerInteractionRuntime.Apply(visual, properties);
        DesignerEffectRuntime.Apply(visual, properties);
        DesignerRangeRuntime.Apply(visual, properties);
        DesignerTextInputRuntime.Apply(visual, properties);
        ApplyTemplatedAppearanceProperties(visual, properties);

        if (visual is GridSplitter gridSplitter)
        {
            DesignerGridSplitterRuntime.Apply(gridSplitter, properties);
            return;
        }

        if (visual is AutoCompleteBox autoCompleteBox)
        {
            if (properties.TryGetValue("__items", out var itemsJson))
            {
                RestoreAutoCompleteBoxItems(autoCompleteBox, itemsJson);
            }

            DesignerAutoCompleteBoxRuntime.Apply(autoCompleteBox, properties);
            return;
        }

        if (visual is MaskedTextBox maskedTextBox)
        {
            DesignerMaskedTextBoxRuntime.Apply(maskedTextBox, properties);
            return;
        }

        if (visual is Shape shape)
        {
            ApplyShapeProperties(shape, properties);
            return;
        }

        if (visual is ToggleButton toggleButton)
        {
            DesignerToggleRuntime.Apply(toggleButton, properties);
            return;
        }

        if (visual is Button button)
        {
            DesignerButtonRuntime.Apply(button, properties);
            return;
        }

        if (visual is SelectableTextBlock selectableTextBlock
            && visual.GetType() == typeof(SelectableTextBlock))
        {
            if (properties.TryGetValue("Text", out var text))
            {
                selectableTextBlock.Text = text;
            }

            DesignerSelectableTextBlockRuntime.Apply(selectableTextBlock, properties);
            return;
        }

        if (visual is TextBlock textBlock)
        {
            if (properties.TryGetValue("Text", out var text))
            {
                textBlock.Text = text;
            }

            if (properties.TryGetValue("FontSize", out var fontSize)
                && double.TryParse(fontSize, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedFontSize))
            {
                textBlock.FontSize = Math.Clamp(parsedFontSize, 8, 96);
            }

            if (properties.TryGetValue("FontWeight", out var fontWeight)
                && TryParseTextWeight(fontWeight, out var parsedFontWeight))
            {
                textBlock.FontWeight = parsedFontWeight;
            }

            if (properties.TryGetValue("Foreground", out var foreground))
            {
                TrySetTextForeground(textBlock, foreground);
            }

            if (properties.TryGetValue("Background", out var background))
            {
                TrySetAppearanceBrush(textBlock, "Background", brush => textBlock.Background = brush, background);
            }

            return;
        }

        if (visual is Label label)
        {
            if (properties.TryGetValue("Content", out var content))
            {
                label.Content = content;
            }

            label.Tag = properties.TryGetValue("Target", out var targetName)
                && !string.IsNullOrWhiteSpace(targetName)
                ? targetName
                : null;
            return;
        }

        if (visual is Image image)
        {
            DesignerImageRuntime.Apply(image, properties);
            return;
        }

        if (visual is ComboBox comboBox)
        {
            if (properties.TryGetValue("__items", out var itemsJson))
            {
                RestoreComboBoxItems(comboBox, itemsJson);
            }

            DesignerSelectionRuntime.Apply(comboBox, properties);
            return;
        }

        if (visual is ListBox listBox)
        {
            if (properties.TryGetValue("__items", out var itemsJson))
            {
                RestoreListBoxItems(listBox, itemsJson);
            }

            DesignerSelectionRuntime.Apply(listBox, properties);
            return;
        }

        if (visual is TreeView treeView)
        {
            if (properties.TryGetValue("__treeItems", out var treeItemsJson)
                && DesignerTreeItemRuntime.TryDeserialize(treeItemsJson, out var definitions))
            {
                DesignerTreeItemRuntime.ReplaceItems(treeView, definitions);
            }

            DesignerSelectionRuntime.Apply(treeView, properties);
            return;
        }

        if (visual is Menu menu)
        {
            if (properties.TryGetValue("__menuItems", out var menuItemsJson)
                && DesignerMenuItemRuntime.TryDeserialize(menuItemsJson, out var definitions))
            {
                DesignerMenuItemRuntime.ReplaceItems(menu, definitions);
            }

            return;
        }

        if (visual is DataGrid dataGrid)
        {
            if (properties.TryGetValue("__dataGridColumns", out var columnsJson)
                && DesignerDataGridRuntime.TryDeserialize(columnsJson, out var definitions))
            {
                DesignerDataGridRuntime.ReplaceColumns(dataGrid, definitions);
            }

            DesignerDataGridBehaviorRuntime.Apply(dataGrid, properties);

            return;
        }

        if (visual is DatePicker datePicker)
        {
            DesignerDateTimeRuntime.Apply(datePicker, properties);
            return;
        }

        if (visual is CalendarDatePicker calendarDatePicker)
        {
            DesignerDateTimeRuntime.Apply(calendarDatePicker, properties);
            return;
        }

        if (visual is Avalonia.Controls.Calendar calendar)
        {
            DesignerDateTimeRuntime.Apply(calendar, properties);
            return;
        }

        if (visual is Avalonia.Controls.ColorPicker colorPicker)
        {
            DesignerColorPickerRuntime.Apply(colorPicker, properties);
            return;
        }

        if (visual is TimePicker timePicker)
        {
            DesignerDateTimeRuntime.Apply(timePicker, properties);
            return;
        }

        if (visual is TabControl tabControl)
        {
            DesignerTabControlRuntime.Apply(tabControl, properties);

            if (properties.TryGetValue("__tabs", out var tabsJson))
            {
                RestoreTabControlTabs(tabControl, tabsJson);
            }

            if (properties.TryGetValue("SelectedIndex", out var selectedIndex)
                && int.TryParse(selectedIndex, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSelectedIndex))
            {
                tabControl.SelectedIndex = Math.Clamp(parsedSelectedIndex, -1, tabControl.Items.Count - 1);
            }

            return;
        }

        if (visual is ItemsControl itemsControl && visual.GetType() == typeof(ItemsControl))
        {
            if (properties.TryGetValue("__items", out var itemsJson))
            {
                RestoreItemsControlItems(itemsControl, itemsJson);
            }
            else if (DesignerBindingRuntime.HasBinding(itemsControl, "ItemsSource"))
            {
                itemsControl.Items.Clear();
            }

            return;
        }

        if (visual is SplitView splitView)
        {
            DesignerSplitViewRuntime.Apply(splitView, properties);

            if (properties.TryGetValue("DisplayMode", out var displayMode)
                && Enum.TryParse<SplitViewDisplayMode>(displayMode, true, out var parsedDisplayMode))
            {
                splitView.DisplayMode = parsedDisplayMode;
            }

            if (properties.TryGetValue("IsPaneOpen", out var isPaneOpen)
                && bool.TryParse(isPaneOpen, out var parsedIsPaneOpen))
            {
                splitView.IsPaneOpen = parsedIsPaneOpen;
            }

            if (properties.TryGetValue("OpenPaneLength", out var openPaneLength)
                && double.TryParse(openPaneLength, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedOpenPaneLength))
            {
                splitView.OpenPaneLength = Math.Max(0, parsedOpenPaneLength);
            }

            if (properties.TryGetValue("CompactPaneLength", out var compactPaneLength)
                && double.TryParse(compactPaneLength, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedCompactPaneLength))
            {
                splitView.CompactPaneLength = Math.Max(0, parsedCompactPaneLength);
            }

            if (properties.TryGetValue("PanePlacement", out var panePlacement)
                && Enum.TryParse<SplitViewPanePlacement>(panePlacement, true, out var parsedPanePlacement))
            {
                splitView.PanePlacement = parsedPanePlacement;
            }

            if (properties.TryGetValue("UseLightDismissOverlayMode", out var useLightDismiss)
                && bool.TryParse(useLightDismiss, out var parsedUseLightDismiss))
            {
                splitView.UseLightDismissOverlayMode = parsedUseLightDismiss;
            }

            if (properties.TryGetValue("PaneBackground", out var paneBackground))
            {
                TrySetAppearanceBrush(
                    splitView,
                    "PaneBackground",
                    value => splitView.PaneBackground = value,
                    paneBackground);
            }

            if (properties.TryGetValue("__paneText", out var paneText))
            {
                splitView.Pane = new TextBlock { Text = paneText, Margin = new Thickness(12) };
            }

            if (properties.TryGetValue("__contentText", out var splitContentText))
            {
                splitView.Content = new TextBlock { Text = splitContentText, Margin = new Thickness(16) };
            }

            return;
        }

        if (visual is Expander expander)
        {
            DesignerContainerBehaviorRuntime.Apply(expander, properties);

            if (properties.TryGetValue("__contentText", out var contentText))
            {
                SetExpanderContent(expander, contentText);
            }

            return;
        }

        if (visual is ContentControl contentControl
            && (visual.GetType() == typeof(ContentControl) || visual is UserControl))
        {
            if (properties.TryGetValue("__contentText", out var contentText))
            {
                SetContentControlContent(contentControl, contentText);
            }
            else
            {
                contentControl.Content = null;
            }

            return;
        }

        if (visual is ScrollViewer scrollViewer)
        {
            DesignerContainerBehaviorRuntime.Apply(scrollViewer, properties);
            if (properties.TryGetValue("__contentText", out var contentText))
            {
                SetScrollViewerContent(scrollViewer, contentText);
            }

            return;
        }

        if (visual is Border border)
        {
            if (properties.TryGetValue("Background", out var background))
            {
                TrySetBorderBackground(border, background);
            }

            if (properties.TryGetValue("BorderBrush", out var borderBrush))
            {
                TrySetBorderBrush(border, borderBrush);
            }

            if (properties.TryGetValue("BorderThickness", out var borderThickness)
                && TryParseThickness(borderThickness, out var parsedBorderThickness))
            {
                border.BorderThickness = parsedBorderThickness;
            }

            if (properties.TryGetValue("CornerRadius", out var cornerRadius)
                && TryParseCornerRadius(cornerRadius, out var parsedCornerRadius))
            {
                border.CornerRadius = parsedCornerRadius;
            }

            if (properties.TryGetValue("__contentText", out var contentText))
            {
                SetBorderContent(border, contentText);
            }
            else
            {
                border.Child = null;
            }

            return;
        }

        if (visual is StackPanel stackPanel)
        {
            if (properties.TryGetValue("Orientation", out var orientation)
                && Enum.TryParse<Orientation>(orientation, ignoreCase: true, out var parsedOrientation))
            {
                stackPanel.Orientation = parsedOrientation;
            }

            if (properties.TryGetValue("Spacing", out var spacing)
                && double.TryParse(spacing, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedSpacing))
            {
                stackPanel.Spacing = parsedSpacing;
            }

            if (properties.TryGetValue("__children", out var childrenJson))
            {
                RestoreStackPanelChildren(stackPanel, childrenJson);
            }

            return;
        }

        if (visual is DockPanel dockPanel)
        {
            if (properties.TryGetValue("LastChildFill", out var lastChildFill)
                && bool.TryParse(lastChildFill, out var parsedLastChildFill))
            {
                dockPanel.LastChildFill = parsedLastChildFill;
            }

            return;
        }

        if (visual is WrapPanel wrapPanel)
        {
            if (properties.TryGetValue("Orientation", out var orientation)
                && Enum.TryParse<Orientation>(orientation, ignoreCase: true, out var parsedOrientation))
            {
                wrapPanel.Orientation = parsedOrientation;
            }

            if (properties.TryGetValue("ItemWidth", out var itemWidth)
                && double.TryParse(itemWidth, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedItemWidth))
            {
                wrapPanel.ItemWidth = Math.Max(10, parsedItemWidth);
            }

            if (properties.TryGetValue("ItemHeight", out var itemHeight)
                && double.TryParse(itemHeight, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedItemHeight))
            {
                wrapPanel.ItemHeight = Math.Max(10, parsedItemHeight);
            }

            if (properties.TryGetValue("ItemSpacing", out var itemSpacing)
                && double.TryParse(itemSpacing, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedItemSpacing))
            {
                wrapPanel.ItemSpacing = Math.Max(0, parsedItemSpacing);
            }

            if (properties.TryGetValue("LineSpacing", out var lineSpacing)
                && double.TryParse(lineSpacing, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedLineSpacing))
            {
                wrapPanel.LineSpacing = Math.Max(0, parsedLineSpacing);
            }

            if (properties.TryGetValue("ItemsAlignment", out var itemsAlignment)
                && Enum.TryParse<WrapPanelItemsAlignment>(
                    itemsAlignment,
                    ignoreCase: true,
                    out var parsedItemsAlignment))
            {
                wrapPanel.ItemsAlignment = parsedItemsAlignment;
            }

            return;
        }

        if (visual is UniformGrid uniformGrid)
        {
            if (properties.TryGetValue("Rows", out var rows)
                && int.TryParse(rows, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedRows))
            {
                uniformGrid.Rows = Math.Max(0, parsedRows);
            }

            if (properties.TryGetValue("Columns", out var columns)
                && int.TryParse(columns, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedColumns))
            {
                uniformGrid.Columns = Math.Max(0, parsedColumns);
            }

            if (properties.TryGetValue("FirstColumn", out var firstColumn)
                && int.TryParse(firstColumn, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedFirstColumn))
            {
                uniformGrid.FirstColumn = Math.Max(0, parsedFirstColumn);
            }

            if (properties.TryGetValue("RowSpacing", out var rowSpacing)
                && double.TryParse(rowSpacing, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedRowSpacing))
            {
                uniformGrid.RowSpacing = Math.Max(0, parsedRowSpacing);
            }

            if (properties.TryGetValue("ColumnSpacing", out var columnSpacing)
                && double.TryParse(columnSpacing, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedColumnSpacing))
            {
                uniformGrid.ColumnSpacing = Math.Max(0, parsedColumnSpacing);
            }

            return;
        }

        if (visual is Canvas canvas)
        {
            if (properties.TryGetValue("Background", out var background))
            {
                TrySetAppearanceBrush(canvas, "Background", value => canvas.Background = value, background);
            }

            return;
        }

        if (visual is Grid grid)
        {
            DesignerGridDefinitionRuntime.TryApply(grid, properties, out _);
            if (properties.TryGetValue("ShowGridLines", out var showGrid)
                && bool.TryParse(showGrid, out var parsedShowGrid))
            {
                grid.ShowGridLines = parsedShowGrid;
            }
        }
    }

    private void TrySetTextForeground(TextBlock textBlock, string foreground)
    {
        TrySetAppearanceBrush(textBlock, "Foreground", brush => textBlock.Foreground = brush, foreground);
    }

    private void ApplyTemplatedAppearanceProperties(
        Control visual,
        IReadOnlyDictionary<string, string> properties)
    {
        if (visual is not Avalonia.Controls.Primitives.TemplatedControl templated)
        {
            return;
        }

        if (properties.TryGetValue("Background", out var background))
        {
            TrySetAppearanceBrush(visual, "Background", value => templated.Background = value, background);
        }

        if (properties.TryGetValue("Foreground", out var foreground))
        {
            TrySetAppearanceBrush(visual, "Foreground", value => templated.Foreground = value, foreground);
        }

        if (properties.TryGetValue("BorderBrush", out var borderBrush))
        {
            TrySetAppearanceBrush(visual, "BorderBrush", value => templated.BorderBrush = value, borderBrush);
        }

        if (properties.TryGetValue("BorderThickness", out var borderThickness)
            && TryParseThickness(borderThickness, out var parsedBorderThickness))
        {
            templated.BorderThickness = parsedBorderThickness;
        }

        if (properties.TryGetValue("CornerRadius", out var cornerRadius)
            && TryParseCornerRadius(cornerRadius, out var parsedCornerRadius))
        {
            templated.CornerRadius = parsedCornerRadius;
        }
    }

    private void ApplyShapeProperties(
        Shape shape,
        IReadOnlyDictionary<string, string> properties)
    {
        if (properties.TryGetValue("Fill", out var fill))
        {
            TrySetAppearanceBrush(shape, "Fill", value => shape.Fill = value, fill);
        }

        if (properties.TryGetValue("Stroke", out var stroke))
        {
            TrySetAppearanceBrush(shape, "Stroke", value => shape.Stroke = value, stroke);
        }

        if (TryReadFiniteDouble(properties, "StrokeThickness", out var strokeThickness))
        {
            shape.StrokeThickness = Math.Max(0, strokeThickness);
        }

        if (properties.TryGetValue("Stretch", out var stretch)
            && Enum.TryParse<Stretch>(stretch, true, out var parsedStretch))
        {
            shape.Stretch = parsedStretch;
        }

        if (properties.TryGetValue("StrokeDashArray", out var dashArray))
        {
            shape.StrokeDashArray ??= [];
            shape.StrokeDashArray.Clear();
            foreach (var value in ParseNonNegativeDoubleList(dashArray))
            {
                shape.StrokeDashArray.Add(value);
            }
        }

        if (TryReadFiniteDouble(properties, "StrokeDashOffset", out var dashOffset))
        {
            shape.StrokeDashOffset = dashOffset;
        }

        if (properties.TryGetValue("StrokeLineCap", out var lineCap)
            && Enum.TryParse<PenLineCap>(lineCap, true, out var parsedLineCap))
        {
            shape.StrokeLineCap = parsedLineCap;
        }

        if (properties.TryGetValue("StrokeJoin", out var lineJoin)
            && Enum.TryParse<PenLineJoin>(lineJoin, true, out var parsedLineJoin))
        {
            shape.StrokeJoin = parsedLineJoin;
        }

        if (TryReadFiniteDouble(properties, "StrokeMiterLimit", out var miterLimit))
        {
            shape.StrokeMiterLimit = Math.Max(0, miterLimit);
        }

        if (shape is RectangleShape rectangle)
        {
            if (TryReadFiniteDouble(properties, "RadiusX", out var radiusX))
            {
                rectangle.RadiusX = Math.Max(0, radiusX);
            }

            if (TryReadFiniteDouble(properties, "RadiusY", out var radiusY))
            {
                rectangle.RadiusY = Math.Max(0, radiusY);
            }
        }
        else if (shape is LineShape line)
        {
            if (TryReadPoint(properties, "StartPoint", out var startPoint))
            {
                line.StartPoint = startPoint;
            }

            if (TryReadPoint(properties, "EndPoint", out var endPoint))
            {
                line.EndPoint = endPoint;
            }
        }
        else if (shape is PathShape path
                 && properties.TryGetValue("Data", out var data))
        {
            try
            {
                path.Data = string.IsNullOrWhiteSpace(data)
                    ? null
                    : Geometry.Parse(data);
                path.Tag = string.IsNullOrWhiteSpace(data)
                    ? null
                    : new DesignerPathDataMetadata(data);
            }
            catch (Exception exception) when (
                exception is FormatException or ArgumentException or System.IO.InvalidDataException)
            {
                // Keep the default geometry when imported Path data is malformed.
            }
        }
    }

    private static bool TryReadFiniteDouble(
        IReadOnlyDictionary<string, string> properties,
        string propertyName,
        out double value)
    {
        value = 0;
        return properties.TryGetValue(propertyName, out var rawValue)
            && double.TryParse(
                rawValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value)
            && double.IsFinite(value);
    }

    private static IEnumerable<double> ParseNonNegativeDoubleList(string value)
    {
        foreach (var token in value.Split(
                     [',', ' ', '\t'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (double.TryParse(
                    token,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var parsed)
                && double.IsFinite(parsed)
                && parsed >= 0)
            {
                yield return parsed;
            }
        }
    }

    private static bool TryReadPoint(
        IReadOnlyDictionary<string, string> properties,
        string propertyName,
        out Point point)
    {
        point = default;
        if (!properties.TryGetValue(propertyName, out var value))
        {
            return false;
        }

        try
        {
            point = Point.Parse(value);
            return double.IsFinite(point.X) && double.IsFinite(point.Y);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void RestoreComboBoxItems(ComboBox comboBox, string json)
    {
        List<string>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return;
        }

        if (items is null)
        {
            return;
        }

        comboBox.Items.Clear();
        foreach (var item in items)
        {
            comboBox.Items.Add(item);
        }
    }

    private static void RestoreAutoCompleteBoxItems(AutoCompleteBox autoCompleteBox, string json)
    {
        List<string>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return;
        }

        if (items is null)
        {
            return;
        }

        autoCompleteBox.ItemsSource = items;
    }

    private static void RestoreListBoxItems(ListBox listBox, string json)
    {
        List<string>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return;
        }

        if (items is null)
        {
            return;
        }

        listBox.Items.Clear();
        foreach (var item in items)
        {
            listBox.Items.Add(item);
        }
    }

    private static void RestoreItemsControlItems(ItemsControl itemsControl, string json)
    {
        List<string>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return;
        }

        if (items is null)
        {
            return;
        }

        itemsControl.Items.Clear();
        foreach (var item in items)
        {
            itemsControl.Items.Add(item);
        }
    }

    private static void RestoreTabControlTabs(TabControl tabControl, string json)
    {
        List<string>? tabs;
        try
        {
            tabs = JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return;
        }

        if (tabs is null)
        {
            return;
        }

        tabControl.Items.Clear();
        foreach (var tab in tabs)
        {
            tabControl.Items.Add(CreateTabItem(tab));
        }
    }

    private static TabItem CreateTabItem(string header)
        => new()
        {
            Header = header,
            Content = new TextBlock { Text = $"{header} content", Margin = new Thickness(12) },
        };

    private static void SetExpanderContent(Expander expander, string contentText)
        => expander.Content = new TextBlock { Text = contentText, Margin = new Thickness(8) };

    private static void SetScrollViewerContent(ScrollViewer scrollViewer, string contentText)
        => scrollViewer.Content = new TextBlock { Text = contentText, Margin = new Thickness(8) };

    private static void SetContentControlContent(ContentControl contentControl, string contentText)
        => contentControl.Content = new TextBlock { Text = contentText, Margin = new Thickness(8) };

    private static void SetBorderContent(Border border, string contentText)
        => border.Child = new TextBlock
        {
            Text = contentText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

    private void TrySetBorderBackground(Border border, string value)
    {
        TrySetAppearanceBrush(border, "Background", brush => border.Background = brush, value);
    }

    private void TrySetBorderBrush(Border border, string value)
    {
        TrySetAppearanceBrush(border, "BorderBrush", brush => border.BorderBrush = brush, value);
    }

    private void TrySetAppearanceBrush(
        Control visual,
        string propertyName,
        Action<IBrush?> applyBrush,
        string value)
    {
        if (DesignerResourceReferenceMetadata.TryParseExpression(value, out var resourceKey))
        {
            DesignerResourceReferenceMetadata.SetReference(visual, propertyName, resourceKey);
            if (_colorResources.TryGetValue(resourceKey, out var resourceValue))
            {
                TrySetBrush(applyBrush, resourceValue);
            }

            return;
        }

        DesignerResourceReferenceMetadata.SetReference(visual, propertyName, null);
        TrySetBrush(applyBrush, value);
    }

    private static void TrySetBrush(Action<IBrush?> applyBrush, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            applyBrush(null);
            return;
        }

        try
        {
            applyBrush(Brush.Parse(value));
        }
        catch (FormatException)
        {
            // Ignore malformed imported brushes while keeping the control usable.
        }
    }

    private static void SetUserStyleClasses(Control visual, string classes)
    {
        foreach (var className in GetUserStyleClasses(visual))
        {
            visual.Classes.Remove(className);
        }

        foreach (var className in classes.Split(
                     [' ', '\t', '\r', '\n'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .Distinct(StringComparer.Ordinal))
        {
            visual.Classes.Add(className);
        }
    }

    private static bool TryParseThickness(string value, out Thickness thickness)
    {
        try
        {
            thickness = Thickness.Parse(value);
            return true;
        }
        catch (FormatException)
        {
            thickness = default;
            return false;
        }
    }

    private static bool TryParseCornerRadius(string value, out CornerRadius cornerRadius)
    {
        try
        {
            cornerRadius = CornerRadius.Parse(value);
            return true;
        }
        catch (FormatException)
        {
            cornerRadius = default;
            return false;
        }
    }

    private static bool TryParseTextWeight(string value, out FontWeight fontWeight)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "normal":
            case "regular":
            case "400":
                fontWeight = FontWeight.Normal;
                return true;
            case "semibold":
            case "semi-bold":
            case "600":
                fontWeight = FontWeight.SemiBold;
                return true;
            case "bold":
            case "700":
                fontWeight = FontWeight.Bold;
                return true;
            default:
                fontWeight = FontWeight.Normal;
                return false;
        }
    }

    private static void RestoreStackPanelChildren(StackPanel stackPanel, string json)
    {
        stackPanel.Children.Clear();

        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        List<StackPanelChildSnapshot>? children;
        try
        {
            children = JsonSerializer.Deserialize<List<StackPanelChildSnapshot>>(json);
        }
        catch
        {
            return;
        }

        if (children is null)
        {
            return;
        }

        foreach (var child in children)
        {
            Control? control = child.TypeName switch
            {
                "TextBlock" => new TextBlock
                {
                    Text = child.Text ?? string.Empty,
                },
                "Button" => new Button
                {
                    Content = child.Content ?? string.Empty,
                },
                "TextBox" => new TextBox
                {
                    Text = string.IsNullOrEmpty(child.PasswordChar) ? child.Text ?? string.Empty : string.Empty,
                    Watermark = child.Watermark,
                    PasswordChar = string.IsNullOrEmpty(child.PasswordChar) ? '\0' : child.PasswordChar[0],
                    RevealPassword = child.RevealPassword ?? false,
                    AcceptsReturn = child.AcceptsReturn ?? false,
                    TextWrapping = Enum.TryParse<TextWrapping>(child.TextWrapping, ignoreCase: true, out var textWrapping)
                        ? textWrapping
                        : TextWrapping.NoWrap,
                },
                _ => null,
            };

            if (control is not null)
            {
                stackPanel.Children.Add(control);
            }
        }
    }

    private string BuildUniqueDisplayName(string displayPrefix)
    {
        var index = 1;
        var candidate = $"{displayPrefix}{index}";
        while (Elements.Any(element => string.Equals(element.DisplayName, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            index++;
            candidate = $"{displayPrefix}{index}";
        }

        return candidate;
    }

    private sealed record StackPanelChildSnapshot(
        string TypeName,
        string? Text = null,
        string? Content = null,
        string? Watermark = null,
        string? PasswordChar = null,
        bool? RevealPassword = null,
        bool? AcceptsReturn = null,
        string? TextWrapping = null);
}
