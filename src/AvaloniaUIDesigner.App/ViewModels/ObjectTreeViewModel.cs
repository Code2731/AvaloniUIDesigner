using System.Collections.ObjectModel;
using System.Linq;
using AvaloniaUIDesigner.App.Designer.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUIDesigner.App.ViewModels;

public partial class ObjectTreeViewModel : ViewModelBase
{
    private readonly System.Collections.Generic.List<ObjectNodeViewModel> _allChildren = [];

    public ObjectTreeViewModel()
    {
        Root = new ObjectNodeViewModel("Window (Root)");
        Root.IsExpanded = true;
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
            : SelectedNode is { } selectedNode
                && Root.Children.IndexOf(selectedNode) is var selectedIndex
                && selectedIndex >= 0
                ? $"{selectedIndex + 1} of {Root.Children.Count} matching control(s)"
                : $"{Root.Children.Count} matching control(s)";

    partial void OnSelectedNodeChanged(ObjectNodeViewModel? value)
    {
        OnPropertyChanged(nameof(SearchResultText));
    }

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
        var expandedNames = _allChildren
            .Where(node => node.IsExpanded)
            .Select(node => node.DisplayName)
            .ToHashSet(System.StringComparer.OrdinalIgnoreCase);
        _allChildren.Clear();
        foreach (var element in elements)
        {
            _allChildren.Add(new ObjectNodeViewModel(element.DisplayName)
            {
                Element = element,
                IsExpanded = expandedNames.Contains(element.DisplayName),
            });
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
                if (!IsSearchActive)
                {
                    ExpandAncestors(node);
                }

                SelectedNode = node;
                return;
            }
        }

        SelectedNode = null;
    }

    public void SetDropFeedback(ObjectNodeViewModel? target, bool accepted)
        => SetDropFeedback(target, accepted, insertBefore: false, insertAfter: false);

    public void SetDropFeedback(
        ObjectNodeViewModel? target,
        bool accepted,
        bool insertBefore,
        bool insertAfter)
    {
        foreach (var node in _allChildren)
        {
            node.IsDropTarget = false;
            node.IsDropRejected = false;
            node.IsDropBefore = false;
            node.IsDropAfter = false;
        }

        if (target is not null)
        {
            target.IsDropTarget = accepted;
            target.IsDropRejected = !accepted;
            target.IsDropBefore = accepted && insertBefore;
            target.IsDropAfter = accepted && insertAfter;
        }
    }

    public void ClearDropFeedback() => SetDropFeedback(null, accepted: false);

    private void ExpandAncestors(ObjectNodeViewModel node)
    {
        Root.IsExpanded = true;
        var current = node;
        while (true)
        {
            var parent = _allChildren.FirstOrDefault(candidate => candidate.Children.Contains(current));
            if (parent is null)
            {
                return;
            }

            parent.IsExpanded = true;
            current = parent;
        }
    }

    public bool SelectNextMatch(bool reverse = false)
    {
        if (!IsSearchActive || Root.Children.Count == 0)
        {
            return false;
        }

        var currentIndex = SelectedNode is { } selectedNode
            ? Root.Children.IndexOf(selectedNode)
            : -1;
        var nextIndex = reverse
            ? currentIndex <= 0 ? Root.Children.Count - 1 : currentIndex - 1
            : currentIndex < 0 || currentIndex == Root.Children.Count - 1 ? 0 : currentIndex + 1;

        SelectedNode = Root.Children[nextIndex];
        return true;
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
                        or Avalonia.Controls.WrapPanel
                        or Avalonia.Controls.Primitives.UniformGrid
                        or Avalonia.Controls.Canvas
                        or Avalonia.Controls.TabControl
                        or Avalonia.Controls.SplitView
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

            foreach (var parent in _allChildren.Where(node =>
                         node.Element?.Visual is Avalonia.Controls.WrapPanel
                         && node.Children.Count > 1))
            {
                var ordered = parent.Children
                    .OrderBy(node => node.Element?.WrapPanelIndex ?? int.MaxValue)
                    .ToList();
                parent.Children.Clear();
                foreach (var child in ordered)
                {
                    parent.Children.Add(child);
                }
            }

            foreach (var parent in _allChildren.Where(node =>
                         node.Element?.Visual is Avalonia.Controls.Primitives.UniformGrid
                         && node.Children.Count > 1))
            {
                var ordered = parent.Children
                    .OrderBy(node => node.Element?.UniformGridIndex ?? int.MaxValue)
                    .ToList();
                parent.Children.Clear();
                foreach (var child in ordered)
                {
                    parent.Children.Add(child);
                }
            }

            foreach (var parent in _allChildren.Where(node =>
                         node.Element?.Visual is Avalonia.Controls.Canvas
                         && node.Children.Count > 1))
            {
                var ordered = parent.Children
                    .OrderBy(node => node.Element?.CanvasChildIndex ?? int.MaxValue)
                    .ToList();
                parent.Children.Clear();
                foreach (var child in ordered)
                {
                    parent.Children.Add(child);
                }
            }

            foreach (var parent in _allChildren.Where(node =>
                         node.Element?.Visual is Avalonia.Controls.TabControl
                         && node.Children.Count > 1))
            {
                var ordered = parent.Children
                    .OrderBy(node => node.Element?.TabIndex ?? int.MaxValue)
                    .ToList();
                parent.Children.Clear();
                foreach (var child in ordered)
                {
                    parent.Children.Add(child);
                }
            }

            foreach (var parent in _allChildren.Where(node =>
                         node.Element?.Visual is Avalonia.Controls.SplitView
                         && node.Children.Count > 1))
            {
                var ordered = parent.Children
                    .OrderBy(node => node.Element?.SplitViewSlot ?? DesignerSplitViewSlot.Content)
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

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isDropTarget;

    [ObservableProperty]
    private bool _isDropRejected;

    [ObservableProperty]
    private bool _isDropBefore;

    [ObservableProperty]
    private bool _isDropAfter;
}
