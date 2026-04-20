using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Controls;
using AvaloniaUIDesigner.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUIDesigner.App.ViewModels;

public partial class CanvasViewModel : ViewModelBase
{
    public CanvasViewModel()
    {
        Elements.CollectionChanged += OnElementsChanged;
    }

    public ObservableCollection<DesignElement> Elements { get; } = new();

    public string PlaceholderText { get; } = "툴박스에서 컨트롤을 선택 후 캔버스를 클릭하세요";

    [ObservableProperty]
    private bool _hasElements;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelectionActive))]
    private DesignElement? _selectedElement;

    public bool IsSelectionActive => SelectedElement is not null;

    public DesignElement PlaceElement(ToolboxItem item, double x, double y)
    {
        var (visual, width, height) = CreateVisual(item);
        var element = new DesignElement(
            displayName: $"{item.DisplayName}{Elements.Count + 1}",
            typeName: item.AvaloniaTypeName,
            visual: visual,
            x: x,
            y: y,
            width: width,
            height: height);

        Elements.Add(element);
        SelectedElement = element;
        return element;
    }

    public void Select(DesignElement? element) => SelectedElement = element;

    partial void OnSelectedElementChanged(DesignElement? oldValue, DesignElement? newValue)
    {
        if (oldValue is not null) oldValue.IsSelected = false;
        if (newValue is not null) newValue.IsSelected = true;
    }

    private void OnElementsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => HasElements = Elements.Count > 0;

    // v0.2: 하드코딩 팩토리. v0.3+에서 리플렉션/플러그인화 예정.
    private static (Control Visual, double Width, double Height) CreateVisual(ToolboxItem item)
    {
        return item.AvaloniaTypeName switch
        {
            "Avalonia.Controls.Button" => (new Button { Content = "Button" }, 100d, 32d),
            "Avalonia.Controls.TextBox" => (new TextBox { Text = "TextBox", Watermark = "입력" }, 160d, 32d),
            "Avalonia.Controls.TextBlock" => (new TextBlock { Text = "TextBlock", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center }, 100d, 24d),
            _ => (new TextBlock { Text = $"[알 수 없음: {item.DisplayName}]" }, 160d, 24d),
        };
    }
}
