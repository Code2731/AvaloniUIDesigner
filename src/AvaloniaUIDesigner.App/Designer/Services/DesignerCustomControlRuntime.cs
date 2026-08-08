using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaUIDesigner.App.Models;

namespace AvaloniaUIDesigner.App.Designer.Services;

public static class DesignerCustomControlRuntime
{
    public static Control CreatePlaceholder(
        string typeName,
        string previewText,
        IReadOnlyDictionary<string, string>? defaultProperties = null)
        => new Border
        {
            Background = Brush.Parse("#E0F2FE"),
            BorderBrush = Brush.Parse("#0284C7"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10),
            Tag = new DesignerCustomControlMetadata(
                previewText,
                new Dictionary<string, string>(
                    defaultProperties ?? new Dictionary<string, string>(),
                    System.StringComparer.Ordinal)),
            Child = new StackPanel
            {
                Spacing = 3,
                Children =
                {
                    new TextBlock
                    {
                        Text = previewText,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = Brush.Parse("#075985"),
                    },
                    new TextBlock
                    {
                        Text = typeName,
                        FontSize = 11,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = Brush.Parse("#0369A1"),
                    },
                },
            },
        };
}
