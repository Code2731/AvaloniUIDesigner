using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Designer.Services;
using AvaloniaUIDesigner.App.Models;

namespace AvaloniaUIDesigner.App.Views;

public sealed class PreviewWindow : Window
{
    public PreviewWindow(DesignerCanvasDocument document)
    {
        Title = "Avalonia UI Designer - Preview";
        Width = 960;
        Height = 640;
        MinWidth = 480;
        MinHeight = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var settings = document.Settings ?? new DesignerCanvasSettings();
        Width = Math.Clamp(settings.Width + 32, MinWidth, 1280);
        Height = Math.Clamp(settings.Height + 72, MinHeight, 960);
        var canvas = CreatePreviewCanvas(document);

        Content = new Border
        {
            Background = Brush.Parse(settings.Background),
            Padding = new Thickness(16),
            Child = new ScrollViewer
            {
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Content = canvas,
            },
        };
    }

    internal static Canvas CreatePreviewCanvas(DesignerCanvasDocument document)
    {
        var settings = document.Settings ?? new DesignerCanvasSettings();
        var colorResources = document.ColorResources ?? new Dictionary<string, string>(StringComparer.Ordinal);
        var styles = document.Styles ?? Array.Empty<DesignerStyleDefinition>();
        var canvas = new Canvas
        {
            Background = Brush.Parse(settings.Background),
            Width = Math.Max(settings.Width, document.Elements.Count == 0 ? 0 : document.Elements.Max(element => element.X + element.Width + 32)),
            Height = Math.Max(settings.Height, document.Elements.Count == 0 ? 0 : document.Elements.Max(element => element.Y + element.Height + 32)),
        };
        foreach (var pair in colorResources)
        {
            canvas.Resources[pair.Key] = Brush.Parse(pair.Value);
        }

        var controlsByName = new Dictionary<string, Control>(StringComparer.OrdinalIgnoreCase);
        var containersByName = document.Elements
            .Where(element => string.Equals(element.TypeName, "Avalonia.Controls.Grid", StringComparison.Ordinal)
                || string.Equals(element.TypeName, "Avalonia.Controls.StackPanel", StringComparison.Ordinal)
                || string.Equals(element.TypeName, "Avalonia.Controls.DockPanel", StringComparison.Ordinal)
                || string.Equals(element.TypeName, "Avalonia.Controls.WrapPanel", StringComparison.Ordinal)
                || string.Equals(
                    element.TypeName,
                    "Avalonia.Controls.Primitives.UniformGrid",
                    StringComparison.Ordinal)
                || string.Equals(element.TypeName, "Avalonia.Controls.Canvas", StringComparison.Ordinal)
                || string.Equals(element.TypeName, "Avalonia.Controls.Border", StringComparison.Ordinal)
                || string.Equals(element.TypeName, "Avalonia.Controls.ScrollViewer", StringComparison.Ordinal)
                || string.Equals(element.TypeName, "Avalonia.Controls.Expander", StringComparison.Ordinal))
            .ToDictionary(element => element.DisplayName, StringComparer.OrdinalIgnoreCase);
        var childrenByParent = document.Elements
            .Where(element => element.ParentName is not null && containersByName.ContainsKey(element.ParentName))
            .GroupBy(element => element.ParentName!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        foreach (var element in document.Elements.Where(element =>
                     element.ParentName is null || !containersByName.ContainsKey(element.ParentName)))
        {
            var control = CreateControl(element, colorResources, styles);
            control.Width = element.Width;
            control.Height = element.Height;
            Canvas.SetLeft(control, element.X);
            Canvas.SetTop(control, element.Y);
            canvas.Children.Add(control);
            controlsByName[element.DisplayName] = control;
            AddContainerChildren(control, element);
        }

        foreach (var label in controlsByName.Values.OfType<Label>())
        {
            if (label.Tag is string targetName && controlsByName.TryGetValue(targetName, out var target))
            {
                label.Target = target;
            }
        }

        return canvas;

        void AddContainerChildren(Control parent, DesignerElementSnapshot parentSnapshot)
        {
            if (!childrenByParent.TryGetValue(parentSnapshot.DisplayName, out var children))
            {
                return;
            }

            var orderedChildren = parent is StackPanel
                ? children.OrderBy(child => child.StackPanelIndex).ToList()
                : parent is DockPanel
                    ? children.OrderBy(child => child.DockPanelIndex).ToList()
                    : parent is WrapPanel
                        ? children.OrderBy(child => child.WrapPanelIndex).ToList()
                        : parent is UniformGrid
                            ? children.OrderBy(child => child.UniformGridIndex).ToList()
                            : parent is Canvas
                                ? children.OrderBy(child => child.CanvasChildIndex).ToList()
                : children;
            if (parent is Panel panel)
            {
                panel.Children.Clear();
            }

            for (var index = 0; index < orderedChildren.Count; index++)
            {
                var childSnapshot = orderedChildren[index];
                var child = CreateControl(childSnapshot, colorResources, styles);
                switch (parent)
                {
                    case Grid grid:
                        Grid.SetRow(child, Math.Max(0, childSnapshot.GridRow));
                        Grid.SetColumn(child, Math.Max(0, childSnapshot.GridColumn));
                        Grid.SetRowSpan(child, Math.Max(1, childSnapshot.GridRowSpan));
                        Grid.SetColumnSpan(child, Math.Max(1, childSnapshot.GridColumnSpan));
                        grid.Children.Add(child);
                        break;
                    case StackPanel stack:
                        if (stack.Orientation == Orientation.Vertical)
                        {
                            child.Height = Math.Max(10, childSnapshot.StackPanelItemSize);
                        }
                        else
                        {
                            child.Width = Math.Max(10, childSnapshot.StackPanelItemSize);
                        }

                        stack.Children.Add(child);
                        break;
                    case DockPanel dock:
                        DockPanel.SetDock(child, childSnapshot.DockPanelDock switch
                        {
                            DesignerDockSide.Top => Dock.Top,
                            DesignerDockSide.Right => Dock.Right,
                            DesignerDockSide.Bottom => Dock.Bottom,
                            _ => Dock.Left,
                        });
                        if (!dock.LastChildFill || index != orderedChildren.Count - 1)
                        {
                            if (childSnapshot.DockPanelDock is DesignerDockSide.Top or DesignerDockSide.Bottom)
                            {
                                child.Height = Math.Max(10, childSnapshot.DockPanelItemSize);
                            }
                            else
                            {
                                child.Width = Math.Max(10, childSnapshot.DockPanelItemSize);
                            }
                        }

                        dock.Children.Add(child);
                        break;
                    case WrapPanel wrap:
                        wrap.Children.Add(child);
                        break;
                    case UniformGrid uniformGrid:
                        uniformGrid.Children.Add(child);
                        break;
                    case Canvas nestedCanvas:
                        child.Width = Math.Max(10, childSnapshot.Width);
                        child.Height = Math.Max(10, childSnapshot.Height);
                        Canvas.SetLeft(child, childSnapshot.CanvasChildLeft);
                        Canvas.SetTop(child, childSnapshot.CanvasChildTop);
                        nestedCanvas.Children.Add(child);
                        break;
                    case Border border:
                        border.Child = child;
                        break;
                    case ScrollViewer scrollViewer:
                        scrollViewer.Content = child;
                        break;
                    case Expander expander:
                        expander.Content = child;
                        break;
                    default:
                        continue;
                }

                controlsByName[childSnapshot.DisplayName] = child;
                AddContainerChildren(child, childSnapshot);
            }
        }
    }

    private static Control CreateControl(
        DesignerElementSnapshot snapshot,
        IReadOnlyDictionary<string, string> colorResources,
        IReadOnlyList<DesignerStyleDefinition> styles)
    {
        Control control = snapshot.TypeName switch
        {
            "Avalonia.Controls.Button" => new Button { Content = "Button" },
            "Avalonia.Controls.TextBox" => new TextBox { Watermark = "Type here" },
            "Avalonia.Controls.TextBlock" => new TextBlock { Text = "Text" },
            "Avalonia.Controls.Label" => new Label { Content = "Label" },
            "Avalonia.Controls.Image" => new Image { Stretch = Stretch.Uniform },
            "Avalonia.Controls.CheckBox" => new CheckBox { Content = "CheckBox" },
            "Avalonia.Controls.RadioButton" => new RadioButton { Content = "Option", GroupName = "Options" },
            "Avalonia.Controls.ToggleSwitch" => new ToggleSwitch { Content = "Toggle" },
            "Avalonia.Controls.Primitives.ToggleButton" => new Avalonia.Controls.Primitives.ToggleButton { Content = "Toggle" },
            "Avalonia.Controls.ComboBox" => new ComboBox
            {
                SelectedIndex = 0,
                Items = { "Option 1", "Option 2", "Option 3" },
            },
            "Avalonia.Controls.ListBox" => new ListBox
            {
                SelectedIndex = 0,
                Items = { "Item 1", "Item 2", "Item 3" },
            },
            "Avalonia.Controls.Slider" => new Slider { Minimum = 0, Maximum = 100, Value = 50 },
            "Avalonia.Controls.ProgressBar" => new ProgressBar { Minimum = 0, Maximum = 100, Value = 50 },
            "Avalonia.Controls.DatePicker" => new DatePicker(),
            "Avalonia.Controls.CalendarDatePicker" => new CalendarDatePicker { Watermark = "Select date" },
            "Avalonia.Controls.TimePicker" => new TimePicker(),
            "Avalonia.Controls.NumericUpDown" => new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                Increment = 1,
                Value = 50,
            },
            "Avalonia.Controls.TabControl" => CreateDefaultTabControl(),
            "Avalonia.Controls.Expander" => new Expander
            {
                Header = "Advanced options",
                IsExpanded = true,
                Content = new TextBlock { Text = "Expanded content", Margin = new Thickness(8) },
            },
            "Avalonia.Controls.ScrollViewer" => new ScrollViewer
            {
                Content = new TextBlock
                {
                    Text = "Scrollable content\n\nUse Edit Content... to add more text.",
                    Margin = new Thickness(8),
                },
            },
            "Avalonia.Controls.Border" => new Border
            {
                Background = Brush.Parse("#F1F5F9"),
                BorderBrush = Brush.Parse("#94A3B8"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Child = new TextBlock
                {
                    Text = "Border content",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            },
            "Avalonia.Controls.Grid" => new Grid
            {
                RowDefinitions = new RowDefinitions("*,*"),
                ColumnDefinitions = new ColumnDefinitions("*,*"),
                ShowGridLines = true,
            },
            "Avalonia.Controls.StackPanel" => new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 6,
            },
            "Avalonia.Controls.DockPanel" => new DockPanel
            {
                LastChildFill = true,
            },
            "Avalonia.Controls.WrapPanel" => new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                ItemWidth = 96,
                ItemHeight = 36,
                ItemSpacing = 8,
                LineSpacing = 8,
                ItemsAlignment = WrapPanelItemsAlignment.Start,
            },
            "Avalonia.Controls.Primitives.UniformGrid" => new UniformGrid
            {
                Rows = 2,
                Columns = 3,
                FirstColumn = 0,
                RowSpacing = 8,
                ColumnSpacing = 8,
            },
            "Avalonia.Controls.Canvas" => new Canvas
            {
                Background = Brush.Parse("#F8FAFC"),
            },
            _ => new TextBlock { Text = $"[Unsupported: {snapshot.DisplayName}]" },
        };

        ApplyProperties(control, snapshot.VisualProperties, colorResources);
        DesignerStyleRuntime.ApplyStyles(control, styles, colorResources);
        WireInteractiveStyleStates(control, styles, colorResources);
        return control;
    }

