using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUIDesigner.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ToolboxViewModel Toolbox { get; } = new();
    public CanvasViewModel Canvas { get; } = new();
    public ObjectTreeViewModel ObjectTree { get; } = new();
    public PropertyInspectorViewModel PropertyInspector { get; } = new();

    [ObservableProperty]
    private string _statusText = "준비";

    public void PlaceFromToolbox(double x, double y)
    {
        var item = Toolbox.SelectedItem;
        if (item is null)
        {
            StatusText = "툴박스에서 컨트롤을 먼저 선택하세요";
            return;
        }

        var element = Canvas.PlaceElement(item, x, y);
        ObjectTree.Add(element);
        StatusText = $"{element.DisplayName} 배치됨 ({x:0},{y:0})";
    }

    public void SelectElement(DesignElement? element)
    {
        Canvas.Select(element);
        StatusText = element is null ? "준비" : $"{element.DisplayName} 선택됨";
    }
}
