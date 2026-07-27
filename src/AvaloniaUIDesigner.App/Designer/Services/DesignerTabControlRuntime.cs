using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerTabControlValues(
    Dock TabStripPlacement,
    HorizontalAlignment HorizontalContentAlignment,
    VerticalAlignment VerticalContentAlignment);

public sealed record DesignerTabControlEditorInput(
    string TabStripPlacement,
    string HorizontalContentAlignment,
    string VerticalContentAlignment);

public sealed record DesignerTabControlAttribute(string Name, string Value);

public static class DesignerTabControlRuntime
{
    private static readonly string[] PropertyNames =
    [
        "TabStripPlacement",
        "HorizontalContentAlignment",
        "VerticalContentAlignment",
    ];

    public static IReadOnlyList<string> TabStripPlacementNames { get; } =
        Enum.GetNames<Dock>();

    public static IReadOnlyList<string> HorizontalAlignmentNames { get; } =
        Enum.GetNames<HorizontalAlignment>();

    public static IReadOnlyList<string> VerticalAlignmentNames { get; } =
        Enum.GetNames<VerticalAlignment>();

    public static bool IsSupportedControl(Control control)
        => control is TabControl;

    public static bool TryRead(
        Control control,
        out DesignerTabControlValues values,
        out string error)
    {
        if (control is not TabControl tabControl)
        {
            values = default!;
            error = "TabControl behavior editing is available for TabControl controls.";
            return false;
        }

        values = new DesignerTabControlValues(
            tabControl.TabStripPlacement,
            tabControl.HorizontalContentAlignment,
            tabControl.VerticalContentAlignment);
        error = string.Empty;
        return true;
    }

    public static bool TryParseValues(
        Control control,
        DesignerTabControlEditorInput input,
        out DesignerTabControlValues values,
        out string error)
    {
        if (!TryRead(control, out var current, out error))
        {
            values = default!;
            return false;
        }

        if (!TryParseEnum(
                input.TabStripPlacement,
                "Tab strip placement",
                out Dock tabStripPlacement,
                out error)
            || !TryParseEnum(
                input.HorizontalContentAlignment,
                "Horizontal content alignment",
                out HorizontalAlignment horizontalContentAlignment,
                out error)
            || !TryParseEnum(
                input.VerticalContentAlignment,
                "Vertical content alignment",
                out VerticalAlignment verticalContentAlignment,
                out error))
        {
            values = default!;
            return false;
        }

        values = current with
        {
            TabStripPlacement = tabStripPlacement,
            HorizontalContentAlignment = horizontalContentAlignment,
            VerticalContentAlignment = verticalContentAlignment,
        };
        error = string.Empty;
        return true;
    }

    public static void Capture(
        Control control,
        IDictionary<string, string> properties)
    {
        foreach (var attribute in GetAxamlAttributes(control))
        {
            properties[attribute.Name] = attribute.Value;
        }
    }

    public static void Apply(
        TabControl tabControl,
        DesignerTabControlValues values)
    {
        tabControl.TabStripPlacement = values.TabStripPlacement;
        tabControl.HorizontalContentAlignment = values.HorizontalContentAlignment;
        tabControl.VerticalContentAlignment = values.VerticalContentAlignment;
    }

    public static void Apply(
        TabControl tabControl,
        IReadOnlyDictionary<string, string> properties)
    {
        if (!TryRead(tabControl, out var current, out _))
        {
            return;
        }

        var input = new DesignerTabControlEditorInput(
            Get(properties, "TabStripPlacement", current.TabStripPlacement.ToString()),
            Get(
                properties,
                "HorizontalContentAlignment",
                current.HorizontalContentAlignment.ToString()),
            Get(
                properties,
                "VerticalContentAlignment",
                current.VerticalContentAlignment.ToString()));

        if (TryParseValues(tabControl, input, out var values, out _))
        {
            Apply(tabControl, values);
        }
    }

    public static bool IsSupportedProperty(string tagName, string propertyName)
        => string.Equals(tagName, "TabControl", StringComparison.OrdinalIgnoreCase)
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
            error = $"{tagName}.{propertyName} is not a supported TabControl behavior property.";
            return false;
        }

        switch (canonicalName)
        {
            case "TabStripPlacement":
                if (!TryParseEnum(rawValue, "Tab strip placement", out Dock placement, out error))
                {
                    return false;
                }

                normalizedValue = placement.ToString();
                error = string.Empty;
                return true;
            case "HorizontalContentAlignment":
                if (!TryParseEnum(
                        rawValue,
                        "Horizontal content alignment",
                        out HorizontalAlignment horizontalAlignment,
                        out error))
                {
                    return false;
                }

                normalizedValue = horizontalAlignment.ToString();
                error = string.Empty;
                return true;
            case "VerticalContentAlignment":
                if (!TryParseEnum(
                        rawValue,
                        "Vertical content alignment",
                        out VerticalAlignment verticalAlignment,
                        out error))
                {
                    return false;
                }

                normalizedValue = verticalAlignment.ToString();
                error = string.Empty;
                return true;
            default:
                error = $"{canonicalName} is not a supported TabControl behavior property.";
                return false;
        }
    }

    public static bool TryValidateProperties(
        string tagName,
        IReadOnlyDictionary<string, string> properties,
        out string error)
    {
        if (!string.Equals(tagName, "TabControl", StringComparison.OrdinalIgnoreCase))
        {
            error = string.Empty;
            return true;
        }

        foreach (var propertyName in PropertyNames)
        {
            if (TryGetValue(properties, propertyName, out var value)
                && !TryNormalizeProperty(
                    tagName,
                    propertyName,
                    value,
                    out _,
                    out _,
                    out error))
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
        if (!string.Equals(tagName, "TabControl", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var propertyName in PropertyNames)
        {
            properties.Remove(propertyName);
        }
    }

    public static IReadOnlyList<DesignerTabControlAttribute> GetAxamlAttributes(Control control)
    {
        if (!TryRead(control, out var values, out _))
        {
            return [];
        }

        return
        [
            new("TabStripPlacement", values.TabStripPlacement.ToString()),
            new("HorizontalContentAlignment", values.HorizontalContentAlignment.ToString()),
            new("VerticalContentAlignment", values.VerticalContentAlignment.ToString()),
        ];
    }

    private static bool TryParseEnum<T>(
        string rawValue,
        string displayName,
        out T value,
        out string error)
        where T : struct, Enum
    {
        if (!Enum.TryParse(rawValue.Trim(), true, out value))
        {
            error = $"{displayName} must be one of {string.Join(", ", Enum.GetNames<T>())}.";
            return false;
        }

        error = string.Empty;
        return true;
    }

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
