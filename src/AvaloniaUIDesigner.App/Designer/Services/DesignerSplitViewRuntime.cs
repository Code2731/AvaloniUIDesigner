using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaUIDesigner.App.Models;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerSplitViewValues(
    SplitViewDisplayMode DisplayMode,
    bool IsPaneOpen,
    double OpenPaneLength,
    double CompactPaneLength,
    SplitViewPanePlacement PanePlacement,
    bool UseLightDismissOverlayMode,
    string PaneBackground);

public sealed record DesignerSplitViewEditorInput(
    string DisplayMode,
    bool IsPaneOpen,
    string OpenPaneLength,
    string CompactPaneLength,
    string PanePlacement,
    bool UseLightDismissOverlayMode,
    string PaneBackground);

public sealed record DesignerSplitViewAttribute(string Name, string Value);

public static class DesignerSplitViewRuntime
{
    private static readonly string[] PropertyNames =
    [
        "DisplayMode",
        "IsPaneOpen",
        "OpenPaneLength",
        "CompactPaneLength",
        "PanePlacement",
        "UseLightDismissOverlayMode",
        "PaneBackground",
    ];

    public static IReadOnlyList<string> DisplayModeNames { get; } =
        Enum.GetNames<SplitViewDisplayMode>();

    public static IReadOnlyList<string> PanePlacementNames { get; } =
        Enum.GetNames<SplitViewPanePlacement>();

    public static bool IsSupportedControl(Control control)
        => control is SplitView;

    public static bool TryRead(
        Control control,
        out DesignerSplitViewValues values,
        out string error)
    {
        if (control is not SplitView splitView)
        {
            values = default!;
            error = "SplitView behavior editing is available for SplitView controls.";
            return false;
        }

        if (!TryFormatBrush(
                splitView.PaneBackground,
                "PaneBackground",
                out var paneBackground,
                out error))
        {
            values = default!;
            return false;
        }

        values = new DesignerSplitViewValues(
            splitView.DisplayMode,
            splitView.IsPaneOpen,
            splitView.OpenPaneLength,
            splitView.CompactPaneLength,
            splitView.PanePlacement,
            splitView.UseLightDismissOverlayMode,
            paneBackground);
        error = string.Empty;
        return true;
    }

    public static bool TryParseValues(
        Control control,
        DesignerSplitViewEditorInput input,
        out DesignerSplitViewValues values,
        out string error)
    {
        if (!TryRead(control, out var current, out error))
        {
            values = default!;
            return false;
        }

        if (!Enum.TryParse(input.DisplayMode.Trim(), true, out SplitViewDisplayMode displayMode))
        {
            values = default!;
            error = "Display mode must be Inline, CompactInline, Overlay, or CompactOverlay.";
            return false;
        }

        if (!TryParseLength(input.OpenPaneLength, "OpenPaneLength", out var openPaneLength, out error)
            || !TryParseLength(
                input.CompactPaneLength,
                "CompactPaneLength",
                out var compactPaneLength,
                out error)
            || !Enum.TryParse(input.PanePlacement.Trim(), true, out SplitViewPanePlacement panePlacement))
        {
            if (error.Length == 0)
            {
                error = "Pane placement must be Left, Right, Top, or Bottom.";
            }

            values = default!;
            return false;
        }

        if (!TryNormalizeBrush(
                input.PaneBackground,
                "PaneBackground",
                out var paneBackground,
                out error))
        {
            values = default!;
            return false;
        }

        values = current with
        {
            DisplayMode = displayMode,
            IsPaneOpen = input.IsPaneOpen,
            OpenPaneLength = openPaneLength,
            CompactPaneLength = compactPaneLength,
            PanePlacement = panePlacement,
            UseLightDismissOverlayMode = input.UseLightDismissOverlayMode,
            PaneBackground = paneBackground,
        };
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
    }

    public static void Apply(
        SplitView splitView,
        DesignerSplitViewValues values)
    {
        splitView.DisplayMode = values.DisplayMode;
        splitView.IsPaneOpen = values.IsPaneOpen;
        splitView.OpenPaneLength = values.OpenPaneLength;
        splitView.CompactPaneLength = values.CompactPaneLength;
        splitView.PanePlacement = values.PanePlacement;
        splitView.UseLightDismissOverlayMode = values.UseLightDismissOverlayMode;
        splitView.PaneBackground = ParseBrushOrNull(values.PaneBackground);
    }

