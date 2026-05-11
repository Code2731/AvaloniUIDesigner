using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUIDesigner.App.ViewModels;

public partial class ObjectTreeViewModel : ViewModelBase
{
    public ObjectTreeViewModel()
    {
        Root = new ObjectNodeViewModel("Window (Root)");
        Nodes = new ObservableCollection<ObjectNodeViewModel> { Root };
    }

    public ObservableCollection<ObjectNodeViewModel> Nodes { get; }
    public ObjectNodeViewModel Root { get; }

    [ObservableProperty]
    private ObjectNodeViewModel? _selectedNode;

    public ObjectNodeViewModel Add(DesignElement element)
    {
        var node = new ObjectNodeViewModel(element.DisplayName) { Element = element };
        Root.Children.Add(node);
        return node;
    }

    public void RebuildFrom(System.Collections.Generic.IEnumerable<DesignElement> elements)
    {
        Root.Children.Clear();
        foreach (var element in elements)
        {
            Add(element);
        }

        SelectedNode = null;
    }

    public void SelectByElement(DesignElement? element)
    {
        if (element is null)
        {
            SelectedNode = null;
            return;
        }

        foreach (var node in Root.Children)
        {
            if (ReferenceEquals(node.Element, element))
            {
                SelectedNode = node;
                return;
            }
        }

        SelectedNode = null;
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
