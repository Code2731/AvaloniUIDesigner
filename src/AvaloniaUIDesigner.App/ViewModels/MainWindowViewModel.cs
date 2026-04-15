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
}
