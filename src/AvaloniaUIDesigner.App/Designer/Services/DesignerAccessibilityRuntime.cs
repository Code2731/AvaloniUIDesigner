using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerAccessibilityValues(
    string ToolTip,
    string AccessibleName,
    string AutomationId,
    string HelpText,
    AccessibilityView AccessibilityView,
    int HeadingLevel,
    AutomationLiveSetting LiveSetting,
    bool IsRequiredForForm,
    int TabIndex,
    bool IsTabStop,
    bool Focusable);

public sealed record DesignerAccessibilityAttribute(string Name, string Value);

public static class DesignerAccessibilityRuntime
{
    private const string ToolTipKey = "__toolTip";
    private const string NameKey = "__automationName";
    private const string AutomationIdKey = "__automationId";
    private const string HelpTextKey = "__automationHelpText";
    private const string AccessibilityViewKey = "__accessibilityView";
    private const string HeadingLevelKey = "__automationHeadingLevel";
    private const string LiveSettingKey = "__automationLiveSetting";
    private const string IsRequiredForFormKey = "__isRequiredForForm";
    private const string TabIndexKey = "__tabIndex";
    private const string IsTabStopKey = "__isTabStop";
    private const string FocusableKey = "__focusable";

    public static DesignerAccessibilityValues Read(Control control)
        => new(
            ToolTip.GetTip(control)?.ToString() ?? string.Empty,
            AutomationProperties.GetName(control) ?? string.Empty,
            AutomationProperties.GetAutomationId(control) ?? string.Empty,
            AutomationProperties.GetHelpText(control) ?? string.Empty,
            AutomationProperties.GetAccessibilityView(control),
            AutomationProperties.GetHeadingLevel(control),
            AutomationProperties.GetLiveSetting(control),
            AutomationProperties.GetIsRequiredForForm(control),
            control.TabIndex,
            control.IsTabStop,
            control.Focusable);

    public static void Capture(Control control, IDictionary<string, string> properties)
    {
        var values = Read(control);
        AddText(properties, ToolTipKey, values.ToolTip);
        AddText(properties, NameKey, values.AccessibleName);
        AddText(properties, AutomationIdKey, values.AutomationId);
        AddText(properties, HelpTextKey, values.HelpText);
        if (values.AccessibilityView != AccessibilityView.Default)
        {
            properties[AccessibilityViewKey] = values.AccessibilityView.ToString();
        }

        if (values.HeadingLevel > 0)
        {
            properties[HeadingLevelKey] = values.HeadingLevel.ToString(CultureInfo.InvariantCulture);
        }

        if (values.LiveSetting != AutomationLiveSetting.Off)
        {
            properties[LiveSettingKey] = values.LiveSetting.ToString();
        }

        if (values.IsRequiredForForm)
        {
            properties[IsRequiredForFormKey] = bool.TrueString;
        }

        // These defaults vary by control type, so preserve their effective values.
        properties[TabIndexKey] = values.TabIndex.ToString(CultureInfo.InvariantCulture);
        properties[IsTabStopKey] = values.IsTabStop.ToString();
        if (control.IsSet(InputElement.FocusableProperty))
        {
            properties[FocusableKey] = values.Focusable.ToString();
        }
    }

