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

    private readonly List<ToolboxItem> _allItems;

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
        RefreshCategoryOptions();
        ApplyFilter();
    }

    public ObservableCollection<ToolboxItem> Items { get; }

    public ObservableCollection<string> CategoryOptions { get; }

    public ToolboxItem? FindItemByDisplayName(string displayName) =>
        _allItems.FirstOrDefault(item => string.Equals(
            item.DisplayName,
            displayName,
            System.StringComparison.OrdinalIgnoreCase));

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
