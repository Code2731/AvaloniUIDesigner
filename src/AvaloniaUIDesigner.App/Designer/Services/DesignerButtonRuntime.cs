using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using AvaloniaUIDesigner.App.Models;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerButtonValues(
    string Content,
    ClickMode ClickMode,
    string HotKey,
    bool IsDefault,
    bool IsCancel,
    string CommandParameter,
    string ClickHandler);

public sealed record DesignerButtonEditorInput(
    string Content,
    string ClickMode,
    string HotKey,
    bool IsDefault,
    bool IsCancel,
    string CommandParameter,
    string ClickHandler);

public sealed record DesignerButtonAttribute(string Name, string Value);

public static class DesignerButtonRuntime
{
    private static readonly string[] PropertyNames =
    [
        "Content",
        "ClickMode",
        "HotKey",
        "IsDefault",
        "IsCancel",
        "CommandParameter",
    ];

    public static IReadOnlyList<string> ClickModeNames { get; } =
        Enum.GetNames<ClickMode>();

    public static bool IsSupportedControl(Control control)
        => control is Button and not ToggleButton;

    public static bool TryRead(
        Control control,
        out DesignerButtonValues values,
        out string error)
    {
        if (control is not Button button || control is ToggleButton)
        {
            values = default!;
            error = "Button actions and commands editing is available for Button controls.";
            return false;
        }

        values = new DesignerButtonValues(
            button.Content?.ToString() ?? string.Empty,
            button.ClickMode,
            button.HotKey?.ToString() ?? string.Empty,
            button.IsDefault,
            button.IsCancel,
            button.CommandParameter?.ToString() ?? string.Empty,
            (button.Tag as ButtonClickHandlerMetadata)?.HandlerName
                ?? string.Empty);
        error = string.Empty;
        return true;
    }

    public static bool TryParseValues(
        Control control,
        DesignerButtonEditorInput input,
        out DesignerButtonValues values,
        out string error)
    {
        if (!TryRead(control, out _, out error))
        {
            values = default!;
            return false;
        }

        if (!TryParseEnum(
                input.ClickMode,
                "ClickMode",
                out ClickMode clickMode,
                out error)
            || !TryNormalizeHotKey(input.HotKey, out var hotKey, out error)
            || !TryNormalizeClickHandler(
                input.ClickHandler,
                out var clickHandler,
                out error))
        {
            values = default!;
            return false;
        }

        values = new DesignerButtonValues(
            input.Content,
            clickMode,
            hotKey,
            input.IsDefault,
            input.IsCancel,
            input.CommandParameter.Trim(),
            clickHandler);
        error = string.Empty;
        return true;
    }

    public static void Capture(
        Control control,
        IDictionary<string, string> properties)
    {
        if (!TryRead(control, out var values, out _))
        {
            return;
        }

        foreach (var attribute in GetAxamlAttributes(control))
        {
            if (!string.Equals(
                    attribute.Name,
                    "Click",
                    StringComparison.Ordinal))
            {
                properties[attribute.Name] = attribute.Value;
            }
        }

        if (values.HotKey.Length == 0)
        {
            properties.Remove("HotKey");
        }

        if (values.CommandParameter.Length == 0)
        {
            properties.Remove("CommandParameter");
        }

        if (values.ClickHandler.Length == 0)
        {
            properties.Remove("__clickHandler");
        }
        else
        {
            properties["__clickHandler"] = values.ClickHandler;
        }
    }

    public static void Apply(
        Button button,
        IReadOnlyDictionary<string, string> properties)
    {
        if (button is ToggleButton
            || !TryRead(button, out var current, out _))
        {
            return;
        }

        var input = new DesignerButtonEditorInput(
            Get(properties, "Content", current.Content),
            Get(properties, "ClickMode", current.ClickMode.ToString()),
            Get(properties, "HotKey", current.HotKey),
            GetBoolean(properties, "IsDefault", current.IsDefault),
            GetBoolean(properties, "IsCancel", current.IsCancel),
            Get(properties, "CommandParameter", current.CommandParameter),
            Get(properties, "__clickHandler", current.ClickHandler));
        if (TryParseValues(button, input, out var values, out _))
        {
            Apply(button, values);
        }
    }

