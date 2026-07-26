using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerLayoutValues(
    Thickness Margin,
    Thickness Padding,
    HorizontalAlignment HorizontalAlignment,
    VerticalAlignment VerticalAlignment,
    double MinWidth,
    double MinHeight,
    double MaxWidth,
    double MaxHeight);

public static class DesignerLayoutRuntime
{
    private static readonly string[] CommonPropertyNames =
    [
        "Margin",
        "HorizontalAlignment",
        "VerticalAlignment",
        "MinWidth",
        "MinHeight",
        "MaxWidth",
        "MaxHeight",
    ];

    public static bool IsSupportedProperty(string targetType, string propertyName)
        => CommonPropertyNames.Contains(propertyName, StringComparer.OrdinalIgnoreCase)
            || string.Equals(propertyName, "Padding", StringComparison.OrdinalIgnoreCase)
                && SupportsPadding(targetType);

    public static bool SupportsPadding(Control control)
        => control is TemplatedControl or Border;

    public static bool SupportsPadding(string targetType)
        => targetType is "Button" or "TextBox" or "Label" or "CheckBox" or "RadioButton"
            or "ToggleSwitch" or "ToggleButton" or "ComboBox" or "ListBox" or "TreeView"
            or "Menu" or "DataGrid" or "Slider" or "ProgressBar" or "DatePicker"
            or "CalendarDatePicker" or "TimePicker" or "NumericUpDown" or "TabControl"
            or "SplitView" or "Expander" or "ScrollViewer" or "Border";

    public static DesignerLayoutValues Read(Control control)
        => new(
            control.Margin,
            ReadPadding(control),
            control.HorizontalAlignment,
            control.VerticalAlignment,
            control.MinWidth,
            control.MinHeight,
            control.MaxWidth,
            control.MaxHeight);

    public static void Capture(
        Control control,
        IDictionary<string, string> properties)
    {
        if (control.IsSet(Layoutable.MarginProperty))
        {
            properties["Margin"] = FormatThickness(control.Margin);
        }

        if (control.IsSet(Layoutable.HorizontalAlignmentProperty))
        {
            properties["HorizontalAlignment"] = control.HorizontalAlignment.ToString();
        }

        if (control.IsSet(Layoutable.VerticalAlignmentProperty))
        {
            properties["VerticalAlignment"] = control.VerticalAlignment.ToString();
        }

        if (control.IsSet(Layoutable.MinWidthProperty))
        {
            properties["MinWidth"] = FormatNumber(control.MinWidth);
        }

        if (control.IsSet(Layoutable.MinHeightProperty))
        {
            properties["MinHeight"] = FormatNumber(control.MinHeight);
        }

        if (control.IsSet(Layoutable.MaxWidthProperty)
            && !double.IsPositiveInfinity(control.MaxWidth))
        {
            properties["MaxWidth"] = FormatMaximum(control.MaxWidth);
        }

        if (control.IsSet(Layoutable.MaxHeightProperty)
            && !double.IsPositiveInfinity(control.MaxHeight))
        {
            properties["MaxHeight"] = FormatMaximum(control.MaxHeight);
        }

        var padding = ReadPadding(control);
        if (HasLocalPadding(control))
        {
            properties["Padding"] = FormatThickness(padding);
        }
    }

