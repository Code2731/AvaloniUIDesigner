using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerAutoCompleteBoxValues(
    string Text,
    string Watermark,
    bool IsTextCompletionEnabled,
    int MinimumPrefixLength,
    TimeSpan MinimumPopulateDelay,
    AutoCompleteFilterMode FilterMode,
    double MaxDropDownHeight,
    bool IsDropDownOpen);

public sealed record DesignerAutoCompleteBoxEditorInput(
    string Text,
    string Watermark,
    bool IsTextCompletionEnabled,
    string MinimumPrefixLength,
    string MinimumPopulateDelay,
    string FilterMode,
    string MaxDropDownHeight,
    bool IsDropDownOpen);

public sealed record DesignerAutoCompleteBoxAttribute(string Name, string Value);

public static class DesignerAutoCompleteBoxRuntime
{
    private static readonly string[] Properties =
    [
        "Text",
        "Watermark",
        "IsTextCompletionEnabled",
        "MinimumPrefixLength",
        "MinimumPopulateDelay",
        "FilterMode",
        "MaxDropDownHeight",
        "IsDropDownOpen",
    ];

    public static IReadOnlyList<string> FilterModeNames { get; } =
        Enum.GetNames<AutoCompleteFilterMode>();

    public static bool IsSupportedControl(Control control)
        => control is AutoCompleteBox;

    public static bool TryRead(
        Control control,
        out DesignerAutoCompleteBoxValues values,
        out string error)
    {
        if (control is AutoCompleteBox autoCompleteBox)
        {
            values = new DesignerAutoCompleteBoxValues(
                autoCompleteBox.Text ?? string.Empty,
                autoCompleteBox.Watermark ?? string.Empty,
                autoCompleteBox.IsTextCompletionEnabled,
                autoCompleteBox.MinimumPrefixLength,
                autoCompleteBox.MinimumPopulateDelay,
                autoCompleteBox.FilterMode,
                autoCompleteBox.MaxDropDownHeight,
                autoCompleteBox.IsDropDownOpen);
            error = string.Empty;
            return true;
        }

        values = default!;
        error = "AutoCompleteBox editing is available for AutoCompleteBox controls.";
        return false;
    }

    public static bool TryParseValues(
        DesignerAutoCompleteBoxEditorInput input,
        DesignerAutoCompleteBoxValues current,
        out DesignerAutoCompleteBoxValues values,
        out string error)
    {
        if (!int.TryParse(
                input.MinimumPrefixLength.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var minimumPrefixLength)
            || minimumPrefixLength < 0)
        {
            values = default!;
            error = "Minimum prefix length must be a whole number greater than or equal to zero.";
            return false;
        }

        if (!TryParseDelay(input.MinimumPopulateDelay, out var minimumPopulateDelay, out error)
            || !TryParseEnum(
                input.FilterMode,
                "Filter mode",
                out AutoCompleteFilterMode filterMode,
                out error)
            || !TryParseHeight(input.MaxDropDownHeight, out var maxDropDownHeight, out error))
        {
            values = default!;
            return false;
        }

        values = current with
        {
            Text = input.Text,
            Watermark = input.Watermark,
            IsTextCompletionEnabled = input.IsTextCompletionEnabled,
            MinimumPrefixLength = minimumPrefixLength,
            MinimumPopulateDelay = minimumPopulateDelay,
            FilterMode = filterMode,
            MaxDropDownHeight = maxDropDownHeight,
            IsDropDownOpen = input.IsDropDownOpen,
        };
        error = string.Empty;
        return true;
    }

    public static void Capture(Control control, IDictionary<string, string> properties)
    {
        foreach (var attribute in GetAxamlAttributes(control))
        {
            properties[attribute.Name] = attribute.Value;
        }
    }