    public static void Apply(Button button, DesignerButtonValues values)
    {
        button.Content = values.Content;
        button.ClickMode = values.ClickMode;
        button.HotKey = values.HotKey.Length == 0
            ? null
            : KeyGesture.Parse(values.HotKey);
        button.IsDefault = values.IsDefault;
        button.IsCancel = values.IsCancel;
        button.CommandParameter = values.CommandParameter.Length == 0
            ? null
            : values.CommandParameter;
        button.Tag = values.ClickHandler.Length == 0
            ? null
            : new ButtonClickHandlerMetadata(values.ClickHandler);
    }

    public static bool IsSupportedProperty(
        string tagName,
        string propertyName)
        => string.Equals(
                tagName.Trim(),
                "Button",
                StringComparison.OrdinalIgnoreCase)
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
        if (!string.Equals(
                tagName.Trim(),
                "Button",
                StringComparison.OrdinalIgnoreCase)
            || canonicalName.Length == 0)
        {
            error =
                $"{tagName}.{propertyName} is not a supported Button property.";
            return false;
        }

        switch (canonicalName)
        {
            case "ClickMode":
                return TryNormalizeEnum<ClickMode>(
                    rawValue,
                    canonicalName,
                    out normalizedValue,
                    out error);
            case "HotKey":
                return TryNormalizeHotKey(
                    rawValue,
                    out normalizedValue,
                    out error);
            case "IsDefault":
            case "IsCancel":
                if (!bool.TryParse(rawValue.Trim(), out var boolean))
                {
                    error = $"{canonicalName} must be True or False.";
                    return false;
                }

                normalizedValue = boolean.ToString();
                error = string.Empty;
                return true;
            case "CommandParameter":
                normalizedValue = rawValue.Trim();
                error = string.Empty;
                return true;
            default:
                normalizedValue = rawValue;
                error = string.Empty;
                return true;
        }
    }

    public static bool TryNormalizeClickHandler(
        string rawValue,
        out string normalizedValue,
        out string error)
    {
        normalizedValue = rawValue.Trim();
        if (normalizedValue.Length > 0
            && !IsValidIdentifier(normalizedValue))
        {
            error =
                "Click handler names must start with a letter or underscore and contain only letters, numbers, or underscores.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static IReadOnlyList<DesignerButtonAttribute> GetAxamlAttributes(
        Control control)
    {
        if (!TryRead(control, out var values, out _))
        {
            return [];
        }

        var attributes = new List<DesignerButtonAttribute>
        {
            new("Content", values.Content),
            new("ClickMode", values.ClickMode.ToString()),
            new("IsDefault", values.IsDefault.ToString()),
            new("IsCancel", values.IsCancel.ToString()),
        };
        if (values.HotKey.Length > 0)
        {
            attributes.Add(new("HotKey", values.HotKey));
        }

        if (values.CommandParameter.Length > 0)
        {
            attributes.Add(new("CommandParameter", values.CommandParameter));
        }

        if (values.ClickHandler.Length > 0)
        {
            attributes.Add(new("Click", values.ClickHandler));
        }

        return attributes;
    }

    private static bool TryNormalizeHotKey(
        string rawValue,
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
            normalizedValue = KeyGesture.Parse(candidate).ToString();
            error = string.Empty;
            return true;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            normalizedValue = string.Empty;
            error =
                "HotKey must be an Avalonia key gesture such as Ctrl+S or Alt+Enter.";
            return false;
        }
    }

    private static bool IsValidIdentifier(string value)
    {
        if (value.Length == 0
            || !(char.IsLetter(value[0]) || value[0] == '_'))
        {
            return false;
        }

        for (var index = 1; index < value.Length; index++)
        {
            if (!(char.IsLetterOrDigit(value[index])
                  || value[index] == '_'))
            {
                return false;
            }
        }

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
            error =
                $"{label} must be one of: {string.Join(", ", Enum.GetNames<T>())}.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryNormalizeEnum<T>(
        string rawValue,
        string propertyName,
        out string normalizedValue,
        out string error)
        where T : struct, Enum
    {
        if (!TryParseEnum(
                rawValue,
                propertyName,
                out T value,
                out error))
        {
            normalizedValue = string.Empty;
            return false;
        }

        normalizedValue = value.ToString();
        return true;
    }

    private static string Get(
        IReadOnlyDictionary<string, string> properties,
        string propertyName,
        string fallback)
    {
        foreach (var pair in properties)
        {
            if (string.Equals(
                    pair.Key,
                    propertyName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        return fallback;
    }

    private static bool GetBoolean(
        IReadOnlyDictionary<string, string> properties,
        string propertyName,
        bool fallback)
        => bool.TryParse(
            Get(properties, propertyName, fallback.ToString()),
            out var value)
            ? value
            : fallback;
}
