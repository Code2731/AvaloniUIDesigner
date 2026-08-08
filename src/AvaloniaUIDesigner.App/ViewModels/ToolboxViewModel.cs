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

            normalizedPresets.Add(string.Equals(
                    preset.DisplayName,
                    displayName,
                    System.StringComparison.Ordinal)
                ? preset
                : preset with { DisplayName = displayName });
        }

        _allItems.AddRange(normalizedPresets);
        ApplyFilter();
        if (normalizedPresets.Count == 1 && Items.Contains(normalizedPresets[0]))
        {
            SelectedItem = normalizedPresets[0];
        }

        return true;
    }

    public string SearchResultText => string.IsNullOrWhiteSpace(SearchText)
        ? $"{Items.Count} controls"
        : $"{Items.Count} of {_allItems.Count} controls";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPresetSelected))]
    private ToolboxItem? _selectedItem;

    public bool IsPresetSelected => SelectedItem?.IsPreset == true;

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
        NamePrefix: definition.NamePrefix,
        SourceId: definition.SourceId);
}
