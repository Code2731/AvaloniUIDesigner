using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerGridSplitterValues(
    GridResizeDirection ResizeDirection,
    GridResizeBehavior ResizeBehavior,
    bool ShowsPreview,
    double KeyboardIncrement,
    double DragIncrement);

public sealed record DesignerGridSplitterEditorInput(
    string ResizeDirection,
    string ResizeBehavior,
    bool ShowsPreview,
    string KeyboardIncrement,
    string DragIncrement);

public sealed record DesignerGridSplitterAttribute(string Name, string Value);

public static class DesignerGridSplitterRuntime
{
    private static readonly string[] PropertyNames =
    [
        "ResizeDirection",
        "ResizeBehavior",
        "ShowsPreview",
        "KeyboardIncrement",
        "DragIncrement",
    ];

    public static IReadOnlyList<string> ResizeDirectionNames { get; } =
        Enum.GetNames<GridResizeDirection>();

    public static IReadOnlyList<string> ResizeBehaviorNames { get; } =
        Enum.GetNames<GridResizeBehavior>();

    public static bool IsSupportedControl(Control control)
        => control is GridSplitter;

    public static bool TryRead(
        Control control,
        out DesignerGridSplitterValues values,
        out string error)
    {
        if (control is not GridSplitter splitter)
        {
            values = default!;
            error = "GridSplitter behavior editing is available for GridSplitter controls.";
            return false;
        }

        values = new DesignerGridSplitterValues(
            splitter.ResizeDirection,
            splitter.ResizeBehavior,
            splitter.ShowsPreview,
            splitter.KeyboardIncrement,
            splitter.DragIncrement);
        error = string.Empty;
        return true;
    }

    public static bool TryParseValues(
        Control control,
        DesignerGridSplitterEditorInput input,
        out DesignerGridSplitterValues values,
        out string error)
    {
        if (!TryRead(control, out var current, out error))
        {
            values = default!;
            return false;
        }

        if (!TryParseEnum(
                input.ResizeDirection,
                "Resize direction",
                out GridResizeDirection resizeDirection,
                out error)
            || !TryParseEnum(
                input.ResizeBehavior,
                "Resize behavior",
                out GridResizeBehavior resizeBehavior,
                out error)
            || !TryParseNonNegative(
                input.KeyboardIncrement,
                "Keyboard increment",
                out var keyboardIncrement,
                out error)
            || !TryParseNonNegative(
                input.DragIncrement,
                "Drag increment",
                out var dragIncrement,
                out error))
        {
            values = default!;
            return false;
        }

        values = current with
        {
            ResizeDirection = resizeDirection,
            ResizeBehavior = resizeBehavior,
            ShowsPreview = input.ShowsPreview,
            KeyboardIncrement = keyboardIncrement,
            DragIncrement = dragIncrement,
        };
        error = string.Empty;
        return true;
    }

    public static void Apply(
        Control control,
        IReadOnlyDictionary<string, string> properties)
    {
        if (!TryRead(control, out var current, out _))
        {
            return;
        }

        var input = new DesignerGridSplitterEditorInput(
            Get(properties, "ResizeDirection", current.ResizeDirection.ToString()),
            Get(properties, "ResizeBehavior", current.ResizeBehavior.ToString()),
            GetBoolean(properties, "ShowsPreview", current.ShowsPreview),
            Get(properties, "KeyboardIncrement", Format(current.KeyboardIncrement)),
            Get(properties, "DragIncrement", Format(current.DragIncrement)));
        if (TryParseValues(control, input, out var values, out _))
        {
            Apply(control, values);
        }
    }

    public static void Apply(Control control, DesignerGridSplitterValues values)
    {
        if (control is not GridSplitter splitter)
        {
            return;
        }

        splitter.ResizeDirection = values.ResizeDirection;
        splitter.ResizeBehavior = values.ResizeBehavior;
        splitter.ShowsPreview = values.ShowsPreview;
        splitter.KeyboardIncrement = values.KeyboardIncrement;
        splitter.DragIncrement = values.DragIncrement;
    }

