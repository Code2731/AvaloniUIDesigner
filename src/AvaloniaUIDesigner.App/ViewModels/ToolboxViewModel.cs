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
        Items = new ObservableCollection<ToolboxItem>(
            componentCatalog
                .GetAll()
                .Select(def => new ToolboxItem(def.DisplayName, def.AvaloniaTypeName)));

        Items.Add(new ToolboxItem(
            "Preset: Form Field",
            "Preset.FormField",
            [
                new DesignerElementSnapshot("Label", "Avalonia.Controls.TextBlock", 0, 0, 220, 24,
                    new System.Collections.Generic.Dictionary<string, string> { ["Text"] = "Label" }),
                new DesignerElementSnapshot("Input", "Avalonia.Controls.TextBox", 0, 28, 220, 32,
                    new System.Collections.Generic.Dictionary<string, string> { ["Watermark"] = "Enter value" }),
            ]));

        Items.Add(new ToolboxItem(
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

        _allItems = Items.ToList();
    }

    public ObservableCollection<ToolboxItem> Items { get; }

    public ToolboxItem? FindItem(string avaloniaTypeName) =>
        _allItems.FirstOrDefault(item => string.Equals(
            item.AvaloniaTypeName,
            avaloniaTypeName,
            System.StringComparison.Ordinal));

    public string SearchResultText => string.IsNullOrWhiteSpace(SearchText)
        ? $"{Items.Count} controls"
        : $"{Items.Count} of {_allItems.Count} controls";

    [ObservableProperty]
    private ToolboxItem? _selectedItem;

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        var selected = SelectedItem;
        var query = value.Trim();
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
}
