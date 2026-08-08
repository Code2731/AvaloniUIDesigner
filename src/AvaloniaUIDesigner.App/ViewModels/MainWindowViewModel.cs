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
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaUIDesigner.App.Designer.Contracts;
using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Designer.Services;
using AvaloniaUIDesigner.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using EllipseShape = Avalonia.Controls.Shapes.Ellipse;
using LineShape = Avalonia.Controls.Shapes.Line;
using PathShape = Avalonia.Controls.Shapes.Path;
using RectangleShape = Avalonia.Controls.Shapes.Rectangle;
using Shape = Avalonia.Controls.Shapes.Shape;

namespace AvaloniaUIDesigner.App.ViewModels;

public sealed record StylePreviewOption(string DisplayName, string? PseudoClass);

public enum ItemsEditorMode
{
    Flat,
    TreeView,
    Menu,
    DataGrid,
}

public sealed record ItemsEditorState(
    string ControlName,
    IReadOnlyList<string> Items,
    ItemsEditorMode Mode);

public sealed record DataGridBehaviorEditorState(
    string ControlName,
    bool AutoGenerateColumns,
    bool IsReadOnly,
    bool CanUserReorderColumns,
    bool CanUserResizeColumns,
    bool CanUserSortColumns,
    string HeadersVisibility,
    string GridLinesVisibility,
    string SelectionMode,
    string ClipboardCopyMode,
    bool AreRowDetailsFrozen,
    bool AreRowGroupHeadersFrozen,
    bool IsScrollInertiaEnabled,
    string FrozenColumnCount,
    string RowHeight,
    string RowHeaderWidth,
    string ColumnHeaderHeight,
    string MinColumnWidth,
    string MaxColumnWidth,
    string ColumnWidth,
    string HorizontalScrollBarVisibility,
    string VerticalScrollBarVisibility);

public sealed record BindingEditorState(
    string ControlName,
    string TargetType,
    IReadOnlyList<string> Lines,
    IReadOnlyList<string> SupportedProperties);

public sealed record LayoutEditorState(
    string ControlName,
    string Margin,
    string Padding,
    bool SupportsPadding,
    string HorizontalAlignment,
    string VerticalAlignment,
    string MinWidth,
    string MinHeight,
    string MaxWidth,
    string MaxHeight);

public sealed record RootEditorState(
    string RootKind,
    string Title,
    bool CanResize,
    string StartupLocation,
    string MinWidth,
    string MinHeight,
    string MaxWidth,
    string MaxHeight);

public sealed record TypographyEditorState(
    string ControlName,
    string FontFamily,
    string FontSize,
    string FontStyle,
    string FontWeight,
    string TextAlignment,
    string TextWrapping,
    bool SupportsTextAlignment,
    bool SupportsTextWrapping);

public sealed record TransformEditorState(
    string ControlName,
    string TranslateX,
    string TranslateY,
    string Rotation,
    string ScaleX,
    string ScaleY,
    string SkewX,
    string SkewY,
    string OriginX,
    string OriginY);

public sealed record AccessibilityEditorState(
    string ControlName,
    string ToolTip,
    string AccessibleName,
    string AutomationId,
    string HelpText,
    string AccessibilityView,
    string HeadingLevel,
    string LiveSetting,
    bool IsRequiredForForm,
    string TabIndex,
    bool IsTabStop,
    bool Focusable);

public sealed record InteractionEditorState(
    string ControlName,
    string Opacity,
    bool IsEnabled,
    bool IsVisible,
    bool IsHitTestVisible,
    bool ClipToBounds,
    bool UseLayoutRounding,
    string FlowDirection,
    string Cursor);

public sealed record EffectEditorState(
    string ControlName,
    string Kind,
    string BlurRadius,
    string OffsetX,
    string OffsetY,
    string ShadowBlurRadius,
    string ShadowColor,
    string ShadowOpacity);

public sealed record RangeEditorState(
    string ControlName,
    string ControlKind,
    string Minimum,
    string Maximum,
    string Value,
    string SmallChange,
    string LargeChange,
    string Orientation,
    bool IsDirectionReversed,
    string TickFrequency,
    string TickPlacement,
    bool IsSnapToTickEnabled,
    bool IsIndeterminate,
    bool ShowProgressText,
    string ProgressTextFormat,
    string Increment,
    string FormatString,
    bool ClipValueToMinMax,
    bool AllowSpin,
    bool ShowButtonSpinner,
    string ButtonSpinnerLocation);

public sealed record TextInputEditorState(
    string ControlName,
    string Text,
    string Watermark,
    bool AcceptsReturn,
    bool AcceptsTab,
    string TextWrapping,
    string TextAlignment,
    bool IsReadOnly,
    string MaxLength,
    string MinLines,
    string MaxLines,
    string PasswordChar,
    bool RevealPassword,
    bool UseFloatingWatermark,
    bool IsUndoEnabled,
    string UndoLimit,
    bool ClearSelectionOnLostFocus,
    bool IsInactiveSelectionHighlightEnabled);

public sealed record MaskedTextBoxEditorState(
    string ControlName,
    string Mask,
    string PromptChar,
    bool HidePromptOnLeave);

public sealed record SelectableTextBlockEditorState(
    string ControlName,
    string Text,
    string SelectionBrush,
    string SelectionForegroundBrush);

public sealed record SplitViewEditorState(
    string ControlName,
    string DisplayMode,
    bool IsPaneOpen,
    string OpenPaneLength,
    string CompactPaneLength,
    string PanePlacement,
    bool UseLightDismissOverlayMode,
    string PaneBackground);

public sealed record TabControlBehaviorEditorState(
    string ControlName,
    string TabStripPlacement,
    string HorizontalContentAlignment,
    string VerticalContentAlignment);

public sealed record SelectionEditorState(
    string ControlName,
    string ControlKind,
    string SelectedIndex,
    bool IsTextSearchEnabled,
    bool AutoScrollToSelectedItem,
    bool WrapSelection,
    bool AllowMultiple,
    bool ToggleSelection,
    bool AlwaysSelected,
    bool IsEditable,
    string Text,
    string PlaceholderText,
    string MaxDropDownHeight,
    string HorizontalContentAlignment,
    string VerticalContentAlignment);

public sealed record DateTimeEditorState(
    string ControlName,
    string ControlKind,
    string SelectedDate,
    string MinYear,
    string MaxYear,
    bool DayVisible,
    bool MonthVisible,
    bool YearVisible,
    string DayFormat,
    string MonthFormat,
    string YearFormat,
    string DisplayDate,
    string DisplayDateStart,
    string DisplayDateEnd,
    string FirstDayOfWeek,
    bool IsTodayHighlighted,
    string SelectedDateFormat,
    string CustomDateFormatString,
    string Watermark,
    bool UseFloatingWatermark,
    string HorizontalContentAlignment,
    string VerticalContentAlignment,
    string SelectedTime,
    string MinuteIncrement,
    string SecondIncrement,
    string ClockIdentifier,
    bool UseSeconds,
    string CalendarSelectionMode,
    string CalendarDisplayMode,
    bool AllowTapRangeSelection);

public sealed record ColorPickerEditorState(
    string ControlName,
    string Color,
    string ColorModel,
    string ColorSpectrumComponents,
    string ColorSpectrumShape,
    string HexInputAlphaPosition,
    bool IsAccentColorsVisible,
    bool IsAlphaEnabled,
    bool IsAlphaVisible,
    bool IsColorComponentsVisible,
    bool IsColorModelVisible,
    bool IsColorPaletteVisible,
    bool IsColorPreviewVisible,
    bool IsColorSpectrumVisible,
    bool IsColorSpectrumSliderVisible,
    bool IsComponentSliderVisible,
    bool IsComponentTextInputVisible,
    bool IsHexInputVisible,
    string PaletteColumnCount);

public sealed record AutoCompleteBoxEditorState(
    string ControlName,
    string Text,
    string Watermark,
    bool IsTextCompletionEnabled,
    string MinimumPrefixLength,
    string MinimumPopulateDelay,
    string FilterMode,
    string MaxDropDownHeight,
    bool IsDropDownOpen);

public sealed record ToggleEditorState(
    string ControlName,
    string ControlKind,
    string Content,
    string State,
    bool IsThreeState,
    string ClickMode,
    string GroupName,
    string OnContent,
    string OffContent,
    string HorizontalContentAlignment,
    string VerticalContentAlignment);

public sealed record ContainerBehaviorEditorState(
    string ControlName,
    string ControlKind,
    string Header,
    bool IsExpanded,
    string ExpandDirection,
    string HorizontalContentAlignment,
    string VerticalContentAlignment,
    string HorizontalScrollBarVisibility,
    string VerticalScrollBarVisibility,
    bool AllowAutoHide,
    bool IsScrollChainingEnabled,
    bool IsDeferredScrollingEnabled,
    bool BringIntoViewOnFocusChange,
    string HorizontalSnapPointsType,
    string VerticalSnapPointsType,
    string HorizontalSnapPointsAlignment,
    string VerticalSnapPointsAlignment);

public sealed record ImageEditorState(
    string ControlName,
    string Source,
    string Stretch,
    string StretchDirection,
    string BitmapInterpolationMode,
    string EdgeMode,
    string BitmapBlendingMode);

public sealed record ButtonEditorState(
    string ControlName,
    string Content,
    string ClickMode,
    string HotKey,
    bool IsDefault,
    bool IsCancel,
    string CommandParameter,
    string ClickHandler);

public sealed record QuickContentEditorState(
    string ControlName,
    string ControlKind,
    string PropertyName,
    string Content,
    bool IsMultiline);

public sealed record GridDefinitionEditorState(
    string ControlName,
    string RowDefinitions,
    string ColumnDefinitions,
    bool ShowGridLines);

public sealed record GridSplitterEditorState(
    string ControlName,
    string ResizeDirection,
    string ResizeBehavior,
    bool ShowsPreview,
    string KeyboardIncrement,
    string DragIncrement);

public sealed record GridCellParentOption(string DisplayName, int RowCount, int ColumnCount)
{
    public override string ToString() => DisplayName;
}

public sealed record GridCellAssignmentEditorState(
    string ControlName,
    IReadOnlyList<GridCellParentOption> Parents,
    string SelectedParentName,
    int GridRow,
    int GridColumn,
    int GridRowSpan,
    int GridColumnSpan);

public sealed record StackPanelParentOption(
    string DisplayName,
    Orientation Orientation,
    int ChildCount)
{
    public override string ToString() => $"{DisplayName} ({Orientation})";
}

public sealed record StackPanelAssignmentEditorState(
    string ControlName,
    IReadOnlyList<StackPanelParentOption> Parents,
    string SelectedParentName,
    int ItemIndex,
    double ItemSize);

public sealed record DockPanelParentOption(
    string DisplayName,
    bool LastChildFill,
    int ChildCount)
{
    public override string ToString() => DisplayName;
}

public sealed record DockPanelAssignmentEditorState(
    string ControlName,
    IReadOnlyList<DockPanelParentOption> Parents,
    string SelectedParentName,
    int ItemIndex,
    DesignerDockSide Dock,
    double ItemSize,
    bool LastChildFill);

public sealed record WrapPanelParentOption(
    string DisplayName,
    Orientation Orientation,
    int ChildCount)
{
    public override string ToString() => $"{DisplayName} ({Orientation})";
}

public sealed record WrapPanelAssignmentEditorState(
    string ControlName,
    IReadOnlyList<WrapPanelParentOption> Parents,
    string SelectedParentName,
    int ItemIndex);

public sealed record UniformGridParentOption(
    string DisplayName,
    int Rows,
    int Columns,
    int ChildCount)
{
    public override string ToString() => $"{DisplayName} ({Rows}x{Columns})";
}

public sealed record UniformGridAssignmentEditorState(
    string ControlName,
    IReadOnlyList<UniformGridParentOption> Parents,
    string SelectedParentName,
    int ItemIndex);

public sealed record CanvasParentOption(string DisplayName, int ChildCount)
{
    public override string ToString() => DisplayName;
}

public sealed record CanvasAssignmentEditorState(
    string ControlName,
    IReadOnlyList<CanvasParentOption> Parents,
    string SelectedParentName,
    int ItemIndex,
    double Left,
    double Top);

public sealed record TabSlotOption(int Index, string Header, string? ChildName)
{
    public override string ToString()
        => string.IsNullOrWhiteSpace(ChildName)
            ? $"{Index + 1}. {Header}"
            : $"{Index + 1}. {Header} ({ChildName})";
}

public sealed record TabControlParentOption(
    string DisplayName,
    IReadOnlyList<TabSlotOption> Tabs)
{
    public override string ToString() => $"{DisplayName} ({Tabs.Count} tabs)";
}

public sealed record TabControlAssignmentEditorState(
    string ControlName,
    IReadOnlyList<TabControlParentOption> Parents,
    string SelectedParentName,
    int TabIndex);

public sealed record SplitViewParentOption(
    string DisplayName,
    string? PaneChildName,
    string? ContentChildName)
{
    public override string ToString() => DisplayName;
}

public sealed record SplitViewAssignmentEditorState(
    string ControlName,
    IReadOnlyList<SplitViewParentOption> Parents,
    string SelectedParentName,
    DesignerSplitViewSlot Slot);

public sealed record ContentParentOption(string DisplayName, string ContainerType)
{
    public override string ToString() => $"{DisplayName} ({ContainerType})";
}

public sealed record ContentAssignmentEditorState(
    string ControlName,
    IReadOnlyList<ContentParentOption> Parents,
    string SelectedParentName);

public partial class MainWindowViewModel : ViewModelBase
{
    private const string DesignerMetadataPrefix = "AvaloniaUIDesigner:";
    private static readonly string[] StylePreviewStateOrder =
    [
        "pointerover",
        "pressed",
        "disabled",
        "focus",
        "focus-visible",
        "checked",
        "unchecked",
        "expanded",
        "collapsed",
    ];

    private readonly IComponentCatalog _componentCatalog;
    private readonly IDesignerSerializer _serializer;
    private readonly ComponentPackLoader _componentPackLoader = new();
    private readonly ToolboxPresetPackLoader _toolboxPresetPackLoader = new();
    private readonly Stack<HistoryEntry> _undoStack = new();
    private readonly Stack<HistoryEntry> _redoStack = new();
    private readonly Dictionary<string, string> _colorResources = new(StringComparer.Ordinal);
    private readonly List<DesignerStyleDefinition> _documentStyles = new();

    private PendingMutation? _pendingMutation;
    private bool _isSyncingSelection;
    private bool _isRefreshingStylePreviewOptions;
    private string? _currentDocumentPath;
    private DesignerCanvasDocument _lastSavedSnapshot = new(Array.Empty<DesignerElementSnapshot>());
    private List<DesignerElementSnapshot>? _clipboardSnapshots;
    private string? _sampleDataJson;
    private DesignerSampleObject? _sampleDataRoot;
    private DesignerRootSettings _rootSettings = new();
    private DesignElement? _standaloneAxamlElement;

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
        StylePreviewOptions = new ObservableCollection<StylePreviewOption>();

        ObjectTree.PropertyChanged += OnObjectTreePropertyChanged;
        Canvas.PropertyChanged += OnDesignerCanvasPropertyChanged;
        LoadRecentFilesFromDisk();
    }

    public ToolboxViewModel Toolbox { get; }
    public CanvasViewModel Canvas { get; }
    public ObjectTreeViewModel ObjectTree { get; }
    public PropertyInspectorViewModel PropertyInspector { get; }
    public ObservableCollection<string> RecentFiles { get; }
    public ObservableCollection<StylePreviewOption> StylePreviewOptions { get; }

    public event EventHandler? DocumentChanged;

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
    public bool HasStylePreviewOptions => StylePreviewOptions.Count > 1;
    public bool HasSampleData => _sampleDataRoot is not null;
    public string SampleDataJson => _sampleDataJson ?? string.Empty;
    public string RootKindLabel => _rootSettings.Kind.ToString();

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private bool _isDirty;

    [ObservableProperty]
    private StylePreviewOption? _selectedStylePreviewOption;

    partial void OnSelectedStylePreviewOptionChanged(StylePreviewOption? value)
    {
        if (!_isRefreshingStylePreviewOptions && value is not null)
        {
            SetSelectedStylePreviewState(value.PseudoClass);
        }
    }

    public void PlaceFromToolbox(double x, double y)
    {
        var item = Toolbox.SelectedItem;
        if (item is null)
        {
            StatusText = "Select a control in Toolbox first.";
            return;
        }

        PlaceToolboxItem(item, x, y);
    }

    public void PlaceToolboxItem(ToolboxItem item, double x, double y)
    {
        var snappedX = Canvas.SnapPosition(x);
        var snappedY = Canvas.SnapPosition(y);

        BeginCanvasMutation(HistoryActionType.AddElement, "Added control to canvas.");
        if (item.IsPreset)
        {
            var elements = Canvas.PlacePreset(item, snappedX, snappedY);
            foreach (var presetElement in elements)
            {
                ObjectTree.Add(presetElement);
            }

            ObjectTree.SelectByElement(Canvas.SelectedElement);
            Canvas.SelectMany(elements);
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

    public bool TryLoadComponentPack(string json, out string result)
    {
        if (!_componentPackLoader.TryLoad(
                json,
                _componentCatalog,
                displayName => Toolbox.FindItemByDisplayName(displayName) is null,
                out var pack,
                out var error))
        {
            result = error;
            return false;
        }

        Toolbox.AddComponents(pack.Definitions);
        result = $"Loaded {pack.Definitions.Count} component(s) from {pack.Name}.";
        StatusText = result;
        return true;
    }

    public bool TryLoadToolboxPresetPack(string json, out string result)
    {
        if (!_toolboxPresetPackLoader.TryLoad(
                json,
                displayName => Toolbox.FindItemByDisplayName(displayName) is null,
                typeName => _componentCatalog.TryGet(typeName, out _),
                out var pack,
                out var error))
        {
            result = error;
            return false;
        }

        if (!Toolbox.TryAddPresets(pack.Presets, out error))
        {
            result = error;
            return false;
        }

        result = $"Loaded {pack.Presets.Count} Toolbox preset(s) from {pack.Name}.";
        StatusText = result;
        return true;
    }

    public bool TryGetSelectedComponentPackDefaults(
        out string packName,
        out string displayName,
        out string namePrefix)
    {
        if (Canvas.SelectedElement is not { } target)
        {
            packName = string.Empty;
            displayName = string.Empty;
            namePrefix = string.Empty;
            StatusText = "Select a control to export as a component pack.";
            return false;
        }

        displayName = target.DisplayName;
        packName = $"{displayName} Components";
        namePrefix = target.DisplayName;
        return true;
    }

    public bool TryGetSelectedToolboxPresetDefaults(out string displayName)
    {
        var targets = Canvas.SelectedElements.ToList();
        if (!TryValidateToolboxPresetSelection(targets, out _, out var error))
        {
            displayName = string.Empty;
            StatusText = error;
            return false;
        }

        displayName = $"Preset: {targets[0].DisplayName} Layout";
        return true;
    }

    public bool TryGetSelectedToolboxPresetExportDefaults(out string displayName)
    {
        if (Toolbox.SelectedItem is not { IsPreset: true } preset)
        {
            displayName = string.Empty;
            StatusText = "Select a Toolbox preset to export.";
            return false;
        }

        displayName = preset.DisplayName;
        return true;
    }

    public bool TryExportSelectedToolboxPreset(out string json, out string error)
    {
        json = string.Empty;
        error = string.Empty;
        if (Toolbox.SelectedItem is not { IsPreset: true } preset
            || preset.PresetElements is not { Count: > 0 } elements)
        {
            error = "Select a Toolbox preset to export.";
            return false;
        }

        var document = new ToolboxPresetPackDocument
        {
            Name = preset.DisplayName,
            Presets =
            [
                new ToolboxPresetDefinition
                {
                    DisplayName = preset.DisplayName,
                    AvaloniaTypeName = preset.AvaloniaTypeName,
                    Elements = elements.ToList(),
                }
            ],
        };
        json = JsonSerializer.Serialize(document, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        return true;
    }

    public bool TryAddSelectedAsToolboxPreset(string proposedDisplayName, out string error)
    {
        error = string.Empty;
        var targets = Canvas.SelectedElements.ToList();
        if (!TryValidateToolboxPresetSelection(targets, out var canvasParent, out error))
        {
            StatusText = error;
            return false;
        }

        var displayName = proposedDisplayName.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            error = "Toolbox preset name is required.";
            StatusText = error;
            return false;
        }

        var left = targets.Min(element => element.X);
        var top = targets.Min(element => element.Y);
        var right = targets.Max(element => element.X + element.Width);
        var bottom = targets.Max(element => element.Y + element.Height);
        var snapshots = new List<DesignerElementSnapshot>();
        if (canvasParent is not null)
        {
            var groupName = $"{displayName} Canvas";
            while (targets.Any(element => string.Equals(
                       element.DisplayName,
                       groupName,
                       StringComparison.OrdinalIgnoreCase)))
            {
                groupName += " Canvas";
            }

            snapshots.Add(CreateSnapshot(canvasParent, groupName, 0, 0) with
            {
                Width = Math.Max(10, right - left),
                Height = Math.Max(10, bottom - top),
                IsLocked = false,
                ParentName = null,
                ParentLayout = DesignerParentLayoutKind.None,
                GridRow = 0,
                GridColumn = 0,
                GridRowSpan = 1,
                GridColumnSpan = 1,
                StackPanelIndex = -1,
                DockPanelIndex = -1,
                WrapPanelIndex = -1,
                UniformGridIndex = -1,
                CanvasChildIndex = -1,
                CanvasChildLeft = 0,
                CanvasChildTop = 0,
                TabIndex = -1,
                TabHeader = null,
            });

            var childIndex = 0;
            foreach (var target in targets
                         .OrderBy(element => element.CanvasChildIndex)
                         .ThenBy(element => element.X)
                         .ThenBy(element => element.Y))
            {
                snapshots.Add(CreateSnapshot(
                    target,
                    target.DisplayName,
                    target.X - left,
                    target.Y - top) with
                {
                    ParentName = groupName,
                    ParentLayout = DesignerParentLayoutKind.Canvas,
                    CanvasChildIndex = childIndex++,
                    CanvasChildLeft = target.X - left,
                    CanvasChildTop = target.Y - top,
                    GridRow = 0,
                    GridColumn = 0,
                    GridRowSpan = 1,
                    GridColumnSpan = 1,
                    StackPanelIndex = -1,
                    DockPanelIndex = -1,
                    WrapPanelIndex = -1,
                    UniformGridIndex = -1,
                    TabIndex = -1,
                    TabHeader = null,
                });
            }
        }
        else
        {
            snapshots.AddRange(targets
                .OrderBy(element => element.Y)
                .ThenBy(element => element.X)
                .Select(element => CreateSnapshot(
                    element,
                    element.DisplayName,
                    element.X - left,
                    element.Y - top) with
                {
                    ParentName = null,
                    ParentLayout = DesignerParentLayoutKind.None,
                    GridRow = 0,
                    GridColumn = 0,
                    GridRowSpan = 1,
                    GridColumnSpan = 1,
                    StackPanelIndex = -1,
                    DockPanelIndex = -1,
                    WrapPanelIndex = -1,
                    UniformGridIndex = -1,
                    CanvasChildIndex = -1,
                    CanvasChildLeft = 0,
                    CanvasChildTop = 0,
                    TabIndex = -1,
                    TabHeader = null,
                }));
        }

        var preset = new ToolboxItem(displayName, "Preset.Selection", snapshots);
        if (!Toolbox.TryAddPreset(preset, out error))
        {
            StatusText = error;
            return false;
        }

        StatusText = $"Added Toolbox preset '{displayName}' ({targets.Count} control(s)).";
        return true;
    }

    private bool TryValidateToolboxPresetSelection(
        IReadOnlyList<DesignElement> targets,
        out DesignElement? canvasParent,
        out string error)
    {
        canvasParent = null;
        error = string.Empty;
        if (targets.Count < 2)
        {
            error = "Select at least two root controls or siblings inside the same Canvas.";
            return false;
        }

        var parentName = targets[0].ParentName;
        if (targets.Any(element => !string.Equals(
                element.ParentName,
                parentName,
                StringComparison.OrdinalIgnoreCase)))
        {
            error = "Toolbox presets require root controls or siblings inside the same Canvas.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(parentName))
        {
            if (targets.Any(element => element.IsContainerChild))
            {
                error = "The selected controls have an invalid parent relationship.";
                return false;
            }

            return true;
        }

        canvasParent = Canvas.Elements.FirstOrDefault(element => string.Equals(
            element.DisplayName,
            parentName,
            StringComparison.OrdinalIgnoreCase));
        if (canvasParent?.Visual is not Avalonia.Controls.Canvas
            || targets.Any(element => element.ParentLayout != DesignerParentLayoutKind.Canvas))
        {
            canvasParent = null;
            error = "Only siblings directly inside the same Canvas can be added as a nested Toolbox preset.";
            return false;
        }

        return true;
    }

    public bool TryExportSelectedComponentPack(
        string proposedPackName,
        string proposedDisplayName,
        string proposedNamePrefix,
        out string json,
        out string error)
    {
        json = string.Empty;
        error = string.Empty;

        if (Canvas.SelectedElement is not { } target)
        {
            error = "Select a control to export as a component pack.";
            return false;
        }

        var packName = proposedPackName.Trim();
        var displayName = proposedDisplayName.Trim();
        var namePrefix = proposedNamePrefix.Trim();
        if (string.IsNullOrWhiteSpace(packName) || string.IsNullOrWhiteSpace(displayName))
        {
            error = "Pack name and Toolbox display name are required.";
            return false;
        }

        if (!IsValidControlName(namePrefix))
        {
            error = "Name prefix must start with a letter or underscore and contain only letters, numbers, or underscores.";
            return false;
        }

        if (!_componentCatalog.TryGet(target.TypeName, out _))
        {
            error = $"Unsupported Avalonia type: {target.TypeName}";
            return false;
        }

        var properties = CaptureVisualProperties(target.Visual)
            ?.Where(pair => !pair.Key.StartsWith("__", StringComparison.Ordinal)
                && !string.Equals(pair.Key, "Classes", StringComparison.Ordinal)
                && !(target.Visual is Label && string.Equals(pair.Key, "Target", StringComparison.Ordinal)))
            .ToDictionary(
                pair => pair.Key,
                pair => (string?)(DesignerResourceReferenceMetadata.TryParseExpression(pair.Value, out var resourceKey)
                    && _colorResources.TryGetValue(resourceKey, out var resourceValue)
                        ? resourceValue
                        : pair.Value),
                StringComparer.Ordinal);
        properties ??= new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var propertyName in GetStyleManagedPropertyNames(target.Visual)
                     .Concat(DesignerStyleApplicationMetadata.GetAppliedProperties(target.Visual))
                     .Distinct(StringComparer.Ordinal))
        {
            if (DesignerStyleRuntime.TryReadCurrentValue(target.Visual, propertyName, out var value))
            {
                properties[propertyName] = value;
            }
        }

        var document = new ComponentPackDocument
        {
            Name = packName,
            Components =
            [
                new ComponentPackComponent
                {
                    DisplayName = displayName,
                    AvaloniaTypeName = target.TypeName,
                    NamePrefix = namePrefix,
                    DefaultWidth = target.Width,
                    DefaultHeight = target.Height,
                    DefaultProperties = properties,
                }
            ]
        };

        json = JsonSerializer.Serialize(document, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        if (!_componentPackLoader.TryLoad(
                json,
                new BuiltInComponentCatalog(),
                _ => true,
                out _,
                out var validationError))
        {
            json = string.Empty;
            error = $"Generated component pack is invalid: {validationError}";
            return false;
        }

        return true;
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

        RefreshStylePreviewOptions();
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

        RefreshStylePreviewOptions();
        StatusText = selection.Count == 0 ? "Ready" : $"Selected {selection.Count} control(s)";
    }

    public bool SelectNextVisibleElement(bool reverse = false)
    {
        var candidates = Canvas.Elements
            .Where(element => element.IsVisibleOnArtboard)
            .ToList();
        if (candidates.Count == 0)
        {
            StatusText = "No visible controls to select.";
            return false;
        }

        var currentIndex = Canvas.SelectedElement is { } selected
            ? candidates.IndexOf(selected)
            : -1;
        var nextIndex = reverse
            ? currentIndex <= 0 ? candidates.Count - 1 : currentIndex - 1
            : currentIndex < 0 || currentIndex == candidates.Count - 1 ? 0 : currentIndex + 1;

        var next = candidates[nextIndex];
        SelectElement(next);
        StatusText = $"Selected {next.DisplayName} ({nextIndex + 1}/{candidates.Count})";
        return true;
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
            DesignerStyleApplicationMetadata.ClearApplied(element.Visual, "Opacity");
        }

        CommitCanvasMutation();
        StatusText = $"Set opacity to {normalizedOpacity * 100:0}% for {targets.Count} control(s)";
    }

    public bool TryGetSelectedAppearance(
        out string controlName,
        out IReadOnlyDictionary<string, string> appearance)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target)
        {
            controlName = string.Empty;
            appearance = new Dictionary<string, string>();
            StatusText = "Select an unlocked control to edit its appearance.";
            return false;
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        switch (target.Visual)
        {
            case Avalonia.Controls.Primitives.TemplatedControl templated:
                values["Background"] = templated.Background?.ToString() ?? string.Empty;
                values["Foreground"] = templated.Foreground?.ToString() ?? string.Empty;
                values["BorderBrush"] = templated.BorderBrush?.ToString() ?? string.Empty;
                values["BorderThickness"] = templated.BorderThickness.ToString();
                values["CornerRadius"] = templated.CornerRadius.ToString();
                break;

            case Border border:
                values["Background"] = border.Background?.ToString() ?? string.Empty;
                values["BorderBrush"] = border.BorderBrush?.ToString() ?? string.Empty;
                values["BorderThickness"] = border.BorderThickness.ToString();
                values["CornerRadius"] = border.CornerRadius.ToString();
                break;

            case TextBlock textBlock:
                values["Background"] = textBlock.Background?.ToString() ?? string.Empty;
                values["Foreground"] = textBlock.Foreground?.ToString() ?? string.Empty;
                break;

            case Shape shape:
                values["Fill"] = shape.Fill?.ToString() ?? string.Empty;
                values["Stroke"] = shape.Stroke?.ToString() ?? string.Empty;
                values["StrokeThickness"] = shape.StrokeThickness.ToString("0.###", CultureInfo.InvariantCulture);
                values["Stretch"] = shape.Stretch.ToString();
                values["StrokeDashArray"] = string.Join(
                    ",",
                    (shape.StrokeDashArray ?? [])
                    .Select(value => value.ToString("0.###", CultureInfo.InvariantCulture)));
                values["StrokeDashOffset"] = shape.StrokeDashOffset.ToString("0.###", CultureInfo.InvariantCulture);
                values["StrokeLineCap"] = shape.StrokeLineCap.ToString();
                values["StrokeJoin"] = shape.StrokeJoin.ToString();
                values["StrokeMiterLimit"] = shape.StrokeMiterLimit.ToString("0.###", CultureInfo.InvariantCulture);
                if (shape is RectangleShape rectangle)
                {
                    values["RadiusX"] = rectangle.RadiusX.ToString("0.###", CultureInfo.InvariantCulture);
                    values["RadiusY"] = rectangle.RadiusY.ToString("0.###", CultureInfo.InvariantCulture);
                }
                else if (shape is LineShape line)
                {
                    values["StartPoint"] = FormatPoint(line.StartPoint);
                    values["EndPoint"] = FormatPoint(line.EndPoint);
                }

                break;

            default:
                controlName = string.Empty;
                appearance = new Dictionary<string, string>();
                StatusText = "The selected control does not expose editable appearance properties.";
                return false;
        }

        foreach (var pair in DesignerResourceReferenceMetadata.GetReferences(target.Visual))
        {
            if (values.ContainsKey(pair.Key))
            {
                values[pair.Key] = DesignerResourceReferenceMetadata.FormatExpression(pair.Value);
            }
        }

        controlName = target.DisplayName;
        appearance = values;
        return true;
    }

    public bool SetSelectedAppearance(IReadOnlyDictionary<string, string> appearance)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target)
        {
            StatusText = "Select an unlocked control to edit its appearance.";
            return false;
        }

        if (target.Visual is not (Avalonia.Controls.Primitives.TemplatedControl or Border or TextBlock or Shape))
        {
            StatusText = "The selected control does not expose editable appearance properties.";
            return false;
        }

        if (!TryResolveAppearanceResources(appearance, out var resolvedAppearance, out var resourceReferences, out var error)
            || !TryReadOptionalBrush(resolvedAppearance, "Background", out var hasBackground, out var background, out error)
            || !TryReadOptionalBrush(resolvedAppearance, "Foreground", out var hasForeground, out var foreground, out error)
            || !TryReadOptionalBrush(resolvedAppearance, "BorderBrush", out var hasBorderBrush, out var borderBrush, out error)
            || !TryReadOptionalBrush(resolvedAppearance, "Fill", out var hasFill, out var fill, out error)
            || !TryReadOptionalBrush(resolvedAppearance, "Stroke", out var hasStroke, out var stroke, out error)
            || !TryReadThickness(appearance, "BorderThickness", out var hasBorderThickness, out var borderThickness, out error)
            || !TryReadCornerRadius(appearance, "CornerRadius", out var hasCornerRadius, out var cornerRadius, out error)
            || !TryReadOptionalDouble(appearance, "StrokeThickness", 0, out var hasStrokeThickness, out var strokeThickness, out error)
            || !TryReadOptionalEnum<Stretch>(appearance, "Stretch", out var hasStretch, out var stretch, out error)
            || !TryReadDoubleList(appearance, "StrokeDashArray", out var hasStrokeDashArray, out var strokeDashArray, out error)
            || !TryReadOptionalDouble(appearance, "StrokeDashOffset", double.NegativeInfinity, out var hasStrokeDashOffset, out var strokeDashOffset, out error)
            || !TryReadOptionalEnum<PenLineCap>(appearance, "StrokeLineCap", out var hasStrokeLineCap, out var strokeLineCap, out error)
            || !TryReadOptionalEnum<PenLineJoin>(appearance, "StrokeJoin", out var hasStrokeJoin, out var strokeJoin, out error)
            || !TryReadOptionalDouble(appearance, "StrokeMiterLimit", 0, out var hasStrokeMiterLimit, out var strokeMiterLimit, out error)
            || !TryReadOptionalDouble(appearance, "RadiusX", 0, out var hasRadiusX, out var radiusX, out error)
            || !TryReadOptionalDouble(appearance, "RadiusY", 0, out var hasRadiusY, out var radiusY, out error)
            || !TryReadOptionalPoint(appearance, "StartPoint", out var hasStartPoint, out var startPoint, out error)
            || !TryReadOptionalPoint(appearance, "EndPoint", out var hasEndPoint, out var endPoint, out error))
        {
            StatusText = error;
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated control appearance.");
        switch (target.Visual)
        {
            case Avalonia.Controls.Primitives.TemplatedControl templated:
                if (hasBackground) templated.Background = background;
                if (hasForeground) templated.Foreground = foreground;
                if (hasBorderBrush) templated.BorderBrush = borderBrush;
                if (hasBorderThickness) templated.BorderThickness = borderThickness;
                if (hasCornerRadius) templated.CornerRadius = cornerRadius;
                break;

            case Border border:
                if (hasBackground) border.Background = background;
                if (hasBorderBrush) border.BorderBrush = borderBrush;
                if (hasBorderThickness) border.BorderThickness = borderThickness;
                if (hasCornerRadius) border.CornerRadius = cornerRadius;
                break;

            case TextBlock textBlock:
                if (hasBackground) textBlock.Background = background;
                if (hasForeground) textBlock.Foreground = foreground;
                break;

            case Shape shape:
                if (hasFill) shape.Fill = fill;
                if (hasStroke) shape.Stroke = stroke;
                if (hasStrokeThickness) shape.StrokeThickness = strokeThickness;
                if (hasStretch) shape.Stretch = stretch;
                if (hasStrokeDashArray)
                {
                    shape.StrokeDashArray ??= [];
                    shape.StrokeDashArray.Clear();
                    shape.StrokeDashArray.AddRange(strokeDashArray);
                }

                if (hasStrokeDashOffset) shape.StrokeDashOffset = strokeDashOffset;
                if (hasStrokeLineCap) shape.StrokeLineCap = strokeLineCap;
                if (hasStrokeJoin) shape.StrokeJoin = strokeJoin;
                if (hasStrokeMiterLimit) shape.StrokeMiterLimit = strokeMiterLimit;
                if (shape is RectangleShape rectangle)
                {
                    if (hasRadiusX) rectangle.RadiusX = radiusX;
                    if (hasRadiusY) rectangle.RadiusY = radiusY;
                }
                else if (shape is LineShape line)
                {
                    if (hasStartPoint) line.StartPoint = startPoint;
                    if (hasEndPoint) line.EndPoint = endPoint;
                }

                break;
        }

        void PreserveBrushReference(string propertyName, bool hasValue)
        {
            if (!hasValue)
            {
                return;
            }

            DesignerStyleApplicationMetadata.ClearApplied(target.Visual, propertyName);
            DesignerResourceReferenceMetadata.SetReference(
                target.Visual,
                propertyName,
                resourceReferences.TryGetValue(propertyName, out var resourceKey) ? resourceKey : null);
        }

        PreserveBrushReference("Background", hasBackground);
        PreserveBrushReference("Foreground", hasForeground);
        PreserveBrushReference("BorderBrush", hasBorderBrush);
        PreserveBrushReference("Fill", hasFill);
        PreserveBrushReference("Stroke", hasStroke);
        if (hasBorderThickness)
        {
            DesignerStyleApplicationMetadata.ClearApplied(target.Visual, "BorderThickness");
        }

        if (hasCornerRadius)
        {
            DesignerStyleApplicationMetadata.ClearApplied(target.Visual, "CornerRadius");
        }

        foreach (var propertyName in new[]
                 {
                     "StrokeThickness", "Stretch", "StrokeDashArray", "StrokeDashOffset",
                     "StrokeLineCap", "StrokeJoin", "StrokeMiterLimit", "RadiusX", "RadiusY",
                     "StartPoint", "EndPoint",
                 })
        {
            if (appearance.ContainsKey(propertyName))
            {
                DesignerStyleApplicationMetadata.ClearApplied(target.Visual, propertyName);
            }
        }

        if (target.Visual is Border)
        {
            Canvas.ReflowContainerChildren(target);
        }

        CommitCanvasMutation();
        StatusText = $"Updated appearance for {target.DisplayName}.";
        return true;
    }

    public string GetColorResourceEditorText()
        => string.Join(
            Environment.NewLine,
            _colorResources.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key} = {pair.Value}"));

    public string GetDocumentStyleEditorText()
    {
        var sections = new List<string>();
        foreach (var style in _documentStyles)
        {
            var lines = new List<string> { $"[{style.Selector}]" };
            lines.AddRange(style.Setters.Select(setter => $"{setter.Key} = {setter.Value}"));
            sections.Add(string.Join(Environment.NewLine, lines));
        }

        return string.Join(Environment.NewLine + Environment.NewLine, sections);
    }

    public bool SetDocumentStylesFromText(string text)
    {
        if (!TryParseDocumentStylesText(text, out var styles, out var error))
        {
            StatusText = error;
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated document styles.");
        _documentStyles.Clear();
        _documentStyles.AddRange(styles);
        Canvas.SetDocumentStyles(_documentStyles);
        RefreshStylePreviewOptions();
        CommitCanvasMutation();
        StatusText = $"Updated {_documentStyles.Count} document style(s).";
        return true;
    }

    public bool TryGetSelectedStyleClasses(out string controlName, out string classes)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target)
        {
            controlName = string.Empty;
            classes = string.Empty;
            StatusText = "Select an unlocked control to edit its style classes.";
            return false;
        }

        controlName = target.DisplayName;
        classes = string.Join(" ", CanvasViewModel.GetUserStyleClasses(target.Visual));
        return true;
    }

    public bool SetSelectedStyleClassesFromText(string text)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target)
        {
            StatusText = "Select an unlocked control to edit its style classes.";
            return false;
        }

        var classes = text.Split(
                [' ', '\t', '\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var invalidClass = classes.FirstOrDefault(className => !IsValidStyleClassName(className));
        if (invalidClass is not null)
        {
            StatusText = $"Style class '{invalidClass}' is invalid.";
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated control style classes.");
        Canvas.SetStyleClasses(target.Visual, classes, clearNewStyleConflicts: true);
        RefreshStylePreviewOptions();
        CommitCanvasMutation();
        StatusText = classes.Count == 0
            ? $"Cleared style classes from {target.DisplayName}."
            : $"Applied {classes.Count} style class(es) to {target.DisplayName}.";
        return true;
    }

    public bool SetSelectedStylePreviewState(string? pseudoClass)
    {
        if (Canvas.SelectedElement is not { } target)
        {
            StatusText = "Select a control to preview a style state.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(pseudoClass))
        {
            Canvas.SetStylePreviewState(target.Visual, null);
            SyncSelectedStylePreviewOption(null);
            StatusText = $"Reset style preview for {target.DisplayName}.";
            return true;
        }

        var normalizedPseudoClass = pseudoClass.Trim().TrimStart(':').ToLowerInvariant();
        var targetType = target.Visual.GetType().Name;
        if (!DesignerStyleRuntime.IsSupportedPseudoClass(targetType, normalizedPseudoClass))
        {
            StatusText = $"{targetType} does not support :{normalizedPseudoClass} preview.";
            return false;
        }

        var classes = CanvasViewModel.GetUserStyleClasses(target.Visual).ToHashSet(StringComparer.Ordinal);
        if (!_documentStyles.Any(style =>
                string.Equals(style.TargetType, targetType, StringComparison.Ordinal)
                && string.Equals(style.PseudoClass, normalizedPseudoClass, StringComparison.Ordinal)
                && classes.Contains(style.ClassName)))
        {
            StatusText = $"No matching :{normalizedPseudoClass} style exists for {target.DisplayName}.";
            return false;
        }

        Canvas.SetStylePreviewState(target.Visual, normalizedPseudoClass);
        SyncSelectedStylePreviewOption(normalizedPseudoClass);
        StatusText = $"Previewing :{normalizedPseudoClass} on {target.DisplayName}.";
        return true;
    }

    public void RefreshStylePreviewOptions()
    {
        var selected = Canvas.SelectedElement;
        var activePseudoClass = selected is not null && Canvas.IsStylePreviewTarget(selected.Visual)
            ? Canvas.ActiveStylePreviewPseudoClass
            : null;
        var pseudoClasses = selected is null
            ? []
            : GetMatchingStylePreviewStates(selected);

        if (activePseudoClass is not null && !pseudoClasses.Contains(activePseudoClass, StringComparer.Ordinal))
        {
            Canvas.ClearStylePreviewState();
            activePseudoClass = null;
        }

        _isRefreshingStylePreviewOptions = true;
        try
        {
            StylePreviewOptions.Clear();
            if (selected is not null)
            {
                StylePreviewOptions.Add(new StylePreviewOption("Normal", null));
                foreach (var state in pseudoClasses)
                {
                    StylePreviewOptions.Add(new StylePreviewOption(FormatStylePreviewState(state), state));
                }
            }

            SelectedStylePreviewOption = StylePreviewOptions.FirstOrDefault(option =>
                    string.Equals(option.PseudoClass, activePseudoClass, StringComparison.Ordinal))
                ?? StylePreviewOptions.FirstOrDefault();
        }
        finally
        {
            _isRefreshingStylePreviewOptions = false;
        }

        OnPropertyChanged(nameof(HasStylePreviewOptions));
    }

    public bool SetColorResourcesFromText(string text)
    {
        var parsedResources = new Dictionary<string, string>(StringComparer.Ordinal);
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0 || separator == line.Length - 1)
            {
                StatusText = $"Resource line {index + 1} must use Key = Brush format.";
                return false;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (!IsValidControlName(key))
            {
                StatusText = $"Resource key '{key}' must be a valid AXAML identifier.";
                return false;
            }

            if (parsedResources.ContainsKey(key))
            {
                StatusText = $"Resource key '{key}' is duplicated.";
                return false;
            }

            try
            {
                parsedResources[key] = FormatBrushValue(Brush.Parse(value));
            }
            catch (FormatException)
            {
                StatusText = $"Resource '{key}' must contain a valid Avalonia brush.";
                return false;
            }
        }

        var missingReference = Canvas.Elements
            .SelectMany(element => DesignerResourceReferenceMetadata.GetReferences(element.Visual).Values)
            .FirstOrDefault(resourceKey => !parsedResources.ContainsKey(resourceKey));
        missingReference ??= _documentStyles
            .SelectMany(style => style.Setters.Values)
            .Select(value => DesignerResourceReferenceMetadata.TryParseExpression(value, out var resourceKey)
                ? resourceKey
                : null)
            .FirstOrDefault(resourceKey => resourceKey is not null && !parsedResources.ContainsKey(resourceKey));
        if (!string.IsNullOrWhiteSpace(missingReference))
        {
            StatusText = $"Resource '{missingReference}' is still used by the document and cannot be removed.";
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated color resources.");
        _colorResources.Clear();
        foreach (var pair in parsedResources)
        {
            _colorResources[pair.Key] = pair.Value;
        }

        Canvas.SetColorResources(_colorResources);
        RefreshResourceBackedAppearance();
        CommitCanvasMutation();
        StatusText = $"Updated {_colorResources.Count} color resource(s).";
        return true;
    }

    public bool TryGetColorResourceApplicationOptions(
        out string controlName,
        out IReadOnlyList<string> resourceNames,
        out IReadOnlyList<string> propertyNames)
    {
        resourceNames = _colorResources.Keys.OrderBy(key => key, StringComparer.Ordinal).ToList();
        if (resourceNames.Count == 0)
        {
            controlName = string.Empty;
            propertyNames = Array.Empty<string>();
            StatusText = "Create at least one color resource before applying it.";
            return false;
        }

        if (Canvas.SelectedElement is not { IsLocked: false } target)
        {
            controlName = string.Empty;
            propertyNames = Array.Empty<string>();
            StatusText = "Select an unlocked control to apply a color resource.";
            return false;
        }

        propertyNames = target.Visual switch
        {
            Avalonia.Controls.Primitives.TemplatedControl => new[] { "Background", "Foreground", "BorderBrush" },
            Border => new[] { "Background", "BorderBrush" },
            TextBlock => new[] { "Background", "Foreground" },
            Shape => new[] { "Fill", "Stroke" },
            _ => Array.Empty<string>(),
        };
        if (propertyNames.Count == 0)
        {
            controlName = string.Empty;
            StatusText = "The selected control does not expose a brush property.";
            return false;
        }

        controlName = target.DisplayName;
        return true;
    }

    public bool ApplyColorResource(string resourceName, string propertyName)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target
            || !_colorResources.TryGetValue(resourceName, out var brushValue))
        {
            StatusText = "The selected control or color resource is no longer available.";
            return false;
        }

        var brush = Brush.Parse(brushValue);
        if (!SupportsAppearanceBrush(target.Visual, propertyName))
        {
            StatusText = $"{target.DisplayName} does not support {propertyName}.";
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Applied color resource.");
        TryApplyAppearanceBrush(target.Visual, propertyName, brush);
        DesignerStyleApplicationMetadata.ClearApplied(target.Visual, propertyName);
        DesignerResourceReferenceMetadata.SetReference(target.Visual, propertyName, resourceName);
        CommitCanvasMutation();
        StatusText = $"Applied {resourceName} to {target.DisplayName}.{propertyName}.";
        return true;
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
            DesignerStyleApplicationMetadata.ClearApplied(target.Visual, "FontSize");
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
            DesignerStyleApplicationMetadata.ClearApplied(target.Visual, "Foreground");
            DesignerResourceReferenceMetadata.SetReference(target.Visual, "Foreground", null);
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
            DesignerStyleApplicationMetadata.ClearApplied(target.Visual, "FontWeight");
        }

        CommitCanvasMutation();
        StatusText = $"Set text weight to {weightName} for {targets.Count} control(s)";
    }

    public void ToggleSelectedTextBoxMultiline()
    {
        var targets = Canvas.SelectedElements
            .Where(element => !element.IsLocked && element.Visual is TextBox)
            .ToList();
        if (targets.Count == 0)
        {
            StatusText = "Select an unlocked TextBox to change its input mode.";
            return;
        }

        var enableMultiline = targets.All(element => !((TextBox)element.Visual).AcceptsReturn);
        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated TextBox input mode.");
        foreach (var target in targets)
        {
            var textBox = (TextBox)target.Visual;
            textBox.AcceptsReturn = enableMultiline;
            textBox.TextWrapping = enableMultiline ? TextWrapping.Wrap : TextWrapping.NoWrap;
            DesignerStyleApplicationMetadata.ClearApplied(textBox, "AcceptsReturn");
            DesignerStyleApplicationMetadata.ClearApplied(textBox, "TextWrapping");
            Canvas.RefreshDocumentStyles(textBox);
        }

        CommitCanvasMutation();
        StatusText = enableMultiline
            ? $"Enabled multiline input for {targets.Count} TextBox control(s)."
            : $"Enabled single-line input for {targets.Count} TextBox control(s).";
    }

    public bool TryGetSelectedGridDefinitions(out GridDefinitionEditorState state)
    {
        if (Canvas.SelectedElement is not { Visual: Grid grid } target)
        {
            state = new GridDefinitionEditorState(string.Empty, string.Empty, string.Empty, false);
            StatusText = "Select a Grid to edit its row and column definitions.";
            return false;
        }

        if (target.IsLocked)
        {
            state = new GridDefinitionEditorState(string.Empty, string.Empty, string.Empty, false);
            StatusText = "Unlock the selected Grid before editing its definitions.";
            return false;
        }

        state = new GridDefinitionEditorState(
            target.DisplayName,
            DesignerGridDefinitionRuntime.Format(grid.RowDefinitions),
            DesignerGridDefinitionRuntime.Format(grid.ColumnDefinitions),
            grid.ShowGridLines);
        return true;
    }

    public bool SetSelectedGridDefinitions(
        string rowDefinitions,
        string columnDefinitions,
        bool showGridLines)
    {
        if (Canvas.SelectedElement is not { IsLocked: false, Visual: Grid grid } target)
        {
            StatusText = "Select an unlocked Grid to edit its definitions.";
            return false;
        }

        if (!DesignerGridDefinitionRuntime.TryParse(
                rowDefinitions,
                columnDefinitions,
                out var parsedRows,
                out var parsedColumns,
                out var error))
        {
            StatusText = error;
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated Grid definitions.");
        grid.RowDefinitions = parsedRows;
        grid.ColumnDefinitions = parsedColumns;
        grid.ShowGridLines = showGridLines;
        Canvas.ReflowGridChildren(target);
        CommitCanvasMutation();
        StatusText = $"Updated Grid definitions for {target.DisplayName}.";
        return true;
    }

    public bool TryGetSelectedGridSplitterProperties(out GridSplitterEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null
            || target.IsLocked
            || !DesignerGridSplitterRuntime.IsSupportedControl(target.Visual))
        {
            state = default!;
            StatusText = target switch
            {
                null => "Select a GridSplitter before editing its behavior.",
                { IsLocked: true } => "Unlock the selected GridSplitter before editing its behavior.",
                _ => "GridSplitter behavior editing is available for GridSplitter controls.",
            };
            return false;
        }

        if (!DesignerGridSplitterRuntime.TryRead(
                target.Visual,
                out var values,
                out var error))
        {
            state = default!;
            StatusText = $"GridSplitter cannot be edited. {error}";
            return false;
        }

        state = new GridSplitterEditorState(
            target.DisplayName,
            values.ResizeDirection.ToString(),
            values.ResizeBehavior.ToString(),
            values.ShowsPreview,
            values.KeyboardIncrement.ToString("0.###", CultureInfo.InvariantCulture),
            values.DragIncrement.ToString("0.###", CultureInfo.InvariantCulture));
        return true;
    }

    public bool SetSelectedGridSplitterProperties(
        DesignerGridSplitterEditorInput input)
    {
        var target = Canvas.SelectedElement;
        if (target is null
            || target.IsLocked
            || !DesignerGridSplitterRuntime.IsSupportedControl(target.Visual))
        {
            StatusText = "Select an unlocked GridSplitter before editing its behavior.";
            return false;
        }

        if (!DesignerGridSplitterRuntime.TryRead(
                target.Visual,
                out var current,
                out var error)
            || !DesignerGridSplitterRuntime.TryParseValues(
                target.Visual,
                input,
                out var values,
                out error))
        {
            StatusText = $"GridSplitter was not changed. {error}";
            return false;
        }

        if (current == values)
        {
            StatusText = "GridSplitter behavior is unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated GridSplitter behavior.");
        DesignerGridSplitterRuntime.Apply(target.Visual, values);
        CommitCanvasMutation();
        StatusText = $"Updated GridSplitter behavior for {target.DisplayName}.";
        return true;
    }

    public bool TryGetSelectedGridCellAssignment(out GridCellAssignmentEditorState state)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target
            || target.Visual is Grid)
        {
            state = EmptyGridCellAssignmentState();
            StatusText = "Select an unlocked non-Grid control to assign it to a Grid cell.";
            return false;
        }

        var parents = Canvas.Elements
            .Where(element => element.Visual is Grid && !element.IsContainerChild && !element.IsLocked)
            .Select(element =>
            {
                var grid = (Grid)element.Visual;
                return new GridCellParentOption(
                    element.DisplayName,
                    DesignerGridDefinitionRuntime.GetRowCount(grid),
                    DesignerGridDefinitionRuntime.GetColumnCount(grid));
            })
            .ToList();
        if (parents.Count == 0)
        {
            state = EmptyGridCellAssignmentState();
            StatusText = "Place an unlocked root Grid before assigning a control to a cell.";
            return false;
        }

        var selectedParent = parents.FirstOrDefault(parent => string.Equals(
                parent.DisplayName,
                target.ParentName,
                StringComparison.OrdinalIgnoreCase))
            ?? parents[0];
        state = new GridCellAssignmentEditorState(
            target.DisplayName,
            parents,
            selectedParent.DisplayName,
            target.GridRow,
            target.GridColumn,
            target.GridRowSpan,
            target.GridColumnSpan);
        return true;
    }

    public bool SetSelectedGridCellAssignment(
        string parentName,
        int row,
        int column,
        int rowSpan,
        int columnSpan)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target
            || target.Visual is Grid)
        {
            StatusText = "Select an unlocked non-Grid control to assign it to a Grid cell.";
            return false;
        }

        var parent = Canvas.Elements.FirstOrDefault(element =>
            !element.IsContainerChild
            && !element.IsLocked
            && element.Visual is Grid
            && string.Equals(element.DisplayName, parentName, StringComparison.OrdinalIgnoreCase));
        if (parent?.Visual is not Grid grid)
        {
            StatusText = $"Grid '{parentName}' is not available.";
            return false;
        }

        var rowCount = DesignerGridDefinitionRuntime.GetRowCount(grid);
        var columnCount = DesignerGridDefinitionRuntime.GetColumnCount(grid);
        if (row < 0 || row >= rowCount
            || column < 0 || column >= columnCount
            || rowSpan < 1 || row + rowSpan > rowCount
            || columnSpan < 1 || column + columnSpan > columnCount)
        {
            StatusText = $"Cell assignment must fit within {rowCount} row(s) and {columnCount} column(s).";
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Assigned control to Grid cell.");
        target.GridRow = row;
        target.GridColumn = column;
        target.GridRowSpan = rowSpan;
        target.GridColumnSpan = columnSpan;
        target.StackPanelIndex = -1;
        target.DockPanelIndex = -1;
        target.DockPanelDock = DesignerDockSide.Left;
        target.DockPanelItemSize = 40;
        target.WrapPanelIndex = -1;
        target.UniformGridIndex = -1;
        target.CanvasChildIndex = -1;
        target.CanvasChildLeft = 0;
        target.CanvasChildTop = 0;
        target.TabIndex = -1;
        target.TabHeader = null;
        target.SplitViewSlot = DesignerSplitViewSlot.Content;
        target.ParentLayout = DesignerParentLayoutKind.Grid;
        target.ParentName = parent.DisplayName;
        Canvas.MoveElementsToFront([target]);
        Canvas.NormalizeContainerRelationships();
        ObjectTree.RebuildFrom(Canvas.Elements);
        ObjectTree.SelectByElement(target);
        CommitCanvasMutation();
        StatusText = $"Assigned {target.DisplayName} to {parent.DisplayName} row {row + 1}, column {column + 1}.";
        return true;
    }

    public bool RemoveSelectedFromGrid() => RemoveSelectedFromContainer();

    public bool RemoveSelectedFromContainer()
    {
        if (Canvas.SelectedElement is not { IsLocked: false, IsContainerChild: true } target)
        {
            StatusText = "Select an unlocked container child to move it back to the Canvas root.";
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Removed control from container.");
        target.ParentName = null;
        target.GridRow = 0;
        target.GridColumn = 0;
        target.GridRowSpan = 1;
        target.GridColumnSpan = 1;
        target.StackPanelIndex = -1;
        target.StackPanelItemSize = 40;
        target.DockPanelIndex = -1;
        target.DockPanelDock = DesignerDockSide.Left;
        target.DockPanelItemSize = 40;
        target.WrapPanelIndex = -1;
        target.UniformGridIndex = -1;
        target.CanvasChildIndex = -1;
        target.CanvasChildLeft = 0;
        target.CanvasChildTop = 0;
        target.TabIndex = -1;
        target.TabHeader = null;
        target.SplitViewSlot = DesignerSplitViewSlot.Content;
        target.IsVisibleOnArtboard = true;
        target.ParentLayout = DesignerParentLayoutKind.None;
        Canvas.NormalizeContainerRelationships();
        ObjectTree.RebuildFrom(Canvas.Elements);
        ObjectTree.SelectByElement(target);
        CommitCanvasMutation();
        StatusText = $"Moved {target.DisplayName} to the Canvas root.";
        return true;
    }

    private static GridCellAssignmentEditorState EmptyGridCellAssignmentState()
        => new(string.Empty, Array.Empty<GridCellParentOption>(), string.Empty, 0, 0, 1, 1);

    public bool TryGetSelectedStackPanelAssignment(out StackPanelAssignmentEditorState state)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target
            || target.Visual is Grid or StackPanel)
        {
            state = EmptyStackPanelAssignmentState();
            StatusText = "Select an unlocked non-container control to assign it to a StackPanel.";
            return false;
        }

        var parents = Canvas.Elements
            .Where(element => element.Visual is StackPanel && !element.IsContainerChild && !element.IsLocked)
            .Select(element =>
            {
                var stackPanel = (StackPanel)element.Visual;
                var childCount = Canvas.Elements.Count(child =>
                    !ReferenceEquals(child, target)
                    && string.Equals(child.ParentName, element.DisplayName, StringComparison.OrdinalIgnoreCase)
                    && child.IsStackPanelChild);
                return new StackPanelParentOption(element.DisplayName, stackPanel.Orientation, childCount);
            })
            .ToList();
        if (parents.Count == 0)
        {
            state = EmptyStackPanelAssignmentState();
            StatusText = "Place an unlocked root StackPanel before assigning a control.";
            return false;
        }

        var selectedParent = parents.FirstOrDefault(parent => string.Equals(
                parent.DisplayName,
                target.ParentName,
                StringComparison.OrdinalIgnoreCase))
            ?? parents[0];
        var itemIndex = target.IsStackPanelChild
            ? Math.Clamp(target.StackPanelIndex, 0, selectedParent.ChildCount)
            : selectedParent.ChildCount;
        var itemSize = target.IsStackPanelChild
            ? target.StackPanelItemSize
            : selectedParent.Orientation == Orientation.Vertical
                ? target.Height
                : target.Width;
        state = new StackPanelAssignmentEditorState(
            target.DisplayName,
            parents,
            selectedParent.DisplayName,
            itemIndex,
            Math.Max(10, itemSize));
        return true;
    }

    public bool SetSelectedStackPanelAssignment(
        string parentName,
        int itemIndex,
        double itemSize)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target
            || target.Visual is Grid or StackPanel)
        {
            StatusText = "Select an unlocked non-container control to assign it to a StackPanel.";
            return false;
        }

        var parent = Canvas.Elements.FirstOrDefault(element =>
            !element.IsContainerChild
            && !element.IsLocked
            && element.Visual is StackPanel
            && string.Equals(element.DisplayName, parentName, StringComparison.OrdinalIgnoreCase));
        if (parent is null)
        {
            StatusText = $"StackPanel '{parentName}' is not available.";
            return false;
        }

        var siblings = Canvas.Elements
            .Where(element => !ReferenceEquals(element, target)
                && element.IsStackPanelChild
                && string.Equals(element.ParentName, parent.DisplayName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(element => element.StackPanelIndex)
            .ThenBy(element => Canvas.Elements.IndexOf(element))
            .ToList();
        if (itemIndex < 0 || itemIndex > siblings.Count || !double.IsFinite(itemSize) || itemSize < 10)
        {
            StatusText = $"StackPanel position must be between 1 and {siblings.Count + 1}, with a size of at least 10.";
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Assigned control to StackPanel.");
        target.StackPanelItemSize = itemSize;
        siblings.Insert(itemIndex, target);
        Canvas.SetStackPanelChildOrder(parent, siblings);
        Canvas.MoveElementsToFrontInOrder(siblings);
        Canvas.NormalizeContainerRelationships();
        ObjectTree.RebuildFrom(Canvas.Elements);
        ObjectTree.SelectByElement(target);
        CommitCanvasMutation();
        StatusText = $"Assigned {target.DisplayName} to {parent.DisplayName} at position {itemIndex + 1}.";
        return true;
    }

    public bool MoveSelectedStackPanelItem(int offset)
    {
        if (Canvas.SelectedElement is not { IsLocked: false, IsStackPanelChild: true } target)
        {
            StatusText = "Select an unlocked StackPanel child to change its order.";
            return false;
        }

        var siblings = Canvas.Elements
            .Where(element => element.IsStackPanelChild
                && string.Equals(element.ParentName, target.ParentName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(element => element.StackPanelIndex)
            .ThenBy(element => Canvas.Elements.IndexOf(element))
            .ToList();
        var currentIndex = siblings.IndexOf(target);
        var nextIndex = currentIndex + offset;
        if (currentIndex < 0 || nextIndex < 0 || nextIndex >= siblings.Count)
        {
            StatusText = offset < 0
                ? $"{target.DisplayName} is already the first StackPanel item."
                : $"{target.DisplayName} is already the last StackPanel item.";
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Reordered StackPanel item.");
        (siblings[currentIndex], siblings[nextIndex]) = (siblings[nextIndex], siblings[currentIndex]);
        var parent = Canvas.Elements.First(element => string.Equals(
            element.DisplayName,
            target.ParentName,
            StringComparison.OrdinalIgnoreCase));
        Canvas.SetStackPanelChildOrder(parent, siblings);
        Canvas.MoveElementsToFrontInOrder(siblings);
        Canvas.NormalizeContainerRelationships();
        ObjectTree.RebuildFrom(Canvas.Elements);
        ObjectTree.SelectByElement(target);
        CommitCanvasMutation();
        StatusText = $"Moved {target.DisplayName} to StackPanel position {nextIndex + 1}.";
        return true;
    }

    private static StackPanelAssignmentEditorState EmptyStackPanelAssignmentState()
        => new(string.Empty, Array.Empty<StackPanelParentOption>(), string.Empty, 0, 40);

    public bool TryGetSelectedDockPanelAssignment(out DockPanelAssignmentEditorState state)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target
            || target.Visual is DockPanel)
        {
            state = EmptyDockPanelAssignmentState();
            StatusText = "Select an unlocked non-DockPanel control to assign it to a DockPanel.";
            return false;
        }

        var parents = Canvas.Elements
            .Where(element => element.Visual is DockPanel && !element.IsContainerChild && !element.IsLocked)
            .Select(element =>
            {
                var dockPanel = (DockPanel)element.Visual;
                var childCount = Canvas.Elements.Count(child =>
                    !ReferenceEquals(child, target)
                    && child.IsDockPanelChild
                    && string.Equals(child.ParentName, element.DisplayName, StringComparison.OrdinalIgnoreCase));
                return new DockPanelParentOption(
                    element.DisplayName,
                    dockPanel.LastChildFill,
                    childCount);
            })
            .ToList();
        if (parents.Count == 0)
        {
            state = EmptyDockPanelAssignmentState();
            StatusText = "Place an unlocked root DockPanel before assigning a control.";
            return false;
        }

        var selectedParent = parents.FirstOrDefault(parent => string.Equals(
                parent.DisplayName,
                target.ParentName,
                StringComparison.OrdinalIgnoreCase))
            ?? parents[0];
        state = new DockPanelAssignmentEditorState(
            target.DisplayName,
            parents,
            selectedParent.DisplayName,
            target.IsDockPanelChild
                ? Math.Clamp(target.DockPanelIndex, 0, selectedParent.ChildCount)
                : selectedParent.ChildCount,
            target.IsDockPanelChild ? target.DockPanelDock : DesignerDockSide.Left,
            target.IsDockPanelChild ? target.DockPanelItemSize : Math.Max(10, target.Width),
            selectedParent.LastChildFill);
        return true;
    }

    public bool SetSelectedDockPanelAssignment(
        string parentName,
        int itemIndex,
        DesignerDockSide dock,
        double itemSize,
        bool lastChildFill)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target
            || target.Visual is DockPanel)
        {
            StatusText = "Select an unlocked non-DockPanel control to assign it to a DockPanel.";
            return false;
        }

        var parent = Canvas.Elements.FirstOrDefault(element =>
            !element.IsContainerChild
            && !element.IsLocked
            && element.Visual is DockPanel
            && string.Equals(element.DisplayName, parentName, StringComparison.OrdinalIgnoreCase));
        if (parent?.Visual is not DockPanel dockPanel)
        {
            StatusText = $"DockPanel '{parentName}' is not available.";
            return false;
        }

        var siblings = Canvas.Elements
            .Where(element => !ReferenceEquals(element, target)
                && element.IsDockPanelChild
                && string.Equals(element.ParentName, parent.DisplayName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(element => element.DockPanelIndex)
            .ThenBy(element => Canvas.Elements.IndexOf(element))
            .ToList();
        if (itemIndex < 0 || itemIndex > siblings.Count || !double.IsFinite(itemSize) || itemSize < 10)
        {
            StatusText = $"DockPanel position must be between 1 and {siblings.Count + 1}, with a size of at least 10.";
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Assigned control to DockPanel.");
        target.DockPanelDock = dock;
        target.DockPanelItemSize = itemSize;
        dockPanel.LastChildFill = lastChildFill;
        siblings.Insert(itemIndex, target);
        Canvas.SetDockPanelChildOrder(parent, siblings);
        Canvas.MoveElementsToFrontInOrder(siblings);
        Canvas.NormalizeContainerRelationships();
        ObjectTree.RebuildFrom(Canvas.Elements);
        ObjectTree.SelectByElement(target);
        CommitCanvasMutation();
        StatusText = $"Assigned {target.DisplayName} to {parent.DisplayName} at position {itemIndex + 1} ({dock}).";
        return true;
    }

    public bool MoveSelectedDockPanelItem(int offset)
    {
        if (Canvas.SelectedElement is not { IsLocked: false, IsDockPanelChild: true } target)
        {
            StatusText = "Select an unlocked DockPanel child to change its order.";
            return false;
        }

        var siblings = Canvas.Elements
            .Where(element => element.IsDockPanelChild
                && string.Equals(element.ParentName, target.ParentName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(element => element.DockPanelIndex)
            .ThenBy(element => Canvas.Elements.IndexOf(element))
            .ToList();
        var currentIndex = siblings.IndexOf(target);
        var nextIndex = currentIndex + offset;
        if (currentIndex < 0 || nextIndex < 0 || nextIndex >= siblings.Count)
        {
            StatusText = offset < 0
                ? $"{target.DisplayName} is already the first DockPanel item."
                : $"{target.DisplayName} is already the last DockPanel item.";
            return false;
        }

        var parent = Canvas.Elements.First(element => string.Equals(
            element.DisplayName,
            target.ParentName,
            StringComparison.OrdinalIgnoreCase));
        BeginCanvasMutation(HistoryActionType.EditProperty, "Reordered DockPanel item.");
        (siblings[currentIndex], siblings[nextIndex]) = (siblings[nextIndex], siblings[currentIndex]);
        Canvas.SetDockPanelChildOrder(parent, siblings);
        Canvas.MoveElementsToFrontInOrder(siblings);
        Canvas.NormalizeContainerRelationships();
        ObjectTree.RebuildFrom(Canvas.Elements);
        ObjectTree.SelectByElement(target);
        CommitCanvasMutation();
        StatusText = $"Moved {target.DisplayName} to DockPanel position {nextIndex + 1}.";
        return true;
    }

    private static DockPanelAssignmentEditorState EmptyDockPanelAssignmentState()
        => new(
            string.Empty,
            Array.Empty<DockPanelParentOption>(),
            string.Empty,
            0,
            DesignerDockSide.Left,
            40,
            true);

    public bool TryGetSelectedWrapPanelAssignment(out WrapPanelAssignmentEditorState state)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target
            || target.Visual is WrapPanel)
        {
            state = EmptyWrapPanelAssignmentState();
            StatusText = "Select an unlocked non-WrapPanel control to assign it to a WrapPanel.";
            return false;
        }

        var parents = Canvas.Elements
            .Where(element => element.Visual is WrapPanel && !element.IsContainerChild && !element.IsLocked)
            .Select(element =>
            {
                var wrapPanel = (WrapPanel)element.Visual;
                var childCount = Canvas.Elements.Count(child =>
                    !ReferenceEquals(child, target)
                    && child.IsWrapPanelChild
                    && string.Equals(child.ParentName, element.DisplayName, StringComparison.OrdinalIgnoreCase));
                return new WrapPanelParentOption(element.DisplayName, wrapPanel.Orientation, childCount);
            })
            .ToList();
        if (parents.Count == 0)
        {
            state = EmptyWrapPanelAssignmentState();
            StatusText = "Place an unlocked root WrapPanel before assigning a control.";
            return false;
        }

        var selectedParent = parents.FirstOrDefault(parent => string.Equals(
                parent.DisplayName,
                target.ParentName,
                StringComparison.OrdinalIgnoreCase))
            ?? parents[0];
        state = new WrapPanelAssignmentEditorState(
            target.DisplayName,
            parents,
            selectedParent.DisplayName,
            target.IsWrapPanelChild
                ? Math.Clamp(target.WrapPanelIndex, 0, selectedParent.ChildCount)
                : selectedParent.ChildCount);
        return true;
    }

    public bool SetSelectedWrapPanelAssignment(string parentName, int itemIndex)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target
            || target.Visual is WrapPanel)
        {
            StatusText = "Select an unlocked non-WrapPanel control to assign it to a WrapPanel.";
            return false;
        }

        var parent = Canvas.Elements.FirstOrDefault(element =>
            !element.IsContainerChild
            && !element.IsLocked
            && element.Visual is WrapPanel
            && string.Equals(element.DisplayName, parentName, StringComparison.OrdinalIgnoreCase));
        if (parent is null)
        {
            StatusText = $"WrapPanel '{parentName}' is not available.";
            return false;
        }

        var siblings = Canvas.Elements
            .Where(element => !ReferenceEquals(element, target)
                && element.IsWrapPanelChild
                && string.Equals(element.ParentName, parent.DisplayName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(element => element.WrapPanelIndex)
            .ThenBy(element => Canvas.Elements.IndexOf(element))
            .ToList();
        if (itemIndex < 0 || itemIndex > siblings.Count)
        {
            StatusText = $"WrapPanel position must be between 1 and {siblings.Count + 1}.";
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Assigned control to WrapPanel.");
        siblings.Insert(itemIndex, target);
        Canvas.SetWrapPanelChildOrder(parent, siblings);
        Canvas.MoveElementsToFrontInOrder(siblings);
        Canvas.NormalizeContainerRelationships();
        ObjectTree.RebuildFrom(Canvas.Elements);
        ObjectTree.SelectByElement(target);
        CommitCanvasMutation();
        StatusText = $"Assigned {target.DisplayName} to {parent.DisplayName} at position {itemIndex + 1}.";
        return true;
    }

    public bool MoveSelectedWrapPanelItem(int offset)
    {
        if (Canvas.SelectedElement is not { IsLocked: false, IsWrapPanelChild: true } target)
        {
            StatusText = "Select an unlocked WrapPanel child to change its order.";
            return false;
        }

        var siblings = Canvas.Elements
            .Where(element => element.IsWrapPanelChild
                && string.Equals(element.ParentName, target.ParentName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(element => element.WrapPanelIndex)
            .ThenBy(element => Canvas.Elements.IndexOf(element))
            .ToList();
        var currentIndex = siblings.IndexOf(target);
        var nextIndex = currentIndex + offset;
        if (currentIndex < 0 || nextIndex < 0 || nextIndex >= siblings.Count)
        {
            StatusText = offset < 0
                ? $"{target.DisplayName} is already the first WrapPanel item."
                : $"{target.DisplayName} is already the last WrapPanel item.";
            return false;
        }

        var parent = Canvas.Elements.First(element => string.Equals(
            element.DisplayName,
            target.ParentName,
            StringComparison.OrdinalIgnoreCase));
        BeginCanvasMutation(HistoryActionType.EditProperty, "Reordered WrapPanel item.");
        (siblings[currentIndex], siblings[nextIndex]) = (siblings[nextIndex], siblings[currentIndex]);
        Canvas.SetWrapPanelChildOrder(parent, siblings);
        Canvas.MoveElementsToFrontInOrder(siblings);
        Canvas.NormalizeContainerRelationships();
        ObjectTree.RebuildFrom(Canvas.Elements);
        ObjectTree.SelectByElement(target);
        CommitCanvasMutation();
        StatusText = $"Moved {target.DisplayName} to WrapPanel position {nextIndex + 1}.";
        return true;
    }

    private static WrapPanelAssignmentEditorState EmptyWrapPanelAssignmentState()
        => new(string.Empty, Array.Empty<WrapPanelParentOption>(), string.Empty, 0);

    public bool TryGetSelectedUniformGridAssignment(out UniformGridAssignmentEditorState state)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target
            || target.Visual is UniformGrid)
        {
            state = EmptyUniformGridAssignmentState();
            StatusText = "Select an unlocked non-UniformGrid control to assign it to a UniformGrid.";
            return false;
        }

        var parents = Canvas.Elements
            .Where(element => element.Visual is UniformGrid && !element.IsContainerChild && !element.IsLocked)
            .Select(element =>
            {
                var uniformGrid = (UniformGrid)element.Visual;
                var childCount = Canvas.Elements.Count(child =>
                    !ReferenceEquals(child, target)
                    && child.IsUniformGridChild
                    && string.Equals(child.ParentName, element.DisplayName, StringComparison.OrdinalIgnoreCase));
                return new UniformGridParentOption(
                    element.DisplayName,
                    uniformGrid.Rows,
                    uniformGrid.Columns,
                    childCount);
            })
            .ToList();
        if (parents.Count == 0)
        {
            state = EmptyUniformGridAssignmentState();
            StatusText = "Place an unlocked root UniformGrid before assigning a control.";
            return false;
        }

        var selectedParent = parents.FirstOrDefault(parent => string.Equals(
                parent.DisplayName,
                target.ParentName,
                StringComparison.OrdinalIgnoreCase))
            ?? parents[0];
        state = new UniformGridAssignmentEditorState(
            target.DisplayName,
            parents,
            selectedParent.DisplayName,
            target.IsUniformGridChild
                ? Math.Clamp(target.UniformGridIndex, 0, selectedParent.ChildCount)
                : selectedParent.ChildCount);
        return true;
    }

    public bool SetSelectedUniformGridAssignment(string parentName, int itemIndex)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target
            || target.Visual is UniformGrid)
        {
            StatusText = "Select an unlocked non-UniformGrid control to assign it to a UniformGrid.";
            return false;
        }

        var parent = Canvas.Elements.FirstOrDefault(element =>
            !element.IsContainerChild
            && !element.IsLocked
            && element.Visual is UniformGrid
            && string.Equals(element.DisplayName, parentName, StringComparison.OrdinalIgnoreCase));
        if (parent is null)
        {
            StatusText = $"UniformGrid '{parentName}' is not available.";
            return false;
        }

        var siblings = Canvas.Elements
            .Where(element => !ReferenceEquals(element, target)
                && element.IsUniformGridChild
                && string.Equals(element.ParentName, parent.DisplayName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(element => element.UniformGridIndex)
            .ThenBy(element => Canvas.Elements.IndexOf(element))
            .ToList();
        if (itemIndex < 0 || itemIndex > siblings.Count)
        {
            StatusText = $"UniformGrid position must be between 1 and {siblings.Count + 1}.";
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Assigned control to UniformGrid.");
        siblings.Insert(itemIndex, target);
        Canvas.SetUniformGridChildOrder(parent, siblings);
        Canvas.MoveElementsToFrontInOrder(siblings);
        Canvas.NormalizeContainerRelationships();
        ObjectTree.RebuildFrom(Canvas.Elements);
        ObjectTree.SelectByElement(target);
        CommitCanvasMutation();
        StatusText = $"Assigned {target.DisplayName} to {parent.DisplayName} at position {itemIndex + 1}.";
        return true;
    }

    public bool MoveSelectedUniformGridItem(int offset)
    {
        if (Canvas.SelectedElement is not { IsLocked: false, IsUniformGridChild: true } target)
        {
            StatusText = "Select an unlocked UniformGrid child to change its order.";
            return false;
        }

        var siblings = Canvas.Elements
            .Where(element => element.IsUniformGridChild
                && string.Equals(element.ParentName, target.ParentName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(element => element.UniformGridIndex)
            .ThenBy(element => Canvas.Elements.IndexOf(element))
            .ToList();
        var currentIndex = siblings.IndexOf(target);
        var nextIndex = currentIndex + offset;
        if (currentIndex < 0 || nextIndex < 0 || nextIndex >= siblings.Count)
        {
            StatusText = offset < 0
                ? $"{target.DisplayName} is already the first UniformGrid item."
                : $"{target.DisplayName} is already the last UniformGrid item.";
            return false;
        }

        var parent = Canvas.Elements.First(element => string.Equals(
            element.DisplayName,
            target.ParentName,
            StringComparison.OrdinalIgnoreCase));
        BeginCanvasMutation(HistoryActionType.EditProperty, "Reordered UniformGrid item.");
        (siblings[currentIndex], siblings[nextIndex]) = (siblings[nextIndex], siblings[currentIndex]);
        Canvas.SetUniformGridChildOrder(parent, siblings);
        Canvas.MoveElementsToFrontInOrder(siblings);
        Canvas.NormalizeContainerRelationships();
        ObjectTree.RebuildFrom(Canvas.Elements);
        ObjectTree.SelectByElement(target);
        CommitCanvasMutation();
        StatusText = $"Moved {target.DisplayName} to UniformGrid position {nextIndex + 1}.";
        return true;
    }

    private static UniformGridAssignmentEditorState EmptyUniformGridAssignmentState()
        => new(string.Empty, Array.Empty<UniformGridParentOption>(), string.Empty, 0);

    public bool TryGetSelectedCanvasAssignment(out CanvasAssignmentEditorState state)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target
            || target.Visual is Canvas)
        {
            state = EmptyCanvasAssignmentState();
            StatusText = "Select an unlocked non-Canvas control to assign it to a Canvas.";
            return false;
        }

        var parents = Canvas.Elements
            .Where(element => element.Visual is Canvas && !element.IsContainerChild && !element.IsLocked)
            .Select(element => new CanvasParentOption(
                element.DisplayName,
                Canvas.Elements.Count(child =>
                    !ReferenceEquals(child, target)
                    && child.IsCanvasChild
                    && string.Equals(child.ParentName, element.DisplayName, StringComparison.OrdinalIgnoreCase))))
            .ToList();
        if (parents.Count == 0)
        {
            state = EmptyCanvasAssignmentState();
            StatusText = "Place an unlocked root Canvas before assigning a control.";
            return false;
        }

        var selectedParent = parents.FirstOrDefault(parent => string.Equals(
                parent.DisplayName,
                target.ParentName,
                StringComparison.OrdinalIgnoreCase))
            ?? parents[0];
        var parentElement = Canvas.Elements.First(element => string.Equals(
            element.DisplayName,
            selectedParent.DisplayName,
            StringComparison.OrdinalIgnoreCase));
        state = new CanvasAssignmentEditorState(
            target.DisplayName,
            parents,
            selectedParent.DisplayName,
            target.IsCanvasChild
                ? Math.Clamp(target.CanvasChildIndex, 0, selectedParent.ChildCount)
                : selectedParent.ChildCount,
            target.IsCanvasChild ? target.CanvasChildLeft : target.X - parentElement.X,
            target.IsCanvasChild ? target.CanvasChildTop : target.Y - parentElement.Y);
        return true;
    }

    public bool SetSelectedCanvasAssignment(
        string parentName,
        int itemIndex,
        double left,
        double top)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target
            || target.Visual is Canvas
            || !double.IsFinite(left)
            || !double.IsFinite(top))
        {
            StatusText = "Select an unlocked non-Canvas control and enter finite local coordinates.";
            return false;
        }

        var parent = Canvas.Elements.FirstOrDefault(element =>
            !element.IsContainerChild
            && !element.IsLocked
            && element.Visual is Canvas
            && string.Equals(element.DisplayName, parentName, StringComparison.OrdinalIgnoreCase));
        if (parent is null)
        {
            StatusText = $"Canvas '{parentName}' is not available.";
            return false;
        }

        var siblings = Canvas.Elements
            .Where(element => !ReferenceEquals(element, target)
                && element.IsCanvasChild
                && string.Equals(element.ParentName, parent.DisplayName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(element => element.CanvasChildIndex)
            .ThenBy(element => Canvas.Elements.IndexOf(element))
            .ToList();
        if (itemIndex < 0 || itemIndex > siblings.Count)
        {
            StatusText = $"Canvas z-order position must be between 1 and {siblings.Count + 1}.";
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Assigned control to Canvas.");
        target.CanvasChildLeft = left;
        target.CanvasChildTop = top;
        siblings.Insert(itemIndex, target);
        Canvas.SetCanvasChildOrder(parent, siblings);
        Canvas.MoveElementsToFrontInOrder(siblings);
        Canvas.NormalizeContainerRelationships();
        ObjectTree.RebuildFrom(Canvas.Elements);
        ObjectTree.SelectByElement(target);
        CommitCanvasMutation();
        StatusText = $"Assigned {target.DisplayName} to {parent.DisplayName} at z-order {itemIndex + 1}.";
        return true;
    }

    public bool MoveSelectedCanvasItem(int offset)
    {
        if (Canvas.SelectedElement is not { IsLocked: false, IsCanvasChild: true } target)
        {
            StatusText = "Select an unlocked Canvas child to change its z-order.";
            return false;
        }

        var siblings = Canvas.Elements
            .Where(element => element.IsCanvasChild
                && string.Equals(element.ParentName, target.ParentName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(element => element.CanvasChildIndex)
            .ThenBy(element => Canvas.Elements.IndexOf(element))
            .ToList();
        var currentIndex = siblings.IndexOf(target);
        var nextIndex = currentIndex + offset;
        if (currentIndex < 0 || nextIndex < 0 || nextIndex >= siblings.Count)
        {
            StatusText = offset < 0
                ? $"{target.DisplayName} is already the first Canvas item."
                : $"{target.DisplayName} is already the last Canvas item.";
            return false;
        }

        var parent = Canvas.Elements.First(element => string.Equals(
            element.DisplayName,
            target.ParentName,
            StringComparison.OrdinalIgnoreCase));
        BeginCanvasMutation(HistoryActionType.TransformElement, "Reordered Canvas child.");
        (siblings[currentIndex], siblings[nextIndex]) = (siblings[nextIndex], siblings[currentIndex]);
        Canvas.SetCanvasChildOrder(parent, siblings);
        Canvas.MoveElementsToFrontInOrder(siblings);
        Canvas.NormalizeContainerRelationships();
        ObjectTree.RebuildFrom(Canvas.Elements);
        ObjectTree.SelectByElement(target);
        CommitCanvasMutation();
        StatusText = $"Moved {target.DisplayName} to Canvas z-order {nextIndex + 1}.";
        return true;
    }

    private static CanvasAssignmentEditorState EmptyCanvasAssignmentState()
        => new(string.Empty, Array.Empty<CanvasParentOption>(), string.Empty, 0, 0, 0);

    public bool TryGetSelectedTabControlAssignment(out TabControlAssignmentEditorState state)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target)
        {
            state = EmptyTabControlAssignmentState();
            StatusText = "Select an unlocked control to assign it to a TabControl tab.";
            return false;
        }

        var parents = Canvas.Elements
            .Where(element => !ReferenceEquals(element, target)
                && !element.IsLocked
                && element.Visual is TabControl)
            .Where(element => !IsDescendantOf(element, target))
            .Select(element =>
            {
                var headers = ReadTabHeaders((TabControl)element.Visual);
                var slots = headers
                    .Select((header, index) => new TabSlotOption(
                        index,
                        header,
                        Canvas.Elements.FirstOrDefault(child =>
                            !ReferenceEquals(child, target)
                            && child.IsTabControlChild
                            && child.TabIndex == index
                            && string.Equals(
                                child.ParentName,
                                element.DisplayName,
                                StringComparison.OrdinalIgnoreCase))?.DisplayName))
                    .ToList();
                return new TabControlParentOption(element.DisplayName, slots);
            })
            .Where(parent => parent.Tabs.Count > 0)
            .ToList();
        if (parents.Count == 0)
        {
            state = EmptyTabControlAssignmentState();
            StatusText = "Place an unlocked TabControl with at least one tab before assigning a control.";
            return false;
        }

        var selectedParent = parents.FirstOrDefault(parent => string.Equals(
                parent.DisplayName,
                target.ParentName,
                StringComparison.OrdinalIgnoreCase))
            ?? parents[0];
        var selectedTabIndex = target.IsTabControlChild
            && string.Equals(target.ParentName, selectedParent.DisplayName, StringComparison.OrdinalIgnoreCase)
                ? Math.Clamp(target.TabIndex, 0, selectedParent.Tabs.Count - 1)
                : selectedParent.Tabs.FirstOrDefault(slot => slot.ChildName is null)?.Index ?? 0;
        state = new TabControlAssignmentEditorState(
            target.DisplayName,
            parents,
            selectedParent.DisplayName,
            selectedTabIndex);
        return true;
    }

    public bool SetSelectedTabControlAssignment(string parentName, int tabIndex)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target)
        {
            StatusText = "Select an unlocked control to assign it to a TabControl tab.";
            return false;
        }

        var parent = Canvas.Elements.FirstOrDefault(element =>
            !ReferenceEquals(element, target)
            && !element.IsLocked
            && element.Visual is TabControl
            && string.Equals(element.DisplayName, parentName, StringComparison.OrdinalIgnoreCase));
        if (parent is null || IsDescendantOf(parent, target))
        {
            StatusText = $"TabControl '{parentName}' is not available.";
            return false;
        }

        var headers = ReadTabHeaders((TabControl)parent.Visual);
        if (tabIndex < 0 || tabIndex >= headers.Count)
        {
            StatusText = $"Tab index must be between 1 and {headers.Count}.";
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Assigned control to TabControl tab.");
        var replaced = Canvas.SetTabControlChild(parent, target, tabIndex);
        Canvas.MoveElementsToFront([target]);
        Canvas.NormalizeContainerRelationships();
        ObjectTree.RebuildFrom(Canvas.Elements);
        ObjectTree.SelectByElement(target);
        CommitCanvasMutation();
        StatusText = replaced is null
            ? $"Assigned {target.DisplayName} to {parent.DisplayName} tab '{headers[tabIndex]}'."
            : $"Assigned {target.DisplayName} to {parent.DisplayName} tab '{headers[tabIndex]}'; moved {replaced.DisplayName} to the Canvas root.";
        return true;
    }

    private static TabControlAssignmentEditorState EmptyTabControlAssignmentState()
        => new(string.Empty, Array.Empty<TabControlParentOption>(), string.Empty, 0);

    public bool TryGetSelectedSplitViewAssignment(out SplitViewAssignmentEditorState state)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target)
        {
            state = EmptySplitViewAssignmentState();
            StatusText = "Select an unlocked control to assign it to a SplitView slot.";
            return false;
        }

        var parents = Canvas.Elements
            .Where(element => !ReferenceEquals(element, target)
                && !element.IsLocked
                && element.Visual is SplitView)
            .Where(element => !IsDescendantOf(element, target))
            .Select(element => new SplitViewParentOption(
                element.DisplayName,
                Canvas.Elements.FirstOrDefault(child =>
                    !ReferenceEquals(child, target)
                    && child.IsSplitViewChild
                    && child.SplitViewSlot == DesignerSplitViewSlot.Pane
                    && string.Equals(
                        child.ParentName,
                        element.DisplayName,
                        StringComparison.OrdinalIgnoreCase))?.DisplayName,
                Canvas.Elements.FirstOrDefault(child =>
                    !ReferenceEquals(child, target)
                    && child.IsSplitViewChild
                    && child.SplitViewSlot == DesignerSplitViewSlot.Content
                    && string.Equals(
                        child.ParentName,
                        element.DisplayName,
                        StringComparison.OrdinalIgnoreCase))?.DisplayName))
            .ToList();
        if (parents.Count == 0)
        {
            state = EmptySplitViewAssignmentState();
            StatusText = "Place an unlocked SplitView before assigning a control.";
            return false;
        }

        var selectedParent = parents.FirstOrDefault(parent => string.Equals(
                parent.DisplayName,
                target.ParentName,
                StringComparison.OrdinalIgnoreCase))
            ?? parents[0];
        var selectedSlot = target.IsSplitViewChild
            && string.Equals(target.ParentName, selectedParent.DisplayName, StringComparison.OrdinalIgnoreCase)
                ? target.SplitViewSlot
                : selectedParent.ContentChildName is null
                    ? DesignerSplitViewSlot.Content
                    : DesignerSplitViewSlot.Pane;
        state = new SplitViewAssignmentEditorState(
            target.DisplayName,
            parents,
            selectedParent.DisplayName,
            selectedSlot);
        return true;
    }

    public bool SetSelectedSplitViewAssignment(string parentName, DesignerSplitViewSlot slot)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target)
        {
            StatusText = "Select an unlocked control to assign it to a SplitView slot.";
            return false;
        }

        var parent = Canvas.Elements.FirstOrDefault(element =>
            !ReferenceEquals(element, target)
            && !element.IsLocked
            && element.Visual is SplitView
            && string.Equals(element.DisplayName, parentName, StringComparison.OrdinalIgnoreCase));
        if (parent is null || IsDescendantOf(parent, target))
        {
            StatusText = $"SplitView '{parentName}' is not available.";
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Assigned control to SplitView slot.");
        var replaced = Canvas.SetSplitViewChild(parent, target, slot);
        var splitChildren = Canvas.Elements
            .Where(child => child.IsSplitViewChild
                && string.Equals(child.ParentName, parent.DisplayName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(child => child.SplitViewSlot == DesignerSplitViewSlot.Content ? 0 : 1)
            .ToList();
        Canvas.MoveElementsToFrontInOrder(splitChildren);
        Canvas.NormalizeContainerRelationships();
        ObjectTree.RebuildFrom(Canvas.Elements);
        ObjectTree.SelectByElement(target);
        CommitCanvasMutation();
        StatusText = replaced is null
            ? $"Assigned {target.DisplayName} to {parent.DisplayName} {slot}."
            : $"Assigned {target.DisplayName} to {parent.DisplayName} {slot}; moved {replaced.DisplayName} to the Canvas root.";
        return true;
    }

    private static SplitViewAssignmentEditorState EmptySplitViewAssignmentState()
        => new(
            string.Empty,
            Array.Empty<SplitViewParentOption>(),
            string.Empty,
            DesignerSplitViewSlot.Content);

    public bool TryGetSelectedContentAssignment(out ContentAssignmentEditorState state)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target)
        {
            state = EmptyContentAssignmentState();
            StatusText = "Select an unlocked control to assign it as container content.";
            return false;
        }

        var parents = Canvas.Elements
            .Where(element => !ReferenceEquals(element, target)
                && !element.IsContainerChild
                && !element.IsLocked
                && IsDesignerContentContainer(element.Visual))
            .Where(element => !IsDescendantOf(element, target))
            .Select(element => new ContentParentOption(
                element.DisplayName,
                element.Visual.GetType().Name))
            .ToList();
        if (parents.Count == 0)
        {
            state = EmptyContentAssignmentState();
            StatusText = "Place an unlocked root Border, ContentControl, UserControl, ScrollViewer, or Expander first.";
            return false;
        }

        var selectedParent = parents.FirstOrDefault(parent => string.Equals(
                parent.DisplayName,
                target.ParentName,
                StringComparison.OrdinalIgnoreCase))
            ?? parents[0];
        state = new ContentAssignmentEditorState(
            target.DisplayName,
            parents,
            selectedParent.DisplayName);
        return true;
    }

    public bool SetSelectedContentAssignment(string parentName)
    {
        if (Canvas.SelectedElement is not { IsLocked: false } target)
        {
            StatusText = "Select an unlocked control to assign it as container content.";
            return false;
        }

        var parent = Canvas.Elements.FirstOrDefault(element =>
            !ReferenceEquals(element, target)
            && !element.IsContainerChild
            && !element.IsLocked
            && IsDesignerContentContainer(element.Visual)
            && string.Equals(element.DisplayName, parentName, StringComparison.OrdinalIgnoreCase));
        if (parent is null || IsDescendantOf(parent, target))
        {
            StatusText = $"Content container '{parentName}' is not available.";
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Assigned control as container content.");
        var replaced = Canvas.SetContentChild(parent, target);
        Canvas.MoveElementsToFront([target]);
        Canvas.NormalizeContainerRelationships();
        ObjectTree.RebuildFrom(Canvas.Elements);
        ObjectTree.SelectByElement(target);
        CommitCanvasMutation();
        StatusText = replaced is null
            ? $"Assigned {target.DisplayName} as content of {parent.DisplayName}."
            : $"Assigned {target.DisplayName} as content of {parent.DisplayName}; moved {replaced.DisplayName} to the Canvas root.";
        return true;
    }

    private bool IsDescendantOf(DesignElement candidate, DesignElement ancestor)
    {
        var current = candidate;
        var visited = new HashSet<DesignElement>();
        while (current.ParentName is not null && visited.Add(current))
        {
            var parent = Canvas.Elements.FirstOrDefault(element => string.Equals(
                element.DisplayName,
                current.ParentName,
                StringComparison.OrdinalIgnoreCase));
            if (parent is null)
            {
                return false;
            }

            if (ReferenceEquals(parent, ancestor))
            {
                return true;
            }

            current = parent;
        }

        return false;
    }

    private static ContentAssignmentEditorState EmptyContentAssignmentState()
        => new(string.Empty, Array.Empty<ContentParentOption>(), string.Empty);

    public bool TryGetSelectedItems(out string controlName, out IReadOnlyList<string> items)
    {
        if (!TryGetSelectedItemsEditor(out var state))
        {
            controlName = string.Empty;
            items = Array.Empty<string>();
            return false;
        }

        controlName = state.ControlName;
        items = state.Items;
        return true;
    }

    public bool TryGetSelectedItems(
        out string controlName,
        out IReadOnlyList<string> items,
        out bool supportsHierarchy)
    {
        if (!TryGetSelectedItemsEditor(out var state))
        {
            controlName = string.Empty;
            items = Array.Empty<string>();
            supportsHierarchy = false;
            return false;
        }

        controlName = state.ControlName;
        items = state.Items;
        supportsHierarchy = state.Mode is ItemsEditorMode.TreeView or ItemsEditorMode.Menu;
        return true;
    }

    public bool TryGetSelectedItemsEditor(out ItemsEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null)
        {
            state = EmptyItemsEditorState();
            StatusText = "Select a ComboBox, AutoCompleteBox, ListBox, ItemsControl, TreeView, Menu, TabControl, or DataGrid to edit its items or columns.";
            return false;
        }

        if (target.IsLocked)
        {
            state = EmptyItemsEditorState();
            StatusText = "Unlock the selected control before editing its items.";
            return false;
        }

        switch (target.Visual)
        {
            case AutoCompleteBox autoCompleteBox:
                state = new ItemsEditorState(
                    target.DisplayName,
                    ReadAutoCompleteBoxItems(autoCompleteBox),
                    ItemsEditorMode.Flat);
                return true;
            case ComboBox comboBox:
                state = new ItemsEditorState(
                    target.DisplayName,
                    ReadItems(comboBox),
                    ItemsEditorMode.Flat);
                return true;
            case ListBox listBox:
                state = new ItemsEditorState(
                    target.DisplayName,
                    ReadItems(listBox),
                    ItemsEditorMode.Flat);
                return true;
            case TreeView treeView:
                state = new ItemsEditorState(
                    target.DisplayName,
                    DesignerTreeItemRuntime.FormatEditorLines(treeView),
                    ItemsEditorMode.TreeView);
                return true;
            case Menu menu:
                state = new ItemsEditorState(
                    target.DisplayName,
                    DesignerMenuItemRuntime.FormatEditorLines(menu),
                    ItemsEditorMode.Menu);
                return true;
            case DataGrid dataGrid:
                state = new ItemsEditorState(
                    target.DisplayName,
                    DesignerDataGridRuntime.FormatEditorLines(dataGrid),
                    ItemsEditorMode.DataGrid);
                return true;
            case TabControl tabControl:
                state = new ItemsEditorState(
                    target.DisplayName,
                    ReadTabHeaders(tabControl),
                    ItemsEditorMode.Flat);
                return true;
            case ItemsControl itemsControl when itemsControl.GetType() == typeof(ItemsControl):
                state = new ItemsEditorState(
                    target.DisplayName,
                    ReadItems(itemsControl),
                    ItemsEditorMode.Flat);
                return true;
            default:
                state = EmptyItemsEditorState();
                StatusText = "Item and column editing is available for ComboBox, AutoCompleteBox, ListBox, ItemsControl, TreeView, Menu, TabControl, and DataGrid controls.";
                return false;
        }
    }

    public bool TryGetSelectedBindings(out BindingEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null)
        {
            state = new BindingEditorState(string.Empty, string.Empty, [], []);
            StatusText = "Select a control before editing bindings.";
            return false;
        }

        if (target.IsLocked)
        {
            state = new BindingEditorState(string.Empty, string.Empty, [], []);
            StatusText = "Unlock the selected control before editing bindings.";
            return false;
        }

        var targetType = target.Visual.GetType().Name;
        var supportedProperties = DesignerBindingRuntime.GetSupportedProperties(targetType);
        state = new BindingEditorState(
            target.DisplayName,
            targetType,
            DesignerBindingRuntime.FormatEditorLines(target.Visual),
            supportedProperties);
        return true;
    }

    public bool SetSelectedBindings(IEnumerable<string> lines)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            StatusText = "Select an unlocked control before editing bindings.";
            return false;
        }

        var targetType = target.Visual.GetType().Name;
        if (!DesignerBindingRuntime.TryParseEditorLines(
                targetType,
                lines,
                out var definitions,
                out var error))
        {
            StatusText = $"Bindings were not changed. {error}";
            return false;
        }

        var currentDefinitions = DesignerBindingRuntime.ReadBindings(target.Visual);
        if (string.Equals(
                DesignerBindingRuntime.Serialize(currentDefinitions),
                DesignerBindingRuntime.Serialize(definitions),
                StringComparison.Ordinal))
        {
            StatusText = "Bindings are unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated control bindings.");
        DesignerBindingRuntime.ReplaceBindings(target.Visual, definitions);
        RefreshSampleDataPreview();
        CommitCanvasMutation();
        StatusText = definitions.Count == 0
            ? $"Cleared bindings from {target.DisplayName}."
            : $"Updated {definitions.Count} binding(s) on {target.DisplayName}.";
        return true;
    }

    public bool TryGetSelectedLayoutProperties(out LayoutEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null)
        {
            state = new LayoutEditorState(
                string.Empty,
                "0",
                "0",
                false,
                HorizontalAlignment.Stretch.ToString(),
                VerticalAlignment.Stretch.ToString(),
                "0",
                "0",
                string.Empty,
                string.Empty);
            StatusText = "Select a control before editing layout properties.";
            return false;
        }

        if (target.IsLocked)
        {
            state = new LayoutEditorState(
                string.Empty,
                "0",
                "0",
                false,
                HorizontalAlignment.Stretch.ToString(),
                VerticalAlignment.Stretch.ToString(),
                "0",
                "0",
                string.Empty,
                string.Empty);
            StatusText = "Unlock the selected control before editing layout properties.";
            return false;
        }

        var values = DesignerLayoutRuntime.Read(target.Visual);
        state = new LayoutEditorState(
            target.DisplayName,
            DesignerLayoutRuntime.FormatThickness(values.Margin),
            DesignerLayoutRuntime.FormatThickness(values.Padding),
            DesignerLayoutRuntime.SupportsPadding(target.Visual),
            values.HorizontalAlignment.ToString(),
            values.VerticalAlignment.ToString(),
            DesignerLayoutRuntime.FormatNumber(values.MinWidth),
            DesignerLayoutRuntime.FormatNumber(values.MinHeight),
            DesignerLayoutRuntime.FormatMaximum(values.MaxWidth),
            DesignerLayoutRuntime.FormatMaximum(values.MaxHeight));
        return true;
    }

    public bool SetSelectedLayoutProperties(
        string margin,
        string padding,
        string horizontalAlignment,
        string verticalAlignment,
        string minWidth,
        string minHeight,
        string maxWidth,
        string maxHeight)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            StatusText = "Select an unlocked control before editing layout properties.";
            return false;
        }

        if (!DesignerLayoutRuntime.TryParseValues(
                target.Visual,
                margin,
                padding,
                horizontalAlignment,
                verticalAlignment,
                minWidth,
                minHeight,
                maxWidth,
                maxHeight,
                out var values,
                out var error))
        {
            StatusText = $"Layout properties were not changed. {error}";
            return false;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated common layout properties.");
        DesignerLayoutRuntime.Apply(target.Visual, values);
        CommitCanvasMutation();
        StatusText = $"Updated layout constraints for {target.DisplayName}.";
        return true;
    }

    public bool TryGetSelectedTypographyProperties(out TypographyEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null
            || target.IsLocked
            || !DesignerTypographyRuntime.SupportsTypography(target.Visual))
        {
            state = new TypographyEditorState(
                string.Empty,
                FontFamily.Default.ToString(),
                "12",
                FontStyle.Normal.ToString(),
                FontWeight.Normal.ToString(),
                TextAlignment.Start.ToString(),
                TextWrapping.NoWrap.ToString(),
                false,
                false);
            StatusText = target is { IsLocked: true }
                ? "Unlock the selected control before editing typography."
                : "Select a text or font-aware control before editing typography.";
            return false;
        }

        var values = DesignerTypographyRuntime.Read(target.Visual);
        state = new TypographyEditorState(
            target.DisplayName,
            values.FontFamily,
            values.FontSize.ToString("0.###", CultureInfo.InvariantCulture),
            values.FontStyle.ToString(),
            DesignerTypographyRuntime.FormatFontWeight(values.FontWeight),
            values.TextAlignment.ToString(),
            values.TextWrapping.ToString(),
            DesignerTypographyRuntime.SupportsTextAlignment(target.Visual),
            DesignerTypographyRuntime.SupportsTextWrapping(target.Visual));
        return true;
    }

    public bool SetSelectedTypographyProperties(
        string fontFamily,
        string fontSize,
        string fontStyle,
        string fontWeight,
        string textAlignment,
        string textWrapping)
    {
        var target = Canvas.SelectedElement;
        if (target is null
            || target.IsLocked
            || !DesignerTypographyRuntime.SupportsTypography(target.Visual))
        {
            StatusText = "Select an unlocked text or font-aware control before editing typography.";
            return false;
        }

        if (!DesignerTypographyRuntime.TryParseValues(
                target.Visual,
                fontFamily,
                fontSize,
                fontStyle,
                fontWeight,
                textAlignment,
                textWrapping,
                out var values,
                out var error))
        {
            StatusText = $"Typography properties were not changed. {error}";
            return false;
        }

        if (DesignerTypographyRuntime.Read(target.Visual) == values)
        {
            StatusText = "Typography properties are unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated typography properties.");
        DesignerTypographyRuntime.Apply(target.Visual, values);
        foreach (var propertyName in new[]
                 {
                     "FontFamily",
                     "FontSize",
                     "FontStyle",
                     "FontWeight",
                     "TextAlignment",
                     "TextWrapping",
                 })
        {
            DesignerStyleApplicationMetadata.ClearApplied(target.Visual, propertyName);
        }

        CommitCanvasMutation();
        StatusText = $"Updated typography for {target.DisplayName}.";
        return true;
    }

    public bool TryGetSelectedTransformProperties(out TransformEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            state = CreateTransformEditorState(string.Empty, DesignerTransformValues.Default);
            StatusText = target is { IsLocked: true }
                ? "Unlock the selected control before editing its transform."
                : "Select a control before editing its transform.";
            return false;
        }

        if (!DesignerTransformRuntime.TryRead(target.Visual, out var values, out var error))
        {
            state = CreateTransformEditorState(target.DisplayName, DesignerTransformValues.Default);
            StatusText = $"Transform properties cannot be edited. {error}";
            return false;
        }

        state = CreateTransformEditorState(target.DisplayName, values);
        return true;
    }

    public bool SetSelectedTransformProperties(
        string translateX,
        string translateY,
        string rotation,
        string scaleX,
        string scaleY,
        string skewX,
        string skewY,
        string originX,
        string originY)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            StatusText = "Select an unlocked control before editing its transform.";
            return false;
        }

        if (!DesignerTransformRuntime.TryParseValues(
                translateX,
                translateY,
                rotation,
                scaleX,
                scaleY,
                skewX,
                skewY,
                originX,
                originY,
                out var values,
                out var error))
        {
            StatusText = $"Transform properties were not changed. {error}";
            return false;
        }

        if (!DesignerTransformRuntime.TryRead(target.Visual, out var current, out error))
        {
            StatusText = $"Transform properties were not changed. {error}";
            return false;
        }

        if (DesignerTransformRuntime.AreEquivalent(current, values))
        {
            StatusText = "Transform properties are unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated common transform properties.");
        DesignerTransformRuntime.Apply(target.Visual, values);
        DesignerStyleApplicationMetadata.ClearApplied(target.Visual, "RenderTransform");
        DesignerStyleApplicationMetadata.ClearApplied(target.Visual, "RenderTransformOrigin");
        CommitCanvasMutation();
        StatusText = $"Updated transform for {target.DisplayName}.";
        return true;
    }

    private static TransformEditorState CreateTransformEditorState(
        string controlName,
        DesignerTransformValues values)
        => new(
            controlName,
            FormatTransformEditorNumber(values.TranslateX),
            FormatTransformEditorNumber(values.TranslateY),
            FormatTransformEditorNumber(values.Rotation),
            FormatTransformEditorNumber(values.ScaleX),
            FormatTransformEditorNumber(values.ScaleY),
            FormatTransformEditorNumber(values.SkewX),
            FormatTransformEditorNumber(values.SkewY),
            FormatTransformEditorNumber(values.OriginX),
            FormatTransformEditorNumber(values.OriginY));

    private static string FormatTransformEditorNumber(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    public RootEditorState GetRootEditorState()
        => new(
            _rootSettings.Kind.ToString(),
            _rootSettings.Title,
            _rootSettings.CanResize,
            _rootSettings.StartupLocation.ToString(),
            FormatRootNumber(_rootSettings.MinWidth),
            FormatRootNumber(_rootSettings.MinHeight),
            FormatRootMaximum(_rootSettings.MaxWidth),
            FormatRootMaximum(_rootSettings.MaxHeight));

    public bool SetRootProperties(
        string rootKind,
        string title,
        bool canResize,
        string startupLocation,
        string minWidth,
        string minHeight,
        string maxWidth,
        string maxHeight)
    {
        if (!Enum.TryParse<DesignerRootKind>(rootKind, true, out var parsedKind)
            || !Enum.IsDefined(parsedKind))
        {
            StatusText = "Root properties were not changed. Choose Window or UserControl.";
            return false;
        }

        if (!Enum.TryParse<DesignerWindowStartupLocation>(
                startupLocation,
                true,
                out var parsedStartupLocation)
            || !Enum.IsDefined(parsedStartupLocation))
        {
            StatusText = "Root properties were not changed. Choose a supported startup location.";
            return false;
        }

        if (title.Any(char.IsControl))
        {
            StatusText = "Root properties were not changed. Window title must be a single line without control characters.";
            return false;
        }

        if (!TryParseRootConstraint(minWidth, false, out var parsedMinWidth)
            || !TryParseRootConstraint(minHeight, false, out var parsedMinHeight)
            || !TryParseRootConstraint(maxWidth, true, out var parsedMaxWidth)
            || !TryParseRootConstraint(maxHeight, true, out var parsedMaxHeight))
        {
            StatusText = "Root properties were not changed. Sizes must be non-negative numbers; leave a maximum blank for no limit.";
            return false;
        }

        if (parsedMinWidth > parsedMaxWidth || parsedMinHeight > parsedMaxHeight)
        {
            StatusText = "Root properties were not changed. Each minimum size must not exceed its maximum.";
            return false;
        }

        var updated = parsedKind == DesignerRootKind.UserControl
            ? new DesignerRootSettings(
                parsedKind,
                MinWidth: parsedMinWidth,
                MinHeight: parsedMinHeight,
                MaxWidth: parsedMaxWidth,
                MaxHeight: parsedMaxHeight)
            : new DesignerRootSettings(
                parsedKind,
                title,
                canResize,
                parsedStartupLocation,
                parsedMinWidth,
                parsedMinHeight,
                parsedMaxWidth,
                parsedMaxHeight);
        if (updated == _rootSettings)
        {
            StatusText = "Root properties are unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditRootProperties, "Updated document root properties.");
        _rootSettings = updated;
        OnPropertyChanged(nameof(RootKindLabel));
        CommitCanvasMutation();
        StatusText = $"Updated {_rootSettings.Kind} root properties.";
        return true;
    }

    private static bool TryParseRootConstraint(string value, bool allowEmpty, out double result)
    {
        if (allowEmpty && (string.IsNullOrWhiteSpace(value)
                || string.Equals(value.Trim(), "Infinity", StringComparison.OrdinalIgnoreCase)))
        {
            result = double.PositiveInfinity;
            return true;
        }

        return double.TryParse(
                value.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out result)
            && double.IsFinite(result)
            && result >= 0;
    }

    private static string FormatRootNumber(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string FormatRootMaximum(double value)
        => double.IsPositiveInfinity(value) ? string.Empty : FormatRootNumber(value);

    public string GetSampleDataEditorText()
        => _sampleDataJson ?? """
        {
          "User": {
            "Name": "Ada Lovelace",
            "Email": "ada@example.com"
          },
          "CanEdit": true,
          "Progress": 72,
          "Items": [
            { "Name": "Alpha", "Status": "Ready" },
            { "Name": "Beta", "Status": "Review" }
          ]
        }
        """;

    public bool TryValidateSampleDataJson(string json, out string result)
    {
        if (!DesignerSampleDataRuntime.TryParse(json, out var document, out result))
        {
            return false;
        }

        result = document is null
            ? "Sample data is empty; applying it will clear the current sample DataContext."
            : $"Sample data JSON is valid ({document.Root.Count} top-level propert{(document.Root.Count == 1 ? "y" : "ies")}).";
        return true;
    }

    public bool TrySetSampleDataJson(string json, out string result)
    {
        if (!DesignerSampleDataRuntime.TryParse(json, out var document, out result))
        {
            return false;
        }

        var canonical = document?.Json;
        if (string.Equals(_sampleDataJson, canonical, StringComparison.Ordinal))
        {
            result = document is null
                ? "Sample data is already clear."
                : "Sample data is unchanged.";
            StatusText = result;
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditSampleData, "Updated sample DataContext.");
        ClearSampleDataPreview();
        _sampleDataJson = canonical;
        _sampleDataRoot = document?.Root;
        var applied = RefreshSampleDataPreview();
        CommitCanvasMutation();
        OnPropertyChanged(nameof(HasSampleData));
        OnPropertyChanged(nameof(SampleDataJson));

        result = document is null
            ? "Cleared sample DataContext."
            : FormatSampleApplyResult(applied);
        StatusText = result;
        return true;
    }

    public void SetSelectedItems(IEnumerable<string> items)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            StatusText = "Select an unlocked ComboBox, AutoCompleteBox, ListBox, ItemsControl, TreeView, Menu, TabControl, or DataGrid to edit its items or columns.";
            return;
        }

        if (target.Visual is TreeView treeView)
        {
            if (!DesignerTreeItemRuntime.TryParseEditorLines(items, out var definitions, out var error))
            {
                StatusText = $"TreeView items were not changed. {error}";
                return;
            }

            var currentDefinitions = DesignerTreeItemRuntime.ReadItems(treeView);
            if (DesignerTreeItemRuntime.AreEquivalent(currentDefinitions, definitions))
            {
                StatusText = "TreeView items are unchanged.";
                return;
            }

            BeginCanvasMutation(HistoryActionType.EditProperty, "Updated TreeView items.");
            DesignerTreeItemRuntime.ReplaceItems(treeView, definitions);
            CommitCanvasMutation();
            StatusText = $"Updated {CountTreeItems(definitions)} TreeView item(s).";
            return;
        }

        if (target.Visual is Menu menu)
        {
            if (!DesignerMenuItemRuntime.TryParseEditorLines(items, out var definitions, out var error))
            {
                StatusText = $"Menu items were not changed. {error}";
                return;
            }

            var currentDefinitions = DesignerMenuItemRuntime.ReadItems(menu);
            if (DesignerMenuItemRuntime.AreEquivalent(currentDefinitions, definitions))
            {
                StatusText = "Menu items are unchanged.";
                return;
            }

            BeginCanvasMutation(HistoryActionType.EditProperty, "Updated Menu items.");
            DesignerMenuItemRuntime.ReplaceItems(menu, definitions);
            CommitCanvasMutation();
            StatusText = $"Updated {DesignerMenuItemRuntime.CountEntries(definitions)} Menu entry(s).";
            return;
        }

        if (target.Visual is DataGrid dataGrid)
        {
            if (!DesignerDataGridRuntime.TryParseEditorLines(items, out var definitions, out var error))
            {
                StatusText = $"DataGrid columns were not changed. {error}";
                return;
            }

            var currentDefinitions = DesignerDataGridRuntime.ReadColumns(dataGrid);
            if (DesignerDataGridRuntime.AreEquivalent(currentDefinitions, definitions))
            {
                StatusText = "DataGrid columns are unchanged.";
                return;
            }

            BeginCanvasMutation(HistoryActionType.EditProperty, "Updated DataGrid columns.");
            DesignerDataGridRuntime.ReplaceColumns(dataGrid, definitions);
            CommitCanvasMutation();
            StatusText = $"Updated {definitions.Count} DataGrid column(s).";
            return;
        }

        var updatedItems = items
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .ToList();

        switch (target.Visual)
        {
            case AutoCompleteBox autoCompleteBox:
                if (ReadAutoCompleteBoxItems(autoCompleteBox).SequenceEqual(updatedItems, StringComparer.Ordinal))
                {
                    StatusText = "AutoCompleteBox items are unchanged.";
                    return;
                }

                BeginCanvasMutation(HistoryActionType.EditProperty, "Updated AutoCompleteBox items.");
                autoCompleteBox.ItemsSource = updatedItems;
                CommitCanvasMutation();
                StatusText = $"Updated {updatedItems.Count} AutoCompleteBox item(s).";
                return;
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
                var promotedTabChildren = Canvas.SynchronizeTabControlChildren(target, updatedItems);
                Canvas.NormalizeContainerRelationships();
                ObjectTree.RebuildFrom(Canvas.Elements);
                ObjectTree.SelectByElement(target);
                CommitCanvasMutation();
                StatusText = promotedTabChildren.Count == 0
                    ? $"Updated {updatedItems.Count} TabControl tab(s)."
                    : $"Updated {updatedItems.Count} TabControl tab(s); moved {promotedTabChildren.Count} orphaned child control(s) to the Canvas root.";
                return;

            case ItemsControl itemsControl when itemsControl.GetType() == typeof(ItemsControl):
                if (ReadItems(itemsControl).SequenceEqual(updatedItems, StringComparer.Ordinal))
                {
                    StatusText = "ItemsControl items are unchanged.";
                    return;
                }

                BeginCanvasMutation(HistoryActionType.EditProperty, "Updated ItemsControl items.");
                ReplaceItems(itemsControl, updatedItems);
                CommitCanvasMutation();
                StatusText = $"Updated {updatedItems.Count} ItemsControl item(s).";
                return;

            default:
                StatusText = "Item and column editing is available for ComboBox, ListBox, ItemsControl, TreeView, Menu, TabControl, and DataGrid controls.";
                return;
        }
    }

    private static ItemsEditorState EmptyItemsEditorState()
        => new(string.Empty, Array.Empty<string>(), ItemsEditorMode.Flat);

    public bool TryGetSelectedDataGridBehaviorProperties(
        out DataGridBehaviorEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null
            || target.IsLocked
            || !DesignerDataGridBehaviorRuntime.IsSupportedControl(target.Visual))
        {
            state = default!;
            StatusText = target switch
            {
                null => "Select a DataGrid before editing table behavior.",
                { IsLocked: true } => "Unlock the selected DataGrid before editing table behavior.",
                _ => "DataGrid behavior editing is available for DataGrid controls.",
            };
            return false;
        }

        if (!DesignerDataGridBehaviorRuntime.TryRead(
                target.Visual,
                out var values,
                out var error))
        {
            state = default!;
            StatusText = $"DataGrid behavior cannot be edited. {error}";
            return false;
        }

        state = CreateDataGridBehaviorEditorState(target.DisplayName, values);
        return true;
    }

    public bool SetSelectedDataGridBehaviorProperties(
        DesignerDataGridBehaviorEditorInput input)
    {
        var target = Canvas.SelectedElement;
        if (target is null
            || target.IsLocked
            || target.Visual is not DataGrid dataGrid)
        {
            StatusText = "Select an unlocked DataGrid before editing table behavior.";
            return false;
        }

        if (!DesignerDataGridBehaviorRuntime.TryRead(dataGrid, out var current, out var error))
        {
            StatusText = $"DataGrid behavior was not changed. {error}";
            return false;
        }

        if (!DesignerDataGridBehaviorRuntime.TryParseValues(
                dataGrid,
                input,
                out var values,
                out error))
        {
            StatusText = $"DataGrid behavior was not changed. {error}";
            return false;
        }

        if (current == values)
        {
            StatusText = "DataGrid behavior is unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated DataGrid behavior.");
        DesignerDataGridBehaviorRuntime.Apply(dataGrid, values);
        foreach (var attribute in DesignerDataGridBehaviorRuntime.GetAxamlAttributes(dataGrid))
        {
            DesignerStyleApplicationMetadata.ClearApplied(dataGrid, attribute.Name);
        }

        Canvas.RefreshDocumentStyles(dataGrid);
        CommitCanvasMutation();
        StatusText = $"Updated table behavior for {target.DisplayName}.";
        return true;
    }

    private static DataGridBehaviorEditorState CreateDataGridBehaviorEditorState(
        string controlName,
        DesignerDataGridBehaviorValues values)
        => new(
            controlName,
            values.AutoGenerateColumns,
            values.IsReadOnly,
            values.CanUserReorderColumns,
            values.CanUserResizeColumns,
            values.CanUserSortColumns,
            values.HeadersVisibility.ToString(),
            values.GridLinesVisibility.ToString(),
            values.SelectionMode.ToString(),
            values.ClipboardCopyMode.ToString(),
            values.AreRowDetailsFrozen,
            values.AreRowGroupHeadersFrozen,
            values.IsScrollInertiaEnabled,
            values.FrozenColumnCount.ToString(CultureInfo.InvariantCulture),
            DesignerDataGridBehaviorRuntime.FormatEditorDouble(values.RowHeight),
            DesignerDataGridBehaviorRuntime.FormatEditorDouble(values.RowHeaderWidth),
            DesignerDataGridBehaviorRuntime.FormatEditorDouble(values.ColumnHeaderHeight),
            DesignerDataGridBehaviorRuntime.FormatEditorDouble(values.MinColumnWidth),
            DesignerDataGridBehaviorRuntime.FormatEditorDouble(values.MaxColumnWidth),
            DesignerDataGridBehaviorRuntime.FormatColumnWidth(values.ColumnWidth),
            values.HorizontalScrollBarVisibility.ToString(),
            values.VerticalScrollBarVisibility.ToString());

    public bool TrySetSelectedImageSource(string source)
    {
        if (!TryGetSelectedImageProperties(out var state))
        {
            return false;
        }

        return SetSelectedImageProperties(new DesignerImageEditorInput(
            source,
            state.Stretch,
            state.StretchDirection,
            state.BitmapInterpolationMode,
            state.EdgeMode,
            state.BitmapBlendingMode));
    }

    public bool TryGetSelectedPathData(out string controlName, out string data)
    {
        if (Canvas.SelectedElement is not
            {
                IsLocked: false,
                Visual: PathShape path,
            } target)
        {
            controlName = string.Empty;
            data = string.Empty;
            StatusText = "Select an unlocked Path control to edit its geometry data.";
            return false;
        }

        controlName = target.DisplayName;
        data = path.Tag is DesignerPathDataMetadata metadata
            ? metadata.Data
            : string.Empty;
        return true;
    }

    public bool SetSelectedPathData(string data)
    {
        if (Canvas.SelectedElement is not
            {
                IsLocked: false,
                Visual: PathShape path,
            } target)
        {
            StatusText = "Select an unlocked Path control to edit its geometry data.";
            return false;
        }

        var normalized = data.Trim();
        var current = path.Tag is DesignerPathDataMetadata metadata
            ? metadata.Data
            : string.Empty;
        if (string.Equals(current, normalized, StringComparison.Ordinal))
        {
            StatusText = "Path data is unchanged.";
            return true;
        }

        Geometry? geometry = null;
        if (normalized.Length > 0)
        {
            try
            {
                geometry = Geometry.Parse(normalized);
            }
            catch (Exception exception) when (
                exception is FormatException or ArgumentException or InvalidDataException)
            {
                StatusText = "Path data must be valid Avalonia geometry mini-language.";
                return false;
            }
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated Path geometry data.");
        path.Data = geometry;
        path.Tag = normalized.Length == 0
            ? null
            : new DesignerPathDataMetadata(normalized);
        CommitCanvasMutation();
        StatusText = normalized.Length == 0
            ? $"Cleared Path data for {target.DisplayName}."
            : $"Updated Path data for {target.DisplayName}.";
        return true;
    }

    public bool TryGetSelectedQuickContent(out QuickContentEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            state = default!;
            StatusText = target is { IsLocked: true }
                ? "Unlock the selected control before quick content editing."
                : "Select a control before quick content editing.";
            return false;
        }

        switch (target.Visual)
        {
            case SelectableTextBlock selectableTextBlock:
                state = new QuickContentEditorState(
                    target.DisplayName,
                    nameof(SelectableTextBlock),
                    "Text",
                    selectableTextBlock.Text ?? string.Empty,
                    true);
                return true;
            case TextBlock textBlock:
                state = new QuickContentEditorState(
                    target.DisplayName,
                    nameof(TextBlock),
                    "Text",
                    textBlock.Text ?? string.Empty,
                    true);
                return true;
            case TextBox textBox:
                state = new QuickContentEditorState(
                    target.DisplayName,
                    target.Visual.GetType().Name,
                    "Text",
                    textBox.Text ?? string.Empty,
                    true);
                return true;
            case AutoCompleteBox autoCompleteBox:
                state = new QuickContentEditorState(
                    target.DisplayName,
                    nameof(AutoCompleteBox),
                    "Text",
                    autoCompleteBox.Text ?? string.Empty,
                    true);
                return true;
            case ToggleButton toggleButton:
                state = new QuickContentEditorState(
                    target.DisplayName,
                    target.Visual.GetType().Name,
                    "Content",
                    toggleButton.Content?.ToString() ?? string.Empty,
                    false);
                return true;
            case Button button when DesignerButtonRuntime.IsSupportedControl(button):
                state = new QuickContentEditorState(
                    target.DisplayName,
                    nameof(Button),
                    "Content",
                    button.Content?.ToString() ?? string.Empty,
                    false);
                return true;
            case Label label:
                state = new QuickContentEditorState(
                    target.DisplayName,
                    nameof(Label),
                    "Content",
                    label.Content?.ToString() ?? string.Empty,
                    true);
                return true;
        }

        if (IsDesignerContentContainer(target.Visual))
        {
            if (GetDesignerContentChild(target) is not null)
            {
                state = default!;
                StatusText = "This container uses a designer child. Select that child to quick-edit its content.";
                return false;
            }

            state = new QuickContentEditorState(
                target.DisplayName,
                target.Visual.GetType().Name,
                "Content",
                target.Visual switch
                {
                    Expander expander => ReadExpanderContent(expander),
                    ScrollViewer scrollViewer => ReadScrollViewerContent(scrollViewer),
                    Border border => ReadBorderContent(border),
                    ContentControl contentControl => ReadContentControlContent(contentControl),
                    _ => string.Empty,
                },
                true);
            return true;
        }

        state = default!;
        StatusText = "Quick content editing is available for text, button, toggle, label, and fallback content controls.";
        return false;
    }

    public bool SetSelectedQuickContent(string content)
    {
        if (!TryGetSelectedQuickContent(out var state))
        {
            return false;
        }

        var target = Canvas.SelectedElement!;
        if (string.Equals(state.Content, content, StringComparison.Ordinal))
        {
            StatusText = "Content is unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, $"Updated {state.PropertyName}.");
        switch (target.Visual)
        {
            case SelectableTextBlock selectableTextBlock:
                selectableTextBlock.Text = content;
                DesignerStyleApplicationMetadata.ClearApplied(selectableTextBlock, "Text");
                break;
            case TextBlock textBlock:
                textBlock.Text = content;
                DesignerStyleApplicationMetadata.ClearApplied(textBlock, "Text");
                break;
            case TextBox textBox:
                textBox.Text = content;
                DesignerStyleApplicationMetadata.ClearApplied(textBox, "Text");
                break;
            case AutoCompleteBox autoCompleteBox:
                autoCompleteBox.Text = content;
                DesignerStyleApplicationMetadata.ClearApplied(autoCompleteBox, "Text");
                break;
            case ToggleButton toggleButton:
                toggleButton.Content = content;
                DesignerStyleApplicationMetadata.ClearApplied(toggleButton, "Content");
                break;
            case Button button when DesignerButtonRuntime.IsSupportedControl(button):
                button.Content = content;
                DesignerStyleApplicationMetadata.ClearApplied(button, "Content");
                break;
            case Label label:
                label.Content = content;
                DesignerStyleApplicationMetadata.ClearApplied(label, "Content");
                break;
            case Expander expander:
                SetExpanderContent(expander, content);
                break;
            case ScrollViewer scrollViewer:
                SetScrollViewerContent(scrollViewer, content);
                break;
            case Border border:
                SetBorderContent(border, content);
                break;
            case ContentControl contentControl:
                SetContentControlContent(contentControl, content);
                break;
        }

        Canvas.RefreshDocumentStyles(target.Visual);
        CommitCanvasMutation();
        StatusText = $"Updated quick content for {target.DisplayName}.";
        return true;
    }

    public bool TryGetSelectedExpanderContent(out string controlName, out string content)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || !IsDesignerContentContainer(target.Visual))
        {
            controlName = string.Empty;
            content = string.Empty;
            StatusText = "Select an unlocked Expander, ContentControl, UserControl, ScrollViewer, or Border to edit its content.";
            return false;
        }

        if (GetDesignerContentChild(target) is not null)
        {
            controlName = target.DisplayName;
            content = string.Empty;
            StatusText = "This container uses a designer child. Remove it from the container before editing fallback text content.";
            return false;
        }

        controlName = target.DisplayName;
        content = target.Visual switch
        {
            Expander expander => ReadExpanderContent(expander),
            ScrollViewer scrollViewer => ReadScrollViewerContent(scrollViewer),
            Border border => ReadBorderContent(border),
            ContentControl contentControl => ReadContentControlContent(contentControl),
            _ => string.Empty,
        };
        return true;
    }

    public void SetSelectedExpanderContent(string content)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || !IsDesignerContentContainer(target.Visual))
        {
            StatusText = "Select an unlocked Expander, ContentControl, UserControl, ScrollViewer, or Border to edit its content.";
            return;
        }

        if (GetDesignerContentChild(target) is not null)
        {
            StatusText = "This container uses a designer child. Remove it from the container before editing fallback text content.";
            return;
        }

        var currentContent = target.Visual switch
        {
            Expander expander => ReadExpanderContent(expander),
            ScrollViewer scrollViewer => ReadScrollViewerContent(scrollViewer),
            Border border => ReadBorderContent(border),
            ContentControl contentControl => ReadContentControlContent(contentControl),
            _ => string.Empty,
        };
        if (string.Equals(currentContent, content, StringComparison.Ordinal))
        {
            StatusText = "Content is unchanged.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated container content.");
        if (target.Visual is Expander targetExpander)
        {
            SetExpanderContent(targetExpander, content);
        }
        else if (target.Visual is ContentControl targetContentControl
            && (target.Visual.GetType() == typeof(ContentControl) || target.Visual is UserControl))
        {
            SetContentControlContent(targetContentControl, content);
        }
        else if (target.Visual is ScrollViewer targetScrollViewer)
        {
            SetScrollViewerContent(targetScrollViewer, content);
        }
        else
        {
            SetBorderContent((Border)target.Visual, content);
        }
        CommitCanvasMutation();
        StatusText = $"Updated content for {target.DisplayName}.";
    }

    public bool TryGetSelectedAccessibilityProperties(out AccessibilityEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            state = new AccessibilityEditorState(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                AccessibilityView.Default.ToString(),
                "0",
                AutomationLiveSetting.Off.ToString(),
                false,
                int.MaxValue.ToString(CultureInfo.InvariantCulture),
                true,
                true);
            StatusText = target is { IsLocked: true }
                ? "Unlock the selected control before editing accessibility and navigation."
                : "Select a control before editing accessibility and navigation.";
            return false;
        }

        state = CreateAccessibilityEditorState(
            target.DisplayName,
            DesignerAccessibilityRuntime.Read(target.Visual));
        return true;
    }

    public bool SetSelectedAccessibilityProperties(
        string toolTip,
        string accessibleName,
        string automationId,
        string helpText,
        string accessibilityView,
        string headingLevel,
        string liveSetting,
        bool isRequiredForForm,
        string tabIndex,
        bool isTabStop,
        bool focusable)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            StatusText = "Select an unlocked control before editing accessibility and navigation.";
            return false;
        }

        if (!DesignerAccessibilityRuntime.TryParseValues(
                toolTip,
                accessibleName,
                automationId,
                helpText,
                accessibilityView,
                headingLevel,
                liveSetting,
                isRequiredForForm,
                tabIndex,
                isTabStop,
                focusable,
                out var values,
                out var error))
        {
            StatusText = $"Accessibility properties were not changed. {error}";
            return false;
        }

        if (DesignerAccessibilityRuntime.Read(target.Visual) == values)
        {
            StatusText = "Accessibility and navigation properties are unchanged.";
            return true;
        }

        BeginCanvasMutation(
            HistoryActionType.EditProperty,
            "Updated accessibility and navigation properties.");
        DesignerAccessibilityRuntime.Apply(target.Visual, values);
        CommitCanvasMutation();
        StatusText = $"Updated accessibility and navigation for {target.DisplayName}.";
        return true;
    }

    private static AccessibilityEditorState CreateAccessibilityEditorState(
        string controlName,
        DesignerAccessibilityValues values)
        => new(
            controlName,
            values.ToolTip,
            values.AccessibleName,
            values.AutomationId,
            values.HelpText,
            values.AccessibilityView.ToString(),
            values.HeadingLevel.ToString(CultureInfo.InvariantCulture),
            values.LiveSetting.ToString(),
            values.IsRequiredForForm,
            values.TabIndex.ToString(CultureInfo.InvariantCulture),
            values.IsTabStop,
            values.Focusable);

    public bool TryGetSelectedInteractionProperties(out InteractionEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            state = new InteractionEditorState(
                string.Empty,
                "1",
                true,
                true,
                true,
                false,
                true,
                FlowDirection.LeftToRight.ToString(),
                "Default");
            StatusText = target is { IsLocked: true }
                ? "Unlock the selected control before editing interaction and rendering."
                : "Select a control before editing interaction and rendering.";
            return false;
        }

        if (!DesignerInteractionRuntime.TryRead(target.Visual, out var values, out var error))
        {
            state = new InteractionEditorState(
                target.DisplayName,
                "1",
                true,
                true,
                true,
                target.Visual.ClipToBounds,
                true,
                FlowDirection.LeftToRight.ToString(),
                "Default");
            StatusText = $"Interaction properties cannot be edited. {error}";
            return false;
        }

        state = CreateInteractionEditorState(target.DisplayName, values);
        return true;
    }

    public bool SetSelectedInteractionProperties(
        string opacity,
        bool isEnabled,
        bool isVisible,
        bool isHitTestVisible,
        bool clipToBounds,
        bool useLayoutRounding,
        string flowDirection,
        string cursor)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            StatusText = "Select an unlocked control before editing interaction and rendering.";
            return false;
        }

        if (!DesignerInteractionRuntime.TryParseValues(
                opacity,
                isEnabled,
                isVisible,
                isHitTestVisible,
                clipToBounds,
                useLayoutRounding,
                flowDirection,
                cursor,
                out var values,
                out var error))
        {
            StatusText = $"Interaction properties were not changed. {error}";
            return false;
        }

        if (!DesignerInteractionRuntime.TryRead(target.Visual, out var current, out error))
        {
            StatusText = $"Interaction properties were not changed. {error}";
            return false;
        }

        if (current == values)
        {
            StatusText = "Interaction and rendering properties are unchanged.";
            return true;
        }

        BeginCanvasMutation(
            HistoryActionType.EditProperty,
            "Updated interaction and rendering properties.");
        DesignerInteractionRuntime.Apply(target.Visual, values);
        DesignerStyleApplicationMetadata.ClearApplied(target.Visual, "Opacity");
        Canvas.RefreshDocumentStyles(target.Visual);
        CommitCanvasMutation();
        StatusText = $"Updated interaction and rendering for {target.DisplayName}.";
        return true;
    }

    private static InteractionEditorState CreateInteractionEditorState(
        string controlName,
        DesignerInteractionValues values)
        => new(
            controlName,
            values.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            values.IsEnabled,
            values.IsVisible,
            values.IsHitTestVisible,
            values.ClipToBounds,
            values.UseLayoutRounding,
            values.FlowDirection.ToString(),
            values.Cursor);

    public bool TryGetSelectedEffectProperties(out EffectEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            state = CreateEffectEditorState(string.Empty, DesignerEffectRuntime.DefaultValues);
            StatusText = target is { IsLocked: true }
                ? "Unlock the selected control before editing visual effects."
                : "Select a control before editing visual effects.";
            return false;
        }

        if (!DesignerEffectRuntime.TryRead(target.Visual, out var values, out var error))
        {
            state = CreateEffectEditorState(target.DisplayName, DesignerEffectRuntime.DefaultValues);
            StatusText = $"Visual effects cannot be edited. {error}";
            return false;
        }

        state = CreateEffectEditorState(target.DisplayName, values);
        return true;
    }

    public bool SetSelectedEffectProperties(
        string kind,
        string blurRadius,
        string offsetX,
        string offsetY,
        string shadowBlurRadius,
        string shadowColor,
        string shadowOpacity)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            StatusText = "Select an unlocked control before editing visual effects.";
            return false;
        }

        if (!DesignerEffectRuntime.TryParseValues(
                kind,
                blurRadius,
                offsetX,
                offsetY,
                shadowBlurRadius,
                shadowColor,
                shadowOpacity,
                out var values,
                out var error))
        {
            StatusText = $"Visual effects were not changed. {error}";
            return false;
        }

        if (!DesignerEffectRuntime.TryRead(target.Visual, out var current, out error))
        {
            StatusText = $"Visual effects were not changed. {error}";
            return false;
        }

        if (current == values)
        {
            StatusText = "Visual effects are unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated visual effects.");
        DesignerEffectRuntime.Apply(target.Visual, values);
        Canvas.RefreshDocumentStyles(target.Visual);
        CommitCanvasMutation();
        StatusText = $"Updated visual effects for {target.DisplayName}.";
        return true;
    }

    private static EffectEditorState CreateEffectEditorState(
        string controlName,
        DesignerEffectValues values)
        => new(
            controlName,
            DesignerEffectRuntime.GetDisplayKind(values.Kind),
            values.BlurRadius.ToString("0.###", CultureInfo.InvariantCulture),
            values.OffsetX.ToString("0.###", CultureInfo.InvariantCulture),
            values.OffsetY.ToString("0.###", CultureInfo.InvariantCulture),
            values.ShadowBlurRadius.ToString("0.###", CultureInfo.InvariantCulture),
            values.ShadowColor,
            values.ShadowOpacity.ToString("0.###", CultureInfo.InvariantCulture));

    public bool TryGetSelectedRangeProperties(out RangeEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || !DesignerRangeRuntime.IsSupportedControl(target.Visual))
        {
            state = default!;
            StatusText = target switch
            {
                null => "Select a Slider, ProgressBar, or NumericUpDown before editing range and value.",
                { IsLocked: true } => "Unlock the selected control before editing range and value.",
                _ => "Range and value editing is available for Slider, ProgressBar, and NumericUpDown.",
            };
            return false;
        }

        if (!DesignerRangeRuntime.TryRead(target.Visual, out var values, out var error))
        {
            state = default!;
            StatusText = $"Range and value properties cannot be edited. {error}";
            return false;
        }

        state = CreateRangeEditorState(target.DisplayName, values);
        return true;
    }

    public bool SetSelectedRangeProperties(DesignerRangeEditorInput input)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || !DesignerRangeRuntime.IsSupportedControl(target.Visual))
        {
            StatusText = "Select an unlocked Slider, ProgressBar, or NumericUpDown before editing range and value.";
            return false;
        }

        if (!DesignerRangeRuntime.TryParseValues(target.Visual, input, out var values, out var error))
        {
            StatusText = $"Range and value properties were not changed. {error}";
            return false;
        }

        if (!DesignerRangeRuntime.TryRead(target.Visual, out var current, out error))
        {
            StatusText = $"Range and value properties were not changed. {error}";
            return false;
        }

        if (current == values)
        {
            StatusText = "Range and value properties are unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated range and value properties.");
        DesignerRangeRuntime.Apply(target.Visual, values);
        foreach (var attribute in DesignerRangeRuntime.GetAxamlAttributes(target.Visual))
        {
            DesignerStyleApplicationMetadata.ClearApplied(target.Visual, attribute.Name);
        }

        Canvas.RefreshDocumentStyles(target.Visual);
        CommitCanvasMutation();
        StatusText = $"Updated range and value properties for {target.DisplayName}.";
        return true;
    }

    private static RangeEditorState CreateRangeEditorState(
        string controlName,
        DesignerRangeValues values)
    {
        var isNumeric = values.Kind == DesignerRangeControlKind.NumericUpDown;
        return new RangeEditorState(
            controlName,
            values.Kind.ToString(),
            isNumeric ? FormatRangeValue(values.NumericMinimum) : FormatRangeValue(values.Minimum),
            isNumeric ? FormatRangeValue(values.NumericMaximum) : FormatRangeValue(values.Maximum),
            isNumeric
                ? values.NumericValue is { } numericValue ? FormatRangeValue(numericValue) : string.Empty
                : FormatRangeValue(values.Value),
            FormatRangeValue(values.SmallChange),
            FormatRangeValue(values.LargeChange),
            values.Orientation.ToString(),
            values.IsDirectionReversed,
            FormatRangeValue(values.TickFrequency),
            values.TickPlacement.ToString(),
            values.IsSnapToTickEnabled,
            values.IsIndeterminate,
            values.ShowProgressText,
            values.ProgressTextFormat,
            FormatRangeValue(values.Increment),
            values.FormatString,
            values.ClipValueToMinMax,
            values.AllowSpin,
            values.ShowButtonSpinner,
            values.ButtonSpinnerLocation.ToString());
    }

    private static string FormatRangeValue(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string FormatRangeValue(decimal value)
        => value.ToString(CultureInfo.InvariantCulture);

    public bool TryGetSelectedTextInputProperties(out TextInputEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || target.Visual is not TextBox)
        {
            state = default!;
            StatusText = target switch
            {
                null => "Select a TextBox before editing text input properties.",
                { IsLocked: true } => "Unlock the selected TextBox before editing text input properties.",
                _ => "Text input editing is available for TextBox controls.",
            };
            return false;
        }

        if (!DesignerTextInputRuntime.TryRead(target.Visual, out var values, out var error))
        {
            state = default!;
            StatusText = $"Text input properties cannot be edited. {error}";
            return false;
        }

        state = CreateTextInputEditorState(target.DisplayName, values);
        return true;
    }

    public bool SetSelectedTextInputProperties(DesignerTextInputEditorInput input)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || target.Visual is not TextBox)
        {
            StatusText = "Select an unlocked TextBox before editing text input properties.";
            return false;
        }

        if (!DesignerTextInputRuntime.TryParseValues(input, out var values, out var error))
        {
            StatusText = $"Text input properties were not changed. {error}";
            return false;
        }

        if (!DesignerTextInputRuntime.TryRead(target.Visual, out var current, out error))
        {
            StatusText = $"Text input properties were not changed. {error}";
            return false;
        }

        if (current == values)
        {
            StatusText = "Text input properties are unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated text input properties.");
        DesignerTextInputRuntime.Apply(target.Visual, values);
        DesignerStyleApplicationMetadata.ClearApplied(target.Visual, "Text");
        foreach (var attribute in DesignerTextInputRuntime.GetAxamlAttributes(target.Visual))
        {
            DesignerStyleApplicationMetadata.ClearApplied(target.Visual, attribute.Name);
        }

        Canvas.RefreshDocumentStyles(target.Visual);
        CommitCanvasMutation();
        StatusText = $"Updated text input properties for {target.DisplayName}.";
        return true;
    }

    private static TextInputEditorState CreateTextInputEditorState(
        string controlName,
        DesignerTextInputValues values)
        => new(
            controlName,
            values.Text,
            values.Watermark,
            values.AcceptsReturn,
            values.AcceptsTab,
            values.TextWrapping.ToString(),
            values.TextAlignment.ToString(),
            values.IsReadOnly,
            values.MaxLength.ToString(CultureInfo.InvariantCulture),
            values.MinLines.ToString(CultureInfo.InvariantCulture),
            values.MaxLines.ToString(CultureInfo.InvariantCulture),
            values.PasswordChar,
            values.RevealPassword,
            values.UseFloatingWatermark,
            values.IsUndoEnabled,
            values.UndoLimit.ToString(CultureInfo.InvariantCulture),
            values.ClearSelectionOnLostFocus,
            values.IsInactiveSelectionHighlightEnabled);

    public bool TryGetSelectedMaskedTextBoxProperties(out MaskedTextBoxEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || !DesignerMaskedTextBoxRuntime.IsSupportedControl(target.Visual))
        {
            state = default!;
            StatusText = target switch
            {
                null => "Select a MaskedTextBox before editing mask behavior.",
                { IsLocked: true } => "Unlock the selected MaskedTextBox before editing mask behavior.",
                _ => "Mask editing is available for MaskedTextBox controls.",
            };
            return false;
        }

        if (!DesignerMaskedTextBoxRuntime.TryRead(target.Visual, out var values, out var error))
        {
            state = default!;
            StatusText = $"MaskedTextBox cannot be edited. {error}";
            return false;
        }

        state = new MaskedTextBoxEditorState(
            target.DisplayName,
            values.Mask,
            values.PromptChar.ToString(),
            values.HidePromptOnLeave);
        return true;
    }

    public bool SetSelectedMaskedTextBoxProperties(DesignerMaskedTextBoxEditorInput input)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || !DesignerMaskedTextBoxRuntime.IsSupportedControl(target.Visual))
        {
            StatusText = "Select an unlocked MaskedTextBox before editing mask behavior.";
            return false;
        }

        if (!DesignerMaskedTextBoxRuntime.TryRead(target.Visual, out var current, out var error))
        {
            StatusText = $"MaskedTextBox was not changed. {error}";
            return false;
        }

        if (!DesignerMaskedTextBoxRuntime.TryParseValues(input, current, out var values, out error))
        {
            StatusText = $"MaskedTextBox was not changed. {error}";
            return false;
        }

        if (current == values)
        {
            StatusText = "MaskedTextBox mask behavior is unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated MaskedTextBox mask behavior.");
        DesignerMaskedTextBoxRuntime.Apply((MaskedTextBox)target.Visual, values);
        foreach (var attribute in DesignerMaskedTextBoxRuntime.GetAxamlAttributes(target.Visual))
        {
            DesignerStyleApplicationMetadata.ClearApplied(target.Visual, attribute.Name);
        }

        Canvas.RefreshDocumentStyles(target.Visual);
        CommitCanvasMutation();
        StatusText = $"Updated mask behavior for {target.DisplayName}.";
        return true;
    }

    public bool TryGetSelectedSelectableTextBlockProperties(
        out SelectableTextBlockEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null
            || target.IsLocked
            || !DesignerSelectableTextBlockRuntime.IsSupportedControl(target.Visual))
        {
            state = default!;
            StatusText = target switch
            {
                null => "Select a SelectableTextBlock before editing selection styling.",
                { IsLocked: true } => "Unlock the selected SelectableTextBlock before editing selection styling.",
                _ => "Selection styling editing is available for SelectableTextBlock controls.",
            };
            return false;
        }

        if (!DesignerSelectableTextBlockRuntime.TryRead(target.Visual, out var values, out var error))
        {
            state = default!;
            StatusText = $"SelectableTextBlock cannot be edited. {error}";
            return false;
        }

        state = new SelectableTextBlockEditorState(
            target.DisplayName,
            values.Text,
            values.SelectionBrush,
            values.SelectionForegroundBrush);
        return true;
    }

    public bool SetSelectedSelectableTextBlockProperties(
        DesignerSelectableTextBlockEditorInput input)
    {
        var target = Canvas.SelectedElement;
        if (target is null
            || target.IsLocked
            || !DesignerSelectableTextBlockRuntime.IsSupportedControl(target.Visual))
        {
            StatusText = "Select an unlocked SelectableTextBlock before editing selection styling.";
            return false;
        }

        if (!DesignerSelectableTextBlockRuntime.TryRead(target.Visual, out var current, out var error))
        {
            StatusText = $"SelectableTextBlock was not changed. {error}";
            return false;
        }

        if (!DesignerSelectableTextBlockRuntime.TryParseValues(
                target.Visual,
                input,
                out var values,
                out error))
        {
            StatusText = $"SelectableTextBlock was not changed. {error}";
            return false;
        }

        if (current == values)
        {
            StatusText = "SelectableTextBlock selection styling is unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated SelectableTextBlock selection styling.");
        DesignerSelectableTextBlockRuntime.Apply((SelectableTextBlock)target.Visual, values);
        DesignerStyleApplicationMetadata.ClearApplied(target.Visual, "Text");
        foreach (var attribute in DesignerSelectableTextBlockRuntime.GetAxamlAttributes(target.Visual))
        {
            DesignerStyleApplicationMetadata.ClearApplied(target.Visual, attribute.Name);
        }

        Canvas.RefreshDocumentStyles(target.Visual);
        CommitCanvasMutation();
        StatusText = $"Updated selection styling for {target.DisplayName}.";
        return true;
    }

    public bool TryGetSelectedSplitViewProperties(out SplitViewEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null
            || target.IsLocked
            || !DesignerSplitViewRuntime.IsSupportedControl(target.Visual))
        {
            state = default!;
            StatusText = target switch
            {
                null => "Select a SplitView before editing pane behavior.",
                { IsLocked: true } => "Unlock the selected SplitView before editing pane behavior.",
                _ => "Pane behavior editing is available for SplitView controls.",
            };
            return false;
        }

        if (!DesignerSplitViewRuntime.TryRead(target.Visual, out var values, out var error))
        {
            state = default!;
            StatusText = $"SplitView cannot be edited. {error}";
            return false;
        }

        state = CreateSplitViewEditorState(target.DisplayName, values);
        return true;
    }

    public bool SetSelectedSplitViewProperties(DesignerSplitViewEditorInput input)
    {
        var target = Canvas.SelectedElement;
        if (target is null
            || target.IsLocked
            || target.Visual is not SplitView splitView)
        {
            StatusText = "Select an unlocked SplitView before editing pane behavior.";
            return false;
        }

        if (!DesignerSplitViewRuntime.TryRead(splitView, out var current, out var error))
        {
            StatusText = $"SplitView was not changed. {error}";
            return false;
        }

        if (!DesignerSplitViewRuntime.TryParseValues(
                splitView,
                input,
                out var values,
                out error))
        {
            StatusText = $"SplitView was not changed. {error}";
            return false;
        }

        if (current == values)
        {
            StatusText = "SplitView pane behavior is unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated SplitView pane behavior.");
        DesignerSplitViewRuntime.Apply(splitView, values);
        DesignerResourceReferenceMetadata.SetReference(splitView, "PaneBackground", null);
        Canvas.ReflowContainerChildren(target);
        foreach (var attribute in DesignerSplitViewRuntime.GetAxamlAttributes(splitView))
        {
            DesignerStyleApplicationMetadata.ClearApplied(splitView, attribute.Name);
        }

        DesignerStyleApplicationMetadata.ClearApplied(splitView, "PaneBackground");
        Canvas.RefreshDocumentStyles(splitView);
        CommitCanvasMutation();
        StatusText = $"Updated pane behavior for {target.DisplayName}.";
        return true;
    }

    private static SplitViewEditorState CreateSplitViewEditorState(
        string controlName,
        DesignerSplitViewValues values)
        => new(
            controlName,
            values.DisplayMode.ToString(),
            values.IsPaneOpen,
            values.OpenPaneLength.ToString("0.###", CultureInfo.InvariantCulture),
            values.CompactPaneLength.ToString("0.###", CultureInfo.InvariantCulture),
            values.PanePlacement.ToString(),
            values.UseLightDismissOverlayMode,
            values.PaneBackground);

    public bool TryGetSelectedTabControlBehaviorProperties(
        out TabControlBehaviorEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null
            || target.IsLocked
            || !DesignerTabControlRuntime.IsSupportedControl(target.Visual))
        {
            state = default!;
            StatusText = target switch
            {
                null => "Select a TabControl before editing tab behavior.",
                { IsLocked: true } => "Unlock the selected TabControl before editing tab behavior.",
                _ => "Tab behavior editing is available for TabControl controls.",
            };
            return false;
        }

        if (!DesignerTabControlRuntime.TryRead(
                target.Visual,
                out var values,
                out var error))
        {
            state = default!;
            StatusText = $"TabControl behavior cannot be edited. {error}";
            return false;
        }

        state = CreateTabControlBehaviorEditorState(target.DisplayName, values);
        return true;
    }

    public bool SetSelectedTabControlBehaviorProperties(
        DesignerTabControlEditorInput input)
    {
        var target = Canvas.SelectedElement;
        if (target is null
            || target.IsLocked
            || target.Visual is not TabControl tabControl)
        {
            StatusText = "Select an unlocked TabControl before editing tab behavior.";
            return false;
        }

        if (!DesignerTabControlRuntime.TryRead(tabControl, out var current, out var error))
        {
            StatusText = $"TabControl behavior was not changed. {error}";
            return false;
        }

        if (!DesignerTabControlRuntime.TryParseValues(
                tabControl,
                input,
                out var values,
                out error))
        {
            StatusText = $"TabControl behavior was not changed. {error}";
            return false;
        }

        if (current == values)
        {
            StatusText = "TabControl behavior is unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated TabControl behavior.");
        DesignerTabControlRuntime.Apply(tabControl, values);
        Canvas.ReflowContainerChildren(target);
        foreach (var attribute in DesignerTabControlRuntime.GetAxamlAttributes(tabControl))
        {
            DesignerStyleApplicationMetadata.ClearApplied(tabControl, attribute.Name);
        }

        Canvas.RefreshDocumentStyles(tabControl);
        CommitCanvasMutation();
        StatusText = $"Updated tab behavior for {target.DisplayName}.";
        return true;
    }

    private static TabControlBehaviorEditorState CreateTabControlBehaviorEditorState(
        string controlName,
        DesignerTabControlValues values)
        => new(
            controlName,
            values.TabStripPlacement.ToString(),
            values.HorizontalContentAlignment.ToString(),
            values.VerticalContentAlignment.ToString());

    public bool TryGetSelectedSelectionProperties(out SelectionEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || !DesignerSelectionRuntime.IsSupportedControl(target.Visual))
        {
            state = default!;
            StatusText = target switch
            {
                null => "Select a ComboBox, ListBox, or TreeView before editing selection behavior.",
                { IsLocked: true } => "Unlock the selected control before editing selection behavior.",
                _ => "Selection behavior editing is available for ComboBox, ListBox, and TreeView controls.",
            };
            return false;
        }

        if (!DesignerSelectionRuntime.TryRead(target.Visual, out var values, out var error))
        {
            state = default!;
            StatusText = $"Selection behavior cannot be edited. {error}";
            return false;
        }

        state = CreateSelectionEditorState(target.DisplayName, values);
        return true;
    }

    public bool SetSelectedSelectionProperties(DesignerSelectionEditorInput input)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || !DesignerSelectionRuntime.IsSupportedControl(target.Visual))
        {
            StatusText = "Select an unlocked ComboBox, ListBox, or TreeView before editing selection behavior.";
            return false;
        }

        if (!DesignerSelectionRuntime.TryParseValues(target.Visual, input, out var values, out var error))
        {
            StatusText = $"Selection behavior was not changed. {error}";
            return false;
        }

        if (!DesignerSelectionRuntime.TryRead(target.Visual, out var current, out error))
        {
            StatusText = $"Selection behavior was not changed. {error}";
            return false;
        }

        if (current == values)
        {
            StatusText = "Selection behavior is unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated selection behavior.");
        DesignerSelectionRuntime.Apply(target.Visual, values);
        DesignerStyleApplicationMetadata.ClearApplied(target.Visual, "Text");
        foreach (var attribute in DesignerSelectionRuntime.GetAxamlAttributes(target.Visual))
        {
            DesignerStyleApplicationMetadata.ClearApplied(target.Visual, attribute.Name);
        }

        Canvas.RefreshDocumentStyles(target.Visual);
        CommitCanvasMutation();
        StatusText = $"Updated selection behavior for {target.DisplayName}.";
        return true;
    }

    private static SelectionEditorState CreateSelectionEditorState(
        string controlName,
        DesignerSelectionValues values)
        => new(
            controlName,
            values.Kind.ToString(),
            values.SelectedIndex.ToString(CultureInfo.InvariantCulture),
            values.IsTextSearchEnabled,
            values.AutoScrollToSelectedItem,
            values.WrapSelection,
            DesignerSelectionRuntime.HasFlag(values.SelectionMode, SelectionMode.Multiple),
            DesignerSelectionRuntime.HasFlag(values.SelectionMode, SelectionMode.Toggle),
            DesignerSelectionRuntime.HasFlag(values.SelectionMode, SelectionMode.AlwaysSelected),
            values.IsEditable,
            values.Text,
            values.PlaceholderText,
            values.MaxDropDownHeight.ToString("0.###", CultureInfo.InvariantCulture),
            values.HorizontalContentAlignment.ToString(),
            values.VerticalContentAlignment.ToString());

    public bool TryGetSelectedDateTimeProperties(out DateTimeEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || !DesignerDateTimeRuntime.IsSupportedControl(target.Visual))
        {
            state = default!;
            StatusText = target switch
            {
                null => "Select a DatePicker, CalendarDatePicker, Calendar, or TimePicker before editing date and time input.",
                { IsLocked: true } => "Unlock the selected control before editing date and time input.",
                _ => "Date and time editing is available for DatePicker, CalendarDatePicker, Calendar, and TimePicker controls.",
            };
            return false;
        }

        if (!DesignerDateTimeRuntime.TryRead(target.Visual, out var values, out var error))
        {
            state = default!;
            StatusText = $"Date and time input cannot be edited. {error}";
            return false;
        }

        state = CreateDateTimeEditorState(target.DisplayName, values);
        return true;
    }

    public bool SetSelectedDateTimeProperties(DesignerDateTimeEditorInput input)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || !DesignerDateTimeRuntime.IsSupportedControl(target.Visual))
        {
            StatusText = "Select an unlocked DatePicker, CalendarDatePicker, Calendar, or TimePicker before editing date and time input.";
            return false;
        }

        if (!DesignerDateTimeRuntime.TryParseValues(target.Visual, input, out var values, out var error))
        {
            StatusText = $"Date and time input was not changed. {error}";
            return false;
        }

        if (!DesignerDateTimeRuntime.TryRead(target.Visual, out var current, out error))
        {
            StatusText = $"Date and time input was not changed. {error}";
            return false;
        }

        if (current == values)
        {
            StatusText = "Date and time input is unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated date and time input.");
        DesignerDateTimeRuntime.Apply(target.Visual, values);
        foreach (var propertyName in new[]
                 {
                     "SelectedDate",
                     "MinYear",
                     "MaxYear",
                     "DayVisible",
                     "MonthVisible",
                     "YearVisible",
                     "DayFormat",
                     "MonthFormat",
                     "YearFormat",
                     "DisplayDate",
                     "DisplayDateStart",
                     "DisplayDateEnd",
                     "FirstDayOfWeek",
                     "IsTodayHighlighted",
                     "SelectedDateFormat",
                     "CustomDateFormatString",
                     "Watermark",
                     "UseFloatingWatermark",
                     "HorizontalContentAlignment",
                     "VerticalContentAlignment",
                     "SelectedTime",
                     "MinuteIncrement",
                     "SecondIncrement",
                     "ClockIdentifier",
                     "UseSeconds",
                     "SelectionMode",
                     "DisplayMode",
                     "AllowTapRangeSelection",
                 })
        {
            DesignerStyleApplicationMetadata.ClearApplied(target.Visual, propertyName);
        }

        Canvas.RefreshDocumentStyles(target.Visual);
        CommitCanvasMutation();
        StatusText = $"Updated date and time input for {target.DisplayName}.";
        return true;
    }

    public bool TryGetSelectedColorPickerProperties(out ColorPickerEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || !DesignerColorPickerRuntime.IsSupportedControl(target.Visual))
        {
            state = default!;
            StatusText = target switch
            {
                null => "Select a ColorPicker before editing color picker behavior.",
                { IsLocked: true } => "Unlock the selected control before editing color picker behavior.",
                _ => "Color picker editing is available for ColorPicker controls.",
            };
            return false;
        }

        if (!DesignerColorPickerRuntime.TryRead(target.Visual, out var values, out var error))
        {
            state = default!;
            StatusText = $"Color picker cannot be edited. {error}";
            return false;
        }

        state = CreateColorPickerEditorState(target.DisplayName, values);
        return true;
    }

    public bool SetSelectedColorPickerProperties(DesignerColorPickerEditorInput input)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || !DesignerColorPickerRuntime.IsSupportedControl(target.Visual))
        {
            StatusText = "Select an unlocked ColorPicker before editing color picker behavior.";
            return false;
        }

        if (!DesignerColorPickerRuntime.TryRead(target.Visual, out var current, out var error))
        {
            StatusText = $"Color picker was not changed. {error}";
            return false;
        }

        if (!DesignerColorPickerRuntime.TryParseValues(input, current, out var values, out error))
        {
            StatusText = $"Color picker was not changed. {error}";
            return false;
        }

        if (current == values)
        {
            StatusText = "Color picker is unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated color picker behavior.");
        DesignerColorPickerRuntime.Apply((Avalonia.Controls.ColorPicker)target.Visual, values);
        foreach (var propertyName in new[]
                 {
                     "Color",
                     "ColorModel",
                     "ColorSpectrumComponents",
                     "ColorSpectrumShape",
                     "HexInputAlphaPosition",
                     "IsAccentColorsVisible",
                     "IsAlphaEnabled",
                     "IsAlphaVisible",
                     "IsColorComponentsVisible",
                     "IsColorModelVisible",
                     "IsColorPaletteVisible",
                     "IsColorPreviewVisible",
                     "IsColorSpectrumVisible",
                     "IsColorSpectrumSliderVisible",
                     "IsComponentSliderVisible",
                     "IsComponentTextInputVisible",
                     "IsHexInputVisible",
                     "PaletteColumnCount",
                 })
        {
            DesignerStyleApplicationMetadata.ClearApplied(target.Visual, propertyName);
        }

        Canvas.RefreshDocumentStyles(target.Visual);
        CommitCanvasMutation();
        StatusText = $"Updated color picker behavior for {target.DisplayName}.";
        return true;
    }

    public bool TryGetSelectedAutoCompleteBoxProperties(out AutoCompleteBoxEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || !DesignerAutoCompleteBoxRuntime.IsSupportedControl(target.Visual))
        {
            state = default!;
            StatusText = target switch
            {
                null => "Select an AutoCompleteBox before editing autocomplete behavior.",
                { IsLocked: true } => "Unlock the selected AutoCompleteBox before editing autocomplete behavior.",
                _ => "Autocomplete editing is available for AutoCompleteBox controls.",
            };
            return false;
        }

        if (!DesignerAutoCompleteBoxRuntime.TryRead(target.Visual, out var values, out var error))
        {
            state = default!;
            StatusText = $"AutoCompleteBox cannot be edited. {error}";
            return false;
        }

        state = new AutoCompleteBoxEditorState(
            target.DisplayName,
            values.Text,
            values.Watermark,
            values.IsTextCompletionEnabled,
            values.MinimumPrefixLength.ToString(CultureInfo.InvariantCulture),
            values.MinimumPopulateDelay.ToString("c", CultureInfo.InvariantCulture),
            values.FilterMode.ToString(),
            double.IsPositiveInfinity(values.MaxDropDownHeight)
                ? "Infinity"
                : values.MaxDropDownHeight.ToString("0.###", CultureInfo.InvariantCulture),
            values.IsDropDownOpen);
        return true;
    }

    public bool SetSelectedAutoCompleteBoxProperties(DesignerAutoCompleteBoxEditorInput input)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || !DesignerAutoCompleteBoxRuntime.IsSupportedControl(target.Visual))
        {
            StatusText = "Select an unlocked AutoCompleteBox before editing autocomplete behavior.";
            return false;
        }

        if (!DesignerAutoCompleteBoxRuntime.TryRead(target.Visual, out var current, out var error))
        {
            StatusText = $"AutoCompleteBox was not changed. {error}";
            return false;
        }

        if (!DesignerAutoCompleteBoxRuntime.TryParseValues(input, current, out var values, out error))
        {
            StatusText = $"AutoCompleteBox was not changed. {error}";
            return false;
        }

        if (current == values)
        {
            StatusText = "AutoCompleteBox is unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated AutoCompleteBox behavior.");
        DesignerAutoCompleteBoxRuntime.Apply((AutoCompleteBox)target.Visual, values);
        foreach (var propertyName in new[]
                 {
                     "Text",
                     "Watermark",
                     "IsTextCompletionEnabled",
                     "MinimumPrefixLength",
                     "MinimumPopulateDelay",
                     "FilterMode",
                     "MaxDropDownHeight",
                     "IsDropDownOpen",
                 })
        {
            DesignerStyleApplicationMetadata.ClearApplied(target.Visual, propertyName);
        }

        Canvas.RefreshDocumentStyles(target.Visual);
        CommitCanvasMutation();
        StatusText = $"Updated autocomplete behavior for {target.DisplayName}.";
        return true;
    }

    private static ColorPickerEditorState CreateColorPickerEditorState(
        string controlName,
        DesignerColorPickerValues values)
        => new(
            controlName,
            FormatColorValue(values.Color),
            values.ColorModel.ToString(),
            values.ColorSpectrumComponents.ToString(),
            values.ColorSpectrumShape.ToString(),
            values.HexInputAlphaPosition.ToString(),
            values.IsAccentColorsVisible,
            values.IsAlphaEnabled,
            values.IsAlphaVisible,
            values.IsColorComponentsVisible,
            values.IsColorModelVisible,
            values.IsColorPaletteVisible,
            values.IsColorPreviewVisible,
            values.IsColorSpectrumVisible,
            values.IsColorSpectrumSliderVisible,
            values.IsComponentSliderVisible,
            values.IsComponentTextInputVisible,
            values.IsHexInputVisible,
            values.PaletteColumnCount.ToString(CultureInfo.InvariantCulture));

    private static DateTimeEditorState CreateDateTimeEditorState(
        string controlName,
        DesignerDateTimeValues values)
        => new(
            controlName,
            values.Kind.ToString(),
            FormatDate(values.Kind is DesignerDateTimeControlKind.CalendarDatePicker
                    or DesignerDateTimeControlKind.Calendar
                ? values.CalendarSelectedDate
                : values.SelectedDate),
            FormatDate(values.MinYear),
            FormatDate(values.MaxYear),
            values.DayVisible,
            values.MonthVisible,
            values.YearVisible,
            values.DayFormat,
            values.MonthFormat,
            values.YearFormat,
            FormatDate(values.DisplayDate),
            FormatDate(values.DisplayDateStart),
            FormatDate(values.DisplayDateEnd),
            values.FirstDayOfWeek.ToString(),
            values.IsTodayHighlighted,
            values.SelectedDateFormat.ToString(),
            values.CustomDateFormatString,
            values.Watermark,
            values.UseFloatingWatermark,
            values.HorizontalContentAlignment.ToString(),
            values.VerticalContentAlignment.ToString(),
            values.SelectedTime?.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture) ?? string.Empty,
            values.MinuteIncrement.ToString(CultureInfo.InvariantCulture),
            values.SecondIncrement.ToString(CultureInfo.InvariantCulture),
            values.ClockIdentifier,
            values.UseSeconds,
            values.CalendarSelectionMode.ToString(),
            values.CalendarDisplayMode.ToString(),
            values.AllowTapRangeSelection);

    private static string FormatDate(DateTimeOffset? value)
        => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string FormatDate(DateTime? value)
        => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    public bool TryGetSelectedToggleProperties(out ToggleEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || !DesignerToggleRuntime.IsSupportedControl(target.Visual))
        {
            state = default!;
            StatusText = target switch
            {
                null => "Select a CheckBox, RadioButton, ToggleSwitch, or ToggleButton before editing toggle behavior.",
                { IsLocked: true } => "Unlock the selected control before editing toggle behavior.",
                _ => "Toggle behavior editing is available for CheckBox, RadioButton, ToggleSwitch, and ToggleButton controls.",
            };
            return false;
        }

        if (!DesignerToggleRuntime.TryRead(target.Visual, out var values, out var error))
        {
            state = default!;
            StatusText = $"Toggle behavior cannot be edited. {error}";
            return false;
        }

        state = CreateToggleEditorState(target.DisplayName, values);
        return true;
    }

    public bool SetSelectedToggleProperties(DesignerToggleEditorInput input)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || !DesignerToggleRuntime.IsSupportedControl(target.Visual))
        {
            StatusText = "Select an unlocked CheckBox, RadioButton, ToggleSwitch, or ToggleButton before editing toggle behavior.";
            return false;
        }

        if (!DesignerToggleRuntime.TryParseValues(target.Visual, input, out var values, out var error))
        {
            StatusText = $"Toggle behavior was not changed. {error}";
            return false;
        }

        if (!DesignerToggleRuntime.TryRead(target.Visual, out var current, out error))
        {
            StatusText = $"Toggle behavior was not changed. {error}";
            return false;
        }

        if (current == values)
        {
            StatusText = "Toggle behavior is unchanged.";
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated toggle behavior.");
        DesignerToggleRuntime.Apply(target.Visual, values);
        foreach (var propertyName in new[]
                 {
                     "Content",
                     "IsChecked",
                     "IsThreeState",
                     "ClickMode",
                     "GroupName",
                     "OnContent",
                     "OffContent",
                     "HorizontalContentAlignment",
                     "VerticalContentAlignment",
                 })
        {
            DesignerStyleApplicationMetadata.ClearApplied(target.Visual, propertyName);
        }

        Canvas.RefreshDocumentStyles(target.Visual);
        CommitCanvasMutation();
        StatusText = $"Updated toggle behavior for {target.DisplayName}.";
        return true;
    }

    private static ToggleEditorState CreateToggleEditorState(
        string controlName,
        DesignerToggleValues values)
        => new(
            controlName,
            values.Kind.ToString(),
            values.Content,
            DesignerToggleRuntime.ToState(values.IsChecked).ToString(),
            values.IsThreeState,
            values.ClickMode.ToString(),
            values.GroupName,
            values.OnContent,
            values.OffContent,
            values.HorizontalContentAlignment.ToString(),
            values.VerticalContentAlignment.ToString());

    public bool TryGetSelectedContainerBehaviorProperties(
        out ContainerBehaviorEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null
            || target.IsLocked
            || !DesignerContainerBehaviorRuntime.IsSupportedControl(target.Visual))
        {
            state = default!;
            StatusText = target switch
            {
                null => "Select an Expander or ScrollViewer before editing container behavior.",
                { IsLocked: true } => "Unlock the selected control before editing container behavior.",
                _ => "Container behavior editing is available for Expander and ScrollViewer controls.",
            };
            return false;
        }

        if (!DesignerContainerBehaviorRuntime.TryRead(
                target.Visual,
                out var values,
                out var error))
        {
            state = default!;
            StatusText = $"Container behavior cannot be edited. {error}";
            return false;
        }

        state = CreateContainerBehaviorEditorState(target.DisplayName, values);
        return true;
    }

    public bool SetSelectedContainerBehaviorProperties(
        DesignerContainerBehaviorEditorInput input)
    {
        var target = Canvas.SelectedElement;
        if (target is null
            || target.IsLocked
            || !DesignerContainerBehaviorRuntime.IsSupportedControl(target.Visual))
        {
            StatusText = "Select an unlocked Expander or ScrollViewer before editing container behavior.";
            return false;
        }

        if (!DesignerContainerBehaviorRuntime.TryParseValues(
                target.Visual,
                input,
                out var values,
                out var error))
        {
            StatusText = $"Container behavior was not changed. {error}";
            return false;
        }

        if (!DesignerContainerBehaviorRuntime.TryRead(
                target.Visual,
                out var current,
                out error))
        {
            StatusText = $"Container behavior was not changed. {error}";
            return false;
        }

        if (current == values)
        {
            StatusText = "Container behavior is unchanged.";
            return true;
        }

        BeginCanvasMutation(
            HistoryActionType.EditProperty,
            "Updated container behavior.");
        DesignerContainerBehaviorRuntime.Apply(target.Visual, values);
        Canvas.ReflowContainerChildren(target);
        foreach (var propertyName in new[]
                 {
                     "Header",
                     "IsExpanded",
                     "ExpandDirection",
                     "HorizontalContentAlignment",
                     "VerticalContentAlignment",
                     "HorizontalScrollBarVisibility",
                     "VerticalScrollBarVisibility",
                     "AllowAutoHide",
                     "IsScrollChainingEnabled",
                     "IsDeferredScrollingEnabled",
                     "BringIntoViewOnFocusChange",
                     "HorizontalSnapPointsType",
                     "VerticalSnapPointsType",
                     "HorizontalSnapPointsAlignment",
                     "VerticalSnapPointsAlignment",
                 })
        {
            DesignerStyleApplicationMetadata.ClearApplied(
                target.Visual,
                propertyName);
        }

        Canvas.RefreshDocumentStyles(target.Visual);
        CommitCanvasMutation();
        StatusText = $"Updated container behavior for {target.DisplayName}.";
        return true;
    }

    private static ContainerBehaviorEditorState CreateContainerBehaviorEditorState(
        string controlName,
        DesignerContainerBehaviorValues values)
        => new(
            controlName,
            values.Kind.ToString(),
            values.Header,
            values.IsExpanded,
            values.ExpandDirection.ToString(),
            values.HorizontalContentAlignment.ToString(),
            values.VerticalContentAlignment.ToString(),
            values.HorizontalScrollBarVisibility.ToString(),
            values.VerticalScrollBarVisibility.ToString(),
            values.AllowAutoHide,
            values.IsScrollChainingEnabled,
            values.IsDeferredScrollingEnabled,
            values.BringIntoViewOnFocusChange,
            values.HorizontalSnapPointsType.ToString(),
            values.VerticalSnapPointsType.ToString(),
            values.HorizontalSnapPointsAlignment.ToString(),
            values.VerticalSnapPointsAlignment.ToString());

    public bool TryGetSelectedImageProperties(out ImageEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || target.Visual is not Image)
        {
            state = default!;
            StatusText = target switch
            {
                null => "Select an Image control before editing image source and rendering.",
                { IsLocked: true } => "Unlock the selected Image control before editing image source and rendering.",
                _ => "Image source and rendering editing is available for Image controls.",
            };
            return false;
        }

        if (!DesignerImageRuntime.TryRead(target.Visual, out var values, out var error))
        {
            state = default!;
            StatusText = $"Image source and rendering cannot be edited. {error}";
            return false;
        }

        state = CreateImageEditorState(target.DisplayName, values);
        return true;
    }

    public bool SetSelectedImageProperties(DesignerImageEditorInput input)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked || target.Visual is not Image image)
        {
            StatusText = "Select an unlocked Image control before editing image source and rendering.";
            return false;
        }

        if (!DesignerImageRuntime.TryParseValues(
                image,
                input,
                out var values,
                out var error))
        {
            StatusText = $"Image source and rendering were not changed. {error}";
            return false;
        }

        if (!DesignerImageRuntime.TryRead(image, out var current, out error))
        {
            StatusText = $"Image source and rendering were not changed. {error}";
            return false;
        }

        if (current == values)
        {
            StatusText = "Image source and rendering are unchanged.";
            return true;
        }

        BeginCanvasMutation(
            HistoryActionType.EditProperty,
            "Updated image source and rendering.");
        if (!DesignerImageRuntime.TryApply(
                image,
                values,
                retainSourceOnFailure: false,
                out error))
        {
            _pendingMutation = null;
            StatusText = $"Image source and rendering were not changed. {error}";
            return false;
        }

        foreach (var propertyName in new[]
                 {
                     "Source",
                     "Stretch",
                     "StretchDirection",
                     "RenderOptions.BitmapInterpolationMode",
                     "RenderOptions.EdgeMode",
                     "RenderOptions.BitmapBlendingMode",
                 })
        {
            DesignerStyleApplicationMetadata.ClearApplied(image, propertyName);
        }

        Canvas.RefreshDocumentStyles(image);
        CommitCanvasMutation();
        StatusText = $"Updated image source and rendering for {target.DisplayName}.";
        return true;
    }

    private static ImageEditorState CreateImageEditorState(
        string controlName,
        DesignerImageValues values)
        => new(
            controlName,
            values.Source,
            values.Stretch.ToString(),
            values.StretchDirection.ToString(),
            values.BitmapInterpolationMode.ToString(),
            values.EdgeMode.ToString(),
            values.BitmapBlendingMode.ToString());

    public bool TryGetSelectedButtonProperties(out ButtonEditorState state)
    {
        var target = Canvas.SelectedElement;
        if (target is null
            || target.IsLocked
            || !DesignerButtonRuntime.IsSupportedControl(target.Visual))
        {
            state = default!;
            StatusText = target switch
            {
                null => "Select a Button before editing actions and commands.",
                { IsLocked: true } => "Unlock the selected Button before editing actions and commands.",
                _ => "Button actions and commands editing is available for Button controls.",
            };
            return false;
        }

        if (!DesignerButtonRuntime.TryRead(
                target.Visual,
                out var values,
                out var error))
        {
            state = default!;
            StatusText = $"Button actions and commands cannot be edited. {error}";
            return false;
        }

        state = CreateButtonEditorState(target.DisplayName, values);
        return true;
    }

    public bool SetSelectedButtonProperties(DesignerButtonEditorInput input)
    {
        var target = Canvas.SelectedElement;
        if (target is null
            || target.IsLocked
            || target.Visual is not Button button
            || !DesignerButtonRuntime.IsSupportedControl(button))
        {
            StatusText =
                "Select an unlocked Button before editing actions and commands.";
            return false;
        }

        if (!DesignerButtonRuntime.TryParseValues(
                button,
                input,
                out var values,
                out var error))
        {
            StatusText = $"Button actions and commands were not changed. {error}";
            return false;
        }

        if (!DesignerButtonRuntime.TryRead(
                button,
                out var current,
                out error))
        {
            StatusText = $"Button actions and commands were not changed. {error}";
            return false;
        }

        if (current == values)
        {
            StatusText = "Button actions and commands are unchanged.";
            return true;
        }

        BeginCanvasMutation(
            HistoryActionType.EditProperty,
            "Updated Button actions and commands.");
        DesignerButtonRuntime.Apply(button, values);
        foreach (var propertyName in new[]
                 {
                     "Content",
                     "ClickMode",
                     "HotKey",
                     "IsDefault",
                     "IsCancel",
                     "CommandParameter",
                 })
        {
            DesignerStyleApplicationMetadata.ClearApplied(
                button,
                propertyName);
        }

        Canvas.RefreshDocumentStyles(button);
        CommitCanvasMutation();
        StatusText =
            $"Updated Button actions and commands for {target.DisplayName}.";
        return true;
    }

    private static ButtonEditorState CreateButtonEditorState(
        string controlName,
        DesignerButtonValues values)
        => new(
            controlName,
            values.Content,
            values.ClickMode.ToString(),
            values.HotKey,
            values.IsDefault,
            values.IsCancel,
            values.CommandParameter,
            values.ClickHandler);

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

    public bool TryGetSelectedElementName(out string controlName, out string elementName)
    {
        var target = Canvas.SelectedElement;
        if (target is null || target.IsLocked)
        {
            controlName = string.Empty;
            elementName = string.Empty;
            StatusText = "Select an unlocked control to rename it.";
            return false;
        }

        controlName = target.DisplayName;
        elementName = target.DisplayName;
        return true;
    }

    public bool RenameSelectedElement(string proposedName)
    {
        var target = Canvas.SelectedElement;
        if (target is null)
        {
            StatusText = "Select a control to rename it.";
            return false;
        }

        return TryRenameElement(target, proposedName);
    }

    public bool TryGetSelectedButtonClickHandler(out string buttonName, out string handlerName)
    {
        if (Canvas.SelectedElement is not { IsLocked: false, Visual: Button button } target)
        {
            buttonName = string.Empty;
            handlerName = string.Empty;
            StatusText = "Select an unlocked Button to edit its Click handler.";
            return false;
        }

        buttonName = target.DisplayName;
        handlerName = (button.Tag as ButtonClickHandlerMetadata)?.HandlerName ?? string.Empty;
        return true;
    }

    public bool SetSelectedButtonClickHandler(string handlerName)
    {
        if (Canvas.SelectedElement is not { IsLocked: false, Visual: Button button } target)
        {
            StatusText = "Select an unlocked Button to edit its Click handler.";
            return false;
        }

        var normalizedHandler = handlerName.Trim();
        if (!string.IsNullOrEmpty(normalizedHandler) && !IsValidControlName(normalizedHandler))
        {
            StatusText = "Click handler names must start with a letter or underscore and contain only letters, numbers, or underscores.";
            return false;
        }

        var currentHandler = (button.Tag as ButtonClickHandlerMetadata)?.HandlerName ?? string.Empty;
        if (string.Equals(currentHandler, normalizedHandler, StringComparison.Ordinal))
        {
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated Button Click handler.");
        button.Tag = string.IsNullOrEmpty(normalizedHandler)
            ? null
            : new ButtonClickHandlerMetadata(normalizedHandler);
        CommitCanvasMutation();
        StatusText = string.IsNullOrEmpty(normalizedHandler)
            ? $"Cleared Click handler for {target.DisplayName}."
            : $"Set Click handler for {target.DisplayName}: {normalizedHandler}.";
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
            Canvas.RefreshDocumentStyles(target.Visual);
        }

        CommitCanvasMutation();
        StatusText = enable
            ? $"Enabled {targets.Count} control(s)."
            : $"Disabled {targets.Count} control(s).";
    }

    public void ToggleSelectedVisibility()
    {
        var targets = Canvas.SelectedElements.Where(element => !element.IsLocked).ToList();
        if (targets.Count == 0)
        {
            StatusText = "Select at least one unlocked control to change its visibility.";
            return;
        }

        var show = targets.All(element => !element.Visual.IsVisible);
        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated control visibility.");
        foreach (var target in targets)
        {
            target.Visual.IsVisible = show;
        }

        CommitCanvasMutation();
        StatusText = show
            ? $"Showed {targets.Count} control(s)."
            : $"Hid {targets.Count} control(s).";
    }

    public bool TryGetSelectedLabelTarget(out string labelName, out string targetName)
    {
        if (Canvas.SelectedElement is not { IsLocked: false, Visual: Label label } target)
        {
            labelName = string.Empty;
            targetName = string.Empty;
            StatusText = "Select an unlocked Label to edit its target.";
            return false;
        }

        labelName = target.DisplayName;
        targetName = label.Tag?.ToString() ?? string.Empty;
        return true;
    }

    public void SetSelectedLabelTarget(string targetName)
    {
        if (Canvas.SelectedElement is not { IsLocked: false, Visual: Label label } source)
        {
            StatusText = "Select an unlocked Label to edit its target.";
            return;
        }

        var normalizedName = targetName.Trim();
        if (string.IsNullOrEmpty(normalizedName))
        {
            if (label.Target is null && label.Tag is null)
            {
                StatusText = "Label target is unchanged.";
                return;
            }

            BeginCanvasMutation(HistoryActionType.EditProperty, "Cleared label target.");
            label.Target = null;
            label.Tag = null;
            CommitCanvasMutation();
            StatusText = $"Cleared target for {source.DisplayName}.";
            return;
        }

        var target = Canvas.Elements.FirstOrDefault(element => string.Equals(
            element.DisplayName,
            normalizedName,
            StringComparison.OrdinalIgnoreCase));
        if (target is null || ReferenceEquals(target, source))
        {
            StatusText = "Choose the name of another control on the canvas.";
            return;
        }

        if (ReferenceEquals(label.Target, target.Visual)
            && string.Equals(label.Tag?.ToString(), target.DisplayName, StringComparison.Ordinal))
        {
            StatusText = "Label target is unchanged.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.EditProperty, "Updated label target.");
        label.Target = target.Visual;
        label.Tag = target.DisplayName;
        CommitCanvasMutation();
        StatusText = $"Linked {source.DisplayName} to {target.DisplayName}.";
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
        var oldName = element.DisplayName;
        element.DisplayName = name;
        foreach (var child in Canvas.Elements.Where(candidate =>
                     string.Equals(candidate.ParentName, oldName, StringComparison.OrdinalIgnoreCase)))
        {
            child.ParentName = name;
        }

        foreach (var label in Canvas.Elements.Select(candidate => candidate.Visual).OfType<Label>())
        {
            if (ReferenceEquals(label.Target, element.Visual)
                || string.Equals(label.Tag?.ToString(), oldName, StringComparison.OrdinalIgnoreCase))
            {
                label.Tag = name;
            }
        }
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
        var targets = Canvas.SelectedElements
            .Where(element => !element.IsLocked && !element.IsContainerChild)
            .ToList();
        if (targets.Count == 0)
        {
            StatusText = Canvas.SelectedElements.Any(element => element.IsContainerChild)
                ? "Container child positions are managed by their parent layout."
                : "Selected controls are locked.";
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

        if (!TryValidateRootOrCanvasSiblingSelection(targets, "Arrange", out var selectionError))
        {
            StatusText = selectionError;
            return;
        }

        var primary = Canvas.SelectedElement is { } selectedElement
            && targets.Contains(selectedElement)
                ? selectedElement
                : targets[^1];
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

    public bool GroupSelectedElements()
    {
        var targets = Canvas.SelectedElements.ToList();
        BeginCanvasMutation(HistoryActionType.TransformElement, "Grouped selected controls into a Canvas.");
        if (!Canvas.TryCreateCanvasGroup(targets, out var group, out var error)
            || group is null)
        {
            _pendingMutation = null;
            StatusText = error;
            return false;
        }

        _isSyncingSelection = true;
        try
        {
            ObjectTree.RebuildFrom(Canvas.Elements);
            Canvas.Select(group);
            ObjectTree.SelectByElement(group);
        }
        finally
        {
            _isSyncingSelection = false;
        }

        RefreshStylePreviewOptions();
        CommitCanvasMutation();
        StatusText = $"Grouped {targets.Count} control(s) into {group.DisplayName}.";
        return true;
    }

    public bool UngroupSelectedCanvas()
    {
        if (Canvas.SelectedElement is not { } target)
        {
            StatusText = "Select a Canvas group to ungroup.";
            return false;
        }

        BeginCanvasMutation(HistoryActionType.TransformElement, "Ungrouped Canvas control.");
        if (!Canvas.TryUngroupCanvas(target, out var children, out var error))
        {
            _pendingMutation = null;
            StatusText = error;
            return false;
        }

        _isSyncingSelection = true;
        try
        {
            ObjectTree.RebuildFrom(Canvas.Elements);
            Canvas.SelectMany(children);
            ObjectTree.SelectByElement(Canvas.SelectedElement);
        }
        finally
        {
            _isSyncingSelection = false;
        }

        RefreshStylePreviewOptions();
        CommitCanvasMutation();
        StatusText = $"Ungrouped {children.Count} control(s).";
        return true;
    }

    public void CenterSelectedElementsOnArtboard(bool horizontally, bool vertically)
    {
        var targets = Canvas.SelectedElements.Where(element => !element.IsLocked).ToList();
        if (targets.Count == 0)
        {
            StatusText = "Select an unlocked control to center on the artboard.";
            return;
        }

        if (!TryValidateRootOrCanvasSiblingSelection(
                targets,
                "Center on Artboard",
                allowCanvasChildren: false,
                out var selectionError))
        {
            StatusText = selectionError;
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

    private bool TryValidateRootOrCanvasSiblingSelection(
        IReadOnlyList<DesignElement> targets,
        string operation,
        out string error)
        => TryValidateRootOrCanvasSiblingSelection(
            targets,
            operation,
            allowCanvasChildren: true,
            out error);

    private bool TryValidateRootOrCanvasSiblingSelection(
        IReadOnlyList<DesignElement> targets,
        string operation,
        bool allowCanvasChildren,
        out string error)
    {
        var parentNames = targets
            .Select(element => element.ParentName ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (parentNames.Count != 1)
        {
            error = $"{operation} requires controls at the same root or inside the same Canvas.";
            return false;
        }

        var parentName = parentNames[0];
        if (parentName.Length == 0)
        {
            if (targets.Any(element => element.IsContainerChild))
            {
                error = $"{operation} is available for root controls or siblings inside the same Canvas.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        if (!allowCanvasChildren)
        {
            error = $"{operation} is available for root controls only; container children are positioned by their parent.";
            return false;
        }

        var parent = Canvas.Elements.FirstOrDefault(element =>
            string.Equals(element.DisplayName, parentName, StringComparison.OrdinalIgnoreCase));
        if (parent?.Visual is Canvas && targets.All(element => element.IsCanvasChild))
        {
            error = string.Empty;
            return true;
        }

        error = $"{operation} supports root controls or siblings inside the same Canvas. "
            + "Grid, StackPanel, DockPanel, WrapPanel, UniformGrid, TabControl, SplitView, and Content children use parent layout.";
        return false;
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
        var selected = Canvas.SelectedElements.Where(element => !element.IsLocked).ToList();
        if (selected.Count == 0)
        {
            StatusText = "No unlocked controls to delete.";
            return;
        }

        var targets = selected
            .SelectMany(target => target.Visual is Canvas
                || target.Visual.GetType() == typeof(ContentControl)
                ? CollectSelectionSubtree([target])
                : [target])
            .Distinct()
            .OrderBy(GetElementDepth)
            .ThenBy(Canvas.Elements.IndexOf)
            .ToList();
        if (targets.Any(element => element.IsLocked))
        {
            StatusText = "Cannot delete a hierarchy that contains locked controls.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.RemoveElement, "Removed control from canvas.");
        var removedVisuals = targets.Select(target => target.Visual).ToHashSet();
        foreach (var label in Canvas.Elements.Select(element => element.Visual).OfType<Label>())
        {
            if (label.Target is Control labelTarget && removedVisuals.Contains(labelTarget))
            {
                label.Target = null;
                label.Tag = null;
            }
        }

        foreach (var target in targets.OrderByDescending(GetElementDepth).ToList())
        {
            Canvas.RemoveElement(target);
        }

        ObjectTree.RebuildFrom(Canvas.Elements);
        CommitCanvasMutation();
        StatusText = $"Deleted {targets.Count} control(s)";
    }

    public void DuplicateSelectedElement()
    {
        var selected = Canvas.SelectedElements.ToList();
        if (selected.Count == 0)
        {
            StatusText = "No selected element to duplicate.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.DuplicateElement, "Duplicated control.");
        var targets = CollectSelectionSubtree(selected);
        var snapshots = CaptureElementSnapshots(targets);
        var nameMap = BuildDuplicateNameMap(snapshots);
        var duplicates = new List<DesignElement>();
        foreach (var snapshot in CreateDuplicatedSnapshots(snapshots, nameMap))
        {
            var duplicated = Canvas.AddElementFromSnapshot(
                snapshot,
                select: false,
                deferContainerReflow: true);
            duplicates.Add(duplicated);
        }

        Canvas.NormalizeContainerRelationships();
        ObjectTree.RebuildFrom(Canvas.Elements);
        _isSyncingSelection = true;
        try
        {
            Canvas.SelectMany(duplicates);
            ObjectTree.SelectByElement(Canvas.SelectedElement);
        }
        finally
        {
            _isSyncingSelection = false;
        }

        CommitCanvasMutation();

        StatusText = $"Duplicated {duplicates.Count} control(s)";
    }

    public void CopySelectedElement()
    {
        var selected = Canvas.SelectedElements.ToList();
        if (selected.Count == 0)
        {
            StatusText = "No selected element to copy.";
            return;
        }

        _clipboardSnapshots = CaptureElementSnapshots(CollectSelectionSubtree(selected));
        OnPropertyChanged(nameof(CanPaste));
        StatusText = $"Copied {_clipboardSnapshots.Count} control(s)";
    }

    public void CutSelectedElement()
    {
        var selected = Canvas.SelectedElements.Where(element => !element.IsLocked).ToList();
        if (selected.Count == 0)
        {
            StatusText = "No unlocked controls to cut.";
            return;
        }

        var targets = CollectSelectionSubtree(selected);
        if (targets.Any(element => element.IsLocked))
        {
            StatusText = "Cannot cut a hierarchy that contains locked controls.";
            return;
        }

        _clipboardSnapshots = CaptureElementSnapshots(targets);
        OnPropertyChanged(nameof(CanPaste));
        BeginCanvasMutation(HistoryActionType.RemoveElement, "Cut control.");
        foreach (var target in targets.OrderByDescending(GetElementDepth).ToList())
        {
            Canvas.RemoveElement(target);
        }

        ObjectTree.RebuildFrom(Canvas.Elements);
        CommitCanvasMutation();
        StatusText = $"Cut {_clipboardSnapshots.Count} control(s)";
    }

    public void PasteElement()
    {
        if (_clipboardSnapshots is not { Count: > 0 })
        {
            StatusText = "Clipboard is empty.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.PasteElement, "Pasted control.");

        var nameMap = BuildDuplicateNameMap(_clipboardSnapshots);
        var pastedSnapshots = CreateDuplicatedSnapshots(_clipboardSnapshots, nameMap);
        var pastedElements = new List<DesignElement>();
        foreach (var pastedSnapshot in pastedSnapshots)
        {
            var pasted = Canvas.AddElementFromSnapshot(
                pastedSnapshot,
                select: false,
                deferContainerReflow: true);
            pastedElements.Add(pasted);
        }

        Canvas.NormalizeContainerRelationships();
        ObjectTree.RebuildFrom(Canvas.Elements);
        _isSyncingSelection = true;
        try
        {
            Canvas.SelectMany(pastedElements);
            ObjectTree.SelectByElement(Canvas.SelectedElement);
        }
        finally
        {
            _isSyncingSelection = false;
        }

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

    public bool TryValidateAxamlSource(string axaml, out string result)
    {
        if (!TryParseAxamlSource(axaml, out var document, out result))
        {
            return false;
        }

        var controlCount = document.Elements.Count;
        result = string.IsNullOrEmpty(result)
            ? $"AXAML source is valid ({controlCount} control(s))."
            : $"AXAML source is valid ({controlCount} control(s)). {result}";
        return true;
    }

    public bool TryCreatePreviewDocumentFromAxaml(
        string axaml,
        out DesignerCanvasDocument document,
        out string result)
    {
        if (!TryParseAxamlSource(axaml, out var parsed, out result))
        {
            document = new DesignerCanvasDocument(Array.Empty<DesignerElementSnapshot>());
            return false;
        }

        document = parsed;
        result = string.IsNullOrEmpty(result)
            ? $"Previewing AXAML source ({document.Elements.Count} control(s))."
            : $"Previewing AXAML source ({document.Elements.Count} control(s)). {result}";
        return true;
    }

    public bool TryApplyAxamlSource(string axaml, out string result)
    {
        if (!TryParseAxamlSource(axaml, out var document, out result))
        {
            return false;
        }

        var before = CaptureDocument();
        if (string.Equals(axaml, ExportFullAxaml(), StringComparison.Ordinal)
            || AreSameDocument(before, document))
        {
            result = string.IsNullOrEmpty(result)
                ? "AXAML source is valid; there are no design changes to apply."
                : $"AXAML source is valid; there are no design changes to apply. {result}";
            StatusText = result;
            return true;
        }

        BeginCanvasMutation(HistoryActionType.EditAxamlSource, "Applied AXAML source.");
        try
        {
            ApplyDocument(document);
            CommitCanvasMutation();
        }
        catch (Exception ex)
        {
            _pendingMutation = null;
            ApplyDocument(before);
            RefreshDirtyState();
            result = $"AXAML source could not be applied: {ex.Message}";
            return false;
        }

        result = string.IsNullOrEmpty(result)
            ? $"Applied AXAML source ({document.Elements.Count} control(s))."
            : $"Applied AXAML source ({document.Elements.Count} control(s)). {result}";
        StatusText = result;
        return true;
    }

    public string ExportFullAxaml() => ExportAxamlDocument(_rootSettings.Kind);

    public string ExportUserControlAxaml() => ExportAxamlDocument(DesignerRootKind.UserControl);

    public bool TryExportSelectedAxaml(
        out string controlName,
        out string axaml,
        out string error)
    {
        var selected = Canvas.SelectedElement;
        if (selected is null)
        {
            controlName = string.Empty;
            axaml = string.Empty;
            error = "Select a control before exporting selected AXAML.";
            return false;
        }

        controlName = selected.DisplayName;
        error = string.Empty;
        axaml = string.Empty;

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("<UserControl xmlns=\"https://github.com/avaloniaui\"");
            sb.AppendLine("        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
            sb.AppendLine("        xmlns:collections=\"clr-namespace:Avalonia.Collections;assembly=Avalonia.Base\"");
            sb.AppendLine($"        Width=\"{FormatRootNumber(selected.Width)}\" Height=\"{FormatRootNumber(selected.Height)}\">");
            sb.AppendLine("  <!-- Selected control AXAML; provide the host DataContext for bindings. -->");
            AppendColorResourcesAxaml(sb, "UserControl", "  ");
            AppendDocumentStylesAxaml(sb, "UserControl", "  ");
            if (selected.IsLocked)
            {
                sb.AppendLine($"  <!-- {DesignerMetadataPrefix} IsLocked=true -->");
            }

            _standaloneAxamlElement = selected;
            try
            {
                WriteTopLevelElementAxaml(sb, selected, "  ");
            }
            finally
            {
                _standaloneAxamlElement = null;
            }

            sb.AppendLine("</UserControl>");
            axaml = sb.ToString();
            return true;
        }
        catch (Exception exception)
        {
            _standaloneAxamlElement = null;
            axaml = string.Empty;
            error = exception.Message;
            return false;
        }
    }

    private string ExportAxamlDocument(DesignerRootKind rootKind)
    {
        var settings = CaptureCanvasSettings();
        var rootElementName = rootKind.ToString();
        var sb = new StringBuilder();
        sb.AppendLine($"<{rootElementName} xmlns=\"https://github.com/avaloniaui\"");
        sb.AppendLine("        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
        sb.AppendLine("        xmlns:collections=\"clr-namespace:Avalonia.Collections;assembly=Avalonia.Base\"");
        sb.Append($"        Width=\"{FormatRootNumber(settings.Width)}\" Height=\"{FormatRootNumber(settings.Height)}\"");
        if (rootKind == DesignerRootKind.Window)
        {
            if (!string.IsNullOrEmpty(_rootSettings.Title))
            {
                sb.Append($" Title=\"{EscapeXmlAttribute(_rootSettings.Title)}\"");
            }

            if (!_rootSettings.CanResize)
            {
                sb.Append(" CanResize=\"False\"");
            }

            if (_rootSettings.StartupLocation != DesignerWindowStartupLocation.Manual)
            {
                sb.Append($" WindowStartupLocation=\"{_rootSettings.StartupLocation}\"");
            }
        }

        AppendRootConstraintAttribute(sb, "MinWidth", _rootSettings.MinWidth, 0);
        AppendRootConstraintAttribute(sb, "MinHeight", _rootSettings.MinHeight, 0);
        AppendRootConstraintAttribute(sb, "MaxWidth", _rootSettings.MaxWidth, double.PositiveInfinity);
        AppendRootConstraintAttribute(sb, "MaxHeight", _rootSettings.MaxHeight, double.PositiveInfinity);
        sb.AppendLine(">");
        if (rootKind == DesignerRootKind.UserControl)
        {
            sb.AppendLine("  <!-- Add x:Class when pairing this layout with a code-behind file. -->");
        }

        AppendColorResourcesAxaml(sb, rootElementName, "  ");
        AppendDocumentStylesAxaml(sb, rootElementName, "  ");

        sb.AppendLine($"  <Canvas Width=\"{settings.Width:0.###}\" Height=\"{settings.Height:0.###}\" Background=\"{EscapeXmlAttribute(settings.Background)}\">");
        sb.AppendLine($"    <!-- {DesignerMetadataPrefix} GridSize={settings.GridSize.ToString("0.###", CultureInfo.InvariantCulture)}; IsGridVisible={settings.IsGridVisible}; SnapToGrid={settings.SnapToGrid} -->");
        if (_sampleDataJson is not null)
        {
            var encodedSampleData = Convert.ToBase64String(Encoding.UTF8.GetBytes(_sampleDataJson));
            sb.AppendLine($"    <!-- {DesignerMetadataPrefix} SampleDataBase64={encodedSampleData} -->");
        }

        foreach (var element in Canvas.Elements.Where(element => !HasValidContainerParent(element)))
        {
            if (element.IsLocked)
            {
                sb.AppendLine($"    <!-- {DesignerMetadataPrefix} IsLocked=true -->");
            }

            WriteTopLevelElementAxaml(sb, element, "    ");
        }

        sb.AppendLine("  </Canvas>");
        sb.AppendLine($"</{rootElementName}>");
        return sb.ToString();
    }

    private static void AppendRootConstraintAttribute(
        StringBuilder sb,
        string attributeName,
        double value,
        double defaultValue)
    {
        if (value.Equals(defaultValue))
        {
            return;
        }

        sb.Append($" {attributeName}=\"{FormatRootNumber(value)}\"");
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

    private bool TryParseAxamlSource(
        string axaml,
        out DesignerCanvasDocument document,
        out string result)
    {
        document = new DesignerCanvasDocument(Array.Empty<DesignerElementSnapshot>());
        result = string.Empty;
        if (string.IsNullOrWhiteSpace(axaml))
        {
            result = "AXAML source cannot be empty.";
            return false;
        }

        var warnings = new List<string>();
        try
        {
            document = ParseDraftDocument(axaml, warnings);
        }
        catch (Exception ex)
        {
            result = $"AXAML source is invalid: {ex.Message}";
            return false;
        }

        result = FormatWarnings(warnings);
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

            var current = CaptureDocument();
            var currentByName = current.Elements.ToDictionary(
                element => element.DisplayName,
                StringComparer.Ordinal);
            var parsedByName = parsed.Elements.ToDictionary(
                element => element.DisplayName,
                StringComparer.Ordinal);
            if (!currentByName.Keys.ToHashSet(StringComparer.Ordinal)
                .SetEquals(parsedByName.Keys))
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

            if (currentByName.Any(pair =>
                    parsedByName[pair.Key].IsLocked != pair.Value.IsLocked))
            {
                result = "Validation failed: exported control lock states do not match.";
                return false;
            }

            if (!DictionaryEquals(current.ColorResources, parsed.ColorResources))
            {
                result = "Validation failed: color resources do not round-trip.";
                return false;
            }

            if (!StylesEqual(current.Styles, parsed.Styles))
            {
                result = "Validation failed: document styles do not round-trip.";
                return false;
            }

            if (!string.Equals(
                    current.SampleDataJson,
                    parsed.SampleDataJson,
                    StringComparison.Ordinal))
            {
                result = "Validation failed: sample DataContext does not round-trip.";
                return false;
            }

            if ((current.RootSettings ?? new DesignerRootSettings())
                != (parsed.RootSettings ?? new DesignerRootSettings()))
            {
                result = "Validation failed: document root properties do not round-trip.";
                return false;
            }

            foreach (var pair in currentByName)
            {
                if (!HaveSameBindings(pair.Value, parsedByName[pair.Key]))
                {
                    result = $"Validation failed: bindings do not round-trip for {pair.Key}.";
                    return false;
                }

                if (!HaveSameAppearance(pair.Value, parsedByName[pair.Key]))
                {
                    result = $"Validation failed: appearance properties do not round-trip for {pair.Key}.";
                    return false;
                }
            }

            var userControlWarnings = new List<string>();
            var userControlAxaml = ExportUserControlAxaml();
            var userControlRoot = XDocument.Parse(userControlAxaml).Root;
            if (!string.Equals(userControlRoot?.Name.LocalName, "UserControl", StringComparison.Ordinal))
            {
                result = "Validation failed: UserControl export has an invalid root element.";
                return false;
            }

            var userControl = ParseDraftDocument(userControlAxaml, userControlWarnings);
            if (!userControl.Elements.Select(element => element.DisplayName)
                .ToHashSet(StringComparer.Ordinal)
                .SetEquals(currentByName.Keys))
            {
                result = "Validation failed: UserControl export does not preserve control names.";
                return false;
            }

            if (userControl.Settings != settings)
            {
                result = "Validation failed: UserControl export does not preserve canvas settings.";
                return false;
            }

            var expectedUserControlRoot = new DesignerRootSettings(
                DesignerRootKind.UserControl,
                MinWidth: _rootSettings.MinWidth,
                MinHeight: _rootSettings.MinHeight,
                MaxWidth: _rootSettings.MaxWidth,
                MaxHeight: _rootSettings.MaxHeight);
            if ((userControl.RootSettings ?? new DesignerRootSettings()) != expectedUserControlRoot)
            {
                result = "Validation failed: UserControl export does not preserve root layout constraints.";
                return false;
            }


            if (!DictionaryEquals(userControl.ColorResources, current.ColorResources))
            {
                result = "Validation failed: UserControl export does not preserve color resources.";
                return false;
            }

            if (!StylesEqual(userControl.Styles, current.Styles))
            {
                result = "Validation failed: UserControl export does not preserve document styles.";
                return false;
            }

            if (!string.Equals(
                    userControl.SampleDataJson,
                    current.SampleDataJson,
                    StringComparison.Ordinal))
            {
                result = "Validation failed: UserControl export does not preserve sample DataContext.";
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

        if (_sampleDataRoot is not null)
        {
            RefreshSampleDataPreview();
        }

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
        DocumentChanged?.Invoke(this, EventArgs.Empty);
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

    private void OnDesignerCanvasPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_isSyncingSelection
            && e.PropertyName == nameof(CanvasViewModel.SelectedElement))
        {
            RefreshStylePreviewOptions();
        }
    }

    private IReadOnlyList<string> GetMatchingStylePreviewStates(DesignElement element)
    {
        var targetType = element.Visual.GetType().Name;
        var classes = CanvasViewModel.GetUserStyleClasses(element.Visual).ToHashSet(StringComparer.Ordinal);
        return _documentStyles
            .Where(style =>
                string.Equals(style.TargetType, targetType, StringComparison.Ordinal)
                && style.PseudoClass is not null
                && classes.Contains(style.ClassName))
            .Select(style => style.PseudoClass!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(state =>
            {
                var index = StylePreviewStateOrder.IndexOf(state);
                return index < 0 ? int.MaxValue : index;
            })
            .ThenBy(state => state, StringComparer.Ordinal)
            .ToList();
    }

    private void SyncSelectedStylePreviewOption(string? pseudoClass)
    {
        _isRefreshingStylePreviewOptions = true;
        try
        {
            SelectedStylePreviewOption = StylePreviewOptions.FirstOrDefault(option =>
                    string.Equals(option.PseudoClass, pseudoClass, StringComparison.Ordinal))
                ?? StylePreviewOptions.FirstOrDefault();
        }
        finally
        {
            _isRefreshingStylePreviewOptions = false;
        }
    }

    private static string FormatStylePreviewState(string pseudoClass)
        => pseudoClass switch
        {
            "pointerover" => "Pointer Over",
            "focus-visible" => "Focus Visible",
            _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(pseudoClass.Replace('-', ' ')),
        };

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
            ClearSampleDataPreview();
            var settings = document.Settings ?? new DesignerCanvasSettings();
            Canvas.SetArtboard(settings.Width, settings.Height, settings.Background);
            Canvas.SetGridSize(settings.GridSize);
            Canvas.IsGridVisible = settings.IsGridVisible;
            Canvas.SnapToGrid = settings.SnapToGrid;
            Canvas.Clear();
            _colorResources.Clear();
            if (document.ColorResources is not null)
            {
                foreach (var pair in document.ColorResources)
                {
                    _colorResources[pair.Key] = FormatBrushValue(Brush.Parse(pair.Value));
                }
            }

            _documentStyles.Clear();
            if (document.Styles is not null)
            {
                _documentStyles.AddRange(CloneStyles(document.Styles));
            }

            Canvas.SetColorResources(_colorResources);
            Canvas.SetDocumentStyles(_documentStyles);
            if (!DesignerSampleDataRuntime.TryParse(
                    document.SampleDataJson ?? string.Empty,
                    out var sampleData,
                    out _))
            {
                sampleData = null;
            }

            _sampleDataJson = sampleData?.Json;
            _sampleDataRoot = sampleData?.Root;
            _rootSettings = document.RootSettings ?? new DesignerRootSettings();
            foreach (var snapshot in document.Elements)
            {
                Canvas.AddElementFromSnapshot(
                    snapshot,
                    select: false,
                    deferContainerReflow: true);
            }

            Canvas.NormalizeContainerRelationships();
            ObjectTree.RebuildFrom(Canvas.Elements);
            Canvas.Select(null);
            RefreshSampleDataPreview();
        }
        finally
        {
            _isSyncingSelection = false;
        }

        RefreshStylePreviewOptions();
        OnPropertyChanged(nameof(HasSampleData));
        OnPropertyChanged(nameof(SampleDataJson));
        OnPropertyChanged(nameof(RootKindLabel));
        DocumentChanged?.Invoke(this, EventArgs.Empty);
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
                e.IsLocked,
                e.ParentName,
                e.GridRow,
                e.GridColumn,
                e.GridRowSpan,
                e.GridColumnSpan,
                e.StackPanelIndex,
                e.StackPanelItemSize,
                e.ParentLayout,
                e.DockPanelIndex,
                e.DockPanelDock,
                e.DockPanelItemSize,
                e.WrapPanelIndex,
                e.UniformGridIndex,
                e.CanvasChildIndex,
                e.CanvasChildLeft,
                e.CanvasChildTop,
                e.TabIndex,
                e.TabHeader,
                e.SplitViewSlot))
            .ToList();

        return new DesignerCanvasDocument(
            snapshots,
            CaptureCanvasSettings(),
            new Dictionary<string, string>(_colorResources, StringComparer.Ordinal),
            CloneStyles(_documentStyles),
            _sampleDataJson,
            _rootSettings);
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
            new("Title", "Avalonia.Controls.TextBlock", 48, 40, 520, 36, Props("Text", "Project dashboard", "Classes", "heading")),
            new("Summary", "Avalonia.Controls.TextBlock", 48, 96, 520, 24, Props("Text", "A quick overview of this week's progress")),
            new("ProgressLabel", "Avalonia.Controls.TextBlock", 48, 158, 240, 24, Props("Text", "Completion")),
            new("Progress", "Avalonia.Controls.Slider", 48, 186, 360, 32, Props("Minimum", "0", "Maximum", "100", "Value", "72")),
            new("Refresh", "Avalonia.Controls.Button", 48, 252, 140, 36, Props("Content", "Refresh", "Classes", "primary")),
            new("AutoRefresh", "Avalonia.Controls.CheckBox", 214, 254, 240, 32, Props("Content", "Refresh automatically")),
        ],
        new DesignerCanvasSettings(720, 440, "#F7F9FC"),
        Props("AccentBrush", "#2563EB", "SurfaceBrush", "#F1F5F9"),
        [
            new DesignerStyleDefinition(
                "TextBlock",
                "heading",
                Props(
                    "Foreground", "{DynamicResource AccentBrush}",
                    "FontSize", "26",
                    "FontWeight", "SemiBold")),
            new DesignerStyleDefinition(
                "Button",
                "primary",
                Props(
                    "Background", "{DynamicResource AccentBrush}",
                    "Foreground", "#ffffffff",
                    "CornerRadius", "6,6,6,6")),
            new DesignerStyleDefinition(
                "Button",
                "primary",
                Props("Background", "#ff3b82f6"),
                "pointerover"),
            new DesignerStyleDefinition(
                "Button",
                "primary",
                Props("Background", "#ff1d4ed8"),
                "pressed"),
            new DesignerStyleDefinition(
                "Button",
                "primary",
                Props("Opacity", "0.55"),
                "disabled"),
        ]);

    private static IReadOnlyDictionary<string, string> Props(params string[] values)
    {
        var properties = new Dictionary<string, string>();
        for (var index = 0; index < values.Length; index += 2)
        {
            properties[values[index]] = values[index + 1];
        }

        return properties;
    }

    private List<DesignElement> CollectSelectionSubtree(IReadOnlyList<DesignElement> selected)
    {
        var roots = selected
            .Where(candidate => !selected.Any(ancestor =>
                !ReferenceEquals(candidate, ancestor)
                && IsDescendantOf(candidate, ancestor)))
            .ToHashSet();

        return Canvas.Elements
            .Where(element => roots.Any(root =>
                ReferenceEquals(element, root)
                || IsDescendantOf(element, root)))
            .OrderBy(GetElementDepth)
            .ThenBy(Canvas.Elements.IndexOf)
            .ToList();
    }

    private int GetElementDepth(DesignElement element)
    {
        var depth = 0;
        var current = element;
        var visited = new HashSet<DesignElement>();
        while (current.ParentName is not null && visited.Add(current))
        {
            var parent = Canvas.Elements.FirstOrDefault(candidate => string.Equals(
                candidate.DisplayName,
                current.ParentName,
                StringComparison.OrdinalIgnoreCase));
            if (parent is null)
            {
                break;
            }

            depth++;
            current = parent;
        }

        return depth;
    }

    private List<DesignerElementSnapshot> CaptureElementSnapshots(
        IEnumerable<DesignElement> elements)
        => elements
            .Select(element => CreateSnapshot(
                element,
                element.DisplayName,
                element.X,
                element.Y))
            .ToList();

    private Dictionary<string, string> BuildDuplicateNameMap(
        IEnumerable<DesignerElementSnapshot> snapshots)
    {
        var nameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var snapshot in snapshots)
        {
            nameMap[snapshot.DisplayName] = BuildDuplicateDisplayName(snapshot.DisplayName);
        }

        return nameMap;
    }

    private static List<DesignerElementSnapshot> CreateDuplicatedSnapshots(
        IReadOnlyList<DesignerElementSnapshot> snapshots,
        IReadOnlyDictionary<string, string> nameMap)
        => snapshots
            .Select(snapshot => snapshot with
            {
                DisplayName = nameMap[snapshot.DisplayName],
                X = snapshot.X + 16,
                Y = snapshot.Y + 16,
                VisualProperties = CloneProperties(snapshot.VisualProperties),
                ParentName = snapshot.ParentName is not null
                    && nameMap.TryGetValue(snapshot.ParentName, out var duplicateParentName)
                        ? duplicateParentName
                        : snapshot.ParentName,
            })
            .ToList();

    private DesignerElementSnapshot CreateSnapshot(
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
            CloneProperties(CaptureVisualProperties(element.Visual)),
            element.IsLocked,
            element.ParentName,
            element.GridRow,
            element.GridColumn,
            element.GridRowSpan,
            element.GridColumnSpan,
            element.StackPanelIndex,
            element.StackPanelItemSize,
            element.ParentLayout,
            element.DockPanelIndex,
            element.DockPanelDock,
            element.DockPanelItemSize,
            element.WrapPanelIndex,
            element.UniformGridIndex,
            element.CanvasChildIndex,
            element.CanvasChildLeft,
            element.CanvasChildTop,
            element.TabIndex,
            element.TabHeader,
            element.SplitViewSlot);
    }

    private IReadOnlyDictionary<string, string>? CaptureVisualProperties(Control visual)
    {
        var reapplySample = _sampleDataRoot is not null
            && DesignerSampleDataRuntime.IsApplied(visual);
        if (reapplySample)
        {
            DesignerSampleDataRuntime.Clear(visual);
        }

        try
        {
            return CaptureVisualPropertiesWithoutSample(visual);
        }
        finally
        {
            if (reapplySample && _sampleDataRoot is not null)
            {
                DesignerSampleDataRuntime.Apply(visual, _sampleDataRoot);
            }
        }
    }

    private IReadOnlyDictionary<string, string>? CaptureVisualPropertiesWithoutSample(Control visual)
    {
        var properties = CaptureVisualPropertiesCore(visual);

        var result = properties is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(properties, StringComparer.Ordinal);
        CaptureCommonAppearanceProperties(result, visual);
        DesignerLayoutRuntime.Capture(visual, result);
        DesignerTypographyRuntime.Capture(visual, result);
        DesignerTransformRuntime.Capture(visual, result);
        DesignerAccessibilityRuntime.Capture(visual, result);
        DesignerInteractionRuntime.Capture(visual, result);
        DesignerEffectRuntime.Capture(visual, result);
        DesignerRangeRuntime.Capture(visual, result);
        DesignerTextInputRuntime.Capture(visual, result);
        DesignerMaskedTextBoxRuntime.Capture(visual, result);
        DesignerSelectableTextBlockRuntime.Capture(visual, result);
        DesignerSplitViewRuntime.Capture(visual, result);
        DesignerTabControlRuntime.Capture(visual, result);
        DesignerSelectionRuntime.Capture(visual, result);
        DesignerDateTimeRuntime.Capture(visual, result);
        DesignerColorPickerRuntime.Capture(visual, result);
        DesignerAutoCompleteBoxRuntime.Capture(visual, result);
        DesignerToggleRuntime.Capture(visual, result);
        DesignerContainerBehaviorRuntime.Capture(visual, result);
        DesignerImageRuntime.Capture(visual, result);
        DesignerButtonRuntime.Capture(visual, result);
        DesignerDataGridBehaviorRuntime.Capture(visual, result);
        foreach (var pair in DesignerResourceReferenceMetadata.GetReferences(visual))
        {
            result[pair.Key] = DesignerResourceReferenceMetadata.FormatExpression(pair.Value);
        }

        foreach (var propertyName in DesignerStyleApplicationMetadata.GetAppliedProperties(visual))
        {
            result.Remove(propertyName);
        }

        foreach (var propertyName in GetStyleManagedPropertyNames(visual))
        {
            if (!DesignerStyleRuntime.HasLocalValue(visual, propertyName))
            {
                result.Remove(propertyName);
            }
        }

        var classes = CanvasViewModel.GetUserStyleClasses(visual);
        if (classes.Count > 0)
        {
            result["Classes"] = string.Join(" ", classes);
        }

        var bindings = DesignerBindingRuntime.ReadBindings(visual);
        if (bindings.Count > 0)
        {
            result["__bindings"] = DesignerBindingRuntime.Serialize(bindings);
        }

        return result;
    }

    private void ClearSampleDataPreview()
    {
        foreach (var element in Canvas.Elements)
        {
            DesignerSampleDataRuntime.Clear(element.Visual);
        }
    }

    private DesignerSampleApplyResult RefreshSampleDataPreview()
    {
        var result = new DesignerSampleApplyResult(0, 0, 0);
        foreach (var element in Canvas.Elements)
        {
            DesignerSampleDataRuntime.Clear(element.Visual);
            if (_sampleDataRoot is not null)
            {
                result += DesignerSampleDataRuntime.Apply(element.Visual, _sampleDataRoot);
            }
        }

        return result;
    }

    private static string FormatSampleApplyResult(DesignerSampleApplyResult result)
    {
        var message = $"Applied sample DataContext to {result.AppliedCount} binding(s).";
        if (result.MissingPathCount > 0)
        {
            message += $" {result.MissingPathCount} path(s) were not found.";
        }

        if (result.ConversionFailureCount > 0)
        {
            message += $" {result.ConversionFailureCount} value(s) could not be converted.";
        }

        return message;
    }

    private IReadOnlyCollection<string> GetStyleManagedPropertyNames(Control visual)
    {
        var targetType = visual.GetType().Name;
        var classes = CanvasViewModel.GetUserStyleClasses(visual).ToHashSet(StringComparer.Ordinal);
        return _documentStyles
            .Where(style =>
                string.Equals(style.TargetType, targetType, StringComparison.Ordinal)
                && classes.Contains(style.ClassName))
            .SelectMany(style => style.Setters.Keys)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private bool ShouldSuppressInlineStyleProperty(Control visual, string propertyName)
        => DesignerStyleApplicationMetadata.IsApplied(visual, propertyName)
            || GetStyleManagedPropertyNames(visual).Contains(propertyName)
                && !DesignerStyleRuntime.HasLocalValue(visual, propertyName);

    private static void CaptureCommonAppearanceProperties(IDictionary<string, string> properties, Control visual)
    {
        if (visual is not Avalonia.Controls.Primitives.TemplatedControl templated)
        {
            return;
        }

        if (templated.IsSet(Avalonia.Controls.Primitives.TemplatedControl.BackgroundProperty)
            && templated.Background is { } background)
        {
            properties["Background"] = FormatBrushValue(background);
        }

        if (templated.IsSet(Avalonia.Controls.Primitives.TemplatedControl.ForegroundProperty)
            && templated.Foreground is { } foreground)
        {
            properties["Foreground"] = FormatBrushValue(foreground);
        }

        if (templated.IsSet(Avalonia.Controls.Primitives.TemplatedControl.BorderBrushProperty)
            && templated.BorderBrush is { } borderBrush)
        {
            properties["BorderBrush"] = FormatBrushValue(borderBrush);
        }

        if (templated.IsSet(Avalonia.Controls.Primitives.TemplatedControl.BorderThicknessProperty))
        {
            properties["BorderThickness"] = templated.BorderThickness.ToString();
        }

        if (templated.IsSet(Avalonia.Controls.Primitives.TemplatedControl.CornerRadiusProperty))
        {
            properties["CornerRadius"] = templated.CornerRadius.ToString();
        }
    }

    private static IReadOnlyDictionary<string, string>? CaptureVisualPropertiesCore(Control visual)
    {
        if (visual is ToggleButton toggleButton)
        {
            var properties = DesignerToggleRuntime.GetAxamlAttributes(toggleButton)
                .ToDictionary(
                    attribute => attribute.Name,
                    attribute => attribute.Value,
                    StringComparer.Ordinal);
            properties["Opacity"] =
                toggleButton.Opacity.ToString("0.###", CultureInfo.InvariantCulture);
            return properties;
        }

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
                ["AcceptsReturn"] = textBox.AcceptsReturn.ToString(),
                ["TextWrapping"] = textBox.TextWrapping.ToString(),
                ["Opacity"] = textBox.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is AutoCompleteBox autoCompleteBox)
        {
            var properties = DesignerAutoCompleteBoxRuntime.GetAxamlAttributes(autoCompleteBox)
                .ToDictionary(
                    attribute => attribute.Name,
                    attribute => attribute.Value,
                    StringComparer.Ordinal);
            properties["__items"] = SerializeAutoCompleteBoxItems(autoCompleteBox);
            properties["Opacity"] = autoCompleteBox.Opacity.ToString("0.###", CultureInfo.InvariantCulture);
            return properties;
        }

        if (visual is SelectableTextBlock selectableTextBlock
            && visual.GetType() == typeof(SelectableTextBlock))
        {
            return new Dictionary<string, string>
            {
                ["Text"] = selectableTextBlock.Text ?? string.Empty,
                ["Opacity"] = selectableTextBlock.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
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

            if (textBlock.IsSet(TextBlock.ForegroundProperty)
                && textBlock.Foreground is { } foreground)
            {
                properties["Foreground"] = FormatBrushValue(foreground);
            }

            if (textBlock.IsSet(TextBlock.BackgroundProperty)
                && textBlock.Background is { } background)
            {
                properties["Background"] = FormatBrushValue(background);
            }

            return properties;
        }

        if (visual is Label label)
        {
            return new Dictionary<string, string>
            {
                ["Content"] = label.Content?.ToString() ?? string.Empty,
                ["Target"] = label.Tag?.ToString() ?? string.Empty,
                ["Opacity"] = label.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
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

        if (visual is Shape shape)
        {
            var properties = new Dictionary<string, string>
            {
                ["StrokeThickness"] = shape.StrokeThickness.ToString("0.###", CultureInfo.InvariantCulture),
                ["Stretch"] = shape.Stretch.ToString(),
                ["StrokeDashOffset"] = shape.StrokeDashOffset.ToString("0.###", CultureInfo.InvariantCulture),
                ["StrokeLineCap"] = shape.StrokeLineCap.ToString(),
                ["StrokeJoin"] = shape.StrokeJoin.ToString(),
                ["StrokeMiterLimit"] = shape.StrokeMiterLimit.ToString("0.###", CultureInfo.InvariantCulture),
                ["Opacity"] = shape.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
            if (shape.Fill is { } fill)
            {
                properties["Fill"] = FormatBrushValue(fill);
            }

            if (shape.Stroke is { } stroke)
            {
                properties["Stroke"] = FormatBrushValue(stroke);
            }

            if (shape.StrokeDashArray is { Count: > 0 } dashArray)
            {
                properties["StrokeDashArray"] = string.Join(
                    ",",
                    dashArray.Select(value =>
                        value.ToString("0.###", CultureInfo.InvariantCulture)));
            }

            switch (shape)
            {
                case RectangleShape rectangle:
                    properties["RadiusX"] = rectangle.RadiusX.ToString("0.###", CultureInfo.InvariantCulture);
                    properties["RadiusY"] = rectangle.RadiusY.ToString("0.###", CultureInfo.InvariantCulture);
                    break;
                case LineShape line:
                    properties["StartPoint"] = FormatPoint(line.StartPoint);
                    properties["EndPoint"] = FormatPoint(line.EndPoint);
                    break;
                case PathShape { Tag: DesignerPathDataMetadata pathData }:
                    properties["Data"] = pathData.Data;
                    break;
            }

            return properties;
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

        if (visual is TreeView treeView)
        {
            return new Dictionary<string, string>
            {
                ["__treeItems"] = DesignerTreeItemRuntime.Serialize(treeView),
                ["Opacity"] = treeView.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is Menu menu)
        {
            return new Dictionary<string, string>
            {
                ["__menuItems"] = DesignerMenuItemRuntime.Serialize(menu),
                ["Opacity"] = menu.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is DataGrid dataGrid)
        {
            return new Dictionary<string, string>
            {
                ["__dataGridColumns"] = DesignerDataGridRuntime.Serialize(dataGrid),
                ["AutoGenerateColumns"] = bool.FalseString,
                ["GridLinesVisibility"] = dataGrid.GridLinesVisibility.ToString(),
                ["IsReadOnly"] = dataGrid.IsReadOnly.ToString(),
                ["Opacity"] = dataGrid.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
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

        if (visual is GridSplitter gridSplitter)
        {
            var properties = new Dictionary<string, string>(StringComparer.Ordinal);
            DesignerGridSplitterRuntime.Capture(gridSplitter, properties);
            return properties;
        }

        if (visual is ItemsControl itemsControl && visual.GetType() == typeof(ItemsControl))
        {
            return new Dictionary<string, string>
            {
                ["__items"] = SerializeItemsControl(itemsControl),
                ["Opacity"] = itemsControl.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is SplitView splitView)
        {
            var properties = new Dictionary<string, string>
            {
                ["DisplayMode"] = splitView.DisplayMode.ToString(),
                ["IsPaneOpen"] = splitView.IsPaneOpen.ToString(),
                ["OpenPaneLength"] = splitView.OpenPaneLength.ToString("0.###", CultureInfo.InvariantCulture),
                ["CompactPaneLength"] = splitView.CompactPaneLength.ToString("0.###", CultureInfo.InvariantCulture),
                ["PanePlacement"] = splitView.PanePlacement.ToString(),
                ["UseLightDismissOverlayMode"] = splitView.UseLightDismissOverlayMode.ToString(),
                ["Opacity"] = splitView.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
            if (splitView.PaneBackground is { } paneBackground)
            {
                properties["PaneBackground"] = FormatBrushValue(paneBackground);
            }

            if (splitView.Pane is TextBlock paneText)
            {
                properties["__paneText"] = paneText.Text ?? string.Empty;
            }

            if (splitView.Content is TextBlock contentText)
            {
                properties["__contentText"] = contentText.Text ?? string.Empty;
            }

            return properties;
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

        if (visual is ScrollViewer scrollViewer)
        {
            return new Dictionary<string, string>
            {
                ["__contentText"] = ReadScrollViewerContent(scrollViewer),
                ["Opacity"] = scrollViewer.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is ContentControl contentControl
            && (visual.GetType() == typeof(ContentControl) || visual is UserControl))
        {
            return new Dictionary<string, string>
            {
                ["__contentText"] = ReadContentControlContent(contentControl),
                ["Opacity"] = contentControl.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
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

            if (border.Child is TextBlock)
            {
                properties["__contentText"] = ReadBorderContent(border);
            }

            if (border.IsSet(Border.BackgroundProperty)
                && border.Background is { } background)
            {
                properties["Background"] = FormatBrushValue(background);
            }

            if (border.IsSet(Border.BorderBrushProperty)
                && border.BorderBrush is { } borderBrush)
            {
                properties["BorderBrush"] = FormatBrushValue(borderBrush);
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

        if (visual is DockPanel dockPanel)
        {
            return new Dictionary<string, string>
            {
                ["LastChildFill"] = dockPanel.LastChildFill.ToString(),
                ["Opacity"] = dockPanel.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is WrapPanel wrapPanel)
        {
            return new Dictionary<string, string>
            {
                ["Orientation"] = wrapPanel.Orientation.ToString(),
                ["ItemWidth"] = wrapPanel.ItemWidth.ToString("0.###", CultureInfo.InvariantCulture),
                ["ItemHeight"] = wrapPanel.ItemHeight.ToString("0.###", CultureInfo.InvariantCulture),
                ["ItemSpacing"] = wrapPanel.ItemSpacing.ToString("0.###", CultureInfo.InvariantCulture),
                ["LineSpacing"] = wrapPanel.LineSpacing.ToString("0.###", CultureInfo.InvariantCulture),
                ["ItemsAlignment"] = wrapPanel.ItemsAlignment.ToString(),
                ["Opacity"] = wrapPanel.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is UniformGrid uniformGrid)
        {
            return new Dictionary<string, string>
            {
                ["Rows"] = uniformGrid.Rows.ToString(CultureInfo.InvariantCulture),
                ["Columns"] = uniformGrid.Columns.ToString(CultureInfo.InvariantCulture),
                ["FirstColumn"] = uniformGrid.FirstColumn.ToString(CultureInfo.InvariantCulture),
                ["RowSpacing"] = uniformGrid.RowSpacing.ToString("0.###", CultureInfo.InvariantCulture),
                ["ColumnSpacing"] = uniformGrid.ColumnSpacing.ToString("0.###", CultureInfo.InvariantCulture),
                ["Opacity"] = uniformGrid.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is Canvas canvas)
        {
            return new Dictionary<string, string>
            {
                ["Background"] = canvas.Background is null
                    ? "#00000000"
                    : FormatBrushValue(canvas.Background),
                ["Opacity"] = canvas.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is Grid grid)
        {
            return new Dictionary<string, string>
            {
                ["RowDefinitions"] = DesignerGridDefinitionRuntime.Format(grid.RowDefinitions),
                ["ColumnDefinitions"] = DesignerGridDefinitionRuntime.Format(grid.ColumnDefinitions),
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

    private static int CountTreeItems(IEnumerable<DesignerTreeItemDefinition> definitions)
        => definitions.Sum(definition => 1 + CountTreeItems(definition.Children));

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

    private static string ReadScrollViewerContent(ScrollViewer scrollViewer)
        => scrollViewer.Content is TextBlock textBlock
            ? textBlock.Text ?? string.Empty
            : scrollViewer.Content?.ToString() ?? string.Empty;

    private static void SetScrollViewerContent(ScrollViewer scrollViewer, string content)
        => scrollViewer.Content = new TextBlock { Text = content, Margin = new Thickness(8) };

    private static string ReadContentControlContent(ContentControl contentControl)
        => contentControl.Content is TextBlock textBlock
            ? textBlock.Text ?? string.Empty
            : contentControl.Content?.ToString() ?? string.Empty;

    private static void SetContentControlContent(ContentControl contentControl, string content)
        => contentControl.Content = new TextBlock { Text = content, Margin = new Thickness(8) };

    private static string ReadBorderContent(Border border)
        => border.Child is TextBlock textBlock
            ? textBlock.Text ?? string.Empty
            : border.Child?.ToString() ?? string.Empty;

    private static void SetBorderContent(Border border, string content)
        => border.Child = new TextBlock
        {
            Text = content,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

    private static string ReadTextContent(object? content, string fallback)
        => content is TextBlock textBlock
            ? textBlock.Text ?? fallback
            : content?.ToString() ?? fallback;

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

        if (!DictionaryEquals(left.ColorResources, right.ColorResources))
        {
            return false;
        }

        if (!StylesEqual(left.Styles, right.Styles))
        {
            return false;
        }

        if (!string.Equals(left.SampleDataJson, right.SampleDataJson, StringComparison.Ordinal))
        {
            return false;
        }

        if ((left.RootSettings ?? new DesignerRootSettings())
            != (right.RootSettings ?? new DesignerRootSettings()))
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
                || !string.Equals(a.ParentName, b.ParentName, StringComparison.Ordinal)
                || a.GridRow != b.GridRow
                || a.GridColumn != b.GridColumn
                || a.GridRowSpan != b.GridRowSpan
                || a.GridColumnSpan != b.GridColumnSpan
                || a.StackPanelIndex != b.StackPanelIndex
                || a.StackPanelItemSize != b.StackPanelItemSize
                || a.ParentLayout != b.ParentLayout
                || a.DockPanelIndex != b.DockPanelIndex
                || a.DockPanelDock != b.DockPanelDock
                || a.DockPanelItemSize != b.DockPanelItemSize
                || a.WrapPanelIndex != b.WrapPanelIndex
                || a.UniformGridIndex != b.UniformGridIndex
                || a.CanvasChildIndex != b.CanvasChildIndex
                || a.CanvasChildLeft != b.CanvasChildLeft
                || a.CanvasChildTop != b.CanvasChildTop
                || a.TabIndex != b.TabIndex
                || !string.Equals(a.TabHeader, b.TabHeader, StringComparison.Ordinal)
                || a.SplitViewSlot != b.SplitViewSlot
                || !DictionaryEquals(a.VisualProperties, b.VisualProperties))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HaveSameAppearance(DesignerElementSnapshot expected, DesignerElementSnapshot actual)
    {
        foreach (var propertyName in new[]
        {
            "Background",
            "Foreground",
            "BorderBrush",
            "BorderThickness",
            "CornerRadius",
            "FontSize",
            "FontWeight",
            "Opacity",
            "Classes",
            "Margin",
            "Padding",
            "HorizontalAlignment",
            "VerticalAlignment",
            "MinWidth",
            "MinHeight",
            "MaxWidth",
            "MaxHeight",
        })
        {
            var expectedBinding = ReadSnapshotBindings(expected).FirstOrDefault(binding =>
                string.Equals(binding.PropertyName, propertyName, StringComparison.Ordinal));
            var actualBinding = ReadSnapshotBindings(actual).FirstOrDefault(binding =>
                string.Equals(binding.PropertyName, propertyName, StringComparison.Ordinal));
            if (expectedBinding is not null || actualBinding is not null)
            {
                if (expectedBinding is null
                    || actualBinding is null
                    || !string.Equals(
                        DesignerBindingRuntime.Serialize([expectedBinding]),
                        DesignerBindingRuntime.Serialize([actualBinding]),
                        StringComparison.Ordinal))
                {
                    return false;
                }

                continue;
            }

            var expectedValue = ReadSnapshotProperty(expected, propertyName);
            var actualValue = ReadSnapshotProperty(actual, propertyName);
            if (!string.Equals(expectedValue, actualValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HaveSameBindings(
        DesignerElementSnapshot expected,
        DesignerElementSnapshot actual)
        => string.Equals(
            DesignerBindingRuntime.Serialize(ReadSnapshotBindings(expected)),
            DesignerBindingRuntime.Serialize(ReadSnapshotBindings(actual)),
            StringComparison.Ordinal);

    private static IReadOnlyList<DesignerBindingDefinition> ReadSnapshotBindings(
        DesignerElementSnapshot snapshot)
    {
        if (snapshot.VisualProperties is null
            || !snapshot.VisualProperties.TryGetValue("__bindings", out var json)
            || !DesignerBindingRuntime.TryDeserialize(json, out var definitions))
        {
            return [];
        }

        return definitions;
    }

    private static string? ReadSnapshotProperty(DesignerElementSnapshot snapshot, string propertyName)
        => snapshot.VisualProperties is not null
            && snapshot.VisualProperties.TryGetValue(propertyName, out var value)
                ? value
                : null;

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
        ReadElementNodes(parseRoot, null);

        void ReadElementNodes(
            XElement container,
            DesignerElementSnapshot? parent,
            int forcedTabIndex = -1,
            string? forcedTabHeader = null,
            DesignerSplitViewSlot? forcedSplitViewSlot = null)
        {
            var nextIsLocked = false;
            var stackPanelIndex = 0;
            var dockPanelIndex = 0;
            var wrapPanelIndex = 0;
            var uniformGridIndex = 0;
            var canvasChildIndex = 0;
            var tabItemIndex = 0;
            foreach (var node in container.Nodes())
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
                if (string.Equals(
                        parent?.TypeName,
                        "Avalonia.Controls.SplitView",
                        StringComparison.Ordinal)
                    && string.Equals(tagName, "SplitView.Pane", StringComparison.OrdinalIgnoreCase))
                {
                    ReadElementNodes(
                        child,
                        parent,
                        forcedSplitViewSlot: DesignerSplitViewSlot.Pane);
                    continue;
                }

                if (string.Equals(
                        parent?.TypeName,
                        "Avalonia.Controls.SplitView",
                        StringComparison.Ordinal)
                    && string.Equals(tagName, "SplitView.Content", StringComparison.OrdinalIgnoreCase))
                {
                    ReadElementNodes(
                        child,
                        parent,
                        forcedSplitViewSlot: DesignerSplitViewSlot.Content);
                    continue;
                }

                if (string.Equals(
                        parent?.TypeName,
                        "Avalonia.Controls.TabControl",
                        StringComparison.Ordinal)
                    && string.Equals(
                        tagName,
                        "TabControl.Items",
                        StringComparison.OrdinalIgnoreCase))
                {
                    ReadElementNodes(child, parent);
                    continue;
                }

                if (string.Equals(
                        parent?.TypeName,
                        "Avalonia.Controls.TabControl",
                        StringComparison.Ordinal)
                    && string.Equals(tagName, "TabItem", StringComparison.OrdinalIgnoreCase))
                {
                    var header = child.Attribute("Header")?.Value ?? $"Tab {tabItemIndex + 1}";
                    var contentHost = child.Elements().FirstOrDefault(element =>
                            string.Equals(
                                element.Name.LocalName,
                                "TabItem.Content",
                                StringComparison.OrdinalIgnoreCase))
                        ?? child;
                    ReadElementNodes(contentHost, parent, tabItemIndex++, header);
                    continue;
                }

                if (IsIgnoredContainerTag(tagName) || tagName.Contains('.', StringComparison.Ordinal))
                {
                    continue;
                }

                var parentUsesTextFallback = parent?.TypeName is
                    "Avalonia.Controls.Border"
                    or "Avalonia.Controls.ContentControl"
                    or "Avalonia.Controls.UserControl"
                    or "Avalonia.Controls.ScrollViewer"
                    or "Avalonia.Controls.Expander";
                var hasExplicitName = child.Attributes().Any(attribute =>
                    string.Equals(attribute.Name.LocalName, "Name", StringComparison.Ordinal));
                if (parentUsesTextFallback
                    && string.Equals(tagName, "TextBlock", StringComparison.OrdinalIgnoreCase)
                    && !hasExplicitName)
                {
                    continue;
                }

                if (forcedTabIndex >= 0
                    && string.Equals(tagName, "TextBlock", StringComparison.OrdinalIgnoreCase)
                    && !hasExplicitName)
                {
                    continue;
                }

                if (string.Equals(
                        parent?.TypeName,
                        "Avalonia.Controls.SplitView",
                        StringComparison.Ordinal)
                    && string.Equals(tagName, "TextBlock", StringComparison.OrdinalIgnoreCase)
                    && !hasExplicitName)
                {
                    continue;
                }

                if (!TryResolveTypeName(tagName, out var typeName))
                {
                    warnings.Add($"Unsupported control <{tagName}> was imported as a placeholder.");
                }

                var displayName = ReadImportedDisplayName(
                    child,
                    tagName,
                    snapshots.Count + 1,
                    snapshots,
                    warnings);
                var width = ReadDouble(child, "Width", 120);
                var height = ReadDouble(child, "Height", 40);
                var isStackPanelChild = string.Equals(
                    parent?.TypeName,
                    "Avalonia.Controls.StackPanel",
                    StringComparison.Ordinal);
                var isDockPanelChild = string.Equals(
                    parent?.TypeName,
                    "Avalonia.Controls.DockPanel",
                    StringComparison.Ordinal);
                var isWrapPanelChild = string.Equals(
                    parent?.TypeName,
                    "Avalonia.Controls.WrapPanel",
                    StringComparison.Ordinal);
                var isUniformGridChild = string.Equals(
                    parent?.TypeName,
                    "Avalonia.Controls.Primitives.UniformGrid",
                    StringComparison.Ordinal);
                var isCanvasChild = string.Equals(
                    parent?.TypeName,
                    "Avalonia.Controls.Canvas",
                    StringComparison.Ordinal);
                var isTabControlChild = forcedTabIndex >= 0
                    && string.Equals(
                        parent?.TypeName,
                        "Avalonia.Controls.TabControl",
                        StringComparison.Ordinal);
                var isSplitViewChild = string.Equals(
                    parent?.TypeName,
                    "Avalonia.Controls.SplitView",
                    StringComparison.Ordinal);
                var dockSide = Enum.TryParse<DesignerDockSide>(
                    child.Attribute("DockPanel.Dock")?.Value,
                    ignoreCase: true,
                    out var parsedDockSide)
                    ? parsedDockSide
                    : DesignerDockSide.Left;
                var parentOrientation = parent?.VisualProperties is not null
                    && parent.VisualProperties.TryGetValue("Orientation", out var orientation)
                    ? orientation
                    : "Vertical";
                var parentLayout = parent?.TypeName switch
                {
                    "Avalonia.Controls.Grid" => DesignerParentLayoutKind.Grid,
                    "Avalonia.Controls.StackPanel" => DesignerParentLayoutKind.StackPanel,
                    "Avalonia.Controls.DockPanel" => DesignerParentLayoutKind.DockPanel,
                    "Avalonia.Controls.WrapPanel" => DesignerParentLayoutKind.WrapPanel,
                    "Avalonia.Controls.Primitives.UniformGrid" => DesignerParentLayoutKind.UniformGrid,
                    "Avalonia.Controls.Canvas" => DesignerParentLayoutKind.Canvas,
                    "Avalonia.Controls.TabControl" when isTabControlChild
                        => DesignerParentLayoutKind.TabControl,
                    "Avalonia.Controls.SplitView" when isSplitViewChild
                        => DesignerParentLayoutKind.SplitView,
                    "Avalonia.Controls.Border"
                        or "Avalonia.Controls.ContentControl"
                        or "Avalonia.Controls.UserControl"
                        or "Avalonia.Controls.ScrollViewer"
                        or "Avalonia.Controls.Expander" => DesignerParentLayoutKind.Content,
                    _ => DesignerParentLayoutKind.None,
                };
                var snapshot = new DesignerElementSnapshot(
                    displayName,
                    typeName,
                    ReadDouble(child, "Canvas.Left", 0),
                    ReadDouble(child, "Canvas.Top", 0),
                    width,
                    height,
                    ReadVisualProperties(child, warnings),
                    nextIsLocked,
                    parent?.DisplayName,
                    ReadInt(child, "Grid.Row", 0),
                    ReadInt(child, "Grid.Column", 0),
                    Math.Max(1, ReadInt(child, "Grid.RowSpan", 1)),
                    Math.Max(1, ReadInt(child, "Grid.ColumnSpan", 1)),
                    isStackPanelChild ? stackPanelIndex++ : -1,
                    isStackPanelChild
                        ? string.Equals(parentOrientation, "Horizontal", StringComparison.OrdinalIgnoreCase)
                            ? width
                            : height
                        : 40,
                    parentLayout,
                    isDockPanelChild ? dockPanelIndex++ : -1,
                    dockSide,
                    isDockPanelChild
                        ? dockSide is DesignerDockSide.Top or DesignerDockSide.Bottom
                            ? height
                            : width
                        : 40,
                    isWrapPanelChild ? wrapPanelIndex++ : -1,
                    isUniformGridChild ? uniformGridIndex++ : -1,
                    isCanvasChild ? canvasChildIndex++ : -1,
                    isCanvasChild ? ReadDouble(child, "Canvas.Left", 0) : 0,
                    isCanvasChild ? ReadDouble(child, "Canvas.Top", 0) : 0,
                    isTabControlChild ? forcedTabIndex : -1,
                    isTabControlChild ? forcedTabHeader : null,
                    isSplitViewChild
                        ? forcedSplitViewSlot ?? DesignerSplitViewSlot.Content
                        : DesignerSplitViewSlot.Content);
                snapshots.Add(snapshot);
                nextIsLocked = false;

                if (string.Equals(typeName, "Avalonia.Controls.Grid", StringComparison.Ordinal)
                    || string.Equals(typeName, "Avalonia.Controls.StackPanel", StringComparison.Ordinal)
                    || string.Equals(typeName, "Avalonia.Controls.DockPanel", StringComparison.Ordinal)
                    || string.Equals(typeName, "Avalonia.Controls.WrapPanel", StringComparison.Ordinal)
                    || string.Equals(typeName, "Avalonia.Controls.Primitives.UniformGrid", StringComparison.Ordinal)
                    || string.Equals(typeName, "Avalonia.Controls.Canvas", StringComparison.Ordinal)
                    || string.Equals(typeName, "Avalonia.Controls.TabControl", StringComparison.Ordinal)
                    || string.Equals(typeName, "Avalonia.Controls.SplitView", StringComparison.Ordinal)
                    || string.Equals(typeName, "Avalonia.Controls.Border", StringComparison.Ordinal)
                    || string.Equals(typeName, "Avalonia.Controls.ContentControl", StringComparison.Ordinal)
                    || string.Equals(typeName, "Avalonia.Controls.UserControl", StringComparison.Ordinal)
                    || string.Equals(typeName, "Avalonia.Controls.ScrollViewer", StringComparison.Ordinal)
                    || string.Equals(typeName, "Avalonia.Controls.Expander", StringComparison.Ordinal))
                {
                    ReadElementNodes(child, snapshot);
                }
            }
        }

        var colorResources = ReadColorResources(root, parseRoot, warnings);
        return new DesignerCanvasDocument(
            snapshots,
            ReadCanvasSettings(parseRoot, warnings),
            colorResources,
            ReadDocumentStyles(root, parseRoot, colorResources, warnings),
            ReadSampleDataJson(parseRoot, warnings),
            ReadRootSettings(root, parseRoot, warnings));
    }

    private static IReadOnlyDictionary<string, string> ReadColorResources(
        XElement root,
        XElement parseRoot,
        ICollection<string> warnings)
    {
        var resources = new Dictionary<string, string>(StringComparer.Ordinal);
        var hosts = new List<XElement> { root };
        if (!ReferenceEquals(root, parseRoot))
        {
            hosts.Add(parseRoot);
        }

        var resourceElements = hosts
            .SelectMany(host => host.Elements()
                .Where(element => element.Name.LocalName.EndsWith(".Resources", StringComparison.Ordinal)
                    || string.Equals(element.Name.LocalName, "Resources", StringComparison.Ordinal)))
            .SelectMany(container => container.Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "SolidColorBrush", StringComparison.Ordinal)));

        foreach (var resourceElement in resourceElements)
        {
            var key = resourceElement.Attributes()
                .FirstOrDefault(attribute => string.Equals(attribute.Name.LocalName, "Key", StringComparison.Ordinal))?
                .Value.Trim();
            var value = resourceElement.Attribute("Color")?.Value.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                value = resourceElement.Value.Trim();
            }

            if (string.IsNullOrWhiteSpace(key) || !IsValidControlName(key) || resources.ContainsKey(key))
            {
                warnings.Add("Ignored a color resource with a missing, invalid, or duplicate key.");
                continue;
            }

            try
            {
                resources[key] = FormatBrushValue(Brush.Parse(value));
            }
            catch (FormatException)
            {
                warnings.Add($"Ignored color resource '{key}' because its brush value is invalid.");
            }
        }

        return resources;
    }

    private IReadOnlyList<DesignerStyleDefinition> ReadDocumentStyles(
        XElement root,
        XElement parseRoot,
        IReadOnlyDictionary<string, string> colorResources,
        ICollection<string> warnings)
    {
        var styles = new List<DesignerStyleDefinition>();
        var hosts = new List<XElement> { root };
        if (!ReferenceEquals(root, parseRoot))
        {
            hosts.Add(parseRoot);
        }

        var styleElements = hosts
            .SelectMany(host => host.Elements()
                .Where(element => element.Name.LocalName.EndsWith(".Styles", StringComparison.Ordinal)
                    || string.Equals(element.Name.LocalName, "Styles", StringComparison.Ordinal)))
            .SelectMany(container => container.Elements()
                .Where(element => string.Equals(element.Name.LocalName, "Style", StringComparison.Ordinal)));

        foreach (var styleElement in styleElements)
        {
            var selector = styleElement.Attribute("Selector")?.Value.Trim() ?? string.Empty;
            if (!TryParseSimpleStyleSelector(
                    selector,
                    out var proposedTargetType,
                    out var className,
                    out var pseudoClass)
                || !TryResolveStyleTargetType(proposedTargetType, out var targetType)
                || pseudoClass is not null
                    && !DesignerStyleRuntime.IsSupportedPseudoClass(targetType, pseudoClass))
            {
                warnings.Add($"Ignored unsupported style selector '{selector}'.");
                continue;
            }

            var setters = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var setterElement in styleElement.Elements()
                         .Where(element => string.Equals(element.Name.LocalName, "Setter", StringComparison.Ordinal)))
            {
                var rawPropertyName = setterElement.Attribute("Property")?.Value.Trim() ?? string.Empty;
                var rawValue = setterElement.Attribute("Value")?.Value.Trim() ?? string.Empty;
                if (!TryGetCanonicalStylePropertyName(rawPropertyName, out var propertyName))
                {
                    warnings.Add($"Ignored unsupported setter {selector}.{rawPropertyName}.");
                    continue;
                }

                if (!TryNormalizeStyleSetter(
                        targetType,
                        propertyName,
                        rawValue,
                        colorResources,
                        out var normalizedValue,
                        out var setterError))
                {
                    warnings.Add($"Ignored setter {selector}.{rawPropertyName}: {setterError}");
                    continue;
                }

                if (!setters.TryAdd(propertyName, normalizedValue))
                {
                    warnings.Add($"Ignored duplicate setter {selector}.{propertyName}.");
                }
            }

            if (setters.Count == 0)
            {
                warnings.Add($"Ignored style '{selector}' because it has no supported setters.");
                continue;
            }

            styles.Add(new DesignerStyleDefinition(targetType, className, setters, pseudoClass));
        }

        return styles;
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

    private static string? ReadSampleDataJson(XElement canvas, ICollection<string> warnings)
    {
        var metadata = canvas.Nodes()
            .OfType<XComment>()
            .Select(ReadDesignerMetadata)
            .FirstOrDefault(values => values.ContainsKey("SampleDataBase64"));
        if (metadata is null || !metadata.TryGetValue("SampleDataBase64", out var encoded))
        {
            return null;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            if (DesignerSampleDataRuntime.TryParse(json, out var document, out var error)
                && document is not null)
            {
                return document.Json;
            }

            warnings.Add($"Ignored invalid sample data metadata: {error}");
        }
        catch (FormatException)
        {
            warnings.Add("Ignored invalid sample data metadata: Base64 payload is malformed.");
        }

        return null;
    }

    private static DesignerRootSettings ReadRootSettings(
        XElement root,
        XElement parseRoot,
        ICollection<string> warnings)
    {
        var rootName = root.Name.LocalName;
        var isWindow = string.Equals(rootName, "Window", StringComparison.OrdinalIgnoreCase);
        var isUserControl = string.Equals(rootName, "UserControl", StringComparison.OrdinalIgnoreCase);
        var metadata = parseRoot.Nodes()
            .OfType<XComment>()
            .Select(ReadDesignerMetadata)
            .FirstOrDefault(values =>
                values.ContainsKey("RootKind")
                || values.ContainsKey("WindowTitleBase64")
                || values.ContainsKey("CanResize")
                || values.ContainsKey("StartupLocation")
                || values.ContainsKey("RootMinWidth")
                || values.ContainsKey("RootMinHeight")
                || values.ContainsKey("RootMaxWidth")
                || values.ContainsKey("RootMaxHeight"));

        var kind = isUserControl ? DesignerRootKind.UserControl : DesignerRootKind.Window;
        if (!isWindow && !isUserControl
            && metadata is not null
            && metadata.TryGetValue("RootKind", out var rawKind))
        {
            if (!Enum.TryParse<DesignerRootKind>(rawKind, true, out kind)
                || !Enum.IsDefined(kind))
            {
                warnings.Add($"Ignored unsupported document root kind '{rawKind}'.");
                kind = DesignerRootKind.Window;
            }
        }

        var title = isWindow
            ? root.Attribute("Title")?.Value ?? string.Empty
            : ReadRootTitle(metadata, warnings);
        if (title.Any(char.IsControl))
        {
            warnings.Add("Ignored a Window title containing line breaks or control characters.");
            title = string.Empty;
        }

        var canResize = isWindow
            ? ReadRootBooleanAttribute(root, "CanResize", true, warnings)
            : ReadRootBooleanMetadata(metadata, "CanResize", true, warnings);
        var startupLocation = isWindow
            ? ReadRootStartupLocation(root.Attribute("WindowStartupLocation")?.Value, warnings)
            : ReadRootStartupLocation(
                metadata is not null && metadata.TryGetValue("StartupLocation", out var rawStartup)
                    ? rawStartup
                    : null,
                warnings);
        var minWidth = isWindow || isUserControl
            ? ReadRootConstraintAttribute(root, "MinWidth", 0, false, warnings)
            : ReadRootConstraintMetadata(metadata, "RootMinWidth", 0, false, warnings);
        var minHeight = isWindow || isUserControl
            ? ReadRootConstraintAttribute(root, "MinHeight", 0, false, warnings)
            : ReadRootConstraintMetadata(metadata, "RootMinHeight", 0, false, warnings);
        var maxWidth = isWindow || isUserControl
            ? ReadRootConstraintAttribute(root, "MaxWidth", double.PositiveInfinity, true, warnings)
            : ReadRootConstraintMetadata(metadata, "RootMaxWidth", double.PositiveInfinity, true, warnings);
        var maxHeight = isWindow || isUserControl
            ? ReadRootConstraintAttribute(root, "MaxHeight", double.PositiveInfinity, true, warnings)
            : ReadRootConstraintMetadata(metadata, "RootMaxHeight", double.PositiveInfinity, true, warnings);

        if (minWidth > maxWidth || minHeight > maxHeight)
        {
            warnings.Add("Invalid document root size constraints were reset to defaults.");
            minWidth = 0;
            minHeight = 0;
            maxWidth = double.PositiveInfinity;
            maxHeight = double.PositiveInfinity;
        }

        return kind == DesignerRootKind.UserControl
            ? new DesignerRootSettings(
                kind,
                MinWidth: minWidth,
                MinHeight: minHeight,
                MaxWidth: maxWidth,
                MaxHeight: maxHeight)
            : new DesignerRootSettings(
                kind,
                title,
                canResize,
                startupLocation,
                minWidth,
                minHeight,
                maxWidth,
                maxHeight);
    }

    private static string ReadRootTitle(
        IReadOnlyDictionary<string, string>? metadata,
        ICollection<string> warnings)
    {
        if (metadata is null || !metadata.TryGetValue("WindowTitleBase64", out var encoded))
        {
            return string.Empty;
        }

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        }
        catch (FormatException)
        {
            warnings.Add("Ignored invalid Window title metadata: Base64 payload is malformed.");
            return string.Empty;
        }
    }

    private static bool ReadRootBooleanAttribute(
        XElement root,
        string name,
        bool fallback,
        ICollection<string> warnings)
    {
        var raw = root.Attribute(name)?.Value;
        if (raw is null)
        {
            return fallback;
        }

        if (bool.TryParse(raw, out var value))
        {
            return value;
        }

        warnings.Add($"Ignored invalid document root {name} value '{raw}'.");
        return fallback;
    }

    private static bool ReadRootBooleanMetadata(
        IReadOnlyDictionary<string, string>? metadata,
        string name,
        bool fallback,
        ICollection<string> warnings)
    {
        if (metadata is null || !metadata.TryGetValue(name, out var raw))
        {
            return fallback;
        }

        if (bool.TryParse(raw, out var value))
        {
            return value;
        }

        warnings.Add($"Ignored invalid document root {name} metadata '{raw}'.");
        return fallback;
    }

    private static DesignerWindowStartupLocation ReadRootStartupLocation(
        string? raw,
        ICollection<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return DesignerWindowStartupLocation.Manual;
        }

        if (Enum.TryParse<DesignerWindowStartupLocation>(raw, true, out var value)
            && Enum.IsDefined(value))
        {
            return value;
        }

        warnings.Add($"Ignored unsupported Window startup location '{raw}'.");
        return DesignerWindowStartupLocation.Manual;
    }

    private static double ReadRootConstraintAttribute(
        XElement root,
        string name,
        double fallback,
        bool allowInfinity,
        ICollection<string> warnings)
        => ReadRootConstraint(root.Attribute(name)?.Value, name, fallback, allowInfinity, warnings);

    private static double ReadRootConstraintMetadata(
        IReadOnlyDictionary<string, string>? metadata,
        string name,
        double fallback,
        bool allowInfinity,
        ICollection<string> warnings)
        => ReadRootConstraint(
            metadata is not null && metadata.TryGetValue(name, out var raw) ? raw : null,
            name,
            fallback,
            allowInfinity,
            warnings);

    private static double ReadRootConstraint(
        string? raw,
        string name,
        double fallback,
        bool allowInfinity,
        ICollection<string> warnings)
    {
        if (raw is null)
        {
            return fallback;
        }

        if (allowInfinity
            && string.Equals(raw.Trim(), "Infinity", StringComparison.OrdinalIgnoreCase))
        {
            return double.PositiveInfinity;
        }

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            && double.IsFinite(value)
            && value >= 0)
        {
            return value;
        }

        warnings.Add($"Ignored invalid document root {name} value '{raw}'.");
        return fallback;
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
        var bindings = new List<DesignerBindingDefinition>();

        foreach (var attr in element.Attributes())
        {
            var name = attr.Name.LocalName;
            if (DesignerBindingRuntime.IsBindingExpression(attr.Value))
            {
                if (!DesignerBindingRuntime.IsSupportedProperty(tagName, name))
                {
                    warnings.Add($"Ignored unsupported binding property {tagName}.{name}.");
                }
                else if (!DesignerBindingRuntime.TryParseExpression(name, attr.Value, out var binding))
                {
                    warnings.Add($"Ignored unsupported binding expression on {tagName}.{name}.");
                }
                else
                {
                    bindings.Add(binding);
                }

                continue;
            }

            if (tagName == "Button" && name == "Click")
            {
                if (DesignerButtonRuntime.TryNormalizeClickHandler(
                        attr.Value,
                        out var clickHandler,
                        out var clickHandlerError))
                {
                    map["__clickHandler"] = clickHandler;
                }
                else
                {
                    warnings.Add(
                        $"Ignored Button.Click: {clickHandlerError}");
                }

                continue;
            }

            if (DesignerAccessibilityRuntime.IsSupportedAxamlProperty(name))
            {
                if (DesignerAccessibilityRuntime.TryNormalizeAxamlProperty(
                        name,
                        attr.Value,
                        out var internalKey,
                        out var normalizedValue,
                        out var accessibilityError))
                {
                    map[internalKey] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {accessibilityError}");
                }

                continue;
            }

            if (DesignerInteractionRuntime.IsSupportedAxamlProperty(name))
            {
                if (DesignerInteractionRuntime.TryNormalizeAxamlProperty(
                        name,
                        attr.Value,
                        out var internalKey,
                        out var normalizedValue,
                        out var interactionError))
                {
                    map[internalKey] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {interactionError}");
                }

                continue;
            }

            if (DesignerEffectRuntime.IsSupportedAxamlProperty(name))
            {
                if (DesignerEffectRuntime.TryNormalizeAxamlProperty(
                        name,
                        attr.Value,
                        out var internalKey,
                        out var normalizedValue,
                        out var effectError))
                {
                    map[internalKey] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {effectError}");
                }

                continue;
            }

            if (attr.IsNamespaceDeclaration
                || name is "Canvas.Left" or "Canvas.Top" or "Grid.Row" or "Grid.Column"
                    or "Grid.RowSpan" or "Grid.ColumnSpan" or "DockPanel.Dock"
                    or "Width" or "Height" or "Name")
            {
                continue;
            }

            if (DesignerLayoutRuntime.IsSupportedProperty(tagName, name))
            {
                if (DesignerLayoutRuntime.TryNormalizeProperty(
                        tagName,
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var layoutError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {layoutError}");
                }

                continue;
            }

            if (DesignerTextInputRuntime.IsSupportedProperty(tagName, name))
            {
                if (DesignerTextInputRuntime.TryNormalizeProperty(
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var textInputError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {textInputError}");
                }

                continue;
            }

            if (DesignerMaskedTextBoxRuntime.IsSupportedProperty(tagName, name))
            {
                if (DesignerMaskedTextBoxRuntime.TryNormalizeProperty(
                        tagName,
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var maskedTextBoxError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {maskedTextBoxError}");
                }

                continue;
            }

            if (DesignerSelectableTextBlockRuntime.IsSupportedProperty(tagName, name))
            {
                if (DesignerSelectableTextBlockRuntime.TryNormalizeProperty(
                        tagName,
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var selectableTextBlockError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {selectableTextBlockError}");
                }

                continue;
            }

            if (DesignerSplitViewRuntime.IsSupportedProperty(tagName, name))
            {
                if (DesignerSplitViewRuntime.TryNormalizeProperty(
                        tagName,
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var splitViewError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {splitViewError}");
                }

                continue;
            }

            if (DesignerTabControlRuntime.IsSupportedProperty(tagName, name))
            {
                if (DesignerTabControlRuntime.TryNormalizeProperty(
                        tagName,
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var tabControlError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {tabControlError}");
                }

                continue;
            }

            if (DesignerGridSplitterRuntime.IsSupportedProperty(tagName, name))
            {
                if (DesignerGridSplitterRuntime.TryNormalizeProperty(
                        tagName,
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var gridSplitterError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {gridSplitterError}");
                }

                continue;
            }

            if (DesignerDataGridBehaviorRuntime.IsSupportedProperty(tagName, name))
            {
                if (DesignerDataGridBehaviorRuntime.TryNormalizeProperty(
                        tagName,
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var dataGridBehaviorError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {dataGridBehaviorError}");
                }

                continue;
            }

            if (DesignerSelectionRuntime.IsSupportedProperty(tagName, name))
            {
                if (DesignerSelectionRuntime.TryNormalizeProperty(
                        tagName,
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var selectionError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {selectionError}");
                }

                continue;
            }

            if (DesignerDateTimeRuntime.IsSupportedProperty(tagName, name))
            {
                if (DesignerDateTimeRuntime.TryNormalizeProperty(
                        tagName,
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var dateTimeError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {dateTimeError}");
                }

                continue;
            }

            if (DesignerColorPickerRuntime.IsSupportedProperty(tagName, name))
            {
                if (DesignerColorPickerRuntime.TryNormalizeProperty(
                        tagName,
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var colorPickerError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {colorPickerError}");
                }

                continue;
            }

            if (DesignerAutoCompleteBoxRuntime.IsSupportedProperty(tagName, name))
            {
                if (DesignerAutoCompleteBoxRuntime.TryNormalizeProperty(
                        tagName,
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var autoCompleteBoxError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {autoCompleteBoxError}");
                }

                continue;
            }

            if (DesignerToggleRuntime.IsSupportedProperty(tagName, name))
            {
                if (DesignerToggleRuntime.TryNormalizeProperty(
                        tagName,
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var toggleError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {toggleError}");
                }

                continue;
            }

            if (DesignerContainerBehaviorRuntime.IsSupportedProperty(tagName, name))
            {
                if (DesignerContainerBehaviorRuntime.TryNormalizeProperty(
                        tagName,
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var containerBehaviorError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add(
                        $"Ignored {tagName}.{name}: {containerBehaviorError}");
                }

                continue;
            }

            if (DesignerImageRuntime.IsSupportedProperty(tagName, name))
            {
                if (DesignerImageRuntime.TryNormalizeProperty(
                        tagName,
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var imageError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {imageError}");
                }

                continue;
            }

            if (DesignerButtonRuntime.IsSupportedProperty(tagName, name))
            {
                if (DesignerButtonRuntime.TryNormalizeProperty(
                        tagName,
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var buttonError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {buttonError}");
                }

                continue;
            }

            if (DesignerTypographyRuntime.IsSupportedProperty(tagName, name))
            {
                if (DesignerTypographyRuntime.TryNormalizeProperty(
                        tagName,
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var typographyError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {typographyError}");
                }

                continue;
            }

            if (DesignerTransformRuntime.IsSupportedProperty(name))
            {
                if (DesignerTransformRuntime.TryNormalizeProperty(
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var transformError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {transformError}");
                }

                continue;
            }

            if (DesignerRangeRuntime.IsSupportedProperty(tagName, name))
            {
                if (DesignerRangeRuntime.TryNormalizeProperty(
                        tagName,
                        name,
                        attr.Value,
                        out var canonicalName,
                        out var normalizedValue,
                        out var rangeError))
                {
                    map[canonicalName] = normalizedValue;
                }
                else
                {
                    warnings.Add($"Ignored {tagName}.{name}: {rangeError}");
                }

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

        if (!DesignerLayoutRuntime.TryValidateConstraints(map, out var constraintError))
        {
            warnings.Add($"Ignored {tagName} size constraints: {constraintError}");
            map.Remove("MinWidth");
            map.Remove("MinHeight");
            map.Remove("MaxWidth");
            map.Remove("MaxHeight");
        }

        if (!DesignerRangeRuntime.TryValidateProperties(tagName, map, out var rangeConstraintError))
        {
            warnings.Add($"Ignored {tagName} range properties: {rangeConstraintError}");
            DesignerRangeRuntime.RemoveProperties(tagName, map);
        }

        if ((string.Equals(tagName, "TextBox", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tagName, "MaskedTextBox", StringComparison.OrdinalIgnoreCase))
            && !DesignerTextInputRuntime.TryValidateProperties(map, out var textInputConstraintError))
        {
            warnings.Add($"Ignored {tagName} line constraints: {textInputConstraintError}");
            DesignerTextInputRuntime.RemoveLineProperties(map);
        }

        if (!DesignerMaskedTextBoxRuntime.TryValidateProperties(
                tagName,
                map,
                out var maskedTextBoxConstraintError))
        {
            warnings.Add($"Ignored {tagName} mask properties: {maskedTextBoxConstraintError}");
            DesignerMaskedTextBoxRuntime.RemoveProperties(tagName, map);
        }

        if (!DesignerSelectableTextBlockRuntime.TryValidateProperties(
                tagName,
                map,
                out var selectableTextBlockConstraintError))
        {
            warnings.Add($"Ignored {tagName} selection brush properties: {selectableTextBlockConstraintError}");
            DesignerSelectableTextBlockRuntime.RemoveProperties(tagName, map);
        }

        if (!DesignerSplitViewRuntime.TryValidateProperties(
                tagName,
                map,
                out var splitViewConstraintError))
        {
            warnings.Add($"Ignored {tagName} pane properties: {splitViewConstraintError}");
            DesignerSplitViewRuntime.RemoveProperties(tagName, map);
        }

        if (!DesignerTabControlRuntime.TryValidateProperties(
                tagName,
                map,
                out var tabControlConstraintError))
        {
            warnings.Add($"Ignored {tagName} tab behavior properties: {tabControlConstraintError}");
            DesignerTabControlRuntime.RemoveProperties(tagName, map);
        }

        if (!DesignerGridSplitterRuntime.TryValidateProperties(
                tagName,
                map,
                out var gridSplitterConstraintError))
        {
            warnings.Add($"Ignored {tagName} GridSplitter behavior: {gridSplitterConstraintError}");
            DesignerGridSplitterRuntime.RemoveProperties(map);
        }

        if (!DesignerDataGridBehaviorRuntime.TryValidateProperties(
                tagName,
                map,
                out var dataGridBehaviorConstraintError))
        {
            warnings.Add($"Ignored {tagName} behavior properties: {dataGridBehaviorConstraintError}");
            DesignerDataGridBehaviorRuntime.RemoveProperties(tagName, map);
        }

        var hasItemsSourceBinding = bindings.Any(binding =>
            string.Equals(binding.PropertyName, "ItemsSource", StringComparison.Ordinal));
        int? staticItemCount = hasItemsSourceBinding
            ? null
            : element.Elements().Count(child =>
                !child.Name.LocalName.Contains('.', StringComparison.Ordinal));
        if (!DesignerSelectionRuntime.TryValidateProperties(
                tagName,
                map,
                staticItemCount,
                out var selectionConstraintError))
        {
            warnings.Add($"Ignored {tagName}.SelectedIndex: {selectionConstraintError}");
            DesignerSelectionRuntime.RemoveSelectedIndex(map);
        }

        if (!DesignerDateTimeRuntime.TryValidateProperties(
                tagName,
                map,
                out var dateTimeConstraintError))
        {
            warnings.Add($"Ignored {tagName} date/time constraints: {dateTimeConstraintError}");
            DesignerDateTimeRuntime.RemoveConstraintProperties(tagName, map);
        }

        if (!DesignerColorPickerRuntime.TryValidateProperties(
                tagName,
                map,
                out var colorPickerConstraintError))
        {
            warnings.Add($"Ignored {tagName} color picker constraints: {colorPickerConstraintError}");
            DesignerColorPickerRuntime.RemoveProperties(tagName, map);
        }

        if (!DesignerAutoCompleteBoxRuntime.TryValidateProperties(
                tagName,
                map,
                out var autoCompleteBoxConstraintError))
        {
            warnings.Add($"Ignored {tagName} autocomplete constraints: {autoCompleteBoxConstraintError}");
            DesignerAutoCompleteBoxRuntime.RemoveProperties(tagName, map);
        }

        if (!DesignerToggleRuntime.TryValidateProperties(
                tagName,
                map,
                out var toggleConstraintError))
        {
            warnings.Add($"Ignored {tagName} toggle constraints: {toggleConstraintError}");
            DesignerToggleRuntime.RemoveConstraintProperties(map);
        }

        if (string.Equals(element.Name.LocalName, "Grid", StringComparison.OrdinalIgnoreCase))
        {
            map.TryAdd("RowDefinitions", string.Empty);
            map.TryAdd("ColumnDefinitions", string.Empty);
        }
        else if ((string.Equals(element.Name.LocalName, "ComboBox", StringComparison.OrdinalIgnoreCase)
                || string.Equals(element.Name.LocalName, "ListBox", StringComparison.OrdinalIgnoreCase)
                || string.Equals(element.Name.LocalName, "ItemsControl", StringComparison.OrdinalIgnoreCase)
                || string.Equals(element.Name.LocalName, "AutoCompleteBox", StringComparison.OrdinalIgnoreCase))
            && !hasItemsSourceBinding)
        {
            map["__items"] = SerializeItems(element);
        }
        else if (string.Equals(element.Name.LocalName, "TreeView", StringComparison.OrdinalIgnoreCase)
            && !hasItemsSourceBinding)
        {
            map["__treeItems"] = DesignerTreeItemRuntime.Serialize(element);
        }
        else if (string.Equals(element.Name.LocalName, "Menu", StringComparison.OrdinalIgnoreCase))
        {
            map["__menuItems"] = DesignerMenuItemRuntime.Serialize(element);
        }
        else if (string.Equals(element.Name.LocalName, "DataGrid", StringComparison.OrdinalIgnoreCase))
        {
            map["__dataGridColumns"] = DesignerDataGridRuntime.Serialize(element);
        }
        else if (string.Equals(element.Name.LocalName, "TabControl", StringComparison.OrdinalIgnoreCase))
        {
            map["__tabs"] = SerializeTabHeaders(element);
        }
        else if (string.Equals(element.Name.LocalName, "SplitView", StringComparison.OrdinalIgnoreCase))
        {
            var paneHost = element.Elements().FirstOrDefault(child =>
                string.Equals(child.Name.LocalName, "SplitView.Pane", StringComparison.OrdinalIgnoreCase));
            map["__paneText"] = paneHost?.DescendantsAndSelf()
                .FirstOrDefault(child => string.Equals(
                    child.Name.LocalName,
                    "TextBlock",
                    StringComparison.OrdinalIgnoreCase))?
                .Attribute("Text")?.Value ?? string.Empty;
            var contentHost = element.Elements().FirstOrDefault(child =>
                string.Equals(child.Name.LocalName, "SplitView.Content", StringComparison.OrdinalIgnoreCase));
            var contentText = contentHost?.DescendantsAndSelf()
                    .FirstOrDefault(child => string.Equals(
                        child.Name.LocalName,
                        "TextBlock",
                        StringComparison.OrdinalIgnoreCase))
                ?? element.Elements().FirstOrDefault(child =>
                    string.Equals(child.Name.LocalName, "TextBlock", StringComparison.OrdinalIgnoreCase));
            map["__contentText"] = contentText?.Attribute("Text")?.Value ?? string.Empty;
        }
        else if (string.Equals(element.Name.LocalName, "Expander", StringComparison.OrdinalIgnoreCase)
            || string.Equals(element.Name.LocalName, "ScrollViewer", StringComparison.OrdinalIgnoreCase))
        {
            map["__contentText"] = element.Elements()
                .FirstOrDefault(child => string.Equals(child.Name.LocalName, "TextBlock", StringComparison.OrdinalIgnoreCase))?
                .Attribute("Text")?.Value ?? string.Empty;
        }
        else if (string.Equals(element.Name.LocalName, "Border", StringComparison.OrdinalIgnoreCase))
        {
            var content = element.Elements()
                .FirstOrDefault(child => string.Equals(child.Name.LocalName, "TextBlock", StringComparison.OrdinalIgnoreCase));
            if (content is not null)
            {
                map["__contentText"] = content.Attribute("Text")?.Value ?? string.Empty;
            }
        }

        if (bindings.Count > 0)
        {
            map["__bindings"] = DesignerBindingRuntime.Serialize(bindings);
        }

        return map.Count == 0 ? null : map;
    }

    private static bool IsSupportedVisualProperty(string tagName, string propertyName)
    {
        if (DesignerLayoutRuntime.IsSupportedProperty(tagName, propertyName))
        {
            return true;
        }

        if (DesignerSelectionRuntime.IsSupportedProperty(tagName, propertyName))
        {
            return true;
        }

        if (DesignerDateTimeRuntime.IsSupportedProperty(tagName, propertyName))
        {
            return true;
        }

        if (DesignerColorPickerRuntime.IsSupportedProperty(tagName, propertyName))
        {
            return true;
        }

        if (DesignerAutoCompleteBoxRuntime.IsSupportedProperty(tagName, propertyName))
        {
            return true;
        }

        if (DesignerMaskedTextBoxRuntime.IsSupportedProperty(tagName, propertyName))
        {
            return true;
        }

        if (DesignerSelectableTextBlockRuntime.IsSupportedProperty(tagName, propertyName))
        {
            return true;
        }

        if (DesignerToggleRuntime.IsSupportedProperty(tagName, propertyName))
        {
            return true;
        }

        if (DesignerContainerBehaviorRuntime.IsSupportedProperty(tagName, propertyName))
        {
            return true;
        }

        if (DesignerTabControlRuntime.IsSupportedProperty(tagName, propertyName))
        {
            return true;
        }

        if (DesignerGridSplitterRuntime.IsSupportedProperty(tagName, propertyName))
        {
            return true;
        }

        if (DesignerDataGridBehaviorRuntime.IsSupportedProperty(tagName, propertyName))
        {
            return true;
        }

        if (DesignerImageRuntime.IsSupportedProperty(tagName, propertyName))
        {
            return true;
        }

        if (DesignerButtonRuntime.IsSupportedProperty(tagName, propertyName))
        {
            return true;
        }

        if (propertyName is "Opacity" or "Classes")
        {
            return true;
        }

        if (propertyName is "Background" or "Foreground" or "BorderBrush" or "BorderThickness" or "CornerRadius"
            && SupportsTemplatedAppearance(tagName))
        {
            return true;
        }

        return tagName switch
        {
            "Button" => false,
            "TextBox" or "MaskedTextBox" => propertyName is "Text" or "Watermark" or "PasswordChar"
                or "RevealPassword" or "AcceptsReturn" or "AcceptsTab" or "TextWrapping"
                or "TextAlignment" or "IsReadOnly" or "MaxLength" or "MinLines"
                or "MaxLines" or "UseFloatingWatermark" or "IsUndoEnabled" or "UndoLimit"
                or "ClearSelectionOnLostFocus" or "IsInactiveSelectionHighlightEnabled",
            "TextBlock" or "SelectableTextBlock" => propertyName is "Text" or "FontSize" or "FontWeight" or "Background" or "Foreground",
            "Label" => propertyName is "Content" or "Target",
            "Image" => false,
            "Rectangle" => IsSupportedShapeProperty(propertyName)
                || propertyName is "RadiusX" or "RadiusY",
            "Ellipse" => IsSupportedShapeProperty(propertyName),
            "Line" => IsSupportedShapeProperty(propertyName)
                || propertyName is "StartPoint" or "EndPoint",
            "Path" => IsSupportedShapeProperty(propertyName)
                || propertyName == "Data",
            "CheckBox" or "RadioButton" or "ToggleSwitch" or "ToggleButton" => false,
            "ComboBox" or "ListBox" or "ItemsControl" or "TreeView" => false,
            "Menu" => false,
            "DataGrid" => propertyName is "AutoGenerateColumns" or "GridLinesVisibility" or "IsReadOnly",
            "Slider" => propertyName is "Minimum" or "Maximum" or "Value" or "SmallChange"
                or "LargeChange" or "Orientation" or "IsDirectionReversed" or "TickFrequency"
                or "TickPlacement" or "IsSnapToTickEnabled",
            "ProgressBar" => propertyName is "Minimum" or "Maximum" or "Value" or "Orientation"
                or "IsIndeterminate" or "ShowProgressText" or "ProgressTextFormat",
            "DatePicker" or "CalendarDatePicker" or "Calendar"
                or "TimePicker" => false,
            "ColorPicker" => false,
            "NumericUpDown" => propertyName is "Minimum" or "Maximum" or "Increment" or "Value"
                or "FormatString" or "ClipValueToMinMax" or "AllowSpin"
                or "ShowButtonSpinner" or "ButtonSpinnerLocation",
            "TabControl" => propertyName == "SelectedIndex",
            "GridSplitter" => propertyName is "ResizeDirection" or "ResizeBehavior"
                or "ShowsPreview" or "KeyboardIncrement" or "DragIncrement",
            "SplitView" => propertyName is "DisplayMode" or "IsPaneOpen" or "OpenPaneLength"
                or "CompactPaneLength" or "PanePlacement" or "PaneBackground"
                or "UseLightDismissOverlayMode",
            "Expander" or "ScrollViewer" => false,
            "Border" => propertyName is "Background" or "BorderBrush" or "BorderThickness" or "CornerRadius",
            "Grid" => propertyName is "RowDefinitions" or "ColumnDefinitions" or "ShowGridLines",
            "StackPanel" => propertyName is "Orientation" or "Spacing",
            "DockPanel" => propertyName == "LastChildFill",
            "WrapPanel" => propertyName is "Orientation" or "ItemWidth" or "ItemHeight"
                or "ItemSpacing" or "LineSpacing" or "ItemsAlignment",
            "UniformGrid" => propertyName is "Rows" or "Columns" or "FirstColumn"
                or "RowSpacing" or "ColumnSpacing",
            "Canvas" => propertyName == "Background",
            _ => false,
        };
    }

    private static bool SupportsTemplatedAppearance(string tagName)
        => tagName is "Button" or "TextBox" or "MaskedTextBox" or "AutoCompleteBox" or "SelectableTextBlock" or "Label" or "CheckBox" or "RadioButton"
            or "ToggleSwitch" or "ToggleButton" or "ComboBox" or "ListBox" or "ItemsControl" or "TreeView" or "Menu" or "Slider"
            or "ProgressBar" or "DatePicker" or "CalendarDatePicker"
            or "Calendar" or "ColorPicker" or "TimePicker"
            or "NumericUpDown" or "TabControl" or "GridSplitter" or "Expander" or "ScrollViewer" or "DataGrid";

    private static bool IsSupportedShapeProperty(string propertyName)
        => propertyName is "Fill" or "Stroke" or "StrokeThickness" or "Stretch"
            or "StrokeDashArray" or "StrokeDashOffset" or "StrokeLineCap"
            or "StrokeJoin" or "StrokeMiterLimit";

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

    private static bool StylesEqual(
        IReadOnlyList<DesignerStyleDefinition>? left,
        IReadOnlyList<DesignerStyleDefinition>? right)
    {
        var leftStyles = left ?? Array.Empty<DesignerStyleDefinition>();
        var rightStyles = right ?? Array.Empty<DesignerStyleDefinition>();
        if (leftStyles.Count != rightStyles.Count)
        {
            return false;
        }

        for (var index = 0; index < leftStyles.Count; index++)
        {
            if (!string.Equals(leftStyles[index].TargetType, rightStyles[index].TargetType, StringComparison.Ordinal)
                || !string.Equals(leftStyles[index].ClassName, rightStyles[index].ClassName, StringComparison.Ordinal)
                || !string.Equals(leftStyles[index].PseudoClass, rightStyles[index].PseudoClass, StringComparison.Ordinal)
                || !DictionaryEquals(leftStyles[index].Setters, rightStyles[index].Setters))
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

    private static int ReadInt(XElement element, string attributeName, int fallback)
    {
        var raw = element.Attribute(attributeName)?.Value;
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
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
            HistoryActionType.EditAxamlSource => "edit AXAML source",
            HistoryActionType.EditSampleData => "edit sample data",
            HistoryActionType.EditRootProperties => "edit root properties",
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

    private bool TryParseDocumentStylesText(
        string text,
        out IReadOnlyList<DesignerStyleDefinition> styles,
        out string error)
    {
        var parsedStyles = new List<DesignerStyleDefinition>();
        string? currentTargetType = null;
        string? currentClassName = null;
        string? currentPseudoClass = null;
        Dictionary<string, string>? currentSetters = null;
        var parseError = string.Empty;

        bool CommitCurrentStyle()
        {
            if (currentTargetType is null || currentClassName is null || currentSetters is null)
            {
                return true;
            }

            if (currentSetters.Count == 0)
            {
                parseError = $"Style {currentTargetType}.{currentClassName} must contain at least one setter.";
                return false;
            }

            parsedStyles.Add(new DesignerStyleDefinition(
                currentTargetType,
                currentClassName,
                new Dictionary<string, string>(currentSetters, StringComparer.Ordinal),
                currentPseudoClass));
            return true;
        }

        error = string.Empty;
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (string.IsNullOrWhiteSpace(line)
                || line.StartsWith("//", StringComparison.Ordinal)
                || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                if (!CommitCurrentStyle())
                {
                    styles = parsedStyles;
                    error = parseError;
                    return false;
                }

                var selector = line[1..^1].Trim();
                if (!TryParseSimpleStyleSelector(
                        selector,
                        out var targetType,
                        out var className,
                        out currentPseudoClass)
                    || !TryResolveStyleTargetType(targetType, out currentTargetType)
                    || currentPseudoClass is not null
                        && !DesignerStyleRuntime.IsSupportedPseudoClass(currentTargetType, currentPseudoClass))
                {
                    styles = parsedStyles;
                    error = $"Style line {index + 1} must use a supported selector such as [Button.primary:pointerover].";
                    return false;
                }

                currentClassName = className;
                currentSetters = new Dictionary<string, string>(StringComparer.Ordinal);
                continue;
            }

            if (currentSetters is null || currentTargetType is null || currentClassName is null)
            {
                styles = parsedStyles;
                error = $"Style line {index + 1} must follow a [Control.class] section.";
                return false;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0 || separator == line.Length - 1)
            {
                styles = parsedStyles;
                error = $"Style line {index + 1} must use Property = Value format.";
                return false;
            }

            var rawPropertyName = line[..separator].Trim();
            var rawValue = line[(separator + 1)..].Trim();
            if (!TryGetCanonicalStylePropertyName(rawPropertyName, out var propertyName))
            {
                styles = parsedStyles;
                error = $"Style line {index + 1}: setter '{rawPropertyName}' is not supported.";
                return false;
            }

            if (!TryNormalizeStyleSetter(
                    currentTargetType,
                    propertyName,
                    rawValue,
                    _colorResources,
                    out var normalizedValue,
                    out error))
            {
                styles = parsedStyles;
                error = $"Style line {index + 1}: {error}";
                return false;
            }

            if (!currentSetters.TryAdd(propertyName, normalizedValue))
            {
                styles = parsedStyles;
                error = $"Style line {index + 1} duplicates {propertyName}.";
                return false;
            }
        }

        if (!CommitCurrentStyle())
        {
            styles = parsedStyles;
            error = parseError;
            return false;
        }

        styles = parsedStyles;
        error = string.Empty;
        return true;
    }

    private bool TryResolveStyleTargetType(string proposedTargetType, out string targetType)
    {
        var normalized = proposedTargetType.Trim();
        foreach (var definition in _componentCatalog.GetAll())
        {
            var shortName = definition.AvaloniaTypeName[(definition.AvaloniaTypeName.LastIndexOf('.') + 1)..];
            if (string.Equals(shortName, normalized, StringComparison.OrdinalIgnoreCase)
                || string.Equals(definition.AvaloniaTypeName, normalized, StringComparison.OrdinalIgnoreCase))
            {
                targetType = shortName;
                return true;
            }
        }

        targetType = string.Empty;
        return false;
    }

    private static bool TryParseSimpleStyleSelector(
        string selector,
        out string targetType,
        out string className,
        out string? pseudoClass)
    {
        pseudoClass = null;
        var baseSelector = selector;
        var pseudoSeparator = selector.IndexOf(':');
        if (pseudoSeparator >= 0)
        {
            if (pseudoSeparator == selector.Length - 1
                || selector.IndexOf(':', pseudoSeparator + 1) >= 0)
            {
                targetType = string.Empty;
                className = string.Empty;
                return false;
            }

            baseSelector = selector[..pseudoSeparator];
            pseudoClass = selector[(pseudoSeparator + 1)..];
            if (!IsValidPseudoClassName(pseudoClass))
            {
                targetType = string.Empty;
                className = string.Empty;
                pseudoClass = null;
                return false;
            }
        }

        var separator = baseSelector.IndexOf('.');
        if (separator <= 0
            || separator == baseSelector.Length - 1
            || baseSelector.IndexOf('.', separator + 1) >= 0
            || baseSelector.Any(char.IsWhiteSpace))
        {
            targetType = string.Empty;
            className = string.Empty;
            pseudoClass = null;
            return false;
        }

        targetType = baseSelector[..separator].Trim();
        className = baseSelector[(separator + 1)..].Trim();
        return IsValidStyleClassName(className);
    }

    private static bool TryGetCanonicalStylePropertyName(string proposedName, out string propertyName)
    {
        propertyName = proposedName.Trim().ToLowerInvariant() switch
        {
            "opacity" => "Opacity",
            "background" => "Background",
            "foreground" => "Foreground",
            "borderbrush" => "BorderBrush",
            "borderthickness" => "BorderThickness",
            "cornerradius" => "CornerRadius",
            "fontsize" => "FontSize",
            "fontweight" => "FontWeight",
            _ => string.Empty,
        };
        return propertyName.Length > 0;
    }

    private static bool TryNormalizeStyleSetter(
        string targetType,
        string propertyName,
        string rawValue,
        IReadOnlyDictionary<string, string> colorResources,
        out string normalizedValue,
        out string error)
    {
        normalizedValue = string.Empty;
        if (!DesignerStyleRuntime.IsSupportedProperty(targetType, propertyName))
        {
            error = $"{targetType}.{propertyName} is not supported by the designer style editor.";
            return false;
        }

        try
        {
            if (DesignerStyleRuntime.IsBrushProperty(propertyName))
            {
                if (DesignerResourceReferenceMetadata.TryParseExpression(rawValue, out var resourceKey))
                {
                    if (!colorResources.ContainsKey(resourceKey))
                    {
                        error = $"Color resource '{resourceKey}' does not exist.";
                        return false;
                    }

                    normalizedValue = DesignerResourceReferenceMetadata.FormatExpression(resourceKey);
                }
                else
                {
                    normalizedValue = FormatBrushValue(Brush.Parse(rawValue));
                }

                error = string.Empty;
                return true;
            }

            switch (propertyName)
            {
                case "Opacity":
                    if (!double.TryParse(
                            rawValue,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var opacity)
                        || opacity is < 0 or > 1)
                    {
                        error = "Opacity must be a number from 0 to 1.";
                        return false;
                    }

                    normalizedValue = opacity.ToString("0.###", CultureInfo.InvariantCulture);
                    break;
                case "BorderThickness":
                    normalizedValue = Thickness.Parse(rawValue).ToString();
                    break;
                case "CornerRadius":
                    normalizedValue = CornerRadius.Parse(rawValue).ToString();
                    break;
                case "FontSize":
                    if (!double.TryParse(
                            rawValue,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var fontSize)
                        || fontSize is < 8 or > 96)
                    {
                        error = "FontSize must be a number from 8 to 96.";
                        return false;
                    }

                    normalizedValue = fontSize.ToString("0.###", CultureInfo.InvariantCulture);
                    break;
                case "FontWeight":
                    if (!TryParseTextWeight(rawValue, out var fontWeight))
                    {
                        error = "FontWeight must be Regular, SemiBold, or Bold.";
                        return false;
                    }

                    normalizedValue = fontWeight == FontWeight.Bold
                        ? "Bold"
                        : fontWeight == FontWeight.SemiBold
                            ? "SemiBold"
                            : "Regular";
                    break;
                default:
                    error = $"Setter {propertyName} is not supported.";
                    return false;
            }
        }
        catch (FormatException)
        {
            error = $"{propertyName} contains an invalid value.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private bool TryResolveAppearanceResources(
        IReadOnlyDictionary<string, string> appearance,
        out IReadOnlyDictionary<string, string> resolvedAppearance,
        out IReadOnlyDictionary<string, string> resourceReferences,
        out string error)
    {
        var resolved = appearance.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var references = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var propertyName in new[] { "Background", "Foreground", "BorderBrush" })
        {
            if (!appearance.TryGetValue(propertyName, out var value)
                || !DesignerResourceReferenceMetadata.TryParseExpression(value, out var resourceKey))
            {
                continue;
            }

            if (!_colorResources.TryGetValue(resourceKey, out var resourceValue))
            {
                resolvedAppearance = resolved;
                resourceReferences = references;
                error = $"Color resource '{resourceKey}' does not exist.";
                return false;
            }

            resolved[propertyName] = resourceValue;
            references[propertyName] = resourceKey;
        }

        resolvedAppearance = resolved;
        resourceReferences = references;
        error = string.Empty;
        return true;
    }

    private void RefreshResourceBackedAppearance()
    {
        foreach (var element in Canvas.Elements)
        {
            DesignerStyleApplicationMetadata.BeginProgrammaticUpdate(element.Visual);
            try
            {
                foreach (var pair in DesignerResourceReferenceMetadata.GetReferences(element.Visual))
                {
                    if (_colorResources.TryGetValue(pair.Value, out var brushValue))
                    {
                        TryApplyAppearanceBrush(element.Visual, pair.Key, Brush.Parse(brushValue));
                    }
                }
            }
            finally
            {
                DesignerStyleApplicationMetadata.EndProgrammaticUpdate(element.Visual);
            }
        }
    }

    private static bool SupportsAppearanceBrush(Control visual, string propertyName)
        => propertyName switch
        {
            "Background" => visual is Avalonia.Controls.Primitives.TemplatedControl or Border or TextBlock,
            "Foreground" => visual is Avalonia.Controls.Primitives.TemplatedControl or TextBlock,
            "BorderBrush" => visual is Avalonia.Controls.Primitives.TemplatedControl or Border,
            "Fill" or "Stroke" => visual is Shape,
            _ => false,
        };

    private static bool TryApplyAppearanceBrush(Control visual, string propertyName, IBrush? brush)
    {
        switch (propertyName)
        {
            case "Background" when visual is Avalonia.Controls.Primitives.TemplatedControl templated:
                templated.Background = brush;
                return true;
            case "Background" when visual is Border border:
                border.Background = brush;
                return true;
            case "Background" when visual is TextBlock textBlock:
                textBlock.Background = brush;
                return true;
            case "Foreground" when visual is Avalonia.Controls.Primitives.TemplatedControl templated:
                templated.Foreground = brush;
                return true;
            case "Foreground" when visual is TextBlock textBlock:
                textBlock.Foreground = brush;
                return true;
            case "BorderBrush" when visual is Avalonia.Controls.Primitives.TemplatedControl templated:
                templated.BorderBrush = brush;
                return true;
            case "BorderBrush" when visual is Border border:
                border.BorderBrush = brush;
                return true;
            case "Fill" when visual is Shape shape:
                shape.Fill = brush;
                return true;
            case "Stroke" when visual is Shape shape:
                shape.Stroke = brush;
                return true;
            default:
                return false;
        }
    }

    private static bool TryReadOptionalBrush(
        IReadOnlyDictionary<string, string> values,
        string propertyName,
        out bool hasValue,
        out IBrush? brush,
        out string error)
    {
        hasValue = values.TryGetValue(propertyName, out var rawValue);
        brush = null;
        error = string.Empty;
        if (!hasValue || string.IsNullOrWhiteSpace(rawValue))
        {
            return true;
        }

        try
        {
            brush = Brush.Parse(rawValue);
            return true;
        }
        catch (FormatException)
        {
            error = $"{propertyName} must be a valid Avalonia brush, for example #2563EB.";
            return false;
        }
    }

    private static bool TryReadThickness(
        IReadOnlyDictionary<string, string> values,
        string propertyName,
        out bool hasValue,
        out Thickness thickness,
        out string error)
    {
        hasValue = values.TryGetValue(propertyName, out var rawValue);
        thickness = default;
        error = string.Empty;
        if (!hasValue || string.IsNullOrWhiteSpace(rawValue))
        {
            return true;
        }

        try
        {
            thickness = Thickness.Parse(rawValue);
            return true;
        }
        catch (FormatException)
        {
            error = $"{propertyName} must use one, two, or four numeric values.";
            return false;
        }
    }

    private static bool TryReadCornerRadius(
        IReadOnlyDictionary<string, string> values,
        string propertyName,
        out bool hasValue,
        out CornerRadius cornerRadius,
        out string error)
    {
        hasValue = values.TryGetValue(propertyName, out var rawValue);
        cornerRadius = default;
        error = string.Empty;
        if (!hasValue || string.IsNullOrWhiteSpace(rawValue))
        {
            return true;
        }

        try
        {
            cornerRadius = CornerRadius.Parse(rawValue);
            return true;
        }
        catch (FormatException)
        {
            error = $"{propertyName} must use one or four numeric values.";
            return false;
        }
    }

    private static bool TryReadOptionalDouble(
        IReadOnlyDictionary<string, string> values,
        string propertyName,
        double minimum,
        out bool hasValue,
        out double value,
        out string error)
    {
        hasValue = values.TryGetValue(propertyName, out var rawValue);
        value = 0;
        error = string.Empty;
        if (!hasValue)
        {
            return true;
        }

        if (!double.TryParse(
                rawValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value)
            || !double.IsFinite(value)
            || value < minimum)
        {
            error = double.IsNegativeInfinity(minimum)
                ? $"{propertyName} must be a finite number."
                : $"{propertyName} must be a finite number greater than or equal to {minimum:0.###}.";
            return false;
        }

        return true;
    }

    private static bool TryReadOptionalEnum<T>(
        IReadOnlyDictionary<string, string> values,
        string propertyName,
        out bool hasValue,
        out T value,
        out string error)
        where T : struct, Enum
    {
        hasValue = values.TryGetValue(propertyName, out var rawValue);
        value = default;
        error = string.Empty;
        if (!hasValue)
        {
            return true;
        }

        if (!Enum.TryParse<T>(rawValue, ignoreCase: true, out value)
            || !Enum.IsDefined(value))
        {
            error = $"{propertyName} must be one of: {string.Join(", ", Enum.GetNames<T>())}.";
            return false;
        }

        return true;
    }

    private static bool TryReadDoubleList(
        IReadOnlyDictionary<string, string> values,
        string propertyName,
        out bool hasValue,
        out IReadOnlyList<double> parsedValues,
        out string error)
    {
        hasValue = values.TryGetValue(propertyName, out var rawValue);
        var result = new List<double>();
        parsedValues = result;
        error = string.Empty;
        if (!hasValue || string.IsNullOrWhiteSpace(rawValue))
        {
            return true;
        }

        foreach (var token in rawValue.Split(
                     [',', ' ', '\t'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!double.TryParse(
                    token,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var parsed)
                || !double.IsFinite(parsed)
                || parsed < 0)
            {
                error = $"{propertyName} must contain non-negative numbers separated by commas.";
                return false;
            }

            result.Add(parsed);
        }

        return true;
    }

    private static bool TryReadOptionalPoint(
        IReadOnlyDictionary<string, string> values,
        string propertyName,
        out bool hasValue,
        out Point point,
        out string error)
    {
        hasValue = values.TryGetValue(propertyName, out var rawValue);
        point = default;
        error = string.Empty;
        if (!hasValue)
        {
            return true;
        }

        try
        {
            point = Point.Parse(rawValue ?? string.Empty);
            if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
            {
                throw new FormatException();
            }

            return true;
        }
        catch (FormatException)
        {
            error = $"{propertyName} must use X,Y coordinates, for example 0,60.";
            return false;
        }
    }

    private static string FormatPoint(Point point)
        => $"{point.X.ToString("0.###", CultureInfo.InvariantCulture)},{point.Y.ToString("0.###", CultureInfo.InvariantCulture)}";

    private static bool IsValidControlName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !(char.IsLetter(name[0]) || name[0] == '_'))
        {
            return false;
        }

        return name.All(character => char.IsLetterOrDigit(character) || character == '_');
    }

    private static bool IsValidStyleClassName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)
            || !(char.IsLetter(name[0]) || name[0] == '_'))
        {
            return false;
        }

        return name.All(character => char.IsLetterOrDigit(character) || character is '_' or '-');
    }

    private static bool IsValidPseudoClassName(string name)
        => !string.IsNullOrWhiteSpace(name)
            && char.IsLetter(name[0])
            && name.All(character => char.IsLetterOrDigit(character) || character == '-');

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

    private static IReadOnlyList<DesignerStyleDefinition> CloneStyles(
        IEnumerable<DesignerStyleDefinition> styles)
        => styles.Select(style => style with
        {
            Setters = new Dictionary<string, string>(style.Setters, StringComparer.Ordinal),
        }).ToList();

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
                    RevealPassword: textBox.RevealPassword,
                    AcceptsReturn: textBox.AcceptsReturn,
                    TextWrapping: textBox.TextWrapping.ToString()),
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

    private static string SerializeItemsControl(ItemsControl itemsControl)
        => JsonSerializer.Serialize(ReadItems(itemsControl));

    private static string SerializeAutoCompleteBoxItems(AutoCompleteBox autoCompleteBox)
        => JsonSerializer.Serialize(ReadAutoCompleteBoxItems(autoCompleteBox));

    private static IReadOnlyList<string> ReadAutoCompleteBoxItems(AutoCompleteBox autoCompleteBox)
        => autoCompleteBox.ItemsSource is System.Collections.IEnumerable source
            ? source.Cast<object?>().Select(item => item?.ToString() ?? string.Empty).ToList()
            : [];

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
                            : null,
                        AcceptsReturn: bool.TryParse(child.Attribute("AcceptsReturn")?.Value, out var acceptsReturn)
                            ? acceptsReturn
                            : null,
                        TextWrapping: child.Attribute("TextWrapping")?.Value));
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
        var itemElements = itemsControlElement.Elements()
            .Where(element => !element.Name.LocalName.Contains('.', StringComparison.Ordinal))
            .ToList();
        if (itemElements.Count == 0)
        {
            itemElements = itemsControlElement.Elements()
                .Where(element => string.Equals(
                    element.Name.LocalName,
                    $"{itemsControlElement.Name.LocalName}.Items",
                    StringComparison.OrdinalIgnoreCase)
                    || string.Equals(
                        element.Name.LocalName,
                        $"{itemsControlElement.Name.LocalName}.ItemsSource",
                        StringComparison.OrdinalIgnoreCase))
                .SelectMany(element => element
                    .Descendants()
                    .Where(child => string.Equals(child.Name.LocalName, "String", StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var items = itemElements
            .Select(element => element.Attribute("Content")?.Value ?? element.Value)
            .ToList();

        return JsonSerializer.Serialize(items);
    }

    private static string SerializeTabHeaders(XElement tabControlElement)
    {
        var itemsHost = tabControlElement.Elements().FirstOrDefault(element =>
                string.Equals(
                    element.Name.LocalName,
                    "TabControl.Items",
                    StringComparison.OrdinalIgnoreCase))
            ?? tabControlElement;
        var headers = itemsHost.Elements()
            .Where(element => string.Equals(element.Name.LocalName, "TabItem", StringComparison.OrdinalIgnoreCase))
            .Select(element => element.Attribute("Header")?.Value ?? string.Empty)
            .ToList();

        return JsonSerializer.Serialize(headers);
    }

    private void WriteTopLevelElementAxaml(StringBuilder sb, DesignElement element, string indent)
    {
        switch (element.Visual)
        {
            case Button button when button is not Avalonia.Controls.Primitives.ToggleButton:
                sb.Append(indent);
                sb.Append("<Button");
                AppendCanvasLayoutAttributes(sb, element);
                AppendButtonAttributes(sb, button);
                sb.AppendLine(" />");
                break;

            case MaskedTextBox maskedTextBox:
                sb.Append(indent);
                sb.Append("<MaskedTextBox");
                AppendCanvasLayoutAttributes(sb, element);
                AppendTextInputAttributes(sb, maskedTextBox, skipBoundProperties: true);
                AppendMaskedTextBoxAttributes(sb, maskedTextBox);
                sb.AppendLine(" />");
                break;

            case TextBox textBox:
                sb.Append(indent);
                sb.Append("<TextBox");
                AppendCanvasLayoutAttributes(sb, element);
                AppendTextInputAttributes(sb, textBox, skipBoundProperties: true);
                sb.AppendLine(" />");
                break;

            case SelectableTextBlock selectableTextBlock when selectableTextBlock.GetType() == typeof(SelectableTextBlock):
                sb.Append(indent);
                sb.Append("<SelectableTextBlock");
                AppendCanvasLayoutAttributes(sb, element);
                if (!DesignerBindingRuntime.HasBinding(selectableTextBlock, "Text"))
                {
                    AppendAttribute(sb, "Text", selectableTextBlock.Text ?? string.Empty);
                }

                foreach (var attribute in DesignerSelectableTextBlockRuntime.GetAxamlAttributes(selectableTextBlock))
                {
                    AppendAttribute(sb, attribute.Name, attribute.Value);
                }

                sb.AppendLine(" />");
                break;

            case TextBlock textBlock:
                sb.Append(indent);
                sb.Append("<TextBlock");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Text", textBlock.Text ?? string.Empty);
                sb.AppendLine(" />");
                break;

            case Label label:
                sb.Append(indent);
                sb.Append("<Label");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Content", label.Content?.ToString() ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(label.Tag?.ToString()))
                {
                    AppendAttribute(sb, "Target", label.Tag?.ToString() ?? string.Empty);
                }

                sb.AppendLine(" />");
                break;

            case Image image:
                sb.Append(indent);
                sb.Append("<Image");
                AppendCanvasLayoutAttributes(sb, element);
                AppendImageAttributes(sb, image);
                sb.AppendLine(" />");
                break;

            case Shape shape:
                sb.Append(indent);
                sb.Append('<');
                sb.Append(shape.GetType().Name);
                AppendCanvasLayoutAttributes(sb, element);
                AppendShapeAttributes(sb, shape);
                sb.AppendLine(" />");
                break;

            case CheckBox checkBox:
                sb.Append(indent);
                sb.Append("<CheckBox");
                AppendCanvasLayoutAttributes(sb, element);
                AppendToggleAttributes(sb, checkBox);
                sb.AppendLine(" />");
                break;

            case RadioButton radioButton:
                sb.Append(indent);
                sb.Append("<RadioButton");
                AppendCanvasLayoutAttributes(sb, element);
                AppendToggleAttributes(sb, radioButton);
                sb.AppendLine(" />");
                break;

            case ToggleSwitch toggleSwitch:
                sb.Append(indent);
                sb.Append("<ToggleSwitch");
                AppendCanvasLayoutAttributes(sb, element);
                AppendToggleAttributes(sb, toggleSwitch);
                sb.AppendLine(" />");
                break;

            case Avalonia.Controls.Primitives.ToggleButton toggleButton:
                sb.Append(indent);
                sb.Append("<ToggleButton");
                AppendCanvasLayoutAttributes(sb, element);
                AppendToggleAttributes(sb, toggleButton);
                sb.AppendLine(" />");
                break;

            case AutoCompleteBox autoCompleteBox:
                sb.Append(indent);
                sb.Append("<AutoCompleteBox");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAutoCompleteBoxAttributes(sb, autoCompleteBox);
                if (DesignerBindingRuntime.HasBinding(autoCompleteBox, "ItemsSource"))
                {
                    sb.AppendLine(" />");
                    break;
                }

                sb.AppendLine(">");
                sb.Append(indent);
                sb.AppendLine("  <AutoCompleteBox.ItemsSource>");
                sb.Append(indent);
                sb.AppendLine("    <collections:AvaloniaList x:TypeArguments=\"x:Object\">");
                foreach (var item in ReadAutoCompleteBoxItems(autoCompleteBox))
                {
                    sb.Append(indent);
                    sb.Append("    <x:String>");
                    sb.Append(EscapeXmlAttribute(item?.ToString() ?? string.Empty));
                    sb.AppendLine("</x:String>");
                }

                sb.Append(indent);
                sb.AppendLine("    </collections:AvaloniaList>");
                sb.Append(indent);
                sb.AppendLine("  </AutoCompleteBox.ItemsSource>");
                sb.Append(indent);
                sb.AppendLine("</AutoCompleteBox>");
                break;

            case ComboBox comboBox:
                sb.Append(indent);
                sb.Append("<ComboBox");
                AppendCanvasLayoutAttributes(sb, element);
                AppendSelectionAttributes(sb, comboBox);
                if (DesignerBindingRuntime.HasBinding(comboBox, "ItemsSource"))
                {
                    sb.AppendLine(" />");
                    break;
                }

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
                AppendSelectionAttributes(sb, listBox);
                if (DesignerBindingRuntime.HasBinding(listBox, "ItemsSource"))
                {
                    sb.AppendLine(" />");
                    break;
                }

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

            case ItemsControl itemsControl when itemsControl.GetType() == typeof(ItemsControl):
                sb.Append(indent);
                sb.Append("<ItemsControl");
                AppendCanvasLayoutAttributes(sb, element);
                if (DesignerBindingRuntime.HasBinding(itemsControl, "ItemsSource"))
                {
                    sb.AppendLine(" />");
                    break;
                }

                var itemsControlItems = ReadItems(itemsControl);
                if (itemsControlItems.Count == 0)
                {
                    sb.AppendLine(" />");
                    break;
                }

                sb.AppendLine(">");
                sb.Append(indent);
                sb.AppendLine("  <ItemsControl.Items>");
                foreach (var item in itemsControlItems)
                {
                    sb.Append(indent);
                    sb.Append("    <x:String>");
                    sb.Append(EscapeXmlAttribute(item));
                    sb.AppendLine("</x:String>");
                }

                sb.Append(indent);
                sb.AppendLine("  </ItemsControl.Items>");
                sb.Append(indent);
                sb.AppendLine("</ItemsControl>");
                break;

            case TreeView treeView:
                sb.Append(indent);
                sb.Append("<TreeView");
                AppendCanvasLayoutAttributes(sb, element);
                AppendSelectionAttributes(sb, treeView);
                if (DesignerBindingRuntime.HasBinding(treeView, "ItemsSource"))
                {
                    sb.AppendLine(" />");
                    break;
                }

                sb.AppendLine(">");
                WriteTreeItemsAxaml(sb, DesignerTreeItemRuntime.ReadItems(treeView), indent + "  ");
                sb.Append(indent);
                sb.AppendLine("</TreeView>");
                break;

            case Menu menu:
                sb.Append(indent);
                sb.Append("<Menu");
                AppendCanvasLayoutAttributes(sb, element);
                sb.AppendLine(">");
                WriteMenuEntriesAxaml(sb, DesignerMenuItemRuntime.ReadItems(menu), indent + "  ");
                sb.Append(indent);
                sb.AppendLine("</Menu>");
                break;

            case DataGrid dataGrid:
                sb.Append(indent);
                sb.Append("<DataGrid");
                AppendCanvasLayoutAttributes(sb, element);
                AppendDataGridBehaviorAttributes(sb, dataGrid);
                sb.AppendLine(">");
                WriteDataGridColumnsAxaml(
                    sb,
                    DesignerDataGridRuntime.ReadColumns(dataGrid),
                    indent + "  ");
                sb.Append(indent);
                sb.AppendLine("</DataGrid>");
                break;

            case Slider slider:
                sb.Append(indent);
                sb.Append("<Slider");
                AppendCanvasLayoutAttributes(sb, element);
                AppendRangeAttributes(sb, slider);
                sb.AppendLine(" />");
                break;

            case ProgressBar progressBar:
                sb.Append(indent);
                sb.Append("<ProgressBar");
                AppendCanvasLayoutAttributes(sb, element);
                AppendRangeAttributes(sb, progressBar);
                sb.AppendLine(" />");
                break;

            case DatePicker datePicker:
                sb.Append(indent);
                sb.Append("<DatePicker");
                AppendCanvasLayoutAttributes(sb, element);
                AppendDateTimeAttributes(sb, datePicker);
                sb.AppendLine(" />");
                break;

            case CalendarDatePicker calendarDatePicker:
                sb.Append(indent);
                sb.Append("<CalendarDatePicker");
                AppendCanvasLayoutAttributes(sb, element);
                AppendDateTimeAttributes(sb, calendarDatePicker);
                sb.AppendLine(" />");
                break;

            case Avalonia.Controls.Calendar calendar:
                sb.Append(indent);
                sb.Append("<Calendar");
                AppendCanvasLayoutAttributes(sb, element);
                AppendDateTimeAttributes(sb, calendar);
                sb.AppendLine(" />");
                break;

            case Avalonia.Controls.ColorPicker colorPicker:
                sb.Append(indent);
                sb.Append("<ColorPicker");
                AppendCanvasLayoutAttributes(sb, element);
                AppendColorPickerAttributes(sb, colorPicker);
                sb.AppendLine(" />");
                break;

            case TimePicker timePicker:
                sb.Append(indent);
                sb.Append("<TimePicker");
                AppendCanvasLayoutAttributes(sb, element);
                AppendDateTimeAttributes(sb, timePicker);
                sb.AppendLine(" />");
                break;

            case NumericUpDown numericUpDown:
                sb.Append(indent);
                sb.Append("<NumericUpDown");
                AppendCanvasLayoutAttributes(sb, element);
                AppendRangeAttributes(sb, numericUpDown);
                sb.AppendLine(" />");
                break;

            case TabControl tabControl:
                sb.Append(indent);
                sb.Append("<TabControl");
                AppendCanvasLayoutAttributes(sb, element);
                AppendTabControlAttributes(sb, tabControl);
                sb.AppendLine(">");
                var tabHeaders = ReadTabHeaders(tabControl);
                for (var tabIndex = 0; tabIndex < tabHeaders.Count; tabIndex++)
                {
                    var header = tabHeaders[tabIndex];
                    sb.Append(indent);
                    sb.Append("  <TabItem");
                    AppendAttribute(sb, "Header", header);
                    sb.AppendLine(">");
                    if (GetDesignerTabChild(element, tabIndex) is { } tabChild)
                    {
                        WriteDesignerChildAxaml(sb, tabChild, indent + "    ");
                    }
                    else
                    {
                        sb.Append(indent);
                        sb.Append("    <TextBlock");
                        AppendAttribute(sb, "Text", $"{header} content");
                        sb.AppendLine(" />");
                    }

                    sb.Append(indent);
                    sb.AppendLine("  </TabItem>");
                }

                sb.Append(indent);
                sb.AppendLine("</TabControl>");
                break;

            case SplitView splitView:
                sb.Append(indent);
                sb.Append("<SplitView");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "DisplayMode", splitView.DisplayMode.ToString());
                AppendAttribute(sb, "IsPaneOpen", splitView.IsPaneOpen.ToString());
                AppendAttribute(
                    sb,
                    "OpenPaneLength",
                    splitView.OpenPaneLength.ToString("0.###", CultureInfo.InvariantCulture));
                AppendAttribute(
                    sb,
                    "CompactPaneLength",
                    splitView.CompactPaneLength.ToString("0.###", CultureInfo.InvariantCulture));
                AppendAttribute(sb, "PanePlacement", splitView.PanePlacement.ToString());
                AppendAttribute(
                    sb,
                    "UseLightDismissOverlayMode",
                    splitView.UseLightDismissOverlayMode.ToString());
                if (splitView.PaneBackground is { } paneBackground)
                {
                    AppendAttribute(sb, "PaneBackground", FormatBrushValue(paneBackground));
                }

                sb.AppendLine(">");
                sb.Append(indent);
                sb.AppendLine("  <SplitView.Pane>");
                if (GetDesignerSplitViewChild(element, DesignerSplitViewSlot.Pane) is { } paneChild)
                {
                    WriteDesignerChildAxaml(sb, paneChild, indent + "    ");
                }
                else
                {
                    sb.Append(indent);
                    sb.Append("    <TextBlock");
                    AppendAttribute(sb, "Text", ReadTextContent(splitView.Pane, "Navigation pane"));
                    sb.AppendLine(" />");
                }

                sb.Append(indent);
                sb.AppendLine("  </SplitView.Pane>");
                if (GetDesignerSplitViewChild(element, DesignerSplitViewSlot.Content) is { } splitContentChild)
                {
                    WriteDesignerChildAxaml(sb, splitContentChild, indent + "  ");
                }
                else
                {
                    sb.Append(indent);
                    sb.Append("  <TextBlock");
                    AppendAttribute(sb, "Text", ReadTextContent(splitView.Content, "Main content"));
                    sb.AppendLine(" />");
                }

                sb.Append(indent);
                sb.AppendLine("</SplitView>");
                break;

            case Expander expander:
                sb.Append(indent);
                sb.Append("<Expander");
                AppendCanvasLayoutAttributes(sb, element);
                AppendContainerBehaviorAttributes(sb, expander);
                sb.AppendLine(">");
                if (GetDesignerContentChild(element) is { } expanderChild)
                {
                    WriteDesignerChildAxaml(sb, expanderChild, indent + "  ");
                }
                else
                {
                    sb.Append(indent);
                    sb.Append("  <TextBlock");
                    AppendAttribute(sb, "Text", ReadExpanderContent(expander));
                    sb.AppendLine(" />");
                }

                sb.Append(indent);
                sb.AppendLine("</Expander>");
                break;

            case ScrollViewer scrollViewer:
                sb.Append(indent);
                sb.Append("<ScrollViewer");
                AppendCanvasLayoutAttributes(sb, element);
                AppendContainerBehaviorAttributes(sb, scrollViewer);
                sb.AppendLine(">");
                if (GetDesignerContentChild(element) is { } scrollViewerChild)
                {
                    WriteDesignerChildAxaml(sb, scrollViewerChild, indent + "  ");
                }
                else
                {
                    sb.Append(indent);
                    sb.Append("  <TextBlock");
                    AppendAttribute(sb, "Text", ReadScrollViewerContent(scrollViewer));
                    sb.AppendLine(" />");
                }

                sb.Append(indent);
                sb.AppendLine("</ScrollViewer>");
                break;

            case ContentControl contentControl when contentControl.GetType() == typeof(ContentControl)
                || contentControl is UserControl:
                sb.Append(indent);
                sb.Append(contentControl is UserControl ? "<UserControl" : "<ContentControl");
                AppendCanvasLayoutAttributes(sb, element);
                var contentControlChild = GetDesignerContentChild(element);
                if (contentControlChild is null
                    && (DesignerBindingRuntime.HasBinding(contentControl, "Content")
                        || contentControl.Content is not TextBlock))
                {
                    sb.AppendLine(" />");
                    break;
                }

                sb.AppendLine(">");
                if (contentControlChild is not null)
                {
                    WriteDesignerChildAxaml(sb, contentControlChild, indent + "  ");
                }
                else
                {
                    sb.Append(indent);
                    sb.Append("  <TextBlock");
                    AppendAttribute(sb, "Text", ReadContentControlContent(contentControl));
                    sb.AppendLine(" />");
                }

                sb.Append(indent);
                sb.Append(contentControl is UserControl ? "</UserControl>" : "</ContentControl>");
                sb.AppendLine();
                break;

            case Border border:
                sb.Append(indent);
                sb.Append("<Border");
                AppendCanvasLayoutAttributes(sb, element);
                var borderChild = GetDesignerContentChild(element);
                if (borderChild is null && border.Child is not TextBlock)
                {
                    sb.AppendLine(" />");
                    break;
                }

                sb.AppendLine(">");
                if (borderChild is not null)
                {
                    WriteDesignerChildAxaml(sb, borderChild, indent + "  ");
                }
                else
                {
                    sb.Append(indent);
                    sb.Append("  <TextBlock");
                    AppendAttribute(sb, "Text", ReadBorderContent(border));
                    sb.AppendLine(" />");
                }

                sb.Append(indent);
                sb.AppendLine("</Border>");
                break;

            case GridSplitter gridSplitter:
                sb.Append(indent);
                sb.Append("<GridSplitter");
                AppendCanvasLayoutAttributes(sb, element);
                AppendGridSplitterAttributes(sb, gridSplitter);
                sb.AppendLine(" />");
                break;

            case Grid grid:
                sb.Append(indent);
                sb.Append("<Grid");
                AppendCanvasLayoutAttributes(sb, element);
                var rowDefinitions = DesignerGridDefinitionRuntime.Format(grid.RowDefinitions);
                var columnDefinitions = DesignerGridDefinitionRuntime.Format(grid.ColumnDefinitions);
                if (!string.IsNullOrWhiteSpace(rowDefinitions))
                {
                    AppendAttribute(sb, "RowDefinitions", rowDefinitions);
                }

                if (!string.IsNullOrWhiteSpace(columnDefinitions))
                {
                    AppendAttribute(sb, "ColumnDefinitions", columnDefinitions);
                }

                AppendAttribute(sb, "ShowGridLines", grid.ShowGridLines.ToString());
                var designerChildren = Canvas.Elements.Where(child => string.Equals(
                        child.ParentName,
                        element.DisplayName,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (designerChildren.Count == 0)
                {
                    sb.AppendLine(" />");
                    break;
                }

                sb.AppendLine(">");
                foreach (var child in designerChildren)
                {
                    if (child.IsLocked)
                    {
                        sb.AppendLine($"{indent}  <!-- {DesignerMetadataPrefix} IsLocked=true -->");
                    }

                    WriteTopLevelElementAxaml(sb, child, indent + "  ");
                }

                sb.Append(indent);
                sb.AppendLine("</Grid>");
                break;

            case StackPanel stackPanel:
                sb.Append(indent);
                sb.Append("<StackPanel");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Orientation", stackPanel.Orientation.ToString());
                AppendAttribute(sb, "Spacing", stackPanel.Spacing.ToString("0.###", CultureInfo.InvariantCulture));
                var stackPanelChildren = Canvas.Elements
                    .Where(child => child.IsStackPanelChild
                        && string.Equals(
                            child.ParentName,
                            element.DisplayName,
                            StringComparison.OrdinalIgnoreCase))
                    .OrderBy(child => child.StackPanelIndex)
                    .ThenBy(child => Canvas.Elements.IndexOf(child))
                    .ToList();

                if (stackPanelChildren.Count == 0 && stackPanel.Children.Count == 0)
                {
                    sb.AppendLine(" />");
                    break;
                }

                sb.AppendLine(">");
                if (stackPanelChildren.Count > 0)
                {
                    foreach (var child in stackPanelChildren)
                    {
                        if (child.IsLocked)
                        {
                            sb.AppendLine($"{indent}  <!-- {DesignerMetadataPrefix} IsLocked=true -->");
                        }

                        WriteTopLevelElementAxaml(sb, child, indent + "  ");
                    }
                }
                else
                {
                    foreach (var child in stackPanel.Children)
                    {
                        WriteChildControlAxaml(sb, child, indent + "  ");
                    }
                }

                sb.Append(indent);
                sb.AppendLine("</StackPanel>");
                break;

            case DockPanel dockPanel:
                sb.Append(indent);
                sb.Append("<DockPanel");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "LastChildFill", dockPanel.LastChildFill.ToString());
                var dockPanelChildren = Canvas.Elements
                    .Where(child => child.IsDockPanelChild
                        && string.Equals(
                            child.ParentName,
                            element.DisplayName,
                            StringComparison.OrdinalIgnoreCase))
                    .OrderBy(child => child.DockPanelIndex)
                    .ThenBy(child => Canvas.Elements.IndexOf(child))
                    .ToList();
                if (dockPanelChildren.Count == 0)
                {
                    sb.AppendLine(" />");
                    break;
                }

                sb.AppendLine(">");
                foreach (var child in dockPanelChildren)
                {
                    WriteDesignerChildAxaml(sb, child, indent + "  ");
                }

                sb.Append(indent);
                sb.AppendLine("</DockPanel>");
                break;

            case WrapPanel wrapPanel:
                sb.Append(indent);
                sb.Append("<WrapPanel");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Orientation", wrapPanel.Orientation.ToString());
                AppendAttribute(sb, "ItemWidth", wrapPanel.ItemWidth.ToString("0.###", CultureInfo.InvariantCulture));
                AppendAttribute(sb, "ItemHeight", wrapPanel.ItemHeight.ToString("0.###", CultureInfo.InvariantCulture));
                AppendAttribute(sb, "ItemSpacing", wrapPanel.ItemSpacing.ToString("0.###", CultureInfo.InvariantCulture));
                AppendAttribute(sb, "LineSpacing", wrapPanel.LineSpacing.ToString("0.###", CultureInfo.InvariantCulture));
                AppendAttribute(sb, "ItemsAlignment", wrapPanel.ItemsAlignment.ToString());
                var wrapPanelChildren = Canvas.Elements
                    .Where(child => child.IsWrapPanelChild
                        && string.Equals(
                            child.ParentName,
                            element.DisplayName,
                            StringComparison.OrdinalIgnoreCase))
                    .OrderBy(child => child.WrapPanelIndex)
                    .ThenBy(child => Canvas.Elements.IndexOf(child))
                    .ToList();
                if (wrapPanelChildren.Count == 0)
                {
                    sb.AppendLine(" />");
                    break;
                }

                sb.AppendLine(">");
                foreach (var child in wrapPanelChildren)
                {
                    WriteDesignerChildAxaml(sb, child, indent + "  ");
                }

                sb.Append(indent);
                sb.AppendLine("</WrapPanel>");
                break;

            case UniformGrid uniformGrid:
                sb.Append(indent);
                sb.Append("<UniformGrid");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Rows", uniformGrid.Rows.ToString(CultureInfo.InvariantCulture));
                AppendAttribute(sb, "Columns", uniformGrid.Columns.ToString(CultureInfo.InvariantCulture));
                AppendAttribute(sb, "FirstColumn", uniformGrid.FirstColumn.ToString(CultureInfo.InvariantCulture));
                AppendAttribute(sb, "RowSpacing", uniformGrid.RowSpacing.ToString("0.###", CultureInfo.InvariantCulture));
                AppendAttribute(sb, "ColumnSpacing", uniformGrid.ColumnSpacing.ToString("0.###", CultureInfo.InvariantCulture));
                var uniformGridChildren = Canvas.Elements
                    .Where(child => child.IsUniformGridChild
                        && string.Equals(
                            child.ParentName,
                            element.DisplayName,
                            StringComparison.OrdinalIgnoreCase))
                    .OrderBy(child => child.UniformGridIndex)
                    .ThenBy(child => Canvas.Elements.IndexOf(child))
                    .ToList();
                if (uniformGridChildren.Count == 0)
                {
                    sb.AppendLine(" />");
                    break;
                }

                sb.AppendLine(">");
                foreach (var child in uniformGridChildren)
                {
                    WriteDesignerChildAxaml(sb, child, indent + "  ");
                }

                sb.Append(indent);
                sb.AppendLine("</UniformGrid>");
                break;

            case Canvas canvas:
                sb.Append(indent);
                sb.Append("<Canvas");
                AppendCanvasLayoutAttributes(sb, element);
                if (canvas.Background is not null)
                {
                    AppendAttribute(sb, "Background", FormatBrushValue(canvas.Background));
                }

                var canvasChildren = Canvas.Elements
                    .Where(child => child.IsCanvasChild
                        && string.Equals(
                            child.ParentName,
                            element.DisplayName,
                            StringComparison.OrdinalIgnoreCase))
                    .OrderBy(child => child.CanvasChildIndex)
                    .ThenBy(child => Canvas.Elements.IndexOf(child))
                    .ToList();
                if (canvasChildren.Count == 0)
                {
                    sb.AppendLine(" />");
                    break;
                }

                sb.AppendLine(">");
                foreach (var child in canvasChildren)
                {
                    WriteDesignerChildAxaml(sb, child, indent + "  ");
                }

                sb.Append(indent);
                sb.AppendLine("</Canvas>");
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
                AppendTextInputAttributes(sb, textBox, skipBoundProperties: false);
                sb.AppendLine(" />");
                break;

            default:
                sb.Append(indent);
                sb.Append("<Border />");
                sb.AppendLine();
                break;
        }
    }

    private static void WriteDataGridColumnsAxaml(
        StringBuilder sb,
        IEnumerable<DesignerDataGridColumnDefinition> definitions,
        string indent)
    {
        sb.Append(indent);
        sb.AppendLine("<DataGrid.Columns>");
        foreach (var definition in definitions)
        {
            sb.Append(indent);
            sb.Append("  <");
            sb.Append(definition.Kind == DesignerDataGridColumnKind.CheckBox
                ? "DataGridCheckBoxColumn"
                : "DataGridTextColumn");
            AppendAttribute(sb, "Header", definition.Header);
            AppendAttribute(sb, "Binding", $"{{ReflectionBinding {definition.BindingPath}}}");
            AppendAttribute(sb, "Width", definition.Width);
            if (definition.IsReadOnly)
            {
                AppendAttribute(sb, "IsReadOnly", bool.TrueString);
            }

            sb.AppendLine(" />");
        }

        sb.Append(indent);
        sb.AppendLine("</DataGrid.Columns>");
    }

    private void AppendShapeAttributes(StringBuilder sb, Shape shape)
    {
        if (!ShouldSuppressInlineStyleProperty(shape, "StrokeThickness"))
        {
            AppendAttribute(
                sb,
                "StrokeThickness",
                shape.StrokeThickness.ToString("0.###", CultureInfo.InvariantCulture));
        }

        AppendAttribute(sb, "Stretch", shape.Stretch.ToString());
        if (shape.StrokeDashArray is { Count: > 0 } dashArray)
        {
            AppendAttribute(
                sb,
                "StrokeDashArray",
                string.Join(",", dashArray.Select(value =>
                    value.ToString("0.###", CultureInfo.InvariantCulture))));
        }

        if (Math.Abs(shape.StrokeDashOffset) > double.Epsilon)
        {
            AppendAttribute(
                sb,
                "StrokeDashOffset",
                shape.StrokeDashOffset.ToString("0.###", CultureInfo.InvariantCulture));
        }

        AppendAttribute(sb, "StrokeLineCap", shape.StrokeLineCap.ToString());
        AppendAttribute(sb, "StrokeJoin", shape.StrokeJoin.ToString());
        AppendAttribute(
            sb,
            "StrokeMiterLimit",
            shape.StrokeMiterLimit.ToString("0.###", CultureInfo.InvariantCulture));

        switch (shape)
        {
            case RectangleShape rectangle:
                AppendAttribute(sb, "RadiusX", rectangle.RadiusX.ToString("0.###", CultureInfo.InvariantCulture));
                AppendAttribute(sb, "RadiusY", rectangle.RadiusY.ToString("0.###", CultureInfo.InvariantCulture));
                break;
            case LineShape line:
                AppendAttribute(sb, "StartPoint", FormatPoint(line.StartPoint));
                AppendAttribute(sb, "EndPoint", FormatPoint(line.EndPoint));
                break;
            case PathShape { Tag: DesignerPathDataMetadata pathData }:
                AppendAttribute(sb, "Data", pathData.Data);
                break;
        }
    }

    private static void WriteTreeItemsAxaml(
        StringBuilder sb,
        IEnumerable<DesignerTreeItemDefinition> definitions,
        string indent)
    {
        foreach (var definition in definitions)
        {
            sb.Append(indent);
            sb.Append("<TreeViewItem");
            AppendAttribute(sb, "Header", definition.Header);
            AppendAttribute(sb, "IsExpanded", definition.IsExpanded.ToString());
            if (definition.Children.Count == 0)
            {
                sb.AppendLine(" />");
                continue;
            }

            sb.AppendLine(">");
            WriteTreeItemsAxaml(sb, definition.Children, indent + "  ");
            sb.Append(indent);
            sb.AppendLine("</TreeViewItem>");
        }
    }

    private static void WriteMenuEntriesAxaml(
        StringBuilder sb,
        IEnumerable<DesignerMenuEntryDefinition> definitions,
        string indent)
    {
        foreach (var definition in definitions)
        {
            sb.Append(indent);
            if (definition.Kind == DesignerMenuEntryKind.Separator)
            {
                sb.AppendLine("<Separator />");
                continue;
            }

            sb.Append("<MenuItem");
            AppendAttribute(sb, "Header", definition.Header);
            if (!string.IsNullOrWhiteSpace(definition.InputGesture))
            {
                AppendAttribute(sb, "InputGesture", definition.InputGesture);
                AppendAttribute(sb, "HotKey", definition.InputGesture);
            }

            if (definition.ToggleType != MenuItemToggleType.None)
            {
                AppendAttribute(sb, "ToggleType", definition.ToggleType.ToString());
                AppendAttribute(sb, "IsChecked", definition.IsChecked.ToString());
            }

            if (!string.IsNullOrWhiteSpace(definition.GroupName))
            {
                AppendAttribute(sb, "GroupName", definition.GroupName);
            }

            if (definition.Children.Count == 0)
            {
                sb.AppendLine(" />");
                continue;
            }

            sb.AppendLine(">");
            WriteMenuEntriesAxaml(sb, definition.Children, indent + "  ");
            sb.Append(indent);
            sb.AppendLine("</MenuItem>");
        }
    }

    private void AppendCanvasLayoutAttributes(StringBuilder sb, DesignElement element)
    {
        AppendAttribute(sb, "x:Name", element.DisplayName);
        if (ReferenceEquals(element, _standaloneAxamlElement))
        {
            AppendAttribute(sb, "Width", FormatRootNumber(element.Width));
            AppendAttribute(sb, "Height", FormatRootNumber(element.Height));
        }
        else if (HasValidGridParent(element))
        {
            if (element.GridRow > 0)
            {
                AppendAttribute(sb, "Grid.Row", element.GridRow.ToString(CultureInfo.InvariantCulture));
            }

            if (element.GridColumn > 0)
            {
                AppendAttribute(sb, "Grid.Column", element.GridColumn.ToString(CultureInfo.InvariantCulture));
            }

            if (element.GridRowSpan > 1)
            {
                AppendAttribute(sb, "Grid.RowSpan", element.GridRowSpan.ToString(CultureInfo.InvariantCulture));
            }

            if (element.GridColumnSpan > 1)
            {
                AppendAttribute(sb, "Grid.ColumnSpan", element.GridColumnSpan.ToString(CultureInfo.InvariantCulture));
            }
        }
        else if (TryGetStackPanelParent(element, out var stackPanel))
        {
            AppendAttribute(
                sb,
                stackPanel.Orientation == Orientation.Vertical ? "Height" : "Width",
                element.StackPanelItemSize.ToString("0.###", CultureInfo.InvariantCulture));
        }
        else if (TryGetContentParent(element, out _))
        {
            // Content controls own their child's layout.
        }
        else if (TryGetDockPanelParent(element, out var dockParent, out var dockPanel))
        {
            AppendAttribute(sb, "DockPanel.Dock", element.DockPanelDock.ToString());
            var siblings = Canvas.Elements
                .Where(child => child.IsDockPanelChild
                    && string.Equals(
                        child.ParentName,
                        dockParent.DisplayName,
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(child => child.DockPanelIndex)
                .ThenBy(child => Canvas.Elements.IndexOf(child))
                .ToList();
            var isLastFill = dockPanel.LastChildFill
                && ReferenceEquals(siblings.LastOrDefault(), element);
            if (!isLastFill)
            {
                AppendAttribute(
                    sb,
                    element.DockPanelDock is DesignerDockSide.Top or DesignerDockSide.Bottom
                        ? "Height"
                        : "Width",
                    element.DockPanelItemSize.ToString("0.###", CultureInfo.InvariantCulture));
            }
        }
        else if (TryGetWrapPanelParent(element, out _))
        {
            // WrapPanel owns item size, spacing, and placement.
        }
        else if (TryGetUniformGridParent(element, out _))
        {
            // UniformGrid owns cell size and placement.
        }
        else if (TryGetCanvasParent(element, out _))
        {
            AppendAttribute(sb, "Canvas.Left", element.CanvasChildLeft.ToString("0.###", CultureInfo.InvariantCulture));
            AppendAttribute(sb, "Canvas.Top", element.CanvasChildTop.ToString("0.###", CultureInfo.InvariantCulture));
            AppendAttribute(sb, "Width", element.Width.ToString("0.###", CultureInfo.InvariantCulture));
            AppendAttribute(sb, "Height", element.Height.ToString("0.###", CultureInfo.InvariantCulture));
        }
        else if (TryGetTabControlParent(element, out _))
        {
            // TabItem content owns its child's layout.
        }
        else if (TryGetSplitViewParent(element, out _))
        {
            // SplitView Pane and Content own their child layout.
        }
        else
        {
            AppendAttribute(sb, "Canvas.Left", element.X.ToString("0.###", CultureInfo.InvariantCulture));
            AppendAttribute(sb, "Canvas.Top", element.Y.ToString("0.###", CultureInfo.InvariantCulture));
            AppendAttribute(sb, "Width", element.Width.ToString("0.###", CultureInfo.InvariantCulture));
            AppendAttribute(sb, "Height", element.Height.ToString("0.###", CultureInfo.InvariantCulture));
        }

        var classes = CanvasViewModel.GetUserStyleClasses(element.Visual);
        if (classes.Count > 0)
        {
            AppendAttribute(sb, "Classes", string.Join(" ", classes));
        }

        foreach (var binding in DesignerBindingRuntime.ReadBindings(element.Visual))
        {
            AppendAttribute(
                sb,
                binding.PropertyName,
                DesignerBindingRuntime.FormatExpression(binding));
        }

        if (!ShouldSuppressInlineStyleProperty(element.Visual, "Opacity"))
        {
            AppendAttribute(sb, "Opacity", element.Visual.Opacity.ToString("0.###", CultureInfo.InvariantCulture));
        }

        AppendCommonAppearanceAttributes(sb, element.Visual);
        AppendCommonLayoutAttributes(sb, element.Visual);
        AppendCommonTypographyAttributes(sb, element.Visual);
        AppendCommonTransformAttributes(sb, element.Visual);
        AppendCommonAccessibilityAttributes(sb, element.Visual);
        AppendCommonInteractionAttributes(sb, element.Visual);
        AppendCommonEffectAttributes(sb, element.Visual);

    }

    private bool HasValidGridParent(DesignElement element)
        => element.ParentName is not null
            && Canvas.Elements.Any(parent =>
                parent.Visual is Grid
                && string.Equals(parent.DisplayName, element.ParentName, StringComparison.OrdinalIgnoreCase));

    private bool HasValidContainerParent(DesignElement element)
        => HasValidGridParent(element)
            || TryGetStackPanelParent(element, out _)
            || TryGetDockPanelParent(element, out _, out _)
            || TryGetWrapPanelParent(element, out _)
            || TryGetUniformGridParent(element, out _)
            || TryGetCanvasParent(element, out _)
            || TryGetTabControlParent(element, out _)
            || TryGetSplitViewParent(element, out _)
            || TryGetContentParent(element, out _);

    private bool TryGetStackPanelParent(DesignElement element, out StackPanel stackPanel)
    {
        var parent = element.ParentName is null
            ? null
            : Canvas.Elements.FirstOrDefault(candidate =>
                candidate.Visual is StackPanel
                && string.Equals(
                    candidate.DisplayName,
                    element.ParentName,
                    StringComparison.OrdinalIgnoreCase));
        stackPanel = parent?.Visual as StackPanel ?? null!;
        return stackPanel is not null;
    }

    private bool TryGetContentParent(DesignElement element, out DesignElement parent)
    {
        parent = element.ParentName is null
            ? null!
            : Canvas.Elements.FirstOrDefault(candidate =>
                IsDesignerContentContainer(candidate.Visual)
                && string.Equals(
                    candidate.DisplayName,
                    element.ParentName,
                    StringComparison.OrdinalIgnoreCase))!;
        return parent is not null;
    }

    private static bool IsDesignerContentContainer(Control visual)
        => visual is Border or ScrollViewer or Expander or UserControl
            || visual.GetType() == typeof(ContentControl);

    private bool TryGetDockPanelParent(
        DesignElement element,
        out DesignElement parent,
        out DockPanel dockPanel)
    {
        parent = element.ParentName is null
            ? null!
            : Canvas.Elements.FirstOrDefault(candidate =>
                candidate.Visual is DockPanel
                && string.Equals(
                    candidate.DisplayName,
                    element.ParentName,
                    StringComparison.OrdinalIgnoreCase))!;
        dockPanel = parent?.Visual as DockPanel ?? null!;
        return dockPanel is not null;
    }

    private bool TryGetWrapPanelParent(DesignElement element, out WrapPanel wrapPanel)
    {
        var parent = element.ParentName is null
            ? null
            : Canvas.Elements.FirstOrDefault(candidate =>
                candidate.Visual is WrapPanel
                && string.Equals(
                    candidate.DisplayName,
                    element.ParentName,
                    StringComparison.OrdinalIgnoreCase));
        wrapPanel = parent?.Visual as WrapPanel ?? null!;
        return wrapPanel is not null;
    }

    private bool TryGetUniformGridParent(DesignElement element, out UniformGrid uniformGrid)
    {
        var parent = element.ParentName is null
            ? null
            : Canvas.Elements.FirstOrDefault(candidate =>
                candidate.Visual is UniformGrid
                && string.Equals(
                    candidate.DisplayName,
                    element.ParentName,
                    StringComparison.OrdinalIgnoreCase));
        uniformGrid = parent?.Visual as UniformGrid ?? null!;
        return uniformGrid is not null;
    }

    private bool TryGetCanvasParent(DesignElement element, out Canvas canvas)
    {
        var parent = element.ParentName is null
            ? null
            : Canvas.Elements.FirstOrDefault(candidate =>
                candidate.Visual is Canvas
                && string.Equals(
                    candidate.DisplayName,
                    element.ParentName,
                    StringComparison.OrdinalIgnoreCase));
        canvas = parent?.Visual as Canvas ?? null!;
        return canvas is not null;
    }

    private bool TryGetTabControlParent(DesignElement element, out DesignElement parent)
    {
        parent = element.ParentName is null
            ? null!
            : Canvas.Elements.FirstOrDefault(candidate =>
                candidate.Visual is TabControl
                && string.Equals(
                    candidate.DisplayName,
                    element.ParentName,
                    StringComparison.OrdinalIgnoreCase))!;
        return parent is not null;
    }

    private bool TryGetSplitViewParent(DesignElement element, out DesignElement parent)
    {
        parent = element.ParentName is null
            ? null!
            : Canvas.Elements.FirstOrDefault(candidate =>
                candidate.Visual is SplitView
                && string.Equals(
                    candidate.DisplayName,
                    element.ParentName,
                    StringComparison.OrdinalIgnoreCase))!;
        return parent is not null;
    }

    private DesignElement? GetDesignerContentChild(DesignElement parent)
        => Canvas.Elements.FirstOrDefault(child =>
            child.IsContentChild
            && string.Equals(
                child.ParentName,
                parent.DisplayName,
                StringComparison.OrdinalIgnoreCase));

    private DesignElement? GetDesignerTabChild(DesignElement parent, int tabIndex)
        => Canvas.Elements.FirstOrDefault(child =>
            child.IsTabControlChild
            && child.TabIndex == tabIndex
            && string.Equals(
                child.ParentName,
                parent.DisplayName,
                StringComparison.OrdinalIgnoreCase));

    private DesignElement? GetDesignerSplitViewChild(
        DesignElement parent,
        DesignerSplitViewSlot slot)
        => Canvas.Elements.FirstOrDefault(child =>
            child.IsSplitViewChild
            && child.SplitViewSlot == slot
            && string.Equals(
                child.ParentName,
                parent.DisplayName,
                StringComparison.OrdinalIgnoreCase));

    private void WriteDesignerChildAxaml(
        StringBuilder sb,
        DesignElement child,
        string indent)
    {
        if (child.IsLocked)
        {
            sb.AppendLine($"{indent}<!-- {DesignerMetadataPrefix} IsLocked=true -->");
        }

        WriteTopLevelElementAxaml(sb, child, indent);
    }

    private void AppendColorResourcesAxaml(StringBuilder sb, string rootElementName, string indent)
    {
        if (_colorResources.Count == 0)
        {
            return;
        }

        sb.Append(indent);
        sb.Append('<');
        sb.Append(rootElementName);
        sb.AppendLine(".Resources>");
        foreach (var pair in _colorResources.OrderBy(resource => resource.Key, StringComparer.Ordinal))
        {
            sb.Append(indent);
            sb.Append("  <SolidColorBrush");
            AppendAttribute(sb, "x:Key", pair.Key);
            AppendAttribute(sb, "Color", pair.Value);
            sb.AppendLine(" />");
        }

        sb.Append(indent);
        sb.Append("</");
        sb.Append(rootElementName);
        sb.AppendLine(".Resources>");
    }

    private void AppendDocumentStylesAxaml(StringBuilder sb, string rootElementName, string indent)
    {
        if (_documentStyles.Count == 0)
        {
            return;
        }

        sb.Append(indent);
        sb.Append('<');
        sb.Append(rootElementName);
        sb.AppendLine(".Styles>");
        foreach (var style in _documentStyles)
        {
            sb.Append(indent);
            sb.Append("  <Style");
            AppendAttribute(sb, "Selector", style.Selector);
            sb.AppendLine(">");
            foreach (var setter in style.Setters)
            {
                sb.Append(indent);
                sb.Append("    <Setter");
                AppendAttribute(sb, "Property", setter.Key);
                AppendAttribute(sb, "Value", setter.Value);
                sb.AppendLine(" />");
            }

            sb.Append(indent);
            sb.AppendLine("  </Style>");
        }

        sb.Append(indent);
        sb.Append("</");
        sb.Append(rootElementName);
        sb.AppendLine(".Styles>");
    }

    private void AppendCommonAppearanceAttributes(StringBuilder sb, Control visual)
    {
        switch (visual)
        {
            case Avalonia.Controls.Primitives.TemplatedControl templated:
                AppendBrushAppearanceAttribute(
                    sb,
                    visual,
                    "Background",
                    templated.Background,
                    templated.IsSet(Avalonia.Controls.Primitives.TemplatedControl.BackgroundProperty)
                        && !ShouldSuppressInlineStyleProperty(visual, "Background"));
                AppendBrushAppearanceAttribute(
                    sb,
                    visual,
                    "Foreground",
                    templated.Foreground,
                    templated.IsSet(Avalonia.Controls.Primitives.TemplatedControl.ForegroundProperty)
                        && !ShouldSuppressInlineStyleProperty(visual, "Foreground"));
                AppendBrushAppearanceAttribute(
                    sb,
                    visual,
                    "BorderBrush",
                    templated.BorderBrush,
                    templated.IsSet(Avalonia.Controls.Primitives.TemplatedControl.BorderBrushProperty)
                        && !ShouldSuppressInlineStyleProperty(visual, "BorderBrush"));
                if (templated.IsSet(Avalonia.Controls.Primitives.TemplatedControl.BorderThicknessProperty)
                    && !ShouldSuppressInlineStyleProperty(visual, "BorderThickness"))
                {
                    AppendAttribute(sb, "BorderThickness", templated.BorderThickness.ToString());
                }

                if (templated.IsSet(Avalonia.Controls.Primitives.TemplatedControl.CornerRadiusProperty)
                    && !ShouldSuppressInlineStyleProperty(visual, "CornerRadius"))
                {
                    AppendAttribute(sb, "CornerRadius", templated.CornerRadius.ToString());
                }

                break;

            case Border border:
                AppendBrushAppearanceAttribute(
                    sb,
                    visual,
                    "Background",
                    border.Background,
                    border.IsSet(Border.BackgroundProperty)
                        && !ShouldSuppressInlineStyleProperty(visual, "Background"));
                AppendBrushAppearanceAttribute(
                    sb,
                    visual,
                    "BorderBrush",
                    border.BorderBrush,
                    border.IsSet(Border.BorderBrushProperty)
                        && !ShouldSuppressInlineStyleProperty(visual, "BorderBrush"));
                if (!ShouldSuppressInlineStyleProperty(visual, "BorderThickness"))
                {
                    AppendAttribute(sb, "BorderThickness", border.BorderThickness.ToString());
                }

                if (!ShouldSuppressInlineStyleProperty(visual, "CornerRadius"))
                {
                    AppendAttribute(sb, "CornerRadius", border.CornerRadius.ToString());
                }

                break;

            case TextBlock textBlock:
                AppendBrushAppearanceAttribute(
                    sb,
                    visual,
                    "Background",
                    textBlock.Background,
                    textBlock.IsSet(TextBlock.BackgroundProperty)
                        && !ShouldSuppressInlineStyleProperty(visual, "Background"));
                AppendBrushAppearanceAttribute(
                    sb,
                    visual,
                    "Foreground",
                    textBlock.Foreground,
                    textBlock.IsSet(TextBlock.ForegroundProperty)
                        && !ShouldSuppressInlineStyleProperty(visual, "Foreground"));
                break;

            case Shape shape:
                AppendBrushAppearanceAttribute(
                    sb,
                    visual,
                    "Fill",
                    shape.Fill,
                    shape.IsSet(Shape.FillProperty)
                        && !ShouldSuppressInlineStyleProperty(visual, "Fill"));
                AppendBrushAppearanceAttribute(
                    sb,
                    visual,
                    "Stroke",
                    shape.Stroke,
                    shape.IsSet(Shape.StrokeProperty)
                        && !ShouldSuppressInlineStyleProperty(visual, "Stroke"));
                break;
        }
    }

    private static void AppendCommonLayoutAttributes(StringBuilder sb, Control visual)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        DesignerLayoutRuntime.Capture(visual, properties);
        foreach (var propertyName in new[]
                 {
                     "Margin",
                     "Padding",
                     "HorizontalAlignment",
                     "VerticalAlignment",
                     "MinWidth",
                     "MinHeight",
                     "MaxWidth",
                     "MaxHeight",
                 })
        {
            if (properties.TryGetValue(propertyName, out var value))
            {
                AppendAttribute(sb, propertyName, value);
            }
        }
    }

    private void AppendCommonTypographyAttributes(StringBuilder sb, Control visual)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        DesignerTypographyRuntime.Capture(visual, properties);
        foreach (var propertyName in new[]
                 {
                     "FontFamily",
                     "FontSize",
                     "FontStyle",
                     "FontWeight",
                     "TextAlignment",
                     "TextWrapping",
                 })
        {
            if (properties.TryGetValue(propertyName, out var value)
                && !ShouldSuppressInlineStyleProperty(visual, propertyName))
            {
                AppendAttribute(sb, propertyName, value);
            }
        }
    }

    private static void AppendCommonTransformAttributes(StringBuilder sb, Control visual)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        DesignerTransformRuntime.Capture(visual, properties);
        foreach (var propertyName in new[] { "RenderTransform", "RenderTransformOrigin" })
        {
            if (properties.TryGetValue(propertyName, out var value))
            {
                AppendAttribute(sb, propertyName, value);
            }
        }
    }

    private static void AppendCommonAccessibilityAttributes(StringBuilder sb, Control visual)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        DesignerAccessibilityRuntime.Capture(visual, properties);
        foreach (var attribute in DesignerAccessibilityRuntime.GetAxamlAttributes(properties))
        {
            AppendAttribute(sb, attribute.Name, attribute.Value);
        }
    }

    private static void AppendCommonInteractionAttributes(StringBuilder sb, Control visual)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        DesignerInteractionRuntime.Capture(visual, properties);
        foreach (var attribute in DesignerInteractionRuntime.GetAxamlAttributes(properties))
        {
            AppendAttribute(sb, attribute.Name, attribute.Value);
        }
    }

    private static void AppendCommonEffectAttributes(StringBuilder sb, Control visual)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        DesignerEffectRuntime.Capture(visual, properties);
        foreach (var attribute in DesignerEffectRuntime.GetAxamlAttributes(properties))
        {
            AppendAttribute(sb, attribute.Name, attribute.Value);
        }
    }

    private static void AppendRangeAttributes(StringBuilder sb, Control visual)
    {
        var boundProperties = DesignerBindingRuntime.ReadBindings(visual)
            .Select(binding => binding.PropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in DesignerRangeRuntime.GetAxamlAttributes(visual))
        {
            if (!boundProperties.Contains(attribute.Name))
            {
                AppendAttribute(sb, attribute.Name, attribute.Value);
            }
        }
    }

    private static void AppendTextInputAttributes(
        StringBuilder sb,
        TextBox textBox,
        bool skipBoundProperties)
    {
        var boundProperties = skipBoundProperties
            ? DesignerBindingRuntime.ReadBindings(textBox)
                .Select(binding => binding.PropertyName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];
        foreach (var attribute in DesignerTextInputRuntime.GetAxamlAttributes(textBox))
        {
            var isWrittenByCommonTypography = skipBoundProperties
                && attribute.Name is "TextWrapping" or "TextAlignment";
            if (!boundProperties.Contains(attribute.Name) && !isWrittenByCommonTypography)
            {
                AppendAttribute(sb, attribute.Name, attribute.Value);
            }
        }
    }

    private static void AppendMaskedTextBoxAttributes(StringBuilder sb, Control visual)
    {
        var boundProperties = DesignerBindingRuntime.ReadBindings(visual)
            .Select(binding => binding.PropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in DesignerMaskedTextBoxRuntime.GetAxamlAttributes(visual))
        {
            if (!boundProperties.Contains(attribute.Name))
            {
                AppendAttribute(sb, attribute.Name, attribute.Value);
            }
        }
    }

    private static void AppendSelectionAttributes(StringBuilder sb, Control visual)
    {
        var boundProperties = DesignerBindingRuntime.ReadBindings(visual)
            .Select(binding => binding.PropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in DesignerSelectionRuntime.GetAxamlAttributes(visual))
        {
            if (!boundProperties.Contains(attribute.Name))
            {
                AppendAttribute(sb, attribute.Name, attribute.Value);
            }
        }
    }

    private static void AppendTabControlAttributes(StringBuilder sb, TabControl tabControl)
    {
        var boundProperties = DesignerBindingRuntime.ReadBindings(tabControl)
            .Select(binding => binding.PropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in DesignerTabControlRuntime.GetAxamlAttributes(tabControl))
        {
            if (!boundProperties.Contains(attribute.Name))
            {
                AppendAttribute(sb, attribute.Name, attribute.Value);
            }
        }

        if (!boundProperties.Contains("SelectedIndex"))
        {
            AppendAttribute(
                sb,
                "SelectedIndex",
                tabControl.SelectedIndex.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void AppendDataGridBehaviorAttributes(StringBuilder sb, DataGrid dataGrid)
    {
        var boundProperties = DesignerBindingRuntime.ReadBindings(dataGrid)
            .Select(binding => binding.PropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in DesignerDataGridBehaviorRuntime.GetAxamlAttributes(dataGrid))
        {
            if (!boundProperties.Contains(attribute.Name))
            {
                AppendAttribute(sb, attribute.Name, attribute.Value);
            }
        }
    }

    private static void AppendDateTimeAttributes(StringBuilder sb, Control visual)
    {
        var boundProperties = DesignerBindingRuntime.ReadBindings(visual)
            .Select(binding => binding.PropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in DesignerDateTimeRuntime.GetAxamlAttributes(visual))
        {
            if (!boundProperties.Contains(attribute.Name))
            {
                AppendAttribute(sb, attribute.Name, attribute.Value);
            }
        }
    }

    private static void AppendColorPickerAttributes(StringBuilder sb, Control visual)
    {
        var boundProperties = DesignerBindingRuntime.ReadBindings(visual)
            .Select(binding => binding.PropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in DesignerColorPickerRuntime.GetAxamlAttributes(visual))
        {
            if (!boundProperties.Contains(attribute.Name))
            {
                AppendAttribute(sb, attribute.Name, attribute.Value);
            }
        }
    }

    private static void AppendAutoCompleteBoxAttributes(StringBuilder sb, Control visual)
    {
        var boundProperties = DesignerBindingRuntime.ReadBindings(visual)
            .Select(binding => binding.PropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in DesignerAutoCompleteBoxRuntime.GetAxamlAttributes(visual))
        {
            if (!boundProperties.Contains(attribute.Name))
            {
                AppendAttribute(sb, attribute.Name, attribute.Value);
            }
        }
    }

    private static void AppendToggleAttributes(StringBuilder sb, Control visual)
    {
        var boundProperties = DesignerBindingRuntime.ReadBindings(visual)
            .Select(binding => binding.PropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in DesignerToggleRuntime.GetAxamlAttributes(visual))
        {
            if (!boundProperties.Contains(attribute.Name))
            {
                AppendAttribute(sb, attribute.Name, attribute.Value);
            }
        }
    }

    private static void AppendContainerBehaviorAttributes(
        StringBuilder sb,
        Control visual)
    {
        var boundProperties = DesignerBindingRuntime.ReadBindings(visual)
            .Select(binding => binding.PropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in DesignerContainerBehaviorRuntime.GetAxamlAttributes(visual))
        {
            if (!boundProperties.Contains(attribute.Name))
            {
                AppendAttribute(sb, attribute.Name, attribute.Value);
            }
        }
    }

    private static void AppendGridSplitterAttributes(
        StringBuilder sb,
        GridSplitter gridSplitter)
    {
        var boundProperties = DesignerBindingRuntime.ReadBindings(gridSplitter)
            .Select(binding => binding.PropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in DesignerGridSplitterRuntime.GetAxamlAttributes(gridSplitter))
        {
            if (!boundProperties.Contains(attribute.Name))
            {
                AppendAttribute(sb, attribute.Name, attribute.Value);
            }
        }
    }

    private static void AppendImageAttributes(StringBuilder sb, Control visual)
    {
        var boundProperties = DesignerBindingRuntime.ReadBindings(visual)
            .Select(binding => binding.PropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in DesignerImageRuntime.GetAxamlAttributes(visual))
        {
            if (!boundProperties.Contains(attribute.Name))
            {
                AppendAttribute(sb, attribute.Name, attribute.Value);
            }
        }
    }

    private static void AppendButtonAttributes(StringBuilder sb, Control visual)
    {
        var boundProperties = DesignerBindingRuntime.ReadBindings(visual)
            .Select(binding => binding.PropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in DesignerButtonRuntime.GetAxamlAttributes(visual))
        {
            if (string.Equals(attribute.Name, "Click", StringComparison.Ordinal)
                || !boundProperties.Contains(attribute.Name))
            {
                AppendAttribute(sb, attribute.Name, attribute.Value);
            }
        }
    }

    private static void AppendBrushAppearanceAttribute(
        StringBuilder sb,
        Control visual,
        string propertyName,
        IBrush? brush,
        bool isExplicit)
    {
        if (DesignerResourceReferenceMetadata.TryGetReference(visual, propertyName, out var resourceKey))
        {
            AppendAttribute(sb, propertyName, DesignerResourceReferenceMetadata.FormatExpression(resourceKey));
            return;
        }

        if (isExplicit && brush is not null)
        {
            AppendAttribute(sb, propertyName, FormatBrushValue(brush));
        }
    }

    private static string FormatBrushValue(IBrush brush)
        => brush is ISolidColorBrush solidColorBrush
            ? FormatColorValue(solidColorBrush.Color)
            : brush.ToString() ?? string.Empty;

    private static string FormatColorValue(Color color)
        => $"#{color.A:x2}{color.R:x2}{color.G:x2}{color.B:x2}";

    private static void AppendAttribute(StringBuilder sb, string name, string value)
    {
        var lineStart = sb.Length - 1;
        while (lineStart >= 0 && sb[lineStart] is not '\r' and not '\n')
        {
            lineStart--;
        }

        lineStart++;
        if (sb.ToString(lineStart, sb.Length - lineStart)
            .Contains($" {name}=\"", StringComparison.Ordinal))
        {
            return;
        }

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
            .Replace(">", "&gt;")
            .Replace("\r", "&#13;")
            .Replace("\n", "&#10;")
            .Replace("\t", "&#9;");
    }

    public enum HistoryActionType
    {
        AddElement,
        DuplicateElement,
        PasteElement,
        RemoveElement,
        TransformElement,
        EditProperty,
        EditAxamlSource,
        EditSampleData,
        EditRootProperties,
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
        bool? RevealPassword = null,
        bool? AcceptsReturn = null,
        string? TextWrapping = null);
}
