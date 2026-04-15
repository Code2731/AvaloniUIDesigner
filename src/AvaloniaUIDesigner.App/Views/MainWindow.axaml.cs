using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaUIDesigner.App.ViewModels;

namespace AvaloniaUIDesigner.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainWindowViewModel? Vm => DataContext as MainWindowViewModel;

    private void OnDesignHostPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm is null) return;
        if (sender is not Control host) return;

        var point = e.GetPosition(host);
        Vm.PlaceFromToolbox(point.X, point.Y);
        e.Handled = true;
    }

    // Handled=true로 버블링 차단: 같은 클릭이 DesignHost 핸들러에 재진입하면 새 요소가 배치됨.
    private void OnElementPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm is null) return;
        if (sender is not Control { DataContext: DesignElement element }) return;

        Vm.SelectElement(element);
        e.Handled = true;
    }
}
