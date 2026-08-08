using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AvaloniaUIDesigner.App.Designer.Contracts;
using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Designer.Services;
using AvaloniaUIDesigner.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUIDesigner.App.ViewModels;

public partial class ToolboxViewModel : ViewModelBase
{
    public const string AllCategories = "All categories";
    public const string RecentCategory = "Recent";
    public const string FavoritesCategory = "Favorites";

    private const int MaxRecentItems = 8;

    private readonly List<ToolboxItem> _allItems;
    private readonly Dictionary<string, bool> _categoryExpandedStates =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _favoriteNames =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _recentNames = new();

    public ToolboxViewModel()
        : this(new BuiltInComponentCatalog())
    {
    }

    public ToolboxViewModel(IComponentCatalog componentCatalog)
    {
        _allItems = componentCatalog
            .GetAll()
            .Select(CreateToolboxItem)
            .ToList();

        _allItems.Add(new ToolboxItem(
            "Preset: Form Field",
            "Preset.FormField",
            [
                new DesignerElementSnapshot("Label", "Avalonia.Controls.TextBlock", 0, 0, 220, 24,
                    new System.Collections.Generic.Dictionary<string, string> { ["Text"] = "Label" }),
                new DesignerElementSnapshot("Input", "Avalonia.Controls.TextBox", 0, 28, 220, 32,
                    new System.Collections.Generic.Dictionary<string, string> { ["Watermark"] = "Enter value" }),
            ],
            Category: "Presets"));

        _allItems.Add(new ToolboxItem(
            "Preset: Volume Control",
            "Preset.VolumeControl",
            [
                new DesignerElementSnapshot("VolumeLabel", "Avalonia.Controls.TextBlock", 0, 0, 180, 24,
                    new System.Collections.Generic.Dictionary<string, string> { ["Text"] = "Volume" }),
                new DesignerElementSnapshot("VolumeSlider", "Avalonia.Controls.Slider", 0, 28, 180, 32,
                    new System.Collections.Generic.Dictionary<string, string>
                    {
                        ["Minimum"] = "0",
                        ["Maximum"] = "100",
                        ["Value"] = "50",
                    }),
            ],
            Category: "Presets"));

        Items = new ObservableCollection<ToolboxItem>();
        CategoryOptions = new ObservableCollection<string>();
        Categories = new ObservableCollection<ToolboxCategoryViewModel>();
        RefreshCategoryOptions();
        ApplyFilter();
    }

    public ObservableCollection<ToolboxItem> Items { get; }

    public ObservableCollection<string> CategoryOptions { get; }

    public ObservableCollection<ToolboxCategoryViewModel> Categories { get; }

    public IReadOnlyList<string> FavoriteItemNames => _favoriteNames.ToList();

    public IReadOnlyList<string> RecentItemNames => _recentNames.ToList();

    public ToolboxItem? FindItemByDisplayName(string displayName) =>
        _allItems.FirstOrDefault(item => string.Equals(
            item.DisplayName,
            displayName,
            System.StringComparison.OrdinalIgnoreCase));

    public bool IsFavorite(ToolboxItem item)
        => _favoriteNames.Contains(item.DisplayName);

    public bool ToggleFavorite(ToolboxItem item)
    {
        var displayName = item.DisplayName.Trim();
        var isFavorite = !_favoriteNames.Remove(displayName);
        if (isFavorite)
        {
            _favoriteNames.Add(displayName);
        }

        RefreshCategories();
        return isFavorite;
    }

    public void RecordPlacement(ToolboxItem item)
    {
        var displayName = item.DisplayName.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return;
        }

        _recentNames.RemoveAll(name => string.Equals(
            name,
            displayName,
            StringComparison.OrdinalIgnoreCase));
        _recentNames.Insert(0, displayName);
        if (_recentNames.Count > MaxRecentItems)
        {
            _recentNames.RemoveRange(MaxRecentItems, _recentNames.Count - MaxRecentItems);
        }

