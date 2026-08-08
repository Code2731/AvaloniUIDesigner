using AvaloniaUIDesigner.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUIDesigner.App.ViewModels;

public sealed partial class ToolboxItemPresentation : ObservableObject
{
    public ToolboxItemPresentation(ToolboxItem item)
    {
        Item = item;
    }

    public ToolboxItem Item { get; }

    public string DisplayName => Item.DisplayName;

    public string AvaloniaTypeName => Item.AvaloniaTypeName;

    public string CategoryLabel => Item.CategoryLabel;

    public string RowBackground => IsSelected ? "#334155" : "#2A2D2E";

    public string RowBorderBrush => IsSelected ? "#60A5FA" : "#2A2D2E";

    public string FavoriteGlyph => IsFavorite ? "\u2605" : "\u2606";

    public string FavoriteToolTip => IsFavorite
        ? "Remove from favorites"
        : "Add to favorites";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowBackground), nameof(RowBorderBrush))]
    private bool _isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteGlyph), nameof(FavoriteToolTip))]
    private bool _isFavorite;
}
