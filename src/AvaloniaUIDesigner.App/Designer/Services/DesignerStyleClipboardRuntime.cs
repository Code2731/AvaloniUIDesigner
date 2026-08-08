using System;
using System.Collections.Generic;
using System.Linq;

namespace AvaloniaUIDesigner.App.Designer.Services;

public static class DesignerStyleClipboardRuntime
{
    private static readonly HashSet<string> StylePropertyNames = new(StringComparer.Ordinal)
    {
        "Opacity",
        "Background",
        "Foreground",
        "BorderBrush",
        "BorderThickness",
        "CornerRadius",
        "Fill",
        "Stroke",
        "StrokeThickness",
        "StrokeDashArray",
        "StrokeDashOffset",
        "StrokeLineCap",
        "StrokeJoin",
        "StrokeMiterLimit",
        "RadiusX",
        "RadiusY",
        "StartPoint",
        "EndPoint",
        "Data",
        "Stretch",
        "Source",
        "BitmapInterpolationMode",
        "EdgeMode",
        "BitmapBlendingMode",
        "FontFamily",
        "FontSize",
        "FontStyle",
        "FontWeight",
        "TextAlignment",
        "TextWrapping",
        "HorizontalContentAlignment",
        "VerticalContentAlignment",
        "RenderTransform",
        "RenderTransformOrigin",
        "__visualEffect",
        "IsEnabled",
        "IsVisible",
        "IsHitTestVisible",
        "ClipToBounds",
        "UseLayoutRounding",
        "FlowDirection",
        "Cursor",
        "SelectionBrush",
        "SelectionForegroundBrush",
        "PaneBackground",
        "Classes",
    };

    public static IReadOnlyDictionary<string, string> Filter(
        IReadOnlyDictionary<string, string>? properties)
        => properties is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : properties
                .Where(pair => StylePropertyNames.Contains(pair.Key))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    public static bool IsStyleProperty(string propertyName)
        => StylePropertyNames.Contains(propertyName);
}
