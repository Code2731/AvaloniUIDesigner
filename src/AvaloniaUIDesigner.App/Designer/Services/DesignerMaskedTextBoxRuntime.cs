using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerMaskedTextBoxValues(
    string Mask,
    char PromptChar,
    bool HidePromptOnLeave);

public sealed record DesignerMaskedTextBoxEditorInput(
    string Mask,
    string PromptChar,
    bool HidePromptOnLeave);

public sealed record DesignerMaskedTextBoxAttribute(string Name, string Value);

public static class DesignerMaskedTextBoxRuntime
{
    private static readonly string[] Properties =
    [
        "Mask",
        "PromptChar",
        "HidePromptOnLeave",
    ];

    public static bool IsSupportedControl(Control control)
        => control is MaskedTextBox;

    public static bool TryRead(
        Control control,
        out DesignerMaskedTextBoxValues values,
        out string error)
    {
        if (control is MaskedTextBox maskedTextBox)
        {
            values = new DesignerMaskedTextBoxValues(
                maskedTextBox.Mask ?? string.Empty,
                maskedTextBox.PromptChar,
                maskedTextBox.HidePromptOnLeave);
            error = string.Empty;
            return true;
        }

        values = default!;
        error = "Mask editing is available for MaskedTextBox controls.";
        return false;
    }

    public static bool TryParseValues(
        DesignerMaskedTextBoxEditorInput input,
        DesignerMaskedTextBoxValues current,
        out DesignerMaskedTextBoxValues values,
        out string error)
    {
        if (input.PromptChar.Length != 1)
        {
            values = default!;
            error = "Prompt character must be exactly one character.";
            return false;
        }

        var promptChar = input.PromptChar[0];
        if (!TryValidateMask(input.Mask, promptChar, out error))
        {
            values = default!;
            return false;
        }

        values = current with
        {
            Mask = input.Mask,
            PromptChar = promptChar,
            HidePromptOnLeave = input.HidePromptOnLeave,
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
        if (control is not MaskedTextBox maskedTextBox
            || !TryRead(maskedTextBox, out var current, out _))
        {
            return;
        }

        var input = new DesignerMaskedTextBoxEditorInput(
            Get(properties, "Mask", current.Mask),
            Get(properties, "PromptChar", current.PromptChar.ToString()),
            GetBoolean(properties, "HidePromptOnLeave", current.HidePromptOnLeave));
        if (TryParseValues(input, current, out var values, out _))
        {
            Apply(maskedTextBox, values);
        }
    }

    public static void Apply(
        MaskedTextBox maskedTextBox,
        DesignerMaskedTextBoxValues values)
    {
        maskedTextBox.PromptChar = values.PromptChar;
        maskedTextBox.Mask = values.Mask;
        maskedTextBox.HidePromptOnLeave = values.HidePromptOnLeave;
    }

    public static bool IsSupportedProperty(string tagName, string propertyName)
        => string.Equals(tagName, "MaskedTextBox", StringComparison.OrdinalIgnoreCase)
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
        if (!string.Equals(tagName, "MaskedTextBox", StringComparison.OrdinalIgnoreCase)
            || canonicalName.Length == 0)
        {
            error = $"{tagName}.{propertyName} is not a supported MaskedTextBox property.";
            return false;
        }

        switch (canonicalName)
        {
            case "Mask":
                if (!TryValidateMask(rawValue, GetPromptCharFallback(), out error))
                {
                    return false;
                }

                normalizedValue = rawValue;
                return true;
            case "PromptChar":
                if (rawValue.Length != 1)
                {
                    error = "PromptChar must be exactly one character.";
                    return false;
                }

                normalizedValue = rawValue;
                error = string.Empty;
                return true;
            case "HidePromptOnLeave":
                if (!bool.TryParse(rawValue.Trim(), out var boolean))
                {
                    error = "HidePromptOnLeave must be True or False.";
                    return false;
                }

                normalizedValue = boolean.ToString();
                error = string.Empty;
                return true;
            default:
                error = $"{canonicalName} is not a supported MaskedTextBox property.";
                return false;
        }
    }

    public static bool TryValidateProperties(
        string tagName,
        IReadOnlyDictionary<string, string> properties,
        out string error)
    {
        if (!string.Equals(tagName, "MaskedTextBox", StringComparison.OrdinalIgnoreCase))
        {
            error = string.Empty;
            return true;
        }

        var promptChar = Get(properties, "PromptChar", GetPromptCharFallback().ToString());
        if (promptChar.Length != 1)
        {
            error = "PromptChar must be exactly one character.";
            return false;
        }

        if (TryGetValue(properties, "Mask", out var mask)
            && !TryValidateMask(mask, promptChar[0], out error))
        {
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static void RemoveProperties(string tagName, IDictionary<string, string> properties)
    {
        if (string.Equals(tagName, "MaskedTextBox", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var propertyName in Properties)
            {
                properties.Remove(propertyName);
            }
        }
    }

    public static IReadOnlyList<DesignerMaskedTextBoxAttribute> GetAxamlAttributes(Control control)
        => TryRead(control, out var values, out _)
            ?
            [
                new("Mask", values.Mask),
                new("PromptChar", values.PromptChar.ToString()),
                new("HidePromptOnLeave", values.HidePromptOnLeave.ToString()),
            ]
            : [];

    private static bool TryValidateMask(string mask, char promptChar, out string error)
    {
        try
        {
            _ = new MaskedTextProvider(
                mask,
                culture: null,
                allowPromptAsInput: true,
                promptChar: promptChar,
                passwordChar: '\0',
                restrictToAscii: false);
            error = string.Empty;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException or FormatException or InvalidOperationException)
        {
            error = $"Mask is not valid: {exception.Message}";
            return false;
        }
    }

    private static char GetPromptCharFallback()
        => '_';

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