        RefreshCategories();
    }

    public void RestoreUsageState(
        IEnumerable<string>? favoriteNames,
        IEnumerable<string>? recentNames)
    {
        _favoriteNames.Clear();
        foreach (var name in favoriteNames ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                _favoriteNames.Add(name.Trim());
            }
        }

        _recentNames.Clear();
        foreach (var name in recentNames ?? Array.Empty<string>())
        {
            var normalizedName = name.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName)
                || _recentNames.Any(existing => string.Equals(
                    existing,
                    normalizedName,
                    StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            _recentNames.Add(normalizedName);
            if (_recentNames.Count == MaxRecentItems)
            {
                break;
            }
        }

        RefreshCategories();
    }

    public void AddComponents(IEnumerable<DesignerComponentDefinition> definitions)
    {
        _allItems.AddRange(definitions.Select(CreateToolboxItem));
        RefreshCategoryOptions();
        ApplyFilter();
    }

    public int RemoveComponentsBySourceId(string sourceId)
    {
        var removed = _allItems.RemoveAll(item => string.Equals(
            item.SourceId,
            sourceId,
            System.StringComparison.OrdinalIgnoreCase));
        if (removed == 0)
        {
            return 0;
        }

        if (SelectedItem?.SourceId is { } selectedSourceId
            && string.Equals(selectedSourceId, sourceId, System.StringComparison.OrdinalIgnoreCase))
        {
            SelectedItem = null;
        }

        RefreshCategoryOptions();
        ApplyFilter();
        return removed;
    }

    public bool TryAddPreset(ToolboxItem preset, out string error)
        => TryAddPresets(new[] { preset }, out error);

    public bool TryAddPresets(IEnumerable<ToolboxItem> presets, out string error)
    {
        error = string.Empty;
        var normalizedPresets = new List<ToolboxItem>();
        var names = new HashSet<string>(
            _allItems.Select(item => item.DisplayName),
            System.StringComparer.OrdinalIgnoreCase);
        foreach (var preset in presets)
        {
            var displayName = preset.DisplayName.Trim();
            if (!preset.IsPreset)
            {
                error = "The Toolbox preset must contain at least one control.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                error = "Toolbox preset name is required.";
                return false;
            }

            if (!names.Add(displayName))
            {
                error = $"A Toolbox item named '{displayName}' already exists.";
                return false;
            }

            var normalizedPreset = string.Equals(
                    preset.DisplayName,
                    displayName,
                    StringComparison.Ordinal)
                ? preset
                : preset with { DisplayName = displayName };
            normalizedPresets.Add(string.IsNullOrWhiteSpace(normalizedPreset.Category)
                ? normalizedPreset with { Category = "Presets" }
                : normalizedPreset);
        }

        _allItems.AddRange(normalizedPresets);
        RefreshCategoryOptions();
        ApplyFilter();
        if (normalizedPresets.Count == 1 && Items.Contains(normalizedPresets[0]))
        {
            SelectedItem = normalizedPresets[0];
        }

        return true;
    }

    public string SearchResultText => !HasActiveFilter
        ? $"{Items.Count} controls"
        : $"{Items.Count} of {_allItems.Count} controls";

    public bool HasActiveFilter => !string.IsNullOrWhiteSpace(SearchText)
        || !string.Equals(CategoryFilter, AllCategories, StringComparison.Ordinal);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPresetSelected))]
    private ToolboxItem? _selectedItem;

    public bool IsPresetSelected => SelectedItem?.IsPreset == true;

    partial void OnSelectedItemChanged(ToolboxItem? value)
        => UpdateCategorySelection(value);

    private void UpdateCategorySelection(ToolboxItem? value)
    {
        foreach (var category in Categories)
        {
            var selected = category.Items.FirstOrDefault(presentation =>
                ReferenceEquals(presentation.Item, value));
            category.SetSelectedItem(selected);
            foreach (var presentation in category.Items)
            {
                presentation.IsSelected = ReferenceEquals(presentation.Item, value);
            }
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveFilter), nameof(SearchResultText))]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveFilter), nameof(SearchResultText))]
    private string _categoryFilter = AllCategories;

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnCategoryFilterChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var selected = SelectedItem;
        var query = SearchText.Trim();
        var matches = _allItems
            .Where(item =>
            {
                var matchesCategory = string.Equals(CategoryFilter, AllCategories, StringComparison.Ordinal)
                    || string.Equals(item.Category, CategoryFilter, StringComparison.OrdinalIgnoreCase);
                var matchesQuery = string.IsNullOrWhiteSpace(query)
                    || item.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || item.AvaloniaTypeName.Contains(query, StringComparison.OrdinalIgnoreCase);
                return matchesCategory && matchesQuery;
            })
            .OrderBy(item => GetCategoryOrder(item.CategoryLabel))
            .ThenBy(item => item.CategoryLabel, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase);

        Items.Clear();
        foreach (var item in matches)
        {
            Items.Add(item);
        }

        RefreshCategories();

        SelectedItem = null;
        if (selected is not null && Items.Contains(selected))
        {
            SelectedItem = selected;
        }

        OnPropertyChanged(nameof(SearchResultText));
    }

    private void RefreshCategoryOptions()
    {
        var current = CategoryFilter;
        var categories = _allItems
            .Select(item => item.Category)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Select(category => category!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(GetCategoryOrder)
            .ThenBy(category => category, StringComparer.OrdinalIgnoreCase)
            .ToList();

        CategoryOptions.Clear();
        CategoryOptions.Add(AllCategories);
        foreach (var category in categories)
        {
            CategoryOptions.Add(category);
        }

        CategoryFilter = CategoryOptions.Contains(current, StringComparer.OrdinalIgnoreCase)
            ? current
            : AllCategories;
        OnPropertyChanged(nameof(CategoryOptions));
    }

    private void RefreshCategories()
    {
        foreach (var category in Categories)
        {
            _categoryExpandedStates[category.Category] = category.IsExpanded;
        }

        Categories.Clear();
        var showUtilityGroups = string.Equals(
                CategoryFilter,
                AllCategories,
                StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(SearchText);
        if (showUtilityGroups)
        {
            var recentItems = GetRecentItems().ToList();
            if (recentItems.Count > 0)
            {
                AddCategory(RecentCategory, recentItems);
            }

            var favoriteItems = _allItems
                .Where(IsFavorite)
                .OrderBy(item => GetCategoryOrder(item.CategoryLabel))
                .ThenBy(item => item.CategoryLabel, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (favoriteItems.Count > 0)
            {
                AddCategory(FavoritesCategory, favoriteItems);
            }
        }

        foreach (var group in Items.GroupBy(item => item.CategoryLabel))
        {
            AddCategory(group.Key, group);
        }

        UpdateCategorySelection(SelectedItem);
    }

    private void AddCategory(string categoryName, IEnumerable<ToolboxItem> items)
    {
        var category = new ToolboxCategoryViewModel(
            categoryName,
            items,
            _categoryExpandedStates.TryGetValue(categoryName, out var isExpanded)
                ? isExpanded
                : true,
            IsFavorite,
            item => SelectedItem = item);
        Categories.Add(category);
    }

    private IEnumerable<ToolboxItem> GetRecentItems()
        => _recentNames
            .Select(FindItemByDisplayName)
            .Where(item => item is not null)
            .Select(item => item!);

    private static int GetCategoryOrder(string category)
        => category switch
        {
            "Layout" => 0,
            "Containers" => 1,
            "Input" => 2,
            "Display" => 3,
            "Shapes" => 4,
            "Presets" => 5,
            _ => 100,
        };

    private static ToolboxItem CreateToolboxItem(DesignerComponentDefinition definition) => new(
        definition.DisplayName,
        definition.AvaloniaTypeName,
        DefaultWidth: definition.DefaultWidth,
        DefaultHeight: definition.DefaultHeight,
        DefaultProperties: definition.DefaultProperties,
        NamePrefix: definition.NamePrefix,
        SourceId: definition.SourceId,
        Category: string.IsNullOrWhiteSpace(definition.Category)
            ? GetDefaultCategory(definition.AvaloniaTypeName)
            : definition.Category.Trim());

    private static string GetDefaultCategory(string typeName)
        => typeName switch
        {
            "Avalonia.Controls.Grid"
                or "Avalonia.Controls.StackPanel"
                or "Avalonia.Controls.DockPanel"
                or "Avalonia.Controls.WrapPanel"
                or "Avalonia.Controls.Primitives.UniformGrid"
                or "Avalonia.Controls.Canvas"
                or "Avalonia.Controls.GridSplitter" => "Layout",
            "Avalonia.Controls.Border"
                or "Avalonia.Controls.ContentControl"
                or "Avalonia.Controls.UserControl"
                or "Avalonia.Controls.ScrollViewer"
                or "Avalonia.Controls.Expander"
                or "Avalonia.Controls.TabControl"
                or "Avalonia.Controls.SplitView" => "Containers",
            "Avalonia.Controls.TextBox"
                or "Avalonia.Controls.Button"
                or "Avalonia.Controls.MaskedTextBox"
                or "Avalonia.Controls.AutoCompleteBox"
                or "Avalonia.Controls.ComboBox"
                or "Avalonia.Controls.ListBox"
                or "Avalonia.Controls.CheckBox"
                or "Avalonia.Controls.RadioButton"
                or "Avalonia.Controls.ToggleSwitch"
                or "Avalonia.Controls.Primitives.ToggleButton"
                or "Avalonia.Controls.Slider"
                or "Avalonia.Controls.NumericUpDown"
                or "Avalonia.Controls.DatePicker"
                or "Avalonia.Controls.CalendarDatePicker"
                or "Avalonia.Controls.Calendar"
                or "Avalonia.Controls.TimePicker"
                or "Avalonia.Controls.ColorPicker" => "Input",
            "Avalonia.Controls.TextBlock"
                or "Avalonia.Controls.SelectableTextBlock"
                or "Avalonia.Controls.Label"
                or "Avalonia.Controls.Image"
                or "Avalonia.Controls.ProgressBar"
                or "Avalonia.Controls.ItemsControl"
                or "Avalonia.Controls.TreeView"
                or "Avalonia.Controls.Menu"
                or "Avalonia.Controls.DataGrid" => "Display",
            "Avalonia.Controls.Shapes.Rectangle"
                or "Avalonia.Controls.Shapes.Ellipse"
                or "Avalonia.Controls.Shapes.Line"
                or "Avalonia.Controls.Shapes.Path" => "Shapes",
            _ => "General",
        };
}
