using Avalonia.Controls;
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
    [NotifyPropertyChangedFor(nameof(IsGridChild))]
    [NotifyPropertyChangedFor(nameof(GridCellLabel))]
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
    [NotifyPropertyChangedFor(nameof(HasStylePreviewState))]
    private string? _stylePreviewStateLabel;

    public double SelectionThickness => IsSelected ? 2.0 : 0.0;

    public bool HasStylePreviewState => !string.IsNullOrWhiteSpace(StylePreviewStateLabel);

    public bool IsGridChild => !string.IsNullOrWhiteSpace(ParentName);

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
}
