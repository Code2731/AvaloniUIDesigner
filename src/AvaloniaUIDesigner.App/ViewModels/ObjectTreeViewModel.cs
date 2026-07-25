using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUIDesigner.App.ViewModels;

public partial class ObjectTreeViewModel : ViewModelBase
{
    private readonly System.Collections.Generic.List<ObjectNodeViewModel> _allChildren = [];

    public ObjectTreeViewModel()
    {
        Root = new ObjectNodeViewModel("Window (Root)");
        Nodes = new ObservableCollection<ObjectNodeViewModel> { Root };
    }

    public ObservableCollection<ObjectNodeViewModel> Nodes { get; }
    public ObjectNodeViewModel Root { get; }

    [ObservableProperty]
    private ObjectNodeViewModel? _selectedNode;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public bool IsSearchActive => !string.IsNullOrWhiteSpace(SearchText);
    public string SearchResultText => !IsSearchActive
        ? string.Empty
        : Root.Children.Count == 0
            ? "No matching controls"
            : $"{Root.Children.Count} matching control(s)";

    partial void OnSearchTextChanged(string value)
    {
        RefreshVisibleChildren();
        OnPropertyChanged(nameof(IsSearchActive));
        OnPropertyChanged(nameof(SearchResultText));

        if (IsSearchActive && System.Linq.Enumerable.FirstOrDefault(Root.Children) is { } match)
        {
            SelectedNode = match;
        }
    }

    public ObjectNodeViewModel Add(DesignElement element)
    {
        var node = new ObjectNodeViewModel(element.DisplayName) { Element = element };
        _allChildren.Add(node);
        RefreshVisibleChildren();
        return node;
    }

    public void RebuildFrom(System.Collections.Generic.IEnumerable<DesignElement> elements)
    {
        _allChildren.Clear();
        foreach (var element in elements)
        {
            _allChildren.Add(new ObjectNodeViewModel(element.DisplayName) { Element = element });
        }

        RefreshVisibleChildren();
        SelectedNode = null;
    }

    public void SelectByElement(DesignElement? element)
    {
        if (element is null)
        {
            SelectedNode = null;
            return;
        }

        foreach (var node in _allChildren)
        {
            if (ReferenceEquals(node.Element, element))
            {
                SelectedNode = node;
                return;
            }
        }

        SelectedNode = null;
    }

    private void RefreshVisibleChildren()
    {
        var query = SearchText.Trim();
        var visibleChildren = string.IsNullOrWhiteSpace(query)
            ? (System.Collections.Generic.IEnumerable<ObjectNodeViewModel>)_allChildren
            : System.Linq.Enumerable.Where(_allChildren, node => MatchesQuery(node, query));

        Root.Children.Clear();
        foreach (var node in visibleChildren)
        {
            Root.Children.Add(node);
        }

        OnPropertyChanged(nameof(SearchResultText));
    }

    private static bool MatchesQuery(ObjectNodeViewModel node, string query)
        => node.DisplayName.Contains(query, System.StringComparison.OrdinalIgnoreCase)
            || node.Element?.TypeName.Contains(query, System.StringComparison.OrdinalIgnoreCase) == true;
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