    public static void Apply(
        SplitView splitView,
        IReadOnlyDictionary<string, string> properties)
    {
        if (!TryRead(splitView, out var current, out _))
        {
            return;
        }

        var input = new DesignerSplitViewEditorInput(
            Get(properties, "DisplayMode", current.DisplayMode.ToString()),
            GetBoolean(properties, "IsPaneOpen", current.IsPaneOpen),
            Get(properties, "OpenPaneLength", FormatLength(current.OpenPaneLength)),
            Get(properties, "CompactPaneLength", FormatLength(current.CompactPaneLength)),
            Get(properties, "PanePlacement", current.PanePlacement.ToString()),
            GetBoolean(
                properties,
                "UseLightDismissOverlayMode",
                current.UseLightDismissOverlayMode),
            GetPaneBackgroundForApply(properties, current.PaneBackground));

        if (TryParseValues(splitView, input, out var values, out _))
        {
            Apply(splitView, values);
        }
    }

    public static bool IsSupportedProperty(string tagName, string propertyName)
        => string.Equals(tagName, "SplitView", StringComparison.OrdinalIgnoreCase)
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
            error = $"{tagName}.{propertyName} is not a supported SplitView property.";
            return false;
        }

        switch (canonicalName)
        {
            case "DisplayMode":
                if (!Enum.TryParse(rawValue.Trim(), true, out SplitViewDisplayMode displayMode))
                {
                    error = "DisplayMode must be Inline, CompactInline, Overlay, or CompactOverlay.";
                    return false;
                }

                normalizedValue = displayMode.ToString();
                error = string.Empty;
                return true;
            case "IsPaneOpen":
            case "UseLightDismissOverlayMode":
                if (!bool.TryParse(rawValue.Trim(), out var boolean))
                {
                    error = $"{canonicalName} must be True or False.";
                    return false;
                }

                normalizedValue = boolean.ToString();
                error = string.Empty;
                return true;
            case "OpenPaneLength":
            case "CompactPaneLength":
                if (!TryParseLength(rawValue, canonicalName, out var length, out error))
                {
                    return false;
                }

                normalizedValue = FormatLength(length);
                return true;
            case "PanePlacement":
                if (!Enum.TryParse(rawValue.Trim(), true, out SplitViewPanePlacement placement))
                {
                    error = "PanePlacement must be Left, Right, Top, or Bottom.";
                    return false;
                }

                normalizedValue = placement.ToString();
                error = string.Empty;
                return true;
            case "PaneBackground":
                if (DesignerResourceReferenceMetadata.TryParseExpression(rawValue, out _))
                {
                    normalizedValue = rawValue.Trim();
                    error = string.Empty;
                    return true;
                }

                return TryNormalizeBrush(rawValue, canonicalName, out normalizedValue, out error);
            default:
                error = $"{canonicalName} is not a supported SplitView property.";
                return false;
        }
    }

    public static bool TryValidateProperties(
        string tagName,
        IReadOnlyDictionary<string, string> properties,
        out string error)
    {
        if (!string.Equals(tagName, "SplitView", StringComparison.OrdinalIgnoreCase))
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
        if (!string.Equals(tagName, "SplitView", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var propertyName in PropertyNames)
        {
            properties.Remove(propertyName);
        }
    }

    public static IReadOnlyList<DesignerSplitViewAttribute> GetAxamlAttributes(Control control)
    {
        if (!TryRead(control, out var values, out _))
        {
            return [];
        }

        var attributes = new List<DesignerSplitViewAttribute>
        {
            new("DisplayMode", values.DisplayMode.ToString()),
            new("IsPaneOpen", values.IsPaneOpen.ToString()),
            new("OpenPaneLength", FormatLength(values.OpenPaneLength)),
            new("CompactPaneLength", FormatLength(values.CompactPaneLength)),
            new("PanePlacement", values.PanePlacement.ToString()),
            new("UseLightDismissOverlayMode", values.UseLightDismissOverlayMode.ToString()),
        };
        if (values.PaneBackground.Length > 0)
        {
            attributes.Add(new("PaneBackground", values.PaneBackground));
        }

        return attributes;
    }

    private static bool TryParseLength(
        string rawValue,
        string propertyName,
        out double value,
        out string error)
    {
        if (!double.TryParse(
                rawValue.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value)
            || !double.IsFinite(value)
            || value < 0)
        {
            value = 0;
            error = $"{propertyName} must be a finite non-negative number.";
            return false;
        }

        error = string.Empty;
        return true;
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
                error = $"{propertyName} must be a solid color such as #E2E8F0 or Transparent.";
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
            error = $"{propertyName} must be a valid color such as #E2E8F0 or Transparent.";
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

    private static string FormatLength(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

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

    private static string GetPaneBackgroundForApply(
        IReadOnlyDictionary<string, string> properties,
        string fallback)
    {
        var value = Get(properties, "PaneBackground", fallback);
        return DesignerResourceReferenceMetadata.TryParseExpression(value, out _)
            ? fallback
            : value;
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
}
