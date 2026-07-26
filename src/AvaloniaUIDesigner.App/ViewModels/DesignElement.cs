using Avalonia.Controls;
using AvaloniaUIDesigner.App.Designer.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUIDesigner.App.ViewModels;

public partial class DesignElement : ViewModelBase
{
    public DesignElement(string displayName, string typeName, Control visual, double x, double y, double width, double height)
    {
        _displayName = displayName;
        TypeName = typeName;
        Visual = visual;
        _x = x;
        _y = y;
        _width = width;
        _height = height;
    }

    [ObservableProperty] private string _displayName;
    public string TypeName { get; }
    public Control Visual { get; }

    [ObservableProperty] private double _x;
    [ObservableProperty] private double _y;
    [ObservableProperty] private double _width;
    [ObservableProperty] private double _height;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionThickness))]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsContainerChild))]
    [NotifyPropertyChangedFor(nameof(IsGridChild))]
    [NotifyPropertyChangedFor(nameof(IsStackPanelChild))]
    [NotifyPropertyChangedFor(nameof(IsDockPanelChild))]
    [NotifyPropertyChangedFor(nameof(IsWrapPanelChild))]
    [NotifyPropertyChangedFor(nameof(IsUniformGridChild))]
    [NotifyPropertyChangedFor(nameof(IsCanvasChild))]
    [NotifyPropertyChangedFor(nameof(IsContentChild))]
    [NotifyPropertyChangedFor(nameof(GridCellLabel))]
    [NotifyPropertyChangedFor(nameof(StackPanelItemLabel))]
    [NotifyPropertyChangedFor(nameof(DockPanelItemLabel))]
    [NotifyPropertyChangedFor(nameof(WrapPanelItemLabel))]
    [NotifyPropertyChangedFor(nameof(UniformGridItemLabel))]
    [NotifyPropertyChangedFor(nameof(CanvasItemLabel))]
    [NotifyPropertyChangedFor(nameof(ContainerLayoutLabel))]
    private string? _parentName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GridCellLabel))]
    private int _gridRow;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GridCellLabel))]
    private int _gridColumn;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GridCellLabel))]
    private int _gridRowSpan = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GridCellLabel))]
    private int _gridColumnSpan = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGridChild))]
    [NotifyPropertyChangedFor(nameof(IsStackPanelChild))]
    [NotifyPropertyChangedFor(nameof(GridCellLabel))]
    [NotifyPropertyChangedFor(nameof(StackPanelItemLabel))]
    private int _stackPanelIndex = -1;

    [ObservableProperty]
    private double _stackPanelItemSize = 40;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGridChild))]
    [NotifyPropertyChangedFor(nameof(IsStackPanelChild))]
    [NotifyPropertyChangedFor(nameof(IsDockPanelChild))]
    [NotifyPropertyChangedFor(nameof(IsWrapPanelChild))]
    [NotifyPropertyChangedFor(nameof(IsUniformGridChild))]
    [NotifyPropertyChangedFor(nameof(IsCanvasChild))]
    [NotifyPropertyChangedFor(nameof(IsContentChild))]
    [NotifyPropertyChangedFor(nameof(GridCellLabel))]
    [NotifyPropertyChangedFor(nameof(StackPanelItemLabel))]
    [NotifyPropertyChangedFor(nameof(DockPanelItemLabel))]
    [NotifyPropertyChangedFor(nameof(WrapPanelItemLabel))]
    [NotifyPropertyChangedFor(nameof(UniformGridItemLabel))]
    [NotifyPropertyChangedFor(nameof(CanvasItemLabel))]
    [NotifyPropertyChangedFor(nameof(ContainerLayoutLabel))]
    private DesignerParentLayoutKind _parentLayout;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DockPanelItemLabel))]
    private int _dockPanelIndex = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DockPanelItemLabel))]
    private DesignerDockSide _dockPanelDock;

    [ObservableProperty]
    private double _dockPanelItemSize = 40;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WrapPanelItemLabel))]
    private int _wrapPanelIndex = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UniformGridItemLabel))]
    private int _uniformGridIndex = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanvasItemLabel))]
    private int _canvasChildIndex = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanvasItemLabel))]
    private double _canvasChildLeft;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanvasItemLabel))]
    private double _canvasChildTop;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStylePreviewState))]
    private string? _stylePreviewStateLabel;

    public double SelectionThickness => IsSelected ? 2.0 : 0.0;

    public bool HasStylePreviewState => !string.IsNullOrWhiteSpace(StylePreviewStateLabel);

    public bool IsContainerChild => !string.IsNullOrWhiteSpace(ParentName);

    public bool IsGridChild => IsContainerChild && ParentLayout == DesignerParentLayoutKind.Grid;

    public bool IsStackPanelChild => IsContainerChild && ParentLayout == DesignerParentLayoutKind.StackPanel;

    public bool IsDockPanelChild => IsContainerChild && ParentLayout == DesignerParentLayoutKind.DockPanel;

    public bool IsWrapPanelChild => IsContainerChild && ParentLayout == DesignerParentLayoutKind.WrapPanel;

    public bool IsUniformGridChild => IsContainerChild && ParentLayout == DesignerParentLayoutKind.UniformGrid;

    public bool IsCanvasChild => IsContainerChild && ParentLayout == DesignerParentLayoutKind.Canvas;

    public bool IsContentChild => IsContainerChild && ParentLayout == DesignerParentLayoutKind.Content;

    public string GridCellLabel
    {
        get
        {
            if (!IsGridChild)
            {
                return string.Empty;
            }

            var span = GridRowSpan > 1 || GridColumnSpan > 1
                ? $" {GridRowSpan}x{GridColumnSpan}"
                : string.Empty;
            return $"GRID R{GridRow + 1} C{GridColumn + 1}{span}";
        }
    }

    public string StackPanelItemLabel
        => IsStackPanelChild ? $"STACK #{StackPanelIndex + 1}" : string.Empty;

    public string DockPanelItemLabel
        => IsDockPanelChild
            ? $"DOCK #{DockPanelIndex + 1} {DockPanelDock.ToString().ToUpperInvariant()}"
            : string.Empty;

    public string WrapPanelItemLabel
        => IsWrapPanelChild ? $"WRAP #{WrapPanelIndex + 1}" : string.Empty;

    public string UniformGridItemLabel
        => IsUniformGridChild ? $"UNIFORM #{UniformGridIndex + 1}" : string.Empty;

    public string CanvasItemLabel
        => IsCanvasChild
            ? $"CANVAS #{CanvasChildIndex + 1} ({CanvasChildLeft:0.#}, {CanvasChildTop:0.#})"
            : string.Empty;

    public string ContainerLayoutLabel
        => IsContentChild ? "CONTENT" : string.Empty;
}
