using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerInteractionValues(
    double Opacity,
    bool IsEnabled,
    bool IsVisible,
    bool IsHitTestVisible,
    bool ClipToBounds,
    bool UseLayoutRounding,
    FlowDirection FlowDirection,
    string Cursor);

public sealed record DesignerInteractionAttribute(string Name, string Value);

public static class DesignerInteractionRuntime
{
    private const string IsEnabledKey = "__isEnabled";
    private const string IsVisibleKey = "__isVisible";
    private const string IsHitTestVisibleKey = "__isHitTestVisible";
    private const string ClipToBoundsKey = "__clipToBounds";
    private const string UseLayoutRoundingKey = "__useLayoutRounding";
    private const string FlowDirectionKey = "__flowDirection";
    private const string CursorKey = "__cursor";

    public static IReadOnlyList<string> CursorNames { get; } =
        [
            "Default",
            .. Enum.GetNames<StandardCursorType>(),
        ];

    public static bool TryRead(
        Control control,
        out DesignerInteractionValues values,
        out string error)
    {
        var cursorName = control.Cursor?.ToString() ?? "Default";
        if (!string.Equals(cursorName, "Default", StringComparison.Ordinal)
            && (!Enum.TryParse<StandardCursorType>(cursorName, true, out var cursorType)
                || !Enum.IsDefined(cursorType)))
        {
            values = default!;
            error = $"The existing cursor '{cursorName}' is not a standard Avalonia cursor.";
            return false;
        }

        values = new DesignerInteractionValues(
            control.Opacity,
            control.IsEnabled,
            control.IsVisible,
            control.IsHitTestVisible,
            control.ClipToBounds,
            control.UseLayoutRounding,
            control.FlowDirection,
            string.Equals(cursorName, "Default", StringComparison.OrdinalIgnoreCase)
                ? "Default"
                : Enum.Parse<StandardCursorType>(cursorName, true).ToString());
        error = string.Empty;
        return true;
    }

    public static void Capture(Control control, IDictionary<string, string> properties)
    {
        properties["Opacity"] = Format(control.Opacity);
        if (!control.IsEnabled)
        {
            properties[IsEnabledKey] = bool.FalseString;
        }

        if (!control.IsVisible)
        {
            properties[IsVisibleKey] = bool.FalseString;
        }

        if (!control.IsHitTestVisible)
        {
            properties[IsHitTestVisibleKey] = bool.FalseString;
        }

        if (control.IsSet(Visual.ClipToBoundsProperty))
        {
            properties[ClipToBoundsKey] = control.ClipToBounds.ToString();
        }

        if (!control.UseLayoutRounding)
        {
            properties[UseLayoutRoundingKey] = bool.FalseString;
        }

        if (control.FlowDirection != FlowDirection.LeftToRight)
        {
            properties[FlowDirectionKey] = control.FlowDirection.ToString();
        }

        var cursorName = control.Cursor?.ToString();
        if (cursorName is not null
            && Enum.TryParse<StandardCursorType>(cursorName, true, out var cursorType)
            && Enum.IsDefined(cursorType))
        {
            properties[CursorKey] = cursorType.ToString();
        }
    }

    public static void Apply(Control control, IReadOnlyDictionary<string, string> properties)
    {
        if (TryGetValue(properties, "Opacity", out var opacity)
            && double.TryParse(
                opacity,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsedOpacity)
            && double.IsFinite(parsedOpacity))
        {
            control.Opacity = Math.Clamp(parsedOpacity, 0, 1);
        }

        ApplyBoolean(control, properties, IsEnabledKey, InputElement.IsEnabledProperty);
        ApplyBoolean(control, properties, IsVisibleKey, Visual.IsVisibleProperty);
        ApplyBoolean(control, properties, IsHitTestVisibleKey, InputElement.IsHitTestVisibleProperty);
        ApplyBoolean(control, properties, ClipToBoundsKey, Visual.ClipToBoundsProperty);
        ApplyBoolean(control, properties, UseLayoutRoundingKey, Layoutable.UseLayoutRoundingProperty);

        if (TryGetValue(properties, FlowDirectionKey, out var flowDirection)
            && Enum.TryParse<FlowDirection>(flowDirection, true, out var parsedFlowDirection)
            && Enum.IsDefined(parsedFlowDirection))
        {
            control.FlowDirection = parsedFlowDirection;
        }

        if (TryGetValue(properties, CursorKey, out var cursor)
            && TryParseCursor(cursor, out var normalizedCursor, out _))
        {
            ApplyCursor(control, normalizedCursor);
        }
    }

    public static void Apply(Control control, DesignerInteractionValues values)
    {
        control.Opacity = values.Opacity;
        control.IsEnabled = values.IsEnabled;
        control.IsVisible = values.IsVisible;
        control.IsHitTestVisible = values.IsHitTestVisible;
        control.ClipToBounds = values.ClipToBounds;
        control.UseLayoutRounding = values.UseLayoutRounding;
        control.FlowDirection = values.FlowDirection;
        ApplyCursor(control, values.Cursor);
    }

