using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerTextInputValues(
    string Text,
    string Watermark,
    bool AcceptsReturn,
    bool AcceptsTab,
    TextWrapping TextWrapping,
    TextAlignment TextAlignment,
    bool IsReadOnly,
    int MaxLength,
    int MinLines,
    int MaxLines,
    string PasswordChar,
    bool RevealPassword,
    bool UseFloatingWatermark,
    bool IsUndoEnabled,
    int UndoLimit,
    bool ClearSelectionOnLostFocus,
    bool IsInactiveSelectionHighlightEnabled);

public sealed record DesignerTextInputEditorInput(
    string Text,
    string Watermark,
    bool AcceptsReturn,
    bool AcceptsTab,
    string TextWrapping,
    string TextAlignment,
    bool IsReadOnly,
    string MaxLength,
    string MinLines,
    string MaxLines,
    string PasswordChar,
    bool RevealPassword,
    bool UseFloatingWatermark,
    bool IsUndoEnabled,
    string UndoLimit,
    bool ClearSelectionOnLostFocus,
    bool IsInactiveSelectionHighlightEnabled);

public sealed record DesignerTextInputAttribute(string Name, string Value);

public static class DesignerTextInputRuntime
{
    private static readonly string[] PropertyNames =
    [
        "Text",
        "Watermark",
        "AcceptsReturn",
        "AcceptsTab",
        "TextWrapping",
        "TextAlignment",
        "IsReadOnly",
        "MaxLength",
        "MinLines",
        "MaxLines",
        "PasswordChar",
        "RevealPassword",
        "UseFloatingWatermark",
        "IsUndoEnabled",
        "UndoLimit",
        "ClearSelectionOnLostFocus",
        "IsInactiveSelectionHighlightEnabled",
    ];

    public static IReadOnlyList<string> TextWrappingNames { get; } = Enum.GetNames<TextWrapping>();

    public static IReadOnlyList<string> TextAlignmentNames { get; } = Enum.GetNames<TextAlignment>();

    public static bool TryRead(
        Control control,
        out DesignerTextInputValues values,
        out string error)
    {
        if (control is not TextBox textBox)
        {
            values = default!;
            error = "Text input editing is available for TextBox controls.";
            return false;
        }

        values = new DesignerTextInputValues(
            textBox.PasswordChar == '\0' ? textBox.Text ?? string.Empty : string.Empty,
            textBox.Watermark?.ToString() ?? string.Empty,
            textBox.AcceptsReturn,
            textBox.AcceptsTab,
            textBox.TextWrapping,
            textBox.TextAlignment,
            textBox.IsReadOnly,
            textBox.MaxLength,
            textBox.MinLines,
            textBox.MaxLines,
            textBox.PasswordChar == '\0' ? string.Empty : textBox.PasswordChar.ToString(),
            textBox.RevealPassword,
            textBox.UseFloatingWatermark,
            textBox.IsUndoEnabled,
            textBox.UndoLimit,
            textBox.ClearSelectionOnLostFocus,
            textBox.IsInactiveSelectionHighlightEnabled);
        error = string.Empty;
        return true;
    }

