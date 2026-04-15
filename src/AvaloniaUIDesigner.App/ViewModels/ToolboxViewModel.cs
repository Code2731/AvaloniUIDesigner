using System.Collections.ObjectModel;
using AvaloniaUIDesigner.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUIDesigner.App.ViewModels;

public partial class ToolboxViewModel : ViewModelBase
{
    // v0.1에서는 하드코딩. 이후 어셈블리 스캔으로 전환 예정.
    public ObservableCollection<ToolboxItem> Items { get; } = new()
    {
        new ToolboxItem("Button", "Avalonia.Controls.Button"),
        new ToolboxItem("TextBox", "Avalonia.Controls.TextBox"),
        new ToolboxItem("TextBlock", "Avalonia.Controls.TextBlock"),
    };

    [ObservableProperty]
    private ToolboxItem? _selectedItem;
}