    public static void Apply(Control control, IReadOnlyDictionary<string, string> properties)
    {
        if (control is not AutoCompleteBox autoCompleteBox
            || !TryRead(autoCompleteBox, out var current, out _))
        {
            return;
        }

        var input = new DesignerAutoCompleteBoxEditorInput(
            Get(properties, "Text", current.Text),
            Get(properties, "Watermark", current.Watermark),
            GetBoolean(properties, "IsTextCompletionEnabled", current.IsTextCompletionEnabled),
            Get(properties, "MinimumPrefixLength", current.MinimumPrefixLength.ToString(CultureInfo.InvariantCulture)),
            Get(properties, "MinimumPopulateDelay", FormatDelay(current.MinimumPopulateDelay)),
            Get(properties, "FilterMode", current.FilterMode.ToString()),
            Get(properties, "MaxDropDownHeight", FormatHeight(current.MaxDropDownHeight)),
            GetBoolean(properties, "IsDropDownOpen", current.IsDropDownOpen));
        if (TryParseValues(input, current, out var values, out _))
        {
            Apply(autoCompleteBox, values);
        }
    }

    public static void Apply(
        AutoCompleteBox autoCompleteBox,
        DesignerAutoCompleteBoxValues values)
    {
        autoCompleteBox.Text = values.Text;
        autoCompleteBox.Watermark = values.Watermark;
        autoCompleteBox.IsTextCompletionEnabled = values.IsTextCompletionEnabled;
        autoCompleteBox.MinimumPrefixLength = values.MinimumPrefixLength;
        autoCompleteBox.MinimumPopulateDelay = values.MinimumPopulateDelay;
        autoCompleteBox.FilterMode = values.FilterMode;
        autoCompleteBox.MaxDropDownHeight = values.MaxDropDownHeight;
        autoCompleteBox.IsDropDownOpen = values.IsDropDownOpen;
    }

