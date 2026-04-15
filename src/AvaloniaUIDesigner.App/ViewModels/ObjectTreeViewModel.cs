using System.Collections.ObjectModel;

namespace AvaloniaUIDesigner.App.ViewModels;

public partial class ObjectTreeViewModel : ViewModelBase
{
    // v0.1 골격. 실제 디자인 트리 연동은 이후 단계.
    public ObservableCollection<ObjectNodeViewModel> Nodes { get; } = new()
    {
        new ObjectNodeViewModel("Window (루트)"),
    };
}

public partial class ObjectNodeViewModel : ViewModelBase
{
    public ObjectNodeViewModel(string displayName)
    {
        DisplayName = displayName;
    }

    public string DisplayName { get; }

    public ObservableCollection<ObjectNodeViewModel> Children { get; } = new();
}
