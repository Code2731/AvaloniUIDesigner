using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerSelectableTextBlockValues(
    string Text,
    string SelectionBrush,
    string SelectionForegroundBrush);

public sealed record DesignerSelectableTextBlockEditorInput(
    string Text,
    string SelectionBrush,
    string SelectionForegroundBrush);

public sealed record DesignerSelectableTextBlockAttribute(string Name, string Value);

public static class DesignerSelectableTextBlockRuntime
{
    private static readonly string[] PropertyNames =
    [
        "SelectionBrush",
        "SelectionForegroundBrush",
    ];

    public static bool IsSupportedControl(Control control)
        => control.GetType() == typeof(SelectableTextBlock);

    public static bool TryRead(
        Control control,
        out DesignerSelectableTextBlockValues values,
        out string error)
    {
        if (control is not SelectableTextBlock textBlock)
        {
            values = default!;
            error = "SelectableTextBlock selection styling is available for SelectableTextBlock controls.";
            return false;
        }

        if (!TryFormatBrush(textBlock.SelectionBrush, "SelectionBrush", out var selectionBrush, out error)
            || !TryFormatBrush(
                textBlock.SelectionForegroundBrush,
                "SelectionForegroundBrush",
                out var selectionForegroundBrush,
                out error))
        {
            values = default!;
            return false;
        }

        values = new DesignerSelectableTextBlockValues(
            textBlock.Text ?? string.Empty,
            selectionBrush,
            selectionForegroundBrush);
        error = string.Empty;
        return true;
    }

    public static bool TryParseValues(
        Control control,
        DesignerSelectableTextBlockEditorInput input,
        out DesignerSelectableTextBlockValues values,
        out string error)
    {
        if (!TryRead(control, out var current, out error))
        {
            values = default!;
            return false;
        }

        if (!TryNormalizeBrush(
                input.SelectionBrush,
                "SelectionBrush",
                out var selectionBrush,
                out error)
            || !TryNormalizeBrush(
                input.SelectionForegroundBrush,
                "SelectionForegroundBrush",
                out var selectionForegroundBrush,
                out error))
        {
            values = default!;
            return false;
        }

        values = new DesignerSelectableTextBlockValues(
            input.Text,
            selectionBrush,
            selectionForegroundBrush);
        error = string.Empty;
        return true;
    }

    public static void Capture(
        Control control,
        IDictionary<string, string> properties)
    {
        if (!TryRead(control, out _, out _))
        {
            return;
        }

        foreach (var attribute in GetAxamlAttributes(control))
        {
            properties[attribute.Name] = attribute.Value;
        }

        foreach (var propertyName in PropertyNames)
        {
            if (!properties.ContainsKey(propertyName))
            {
                properties.Remove(propertyName);
            }
        }
    }

    public static void Apply(
        SelectableTextBlock textBlock,
        IReadOnlyDictionary<string, string> properties)
    {
        foreach (var propertyName in PropertyNames)
        {
            var rawValue = properties.TryGetValue(propertyName, out var storedValue)
                ? storedValue
                : string.Empty;
            if (!TryNormalizeBrush(rawValue, propertyName, out var normalizedValue, out _))
            {
                continue;
            }

            var brush = normalizedValue.Length == 0
                ? null
                : Brush.Parse(normalizedValue);
            if (propertyName == "SelectionBrush")
            {
                textBlock.SelectionBrush = brush;
            }
            else
            {
                textBlock.SelectionForegroundBrush = brush;
            }
        }
    }

    public static void Apply(
        SelectableTextBlock textBlock,
        DesignerSelectableTextBlockValues values)
    {
        textBlock.Text = values.Text;
        textBlock.SelectionBrush = ParseBrushOrNull(values.SelectionBrush);
        textBlock.SelectionForegroundBrush = ParseBrushOrNull(values.SelectionForegroundBrush);
    }