    public static bool TryParseValues(
        DesignerTextInputEditorInput input,
        out DesignerTextInputValues values,
        out string error)
    {
        if (!Enum.TryParse<TextWrapping>(
                input.TextWrapping.Trim(),
                true,
                out var textWrapping)
            || !Enum.IsDefined(textWrapping))
        {
            values = default!;
            error = $"Text wrapping must be one of: {string.Join(", ", TextWrappingNames)}.";
            return false;
        }

        if (!Enum.TryParse<TextAlignment>(
                input.TextAlignment.Trim(),
                true,
                out var textAlignment)
            || !Enum.IsDefined(textAlignment))
        {
            values = default!;
            error = $"Text alignment must be one of: {string.Join(", ", TextAlignmentNames)}.";
            return false;
        }

        if (!TryParseNonNegativeInteger(input.MaxLength, "Max length", out var maxLength, out error)
            || !TryParseNonNegativeInteger(input.MinLines, "Min lines", out var minLines, out error)
            || !TryParseNonNegativeInteger(input.MaxLines, "Max lines", out var maxLines, out error)
            || !TryParseNonNegativeInteger(input.UndoLimit, "Undo limit", out var undoLimit, out error))
        {
            values = default!;
            return false;
        }

        if (minLines > 0 && maxLines > 0 && minLines > maxLines)
        {
            values = default!;
            error = "Min lines must not be greater than Max lines when both are non-zero.";
            return false;
        }

        var passwordChar = input.PasswordChar;
        if (passwordChar.Length > 1)
        {
            values = default!;
            error = "Password character must be blank or exactly one character.";
            return false;
        }

        values = new DesignerTextInputValues(
            passwordChar.Length == 0 ? input.Text : string.Empty,
            input.Watermark,
            input.AcceptsReturn,
            input.AcceptsTab,
            textWrapping,
            textAlignment,
            input.IsReadOnly,
            maxLength,
            minLines,
            maxLines,
            passwordChar,
            input.RevealPassword,
            input.UseFloatingWatermark,
            input.IsUndoEnabled,
            undoLimit,
            input.ClearSelectionOnLostFocus,
            input.IsInactiveSelectionHighlightEnabled);
        error = string.Empty;
        return true;
    }

    public static void Capture(Control control, IDictionary<string, string> properties)
    {
        if (!TryRead(control, out var values, out _))
        {
            return;
        }

        if (values.PasswordChar.Length > 0)
        {
            properties.Remove("Text");
        }

        var includeText = control is TextBox { PasswordChar: '\0', Text: not null };
        foreach (var attribute in GetAxamlAttributes(values, includeText))
        {
            properties[attribute.Name] = attribute.Value;
        }
    }

    public static void Apply(Control control, IReadOnlyDictionary<string, string> properties)
    {
        if (control is not TextBox textBox
            || !TryRead(textBox, out var current, out _))
        {
            return;
        }

        var input = new DesignerTextInputEditorInput(
            Get(properties, "Text", current.Text),
            Get(properties, "Watermark", current.Watermark),
            GetBoolean(properties, "AcceptsReturn", current.AcceptsReturn),
            GetBoolean(properties, "AcceptsTab", current.AcceptsTab),
            Get(properties, "TextWrapping", current.TextWrapping.ToString()),
            Get(properties, "TextAlignment", current.TextAlignment.ToString()),
            GetBoolean(properties, "IsReadOnly", current.IsReadOnly),
            Get(properties, "MaxLength", current.MaxLength.ToString(CultureInfo.InvariantCulture)),
            Get(properties, "MinLines", current.MinLines.ToString(CultureInfo.InvariantCulture)),
            Get(properties, "MaxLines", current.MaxLines.ToString(CultureInfo.InvariantCulture)),
            Get(properties, "PasswordChar", current.PasswordChar),
            GetBoolean(properties, "RevealPassword", current.RevealPassword),
            GetBoolean(properties, "UseFloatingWatermark", current.UseFloatingWatermark),
            GetBoolean(properties, "IsUndoEnabled", current.IsUndoEnabled),
            Get(properties, "UndoLimit", current.UndoLimit.ToString(CultureInfo.InvariantCulture)),
            GetBoolean(properties, "ClearSelectionOnLostFocus", current.ClearSelectionOnLostFocus),
            GetBoolean(
                properties,
                "IsInactiveSelectionHighlightEnabled",
                current.IsInactiveSelectionHighlightEnabled));
        if (TryParseValues(input, out var values, out _))
        {
            var applyText = values.PasswordChar.Length > 0
                || TryGetValue(properties, "Text", out _);
            Apply(textBox, values, applyText);
        }
    }

    public static void Apply(Control control, DesignerTextInputValues values)
        => Apply(control, values, applyText: true);

