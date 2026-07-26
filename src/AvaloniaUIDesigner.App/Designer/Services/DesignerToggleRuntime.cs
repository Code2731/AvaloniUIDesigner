using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace AvaloniaUIDesigner.App.Designer.Services;

public enum DesignerToggleControlKind
{
    CheckBox,
    RadioButton,
    ToggleSwitch,
    ToggleButton,
}

public enum DesignerToggleState
{
    Unchecked,
    Checked,
    Indeterminate,
}

public sealed record DesignerToggleValues(
    DesignerToggleControlKind Kind,
    string Content,
    bool? IsChecked,
    bool IsThreeState,
    ClickMode ClickMode,
    string GroupName,
    string OnContent,
    string OffContent,
    HorizontalAlignment HorizontalContentAlignment,
    VerticalAlignment VerticalContentAlignment);

public sealed record DesignerToggleEditorInput(
    string Content,
    string State,
    bool IsThreeState,
    string ClickMode,
    string GroupName,
    string OnContent,
    string OffContent,
    string HorizontalContentAlignment,
    string VerticalContentAlignment);

public sealed record DesignerToggleAttribute(string Name, string Value);

public static class DesignerToggleRuntime
{
    private const string NullExpression = "{x:Null}";
    private static readonly string[] CommonProperties =
    [
        "Content",
        "IsChecked",
        "IsThreeState",
        "ClickMode",
        "HorizontalContentAlignment",
        "VerticalContentAlignment",
    ];

    private static readonly string[] RadioButtonProperties =
        [.. CommonProperties, "GroupName"];

    private static readonly string[] ToggleSwitchProperties =
        [.. CommonProperties, "OnContent", "OffContent"];

    public static IReadOnlyList<string> StateNames { get; } =
        Enum.GetNames<DesignerToggleState>();

    public static IReadOnlyList<string> ClickModeNames { get; } =
        Enum.GetNames<ClickMode>();

    public static IReadOnlyList<string> HorizontalAlignmentNames { get; } =
        Enum.GetNames<HorizontalAlignment>();

    public static IReadOnlyList<string> VerticalAlignmentNames { get; } =
        Enum.GetNames<VerticalAlignment>();

    public static bool IsSupportedControl(Control control)
        => control is ToggleButton;

    public static bool TryRead(
        Control control,
        out DesignerToggleValues values,
        out string error)
    {
        if (control is not ToggleButton toggleButton)
        {
            values = default!;
            error = "Toggle editing is available for CheckBox, RadioButton, ToggleSwitch, and ToggleButton controls.";
            return false;
        }

        values = new DesignerToggleValues(
            GetKind(toggleButton),
            toggleButton.Content?.ToString() ?? string.Empty,
            toggleButton.IsChecked,
            toggleButton.IsThreeState,
            toggleButton.ClickMode,
            toggleButton is RadioButton radioButton
                ? radioButton.GroupName ?? string.Empty
                : string.Empty,
            toggleButton is ToggleSwitch toggleSwitch
                ? toggleSwitch.OnContent?.ToString() ?? string.Empty
                : string.Empty,
            toggleButton is ToggleSwitch toggle
                ? toggle.OffContent?.ToString() ?? string.Empty
                : string.Empty,
            toggleButton.HorizontalContentAlignment,
            toggleButton.VerticalContentAlignment);
        error = string.Empty;
        return true;
    }

