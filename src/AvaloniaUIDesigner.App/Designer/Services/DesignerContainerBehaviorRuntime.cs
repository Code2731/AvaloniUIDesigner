using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace AvaloniaUIDesigner.App.Designer.Services;

public enum DesignerContainerBehaviorKind
{
    Expander,
    ScrollViewer,
}

public sealed record DesignerContainerBehaviorValues(
    DesignerContainerBehaviorKind Kind,
    string Header,
    bool IsExpanded,
    ExpandDirection ExpandDirection,
    HorizontalAlignment HorizontalContentAlignment,
    VerticalAlignment VerticalContentAlignment,
    ScrollBarVisibility HorizontalScrollBarVisibility,
    ScrollBarVisibility VerticalScrollBarVisibility,
    bool AllowAutoHide,
    bool IsScrollChainingEnabled,
    bool IsDeferredScrollingEnabled,
    bool BringIntoViewOnFocusChange,
    SnapPointsType HorizontalSnapPointsType,
    SnapPointsType VerticalSnapPointsType,
    SnapPointsAlignment HorizontalSnapPointsAlignment,
    SnapPointsAlignment VerticalSnapPointsAlignment);

public sealed record DesignerContainerBehaviorEditorInput(
    string Header,
    bool IsExpanded,
    string ExpandDirection,
    string HorizontalContentAlignment,
    string VerticalContentAlignment,
    string HorizontalScrollBarVisibility,
    string VerticalScrollBarVisibility,
    bool AllowAutoHide,
    bool IsScrollChainingEnabled,
    bool IsDeferredScrollingEnabled,
    bool BringIntoViewOnFocusChange,
    string HorizontalSnapPointsType,
    string VerticalSnapPointsType,
    string HorizontalSnapPointsAlignment,
    string VerticalSnapPointsAlignment);

public sealed record DesignerContainerBehaviorAttribute(string Name, string Value);

public static class DesignerContainerBehaviorRuntime
{
    private static readonly string[] ExpanderProperties =
    [
        "Header",
        "IsExpanded",
        "ExpandDirection",
        "HorizontalContentAlignment",
        "VerticalContentAlignment",
    ];

    private static readonly string[] ScrollViewerProperties =
    [
        "HorizontalScrollBarVisibility",
        "VerticalScrollBarVisibility",
        "AllowAutoHide",
        "IsScrollChainingEnabled",
        "IsDeferredScrollingEnabled",
        "BringIntoViewOnFocusChange",
        "HorizontalSnapPointsType",
        "VerticalSnapPointsType",
        "HorizontalSnapPointsAlignment",
        "VerticalSnapPointsAlignment",
    ];

    public static IReadOnlyList<string> ExpandDirectionNames { get; } =
        Enum.GetNames<ExpandDirection>();

    public static IReadOnlyList<string> HorizontalAlignmentNames { get; } =
        Enum.GetNames<HorizontalAlignment>();

    public static IReadOnlyList<string> VerticalAlignmentNames { get; } =
        Enum.GetNames<VerticalAlignment>();

    public static IReadOnlyList<string> ScrollBarVisibilityNames { get; } =
        Enum.GetNames<ScrollBarVisibility>();

    public static IReadOnlyList<string> SnapPointsTypeNames { get; } =
        Enum.GetNames<SnapPointsType>();

    public static IReadOnlyList<string> SnapPointsAlignmentNames { get; } =
        Enum.GetNames<SnapPointsAlignment>();

    public static bool IsSupportedControl(Control control)
        => control is Expander or ScrollViewer;