    public static void Apply(Control control, IReadOnlyDictionary<string, string> properties)
    {
        if (TryGetValue(properties, ToolTipKey, out var toolTip))
        {
            ToolTip.SetTip(control, string.IsNullOrWhiteSpace(toolTip) ? null : toolTip);
        }

        ApplyText(
            control,
            properties,
            NameKey,
            AutomationProperties.NameProperty,
            AutomationProperties.SetName);
        ApplyText(
            control,
            properties,
            AutomationIdKey,
            AutomationProperties.AutomationIdProperty,
            AutomationProperties.SetAutomationId);
        ApplyText(
            control,
            properties,
            HelpTextKey,
            AutomationProperties.HelpTextProperty,
            AutomationProperties.SetHelpText);

        if (TryGetValue(properties, AccessibilityViewKey, out var view)
            && Enum.TryParse<AccessibilityView>(view, true, out var parsedView)
            && Enum.IsDefined(parsedView))
        {
            if (parsedView == AccessibilityView.Default)
            {
                control.ClearValue(AutomationProperties.AccessibilityViewProperty);
            }
            else
            {
                AutomationProperties.SetAccessibilityView(control, parsedView);
            }
        }

        if (TryGetValue(properties, HeadingLevelKey, out var headingLevel)
            && int.TryParse(
                headingLevel,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsedHeadingLevel)
            && parsedHeadingLevel is >= 0 and <= 9)
        {
            if (parsedHeadingLevel == 0)
            {
                control.ClearValue(AutomationProperties.HeadingLevelProperty);
            }
            else
            {
                AutomationProperties.SetHeadingLevel(control, parsedHeadingLevel);
            }
        }

        if (TryGetValue(properties, LiveSettingKey, out var liveSetting)
            && Enum.TryParse<AutomationLiveSetting>(liveSetting, true, out var parsedLiveSetting)
            && Enum.IsDefined(parsedLiveSetting))
        {
            if (parsedLiveSetting == AutomationLiveSetting.Off)
            {
                control.ClearValue(AutomationProperties.LiveSettingProperty);
            }
            else
            {
                AutomationProperties.SetLiveSetting(control, parsedLiveSetting);
            }
        }

        if (TryGetValue(properties, IsRequiredForFormKey, out var required)
            && bool.TryParse(required, out var parsedRequired))
        {
            if (parsedRequired)
            {
                AutomationProperties.SetIsRequiredForForm(control, true);
            }
            else
            {
                control.ClearValue(AutomationProperties.IsRequiredForFormProperty);
            }
        }

        if (TryGetValue(properties, TabIndexKey, out var tabIndex)
            && int.TryParse(
                tabIndex,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsedTabIndex))
        {
            control.TabIndex = parsedTabIndex;
        }

        if (TryGetValue(properties, IsTabStopKey, out var isTabStop)
            && bool.TryParse(isTabStop, out var parsedIsTabStop))
        {
            control.IsTabStop = parsedIsTabStop;
        }

        if (TryGetValue(properties, FocusableKey, out var focusable)
            && bool.TryParse(focusable, out var parsedFocusable))
        {
            control.Focusable = parsedFocusable;
        }
    }

    public static void Apply(Control control, DesignerAccessibilityValues values)
    {
        ToolTip.SetTip(control, string.IsNullOrEmpty(values.ToolTip) ? null : values.ToolTip);
        SetOrClearText(
            control,
            values.AccessibleName,
            AutomationProperties.NameProperty,
            AutomationProperties.SetName);
        SetOrClearText(
            control,
            values.AutomationId,
            AutomationProperties.AutomationIdProperty,
            AutomationProperties.SetAutomationId);
        SetOrClearText(
            control,
            values.HelpText,
            AutomationProperties.HelpTextProperty,
            AutomationProperties.SetHelpText);
        SetOrClear(
            control,
            values.AccessibilityView,
            AccessibilityView.Default,
            AutomationProperties.AccessibilityViewProperty,
            AutomationProperties.SetAccessibilityView);
        if (values.HeadingLevel == 0)
        {
            control.ClearValue(AutomationProperties.HeadingLevelProperty);
        }
        else
        {
            AutomationProperties.SetHeadingLevel(control, values.HeadingLevel);
        }

        SetOrClear(
            control,
            values.LiveSetting,
            AutomationLiveSetting.Off,
            AutomationProperties.LiveSettingProperty,
            AutomationProperties.SetLiveSetting);
        if (values.IsRequiredForForm)
        {
            AutomationProperties.SetIsRequiredForForm(control, true);
        }
        else
        {
            control.ClearValue(AutomationProperties.IsRequiredForFormProperty);
        }

        control.TabIndex = values.TabIndex;
        control.IsTabStop = values.IsTabStop;
        control.Focusable = values.Focusable;
    }

