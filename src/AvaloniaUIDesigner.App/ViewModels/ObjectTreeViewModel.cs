using System.Collections.ObjectModel;
using System.Linq;
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
        Root.Children.Clear();
        foreach (var node in _allChildren)
        {
            node.Children.Clear();
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            foreach (var node in System.Linq.Enumerable.Where(_allChildren, node => MatchesQuery(node, query)))
            {
                Root.Children.Add(node);
            }
        }
        else
        {
            var nodesByName = _allChildren
                .Where(node => node.Element is not null)
                .ToDictionary(node => node.Element!.DisplayName, System.StringComparer.OrdinalIgnoreCase);
            foreach (var node in _allChildren)
            {
                if (node.Element?.ParentName is { Length: > 0 } parentName
                    && nodesByName.TryGetValue(parentName, out var parent)
                    && parent.Element?.Visual is Avalonia.Controls.Grid
                        or Avalonia.Controls.StackPanel
                        or Avalonia.Controls.DockPanel
                        or Avalonia.Controls.Border
                        or Avalonia.Controls.ScrollViewer
                        or Avalonia.Controls.Expander)
                {
                    parent.Children.Add(node);
                }
                else
                {
                    Root.Children.Add(node);
                }
            }

            foreach (var parent in _allChildren.Where(node =>
                         node.Element?.Visual is Avalonia.Controls.StackPanel
                         && node.Children.Count > 1))
            {
                var ordered = parent.Children
                    .OrderBy(node => node.Element?.StackPanelIndex ?? int.MaxValue)
                    .ToList();
                parent.Children.Clear();
                foreach (var child in ordered)
                {
                    parent.Children.Add(child);
                }
            }

            foreach (var parent in _allChildren.Where(node =>
                         node.Element?.Visual is Avalonia.Controls.DockPanel
                         && node.Children.Count > 1))
            {
                var ordered = parent.Children
                    .OrderBy(node => node.Element?.DockPanelIndex ?? int.MaxValue)
                    .ToList();
                parent.Children.Clear();
                foreach (var child in ordered)
                {
                    parent.Children.Add(child);
                }
            }
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