    public static IReadOnlyList<DesignerGridSplitterAttribute> GetAxamlAttributes(
        Control control)
    {
        if (!TryRead(control, out var values, out _))
        {
            return Array.Empty<DesignerGridSplitterAttribute>();
        }

        var attributes = new List<DesignerGridSplitterAttribute>
        {
            new("ResizeDirection", values.ResizeDirection.ToString()),
            new("ResizeBehavior", values.ResizeBehavior.ToString()),
            new("ShowsPreview", values.ShowsPreview.ToString()),
            new("KeyboardIncrement", Format(values.KeyboardIncrement)),
            new("DragIncrement", Format(values.DragIncrement)),
        };

        return attributes;
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

    public static bool IsSupportedProperty(string tagName, string propertyName)
        => string.Equals(tagName, "GridSplitter", StringComparison.OrdinalIgnoreCase)
            && Array.Exists(
                PropertyNames,
                candidate => string.Equals(
                    candidate,
                    propertyName.Trim(),
                    StringComparison.OrdinalIgnoreCase));

    public static bool TryNormalizeProperty(
        string tagName,
        string propertyName,
        string value,
        out string canonicalName,
        out string normalizedValue,
        out string error)
    {
        canonicalName = propertyName.Trim();
        normalizedValue = value.Trim();
        error = string.Empty;
        if (!IsSupportedProperty(tagName, propertyName))
        {
            error = $"{tagName}.{propertyName} is not a supported GridSplitter property.";
            return false;
        }

        canonicalName = Array.Find(
            PropertyNames,
            candidate => string.Equals(
                candidate,
                propertyName.Trim(),
                StringComparison.OrdinalIgnoreCase))!;
        switch (canonicalName)
        {
            case "ResizeDirection":
                if (!TryParseEnum(
                        value,
                        "Resize direction",
                        out GridResizeDirection resizeDirection,
                        out error))
                {
                    return false;
                }

                normalizedValue = resizeDirection.ToString();
                return true;
            case "ResizeBehavior":
                if (!TryParseEnum(
                        value,
                        "Resize behavior",
                        out GridResizeBehavior resizeBehavior,
                        out error))
                {
                    return false;
                }

                normalizedValue = resizeBehavior.ToString();
                return true;
            case "ShowsPreview":
                if (!bool.TryParse(value, out var showsPreview))
                {
                    error = "Shows preview must be True or False.";
                    return false;
                }

                normalizedValue = showsPreview.ToString();
                return true;
            case "KeyboardIncrement":
                if (!TryParseNonNegative(
                        value,
                        "Keyboard increment",
                        out var keyboardIncrement,
                        out error))
                {
                    return false;
                }

                normalizedValue = Format(keyboardIncrement);
                return true;
            case "DragIncrement":
                if (!TryParseNonNegative(
                        value,
                        "Drag increment",
                        out var dragIncrement,
                        out error))
                {
                    return false;
                }

                normalizedValue = Format(dragIncrement);
                return true;
            default:
                return true;
        }
    }

    public static bool TryValidateProperties(
        string tagName,
        IReadOnlyDictionary<string, string> properties,
        out string error)
    {
        foreach (var propertyName in PropertyNames)
        {
            if (!properties.TryGetValue(propertyName, out var value))
            {
                continue;
            }

            if (!TryNormalizeProperty(
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

    public static void RemoveProperties(IDictionary<string, string> properties)
    {
        foreach (var propertyName in PropertyNames)
        {
            properties.Remove(propertyName);
        }
    }

    private static bool TryParseEnum<T>(
        string value,
        string label,
        out T result,
        out string error)
        where T : struct, Enum
    {
        if (Enum.TryParse(value, ignoreCase: true, out result))
        {
            error = string.Empty;
            return true;
        }

        error = $"{label} '{value}' is not valid. Choose one of: {string.Join(", ", Enum.GetNames<T>())}.";
        return false;
    }

    private static bool TryParseNonNegative(
        string value,
        string label,
        out double result,
        out string error)
    {
        if (double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out result)
            && double.IsFinite(result)
            && result >= 0)
        {
            error = string.Empty;
            return true;
        }

        result = 0;
        error = $"{label} must be a finite non-negative number.";
        return false;
    }

    private static string Get(
        IReadOnlyDictionary<string, string> properties,
        string name,
        string fallback)
        => properties.TryGetValue(name, out var value) ? value : fallback;

    private static bool GetBoolean(
        IReadOnlyDictionary<string, string> properties,
        string name,
        bool fallback)
        => properties.TryGetValue(name, out var value)
            && bool.TryParse(value, out var parsed)
            ? parsed
            : fallback;

    private static string Format(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);
}
