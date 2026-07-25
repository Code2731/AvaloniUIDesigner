using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaUIDesigner.App.Designer.Contracts;
using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Designer.Services;
using AvaloniaUIDesigner.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUIDesigner.App.ViewModels;

public partial class CanvasViewModel : ViewModelBase
{
    private readonly IComponentCatalog _componentCatalog;
    private readonly IControlRenderer _renderer;

    public CanvasViewModel()
        : this(new BuiltInComponentCatalog(), new DefaultControlRenderer())
    {
    }

    public CanvasViewModel(IComponentCatalog componentCatalog, IControlRenderer renderer)
    {
        _componentCatalog = componentCatalog;
        _renderer = renderer;
        Elements.CollectionChanged += OnElementsChanged;
        SelectedElements.CollectionChanged += OnSelectedElementsChanged;
    }

    public ObservableCollection<DesignElement> Elements { get; } = new();

    public ObservableCollection<DesignElement> SelectedElements { get; } = new();

    public string PlaceholderText { get; } = "Select a control in Toolbox, then click the canvas.";

    [ObservableProperty]
    private bool _hasElements;

    [ObservableProperty]
    private bool _snapToGrid = true;

    [ObservableProperty]
    private double _gridSize = 8;

    [ObservableProperty]
    private bool _isGridVisible = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ArtboardBrush))]
    private string _artboardBackground = "#FFFFFF";

    [ObservableProperty]
    private double _artboardWidth = 1280;

    [ObservableProperty]
    private double _artboardHeight = 800;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ZoomPercentage))]
    private double _zoomScale = 1;

    [ObservableProperty]
    private DesignElement? _selectedElement;

    public bool IsSelectionActive => SelectedElements.Count > 0;

    public bool HasMultipleSelection => SelectedElements.Count > 1;

    public string ZoomPercentage => $"{ZoomScale * 100:0}%";

    public IBrush ArtboardBrush => Brush.Parse(ArtboardBackground);

    public double SnapPosition(double value)
        => SnapToGrid && GridSize > 0 ? Math.Round(value / GridSize) * GridSize : value;

    public double SnapSize(double value, double minimum)
        => Math.Max(minimum, SnapPosition(value));

    public void SetGridSize(double gridSize) => GridSize = Math.Clamp(gridSize, 4, 32);

    public void SetArtboard(double width, double height, string? background = null)
    {
        ArtboardWidth = Math.Clamp(width, 320, 3840);
        ArtboardHeight = Math.Clamp(height, 240, 2160);
        if (!string.IsNullOrWhiteSpace(background))
        {
            ArtboardBackground = background;
        }
    }

    public void ZoomIn() => SetZoom(ZoomScale + 0.1);

    public void ZoomOut() => SetZoom(ZoomScale - 0.1);

    public void ResetZoom() => SetZoom(1);

    public void FitToViewport(double viewportWidth, double viewportHeight)
    {
        if (viewportWidth <= 0 || viewportHeight <= 0 || Elements.Count == 0)
        {
            ResetZoom();
            return;
        }

        const double padding = 32;
        var contentWidth = Elements.Max(element => element.X + element.Width) + padding;
        var contentHeight = Elements.Max(element => element.Y + element.Height) + padding;
        SetZoom(Math.Min(viewportWidth / contentWidth, viewportHeight / contentHeight));
    }

    private void SetZoom(double zoom) => ZoomScale = Math.Clamp(zoom, 0.25, 2);

    public DesignElement PlaceElement(ToolboxItem item, double x, double y)
    {
        var (visual, width, height) = CreateVisualByType(item.AvaloniaTypeName, item.DisplayName);
        return AddElement(item.DisplayName, item.AvaloniaTypeName, visual, x, y, width, height, select: true);
    }

    public bool TrySetSelectedImageSource(string source)
    {
        if (SelectedElement?.Visual is not Image image)
        {
            return false;
        }

        return TryLoadImageSource(image, source, retainSourceOnFailure: false);
    }

    public DesignElement AddElementFromSnapshot(DesignerElementSnapshot snapshot, bool select = false)
    {
        var (visual, defaultWidth, defaultHeight) = CreateVisualByType(snapshot.TypeName, snapshot.DisplayName);
        var width = snapshot.Width > 0 ? snapshot.Width : defaultWidth;
        var height = snapshot.Height > 0 ? snapshot.Height : defaultHeight;

        ApplyVisualProperties(visual, snapshot.VisualProperties);

        var element = AddElement(
            snapshot.DisplayName,
            snapshot.TypeName,
            visual,
            snapshot.X,
            snapshot.Y,
            width,
            height,
            select,
            preserveDisplayName: true);
        element.IsLocked = snapshot.IsLocked;
        return element;
    }

    public IReadOnlyList<DesignElement> PlacePreset(ToolboxItem preset, double x, double y)
    {
        if (!preset.IsPreset || preset.PresetElements is null)
        {
            return Array.Empty<DesignElement>();
        }

        var placed = new List<DesignElement>();
        foreach (var snapshot in preset.PresetElements)
        {
            var (visual, defaultWidth, defaultHeight) = CreateVisualByType(snapshot.TypeName, snapshot.DisplayName);
            ApplyVisualProperties(visual, snapshot.VisualProperties);
            placed.Add(AddElement(
                snapshot.DisplayName,
                snapshot.TypeName,
                visual,
                SnapPosition(x + snapshot.X),
                SnapPosition(y + snapshot.Y),
                snapshot.Width > 0 ? snapshot.Width : defaultWidth,
                snapshot.Height > 0 ? snapshot.Height : defaultHeight,
                select: false));
        }

        SelectMany(placed);
        return placed;
    }

    public void Clear()
    {
        ClearSelection();
        Elements.Clear();
    }

    public void Select(DesignElement? element, bool toggle = false)
    {
        if (element is null)
        {
            ClearSelection();
            return;
        }

        if (toggle)
        {
            if (SelectedElements.Remove(element))
            {
                element.IsSelected = false;
                SelectedElement = SelectedElements.LastOrDefault();
                return;
            }

            SelectedElements.Add(element);
            element.IsSelected = true;
            SelectedElement = element;
            return;
        }

        ReplaceSelection(new[] { element });
    }

    public void SelectMany(IEnumerable<DesignElement> elements)
        => ReplaceSelection(elements);

    public void ClearSelection()
    {
        foreach (var element in SelectedElements)
        {
            element.IsSelected = false;
        }

        SelectedElements.Clear();
        SelectedElement = null;
    }

    public bool RemoveElement(DesignElement element)
    {
        var removed = Elements.Remove(element);
        if (!removed)
        {
            return false;
        }

        if (SelectedElements.Remove(element))
        {
            element.IsSelected = false;
            SelectedElement = SelectedElements.LastOrDefault();
        }

        return true;
    }

    public bool MoveElementsToFront(IEnumerable<DesignElement> elements)
    {
        var moving = Elements.Where(elements.Contains).ToList();
        if (moving.Count == 0 || Elements.Skip(Elements.Count - moving.Count).SequenceEqual(moving))
        {
            return false;
        }

        foreach (var element in moving)
        {
            Elements.Remove(element);
        }

        foreach (var element in moving)
        {
            Elements.Add(element);
        }

        return true;
    }

    public bool MoveElementsToBack(IEnumerable<DesignElement> elements)
    {
        var moving = Elements.Where(elements.Contains).ToList();
        if (moving.Count == 0 || Elements.Take(moving.Count).SequenceEqual(moving))
        {
            return false;
        }

        foreach (var element in moving)
        {
            Elements.Remove(element);
        }

        for (var index = moving.Count - 1; index >= 0; index--)
        {
            Elements.Insert(0, moving[index]);
        }

        return true;
    }

    public bool MoveElementsForward(IEnumerable<DesignElement> elements)
    {
        var moving = new HashSet<DesignElement>(elements);
        var changed = false;

        // Walk from front to back so a selected block keeps its internal order.
        for (var index = Elements.Count - 2; index >= 0; index--)
        {
            if (!moving.Contains(Elements[index]) || moving.Contains(Elements[index + 1]))
            {
                continue;
            }

            var next = Elements[index + 1];
            Elements[index + 1] = Elements[index];
            Elements[index] = next;
            changed = true;
        }

        return changed;
    }

    public bool MoveElementsBackward(IEnumerable<DesignElement> elements)
    {
        var moving = new HashSet<DesignElement>(elements);
        var changed = false;

        // Walk from back to front so a selected block keeps its internal order.
        for (var index = 1; index < Elements.Count; index++)
        {
            if (!moving.Contains(Elements[index]) || moving.Contains(Elements[index - 1]))
            {
                continue;
            }

            var previous = Elements[index - 1];
            Elements[index - 1] = Elements[index];
            Elements[index] = previous;
            changed = true;
        }

        return changed;
    }

    private void ReplaceSelection(IEnumerable<DesignElement> elements)
    {
        var next = elements
            .Where(Elements.Contains)
            .Distinct()
            .ToList();

        foreach (var element in SelectedElements)
        {
            element.IsSelected = false;
        }

        SelectedElements.Clear();
        foreach (var element in next)
        {
            element.IsSelected = true;
            SelectedElements.Add(element);
        }

        SelectedElement = next.LastOrDefault();
    }

    private void OnElementsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => HasElements = Elements.Count > 0;

    private void OnSelectedElementsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsSelectionActive));
        OnPropertyChanged(nameof(HasMultipleSelection));
    }

    private DesignElement AddElement(
        string displayPrefix,
        string typeName,
        Control visual,
        double x,
        double y,
        double width,
        double height,
        bool select,
        bool preserveDisplayName = false)
    {
        var element = new DesignElement(
            displayName: preserveDisplayName ? displayPrefix : BuildUniqueDisplayName(displayPrefix),
            typeName: typeName,
            visual: visual,
            x: x,
            y: y,
            width: width,
            height: height);

        Elements.Add(element);
        if (select)
        {
            Select(element);
        }

        return element;
    }

    private (Control Visual, double Width, double Height) CreateVisualByType(string typeName, string displayName)
    {
        if (_componentCatalog.TryGet(typeName, out var definition))
        {
            return (_renderer.CreateControl(definition), definition.DefaultWidth, definition.DefaultHeight);
        }

        return (
            new TextBlock { Text = $"[Unsupported: {displayName}]" },
            160,
            24);
    }

    private static void ApplyVisualProperties(Control visual, IReadOnlyDictionary<string, string>? properties)
    {
        if (properties is null)
        {
            return;
        }

        if (properties.TryGetValue("Opacity", out var opacity)
            && double.TryParse(opacity, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedOpacity))
        {
            visual.Opacity = Math.Clamp(parsedOpacity, 0, 1);
        }

        if (properties.TryGetValue("__toolTip", out var toolTip))
        {
            ToolTip.SetTip(visual, string.IsNullOrWhiteSpace(toolTip) ? null : toolTip);
        }

        if (visual is Button button)
        {
            if (properties.TryGetValue("Content", out var content))
            {
                button.Content = content;
            }

            return;
        }

        if (visual is TextBox textBox)
        {
            if (properties.TryGetValue("PasswordChar", out var passwordChar))
            {
                textBox.PasswordChar = string.IsNullOrEmpty(passwordChar) ? '\0' : passwordChar[0];
            }

            if (properties.TryGetValue("RevealPassword", out var revealPassword)
                && bool.TryParse(revealPassword, out var parsedRevealPassword))
            {
                textBox.RevealPassword = parsedRevealPassword;
            }

            if (textBox.PasswordChar == '\0' && properties.TryGetValue("Text", out var text))
            {
                textBox.Text = text;
            }

            if (properties.TryGetValue("Watermark", out var watermark))
            {
                textBox.Watermark = watermark;
            }

            return;
        }

        if (visual is TextBlock textBlock)
        {
            if (properties.TryGetValue("Text", out var text))
            {
                textBlock.Text = text;
            }

            if (properties.TryGetValue("FontSize", out var fontSize)
                && double.TryParse(fontSize, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedFontSize))
            {
                textBlock.FontSize = Math.Clamp(parsedFontSize, 8, 96);
            }

            if (properties.TryGetValue("FontWeight", out var fontWeight)
                && TryParseTextWeight(fontWeight, out var parsedFontWeight))
            {
                textBlock.FontWeight = parsedFontWeight;
            }

            if (properties.TryGetValue("Foreground", out var foreground))
            {
                TrySetTextForeground(textBlock, foreground);
            }

            return;
        }

        if (visual is Image image)
        {
            if (properties.TryGetValue("Source", out var source))
            {
                TryLoadImageSource(image, source, retainSourceOnFailure: true);
            }

            if (properties.TryGetValue("Stretch", out var stretch)
                && Enum.TryParse<Stretch>(stretch, ignoreCase: true, out var parsedStretch))
            {
                image.Stretch = parsedStretch;
            }

            return;
        }

        if (visual is CheckBox checkBox)
        {
            if (properties.TryGetValue("Content", out var content))
            {
                checkBox.Content = content;
            }

            if (properties.TryGetValue("IsChecked", out var isChecked)
                && bool.TryParse(isChecked, out var parsedIsChecked))
            {
                checkBox.IsChecked = parsedIsChecked;
            }

            return;
        }

        if (visual is RadioButton radioButton)
        {
            if (properties.TryGetValue("Content", out var content))
            {
                radioButton.Content = content;
            }

            if (properties.TryGetValue("IsChecked", out var isChecked)
                && bool.TryParse(isChecked, out var parsedIsChecked))
            {
                radioButton.IsChecked = parsedIsChecked;
            }

            if (properties.TryGetValue("GroupName", out var groupName))
            {
                radioButton.GroupName = groupName;
            }

            return;
        }

        if (visual is ToggleSwitch toggleSwitch)
        {
            if (properties.TryGetValue("Content", out var content))
            {
                toggleSwitch.Content = content;
            }

            if (properties.TryGetValue("IsChecked", out var isChecked)
                && bool.TryParse(isChecked, out var parsedIsChecked))
            {
                toggleSwitch.IsChecked = parsedIsChecked;
            }

            return;
        }

        if (visual is Avalonia.Controls.Primitives.ToggleButton toggleButton)
        {
            if (properties.TryGetValue("Content", out var content))
            {
                toggleButton.Content = content;
            }

            if (properties.TryGetValue("IsChecked", out var isChecked)
                && bool.TryParse(isChecked, out var parsedIsChecked))
            {
                toggleButton.IsChecked = parsedIsChecked;
            }

            return;
        }

        if (visual is ComboBox comboBox)
        {
            if (properties.TryGetValue("__items", out var itemsJson))
            {
                RestoreComboBoxItems(comboBox, itemsJson);
            }

            if (properties.TryGetValue("SelectedIndex", out var selectedIndex)
                && int.TryParse(selectedIndex, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSelectedIndex))
            {
                comboBox.SelectedIndex = Math.Clamp(parsedSelectedIndex, -1, comboBox.Items.Count - 1);
            }

            return;
        }

        if (visual is ListBox listBox)
        {
            if (properties.TryGetValue("__items", out var itemsJson))
            {
                RestoreListBoxItems(listBox, itemsJson);
            }

            if (properties.TryGetValue("SelectedIndex", out var selectedIndex)
                && int.TryParse(selectedIndex, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSelectedIndex))
            {
                listBox.SelectedIndex = Math.Clamp(parsedSelectedIndex, -1, listBox.Items.Count - 1);
            }

            return;
        }

        if (visual is Slider slider)
        {
            if (properties.TryGetValue("Minimum", out var minimum)
                && double.TryParse(minimum, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedMinimum))
            {
                slider.Minimum = parsedMinimum;
            }

            if (properties.TryGetValue("Maximum", out var maximum)
                && double.TryParse(maximum, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedMaximum))
            {
                slider.Maximum = parsedMaximum;
            }

            if (properties.TryGetValue("Value", out var value)
                && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
            {
                slider.Value = parsedValue;
            }

            return;
        }

        if (visual is ProgressBar progressBar)
        {
            if (properties.TryGetValue("Minimum", out var minimum)
                && double.TryParse(minimum, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedMinimum))
            {
                progressBar.Minimum = parsedMinimum;
            }

            if (properties.TryGetValue("Maximum", out var maximum)
                && double.TryParse(maximum, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedMaximum))
            {
                progressBar.Maximum = parsedMaximum;
            }

            if (properties.TryGetValue("Value", out var value)
                && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
            {
                progressBar.Value = parsedValue;
            }

            return;
        }

        if (visual is DatePicker datePicker)
        {
            if (properties.TryGetValue("SelectedDate", out var selectedDate)
                && DateTimeOffset.TryParseExact(
                    selectedDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var parsedSelectedDate))
            {
                datePicker.SelectedDate = parsedSelectedDate;
            }

            return;
        }

        if (visual is CalendarDatePicker calendarDatePicker)
        {
            if (properties.TryGetValue("SelectedDate", out var selectedDate)
                && DateTime.TryParseExact(
                    selectedDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedSelectedDate))
            {
                calendarDatePicker.SelectedDate = parsedSelectedDate;
            }

            if (properties.TryGetValue("Watermark", out var watermark))
            {
                calendarDatePicker.Watermark = watermark;
            }

            return;
        }

        if (visual is TimePicker timePicker)
        {
            if (properties.TryGetValue("SelectedTime", out var selectedTime)
                && TimeSpan.TryParseExact(
                    selectedTime,
                    "hh\\:mm",
                    CultureInfo.InvariantCulture,
                    out var parsedSelectedTime))
            {
                timePicker.SelectedTime = parsedSelectedTime;
            }

            return;
        }

        if (visual is NumericUpDown numericUpDown)
        {
            if (properties.TryGetValue("Minimum", out var minimum)
                && decimal.TryParse(minimum, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedMinimum))
            {
                numericUpDown.Minimum = parsedMinimum;
            }

            if (properties.TryGetValue("Maximum", out var maximum)
                && decimal.TryParse(maximum, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedMaximum))
            {
                numericUpDown.Maximum = parsedMaximum;
            }

            if (properties.TryGetValue("Increment", out var increment)
                && decimal.TryParse(increment, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedIncrement))
            {
                numericUpDown.Increment = parsedIncrement;
            }

            if (properties.TryGetValue("Value", out var value)
                && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedValue))
            {
                numericUpDown.Value = parsedValue;
            }

            return;
        }

        if (visual is TabControl tabControl)
        {
            if (properties.TryGetValue("__tabs", out var tabsJson))
            {
                RestoreTabControlTabs(tabControl, tabsJson);
            }

            if (properties.TryGetValue("SelectedIndex", out var selectedIndex)
                && int.TryParse(selectedIndex, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSelectedIndex))
            {
                tabControl.SelectedIndex = Math.Clamp(parsedSelectedIndex, -1, tabControl.Items.Count - 1);
            }

            return;
        }

        if (visual is Expander expander)
        {
            if (properties.TryGetValue("Header", out var header))
            {
                expander.Header = header;
            }

            if (properties.TryGetValue("IsExpanded", out var isExpanded)
                && bool.TryParse(isExpanded, out var parsedIsExpanded))
            {
                expander.IsExpanded = parsedIsExpanded;
            }

            if (properties.TryGetValue("__contentText", out var contentText))
            {
                SetExpanderContent(expander, contentText);
            }

            return;
        }

        if (visual is Border border)
        {
            if (properties.TryGetValue("Background", out var background))
            {
                TrySetBorderBackground(border, background);
            }

            if (properties.TryGetValue("BorderBrush", out var borderBrush))
            {
                TrySetBorderBrush(border, borderBrush);
            }

            if (properties.TryGetValue("BorderThickness", out var borderThickness)
                && TryParseThickness(borderThickness, out var parsedBorderThickness))
            {
                border.BorderThickness = parsedBorderThickness;
            }

            if (properties.TryGetValue("CornerRadius", out var cornerRadius)
                && TryParseCornerRadius(cornerRadius, out var parsedCornerRadius))
            {
                border.CornerRadius = parsedCornerRadius;
            }

            return;
        }

        if (visual is StackPanel stackPanel)
        {
            if (properties.TryGetValue("Orientation", out var orientation)
                && Enum.TryParse<Orientation>(orientation, ignoreCase: true, out var parsedOrientation))
            {
                stackPanel.Orientation = parsedOrientation;
            }

            if (properties.TryGetValue("Spacing", out var spacing)
                && double.TryParse(spacing, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedSpacing))
            {
                stackPanel.Spacing = parsedSpacing;
            }

            if (properties.TryGetValue("__children", out var childrenJson))
            {
                RestoreStackPanelChildren(stackPanel, childrenJson);
            }

            return;
        }

        if (visual is Grid grid
            && properties.TryGetValue("ShowGridLines", out var showGrid)
            && bool.TryParse(showGrid, out var parsedShowGrid))
        {
            grid.ShowGridLines = parsedShowGrid;
        }
    }

    private static void TrySetTextForeground(TextBlock textBlock, string foreground)
    {
        try
        {
            textBlock.Foreground = Brush.Parse(foreground);
        }
        catch (FormatException)
        {
            // Ignore malformed imported colors while keeping the control usable.
        }
    }

    private static bool TryLoadImageSource(Image image, string source, bool retainSourceOnFailure)
    {
        var path = ResolveImagePath(source);
        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
        {
            if (retainSourceOnFailure)
            {
                DisposeImageSource(image);
                image.Source = null;
                image.Tag = source;
            }

            return false;
        }

        try
        {
            var bitmap = new Bitmap(path);
            DisposeImageSource(image);
            image.Source = bitmap;
            image.Tag = source;
            return true;
        }
        catch
        {
            if (retainSourceOnFailure)
            {
                DisposeImageSource(image);
                image.Source = null;
                image.Tag = source;
            }

            return false;
        }
    }

    private static string? ResolveImagePath(string source)
    {
        if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
        {
            return uri.IsFile ? uri.LocalPath : null;
        }

        return System.IO.Path.GetFullPath(source);
    }

    private static void DisposeImageSource(Image image)
    {
        if (image.Source is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private static void RestoreComboBoxItems(ComboBox comboBox, string json)
    {
        List<string>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return;
        }

        if (items is null)
        {
            return;
        }

        comboBox.Items.Clear();
        foreach (var item in items)
        {
            comboBox.Items.Add(item);
        }
    }

    private static void RestoreListBoxItems(ListBox listBox, string json)
    {
        List<string>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return;
        }

        if (items is null)
        {
            return;
        }

        listBox.Items.Clear();
        foreach (var item in items)
        {
            listBox.Items.Add(item);
        }
    }

    private static void RestoreTabControlTabs(TabControl tabControl, string json)
    {
        List<string>? tabs;
        try
        {
            tabs = JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return;
        }

        if (tabs is null)
        {
            return;
        }

        tabControl.Items.Clear();
        foreach (var tab in tabs)
        {
            tabControl.Items.Add(CreateTabItem(tab));
        }
    }

    private static TabItem CreateTabItem(string header)
        => new()
        {
            Header = header,
            Content = new TextBlock { Text = $"{header} content", Margin = new Thickness(12) },
        };

    private static void SetExpanderContent(Expander expander, string contentText)
        => expander.Content = new TextBlock { Text = contentText, Margin = new Thickness(8) };

    private static void TrySetBorderBackground(Border border, string value)
    {
        try
        {
            border.Background = Brush.Parse(value);
        }
        catch (FormatException)
        {
            // Ignore malformed imported brushes while keeping the control usable.
        }
    }

    private static void TrySetBorderBrush(Border border, string value)
    {
        try
        {
            border.BorderBrush = Brush.Parse(value);
        }
        catch (FormatException)
        {
            // Ignore malformed imported brushes while keeping the control usable.
        }
    }

    private static bool TryParseThickness(string value, out Thickness thickness)
    {
        try
        {
            thickness = Thickness.Parse(value);
            return true;
        }
        catch (FormatException)
        {
            thickness = default;
            return false;
        }
    }

    private static bool TryParseCornerRadius(string value, out CornerRadius cornerRadius)
    {
        try
        {
            cornerRadius = CornerRadius.Parse(value);
            return true;
        }
        catch (FormatException)
        {
            cornerRadius = default;
            return false;
        }
    }

    private static bool TryParseTextWeight(string value, out FontWeight fontWeight)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "normal":
            case "regular":
            case "400":
                fontWeight = FontWeight.Normal;
                return true;
            case "semibold":
            case "semi-bold":
            case "600":
                fontWeight = FontWeight.SemiBold;
                return true;
            case "bold":
            case "700":
                fontWeight = FontWeight.Bold;
                return true;
            default:
                fontWeight = FontWeight.Normal;
                return false;
        }
    }

    private static void RestoreStackPanelChildren(StackPanel stackPanel, string json)
    {
        stackPanel.Children.Clear();

        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        List<StackPanelChildSnapshot>? children;
        try
        {
            children = JsonSerializer.Deserialize<List<StackPanelChildSnapshot>>(json);
        }
        catch
        {
            return;
        }

        if (children is null)
        {
            return;
        }

        foreach (var child in children)
        {
            var control = child.TypeName switch
            {
                "TextBlock" => new TextBlock
                {
                    Text = child.Text ?? string.Empty,
                },
                "Button" => new Button
                {
                    Content = child.Content ?? string.Empty,
                },
                "TextBox" => new TextBox
                {
                    Text = string.IsNullOrEmpty(child.PasswordChar) ? child.Text ?? string.Empty : string.Empty,
                    Watermark = child.Watermark,
                    PasswordChar = string.IsNullOrEmpty(child.PasswordChar) ? '\0' : child.PasswordChar[0],
                    RevealPassword = child.RevealPassword ?? false,
                },
                _ => null,
            };

            if (control is not null)
            {
                stackPanel.Children.Add(control);
            }
        }
    }

    private string BuildUniqueDisplayName(string displayPrefix)
    {
        var index = 1;
        var candidate = $"{displayPrefix}{index}";
        while (Elements.Any(element => string.Equals(element.DisplayName, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            index++;
            candidate = $"{displayPrefix}{index}";
        }

        return candidate;
    }

    private sealed record StackPanelChildSnapshot(
        string TypeName,
        string? Text = null,
        string? Content = null,
        string? Watermark = null,
        string? PasswordChar = null,
        bool? RevealPassword = null);
}