    public static void Apply(
        Control control,
        IReadOnlyDictionary<string, string> properties)
    {
        if (!properties.Keys.Any(propertyName =>
                IsSupportedProperty(control.GetType().Name, propertyName)))
        {
            return;
        }

        var current = Read(control);
        if (!TryParseValues(
                control,
                properties.TryGetValue("Margin", out var margin)
                    ? margin
                    : FormatThickness(current.Margin),
                properties.TryGetValue("Padding", out var padding)
                    ? padding
                    : FormatThickness(current.Padding),
                properties.TryGetValue("HorizontalAlignment", out var horizontalAlignment)
                    ? horizontalAlignment
                    : current.HorizontalAlignment.ToString(),
                properties.TryGetValue("VerticalAlignment", out var verticalAlignment)
                    ? verticalAlignment
                    : current.VerticalAlignment.ToString(),
                properties.TryGetValue("MinWidth", out var minWidth)
                    ? minWidth
                    : FormatNumber(current.MinWidth),
                properties.TryGetValue("MinHeight", out var minHeight)
                    ? minHeight
                    : FormatNumber(current.MinHeight),
                properties.TryGetValue("MaxWidth", out var maxWidth)
                    ? maxWidth
                    : FormatMaximum(current.MaxWidth),
                properties.TryGetValue("MaxHeight", out var maxHeight)
                    ? maxHeight
                    : FormatMaximum(current.MaxHeight),
                out var values,
                out _))
        {
            return;
        }

        if (properties.ContainsKey("Margin"))
        {
            control.Margin = values.Margin;
        }

        if (properties.ContainsKey("HorizontalAlignment"))
        {
            control.HorizontalAlignment = values.HorizontalAlignment;
        }

        if (properties.ContainsKey("VerticalAlignment"))
        {
            control.VerticalAlignment = values.VerticalAlignment;
        }

        if (properties.ContainsKey("MinWidth"))
        {
            control.MinWidth = values.MinWidth;
        }

        if (properties.ContainsKey("MinHeight"))
        {
            control.MinHeight = values.MinHeight;
        }

        if (properties.ContainsKey("MaxWidth"))
        {
            if (double.IsPositiveInfinity(values.MaxWidth))
            {
                control.ClearValue(Layoutable.MaxWidthProperty);
            }
            else
            {
                control.MaxWidth = values.MaxWidth;
            }
        }

        if (properties.ContainsKey("MaxHeight"))
        {
            if (double.IsPositiveInfinity(values.MaxHeight))
            {
                control.ClearValue(Layoutable.MaxHeightProperty);
            }
            else
            {
                control.MaxHeight = values.MaxHeight;
            }
        }

        if (properties.ContainsKey("Padding") && SupportsPadding(control))
        {
            WritePadding(control, values.Padding);
        }
    }

    public static void Apply(Control control, DesignerLayoutValues values)
    {
        var current = Read(control);
        if (current.Margin != values.Margin)
        {
            control.Margin = values.Margin;
        }

        if (current.HorizontalAlignment != values.HorizontalAlignment)
        {
            control.HorizontalAlignment = values.HorizontalAlignment;
        }

        if (current.VerticalAlignment != values.VerticalAlignment)
        {
            control.VerticalAlignment = values.VerticalAlignment;
        }

        if (current.MinWidth != values.MinWidth)
        {
            control.MinWidth = values.MinWidth;
        }

        if (current.MinHeight != values.MinHeight)
        {
            control.MinHeight = values.MinHeight;
        }

        if (current.MaxWidth != values.MaxWidth)
        {
            if (double.IsPositiveInfinity(values.MaxWidth))
            {
                control.ClearValue(Layoutable.MaxWidthProperty);
            }
            else
            {
                control.MaxWidth = values.MaxWidth;
            }
        }

        if (current.MaxHeight != values.MaxHeight)
        {
            if (double.IsPositiveInfinity(values.MaxHeight))
            {
                control.ClearValue(Layoutable.MaxHeightProperty);
            }
            else
            {
                control.MaxHeight = values.MaxHeight;
            }
        }

        if (SupportsPadding(control) && current.Padding != values.Padding)
        {
            WritePadding(control, values.Padding);
        }
    }

    public static bool TryValidateConstraints(
        IReadOnlyDictionary<string, string> properties,
        out string error)
    {
        error = string.Empty;
        if (!TryParseConstraint(
                properties.TryGetValue("MinWidth", out var minWidth) ? minWidth : "0",
                isMaximum: false,
                out var parsedMinWidth,
                out _)
            || !TryParseConstraint(
                properties.TryGetValue("MinHeight", out var minHeight) ? minHeight : "0",
                isMaximum: false,
                out var parsedMinHeight,
                out _)
            || !TryParseConstraint(
                properties.TryGetValue("MaxWidth", out var maxWidth) ? maxWidth : string.Empty,
                isMaximum: true,
                out var parsedMaxWidth,
                out _)
            || !TryParseConstraint(
                properties.TryGetValue("MaxHeight", out var maxHeight) ? maxHeight : string.Empty,
                isMaximum: true,
                out var parsedMaxHeight,
                out _))
        {
            error = "Layout size constraints are invalid.";
            return false;
        }

        if (parsedMinWidth > parsedMaxWidth || parsedMinHeight > parsedMaxHeight)
        {
            error = "Each maximum size must be greater than or equal to its minimum size.";
            return false;
        }

        return true;
    }