    public static bool IsSupportedProperty(
        string tagName,
        string propertyName)
        => string.Equals(tagName.Trim(), "SelectableTextBlock", StringComparison.OrdinalIgnoreCase)
            && Array.Exists(
                PropertyNames,
                candidate => string.Equals(
                    candidate,
                    propertyName.Trim(),
                    StringComparison.OrdinalIgnoreCase));

    public static bool TryNormalizeProperty(
        string tagName,
        string propertyName,
        string rawValue,
        out string canonicalName,
        out string normalizedValue,
        out string error)
    {
        canonicalName = Array.Find(
            PropertyNames,
            candidate => string.Equals(
                candidate,
                propertyName.Trim(),
                StringComparison.OrdinalIgnoreCase))
            ?? string.Empty;
        normalizedValue = string.Empty;
        if (!IsSupportedProperty(tagName, canonicalName))
        {
            error = $"{tagName}.{propertyName} is not a supported SelectableTextBlock property.";
            return false;
        }

        return TryNormalizeBrush(rawValue, canonicalName, out normalizedValue, out error);
    }

    public static bool TryValidateProperties(
        string tagName,
        IReadOnlyDictionary<string, string> properties,
        out string error)
    {
        if (!string.Equals(tagName, "SelectableTextBlock", StringComparison.OrdinalIgnoreCase))
        {
            error = string.Empty;
            return true;
        }

        foreach (var propertyName in PropertyNames)
        {
            if (properties.TryGetValue(propertyName, out var value)
                && !TryNormalizeBrush(value, propertyName, out _, out error))
            {
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    public static void RemoveProperties(
        string tagName,
        IDictionary<string, string> properties)
    {
        if (!string.Equals(tagName, "SelectableTextBlock", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var propertyName in PropertyNames)
        {
            properties.Remove(propertyName);
        }
    }

    public static IReadOnlyList<DesignerSelectableTextBlockAttribute> GetAxamlAttributes(
        Control control)
    {
        if (!TryRead(control, out var values, out _))
        {
            return [];
        }

        var attributes = new List<DesignerSelectableTextBlockAttribute>();
        if (values.SelectionBrush.Length > 0)
        {
            attributes.Add(new DesignerSelectableTextBlockAttribute(
                "SelectionBrush",
                values.SelectionBrush));
        }

        if (values.SelectionForegroundBrush.Length > 0)
        {
            attributes.Add(new DesignerSelectableTextBlockAttribute(
                "SelectionForegroundBrush",
                values.SelectionForegroundBrush));
        }

        return attributes;
    }

    private static bool TryNormalizeBrush(
        string rawValue,
        string propertyName,
        out string normalizedValue,
        out string error)
    {
        var candidate = rawValue.Trim();
        if (candidate.Length == 0)
        {
            normalizedValue = string.Empty;
            error = string.Empty;
            return true;
        }

        try
        {
            var brush = Brush.Parse(candidate);
            if (brush is not ISolidColorBrush solidBrush)
            {
                normalizedValue = string.Empty;
                error = $"{propertyName} must be a solid color such as #663B82F6 or Transparent.";
                return false;
            }

            normalizedValue = solidBrush.Color.ToString();
            error = string.Empty;
            return true;
        }
        catch (Exception exception) when (
            exception is FormatException or ArgumentException)
        {
            normalizedValue = string.Empty;
            error = $"{propertyName} must be a valid color such as #663B82F6 or Transparent.";
            return false;
        }
    }

    private static bool TryFormatBrush(
        IBrush? brush,
        string propertyName,
        out string value,
        out string error)
    {
        if (brush is null)
        {
            value = string.Empty;
            error = string.Empty;
            return true;
        }

        if (brush is not ISolidColorBrush solidBrush)
        {
            value = string.Empty;
            error = $"{propertyName} uses a non-solid brush that this editor cannot represent.";
            return false;
        }

        value = solidBrush.Color.ToString();
        error = string.Empty;
        return true;
    }

    private static IBrush? ParseBrushOrNull(string value)
        => value.Length == 0 ? null : Brush.Parse(value);
}