    public static bool TryParseValues(
        string toolTip,
        string accessibleName,
        string automationId,
        string helpText,
        string accessibilityView,
        string headingLevel,
        string liveSetting,
        bool isRequiredForForm,
        string tabIndex,
        bool isTabStop,
        bool focusable,
        out DesignerAccessibilityValues values,
        out string error)
    {
        values = default!;
        if (!TryNormalizeText(toolTip, 2048, "Tooltip", allowLineBreaks: true, out var normalizedToolTip, out error)
            || !TryNormalizeText(accessibleName, 512, "Accessible name", allowLineBreaks: false, out var normalizedName, out error)
            || !TryNormalizeText(automationId, 512, "Automation ID", allowLineBreaks: false, out var normalizedId, out error)
            || !TryNormalizeText(helpText, 2048, "Help text", allowLineBreaks: true, out var normalizedHelpText, out error))
        {
            return false;
        }

        if (!Enum.TryParse<AccessibilityView>(
                accessibilityView.Trim(),
                true,
                out var parsedView)
            || !Enum.IsDefined(parsedView))
        {
            error = $"Accessibility view must be one of: {string.Join(", ", Enum.GetNames<AccessibilityView>())}.";
            return false;
        }

        if (!int.TryParse(
                headingLevel.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsedHeadingLevel)
            || parsedHeadingLevel is < 0 or > 9)
        {
            error = "Heading level must be an integer from 0 to 9.";
            return false;
        }

        if (!Enum.TryParse<AutomationLiveSetting>(
                liveSetting.Trim(),
                true,
                out var parsedLiveSetting)
            || !Enum.IsDefined(parsedLiveSetting))
        {
            error = $"Live setting must be one of: {string.Join(", ", Enum.GetNames<AutomationLiveSetting>())}.";
            return false;
        }

        if (!int.TryParse(
                tabIndex.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsedTabIndex))
        {
            error = "Tab index must be a 32-bit integer.";
            return false;
        }

        values = new DesignerAccessibilityValues(
            normalizedToolTip,
            normalizedName,
            normalizedId,
            normalizedHelpText,
            parsedView,
            parsedHeadingLevel,
            parsedLiveSetting,
            isRequiredForForm,
            parsedTabIndex,
            isTabStop,
            focusable);
        error = string.Empty;
        return true;
    }

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
            error = $"{propertyName} is not a supported accessibility or navigation property.";
            return false;
        }

        switch (internalKey)
        {
            case ToolTipKey:
                return TryNormalizeText(rawValue, 2048, "ToolTip.Tip", true, out normalizedValue, out error);
            case NameKey:
                return TryNormalizeText(rawValue, 512, "AutomationProperties.Name", false, out normalizedValue, out error);
            case AutomationIdKey:
                return TryNormalizeText(rawValue, 512, "AutomationProperties.AutomationId", false, out normalizedValue, out error);
            case HelpTextKey:
                return TryNormalizeText(rawValue, 2048, "AutomationProperties.HelpText", true, out normalizedValue, out error);
            case AccessibilityViewKey:
                return TryNormalizeEnum<AccessibilityView>(
                    rawValue,
                    "AutomationProperties.AccessibilityView",
                    out normalizedValue,
                    out error);
            case HeadingLevelKey:
                if (!int.TryParse(
                        rawValue.Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var heading)
                    || heading is < 0 or > 9)
                {
                    error = "AutomationProperties.HeadingLevel must be an integer from 0 to 9.";
                    return false;
                }

                normalizedValue = heading.ToString(CultureInfo.InvariantCulture);
                return true;
            case LiveSettingKey:
                return TryNormalizeEnum<AutomationLiveSetting>(
                    rawValue,
                    "AutomationProperties.LiveSetting",
                    out normalizedValue,
                    out error);
            case IsRequiredForFormKey:
            case IsTabStopKey:
            case FocusableKey:
                if (!bool.TryParse(rawValue.Trim(), out var boolean))
                {
                    error = $"{propertyName} must be True or False.";
                    return false;
                }

                normalizedValue = boolean.ToString();
                return true;
            case TabIndexKey:
                if (!int.TryParse(
                        rawValue.Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var index))
                {
                    error = "TabIndex must be a 32-bit integer.";
                    return false;
                }

                normalizedValue = index.ToString(CultureInfo.InvariantCulture);
                return true;
            default:
                return false;
        }
    }

    public static bool IsSupportedAxamlProperty(string propertyName)
        => GetInternalKey(propertyName).Length > 0;

    public static IReadOnlyList<DesignerAccessibilityAttribute> GetAxamlAttributes(
        IReadOnlyDictionary<string, string> properties)
    {
        var attributes = new List<DesignerAccessibilityAttribute>();
        AddAttribute(attributes, properties, ToolTipKey, "ToolTip.Tip");
        AddAttribute(attributes, properties, NameKey, "AutomationProperties.Name");
        AddAttribute(attributes, properties, AutomationIdKey, "AutomationProperties.AutomationId");
        AddAttribute(attributes, properties, HelpTextKey, "AutomationProperties.HelpText");
        AddAttribute(attributes, properties, AccessibilityViewKey, "AutomationProperties.AccessibilityView");
        AddAttribute(attributes, properties, HeadingLevelKey, "AutomationProperties.HeadingLevel");
        AddAttribute(attributes, properties, LiveSettingKey, "AutomationProperties.LiveSetting");
        AddAttribute(attributes, properties, IsRequiredForFormKey, "AutomationProperties.IsRequiredForForm");
        AddAttribute(attributes, properties, TabIndexKey, "TabIndex");
        AddAttribute(attributes, properties, IsTabStopKey, "IsTabStop");
        AddAttribute(attributes, properties, FocusableKey, "Focusable");
        return attributes;
    }

    private static string GetInternalKey(string propertyName)
        => propertyName.Trim().ToLowerInvariant() switch
        {
            "tooltip.tip" => ToolTipKey,
            "automationproperties.name" => NameKey,
            "automationproperties.automationid" => AutomationIdKey,
            "automationproperties.helptext" => HelpTextKey,
            "automationproperties.accessibilityview" => AccessibilityViewKey,
            "automationproperties.headinglevel" => HeadingLevelKey,
            "automationproperties.livesetting" => LiveSettingKey,
            "automationproperties.isrequiredforform" => IsRequiredForFormKey,
            "tabindex" => TabIndexKey,
            "istabstop" => IsTabStopKey,
            "focusable" => FocusableKey,
            _ => string.Empty,
        };

    private static bool TryNormalizeText(
        string value,
        int maximumLength,
        string label,
        bool allowLineBreaks,
        out string normalized,
        out string error)
    {
        normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            error = $"{label} must be {maximumLength} characters or fewer.";
            return false;
        }

        foreach (var character in normalized)
        {
            if (char.IsControl(character)
                && (!allowLineBreaks || character is not ('\r' or '\n' or '\t')))
            {
                error = $"{label} contains an unsupported control character.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static bool TryNormalizeEnum<T>(
        string rawValue,
        string label,
        out string normalized,
        out string error)
        where T : struct, Enum
    {
        if (!Enum.TryParse<T>(rawValue.Trim(), true, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            normalized = string.Empty;
            error = $"{label} must be one of: {string.Join(", ", Enum.GetNames<T>())}.";
            return false;
        }

        normalized = parsed.ToString();
        error = string.Empty;
        return true;
    }

    private static void AddText(
        IDictionary<string, string> properties,
        string key,
        string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            properties[key] = value;
        }
    }

    private static void AddAttribute(
        ICollection<DesignerAccessibilityAttribute> attributes,
        IReadOnlyDictionary<string, string> properties,
        string key,
        string name)
    {
        if (TryGetValue(properties, key, out var value))
        {
            attributes.Add(new DesignerAccessibilityAttribute(name, value));
        }
    }

    private static void ApplyText(
        Control control,
        IReadOnlyDictionary<string, string> properties,
        string key,
        AvaloniaProperty property,
        Action<StyledElement, string?> setter)
    {
        if (TryGetValue(properties, key, out var value))
        {
            SetOrClearText(control, value, property, setter);
        }
    }

    private static void SetOrClearText(
        Control control,
        string value,
        AvaloniaProperty property,
        Action<StyledElement, string?> setter)
    {
        if (string.IsNullOrEmpty(value))
        {
            control.ClearValue(property);
        }
        else
        {
            setter(control, value);
        }
    }

    private static void SetOrClear<T>(
        Control control,
        T value,
        T defaultValue,
        AvaloniaProperty property,
        Action<StyledElement, T> setter)
        where T : struct, Enum
    {
        if (EqualityComparer<T>.Default.Equals(value, defaultValue))
        {
            control.ClearValue(property);
        }
        else
        {
            setter(control, value);
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
}