    public static bool TryParseValues(
        string opacity,
        bool isEnabled,
        bool isVisible,
        bool isHitTestVisible,
        bool clipToBounds,
        bool useLayoutRounding,
        string flowDirection,
        string cursor,
        out DesignerInteractionValues values,
        out string error)
    {
        values = default!;
        if (!double.TryParse(
                opacity.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsedOpacity)
            || !double.IsFinite(parsedOpacity)
            || parsedOpacity is < 0 or > 1)
        {
            error = "Opacity must be a finite number from 0 to 1.";
            return false;
        }

        if (!Enum.TryParse<FlowDirection>(
                flowDirection.Trim(),
                true,
                out var parsedFlowDirection)
            || !Enum.IsDefined(parsedFlowDirection))
        {
            error = $"Flow direction must be one of: {string.Join(", ", Enum.GetNames<FlowDirection>())}.";
            return false;
        }

        if (!TryParseCursor(cursor, out var normalizedCursor, out error))
        {
            return false;
        }

        values = new DesignerInteractionValues(
            parsedOpacity,
            isEnabled,
            isVisible,
            isHitTestVisible,
            clipToBounds,
            useLayoutRounding,
            parsedFlowDirection,
            normalizedCursor);
        error = string.Empty;
        return true;
    }

    public static bool IsSupportedAxamlProperty(string propertyName)
        => GetInternalKey(propertyName).Length > 0;

    public static bool TryNormalizeAxamlProperty(
        string propertyName,
        string rawValue,
        out string internalKey,
        out string normalizedValue,
        out string error)
    {
        internalKey = GetInternalKey(propertyName);
        normalizedValue = string.Empty;
        error = string.Empty;
        if (internalKey.Length == 0)
        {
            error = $"{propertyName} is not a supported interaction or rendering property.";
            return false;
        }

        switch (internalKey)
        {
            case "Opacity":
                if (!double.TryParse(
                        rawValue.Trim(),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var opacity)
                    || !double.IsFinite(opacity)
                    || opacity is < 0 or > 1)
                {
                    error = "Opacity must be a finite number from 0 to 1.";
                    return false;
                }

                normalizedValue = Format(opacity);
                return true;
            case IsEnabledKey:
            case IsVisibleKey:
            case IsHitTestVisibleKey:
            case ClipToBoundsKey:
            case UseLayoutRoundingKey:
                if (!bool.TryParse(rawValue.Trim(), out var boolean))
                {
                    error = $"{propertyName} must be True or False.";
                    return false;
                }

                normalizedValue = boolean.ToString();
                return true;
            case FlowDirectionKey:
                if (!Enum.TryParse<FlowDirection>(
                        rawValue.Trim(),
                        true,
                        out var flowDirection)
                    || !Enum.IsDefined(flowDirection))
                {
                    error = $"FlowDirection must be one of: {string.Join(", ", Enum.GetNames<FlowDirection>())}.";
                    return false;
                }

                normalizedValue = flowDirection.ToString();
                return true;
            case CursorKey:
                return TryParseCursor(rawValue, out normalizedValue, out error);
            default:
                return false;
        }
    }

    public static IReadOnlyList<DesignerInteractionAttribute> GetAxamlAttributes(
        IReadOnlyDictionary<string, string> properties)
    {
        var attributes = new List<DesignerInteractionAttribute>();
        AddAttribute(attributes, properties, IsEnabledKey, "IsEnabled");
        AddAttribute(attributes, properties, IsVisibleKey, "IsVisible");
        AddAttribute(attributes, properties, IsHitTestVisibleKey, "IsHitTestVisible");
        AddAttribute(attributes, properties, ClipToBoundsKey, "ClipToBounds");
        AddAttribute(attributes, properties, UseLayoutRoundingKey, "UseLayoutRounding");
        AddAttribute(attributes, properties, FlowDirectionKey, "FlowDirection");
        AddAttribute(attributes, properties, CursorKey, "Cursor");
        return attributes;
    }

    private static string GetInternalKey(string propertyName)
        => propertyName.Trim().ToLowerInvariant() switch
        {
            "opacity" => "Opacity",
            "isenabled" => IsEnabledKey,
            "isvisible" => IsVisibleKey,
            "ishittestvisible" => IsHitTestVisibleKey,
            "cliptobounds" => ClipToBoundsKey,
            "uselayoutrounding" => UseLayoutRoundingKey,
            "flowdirection" => FlowDirectionKey,
            "cursor" => CursorKey,
            _ => string.Empty,
        };

    private static bool TryParseCursor(
        string rawValue,
        out string normalizedCursor,
        out string error)
    {
        var candidate = rawValue.Trim();
        if (string.Equals(candidate, "Default", StringComparison.OrdinalIgnoreCase)
            || candidate.Length == 0)
        {
            normalizedCursor = "Default";
            error = string.Empty;
            return true;
        }

        if (!Enum.TryParse<StandardCursorType>(candidate, true, out var cursorType)
            || !Enum.IsDefined(cursorType))
        {
            normalizedCursor = string.Empty;
            error = $"Cursor must be one of: {string.Join(", ", CursorNames)}.";
            return false;
        }

        normalizedCursor = cursorType.ToString();
        error = string.Empty;
        return true;
    }

    private static void ApplyCursor(Control control, string cursor)
    {
        if (string.Equals(cursor, "Default", StringComparison.Ordinal))
        {
            control.ClearValue(InputElement.CursorProperty);
            return;
        }

        control.Cursor = new Cursor(Enum.Parse<StandardCursorType>(cursor));
    }

    private static void ApplyBoolean(
        Control control,
        IReadOnlyDictionary<string, string> properties,
        string key,
        AvaloniaProperty property)
    {
        if (TryGetValue(properties, key, out var value)
            && bool.TryParse(value, out var parsedValue))
        {
            control.SetValue(property, parsedValue);
        }
    }

    private static void AddAttribute(
        ICollection<DesignerInteractionAttribute> attributes,
        IReadOnlyDictionary<string, string> properties,
        string key,
        string name)
    {
        if (TryGetValue(properties, key, out var value))
        {
            attributes.Add(new DesignerInteractionAttribute(name, value));
        }
    }

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

    private static string Format(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);
}
