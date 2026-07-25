using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUIDesigner.App.ViewModels;

public partial class DesignElement : ViewModelBase
{
    public DesignElement(string displayName, string typeName, Control visual, double x, double y, double width, double height)
    {
        DisplayName = displayName;
        TypeName = typeName;
        Visual = visual;
        _x = x;
        _y = y;
        _width = width;
        _height = height;
    }

    public string DisplayName { get; }
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

    public double SelectionThickness => IsSelected ? 2.0 : 0.0;
}
