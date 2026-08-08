using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AvaloniaUIDesigner.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUIDesigner.App.ViewModels;

public sealed partial class ToolboxCategoryViewModel : ObservableObject
{
    private readonly Action<ToolboxItem> _itemSelected;

    public ToolboxCategoryViewModel(
        string category,
        IEnumerable<ToolboxItem> items,
        bool isExpanded,
        Func<ToolboxItem, bool> isFavorite,
        Action<ToolboxItem> itemSelected)
    {
        Category = category;
        _itemSelected = itemSelected;
        Items = new ObservableCollection<ToolboxItemPresentation>(
            items.Select(item => new ToolboxItemPresentation(item)
            {
                IsFavorite = isFavorite(item),
            }));
        IsExpanded = isExpanded;
    }

    public string Category { get; }

    public ObservableCollection<ToolboxItemPresentation> Items { get; }

    public int ItemCount => Items.Count;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private ToolboxItemPresentation? _selectedItem;

    partial void OnSelectedItemChanged(ToolboxItemPresentation? value)
    {
        if (value is not null)
        {
            _itemSelected(value.Item);
        }
    }

    public void SetSelectedItem(ToolboxItemPresentation? item)
    {
        SelectedItem = item;
    }
}
