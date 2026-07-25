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
            ]));

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
            ]));

        Items = new ObservableCollection<ToolboxItem>(_allItems);
    }

    public ObservableCollection<ToolboxItem> Items { get; }

    public ToolboxItem? FindItemByDisplayName(string displayName) =>
        _allItems.FirstOrDefault(item => string.Equals(
            item.DisplayName,
            displayName,
            System.StringComparison.OrdinalIgnoreCase));

    public void AddComponents(IEnumerable<DesignerComponentDefinition> definitions)
    {
        _allItems.AddRange(definitions.Select(CreateToolboxItem));
        ApplyFilter();
    }

    public string SearchResultText => string.IsNullOrWhiteSpace(SearchText)
        ? $"{Items.Count} controls"
        : $"{Items.Count} of {_allItems.Count} controls";

    [ObservableProperty]
    private ToolboxItem? _selectedItem;

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var selected = SelectedItem;
        var query = SearchText.Trim();
        var matches = string.IsNullOrWhiteSpace(query)
            ? _allItems
            : _allItems.Where(item => item.DisplayName.Contains(query, System.StringComparison.OrdinalIgnoreCase)
                || item.AvaloniaTypeName.Contains(query, System.StringComparison.OrdinalIgnoreCase));

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

    private static ToolboxItem CreateToolboxItem(DesignerComponentDefinition definition) => new(
        definition.DisplayName,
        definition.AvaloniaTypeName,
        DefaultWidth: definition.DefaultWidth,
        DefaultHeight: definition.DefaultHeight,
        DefaultProperties: definition.DefaultProperties,
        NamePrefix: definition.NamePrefix);
}