    public static bool TryRead(
        Control control,
        out DesignerContainerBehaviorValues values,
        out string error)
    {
        switch (control)
        {
            case Expander expander:
                values = CreateDefaults(DesignerContainerBehaviorKind.Expander) with
                {
                    Header = expander.Header?.ToString() ?? string.Empty,
                    IsExpanded = expander.IsExpanded,
                    ExpandDirection = expander.ExpandDirection,
                    HorizontalContentAlignment = expander.HorizontalContentAlignment,
                    VerticalContentAlignment = expander.VerticalContentAlignment,
                };
                error = string.Empty;
                return true;
            case ScrollViewer scrollViewer:
                values = CreateDefaults(DesignerContainerBehaviorKind.ScrollViewer) with
                {
                    HorizontalScrollBarVisibility =
                        scrollViewer.HorizontalScrollBarVisibility,
                    VerticalScrollBarVisibility =
                        scrollViewer.VerticalScrollBarVisibility,
                    AllowAutoHide = scrollViewer.AllowAutoHide,
                    IsScrollChainingEnabled = scrollViewer.IsScrollChainingEnabled,
                    IsDeferredScrollingEnabled = scrollViewer.IsDeferredScrollingEnabled,
                    BringIntoViewOnFocusChange = scrollViewer.BringIntoViewOnFocusChange,
                    HorizontalSnapPointsType = scrollViewer.HorizontalSnapPointsType,
                    VerticalSnapPointsType = scrollViewer.VerticalSnapPointsType,
                    HorizontalSnapPointsAlignment =
                        scrollViewer.HorizontalSnapPointsAlignment,
                    VerticalSnapPointsAlignment =
                        scrollViewer.VerticalSnapPointsAlignment,
                };
                error = string.Empty;
                return true;
            default:
                values = default!;
                error = "Container behavior editing is available for Expander and ScrollViewer controls.";
                return false;
        }
    }