    private static void Apply(Control control, DesignerTextInputValues values, bool applyText)
    {
        if (control is not TextBox textBox)
        {
            return;
        }

        textBox.MaxLength = values.MaxLength;
        ApplyLineLimits(textBox, values.MinLines, values.MaxLines);
        textBox.PasswordChar = values.PasswordChar.Length == 0 ? '\0' : values.PasswordChar[0];
        if (applyText)
        {
            textBox.Text = values.PasswordChar.Length == 0 ? values.Text : string.Empty;
        }

        textBox.Watermark = values.Watermark;
        textBox.AcceptsReturn = values.AcceptsReturn;
        textBox.AcceptsTab = values.AcceptsTab;
        textBox.TextWrapping = values.TextWrapping;
        textBox.TextAlignment = values.TextAlignment;
        textBox.IsReadOnly = values.IsReadOnly;
        textBox.RevealPassword = values.RevealPassword;
        textBox.UseFloatingWatermark = values.UseFloatingWatermark;
        textBox.IsUndoEnabled = values.IsUndoEnabled;
        textBox.UndoLimit = values.UndoLimit;
        textBox.ClearSelectionOnLostFocus = values.ClearSelectionOnLostFocus;
        textBox.IsInactiveSelectionHighlightEnabled = values.IsInactiveSelectionHighlightEnabled;
    }

    private static void ApplyLineLimits(TextBox textBox, int minLines, int maxLines)
    {
        if (minLines > 0 && textBox.MaxLines > 0 && minLines > textBox.MaxLines)
        {
            textBox.MaxLines = maxLines;
        }
        else if (maxLines > 0 && textBox.MinLines > 0 && maxLines < textBox.MinLines)
        {
            textBox.MinLines = minLines;
        }

        textBox.MinLines = minLines;
        textBox.MaxLines = maxLines;
    }

    public static bool IsSupportedProperty(string tagName, string propertyName)
        => string.Equals(tagName, "TextBox", StringComparison.OrdinalIgnoreCase)
            && Array.Exists(
                PropertyNames,
                candidate => string.Equals(candidate, propertyName.Trim(), StringComparison.OrdinalIgnoreCase));