    private static void WireInteractiveStyleStates(
        Control control,
        IReadOnlyList<DesignerStyleDefinition> styles,
        IReadOnlyDictionary<string, string> colorResources)
    {
        if (!styles.Any(style =>
                style.PseudoClass is not null
                && string.Equals(style.TargetType, control.GetType().Name, StringComparison.Ordinal)
                && control.Classes.Contains(style.ClassName)))
        {
            return;
        }

        control.PropertyChanged += (_, args) =>
        {
            if (args.Property.Name is "IsEnabled" or "IsPointerOver" or "IsFocused"
                or "IsPressed" or "IsChecked" or "IsExpanded")
            {
                DesignerStyleRuntime.ApplyStyles(control, styles, colorResources);
            }
        };
    }

    private static void ApplyProperties(
        Control control,
        IReadOnlyDictionary<string, string>? properties,
        IReadOnlyDictionary<string, string> colorResources)
    {
        if (properties is null)
        {
            return;
        }

        if (properties.TryGetValue("Opacity", out var opacity)
            && double.TryParse(opacity, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedOpacity))
        {
            control.Opacity = Math.Clamp(parsedOpacity, 0, 1);
        }

        if (properties.TryGetValue("Classes", out var classes))
        {
            foreach (var className in classes.Split(
                         [' ', '\t', '\r', '\n'],
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Distinct(StringComparer.Ordinal))
            {
                control.Classes.Add(className);
            }
        }

        ApplyTemplatedAppearanceProperties(control, properties, colorResources);

        if (properties.TryGetValue("__toolTip", out var toolTip))
        {
            ToolTip.SetTip(control, string.IsNullOrWhiteSpace(toolTip) ? null : toolTip);
        }

        if (properties.TryGetValue("__automationName", out var automationName))
        {
            AutomationProperties.SetName(control, automationName);
        }

        if (properties.TryGetValue("__isEnabled", out var isEnabled)
            && bool.TryParse(isEnabled, out var parsedIsEnabled))
        {
            control.IsEnabled = parsedIsEnabled;
        }

        if (properties.TryGetValue("__isVisible", out var isVisible)
            && bool.TryParse(isVisible, out var parsedIsVisible))
        {
            control.IsVisible = parsedIsVisible;
        }

        if (properties.TryGetValue("__tabIndex", out var tabIndex)
            && int.TryParse(tabIndex, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedTabIndex))
        {
            control.TabIndex = parsedTabIndex;
        }

        if (properties.TryGetValue("__isTabStop", out var isTabStop)
            && bool.TryParse(isTabStop, out var parsedIsTabStop))
        {
            control.IsTabStop = parsedIsTabStop;
        }

        switch (control)
        {
            case Button button when properties.TryGetValue("Content", out var content):
                button.Content = content;
                break;
            case TextBox textBox:
                if (properties.TryGetValue("PasswordChar", out var passwordChar))
                {
                    textBox.PasswordChar = string.IsNullOrEmpty(passwordChar) ? '\0' : passwordChar[0];
                }

                if (properties.TryGetValue("RevealPassword", out var revealPassword)
                    && bool.TryParse(revealPassword, out var parsedRevealPassword))
                {
                    textBox.RevealPassword = parsedRevealPassword;
                }

                if (properties.TryGetValue("AcceptsReturn", out var acceptsReturn)
                    && bool.TryParse(acceptsReturn, out var parsedAcceptsReturn))
                {
                    textBox.AcceptsReturn = parsedAcceptsReturn;
                }

                if (properties.TryGetValue("TextWrapping", out var textWrapping)
                    && Enum.TryParse<TextWrapping>(textWrapping, ignoreCase: true, out var parsedTextWrapping))
                {
                    textBox.TextWrapping = parsedTextWrapping;
                }

                if (textBox.PasswordChar == '\0' && properties.TryGetValue("Text", out var text))
                {
                    textBox.Text = text;
                }

                if (properties.TryGetValue("Watermark", out var watermark))
                {
                    textBox.Watermark = watermark;
                }
                break;
            case TextBlock textBlock:
                if (properties.TryGetValue("Text", out var textBlockText))
                {
                    textBlock.Text = textBlockText;
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
                    TrySetTextForeground(textBlock, foreground, colorResources);
                }

                if (properties.TryGetValue("Background", out var background))
                {
                    TrySetBorderBrush(value => textBlock.Background = value, background, colorResources);
                }

                break;
            case Label label:
                if (properties.TryGetValue("Content", out var labelContent))
                {
                    label.Content = labelContent;
                }

                label.Tag = properties.TryGetValue("Target", out var targetName)
                    && !string.IsNullOrWhiteSpace(targetName)
                    ? targetName
                    : null;
                break;
            case Image image:
                ApplyImageProperties(image, properties);
                break;
            case CheckBox checkBox:
                if (properties.TryGetValue("Content", out var checkBoxContent))
                {
                    checkBox.Content = checkBoxContent;
                }

                if (properties.TryGetValue("IsChecked", out var isChecked)
                    && bool.TryParse(isChecked, out var parsedIsChecked))
                {
                    checkBox.IsChecked = parsedIsChecked;
                }
                break;
            case RadioButton radioButton:
                if (properties.TryGetValue("Content", out var radioButtonContent))
                {
                    radioButton.Content = radioButtonContent;
                }

                if (properties.TryGetValue("IsChecked", out var radioButtonIsChecked)
                    && bool.TryParse(radioButtonIsChecked, out var parsedRadioButtonIsChecked))
                {
                    radioButton.IsChecked = parsedRadioButtonIsChecked;
                }

                if (properties.TryGetValue("GroupName", out var radioButtonGroupName))
                {
                    radioButton.GroupName = radioButtonGroupName;
                }
                break;
            case ToggleSwitch toggleSwitch:
                if (properties.TryGetValue("Content", out var toggleSwitchContent))
                {
                    toggleSwitch.Content = toggleSwitchContent;
                }

                if (properties.TryGetValue("IsChecked", out var toggleSwitchIsChecked)
                    && bool.TryParse(toggleSwitchIsChecked, out var parsedToggleSwitchIsChecked))
                {
                    toggleSwitch.IsChecked = parsedToggleSwitchIsChecked;
                }
                break;
            case Avalonia.Controls.Primitives.ToggleButton toggleButton:
                if (properties.TryGetValue("Content", out var toggleButtonContent))
                {
                    toggleButton.Content = toggleButtonContent;
                }

                if (properties.TryGetValue("IsChecked", out var toggleButtonIsChecked)
                    && bool.TryParse(toggleButtonIsChecked, out var parsedToggleButtonIsChecked))
                {
                    toggleButton.IsChecked = parsedToggleButtonIsChecked;
                }
                break;
            case ComboBox comboBox:
                ApplyComboBoxProperties(comboBox, properties);
                break;
            case ListBox listBox:
                ApplyListBoxProperties(listBox, properties);
                break;
            case Slider slider:
                ApplySliderProperties(slider, properties);
                break;
            case ProgressBar progressBar:
                ApplyProgressBarProperties(progressBar, properties);
                break;
            case DatePicker datePicker:
                ApplyDatePickerProperties(datePicker, properties);
                break;
            case CalendarDatePicker calendarDatePicker:
                ApplyCalendarDatePickerProperties(calendarDatePicker, properties);
                break;
            case TimePicker timePicker:
                ApplyTimePickerProperties(timePicker, properties);
                break;
            case NumericUpDown numericUpDown:
                ApplyNumericUpDownProperties(numericUpDown, properties);
                break;
            case TabControl tabControl:
                ApplyTabControlProperties(tabControl, properties);
                break;
            case Expander expander:
                ApplyExpanderProperties(expander, properties);
                break;
            case ScrollViewer scrollViewer:
                ApplyScrollViewerProperties(scrollViewer, properties);
                break;
            case Border border:
                ApplyBorderProperties(border, properties, colorResources);
                break;
            case Grid grid:
                DesignerGridDefinitionRuntime.TryApply(grid, properties, out _);
                if (properties.TryGetValue("ShowGridLines", out var showGrid)
                    && bool.TryParse(showGrid, out var parsedShowGrid))
                {
                    grid.ShowGridLines = parsedShowGrid;
                }
                break;
            case StackPanel stackPanel:
                ApplyStackPanelProperties(stackPanel, properties);
                break;
            case DockPanel dockPanel:
                if (properties.TryGetValue("LastChildFill", out var lastChildFill)
                    && bool.TryParse(lastChildFill, out var parsedLastChildFill))
                {
                    dockPanel.LastChildFill = parsedLastChildFill;
                }
                break;
            case WrapPanel wrapPanel:
                if (properties.TryGetValue("Orientation", out var orientation)
                    && Enum.TryParse<Orientation>(orientation, ignoreCase: true, out var parsedOrientation))
                {
                    wrapPanel.Orientation = parsedOrientation;
                }

                if (properties.TryGetValue("ItemWidth", out var itemWidth)
                    && double.TryParse(itemWidth, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedItemWidth))
                {
                    wrapPanel.ItemWidth = Math.Max(10, parsedItemWidth);
                }

                if (properties.TryGetValue("ItemHeight", out var itemHeight)
                    && double.TryParse(itemHeight, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedItemHeight))
                {
                    wrapPanel.ItemHeight = Math.Max(10, parsedItemHeight);
                }

                if (properties.TryGetValue("ItemSpacing", out var itemSpacing)
                    && double.TryParse(itemSpacing, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedItemSpacing))
                {
                    wrapPanel.ItemSpacing = Math.Max(0, parsedItemSpacing);
                }

                if (properties.TryGetValue("LineSpacing", out var lineSpacing)
                    && double.TryParse(lineSpacing, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedLineSpacing))
                {
                    wrapPanel.LineSpacing = Math.Max(0, parsedLineSpacing);
                }

                if (properties.TryGetValue("ItemsAlignment", out var itemsAlignment)
                    && Enum.TryParse<WrapPanelItemsAlignment>(
                        itemsAlignment,
                        ignoreCase: true,
                        out var parsedItemsAlignment))
                {
                    wrapPanel.ItemsAlignment = parsedItemsAlignment;
                }
                break;
            case UniformGrid uniformGrid:
                if (properties.TryGetValue("Rows", out var rows)
                    && int.TryParse(rows, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedRows))
                {
                    uniformGrid.Rows = Math.Max(0, parsedRows);
                }

                if (properties.TryGetValue("Columns", out var columns)
                    && int.TryParse(columns, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedColumns))
                {
                    uniformGrid.Columns = Math.Max(0, parsedColumns);
                }

                if (properties.TryGetValue("FirstColumn", out var firstColumn)
                    && int.TryParse(firstColumn, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedFirstColumn))
                {
                    uniformGrid.FirstColumn = Math.Max(0, parsedFirstColumn);
                }

                if (properties.TryGetValue("RowSpacing", out var rowSpacing)
                    && double.TryParse(rowSpacing, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedRowSpacing))
                {
                    uniformGrid.RowSpacing = Math.Max(0, parsedRowSpacing);
                }

                if (properties.TryGetValue("ColumnSpacing", out var columnSpacing)
                    && double.TryParse(columnSpacing, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedColumnSpacing))
                {
                    uniformGrid.ColumnSpacing = Math.Max(0, parsedColumnSpacing);
                }
                break;
            case Canvas canvas:
                if (properties.TryGetValue("Background", out var canvasBackground))
                {
                    TrySetBorderBrush(value => canvas.Background = value, canvasBackground, colorResources);
                }
                break;
        }
    }

    private static void TrySetTextForeground(
        TextBlock textBlock,
        string foreground,
        IReadOnlyDictionary<string, string> colorResources)
    {
        TrySetBorderBrush(value => textBlock.Foreground = value, foreground, colorResources);
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

    private static void ApplyImageProperties(Image image, IReadOnlyDictionary<string, string> properties)
    {
        if (properties.TryGetValue("Source", out var source))
        {
            TryLoadImageSource(image, source);
        }

        if (properties.TryGetValue("Stretch", out var stretch)
            && Enum.TryParse<Stretch>(stretch, ignoreCase: true, out var parsedStretch))
        {
            image.Stretch = parsedStretch;
        }
    }

    private static void TryLoadImageSource(Image image, string source)
    {
        try
        {
            var path = Uri.TryCreate(source, UriKind.Absolute, out var uri)
                ? uri.IsFile ? uri.LocalPath : null
                : System.IO.Path.GetFullPath(source);
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            if (image.Source is IDisposable disposable)
            {
                disposable.Dispose();
            }

            image.Source = new Bitmap(path);
            image.Tag = source;
        }
        catch
        {
            // Keep the preview usable when an imported image source is missing or invalid.
        }
    }

    private static void ApplySliderProperties(Slider slider, IReadOnlyDictionary<string, string> properties)
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
    }

    private static void ApplyComboBoxProperties(ComboBox comboBox, IReadOnlyDictionary<string, string> properties)
    {
        if (properties.TryGetValue("__items", out var itemsJson))
        {
            List<string>? items;
            try
            {
                items = JsonSerializer.Deserialize<List<string>>(itemsJson);
            }
            catch
            {
                items = null;
            }

            if (items is not null)
            {
                comboBox.Items.Clear();
                foreach (var item in items)
                {
                    comboBox.Items.Add(item);
                }
            }
        }

        if (properties.TryGetValue("SelectedIndex", out var selectedIndex)
            && int.TryParse(selectedIndex, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSelectedIndex))
        {
            comboBox.SelectedIndex = Math.Clamp(parsedSelectedIndex, -1, comboBox.Items.Count - 1);
        }
    }

    private static void ApplyProgressBarProperties(ProgressBar progressBar, IReadOnlyDictionary<string, string> properties)
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
    }

