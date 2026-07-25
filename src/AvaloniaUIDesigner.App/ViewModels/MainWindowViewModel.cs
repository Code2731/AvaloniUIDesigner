using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaUIDesigner.App.Designer.Contracts;
using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Designer.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUIDesigner.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private const string DesignerMetadataPrefix = "AvaloniaUIDesigner:";

    private readonly IComponentCatalog _componentCatalog;
    private readonly IDesignerSerializer _serializer;
    private readonly Stack<HistoryEntry> _undoStack = new();
    private readonly Stack<HistoryEntry> _redoStack = new();

    private PendingMutation? _pendingMutation;
    private bool _isSyncingSelection;
    private string? _currentDocumentPath;
    private DesignerCanvasDocument _lastSavedSnapshot = new(Array.Empty<DesignerElementSnapshot>());
    private List<DesignerElementSnapshot>? _clipboardSnapshots;

    public MainWindowViewModel()
        : this(new BuiltInComponentCatalog(), new DefaultControlRenderer(), new AxamlDocumentSerializer())
    {
    }

    internal MainWindowViewModel(
        IComponentCatalog componentCatalog,
        IControlRenderer renderer,
        IDesignerSerializer serializer)
    {
        _componentCatalog = componentCatalog;
        _serializer = serializer;

        Toolbox = new ToolboxViewModel(componentCatalog);
        Canvas = new CanvasViewModel(componentCatalog, renderer);
        ObjectTree = new ObjectTreeViewModel();
        PropertyInspector = new PropertyInspectorViewModel();
        RecentFiles = new ObservableCollection<string>();

        ObjectTree.PropertyChanged += OnObjectTreePropertyChanged;
        LoadRecentFilesFromDisk();
    }

    public ToolboxViewModel Toolbox { get; }
    public CanvasViewModel Canvas { get; }
    public ObjectTreeViewModel ObjectTree { get; }
    public PropertyInspectorViewModel PropertyInspector { get; }
    public ObservableCollection<string> RecentFiles { get; }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public string UndoMenuLabel => _undoStack.TryPeek(out var entry)
        ? $"Undo {DescribeAction(entry.ActionType)}"
        : "Undo";
    public string RedoMenuLabel => _redoStack.TryPeek(out var entry)
        ? $"Redo {DescribeAction(entry.ActionType)}"
        : "Redo";
    public string HistorySummary => $"{UndoMenuLabel} | {RedoMenuLabel}";
    public bool CanPaste => _clipboardSnapshots is { Count: > 0 };
    public string? CurrentDocumentPath => _currentDocumentPath;
    public string WindowTitle => $"Avalonia UI Designer - {GetDisplayDocumentName()}{(IsDirty ? "*" : string.Empty)}";

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private bool _isDirty;

    public void PlaceFromToolbox(double x, double y)
    {
        var item = Toolbox.SelectedItem;
        if (item is null)
        {
            StatusText = "Select a control in Toolbox first.";
            return;
        }

        var snappedX = Canvas.SnapPosition(x);
        var snappedY = Canvas.SnapPosition(y);

        BeginCanvasMutation(HistoryActionType.AddElement, "Added control to canvas.");
        if (item.IsPreset)
        {
            var elements = Canvas.PlacePreset(item, snappedX, snappedY);
            foreach (var element in elements)
            {
                ObjectTree.Add(element);
            }

            ObjectTree.SelectByElement(Canvas.SelectedElement);
            CommitCanvasMutation();
            StatusText = $"Placed {item.DisplayName} ({elements.Count} control(s))";
            return;
        }

        var element = Canvas.PlaceElement(item, snappedX, snappedY);
        ObjectTree.Add(element);
        ObjectTree.SelectByElement(element);
        CommitCanvasMutation();

        StatusText = $"Placed {element.DisplayName} ({snappedX:0}, {snappedY:0})";
    }

    public void SetCanvasGridSize(double gridSize)
    {
        if (Canvas.GridSize == Math.Clamp(gridSize, 4, 32))
        {
            return;
        }

        BeginCanvasMutation(HistoryActionType.TransformElement, "Updated grid size.");
        Canvas.SetGridSize(gridSize);
        CommitCanvasMutation();
        StatusText = $"Grid size: {Canvas.GridSize:0}px";
    }

    public void SetCanvasGridVisibility(bool isVisible)
    {
        if (Canvas.IsGridVisible == isVisible)
        {
            return;
        }

        BeginCanvasMutation(HistoryActionType.TransformElement, "Updated grid visibility.");
        Canvas.IsGridVisible = isVisible;
        CommitCanvasMutation();
        StatusText = isVisible ? "Grid shown." : "Grid hidden.";
    }

    public void SetCanvasSnapToGrid(bool snapToGrid)
    {
        if (Canvas.SnapToGrid == snapToGrid)
        {
            return;
        }

        BeginCanvasMutation(HistoryActionType.TransformElement, "Updated snap to grid.");
        Canvas.SnapToGrid = snapToGrid;
        CommitCanvasMutation();
        StatusText = snapToGrid ? "Snap to grid enabled." : "Snap to grid disabled.";
    }

    public void SelectElement(DesignElement? element, bool toggle = false)
    {
        _isSyncingSelection = true;
        try
        {
            Canvas.Select(element, toggle);
            ObjectTree.SelectByElement(Canvas.SelectedElement);
        }
        finally
        {
            _isSyncingSelection = false;
        }

        StatusText = Canvas.SelectedElement is null
            ? "Ready"
            : $"Selected {Canvas.SelectedElements.Count} control(s)";
    }

    public void SelectElements(IEnumerable<DesignElement> elements, bool append = false)
    {
        var selection = append
            ? Canvas.SelectedElements.Concat(elements).Distinct().ToList()
            : elements.Distinct().ToList();

        _isSyncingSelection = true;
        try
        {
            Canvas.SelectMany(selection);
            ObjectTree.SelectByElement(Canvas.SelectedElement);
        }
        finally
        {
            _isSyncingSelection = false;
        }

        StatusText = selection.Count == 0 ? "Ready" : $"Selected {selection.Count} control(s)";
    }

    public void SetSelectedOpacity(double opacity)
    {
        var targets = Canvas.SelectedElements.Where(element => !element.IsLocked).ToList();
        if (targets.Count == 0)
        {
            StatusText = "Select an unlocked control to change opacity.";
            return;
        }

        var normalizedOpacity = Math.Clamp(opacity, 0, 1);
        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated control opacity.");
        foreach (var element in targets)
        {
            element.Visual.Opacity = normalizedOpacity;
        }

        CommitCanvasMutation();
        StatusText = $"Set opacity to {normalizedOpacity * 100:0}% for {targets.Count} control(s)";
    }

    public void SetSelectedTextSize(double fontSize)
    {
        var targets = Canvas.SelectedElements.Where(element => !element.IsLocked && element.Visual is TextBlock).ToList();
        if (targets.Count == 0)
        {
            StatusText = "Select an unlocked TextBlock to change its size.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated text size.");
        foreach (var target in targets)
        {
            ((TextBlock)target.Visual).FontSize = Math.Clamp(fontSize, 8, 96);
        }

        CommitCanvasMutation();
        StatusText = $"Set text size to {fontSize:0}px for {targets.Count} control(s)";
    }

    public void SetSelectedTextColor(string color)
    {
        var targets = Canvas.SelectedElements.Where(element => !element.IsLocked && element.Visual is TextBlock).ToList();
        if (targets.Count == 0)
        {
            StatusText = "Select an unlocked TextBlock to change its color.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated text color.");
        var brush = new SolidColorBrush(Color.Parse(color));
        foreach (var target in targets)
        {
            ((TextBlock)target.Visual).Foreground = brush;
        }

        CommitCanvasMutation();
        StatusText = $"Set text color for {targets.Count} control(s)";
    }

    public void SetSelectedTextWeight(string weightName)
    {
        var targets = Canvas.SelectedElements.Where(element => !element.IsLocked && element.Visual is TextBlock).ToList();
        if (targets.Count == 0)
        {
            StatusText = "Select an unlocked TextBlock to change its weight.";
            return;
        }

        if (!TryParseTextWeight(weightName, out var fontWeight))
        {
            StatusText = "Unsupported text weight.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated text weight.");
        foreach (var target in targets)
        {
            ((TextBlock)target.Visual).FontWeight = fontWeight;
        }

        CommitCanvasMutation();
        StatusText = $"Set text weight to {weightName} for {targets.Count} control(s)";
    }

    public bool TryGetSelectedItems(out string controlName, out IReadOnlyList<string> items)
    {
        var target = Canvas.SelectedElement;
        if (target is null)
        {
            controlName = string.Empty;
            items = Array.Empty<string>();
            StatusText = "Select a ComboBox or ListBox to edit its items.";
            return false;
        }

        if (target.IsLocked)
        {
            controlName = string.Empty;
            items = Array.Empty<string>();
            StatusText = "Unlock the selected control before editing its items.";
            return false;
        }

        switch (target.Visual)
        {
            case ComboBox comboBox:
                controlName = target.DisplayName;
                items = ReadItems(comboBox);
                return true;
            case ListBox listBox:
                controlName = target.DisplayName;
                items = ReadItems(listBox);
                return true;
            case TabControl tabControl:
                controlName = target.DisplayName;
                items = ReadTabHeaders(tabControl);
                return true;
            default:
                controlName = string.Empty;
                items = Array.Empty<string>();
                StatusText = "Item editing is available for ComboBox, ListBox, and TabControl controls.";
                return false;
        }
    }

    public void SetSelectedItems(IEnumerable<string> items)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            StatusText = "Select an unlocked ComboBox, ListBox, or TabControl to edit its items.";
            return;
        }

        var updatedItems = items
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .ToList();

        switch (target.Visual)
        {
            case ComboBox comboBox:
                if (ReadItems(comboBox).SequenceEqual(updatedItems, StringComparer.Ordinal))
                {
                    StatusText = "ComboBox items are unchanged.";
                    return;
                }

                BeginCanvasMutation(HistoryActionType.EditProperty, "Updated ComboBox items.");
                var comboBoxSelectedIndex = comboBox.SelectedIndex;
                ReplaceItems(comboBox, updatedItems);
                comboBox.SelectedIndex = Math.Clamp(comboBoxSelectedIndex, -1, updatedItems.Count - 1);
                CommitCanvasMutation();
                StatusText = $"Updated {updatedItems.Count} ComboBox item(s).";
                return;

            case ListBox listBox:
                if (ReadItems(listBox).SequenceEqual(updatedItems, StringComparer.Ordinal))
                {
                    StatusText = "ListBox items are unchanged.";
                    return;
                }

                BeginCanvasMutation(HistoryActionType.EditProperty, "Updated ListBox items.");
                var listBoxSelectedIndex = listBox.SelectedIndex;
                ReplaceItems(listBox, updatedItems);
                listBox.SelectedIndex = Math.Clamp(listBoxSelectedIndex, -1, updatedItems.Count - 1);
                CommitCanvasMutation();
                StatusText = $"Updated {updatedItems.Count} ListBox item(s).";
                return;

            case TabControl tabControl:
                if (ReadTabHeaders(tabControl).SequenceEqual(updatedItems, StringComparer.Ordinal))
                {
                    StatusText = "TabControl tabs are unchanged.";
                    return;
                }

                BeginCanvasMutation(HistoryActionType.EditProperty, "Updated TabControl tabs.");
                var tabControlSelectedIndex = tabControl.SelectedIndex;
                ReplaceTabHeaders(tabControl, updatedItems);
                tabControl.SelectedIndex = Math.Clamp(tabControlSelectedIndex, -1, updatedItems.Count - 1);
                CommitCanvasMutation();
                StatusText = $"Updated {updatedItems.Count} TabControl tab(s).";
                return;

            default:
                StatusText = "Item editing is available for ComboBox, ListBox, and TabControl controls.";
                return;
        }
    }

    public bool TrySetSelectedImageSource(string source)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || target.Visual is not Image)
        {
            StatusText = "Select an unlocked Image control before choosing an image file.";
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated image source.");
        if (!Canvas.TrySetSelectedImageSource(source))
        {
            _pendingMutation = null;
            StatusText = "The selected file could not be loaded as an image.";
            return false;
        }

        CommitCanvasMutation();
        StatusText = $"Set image source for {target.DisplayName}.";
        return true;
    }

    public bool TryGetSelectedExpanderContent(out string controlName, out string content)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || target.Visual is not Expander expander)
        {
            controlName = string.Empty;
            content = string.Empty;
            StatusText = "Select an unlocked Expander control to edit its content.";
            return false;
        }

        controlName = target.DisplayName;
        content = ReadExpanderContent(expander);
        return true;
    }

    public void SetSelectedExpanderContent(string content)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || target.Visual is not Expander expander)
        {
            StatusText = "Select an unlocked Expander control to edit its content.";
            return;
        }

        if (string.Equals(ReadExpanderContent(expander), content, StringComparison.Ordinal))
        {
            StatusText = "Expander content is unchanged.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated Expander content.");
        SetExpanderContent(expander, content);
        CommitCanvasMutation();
        StatusText = $"Updated content for {target.DisplayName}.";
    }

    public bool TryGetSelectedToolTip(out string controlName, out string toolTip)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            controlName = string.Empty;
            toolTip = string.Empty;
            StatusText = "Select an unlocked control to edit its tooltip.";
            return false;
        }

        controlName = target.DisplayName;
        toolTip = ToolTip.GetTip(target.Visual)?.ToString() ?? string.Empty;
        return true;
    }

    public void SetSelectedToolTip(string toolTip)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            StatusText = "Select an unlocked control to edit its tooltip.";
            return;
        }

        var normalizedToolTip = toolTip.Trim();
        if (string.Equals(ToolTip.GetTip(target.Visual)?.ToString() ?? string.Empty, normalizedToolTip, StringComparison.Ordinal))
        {
            StatusText = "Tooltip is unchanged.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated control tooltip.");
        ToolTip.SetTip(target.Visual, string.IsNullOrEmpty(normalizedToolTip) ? null : normalizedToolTip);
        CommitCanvasMutation();
        StatusText = string.IsNullOrEmpty(normalizedToolTip)
            ? $"Cleared tooltip for {target.DisplayName}."
            : $"Updated tooltip for {target.DisplayName}.";
    }

    public bool TryGetSelectedAutomationName(out string controlName, out string automationName)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            controlName = string.Empty;
            automationName = string.Empty;
            StatusText = "Select an unlocked control to edit its accessible name.";
            return false;
        }

        controlName = target.DisplayName;
        automationName = AutomationProperties.GetName(target.Visual) ?? string.Empty;
        return true;
    }

    public void SetSelectedAutomationName(string automationName)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            StatusText = "Select an unlocked control to edit its accessible name.";
            return;
        }

        var normalizedName = automationName.Trim();
        if (string.Equals(AutomationProperties.GetName(target.Visual) ?? string.Empty, normalizedName, StringComparison.Ordinal))
        {
            StatusText = "Accessible name is unchanged.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated accessible name.");
        AutomationProperties.SetName(target.Visual, normalizedName);
        CommitCanvasMutation();
        StatusText = string.IsNullOrEmpty(normalizedName)
            ? $"Cleared accessible name for {target.DisplayName}."
            : $"Updated accessible name for {target.DisplayName}.";
    }

    public void ToggleSelectedEnabledState()
    {
        var targets = Canvas.SelectedElements.Where(element => !element.IsLocked).ToList();
        if (targets.Count == 0)
        {
            StatusText = "Select at least one unlocked control to change its enabled state.";
            return;
        }

        var enable = targets.All(element => !element.Visual.IsEnabled);
        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated control enabled state.");
        foreach (var target in targets)
        {
            target.Visual.IsEnabled = enable;
        }

        CommitCanvasMutation();
        StatusText = enable
            ? $"Enabled {targets.Count} control(s)."
            : $"Disabled {targets.Count} control(s).";
    }

    public bool TryGetSelectedTabIndex(out string controlName, out int tabIndex)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            controlName = string.Empty;
            tabIndex = 0;
            StatusText = "Select an unlocked control to edit its tab order.";
            return false;
        }

        controlName = target.DisplayName;
        tabIndex = target.Visual.TabIndex;
        return true;
    }

    public void SetSelectedTabIndex(int tabIndex)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            StatusText = "Select an unlocked control to edit its tab order.";
            return;
        }

        if (target.Visual.TabIndex == tabIndex)
        {
            StatusText = "Tab order is unchanged.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated control tab order.");
        target.Visual.TabIndex = tabIndex;
        CommitCanvasMutation();
        StatusText = $"Set tab order to {tabIndex} for {target.DisplayName}.";
    }

    public void ToggleSelectedTabStop()
    {
        var targets = Canvas.SelectedElements.Where(element => !element.IsLocked).ToList();
        if (targets.Count == 0)
        {
            StatusText = "Select at least one unlocked control to change tab navigation.";
            return;
        }

        var includeInTabNavigation = targets.All(element => !element.Visual.IsTabStop);
        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated control tab navigation.");
        foreach (var target in targets)
        {
            target.Visual.IsTabStop = includeInTabNavigation;
        }

        CommitCanvasMutation();
        StatusText = includeInTabNavigation
            ? $"Included {targets.Count} control(s) in tab navigation."
            : $"Excluded {targets.Count} control(s) from tab navigation.";
    }

    public bool TryRenameElement(DesignElement element, string proposedName)
    {
        if (element.IsLocked)
        {
            StatusText = "Selected control is locked.";
            return false;
        }

        var name = proposedName.Trim();
        if (!IsValidControlName(name))
        {
            StatusText = "Names must start with a letter or underscore and contain only letters, numbers, or underscores.";
            return false;
        }

        if (Canvas.Elements.Any(candidate => !ReferenceEquals(candidate, element)
            && string.Equals(candidate.DisplayName, name, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = $"A control named '{name}' already exists.";
            return false;
        }

        if (string.Equals(element.DisplayName, name, StringComparison.Ordinal))
        {
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Renamed control.");
        element.DisplayName = name;
        ObjectTree.RebuildFrom(Canvas.Elements);
        ObjectTree.SelectByElement(element);
        CommitCanvasMutation();
        StatusText = $"Renamed control to {name}.";
        return true;
    }

    public void ToggleSelectedLock()
    {
        var targets = Canvas.SelectedElements.ToList();
        if (targets.Count == 0) return;
        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated control lock state.");
        var locked = targets.Any(element => !element.IsLocked);
        foreach (var element in targets) element.IsLocked = locked;
        CommitCanvasMutation();
        StatusText = locked ? $"Locked {targets.Count} control(s)" : $"Unlocked {targets.Count} control(s)";
    }

    public void MoveSelectedElement(double deltaX, double deltaY)
    {
        var targets = Canvas.SelectedElements.Where(element => !element.IsLocked).ToList();
        if (targets.Count == 0)
        {
            StatusText = "Selected controls are locked.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.TransformElement, "Moved control with keyboard.");
        foreach (var target in targets)
        {
            target.X = Math.Max(0, target.X + deltaX);
            target.Y = Math.Max(0, target.Y + deltaY);
        }

        CommitCanvasMutation();
        StatusText = $"Moved {targets.Count} control(s)";
    }

    public void ArrangeSelectedElements(SelectionLayoutAction action)
    {
        var targets = Canvas.SelectedElements.Where(element => !element.IsLocked).ToList();
        if (targets.Count < 2)
        {
            StatusText = "Select at least two unlocked controls to arrange.";
            return;
        }

        if ((action is SelectionLayoutAction.DistributeHorizontally or SelectionLayoutAction.DistributeVertically)
            && targets.Count < 3)
        {
            StatusText = "Select at least three controls to distribute.";
            return;
        }

        var primary = Canvas.SelectedElement ?? targets[^1];
        BeginCanvasMutation(HistoryActionType.TransformElement, "Arranged selected controls.");

        switch (action)
        {
            case SelectionLayoutAction.AlignLeft:
            {
                var left = targets.Min(element => element.X);
                foreach (var element in targets) element.X = left;
                break;
            }
            case SelectionLayoutAction.AlignCenter:
            {
                var center = (targets.Min(element => element.X) + targets.Max(element => element.X + element.Width)) / 2;
                foreach (var element in targets) element.X = center - element.Width / 2;
                break;
            }
            case SelectionLayoutAction.AlignRight:
            {
                var right = targets.Max(element => element.X + element.Width);
                foreach (var element in targets) element.X = right - element.Width;
                break;
            }
            case SelectionLayoutAction.AlignTop:
            {
                var top = targets.Min(element => element.Y);
                foreach (var element in targets) element.Y = top;
                break;
            }
            case SelectionLayoutAction.AlignMiddle:
            {
                var middle = (targets.Min(element => element.Y) + targets.Max(element => element.Y + element.Height)) / 2;
                foreach (var element in targets) element.Y = middle - element.Height / 2;
                break;
            }
            case SelectionLayoutAction.AlignBottom:
            {
                var bottom = targets.Max(element => element.Y + element.Height);
                foreach (var element in targets) element.Y = bottom - element.Height;
                break;
            }
            case SelectionLayoutAction.DistributeHorizontally:
                DistributeHorizontally(targets);
                break;
            case SelectionLayoutAction.DistributeVertically:
                DistributeVertically(targets);
                break;
            case SelectionLayoutAction.MakeSameWidth:
                foreach (var element in targets) element.Width = primary.Width;
                break;
            case SelectionLayoutAction.MakeSameHeight:
                foreach (var element in targets) element.Height = primary.Height;
                break;
            case SelectionLayoutAction.MakeSameSize:
                foreach (var element in targets)
                {
                    element.Width = primary.Width;
                    element.Height = primary.Height;
                }
                break;
            default:
                _pendingMutation = null;
                return;
        }

        CommitCanvasMutation();
        StatusText = $"{DescribeLayoutAction(action)} {targets.Count} control(s)";
    }

    public void CenterSelectedElementsOnArtboard(bool horizontally, bool vertically)
    {
        var targets = Canvas.SelectedElements.Where(element => !element.IsLocked).ToList();
        if (targets.Count == 0)
        {
            StatusText = "Select an unlocked control to center on the artboard.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.TransformElement, "Centered controls on artboard.");

        if (horizontally)
        {
            var left = targets.Min(element => element.X);
            var right = targets.Max(element => element.X + element.Width);
            var offset = (Canvas.ArtboardWidth - (right - left)) / 2 - left;
            foreach (var element in targets)
            {
                element.X = Math.Max(0, element.X + offset);
            }
        }

        if (vertically)
        {
            var top = targets.Min(element => element.Y);
            var bottom = targets.Max(element => element.Y + element.Height);
            var offset = (Canvas.ArtboardHeight - (bottom - top)) / 2 - top;
            foreach (var element in targets)
            {
                element.Y = Math.Max(0, element.Y + offset);
            }
        }

        CommitCanvasMutation();
        StatusText = horizontally && vertically
            ? $"Centered {targets.Count} control(s) on the artboard."
            : horizontally
                ? $"Horizontally centered {targets.Count} control(s) on the artboard."
                : $"Vertically centered {targets.Count} control(s) on the artboard.";
    }

    public void MoveSelectedElementsInLayerOrder(LayerOrderAction action)
    {
        var targets = Canvas.SelectedElements.Where(element => !element.IsLocked).ToList();
        if (targets.Count == 0)
        {
            StatusText = "No unlocked controls to reorder.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.TransformElement, "Changed control layer order.");
        var changed = action switch
        {
            LayerOrderAction.BringToFront => Canvas.MoveElementsToFront(targets),
            LayerOrderAction.SendToBack => Canvas.MoveElementsToBack(targets),
            LayerOrderAction.BringForward => Canvas.MoveElementsForward(targets),
            LayerOrderAction.SendBackward => Canvas.MoveElementsBackward(targets),
            _ => false,
        };

        if (!changed)
        {
            _pendingMutation = null;
            StatusText = DescribeLayerLimit(action);
            return;
        }

        _isSyncingSelection = true;
        try
        {
            ObjectTree.RebuildFrom(Canvas.Elements);
            ObjectTree.SelectByElement(Canvas.SelectedElement);
        }
        finally
        {
            _isSyncingSelection = false;
        }

        CommitCanvasMutation();
        StatusText = DescribeLayerAction(action, targets.Count);
    }

    public void RemoveSelectedElement()
    {
        var targets = Canvas.SelectedElements.Where(element => !element.IsLocked).ToList();
        if (targets.Count == 0)
        {
            StatusText = "No unlocked controls to delete.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.RemoveElement, "Removed control from canvas.");
        foreach (var target in targets)
        {
            Canvas.RemoveElement(target);
        }

        ObjectTree.RebuildFrom(Canvas.Elements);
        CommitCanvasMutation();
        StatusText = $"Deleted {targets.Count} control(s)";
    }

    public void DuplicateSelectedElement()
    {
        var targets = Canvas.SelectedElements.ToList();
        if (targets.Count == 0)
        {
            StatusText = "No selected element to duplicate.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.DuplicateElement, "Duplicated control.");

        var duplicates = new List<DesignElement>();
        foreach (var target in targets)
        {
            var duplicatedSnapshot = CreateSnapshot(
                target,
                BuildDuplicateDisplayName(target.DisplayName),
                target.X + 16,
                target.Y + 16);
            var duplicated = Canvas.AddElementFromSnapshot(duplicatedSnapshot, select: false);
            duplicates.Add(duplicated);
            ObjectTree.Add(duplicated);
        }

        Canvas.SelectMany(duplicates);
        ObjectTree.SelectByElement(Canvas.SelectedElement);
        CommitCanvasMutation();

        StatusText = $"Duplicated {targets.Count} control(s)";
    }

    public void CopySelectedElement()
    {
        var targets = Canvas.SelectedElements.ToList();
        if (targets.Count == 0)
        {
            StatusText = "No selected element to copy.";
            return;
        }

        _clipboardSnapshots = targets
            .Select(target => CreateSnapshot(target, target.DisplayName, target.X, target.Y))
            .ToList();
        OnPropertyChanged(nameof(CanPaste));
        StatusText = $"Copied {targets.Count} control(s)";
    }

    public void CutSelectedElement()
    {
        var targets = Canvas.SelectedElements.Where(element => !element.IsLocked).ToList();
        if (targets.Count == 0)
        {
            StatusText = "No unlocked controls to cut.";
            return;
        }

        _clipboardSnapshots = targets.Select(element => CreateSnapshot(element, element.DisplayName, element.X, element.Y)).ToList();
        OnPropertyChanged(nameof(CanPaste));
        BeginCanvasMutation(HistoryActionType.RemoveElement, "Cut control.");
        foreach (var target in targets)
        {
            Canvas.RemoveElement(target);
        }

        ObjectTree.RebuildFrom(Canvas.Elements);
        CommitCanvasMutation();
        StatusText = $"Cut {targets.Count} control(s)";
    }

    public void PasteElement()
    {
        if (_clipboardSnapshots is not { Count: > 0 })
        {
            StatusText = "Clipboard is empty.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.PasteElement, "Pasted control.");

        var pastedSnapshots = new List<DesignerElementSnapshot>();
        var pastedElements = new List<DesignElement>();
        foreach (var snapshot in _clipboardSnapshots)
        {
            var pastedSnapshot = snapshot with
            {
                DisplayName = BuildDuplicateDisplayName(snapshot.DisplayName),
                X = snapshot.X + 16,
                Y = snapshot.Y + 16,
                VisualProperties = CloneProperties(snapshot.VisualProperties),
            };
            var pasted = Canvas.AddElementFromSnapshot(pastedSnapshot, select: false);
            pastedSnapshots.Add(pastedSnapshot);
            pastedElements.Add(pasted);
            ObjectTree.Add(pasted);
        }

        Canvas.SelectMany(pastedElements);
        ObjectTree.SelectByElement(Canvas.SelectedElement);
        CommitCanvasMutation();

        // Cascade subsequent pastes so they remain visible instead of stacking.
        _clipboardSnapshots = pastedSnapshots;
        StatusText = $"Pasted {pastedElements.Count} control(s)";
    }

    public void NewDocument()
    {
        ApplyDocument(new DesignerCanvasDocument(Array.Empty<DesignerElementSnapshot>()));
        _currentDocumentPath = null;
        _pendingMutation = null;
        ClearHistory();
        AcceptCurrentAsSaved();
        OnPropertyChanged(nameof(CurrentDocumentPath));
        OnPropertyChanged(nameof(WindowTitle));
        StatusText = "Created a new document.";
    }

    public void CreateDocumentFromTemplate(string templateName)
    {
        var document = templateName switch
        {
            "Login" => CreateLoginTemplate(),
            "Settings" => CreateSettingsTemplate(),
            "Dashboard" => CreateDashboardTemplate(),
            _ => throw new ArgumentOutOfRangeException(nameof(templateName), templateName, "Unknown document template."),
        };

        ApplyDocument(document);
        _currentDocumentPath = null;
        _pendingMutation = null;
        ClearHistory();
        IsDirty = true;
        OnPropertyChanged(nameof(CurrentDocumentPath));
        OnPropertyChanged(nameof(WindowTitle));
        StatusText = $"Created {templateName} template. Save it to choose a file name.";
    }

    public void MarkDocumentLoaded(string path)
    {
        _currentDocumentPath = path;
        RegisterRecentFile(path);
        AcceptCurrentAsSaved();
        OnPropertyChanged(nameof(CurrentDocumentPath));
        OnPropertyChanged(nameof(WindowTitle));
    }

    public void MarkDocumentSaved(string path)
    {
        _currentDocumentPath = path;
        RegisterRecentFile(path);
        AcceptCurrentAsSaved();
        OnPropertyChanged(nameof(CurrentDocumentPath));
        OnPropertyChanged(nameof(WindowTitle));
    }

    public void MarkCurrentStateSaved()
    {
        AcceptCurrentAsSaved();
        OnPropertyChanged(nameof(WindowTitle));
    }

    public void MarkDocumentLoadedWithoutPath()
    {
        _currentDocumentPath = null;
        AcceptCurrentAsSaved();
        OnPropertyChanged(nameof(CurrentDocumentPath));
        OnPropertyChanged(nameof(WindowTitle));
    }

    public string ExportDraftAxaml() => _serializer.Serialize(CaptureDocument());

    public DesignerCanvasDocument CreatePreviewDocument() => CaptureDocument();

    public string ExportFullAxaml()
    {
        var settings = CaptureCanvasSettings();
        var sb = new StringBuilder();
        sb.AppendLine("<Window xmlns=\"https://github.com/avaloniaui\"");
        sb.AppendLine("        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
        sb.AppendLine($"        Width=\"{settings.Width:0.###}\" Height=\"{settings.Height:0.###}\">");
        sb.AppendLine($"  <Canvas Width=\"{settings.Width:0.###}\" Height=\"{settings.Height:0.###}\" Background=\"{EscapeXmlAttribute(settings.Background)}\">");
        sb.AppendLine($"    <!-- {DesignerMetadataPrefix} GridSize={settings.GridSize.ToString(\"0.###\", CultureInfo.InvariantCulture)}; IsGridVisible={settings.IsGridVisible}; SnapToGrid={settings.SnapToGrid} -->");

        foreach (var element in Canvas.Elements)
        {
            if (element.IsLocked)
            {
                sb.AppendLine($"    <!-- {DesignerMetadataPrefix} IsLocked=true -->");
            }

            WriteTopLevelElementAxaml(sb, element, "    ");
        }

        sb.AppendLine("  </Canvas>");
        sb.AppendLine("</Window>");
        return sb.ToString();
    }

    public bool TryImportDraftAxaml(string axaml, out string error, out string warning)
    {
        error = string.Empty;
        warning = string.Empty;

        DesignerCanvasDocument parsed;
        var warnings = new List<string>();
        try
        {
            parsed = ParseDraftDocument(axaml, warnings);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }

        ApplyDocument(parsed);
        _pendingMutation = null;
        ClearHistory();
        AcceptCurrentAsSaved();
        StatusText = "Loaded AXAML document.";
        warning = FormatWarnings(warnings);
        return true;
    }

    public bool TryValidateCurrentAxaml(out string result)
    {
        var warnings = new List<string>();
        try
        {
            var parsed = ParseDraftDocument(ExportFullAxaml(), warnings);
            if (parsed.Elements.Count != Canvas.Elements.Count)
            {
                result = "Validation failed: exported control count does not match the canvas.";
                return false;
            }

            if (!parsed.Elements.Select(element => element.DisplayName)
                .SequenceEqual(Canvas.Elements.Select(element => element.DisplayName), StringComparer.Ordinal))
            {
                result = "Validation failed: exported control names do not match the canvas.";
                return false;
            }

            var settings = parsed.Settings ?? new DesignerCanvasSettings();
            if (settings.GridSize != Canvas.GridSize
                || settings.IsGridVisible != Canvas.IsGridVisible
                || settings.SnapToGrid != Canvas.SnapToGrid)
            {
                result = "Validation failed: exported designer canvas settings do not match.";
                return false;
            }

            if (!parsed.Elements.Select(element => element.IsLocked)
                .SequenceEqual(Canvas.Elements.Select(element => element.IsLocked)))
            {
                result = "Validation failed: exported control lock states do not match.";
                return false;
            }
        }
        catch (Exception ex)
        {
            result = $"Validation failed: {ex.Message}";
            return false;
        }

        var warning = FormatWarnings(warnings);
        result = string.IsNullOrEmpty(warning)
            ? $"AXAML structure is valid ({Canvas.Elements.Count} control(s))."
            : $"AXAML structure is valid. {warning}";
        return true;
    }

    public void BeginCanvasMutation(HistoryActionType actionType, string message)
    {
        _pendingMutation ??= new PendingMutation(CaptureDocument(), actionType, message);
    }

    public void CommitCanvasMutation()
    {
        if (_pendingMutation is null)
        {
            return;
        }

        var pending = _pendingMutation;
        _pendingMutation = null;

        var after = CaptureDocument();
        if (AreSameDocument(pending.Before, after))
        {
            return;
        }

        var entry = new HistoryEntry(pending.Before, after, pending.ActionType, pending.Message);
        _undoStack.Push(entry);
        _redoStack.Clear();
        RaiseHistoryChanged();
        RefreshDirtyState();
    }

    public void Undo()
    {
        if (_undoStack.Count == 0)
        {
            StatusText = "Nothing to undo.";
            return;
        }

        var entry = _undoStack.Pop();
        _redoStack.Push(entry);
        ApplyDocument(entry.Before);
        RaiseHistoryChanged();
        RefreshDirtyState();
        StatusText = $"Undo: {DescribeAction(entry.ActionType)}";
    }

    public void Redo()
    {
        if (_redoStack.Count == 0)
        {
            StatusText = "Nothing to redo.";
            return;
        }

        var entry = _redoStack.Pop();
        _undoStack.Push(entry);
        ApplyDocument(entry.After);
        RaiseHistoryChanged();
        RefreshDirtyState();
        StatusText = $"Redo: {DescribeAction(entry.ActionType)}";
    }

    private void OnObjectTreePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isSyncingSelection)
        {
            return;
        }

        if (e.PropertyName != nameof(ObjectTreeViewModel.SelectedNode))
        {
            return;
        }

        var element = ObjectTree.SelectedNode?.Element;
        Canvas.Select(element);
        StatusText = element is null ? "Ready" : $"Selected {element.DisplayName} from Object Tree";
    }

    private static void DistributeHorizontally(IReadOnlyList<DesignElement> elements)
    {
        if (elements.Count < 3)
        {
            return;
        }

        var ordered = elements.OrderBy(element => element.X).ToList();
        var left = ordered[0].X;
        var right = ordered[^1].X + ordered[^1].Width;
        var gap = (right - left - ordered.Sum(element => element.Width)) / (ordered.Count - 1);
        var x = left;

        foreach (var element in ordered)
        {
            element.X = x;
            x += element.Width + gap;
        }
    }

    private static void DistributeVertically(IReadOnlyList<DesignElement> elements)
    {
        if (elements.Count < 3)
        {
            return;
        }

        var ordered = elements.OrderBy(element => element.Y).ToList();
        var top = ordered[0].Y;
        var bottom = ordered[^1].Y + ordered[^1].Height;
        var gap = (bottom - top - ordered.Sum(element => element.Height)) / (ordered.Count - 1);
        var y = top;

        foreach (var element in ordered)
        {
            element.Y = y;
            y += element.Height + gap;
        }
    }

    private void ApplyDocument(DesignerCanvasDocument document)
    {
        _isSyncingSelection = true;
        try
        {
            var settings = document.Settings ?? new DesignerCanvasSettings();
            Canvas.SetArtboard(settings.Width, settings.Height, settings.Background);
            Canvas.SetGridSize(settings.GridSize);
            Canvas.IsGridVisible = settings.IsGridVisible;
            Canvas.SnapToGrid = settings.SnapToGrid;
            Canvas.Clear();
            foreach (var snapshot in document.Elements)
            {
                Canvas.AddElementFromSnapshot(snapshot, select: false);
            }

            ObjectTree.RebuildFrom(Canvas.Elements);
            Canvas.Select(null);
        }
        finally
        {
            _isSyncingSelection = false;
        }
    }

    private DesignerCanvasDocument CaptureDocument()
    {
        var snapshots = Canvas.Elements
            .Select(e => new DesignerElementSnapshot(
                e.DisplayName,
                e.TypeName,
                e.X,
                e.Y,
                e.Width,
                e.Height,
                CaptureVisualProperties(e.Visual),
                e.IsLocked))
            .ToList();

        return new DesignerCanvasDocument(snapshots, CaptureCanvasSettings());
    }

    private DesignerCanvasSettings CaptureCanvasSettings()
        => new(
            Canvas.ArtboardWidth,
            Canvas.ArtboardHeight,
            Canvas.ArtboardBackground,
            Canvas.GridSize,
            Canvas.IsGridVisible,
            Canvas.SnapToGrid);

    private static DesignerCanvasDocument CreateLoginTemplate() => new(
        [
            new("Title", "Avalonia.Controls.TextBlock", 80, 64, 300, 36, Props("Text", "Welcome back")),
            new("Email", "Avalonia.Controls.TextBox", 80, 126, 300, 32, Props("Watermark", "Email address")),
            new("Password", "Avalonia.Controls.TextBox", 80, 170, 300, 32, Props("Watermark", "Password")),
            new("RememberMe", "Avalonia.Controls.CheckBox", 80, 214, 220, 32, Props("Content", "Remember me")),
            new("SignIn", "Avalonia.Controls.Button", 80, 262, 300, 36, Props("Content", "Sign in")),
        ],
        new DesignerCanvasSettings(520, 380, "#F7F9FC"));

    private static DesignerCanvasDocument CreateSettingsTemplate() => new(
        [
            new("Title", "Avalonia.Controls.TextBlock", 64, 52, 360, 36, Props("Text", "Application settings")),
            new("VolumeLabel", "Avalonia.Controls.TextBlock", 64, 122, 220, 24, Props("Text", "Volume")),
            new("Volume", "Avalonia.Controls.Slider", 64, 150, 300, 32, Props("Minimum", "0", "Maximum", "100", "Value", "65")),
            new("Notifications", "Avalonia.Controls.CheckBox", 64, 208, 300, 32, Props("Content", "Enable notifications")),
            new("SaveSettings", "Avalonia.Controls.Button", 64, 270, 160, 36, Props("Content", "Save changes")),
        ],
        new DesignerCanvasSettings(560, 400, "#FFFFFF"));

    private static DesignerCanvasDocument CreateDashboardTemplate() => new(
        [
            new("Title", "Avalonia.Controls.TextBlock", 48, 40, 520, 36, Props("Text", "Project dashboard")),
            new("Summary", "Avalonia.Controls.TextBlock", 48, 96, 520, 24, Props("Text", "A quick overview of this week's progress")),
            new("ProgressLabel", "Avalonia.Controls.TextBlock", 48, 158, 240, 24, Props("Text", "Completion")),
            new("Progress", "Avalonia.Controls.Slider", 48, 186, 360, 32, Props("Minimum", "0", "Maximum", "100", "Value", "72")),
            new("Refresh", "Avalonia.Controls.Button", 48, 252, 140, 36, Props("Content", "Refresh")),
            new("AutoRefresh", "Avalonia.Controls.CheckBox", 214, 254, 240, 32, Props("Content", "Refresh automatically")),
        ],
        new DesignerCanvasSettings(720, 440, "#F7F9FC"));

    private static IReadOnlyDictionary<string, string> Props(params string[] values)
    {
        var properties = new Dictionary<string, string>();
        for (var index = 0; index < values.Length; index += 2)
        {
            properties[values[index]] = values[index + 1];
        }

        return properties;
    }

    private static DesignerElementSnapshot CreateSnapshot(
        DesignElement element,
        string displayName,
        double x,
        double y)
    {
        return new DesignerElementSnapshot(
            displayName,
            element.TypeName,
            x,
            y,
            element.Width,
            element.Height,
            CloneProperties(CaptureVisualProperties(element.Visual)));
    }

    private static IReadOnlyDictionary<string, string>? CaptureVisualProperties(Control visual)
    {
        var properties = CaptureVisualPropertiesCore(visual);
        var toolTip = ToolTip.GetTip(visual)?.ToString();
        var automationName = AutomationProperties.GetName(visual);
        if (string.IsNullOrWhiteSpace(toolTip) && string.IsNullOrWhiteSpace(automationName) && visual.IsEnabled)
        {
            return properties;
        }

        var result = properties is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(properties, StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(toolTip))
        {
            result["__toolTip"] = toolTip;
        }

        if (!string.IsNullOrWhiteSpace(automationName))
        {
            result["__automationName"] = automationName;
        }

        if (!visual.IsEnabled)
        {
            result["__isEnabled"] = bool.FalseString;
        }

        // Defaults vary by control type, so preserve both keyboard navigation values explicitly.
        result["__tabIndex"] = visual.TabIndex.ToString(CultureInfo.InvariantCulture);
        result["__isTabStop"] = visual.IsTabStop.ToString();

        return result;
    }

    private static IReadOnlyDictionary<string, string>? CaptureVisualPropertiesCore(Control visual)
    {
        if (visual is Button button)
        {
            return new Dictionary<string, string>
            {
                ["Content"] = button.Content?.ToString() ?? string.Empty,
                ["Opacity"] = button.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is TextBox textBox)
        {
            return new Dictionary<string, string>
            {
                ["Text"] = textBox.PasswordChar == '\0' ? textBox.Text ?? string.Empty : string.Empty,
                ["Watermark"] = textBox.Watermark?.ToString() ?? string.Empty,
                ["PasswordChar"] = textBox.PasswordChar == '\0' ? string.Empty : textBox.PasswordChar.ToString(),
                ["RevealPassword"] = textBox.RevealPassword.ToString(),
                ["Opacity"] = textBox.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is TextBlock textBlock)
        {
            var properties = new Dictionary<string, string>
            {
                ["Text"] = textBlock.Text ?? string.Empty,
                ["FontSize"] = textBlock.FontSize.ToString("0.###", CultureInfo.InvariantCulture),
                ["FontWeight"] = textBlock.FontWeight.ToString(),
                ["Opacity"] = textBlock.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };

            if (textBlock.Foreground is SolidColorBrush foreground)
            {
                properties["Foreground"] = foreground.Color.ToString();
            }

            return properties;
        }

        if (visual is Image image)
        {
            return new Dictionary<string, string>
            {
                ["Source"] = image.Tag?.ToString() ?? string.Empty,
                ["Stretch"] = image.Stretch.ToString(),
                ["Opacity"] = image.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is CheckBox checkBox)
        {
            return new Dictionary<string, string>
            {
                ["Content"] = checkBox.Content?.ToString() ?? string.Empty,
                ["IsChecked"] = (checkBox.IsChecked ?? false).ToString(),
                ["Opacity"] = checkBox.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is RadioButton radioButton)
        {
            return new Dictionary<string, string>
            {
                ["Content"] = radioButton.Content?.ToString() ?? string.Empty,
                ["IsChecked"] = (radioButton.IsChecked ?? false).ToString(),
                ["GroupName"] = radioButton.GroupName ?? string.Empty,
                ["Opacity"] = radioButton.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is ToggleSwitch toggleSwitch)
        {
            return new Dictionary<string, string>
            {
                ["Content"] = toggleSwitch.Content?.ToString() ?? string.Empty,
                ["IsChecked"] = (toggleSwitch.IsChecked ?? false).ToString(),
                ["Opacity"] = toggleSwitch.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is Avalonia.Controls.Primitives.ToggleButton toggleButton)
        {
            return new Dictionary<string, string>
            {
                ["Content"] = toggleButton.Content?.ToString() ?? string.Empty,
                ["IsChecked"] = (toggleButton.IsChecked ?? false).ToString(),
                ["Opacity"] = toggleButton.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is ComboBox comboBox)
        {
            return new Dictionary<string, string>
            {
                ["SelectedIndex"] = comboBox.SelectedIndex.ToString(CultureInfo.InvariantCulture),
                ["__items"] = SerializeComboBoxItems(comboBox),
                ["Opacity"] = comboBox.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is ListBox listBox)
        {
            return new Dictionary<string, string>
            {
                ["SelectedIndex"] = listBox.SelectedIndex.ToString(CultureInfo.InvariantCulture),
                ["__items"] = SerializeListBoxItems(listBox),
                ["Opacity"] = listBox.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is Slider slider)
        {
            return new Dictionary<string, string>
            {
                ["Minimum"] = slider.Minimum.ToString("0.###", CultureInfo.InvariantCulture),
                ["Maximum"] = slider.Maximum.ToString("0.###", CultureInfo.InvariantCulture),
                ["Value"] = slider.Value.ToString("0.###", CultureInfo.InvariantCulture),
                ["Opacity"] = slider.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is ProgressBar progressBar)
        {
            return new Dictionary<string, string>
            {
                ["Minimum"] = progressBar.Minimum.ToString("0.###", CultureInfo.InvariantCulture),
                ["Maximum"] = progressBar.Maximum.ToString("0.###", CultureInfo.InvariantCulture),
                ["Value"] = progressBar.Value.ToString("0.###", CultureInfo.InvariantCulture),
                ["Opacity"] = progressBar.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is DatePicker datePicker)
        {
            return new Dictionary<string, string>
            {
                ["SelectedDate"] = datePicker.SelectedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
                ["Opacity"] = datePicker.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is CalendarDatePicker calendarDatePicker)
        {
            return new Dictionary<string, string>
            {
                ["SelectedDate"] = calendarDatePicker.SelectedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
                ["Watermark"] = calendarDatePicker.Watermark ?? string.Empty,
                ["Opacity"] = calendarDatePicker.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is TimePicker timePicker)
        {
            return new Dictionary<string, string>
            {
                ["SelectedTime"] = timePicker.SelectedTime?.ToString("hh\\:mm", CultureInfo.InvariantCulture) ?? string.Empty,
                ["Opacity"] = timePicker.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is NumericUpDown numericUpDown)
        {
            return new Dictionary<string, string>
            {
                ["Minimum"] = numericUpDown.Minimum.ToString(CultureInfo.InvariantCulture),
                ["Maximum"] = numericUpDown.Maximum.ToString(CultureInfo.InvariantCulture),
                ["Increment"] = numericUpDown.Increment.ToString(CultureInfo.InvariantCulture),
                ["Value"] = numericUpDown.Value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                ["Opacity"] = numericUpDown.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is TabControl tabControl)
        {
            return new Dictionary<string, string>
            {
                ["SelectedIndex"] = tabControl.SelectedIndex.ToString(CultureInfo.InvariantCulture),
                ["__tabs"] = SerializeTabHeaders(tabControl),
                ["Opacity"] = tabControl.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is Expander expander)
        {
            return new Dictionary<string, string>
            {
                ["Header"] = expander.Header?.ToString() ?? string.Empty,
                ["IsExpanded"] = expander.IsExpanded.ToString(),
                ["__contentText"] = ReadExpanderContent(expander),
                ["Opacity"] = expander.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is Border border)
        {
            var properties = new Dictionary<string, string>
            {
                ["BorderThickness"] = border.BorderThickness.ToString(),
                ["CornerRadius"] = border.CornerRadius.ToString(),
                ["Opacity"] = border.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };

            if (border.Background is SolidColorBrush background)
            {
                properties["Background"] = background.Color.ToString();
            }

            if (border.BorderBrush is SolidColorBrush borderBrush)
            {
                properties["BorderBrush"] = borderBrush.Color.ToString();
            }

            return properties;
        }

        if (visual is StackPanel stackPanel)
        {
            return new Dictionary<string, string>
            {
                ["Orientation"] = stackPanel.Orientation.ToString(),
                ["Spacing"] = stackPanel.Spacing.ToString("0.###", CultureInfo.InvariantCulture),
                ["__children"] = SerializeStackPanelChildren(stackPanel),
                ["Opacity"] = stackPanel.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is Grid grid)
        {
            return new Dictionary<string, string>
            {
                ["ShowGridLines"] = grid.ShowGridLines.ToString(),
                ["Opacity"] = grid.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        return null;
    }

    private static IReadOnlyList<string> ReadItems(ItemsControl itemsControl)
        => itemsControl.Items
            .Select(item => item is ContentControl contentControl
                ? contentControl.Content?.ToString() ?? string.Empty
                : item?.ToString() ?? string.Empty)
            .ToList();

    private static IReadOnlyList<string> ReadTabHeaders(TabControl tabControl)
        => tabControl.Items
            .Select(item => item is TabItem tabItem
                ? tabItem.Header?.ToString() ?? string.Empty
                : item?.ToString() ?? string.Empty)
            .ToList();

    private static void ReplaceItems(ItemsControl itemsControl, IReadOnlyList<string> items)
    {
        itemsControl.Items.Clear();
        foreach (var item in items)
        {
            itemsControl.Items.Add(item);
        }
    }

    private static void ReplaceTabHeaders(TabControl tabControl, IReadOnlyList<string> headers)
    {
        tabControl.Items.Clear();
        foreach (var header in headers)
        {
            tabControl.Items.Add(CreateTabItem(header));
        }
    }

    private static TabItem CreateTabItem(string header)
        => new()
        {
            Header = header,
            Content = new TextBlock { Text = $"{header} content", Margin = new Thickness(12) },
        };

    private static string ReadExpanderContent(Expander expander)
        => expander.Content is TextBlock textBlock
            ? textBlock.Text ?? string.Empty
            : expander.Content?.ToString() ?? string.Empty;

    private static void SetExpanderContent(Expander expander, string content)
        => expander.Content = new TextBlock { Text = content, Margin = new Thickness(8) };

    private static bool AreSameDocument(DesignerCanvasDocument left, DesignerCanvasDocument right)
    {
        if (left.Settings != right.Settings)
        {
            return false;
        }

        if (left.Elements.Count != right.Elements.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Elements.Count; i++)
        {
            var a = left.Elements[i];
            var b = right.Elements[i];

            if (a.DisplayName != b.DisplayName
                || a.TypeName != b.TypeName
                || a.X != b.X
                || a.Y != b.Y
                || a.Width != b.Width
                || a.Height != b.Height
                || a.IsLocked != b.IsLocked
                || !DictionaryEquals(a.VisualProperties, b.VisualProperties))
            {
                return false;
            }
        }

        return true;
    }

    private DesignerCanvasDocument ParseDraftDocument(string axaml, ICollection<string> warnings)
    {
        var doc = XDocument.Parse(axaml);
        var root = doc.Root ?? throw new InvalidOperationException("AXAML root element is missing.");
        var parseRoot = FindParseRoot(root);

        if (!string.Equals(root.Name.LocalName, "Canvas", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(parseRoot.Name.LocalName, "Canvas", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("No Canvas element was found; imported direct child controls from the root element.");
        }

        var snapshots = new List<DesignerElementSnapshot>();
        var nextIsLocked = false;

        foreach (var node in parseRoot.Nodes())
        {
            if (node is XComment comment)
            {
                var metadata = ReadDesignerMetadata(comment);
                if (metadata.TryGetValue("IsLocked", out var isLocked)
                    && bool.TryParse(isLocked, out var parsedIsLocked))
                {
                    nextIsLocked = parsedIsLocked;
                }

                continue;
            }

            if (node is not XElement child)
            {
                continue;
            }

            var tagName = child.Name.LocalName;
            if (IsIgnoredContainerTag(tagName))
            {
                continue;
            }

            if (!TryResolveTypeName(tagName, out var typeName))
            {
                warnings.Add($"Unsupported control <{tagName}> was imported as a placeholder.");
            }

            var displayName = ReadImportedDisplayName(child, tagName, snapshots.Count + 1, snapshots, warnings);

            var x = ReadDouble(child, "Canvas.Left", 0);
            var y = ReadDouble(child, "Canvas.Top", 0);
            var width = ReadDouble(child, "Width", 120);
            var height = ReadDouble(child, "Height", 40);
            var props = ReadVisualProperties(child, warnings);

            snapshots.Add(new DesignerElementSnapshot(displayName, typeName, x, y, width, height, props, nextIsLocked));
            nextIsLocked = false;
        }

        return new DesignerCanvasDocument(snapshots, ReadCanvasSettings(parseRoot, warnings));
    }

    private static DesignerCanvasSettings ReadCanvasSettings(XElement canvas, ICollection<string> warnings)
    {
        var width = ReadDouble(canvas, "Width", 1280);
        var height = ReadDouble(canvas, "Height", 800);
        var background = canvas.Attribute("Background")?.Value ?? "#FFFFFF";
        if (!background.StartsWith("#", StringComparison.Ordinal))
        {
            warnings.Add("Unsupported canvas background was replaced with white.");
            background = "#FFFFFF";
        }

        var metadata = canvas.Nodes()
            .OfType<XComment>()
            .Select(ReadDesignerMetadata)
            .FirstOrDefault(values => values.ContainsKey("GridSize")
                || values.ContainsKey("IsGridVisible")
                || values.ContainsKey("SnapToGrid"));

        return new DesignerCanvasSettings(
            width,
            height,
            background,
            ReadDesignerDouble(metadata, "GridSize", 8),
            ReadDesignerBoolean(metadata, "IsGridVisible", true),
            ReadDesignerBoolean(metadata, "SnapToGrid", true));
    }

    private static IReadOnlyDictionary<string, string> ReadDesignerMetadata(XComment comment)
    {
        var text = comment.Value.Trim();
        if (!text.StartsWith(DesignerMetadataPrefix, StringComparison.Ordinal))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var segment in text[DesignerMetadataPrefix.Length..].Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = segment.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            metadata[segment[..separator].Trim()] = segment[(separator + 1)..].Trim();
        }

        return metadata;
    }

    private static double ReadDesignerDouble(IReadOnlyDictionary<string, string>? metadata, string name, double fallback)
        => metadata is not null
            && metadata.TryGetValue(name, out var value)
            && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;

    private static bool ReadDesignerBoolean(IReadOnlyDictionary<string, string>? metadata, string name, bool fallback)
        => metadata is not null
            && metadata.TryGetValue(name, out var value)
            && bool.TryParse(value, out var parsed)
                ? parsed
                : fallback;

    private bool TryResolveTypeName(string tagName, out string typeName)
    {
        foreach (var definition in _componentCatalog.GetAll())
        {
            var shortName = definition.AvaloniaTypeName[(definition.AvaloniaTypeName.LastIndexOf('.') + 1)..];
            if (string.Equals(shortName, tagName, StringComparison.OrdinalIgnoreCase))
            {
                typeName = definition.AvaloniaTypeName;
                return true;
            }
        }

        typeName = $"Avalonia.Controls.{tagName}";
        return false;
    }

    private static string BuildImportedDisplayName(string tagName, int sequence)
        => $"{tagName}{sequence}";

    private static string ReadImportedDisplayName(
        XElement element,
        string tagName,
        int sequence,
        IReadOnlyCollection<DesignerElementSnapshot> existing,
        ICollection<string> warnings)
    {
        var importedName = element.Attributes()
            .FirstOrDefault(attribute => string.Equals(attribute.Name.LocalName, "Name", StringComparison.OrdinalIgnoreCase))
            ?.Value
            .Trim();

        if (!string.IsNullOrWhiteSpace(importedName) && IsValidControlName(importedName)
            && !existing.Any(snapshot => string.Equals(snapshot.DisplayName, importedName, StringComparison.OrdinalIgnoreCase)))
        {
            return importedName;
        }

        if (!string.IsNullOrWhiteSpace(importedName))
        {
            warnings.Add($"Ignored invalid or duplicate control name '{importedName}'.");
        }

        return BuildUniqueImportedDisplayName(tagName, sequence, existing);
    }

    private static string BuildUniqueImportedDisplayName(
        string tagName,
        int sequence,
        IReadOnlyCollection<DesignerElementSnapshot> existing)
    {
        var candidate = BuildImportedDisplayName(tagName, sequence);
        var suffix = 2;
        while (existing.Any(snapshot => string.Equals(snapshot.DisplayName, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{tagName}{sequence}_{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static XElement FindParseRoot(XElement root)
    {
        if (string.Equals(root.Name.LocalName, "Canvas", StringComparison.OrdinalIgnoreCase))
        {
            return root;
        }

        var canvas = root
            .Descendants()
            .FirstOrDefault(e => string.Equals(e.Name.LocalName, "Canvas", StringComparison.OrdinalIgnoreCase));
        return canvas ?? root;
    }

    private static bool IsIgnoredContainerTag(string tagName)
        => tagName is "Styles" or "Resources" || tagName.Contains('.', StringComparison.Ordinal);

    private static IReadOnlyDictionary<string, string>? ReadVisualProperties(XElement element, ICollection<string> warnings)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var tagName = element.Name.LocalName;

        foreach (var attr in element.Attributes())
        {
            var name = attr.Name.LocalName;
            if (name == "ToolTip.Tip")
            {
                map["__toolTip"] = attr.Value;
                continue;
            }

            if (name == "AutomationProperties.Name")
            {
                map["__automationName"] = attr.Value;
                continue;
            }

            if (name == "IsEnabled")
            {
                map["__isEnabled"] = attr.Value;
                continue;
            }

            if (name == "TabIndex")
            {
                map["__tabIndex"] = attr.Value;
                continue;
            }

            if (name == "IsTabStop")
            {
                map["__isTabStop"] = attr.Value;
                continue;
            }

            if (attr.IsNamespaceDeclaration || name is "Canvas.Left" or "Canvas.Top" or "Width" or "Height" or "Name")
            {
                continue;
            }

            if (IsSupportedVisualProperty(tagName, name))
            {
                map[name] = attr.Value;
            }
            else
            {
                warnings.Add($"Ignored unsupported property {tagName}.{name}.");
            }
        }

        if (string.Equals(element.Name.LocalName, "StackPanel", StringComparison.OrdinalIgnoreCase))
        {
            map["__children"] = SerializeStackPanelChildren(element);
        }
        else if (string.Equals(element.Name.LocalName, "ComboBox", StringComparison.OrdinalIgnoreCase)
            || string.Equals(element.Name.LocalName, "ListBox", StringComparison.OrdinalIgnoreCase))
        {
            map["__items"] = SerializeItems(element);
        }
        else if (string.Equals(element.Name.LocalName, "TabControl", StringComparison.OrdinalIgnoreCase))
        {
            map["__tabs"] = SerializeTabHeaders(element);
        }
        else if (string.Equals(element.Name.LocalName, "Expander", StringComparison.OrdinalIgnoreCase))
        {
            map["__contentText"] = element.Elements()
                .FirstOrDefault(child => string.Equals(child.Name.LocalName, "TextBlock", StringComparison.OrdinalIgnoreCase))?
                .Attribute("Text")?.Value ?? string.Empty;
        }

        return map.Count == 0 ? null : map;
    }

    private static bool IsSupportedVisualProperty(string tagName, string propertyName)
    {
        if (propertyName == "Opacity")
        {
            return true;
        }

        return tagName switch
        {
            "Button" => propertyName == "Content",
            "TextBox" => propertyName is "Text" or "Watermark" or "PasswordChar" or "RevealPassword",
            "TextBlock" => propertyName is "Text" or "FontSize" or "FontWeight" or "Foreground",
            "Image" => propertyName is "Source" or "Stretch",
            "CheckBox" or "ToggleSwitch" => propertyName is "Content" or "IsChecked",
            "ToggleButton" => propertyName is "Content" or "IsChecked",
            "RadioButton" => propertyName is "Content" or "IsChecked" or "GroupName",
            "ComboBox" or "ListBox" => propertyName == "SelectedIndex",
            "Slider" or "ProgressBar" => propertyName is "Minimum" or "Maximum" or "Value",
            "DatePicker" => propertyName == "SelectedDate",
            "CalendarDatePicker" => propertyName is "SelectedDate" or "Watermark",
            "TimePicker" => propertyName == "SelectedTime",
            "NumericUpDown" => propertyName is "Minimum" or "Maximum" or "Increment" or "Value",
            "TabControl" => propertyName == "SelectedIndex",
            "Expander" => propertyName is "Header" or "IsExpanded",
            "Border" => propertyName is "Background" or "BorderBrush" or "BorderThickness" or "CornerRadius",
            "Grid" => propertyName == "ShowGridLines",
            "StackPanel" => propertyName is "Orientation" or "Spacing",
            _ => false,
        };
    }

    private static string FormatWarnings(IReadOnlyCollection<string> warnings)
    {
        if (warnings.Count == 0)
        {
            return string.Empty;
        }

        var preview = string.Join(" ", warnings.Take(3));
        return warnings.Count > 3
            ? $"Warnings ({warnings.Count}): {preview}"
            : $"Warnings: {preview}";
    }

    private static bool DictionaryEquals(IReadOnlyDictionary<string, string>? left, IReadOnlyDictionary<string, string>? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        foreach (var pair in left)
        {
            if (!right.TryGetValue(pair.Key, out var value) || !string.Equals(value, pair.Value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static double ReadDouble(XElement element, string attributeName, double fallback)
    {
        var raw = element.Attribute(attributeName)?.Value;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    private void RaiseHistoryChanged()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(UndoMenuLabel));
        OnPropertyChanged(nameof(RedoMenuLabel));
        OnPropertyChanged(nameof(HistorySummary));
    }

    private void ClearHistory()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        RaiseHistoryChanged();
    }

    private void AcceptCurrentAsSaved()
    {
        _lastSavedSnapshot = CaptureDocument();
        IsDirty = false;
    }

    private void RefreshDirtyState()
    {
        IsDirty = !AreSameDocument(_lastSavedSnapshot, CaptureDocument());
    }

    private static string DescribeAction(HistoryActionType action)
    {
        return action switch
        {
            HistoryActionType.AddElement => "add element",
            HistoryActionType.DuplicateElement => "duplicate element",
            HistoryActionType.PasteElement => "paste element",
            HistoryActionType.RemoveElement => "remove element",
            HistoryActionType.TransformElement => "move/resize element",
            HistoryActionType.EditProperty => "edit properties",
            HistoryActionType.LoadDocument => "load document",
            HistoryActionType.NewDocument => "new document",
            _ => "change",
        };
    }

    private static string DescribeLayoutAction(SelectionLayoutAction action)
    {
        return action switch
        {
            SelectionLayoutAction.AlignLeft => "Aligned left",
            SelectionLayoutAction.AlignCenter => "Aligned center",
            SelectionLayoutAction.AlignRight => "Aligned right",
            SelectionLayoutAction.AlignTop => "Aligned top",
            SelectionLayoutAction.AlignMiddle => "Aligned middle",
            SelectionLayoutAction.AlignBottom => "Aligned bottom",
            SelectionLayoutAction.DistributeHorizontally => "Distributed horizontally",
            SelectionLayoutAction.DistributeVertically => "Distributed vertically",
            SelectionLayoutAction.MakeSameWidth => "Matched width for",
            SelectionLayoutAction.MakeSameHeight => "Matched height for",
            SelectionLayoutAction.MakeSameSize => "Matched size for",
            _ => "Arranged",
        };
    }

    private static string DescribeLayerAction(LayerOrderAction action, int count)
        => action switch
        {
            LayerOrderAction.BringToFront => $"Brought {count} control(s) to front.",
            LayerOrderAction.SendToBack => $"Sent {count} control(s) to back.",
            LayerOrderAction.BringForward => $"Brought {count} control(s) forward.",
            LayerOrderAction.SendBackward => $"Sent {count} control(s) backward.",
            _ => "Changed control layer order.",
        };

    private static string DescribeLayerLimit(LayerOrderAction action)
        => action switch
        {
            LayerOrderAction.BringToFront => "Selected controls are already at the front.",
            LayerOrderAction.SendToBack => "Selected controls are already at the back.",
            LayerOrderAction.BringForward => "Selected controls cannot move further forward.",
            LayerOrderAction.SendBackward => "Selected controls cannot move further backward.",
            _ => "Selected controls cannot change layer order.",
        };

    private void RegisterRecentFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        for (var i = RecentFiles.Count - 1; i >= 0; i--)
        {
            if (string.Equals(RecentFiles[i], path, StringComparison.OrdinalIgnoreCase))
            {
                RecentFiles.RemoveAt(i);
            }
        }

        RecentFiles.Insert(0, path);
        while (RecentFiles.Count > 8)
        {
            RecentFiles.RemoveAt(RecentFiles.Count - 1);
        }

        SaveRecentFilesToDisk();
    }

    public void RemoveRecentFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        for (var i = RecentFiles.Count - 1; i >= 0; i--)
        {
            if (string.Equals(RecentFiles[i], path, StringComparison.OrdinalIgnoreCase))
            {
                RecentFiles.RemoveAt(i);
            }
        }

        SaveRecentFilesToDisk();
    }

    private void LoadRecentFilesFromDisk()
    {
        try
        {
            var path = GetRecentFilesStorePath();
            if (!File.Exists(path))
            {
                return;
            }

            var json = File.ReadAllText(path);
            var entries = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            foreach (var entry in entries.Where(e => !string.IsNullOrWhiteSpace(e)).Take(8))
            {
                RecentFiles.Add(entry);
            }
        }
        catch
        {
            // ignore persistence failures
        }
    }

    private void SaveRecentFilesToDisk()
    {
        try
        {
            var path = GetRecentFilesStorePath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(RecentFiles.ToList());
            File.WriteAllText(path, json);
        }
        catch
        {
            // ignore persistence failures
        }
    }

    private static string GetRecentFilesStorePath()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(baseDir, "AvaloniaUIDesigner", "recent-files.json");
    }

    private string BuildDuplicateDisplayName(string sourceName)
    {
        var candidate = $"{sourceName}_copy";
        var suffix = 2;

        while (Canvas.Elements.Any(e => string.Equals(e.DisplayName, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{sourceName}_copy{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static bool IsValidControlName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !(char.IsLetter(name[0]) || name[0] == '_'))
        {
            return false;
        }

        return name.All(character => char.IsLetterOrDigit(character) || character == '_');
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

    private static IReadOnlyDictionary<string, string>? CloneProperties(IReadOnlyDictionary<string, string>? source)
    {
        if (source is null)
        {
            return null;
        }

        return source.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private string GetDisplayDocumentName()
    {
        if (string.IsNullOrWhiteSpace(_currentDocumentPath))
        {
            return "Untitled";
        }

        return Path.GetFileName(_currentDocumentPath);
    }

    private static string SerializeStackPanelChildren(StackPanel stackPanel)
    {
        var children = stackPanel.Children
            .Select(child => child switch
            {
                TextBlock textBlock => new StackPanelChildSnapshot(
                    TypeName: "TextBlock",
                    Text: textBlock.Text ?? string.Empty),
                Button button => new StackPanelChildSnapshot(
                    TypeName: "Button",
                    Content: button.Content?.ToString() ?? string.Empty),
                TextBox textBox => new StackPanelChildSnapshot(
                    TypeName: "TextBox",
                    Text: textBox.PasswordChar == '\0' ? textBox.Text ?? string.Empty : string.Empty,
                    Watermark: textBox.Watermark?.ToString(),
                    PasswordChar: textBox.PasswordChar == '\0' ? null : textBox.PasswordChar.ToString(),
                    RevealPassword: textBox.RevealPassword),
                _ => null,
            })
            .Where(snapshot => snapshot is not null)
            .Cast<StackPanelChildSnapshot>()
            .ToList();

        return JsonSerializer.Serialize(children);
    }

    private static string SerializeComboBoxItems(ComboBox comboBox)
    {
        var items = comboBox.Items
            .Select(item => item is ComboBoxItem comboBoxItem
                ? comboBoxItem.Content?.ToString() ?? string.Empty
                : item?.ToString() ?? string.Empty)
            .ToList();

        return JsonSerializer.Serialize(items);
    }

    private static string SerializeStackPanelChildren(XElement stackPanelElement)
    {
        var children = new List<StackPanelChildSnapshot>();
        foreach (var child in stackPanelElement.Elements())
        {
            var tag = child.Name.LocalName;
            switch (tag)
            {
                case "TextBlock":
                    children.Add(new StackPanelChildSnapshot(
                        TypeName: "TextBlock",
                        Text: child.Attribute("Text")?.Value ?? string.Empty));
                    break;
                case "Button":
                    children.Add(new StackPanelChildSnapshot(
                        TypeName: "Button",
                        Content: child.Attribute("Content")?.Value ?? string.Empty));
                    break;
                case "TextBox":
                    var passwordChar = child.Attribute("PasswordChar")?.Value;
                    children.Add(new StackPanelChildSnapshot(
                        TypeName: "TextBox",
                        Text: string.IsNullOrEmpty(passwordChar) ? child.Attribute("Text")?.Value ?? string.Empty : string.Empty,
                        Watermark: child.Attribute("Watermark")?.Value,
                        PasswordChar: passwordChar,
                        RevealPassword: bool.TryParse(child.Attribute("RevealPassword")?.Value, out var revealPassword)
                            ? revealPassword
                            : null));
                    break;
            }
        }

        return JsonSerializer.Serialize(children);
    }

    private static string SerializeListBoxItems(ListBox listBox)
    {
        var items = listBox.Items
            .Select(item => item is ListBoxItem listBoxItem
                ? listBoxItem.Content?.ToString() ?? string.Empty
                : item?.ToString() ?? string.Empty)
            .ToList();

        return JsonSerializer.Serialize(items);
    }

    private static string SerializeTabHeaders(TabControl tabControl)
        => JsonSerializer.Serialize(ReadTabHeaders(tabControl));

    private static string SerializeItems(XElement itemsControlElement)
    {
        var items = itemsControlElement.Elements()
            .Where(element => !element.Name.LocalName.Contains('.', StringComparison.Ordinal))
            .Select(element => element.Attribute("Content")?.Value ?? element.Value)
            .ToList();

        return JsonSerializer.Serialize(items);
    }

    private static string SerializeTabHeaders(XElement tabControlElement)
    {
        var headers = tabControlElement.Elements()
            .Where(element => string.Equals(element.Name.LocalName, "TabItem", StringComparison.OrdinalIgnoreCase))
            .Select(element => element.Attribute("Header")?.Value ?? string.Empty)
            .ToList();

        return JsonSerializer.Serialize(headers);
    }

    private static void WriteTopLevelElementAxaml(StringBuilder sb, DesignElement element, string indent)
    {
        switch (element.Visual)
        {
            case Button button:
                sb.Append(indent);
                sb.Append("<Button");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Content", button.Content?.ToString() ?? string.Empty);
                sb.AppendLine(" />");
                break;

            case TextBox textBox:
                sb.Append(indent);
                sb.Append("<TextBox");
                AppendCanvasLayoutAttributes(sb, element);
                if (textBox.PasswordChar == '\0')
                {
                    AppendAttribute(sb, "Text", textBox.Text ?? string.Empty);
                }

                if (!string.IsNullOrWhiteSpace(textBox.Watermark?.ToString()))
                {
                    AppendAttribute(sb, "Watermark", textBox.Watermark?.ToString() ?? string.Empty);
                }

                if (textBox.PasswordChar != '\0')
                {
                    AppendAttribute(sb, "PasswordChar", textBox.PasswordChar.ToString());
                }

                if (textBox.RevealPassword)
                {
                    AppendAttribute(sb, "RevealPassword", bool.TrueString);
                }

                sb.AppendLine(" />");
                break;

            case TextBlock textBlock:
                sb.Append(indent);
                sb.Append("<TextBlock");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Text", textBlock.Text ?? string.Empty);
                AppendAttribute(sb, "FontSize", textBlock.FontSize.ToString("0.###", CultureInfo.InvariantCulture));
                AppendAttribute(sb, "FontWeight", textBlock.FontWeight.ToString());
                AppendTextForegroundAttribute(sb, textBlock);
                sb.AppendLine(" />");
                break;

            case Image image:
                sb.Append(indent);
                sb.Append("<Image");
                AppendCanvasLayoutAttributes(sb, element);
                if (!string.IsNullOrWhiteSpace(image.Tag?.ToString()))
                {
                    AppendAttribute(sb, "Source", image.Tag?.ToString() ?? string.Empty);
                }

                AppendAttribute(sb, "Stretch", image.Stretch.ToString());
                sb.AppendLine(" />");
                break;

            case CheckBox checkBox:
                sb.Append(indent);
                sb.Append("<CheckBox");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Content", checkBox.Content?.ToString() ?? string.Empty);
                AppendAttribute(sb, "IsChecked", (checkBox.IsChecked ?? false).ToString());
                sb.AppendLine(" />");
                break;

            case RadioButton radioButton:
                sb.Append(indent);
                sb.Append("<RadioButton");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Content", radioButton.Content?.ToString() ?? string.Empty);
                AppendAttribute(sb, "IsChecked", (radioButton.IsChecked ?? false).ToString());
                if (!string.IsNullOrWhiteSpace(radioButton.GroupName))
                {
                    AppendAttribute(sb, "GroupName", radioButton.GroupName);
                }

                sb.AppendLine(" />");
                break;

            case ToggleSwitch toggleSwitch:
                sb.Append(indent);
                sb.Append("<ToggleSwitch");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Content", toggleSwitch.Content?.ToString() ?? string.Empty);
                AppendAttribute(sb, "IsChecked", (toggleSwitch.IsChecked ?? false).ToString());
                sb.AppendLine(" />");
                break;

            case Avalonia.Controls.Primitives.ToggleButton toggleButton:
                sb.Append(indent);
                sb.Append("<ToggleButton");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Content", toggleButton.Content?.ToString() ?? string.Empty);
                AppendAttribute(sb, "IsChecked", (toggleButton.IsChecked ?? false).ToString());
                sb.AppendLine(" />");
                break;

            case ComboBox comboBox:
                sb.Append(indent);
                sb.Append("<ComboBox");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "SelectedIndex", comboBox.SelectedIndex.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine(">");
                foreach (var item in comboBox.Items)
                {
                    sb.Append(indent);
                    sb.Append("  <ComboBoxItem");
                    AppendAttribute(sb, "Content", item is ComboBoxItem comboBoxItem
                        ? comboBoxItem.Content?.ToString() ?? string.Empty
                        : item?.ToString() ?? string.Empty);
                    sb.AppendLine(" />");
                }

                sb.Append(indent);
                sb.AppendLine("</ComboBox>");
                break;

            case ListBox listBox:
                sb.Append(indent);
                sb.Append("<ListBox");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "SelectedIndex", listBox.SelectedIndex.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine(">");
                foreach (var item in listBox.Items)
                {
                    sb.Append(indent);
                    sb.Append("  <ListBoxItem");
                    AppendAttribute(sb, "Content", item is ListBoxItem listBoxItem
                        ? listBoxItem.Content?.ToString() ?? string.Empty
                        : item?.ToString() ?? string.Empty);
                    sb.AppendLine(" />");
                }

                sb.Append(indent);
                sb.AppendLine("</ListBox>");
                break;

            case Slider slider:
                sb.Append(indent);
                sb.Append("<Slider");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Minimum", slider.Minimum.ToString("0.###", CultureInfo.InvariantCulture));
                AppendAttribute(sb, "Maximum", slider.Maximum.ToString("0.###", CultureInfo.InvariantCulture));
                AppendAttribute(sb, "Value", slider.Value.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine(" />");
                break;

            case ProgressBar progressBar:
                sb.Append(indent);
                sb.Append("<ProgressBar");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Minimum", progressBar.Minimum.ToString("0.###", CultureInfo.InvariantCulture));
                AppendAttribute(sb, "Maximum", progressBar.Maximum.ToString("0.###", CultureInfo.InvariantCulture));
                AppendAttribute(sb, "Value", progressBar.Value.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine(" />");
                break;

            case DatePicker datePicker:
                sb.Append(indent);
                sb.Append("<DatePicker");
                AppendCanvasLayoutAttributes(sb, element);
                if (datePicker.SelectedDate is { } selectedDate)
                {
                    AppendAttribute(sb, "SelectedDate", selectedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }

                sb.AppendLine(" />");
                break;

            case CalendarDatePicker calendarDatePicker:
                sb.Append(indent);
                sb.Append("<CalendarDatePicker");
                AppendCanvasLayoutAttributes(sb, element);
                if (calendarDatePicker.SelectedDate is { } selectedDate)
                {
                    AppendAttribute(sb, "SelectedDate", selectedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }

                if (!string.IsNullOrWhiteSpace(calendarDatePicker.Watermark))
                {
                    AppendAttribute(sb, "Watermark", calendarDatePicker.Watermark);
                }

                sb.AppendLine(" />");
                break;

            case TimePicker timePicker:
                sb.Append(indent);
                sb.Append("<TimePicker");
                AppendCanvasLayoutAttributes(sb, element);
                if (timePicker.SelectedTime is { } selectedTime)
                {
                    AppendAttribute(sb, "SelectedTime", selectedTime.ToString("hh\\:mm", CultureInfo.InvariantCulture));
                }

                sb.AppendLine(" />");
                break;

            case NumericUpDown numericUpDown:
                sb.Append(indent);
                sb.Append("<NumericUpDown");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Minimum", numericUpDown.Minimum.ToString(CultureInfo.InvariantCulture));
                AppendAttribute(sb, "Maximum", numericUpDown.Maximum.ToString(CultureInfo.InvariantCulture));
                AppendAttribute(sb, "Increment", numericUpDown.Increment.ToString(CultureInfo.InvariantCulture));
                if (numericUpDown.Value is { } value)
                {
                    AppendAttribute(sb, "Value", value.ToString(CultureInfo.InvariantCulture));
                }

                sb.AppendLine(" />");
                break;

            case TabControl tabControl:
                sb.Append(indent);
                sb.Append("<TabControl");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "SelectedIndex", tabControl.SelectedIndex.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine(">");
                foreach (var header in ReadTabHeaders(tabControl))
                {
                    sb.Append(indent);
                    sb.Append("  <TabItem");
                    AppendAttribute(sb, "Header", header);
                    sb.AppendLine(">");
                    sb.Append(indent);
                    sb.Append("    <TextBlock");
                    AppendAttribute(sb, "Text", $"{header} content");
                    sb.AppendLine(" />");
                    sb.Append(indent);
                    sb.AppendLine("  </TabItem>");
                }

                sb.Append(indent);
                sb.AppendLine("</TabControl>");
                break;

            case Expander expander:
                sb.Append(indent);
                sb.Append("<Expander");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Header", expander.Header?.ToString() ?? string.Empty);
                AppendAttribute(sb, "IsExpanded", expander.IsExpanded.ToString());
                sb.AppendLine(">");
                sb.Append(indent);
                sb.Append("  <TextBlock");
                AppendAttribute(sb, "Text", ReadExpanderContent(expander));
                sb.AppendLine(" />");
                sb.Append(indent);
                sb.AppendLine("</Expander>");
                break;

            case Border border:
                sb.Append(indent);
                sb.Append("<Border");
                AppendCanvasLayoutAttributes(sb, element);
                if (border.Background is SolidColorBrush background)
                {
                    AppendAttribute(sb, "Background", background.Color.ToString());
                }

                if (border.BorderBrush is SolidColorBrush borderBrush)
                {
                    AppendAttribute(sb, "BorderBrush", borderBrush.Color.ToString());
                }

                AppendAttribute(sb, "BorderThickness", border.BorderThickness.ToString());
                AppendAttribute(sb, "CornerRadius", border.CornerRadius.ToString());
                sb.AppendLine(" />");
                break;

            case Grid grid:
                sb.Append(indent);
                sb.Append("<Grid");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "ShowGridLines", grid.ShowGridLines.ToString());
                sb.AppendLine(" />");
                break;

            case StackPanel stackPanel:
                sb.Append(indent);
                sb.Append("<StackPanel");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Orientation", stackPanel.Orientation.ToString());
                AppendAttribute(sb, "Spacing", stackPanel.Spacing.ToString("0.###", CultureInfo.InvariantCulture));

                if (stackPanel.Children.Count == 0)
                {
                    sb.AppendLine(" />");
                    break;
                }

                sb.AppendLine(">");
                foreach (var child in stackPanel.Children)
                {
                    WriteChildControlAxaml(sb, child, indent + "  ");
                }

                sb.Append(indent);
                sb.AppendLine("</StackPanel>");
                break;

            default:
                sb.Append(indent);
                sb.Append("<TextBlock");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Text", element.DisplayName);
                sb.AppendLine(" />");
                break;
        }
    }

    private static void WriteChildControlAxaml(StringBuilder sb, Control child, string indent)
    {
        switch (child)
        {
            case TextBlock textBlock:
                sb.Append(indent);
                sb.Append("<TextBlock");
                AppendAttribute(sb, "Text", textBlock.Text ?? string.Empty);
                sb.AppendLine(" />");
                break;

            case Button button:
                sb.Append(indent);
                sb.Append("<Button");
                AppendAttribute(sb, "Content", button.Content?.ToString() ?? string.Empty);
                sb.AppendLine(" />");
                break;

            case TextBox textBox:
                sb.Append(indent);
                sb.Append("<TextBox");
                if (textBox.PasswordChar == '\0')
                {
                    AppendAttribute(sb, "Text", textBox.Text ?? string.Empty);
                }

                if (!string.IsNullOrWhiteSpace(textBox.Watermark?.ToString()))
                {
                    AppendAttribute(sb, "Watermark", textBox.Watermark?.ToString() ?? string.Empty);
                }

                if (textBox.PasswordChar != '\0')
                {
                    AppendAttribute(sb, "PasswordChar", textBox.PasswordChar.ToString());
                }

                if (textBox.RevealPassword)
                {
                    AppendAttribute(sb, "RevealPassword", bool.TrueString);
                }

                sb.AppendLine(" />");
                break;

            default:
                sb.Append(indent);
                sb.Append("<Border />");
                sb.AppendLine();
                break;
        }
    }

    private static void AppendCanvasLayoutAttributes(StringBuilder sb, DesignElement element)
    {
        AppendAttribute(sb, "x:Name", element.DisplayName);
        AppendAttribute(sb, "Canvas.Left", element.X.ToString("0.###", CultureInfo.InvariantCulture));
        AppendAttribute(sb, "Canvas.Top", element.Y.ToString("0.###", CultureInfo.InvariantCulture));
        AppendAttribute(sb, "Width", element.Width.ToString("0.###", CultureInfo.InvariantCulture));
        AppendAttribute(sb, "Height", element.Height.ToString("0.###", CultureInfo.InvariantCulture));
        AppendAttribute(sb, "Opacity", element.Visual.Opacity.ToString("0.###", CultureInfo.InvariantCulture));
        if (ToolTip.GetTip(element.Visual) is { } toolTip && !string.IsNullOrWhiteSpace(toolTip.ToString()))
        {
            AppendAttribute(sb, "ToolTip.Tip", toolTip.ToString() ?? string.Empty);
        }

        var automationName = AutomationProperties.GetName(element.Visual);
        if (!string.IsNullOrWhiteSpace(automationName))
        {
            AppendAttribute(sb, "AutomationProperties.Name", automationName);
        }

        if (!element.Visual.IsEnabled)
        {
            AppendAttribute(sb, "IsEnabled", bool.FalseString);
        }

        AppendAttribute(sb, "TabIndex", element.Visual.TabIndex.ToString(CultureInfo.InvariantCulture));
        AppendAttribute(sb, "IsTabStop", element.Visual.IsTabStop.ToString());
    }

    private static void AppendTextForegroundAttribute(StringBuilder sb, TextBlock textBlock)
    {
        if (textBlock.Foreground is SolidColorBrush foreground)
        {
            AppendAttribute(sb, "Foreground", foreground.Color.ToString());
        }
    }

    private static void AppendAttribute(StringBuilder sb, string name, string value)
    {
        sb.Append(" ");
        sb.Append(name);
        sb.Append("=\"");
        sb.Append(EscapeXmlAttribute(value));
        sb.Append("\"");
    }

    private static string EscapeXmlAttribute(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    public enum HistoryActionType
    {
        AddElement,
        DuplicateElement,
        PasteElement,
        RemoveElement,
        TransformElement,
        EditProperty,
        LoadDocument,
        NewDocument,
    }

    public enum SelectionLayoutAction
    {
        AlignLeft,
        AlignCenter,
        AlignRight,
        AlignTop,
        AlignMiddle,
        AlignBottom,
        DistributeHorizontally,
        DistributeVertically,
        MakeSameWidth,
        MakeSameHeight,
        MakeSameSize,
    }

    public enum LayerOrderAction
    {
        BringToFront,
        SendToBack,
        BringForward,
        SendBackward,
    }

    private sealed record PendingMutation(DesignerCanvasDocument Before, HistoryActionType ActionType, string Message);

    private sealed record HistoryEntry(
        DesignerCanvasDocument Before,
        DesignerCanvasDocument After,
        HistoryActionType ActionType,
        string Message);

    private sealed record StackPanelChildSnapshot(
        string TypeName,
        string? Text = null,
        string? Content = null,
        string? Watermark = null,
        string? PasswordChar = null,
        bool? RevealPassword = null);
}