    public static bool TryNormalizeProperty(
        string propertyName,
        string rawValue,
        out string canonicalName,
        out string normalizedValue,
        out string error)
    {
        canonicalName = Array.Find(
            PropertyNames,
            candidate => string.Equals(candidate, propertyName.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? string.Empty;
        normalizedValue = string.Empty;
        if (canonicalName.Length == 0)
        {
            error = $"{propertyName} is not a supported TextBox input property.";
            return false;
        }

        switch (canonicalName)
        {
            case "Text":
            case "Watermark":
                normalizedValue = rawValue;
                error = string.Empty;
                return true;
            case "PasswordChar":
                if (rawValue.Length > 1)
                {
                    error = "PasswordChar must be blank or exactly one character.";
                    return false;
                }

                normalizedValue = rawValue;
                error = string.Empty;
                return true;
            case "MaxLength":
            case "MinLines":
            case "MaxLines":
            case "UndoLimit":
                if (!TryParseNonNegativeInteger(
                        rawValue,
                        canonicalName,
                        out var integer,
                        out error))
                {
                    return false;
                }

                normalizedValue = integer.ToString(CultureInfo.InvariantCulture);
                return true;
            case "TextWrapping":
                return TryNormalizeEnum<TextWrapping>(
                    rawValue,
                    canonicalName,
                    out normalizedValue,
                    out error);
            case "TextAlignment":
                return TryNormalizeEnum<TextAlignment>(
                    rawValue,
                    canonicalName,
                    out normalizedValue,
                    out error);
            default:
                if (!bool.TryParse(rawValue.Trim(), out var boolean))
                {
                    error = $"{canonicalName} must be True or False.";
                    return false;
                }

                normalizedValue = boolean.ToString();
                error = string.Empty;
                return true;
        }
    }

    public static bool TryValidateProperties(
        IReadOnlyDictionary<string, string> properties,
        out string error)
    {
        var minLines = TryGetValue(properties, "MinLines", out var rawMin)
            && int.TryParse(rawMin, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedMin)
                ? parsedMin
                : 0;
        var maxLines = TryGetValue(properties, "MaxLines", out var rawMax)
            && int.TryParse(rawMax, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedMax)
                ? parsedMax
                : 0;
        if (minLines > 0 && maxLines > 0 && minLines > maxLines)
        {
            error = "MinLines must not be greater than MaxLines when both are non-zero.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static void RemoveLineProperties(IDictionary<string, string> properties)
    {
        properties.Remove("MinLines");
        properties.Remove("MaxLines");
    }

    public static IReadOnlyList<DesignerTextInputAttribute> GetAxamlAttributes(Control control)
        => TryRead(control, out var values, out _)
            ? GetAxamlAttributes(
                values,
                control is TextBox { PasswordChar: '\0', Text: not null })
            : [];

    private static IReadOnlyList<DesignerTextInputAttribute> GetAxamlAttributes(
        DesignerTextInputValues values,
        bool includeText = true)
    {
        var attributes = new List<DesignerTextInputAttribute>();
        if (values.PasswordChar.Length == 0 && includeText)
        {
            attributes.Add(new("Text", values.Text));
        }

        attributes.Add(new("Watermark", values.Watermark));
        if (values.PasswordChar.Length > 0)
        {
            attributes.Add(new("PasswordChar", values.PasswordChar));
        }

        attributes.Add(new("RevealPassword", values.RevealPassword.ToString()));
        attributes.Add(new("AcceptsReturn", values.AcceptsReturn.ToString()));
        attributes.Add(new("AcceptsTab", values.AcceptsTab.ToString()));
        attributes.Add(new("TextWrapping", values.TextWrapping.ToString()));
        attributes.Add(new("TextAlignment", values.TextAlignment.ToString()));
        attributes.Add(new("IsReadOnly", values.IsReadOnly.ToString()));
        attributes.Add(new("MaxLength", values.MaxLength.ToString(CultureInfo.InvariantCulture)));
        attributes.Add(new("MinLines", values.MinLines.ToString(CultureInfo.InvariantCulture)));
        attributes.Add(new("MaxLines", values.MaxLines.ToString(CultureInfo.InvariantCulture)));
        attributes.Add(new("UseFloatingWatermark", values.UseFloatingWatermark.ToString()));
        attributes.Add(new("IsUndoEnabled", values.IsUndoEnabled.ToString()));
        attributes.Add(new("UndoLimit", values.UndoLimit.ToString(CultureInfo.InvariantCulture)));
        attributes.Add(new("ClearSelectionOnLostFocus", values.ClearSelectionOnLostFocus.ToString()));
        attributes.Add(new(
            "IsInactiveSelectionHighlightEnabled",
            values.IsInactiveSelectionHighlightEnabled.ToString()));
        return attributes;
    }

    private static bool TryParseNonNegativeInteger(
        string rawValue,
        string label,
        out int value,
        out string error)
    {
        if (!int.TryParse(
                rawValue.Trim(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out value)
            || value < 0)
        {
            error = $"{label} must be a non-negative integer.";
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
        if (!Enum.TryParse<T>(rawValue.Trim(), true, out var value)
            || !Enum.IsDefined(value))
        {
            normalizedValue = string.Empty;
            error = $"{label} must be one of: {string.Join(", ", Enum.GetNames<T>())}.";
            return false;
        }

        normalizedValue = value.ToString();
        error = string.Empty;
        return true;
    }

    private static string Get(
        IReadOnlyDictionary<string, string> properties,
        string key,
        string fallback)
        => TryGetValue(properties, key, out var value) ? value : fallback;

    private static bool GetBoolean(
        IReadOnlyDictionary<string, string> properties,
        string key,
        bool fallback)
        => bool.TryParse(Get(properties, key, fallback.ToString()), out var value)
            ? value
            : fallback;

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