    public static bool TryNormalizeProperty(
        string targetType,
        string propertyName,
        string rawValue,
        out string canonicalName,
        out string normalizedValue,
        out string error)
    {
        canonicalName = GetCanonicalPropertyName(propertyName);
        normalizedValue = string.Empty;
        error = string.Empty;
        if (!IsSupportedProperty(targetType, canonicalName))
        {
            error = $"{targetType}.{propertyName} is not a supported layout property.";
            return false;
        }

        if (canonicalName is "Margin" or "Padding")
        {
            if (!TryParseThickness(
                    rawValue,
                    allowNegative: canonicalName == "Margin",
                    out var thickness,
                    out error))
            {
                return false;
            }

            normalizedValue = FormatThickness(thickness);
            return true;
        }

        if (canonicalName == "HorizontalAlignment")
        {
            if (!Enum.TryParse<HorizontalAlignment>(
                    rawValue,
                    ignoreCase: true,
                    out var alignment)
                || !Enum.IsDefined(alignment))
            {
                error = "HorizontalAlignment must be Left, Center, Right, or Stretch.";
                return false;
            }

            normalizedValue = alignment.ToString();
            return true;
        }

        if (canonicalName == "VerticalAlignment")
        {
            if (!Enum.TryParse<VerticalAlignment>(
                    rawValue,
                    ignoreCase: true,
                    out var alignment)
                || !Enum.IsDefined(alignment))
            {
                error = "VerticalAlignment must be Top, Center, Bottom, or Stretch.";
                return false;
            }

            normalizedValue = alignment.ToString();
            return true;
        }

        var isMaximum = canonicalName is "MaxWidth" or "MaxHeight";
        if (!TryParseConstraint(rawValue, isMaximum, out var constraint, out error))
        {
            return false;
        }

        normalizedValue = isMaximum
            ? FormatMaximum(constraint)
            : FormatNumber(constraint);
        return true;
    }

    public static bool TryParseValues(
        Control control,
        string margin,
        string padding,
        string horizontalAlignment,
        string verticalAlignment,
        string minWidth,
        string minHeight,
        string maxWidth,
        string maxHeight,
        out DesignerLayoutValues values,
        out string error)
    {
        values = Read(control);
        if (!TryParseThickness(margin, allowNegative: true, out var parsedMargin, out error))
        {
            error = $"Margin: {error}";
            return false;
        }

        var parsedPadding = default(Thickness);
        if (SupportsPadding(control)
            && !TryParseThickness(padding, allowNegative: false, out parsedPadding, out error))
        {
            error = $"Padding: {error}";
            return false;
        }

        if (!Enum.TryParse<HorizontalAlignment>(
                horizontalAlignment,
                ignoreCase: true,
                out var parsedHorizontalAlignment)
            || !Enum.IsDefined(parsedHorizontalAlignment))
        {
            error = "Horizontal alignment must be Left, Center, Right, or Stretch.";
            return false;
        }

        if (!Enum.TryParse<VerticalAlignment>(
                verticalAlignment,
                ignoreCase: true,
                out var parsedVerticalAlignment)
            || !Enum.IsDefined(parsedVerticalAlignment))
        {
            error = "Vertical alignment must be Top, Center, Bottom, or Stretch.";
            return false;
        }

        if (!TryParseConstraint(minWidth, isMaximum: false, out var parsedMinWidth, out error))
        {
            error = $"Min width: {error}";
            return false;
        }

        if (!TryParseConstraint(minHeight, isMaximum: false, out var parsedMinHeight, out error))
        {
            error = $"Min height: {error}";
            return false;
        }

        if (!TryParseConstraint(maxWidth, isMaximum: true, out var parsedMaxWidth, out error))
        {
            error = $"Max width: {error}";
            return false;
        }

        if (!TryParseConstraint(maxHeight, isMaximum: true, out var parsedMaxHeight, out error))
        {
            error = $"Max height: {error}";
            return false;
        }

        if (parsedMinWidth > parsedMaxWidth || parsedMinHeight > parsedMaxHeight)
        {
            error = "Each maximum size must be greater than or equal to its minimum size.";
            return false;
        }

        values = new DesignerLayoutValues(
            parsedMargin,
            parsedPadding,
            parsedHorizontalAlignment,
            parsedVerticalAlignment,
            parsedMinWidth,
            parsedMinHeight,
            parsedMaxWidth,
            parsedMaxHeight);
        return true;
    }

