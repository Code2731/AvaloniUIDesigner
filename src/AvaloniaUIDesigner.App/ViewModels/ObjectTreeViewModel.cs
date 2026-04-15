using System.Collections.ObjectModel;

namespace AvaloniaUIDesigner.App.ViewModels;

public partial class ObjectTreeViewModel : ViewModelBase
{
    public ObjectTreeViewModel()
    {
        Root = new ObjectNodeViewModel("Window (루트)");
        Nodes = new ObservableCollection<ObjectNodeViewModel> { Root };
    }

    public ObservableCollection<ObjectNodeViewModel> Nodes { get; }
    public ObjectNodeViewModel Root { get; }

    public ObjectNodeViewModel Add(DesignElement element)
    {
        var node = new ObjectNodeViewModel(element.DisplayName) { Element = element };
        Root.Children.Add(node);
        return node;
    }
}

public partial class ObjectNodeViewModel : ViewModelBase
{
    public ObjectNodeViewModel(string displayName)
    {
        DisplayName = displayName;
    }

    public string DisplayName { get; }
    public DesignElement? Element { get; init; }
    public ObservableCollection<ObjectNodeViewModel> Children { get; } = new();
}
