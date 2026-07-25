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
            "Avalonia.Controls.Slider" => new Slider { Minimum = 0, Maximum = 100, Value = 50 },
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
            case Slider slider:
                ApplySliderProperties(slider, properties);
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