    public static bool TryParseValues(
        Control control,
        DesignerContainerBehaviorEditorInput input,
        out DesignerContainerBehaviorValues values,
        out string error)
    {
        if (!TryRead(control, out var current, out error))
        {
            values = default!;
            return false;
        }

        if (control is Expander)
        {
            if (!TryParseEnum(
                    input.ExpandDirection,
                    "Expand direction",
                    out ExpandDirection expandDirection,
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

            values = current with
            {
                Header = input.Header,
                IsExpanded = input.IsExpanded,
                ExpandDirection = expandDirection,
                HorizontalContentAlignment = horizontalAlignment,
                VerticalContentAlignment = verticalAlignment,
            };
            error = string.Empty;
            return true;
        }

        if (!TryParseEnum(
                input.HorizontalScrollBarVisibility,
                "Horizontal scrollbar visibility",
                out ScrollBarVisibility horizontalVisibility,
                out error)
            || !TryParseEnum(
                input.VerticalScrollBarVisibility,
                "Vertical scrollbar visibility",
                out ScrollBarVisibility verticalVisibility,
                out error)
            || !TryParseEnum(
                input.HorizontalSnapPointsType,
                "Horizontal snap points type",
                out SnapPointsType horizontalSnapType,
                out error)
            || !TryParseEnum(
                input.VerticalSnapPointsType,
                "Vertical snap points type",
                out SnapPointsType verticalSnapType,
                out error)
            || !TryParseEnum(
                input.HorizontalSnapPointsAlignment,
                "Horizontal snap points alignment",
                out SnapPointsAlignment horizontalSnapAlignment,
                out error)
            || !TryParseEnum(
                input.VerticalSnapPointsAlignment,
                "Vertical snap points alignment",
                out SnapPointsAlignment verticalSnapAlignment,
                out error))
        {
            values = default!;
            return false;
        }

        values = current with
        {
            HorizontalScrollBarVisibility = horizontalVisibility,
            VerticalScrollBarVisibility = verticalVisibility,
            AllowAutoHide = input.AllowAutoHide,
            IsScrollChainingEnabled = input.IsScrollChainingEnabled,
            IsDeferredScrollingEnabled = input.IsDeferredScrollingEnabled,
            BringIntoViewOnFocusChange = input.BringIntoViewOnFocusChange,
            HorizontalSnapPointsType = horizontalSnapType,
            VerticalSnapPointsType = verticalSnapType,
            HorizontalSnapPointsAlignment = horizontalSnapAlignment,
            VerticalSnapPointsAlignment = verticalSnapAlignment,
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

        var input = new DesignerContainerBehaviorEditorInput(
            Get(properties, "Header", current.Header),
            GetBoolean(properties, "IsExpanded", current.IsExpanded),
            Get(properties, "ExpandDirection", current.ExpandDirection.ToString()),
            Get(
                properties,
                "HorizontalContentAlignment",
                current.HorizontalContentAlignment.ToString()),
            Get(
                properties,
                "VerticalContentAlignment",
                current.VerticalContentAlignment.ToString()),
            Get(
                properties,
                "HorizontalScrollBarVisibility",
                current.HorizontalScrollBarVisibility.ToString()),
            Get(
                properties,
                "VerticalScrollBarVisibility",
                current.VerticalScrollBarVisibility.ToString()),
            GetBoolean(properties, "AllowAutoHide", current.AllowAutoHide),
            GetBoolean(
                properties,
                "IsScrollChainingEnabled",
                current.IsScrollChainingEnabled),
            GetBoolean(
                properties,
                "IsDeferredScrollingEnabled",
                current.IsDeferredScrollingEnabled),
            GetBoolean(
                properties,
                "BringIntoViewOnFocusChange",
                current.BringIntoViewOnFocusChange),
            Get(
                properties,
                "HorizontalSnapPointsType",
                current.HorizontalSnapPointsType.ToString()),
            Get(
                properties,
                "VerticalSnapPointsType",
                current.VerticalSnapPointsType.ToString()),
            Get(
                properties,
                "HorizontalSnapPointsAlignment",
                current.HorizontalSnapPointsAlignment.ToString()),
            Get(
                properties,
                "VerticalSnapPointsAlignment",
                current.VerticalSnapPointsAlignment.ToString()));

        if (TryParseValues(control, input, out var values, out _))
        {
            Apply(control, values);
        }
    }

    public static void Apply(Control control, DesignerContainerBehaviorValues values)
    {
        switch (control)
        {
            case Expander expander
                when values.Kind == DesignerContainerBehaviorKind.Expander:
                expander.Header = values.Header;
                expander.IsExpanded = values.IsExpanded;
                expander.ExpandDirection = values.ExpandDirection;
                expander.HorizontalContentAlignment =
                    values.HorizontalContentAlignment;
                expander.VerticalContentAlignment =
                    values.VerticalContentAlignment;
                break;
            case ScrollViewer scrollViewer
                when values.Kind == DesignerContainerBehaviorKind.ScrollViewer:
                scrollViewer.HorizontalScrollBarVisibility =
                    values.HorizontalScrollBarVisibility;
                scrollViewer.VerticalScrollBarVisibility =
                    values.VerticalScrollBarVisibility;
                scrollViewer.AllowAutoHide = values.AllowAutoHide;
                scrollViewer.IsScrollChainingEnabled =
                    values.IsScrollChainingEnabled;
                scrollViewer.IsDeferredScrollingEnabled =
                    values.IsDeferredScrollingEnabled;
                scrollViewer.BringIntoViewOnFocusChange =
                    values.BringIntoViewOnFocusChange;
                scrollViewer.HorizontalSnapPointsType =
                    values.HorizontalSnapPointsType;
                scrollViewer.VerticalSnapPointsType =
                    values.VerticalSnapPointsType;
                scrollViewer.HorizontalSnapPointsAlignment =
                    values.HorizontalSnapPointsAlignment;
                scrollViewer.VerticalSnapPointsAlignment =
                    values.VerticalSnapPointsAlignment;
                break;
        }
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
            error = $"{tagName}.{propertyName} is not a supported container behavior property.";
            return false;
        }

        switch (canonicalName)
        {
            case "IsExpanded":
            case "AllowAutoHide":
            case "IsScrollChainingEnabled":
            case "IsDeferredScrollingEnabled":
            case "BringIntoViewOnFocusChange":
                if (!bool.TryParse(rawValue.Trim(), out var boolean))
                {
                    error = $"{canonicalName} must be True or False.";
                    return false;
                }

                normalizedValue = boolean.ToString();
                error = string.Empty;
                return true;
            case "ExpandDirection":
                return TryNormalizeEnum<ExpandDirection>(
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
            case "HorizontalScrollBarVisibility":
            case "VerticalScrollBarVisibility":
                return TryNormalizeEnum<ScrollBarVisibility>(
                    rawValue,
                    canonicalName,
                    out normalizedValue,
                    out error);
            case "HorizontalSnapPointsType":
            case "VerticalSnapPointsType":
                return TryNormalizeEnum<SnapPointsType>(
                    rawValue,
                    canonicalName,
                    out normalizedValue,
                    out error);
            case "HorizontalSnapPointsAlignment":
            case "VerticalSnapPointsAlignment":
                return TryNormalizeEnum<SnapPointsAlignment>(
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

    public static IReadOnlyList<DesignerContainerBehaviorAttribute> GetAxamlAttributes(
        Control control)
    {
        if (!TryRead(control, out var values, out _))
        {
            return [];
        }

        if (values.Kind == DesignerContainerBehaviorKind.Expander)
        {
            return
            [
                new("Header", values.Header),
                new("IsExpanded", values.IsExpanded.ToString()),
                new("ExpandDirection", values.ExpandDirection.ToString()),
                new(
                    "HorizontalContentAlignment",
                    values.HorizontalContentAlignment.ToString()),
                new(
                    "VerticalContentAlignment",
                    values.VerticalContentAlignment.ToString()),
            ];
        }

        return
        [
            new(
                "HorizontalScrollBarVisibility",
                values.HorizontalScrollBarVisibility.ToString()),
            new(
                "VerticalScrollBarVisibility",
                values.VerticalScrollBarVisibility.ToString()),
            new("AllowAutoHide", values.AllowAutoHide.ToString()),
            new(
                "IsScrollChainingEnabled",
                values.IsScrollChainingEnabled.ToString()),
            new(
                "IsDeferredScrollingEnabled",
                values.IsDeferredScrollingEnabled.ToString()),
            new(
                "BringIntoViewOnFocusChange",
                values.BringIntoViewOnFocusChange.ToString()),
            new(
                "HorizontalSnapPointsType",
                values.HorizontalSnapPointsType.ToString()),
            new(
                "VerticalSnapPointsType",
                values.VerticalSnapPointsType.ToString()),
            new(
                "HorizontalSnapPointsAlignment",
                values.HorizontalSnapPointsAlignment.ToString()),
            new(
                "VerticalSnapPointsAlignment",
                values.VerticalSnapPointsAlignment.ToString()),
        ];
    }

    private static DesignerContainerBehaviorValues CreateDefaults(
        DesignerContainerBehaviorKind kind)
        => new(
            kind,
            Header: string.Empty,
            IsExpanded: false,
            ExpandDirection: ExpandDirection.Down,
            HorizontalContentAlignment: HorizontalAlignment.Stretch,
            VerticalContentAlignment: VerticalAlignment.Stretch,
            HorizontalScrollBarVisibility: ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility: ScrollBarVisibility.Auto,
            AllowAutoHide: true,
            IsScrollChainingEnabled: true,
            IsDeferredScrollingEnabled: false,
            BringIntoViewOnFocusChange: true,
            HorizontalSnapPointsType: SnapPointsType.None,
            VerticalSnapPointsType: SnapPointsType.None,
            HorizontalSnapPointsAlignment: SnapPointsAlignment.Near,
            VerticalSnapPointsAlignment: SnapPointsAlignment.Near);

    private static string[] GetPropertyNames(string tagName)
        => tagName.Trim().ToUpperInvariant() switch
        {
            "EXPANDER" => ExpanderProperties,
            "SCROLLVIEWER" => ScrollViewerProperties,
            _ => [],
        };

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