    private static void ApplyDatePickerProperties(DatePicker datePicker, IReadOnlyDictionary<string, string> properties)
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
    }

    private static void ApplyCalendarDatePickerProperties(CalendarDatePicker calendarDatePicker, IReadOnlyDictionary<string, string> properties)
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
    }

    private static void ApplyTimePickerProperties(TimePicker timePicker, IReadOnlyDictionary<string, string> properties)
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
    }

    private static void ApplyNumericUpDownProperties(NumericUpDown numericUpDown, IReadOnlyDictionary<string, string> properties)
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
    }

    private static TabControl CreateDefaultTabControl()
        => new()
        {
            SelectedIndex = 0,
            Items =
            {
                CreateTabItem("Overview"),
                CreateTabItem("Details"),
            },
        };

    private static void ApplyTabControlProperties(TabControl tabControl, IReadOnlyDictionary<string, string> properties)
    {
        if (properties.TryGetValue("__tabs", out var tabsJson))
        {
            List<string>? tabs;
            try
            {
                tabs = JsonSerializer.Deserialize<List<string>>(tabsJson);
            }
            catch
            {
                tabs = null;
            }

            if (tabs is not null)
            {
                tabControl.Items.Clear();
                foreach (var tab in tabs)
                {
                    tabControl.Items.Add(CreateTabItem(tab));
                }
            }
        }

        if (properties.TryGetValue("SelectedIndex", out var selectedIndex)
            && int.TryParse(selectedIndex, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSelectedIndex))
        {
            tabControl.SelectedIndex = Math.Clamp(parsedSelectedIndex, -1, tabControl.Items.Count - 1);
        }
    }

    private static TabItem CreateTabItem(string header)
        => new()
        {
            Header = header,
            Content = new TextBlock { Text = $"{header} content", Margin = new Thickness(12) },
        };

    private static void ApplyExpanderProperties(Expander expander, IReadOnlyDictionary<string, string> properties)
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
            expander.Content = new TextBlock { Text = contentText, Margin = new Thickness(8) };
        }
    }

    private static void ApplyScrollViewerProperties(ScrollViewer scrollViewer, IReadOnlyDictionary<string, string> properties)
    {
        if (properties.TryGetValue("__contentText", out var contentText))
        {
            scrollViewer.Content = new TextBlock { Text = contentText, Margin = new Thickness(8) };
        }
    }

    private static void ApplyListBoxProperties(ListBox listBox, IReadOnlyDictionary<string, string> properties)
    {
        if (properties.TryGetValue("__items", out var itemsJson))
        {
            List<string>? items;
            try
            {
                items = JsonSerializer.Deserialize<List<string>>(itemsJson);
            }
            catch
            {
                items = null;
            }

            if (items is not null)
            {
                listBox.Items.Clear();
                foreach (var item in items)
                {
                    listBox.Items.Add(item);
                }
            }
        }

        if (properties.TryGetValue("SelectedIndex", out var selectedIndex)
            && int.TryParse(selectedIndex, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSelectedIndex))
        {
            listBox.SelectedIndex = Math.Clamp(parsedSelectedIndex, -1, listBox.Items.Count - 1);
        }
    }

    private static void ApplyBorderProperties(
        Border border,
        IReadOnlyDictionary<string, string> properties,
        IReadOnlyDictionary<string, string> colorResources)
    {
        if (properties.TryGetValue("Background", out var background))
        {
            TrySetBorderBrush(value => border.Background = value, background, colorResources);
        }

        if (properties.TryGetValue("BorderBrush", out var borderBrush))
        {
            TrySetBorderBrush(value => border.BorderBrush = value, borderBrush, colorResources);
        }

        if (properties.TryGetValue("BorderThickness", out var borderThickness))
        {
            try
            {
                border.BorderThickness = Thickness.Parse(borderThickness);
            }
            catch (FormatException)
            {
                // Ignore malformed imported thickness values.
            }
        }

        if (properties.TryGetValue("CornerRadius", out var cornerRadius))
        {
            try
            {
                border.CornerRadius = CornerRadius.Parse(cornerRadius);
            }
            catch (FormatException)
            {
                // Ignore malformed imported corner-radius values.
            }
        }

        if (properties.TryGetValue("__contentText", out var contentText))
        {
            border.Child = new TextBlock
            {
                Text = contentText,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
        }
        else
        {
            border.Child = null;
        }
    }

    private static void ApplyTemplatedAppearanceProperties(
        Control control,
        IReadOnlyDictionary<string, string> properties,
        IReadOnlyDictionary<string, string> colorResources)
    {
        if (control is not Avalonia.Controls.Primitives.TemplatedControl templated)
        {
            return;
        }

        if (properties.TryGetValue("Background", out var background))
        {
            TrySetBorderBrush(value => templated.Background = value, background, colorResources);
        }

        if (properties.TryGetValue("Foreground", out var foreground))
        {
            TrySetBorderBrush(value => templated.Foreground = value, foreground, colorResources);
        }

        if (properties.TryGetValue("BorderBrush", out var borderBrush))
        {
            TrySetBorderBrush(value => templated.BorderBrush = value, borderBrush, colorResources);
        }

        if (properties.TryGetValue("BorderThickness", out var borderThickness))
        {
            try
            {
                templated.BorderThickness = Thickness.Parse(borderThickness);
            }
            catch (FormatException)
            {
                // Ignore malformed imported thickness values.
            }
        }

        if (properties.TryGetValue("CornerRadius", out var cornerRadius))
        {
            try
            {
                templated.CornerRadius = CornerRadius.Parse(cornerRadius);
            }
            catch (FormatException)
            {
                // Ignore malformed imported corner-radius values.
            }
        }
    }

    private static void TrySetBorderBrush(
        Action<IBrush?> applyBrush,
        string value,
        IReadOnlyDictionary<string, string> colorResources)
    {
        if (DesignerResourceReferenceMetadata.TryParseExpression(value, out var resourceKey))
        {
            if (!colorResources.TryGetValue(resourceKey, out var resourceValue)
                || string.IsNullOrWhiteSpace(resourceValue))
            {
                return;
            }

            value = resourceValue;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            applyBrush(null);
            return;
        }

        try
        {
            applyBrush(Brush.Parse(value));
        }
        catch (FormatException)
        {
            // Ignore malformed imported brushes while keeping the preview available.
        }
    }

    private static void ApplyStackPanelProperties(StackPanel stackPanel, IReadOnlyDictionary<string, string> properties)
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

        if (!properties.TryGetValue("__children", out var childrenJson))
        {
            return;
        }

        List<StackPanelChildSnapshot>? children;
        try
        {
            children = JsonSerializer.Deserialize<List<StackPanelChildSnapshot>>(childrenJson);
        }
        catch
        {
            return;
        }

        if (children is null)
        {
            return;
        }

        stackPanel.Children.Clear();
        foreach (var child in children)
        {
            Control? control = child.TypeName switch
            {
                "TextBlock" => new TextBlock { Text = child.Text ?? string.Empty },
                "Button" => new Button { Content = child.Content ?? string.Empty },
                "TextBox" => new TextBox
                {
                    Text = string.IsNullOrEmpty(child.PasswordChar) ? child.Text ?? string.Empty : string.Empty,
                    Watermark = child.Watermark,
                    PasswordChar = string.IsNullOrEmpty(child.PasswordChar) ? '\0' : child.PasswordChar[0],
                    RevealPassword = child.RevealPassword ?? false,
                    AcceptsReturn = child.AcceptsReturn ?? false,
                    TextWrapping = Enum.TryParse<TextWrapping>(child.TextWrapping, ignoreCase: true, out var textWrapping)
                        ? textWrapping
                        : TextWrapping.NoWrap,
                },
                _ => null,
            };

            if (control is not null)
            {
                stackPanel.Children.Add(control);
            }
        }
    }

    private sealed record StackPanelChildSnapshot(
        string TypeName,
        string? Text = null,
        string? Content = null,
        string? Watermark = null,
        string? PasswordChar = null,
        bool? RevealPassword = null,
        bool? AcceptsReturn = null,
        string? TextWrapping = null);
}