    public static bool IsSupportedProperty(string tagName, string propertyName)
        => string.Equals(tagName, "AutoCompleteBox", StringComparison.OrdinalIgnoreCase)
            && Array.Exists(
                Properties,
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
            Properties,
            candidate => string.Equals(candidate, propertyName.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? string.Empty;
        normalizedValue = string.Empty;
        if (!string.Equals(tagName, "AutoCompleteBox", StringComparison.OrdinalIgnoreCase)
            || canonicalName.Length == 0)
        {
            error = $"{tagName}.{propertyName} is not a supported AutoCompleteBox property.";
            return false;
        }

        switch (canonicalName)
        {
            case "Text":
            case "Watermark":
                normalizedValue = rawValue;
                error = string.Empty;
                return true;
            case "IsTextCompletionEnabled":
            case "IsDropDownOpen":
                if (!bool.TryParse(rawValue.Trim(), out var boolean))
                {
                    error = $"{canonicalName} must be True or False.";
                    return false;
                }

                normalizedValue = boolean.ToString();
                error = string.Empty;
                return true;
            case "MinimumPrefixLength":
                if (!int.TryParse(
                        rawValue.Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var minimumPrefixLength)
                    || minimumPrefixLength < 0)
                {
                    error = "MinimumPrefixLength must be a whole number greater than or equal to zero.";
                    return false;
                }

                normalizedValue = minimumPrefixLength.ToString(CultureInfo.InvariantCulture);
                error = string.Empty;
                return true;
            case "MinimumPopulateDelay":
                if (!TryParseDelay(rawValue, out var delay, out error))
                {
                    return false;
                }

                normalizedValue = FormatDelay(delay);
                return true;
            case "FilterMode":
                if (!TryNormalizeEnum<AutoCompleteFilterMode>(
                        rawValue,
                        canonicalName,
                        out normalizedValue,
                        out error))
                {
                    return false;
                }

                return true;
            case "MaxDropDownHeight":
                if (!TryParseHeight(rawValue, out var height, out error))
                {
                    return false;
                }

                normalizedValue = FormatHeight(height);
                return true;
            default:
                error = $"{canonicalName} is not a supported AutoCompleteBox property.";
                return false;
        }
    }

    public static bool TryValidateProperties(
        string tagName,
        IReadOnlyDictionary<string, string> properties,
        out string error)
    {
        if (!string.Equals(tagName, "AutoCompleteBox", StringComparison.OrdinalIgnoreCase))
        {
            error = string.Empty;
            return true;
        }

        if (TryGetValue(properties, "MinimumPrefixLength", out var rawPrefix)
            && (!int.TryParse(rawPrefix.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var prefix)
                || prefix < 0))
        {
            error = "MinimumPrefixLength must be a whole number greater than or equal to zero.";
            return false;
        }

        if (TryGetValue(properties, "MinimumPopulateDelay", out var rawDelay)
            && !TryParseDelay(rawDelay, out _, out error))
        {
            return false;
        }

        if (TryGetValue(properties, "MaxDropDownHeight", out var rawHeight)
            && !TryParseHeight(rawHeight, out _, out error))
        {
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static void RemoveProperties(string tagName, IDictionary<string, string> properties)
    {
        if (string.Equals(tagName, "AutoCompleteBox", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var propertyName in Properties)
            {
                properties.Remove(propertyName);
            }
        }
    }

    public static IReadOnlyList<DesignerAutoCompleteBoxAttribute> GetAxamlAttributes(Control control)
        => TryRead(control, out var values, out _)
            ? GetAxamlAttributes(values)
            : [];

    private static IReadOnlyList<DesignerAutoCompleteBoxAttribute> GetAxamlAttributes(
        DesignerAutoCompleteBoxValues values)
        =>
        [
            new("Text", values.Text),
            new("Watermark", values.Watermark),
            new("IsTextCompletionEnabled", values.IsTextCompletionEnabled.ToString()),
            new("MinimumPrefixLength", values.MinimumPrefixLength.ToString(CultureInfo.InvariantCulture)),
            new("MinimumPopulateDelay", FormatDelay(values.MinimumPopulateDelay)),
            new("FilterMode", values.FilterMode.ToString()),
            new("MaxDropDownHeight", FormatHeight(values.MaxDropDownHeight)),
            new("IsDropDownOpen", values.IsDropDownOpen.ToString()),
        ];

    private static bool TryParseDelay(string rawValue, out TimeSpan value, out string error)
    {
        var trimmed = rawValue.Trim();
        if (double.TryParse(
                trimmed,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var milliseconds)
            && double.IsFinite(milliseconds)
            && milliseconds >= 0
            && milliseconds <= TimeSpan.MaxValue.TotalMilliseconds)
        {
            value = TimeSpan.FromMilliseconds(milliseconds);
            error = string.Empty;
            return true;
        }

        if (TimeSpan.TryParse(trimmed, CultureInfo.InvariantCulture, out value)
            && value >= TimeSpan.Zero)
        {
            error = string.Empty;
            return true;
        }

        error = "Minimum populate delay must be milliseconds or a non-negative TimeSpan such as 00:00:00.2500000.";
        return false;
    }

    private static bool TryParseHeight(string rawValue, out double value, out string error)
    {
        var trimmed = rawValue.Trim();
        if (string.Equals(trimmed, "Infinity", StringComparison.OrdinalIgnoreCase)
            || trimmed == "∞"
            || trimmed == "+Infinity")
        {
            value = double.PositiveInfinity;
            error = string.Empty;
            return true;
        }

        if (!double.TryParse(
                trimmed,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value)
            || !double.IsFinite(value)
            || value <= 0)
        {
            error = "Maximum drop-down height must be Infinity or a finite number greater than zero.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryParseEnum<T>(
        string rawValue,
        string label,
        out T value,
        out string error)
        where T : struct, Enum
    {
        if (!Enum.TryParse(rawValue.Trim(), true, out value)
            || !Enum.IsDefined(value))
        {
            error = $"{label} must be one of: {string.Join(", ", Enum.GetNames<T>())}.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryNormalizeEnum<T>(
        string rawValue,
        string label,
        out string normalizedValue,
        out string error)
        where T : struct, Enum
    {
        if (!TryParseEnum(rawValue, label, out T value, out error))
        {
            normalizedValue = string.Empty;
            return false;
        }

        normalizedValue = value.ToString();
        return true;
    }

    private static string FormatDelay(TimeSpan value)
        => value.ToString("c", CultureInfo.InvariantCulture);

    private static string FormatHeight(double value)
        => double.IsPositiveInfinity(value)
            ? "Infinity"
            : value.ToString("0.###", CultureInfo.InvariantCulture);

    private static bool GetBoolean(
        IReadOnlyDictionary<string, string> properties,
        string key,
        bool fallback)
        => bool.TryParse(Get(properties, key, fallback.ToString()), out var value)
            ? value
            : fallback;

    private static string Get(
        IReadOnlyDictionary<string, string> properties,
        string key,
        string fallback)
        => TryGetValue(properties, key, out var value) ? value : fallback;

    private static bool TryGetValue(
        IReadOnlyDictionary<string, string> properties,
        string key,
        out string value)
    {
        foreach (var pair in properties)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }
}