    public static string FormatThickness(Thickness value)
    {
        if (value.Left == value.Top && value.Left == value.Right && value.Left == value.Bottom)
        {
            return FormatNumber(value.Left);
        }

        if (value.Left == value.Right && value.Top == value.Bottom)
        {
            return $"{FormatNumber(value.Left)},{FormatNumber(value.Top)}";
        }

        return string.Join(
            ",",
            FormatNumber(value.Left),
            FormatNumber(value.Top),
            FormatNumber(value.Right),
            FormatNumber(value.Bottom));
    }

    public static string FormatNumber(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    public static string FormatMaximum(double value)
        => double.IsPositiveInfinity(value) ? string.Empty : FormatNumber(value);

    private static bool TryParseThickness(
        string text,
        bool allowNegative,
        out Thickness thickness,
        out string error)
    {
        thickness = default;
        error = string.Empty;
        var parts = text.Split(
            ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is not (1 or 2 or 4)
            || parts.Any(part =>
                !double.TryParse(
                    part,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var value)
                || !double.IsFinite(value)
                || !allowNegative && value < 0))
        {
            error = allowNegative
                ? "Use one, two, or four finite comma-separated numbers."
                : "Use one, two, or four non-negative comma-separated numbers.";
            return false;
        }

        var values = parts
            .Select(part => double.Parse(part, CultureInfo.InvariantCulture))
            .ToArray();
        thickness = values.Length switch
        {
            1 => new Thickness(values[0]),
            2 => new Thickness(values[0], values[1]),
            _ => new Thickness(values[0], values[1], values[2], values[3]),
        };
        return true;
    }

    private static bool TryParseConstraint(
        string text,
        bool isMaximum,
        out double value,
        out string error)
    {
        error = string.Empty;
        var normalized = text.Trim();
        if (isMaximum
            && (normalized.Length == 0
                || string.Equals(normalized, "Infinity", StringComparison.OrdinalIgnoreCase)))
        {
            value = double.PositiveInfinity;
            return true;
        }

        if (!double.TryParse(
                normalized,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value)
            || !double.IsFinite(value)
            || value < 0)
        {
            error = isMaximum
                ? "Enter a non-negative finite number, or leave blank for no maximum."
                : "Enter a non-negative finite number.";
            return false;
        }

        return true;
    }

    private static string GetCanonicalPropertyName(string propertyName)
        => CommonPropertyNames
            .Append("Padding")
            .FirstOrDefault(name =>
                string.Equals(name, propertyName, StringComparison.OrdinalIgnoreCase))
            ?? propertyName;

    private static Thickness ReadPadding(Control control)
        => control switch
        {
            TemplatedControl templated => templated.Padding,
            Border border => border.Padding,
            _ => default,
        };

    private static bool HasLocalPadding(Control control)
        => control switch
        {
            TemplatedControl templated => templated.IsSet(TemplatedControl.PaddingProperty),
            Border border => border.IsSet(Border.PaddingProperty),
            _ => false,
        };

    private static void WritePadding(Control control, Thickness padding)
    {
        switch (control)
        {
            case TemplatedControl templated:
                templated.Padding = padding;
                break;
            case Border border:
                border.Padding = padding;
                break;
        }
    }
}
