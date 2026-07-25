using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaUIDesigner.App.Designer.Core;

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

        var canvas = new Canvas
        {
            Background = Brush.Parse(settings.Background),
            Width = Math.Max(settings.Width, document.Elements.Count == 0 ? 0 : document.Elements.Max(element => element.X + element.Width + 32)),
            Height = Math.Max(settings.Height, document.Elements.Count == 0 ? 0 : document.Elements.Max(element => element.Y + element.Height + 32)),
        };

        foreach (var element in document.Elements)
        {
            var control = CreateControl(element);
            control.Width = element.Width;
            control.Height = element.Height;
            Canvas.SetLeft(control, element.X);
            Canvas.SetTop(control, element.Y);
            canvas.Children.Add(control);
        }

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

    private static Control CreateControl(DesignerElementSnapshot snapshot)
    {
        var control = snapshot.TypeName switch
        {
            "Avalonia.Controls.Button" => new Button { Content = "Button" },
            "Avalonia.Controls.TextBox" => new TextBox { Watermark = "Type here" },
            "Avalonia.Controls.TextBlock" => new TextBlock { Text = "Text" },
            "Avalonia.Controls.CheckBox" => new CheckBox { Content = "CheckBox" },
            "Avalonia.Controls.ToggleSwitch" => new ToggleSwitch { Content = "Toggle" },
            "Avalonia.Controls.ComboBox" => new ComboBox
            {
                SelectedIndex = 0,
                Items = { "Option 1", "Option 2", "Option 3" },
            },
            "Avalonia.Controls.Slider" => new Slider { Minimum = 0, Maximum = 100, Value = 50 },
            "Avalonia.Controls.ProgressBar" => new ProgressBar { Minimum = 0, Maximum = 100, Value = 50 },
            "Avalonia.Controls.Border" => new Border
            {
                Background = Brush.Parse("#F1F5F9"),
                BorderBrush = Brush.Parse("#94A3B8"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
            },
            "Avalonia.Controls.Grid" => new Grid { ShowGridLines = true },
            "Avalonia.Controls.StackPanel" => new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 6,
                Children =
                {
                    new TextBlock { Text = "StackPanel" },
                    new Button { Content = "Item" },
                },
            },
            _ => new TextBlock { Text = $"[Unsupported: {snapshot.DisplayName}]" },
        };

        ApplyProperties(control, snapshot.VisualProperties);
        return control;
    }

    private static void ApplyProperties(Control control, IReadOnlyDictionary<string, string>? properties)
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

        switch (control)
        {
            case Button button when properties.TryGetValue("Content", out var content):
                button.Content = content;
                break;
            case TextBox textBox:
                if (properties.TryGetValue("Text", out var text))
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
                    TrySetTextForeground(textBlock, foreground);
                }
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
            case ComboBox comboBox:
                ApplyComboBoxProperties(comboBox, properties);
                break;
            case Slider slider:
                ApplySliderProperties(slider, properties);
                break;
            case ProgressBar progressBar:
                ApplyProgressBarProperties(progressBar, properties);
                break;
            case Border border:
                ApplyBorderProperties(border, properties);
                break;
            case Grid grid when properties.TryGetValue("ShowGridLines", out var showGrid)
                && bool.TryParse(showGrid, out var parsedShowGrid):
                grid.ShowGridLines = parsedShowGrid;
                break;
            case StackPanel stackPanel:
                ApplyStackPanelProperties(stackPanel, properties);
                break;
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
            // Ignore malformed imported colors while keeping the preview available.
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

    private static void ApplyBorderProperties(Border border, IReadOnlyDictionary<string, string> properties)
    {
        if (properties.TryGetValue("Background", out var background))
        {
            TrySetBorderBrush(value => border.Background = value, background);
        }

        if (properties.TryGetValue("BorderBrush", out var borderBrush))
        {
            TrySetBorderBrush(value => border.BorderBrush = value, borderBrush);
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
    }

    private static void TrySetBorderBrush(Action<IBrush?> applyBrush, string value)
    {
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
            var control = child.TypeName switch
            {
                "TextBlock" => new TextBlock { Text = child.Text ?? string.Empty },
                "Button" => new Button { Content = child.Content ?? string.Empty },
                "TextBox" => new TextBox { Text = child.Text ?? string.Empty, Watermark = child.Watermark },
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
        string? Watermark = null);
}