    public static bool TryParseValues(
        Control control,
        DesignerToggleEditorInput input,
        out DesignerToggleValues values,
        out string error)
    {
        if (!TryRead(control, out var current, out error))
        {
            values = default!;
            return false;
        }

        if (!TryParseEnum(
                input.State,
                "State",
                out DesignerToggleState state,
                out error)
            || !TryParseEnum(
                input.ClickMode,
                "Click mode",
                out ClickMode clickMode,
                out error)
            || !TryParseEnum(
                input.HorizontalContentAlignment,
                "Horizontal content alignment",
                out HorizontalAlignment horizontalAlignment,
                out error)
            || !TryParseEnum(
                input.VerticalContentAlignment,
                "Vertical content alignment",
                out VerticalAlignment verticalAlignment,
                out error))
        {
            values = default!;
            return false;
        }

        if (state == DesignerToggleState.Indeterminate && !input.IsThreeState)
        {
            values = default!;
            error = "Indeterminate state requires three-state behavior.";
            return false;
        }

        values = current with
        {
            Content = input.Content,
            IsChecked = FromState(state),
            IsThreeState = input.IsThreeState,
            ClickMode = clickMode,
            GroupName = input.GroupName,
            OnContent = input.OnContent,
            OffContent = input.OffContent,
            HorizontalContentAlignment = horizontalAlignment,
            VerticalContentAlignment = verticalAlignment,
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
        if (!TryRead(control, out var current, out _))
        {
            return;
        }

        var input = new DesignerToggleEditorInput(
            Get(properties, "Content", current.Content),
            ToState(current.IsChecked).ToString(),
            GetBoolean(properties, "IsThreeState", current.IsThreeState),
            Get(properties, "ClickMode", current.ClickMode.ToString()),
            Get(properties, "GroupName", current.GroupName),
            Get(properties, "OnContent", current.OnContent),
            Get(properties, "OffContent", current.OffContent),
            Get(
                properties,
                "HorizontalContentAlignment",
                current.HorizontalContentAlignment.ToString()),
            Get(
                properties,
                "VerticalContentAlignment",
                current.VerticalContentAlignment.ToString()));

        if (TryGetValue(properties, "IsChecked", out var rawState)
            && TryParseNullableBoolean(rawState, out var checkedValue))
        {
            input = input with { State = ToState(checkedValue).ToString() };
        }

        if (TryParseValues(control, input, out var values, out _))
        {
            Apply(control, values);
        }
    }

    public static void Apply(Control control, DesignerToggleValues values)
    {
        if (control is not ToggleButton toggleButton || GetKind(toggleButton) != values.Kind)
        {
            return;
        }

        toggleButton.Content = values.Content;
        toggleButton.IsThreeState = values.IsThreeState;
        toggleButton.ClickMode = values.ClickMode;
        toggleButton.HorizontalContentAlignment = values.HorizontalContentAlignment;
        toggleButton.VerticalContentAlignment = values.VerticalContentAlignment;
        if (toggleButton is RadioButton radioButton)
        {
            radioButton.GroupName = values.GroupName;
        }

        if (toggleButton is ToggleSwitch toggleSwitch)
        {
            toggleSwitch.OnContent = values.OnContent;
            toggleSwitch.OffContent = values.OffContent;
        }

        toggleButton.IsChecked = values.IsChecked;
    }

    public static bool IsSupportedProperty(string tagName, string propertyName)
        => Array.Exists(
            GetPropertyNames(tagName),
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
            GetPropertyNames(tagName),
            candidate => string.Equals(
                candidate,
                propertyName.Trim(),
                StringComparison.OrdinalIgnoreCase))
            ?? string.Empty;
        normalizedValue = string.Empty;
        if (canonicalName.Length == 0)
        {
            error = $"{tagName}.{propertyName} is not a supported toggle property.";
            return false;
        }

        switch (canonicalName)
        {
            case "IsChecked":
                if (!TryParseNullableBoolean(rawValue, out var checkedValue))
                {
                    error = "IsChecked must be True, False, or {x:Null}.";
                    return false;
                }

                normalizedValue = FormatNullableBoolean(checkedValue);
                error = string.Empty;
                return true;
            case "IsThreeState":
                if (!bool.TryParse(rawValue.Trim(), out var boolean))
                {
                    error = "IsThreeState must be True or False.";
                    return false;
                }

                normalizedValue = boolean.ToString();
                error = string.Empty;
                return true;
            case "ClickMode":
                return TryNormalizeEnum<ClickMode>(
                    rawValue,
                    canonicalName,
                    out normalizedValue,
                    out error);
            case "HorizontalContentAlignment":
                return TryNormalizeEnum<HorizontalAlignment>(
                    rawValue,
                    canonicalName,
                    out normalizedValue,
                    out error);
            case "VerticalContentAlignment":
                return TryNormalizeEnum<VerticalAlignment>(
                    rawValue,
                    canonicalName,
                    out normalizedValue,
                    out error);
            default:
                normalizedValue = rawValue;
                error = string.Empty;
                return true;
        }
    }

    public static bool TryValidateProperties(
        string tagName,
        IReadOnlyDictionary<string, string> properties,
        out string error)
    {
        if (GetPropertyNames(tagName).Length == 0)
        {
            error = string.Empty;
            return true;
        }

        var isThreeState = GetBoolean(properties, "IsThreeState", false);
        if (TryGetValue(properties, "IsChecked", out var rawValue)
            && TryParseNullableBoolean(rawValue, out var checkedValue)
            && checkedValue is null
            && !isThreeState)
        {
            error = "IsChecked={x:Null} requires IsThreeState=True.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static void RemoveConstraintProperties(IDictionary<string, string> properties)
    {
        properties.Remove("IsChecked");
        properties.Remove("IsThreeState");
    }

    public static IReadOnlyList<DesignerToggleAttribute> GetAxamlAttributes(Control control)
    {
        if (!TryRead(control, out var values, out _))
        {
            return [];
        }

        var attributes = new List<DesignerToggleAttribute>
        {
            new("Content", values.Content),
            new("IsChecked", FormatNullableBoolean(values.IsChecked)),
            new("IsThreeState", values.IsThreeState.ToString()),
            new("ClickMode", values.ClickMode.ToString()),
            new(
                "HorizontalContentAlignment",
                values.HorizontalContentAlignment.ToString()),
            new(
                "VerticalContentAlignment",
                values.VerticalContentAlignment.ToString()),
        };
        if (values.Kind == DesignerToggleControlKind.RadioButton
            && values.GroupName.Length > 0)
        {
            attributes.Add(new("GroupName", values.GroupName));
        }

        if (values.Kind == DesignerToggleControlKind.ToggleSwitch)
        {
            attributes.Add(new("OnContent", values.OnContent));
            attributes.Add(new("OffContent", values.OffContent));
        }

        return attributes;
    }

    public static DesignerToggleState ToState(bool? value)
        => value switch
        {
            true => DesignerToggleState.Checked,
            false => DesignerToggleState.Unchecked,
            null => DesignerToggleState.Indeterminate,
        };

    private static bool? FromState(DesignerToggleState state)
        => state switch
        {
            DesignerToggleState.Checked => true,
            DesignerToggleState.Unchecked => false,
            _ => null,
        };

    private static DesignerToggleControlKind GetKind(ToggleButton toggleButton)
        => toggleButton switch
        {
            CheckBox => DesignerToggleControlKind.CheckBox,
            RadioButton => DesignerToggleControlKind.RadioButton,
            ToggleSwitch => DesignerToggleControlKind.ToggleSwitch,
            _ => DesignerToggleControlKind.ToggleButton,
        };

    private static string[] GetPropertyNames(string tagName)
        => tagName.Trim().ToUpperInvariant() switch
        {
            "CHECKBOX" or "TOGGLEBUTTON" => CommonProperties,
            "RADIOBUTTON" => RadioButtonProperties,
            "TOGGLESWITCH" => ToggleSwitchProperties,
            _ => [],
        };

    private static bool TryParseNullableBoolean(string rawValue, out bool? value)
    {
        var normalized = rawValue.Trim();
        if (string.Equals(normalized, NullExpression, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "Indeterminate", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "Null", StringComparison.OrdinalIgnoreCase))
        {
            value = null;
            return true;
        }

        if (bool.TryParse(normalized, out var boolean))
        {
            value = boolean;
            return true;
        }

        value = null;
        return false;
    }

    private static string FormatNullableBoolean(bool? value)
        => value?.ToString() ?? NullExpression;

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
        string propertyName,
        out string normalizedValue,
        out string error)
        where T : struct, Enum
    {
        if (!TryParseEnum(rawValue, propertyName, out T value, out error))
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
        => TryGetValue(properties, propertyName, out var value) ? value : fallback;

    private static bool GetBoolean(
        IReadOnlyDictionary<string, string> properties,
        string propertyName,
        bool fallback)
        => TryGetValue(properties, propertyName, out var rawValue)
            && bool.TryParse(rawValue, out var value)
                ? value
                : fallback;

    private static bool TryGetValue(
        IReadOnlyDictionary<string, string> properties,
        string propertyName,
        out string value)
    {
        foreach (var pair in properties)
        {
            if (string.Equals(pair.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }
}
