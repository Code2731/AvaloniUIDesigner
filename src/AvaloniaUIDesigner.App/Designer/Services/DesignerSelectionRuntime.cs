using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace AvaloniaUIDesigner.App.Designer.Services;

public enum DesignerSelectionControlKind
{
    ComboBox,
    ListBox,
    TreeView,
}

public sealed record DesignerSelectionValues(
    DesignerSelectionControlKind Kind,
    int SelectedIndex,
    bool IsTextSearchEnabled,
    bool AutoScrollToSelectedItem,
    bool WrapSelection,
    SelectionMode SelectionMode,
    bool IsEditable,
    string Text,
    string PlaceholderText,
    double MaxDropDownHeight,
    HorizontalAlignment HorizontalContentAlignment,
    VerticalAlignment VerticalContentAlignment);

public sealed record DesignerSelectionEditorInput(
    string SelectedIndex,
    bool IsTextSearchEnabled,
    bool AutoScrollToSelectedItem,
    bool WrapSelection,
    bool AllowMultiple,
    bool ToggleSelection,
    bool AlwaysSelected,
    bool IsEditable,
    string Text,
    string PlaceholderText,
    string MaxDropDownHeight,
    string HorizontalContentAlignment,
    string VerticalContentAlignment);

public sealed record DesignerSelectionAttribute(string Name, string Value);

public static class DesignerSelectionRuntime
{
    private static readonly string[] ComboBoxProperties =
    [
        "SelectedIndex",
        "IsTextSearchEnabled",
        "AutoScrollToSelectedItem",
        "WrapSelection",
        "IsEditable",
        "Text",
        "PlaceholderText",
        "MaxDropDownHeight",
        "HorizontalContentAlignment",
        "VerticalContentAlignment",
    ];

    private static readonly string[] ListBoxProperties =
    [
        "SelectedIndex",
        "IsTextSearchEnabled",
        "AutoScrollToSelectedItem",
        "WrapSelection",
        "SelectionMode",
    ];

    private static readonly string[] TreeViewProperties =
    [
        "AutoScrollToSelectedItem",
        "SelectionMode",
    ];

    private const SelectionMode SupportedSelectionModeFlags =
        SelectionMode.Multiple | SelectionMode.Toggle | SelectionMode.AlwaysSelected;

    public static IReadOnlyList<string> HorizontalAlignmentNames { get; } =
        Enum.GetNames<HorizontalAlignment>();

    public static IReadOnlyList<string> VerticalAlignmentNames { get; } =
        Enum.GetNames<VerticalAlignment>();

    public static bool IsSupportedControl(Control control)
        => control is ComboBox or ListBox or TreeView;

    public static bool TryRead(
        Control control,
        out DesignerSelectionValues values,
        out string error)
    {
        switch (control)
        {
            case ComboBox comboBox:
                values = CreateDefaults(DesignerSelectionControlKind.ComboBox) with
                {
                    SelectedIndex = comboBox.SelectedIndex,
                    IsTextSearchEnabled = comboBox.IsTextSearchEnabled,
                    AutoScrollToSelectedItem = comboBox.AutoScrollToSelectedItem,
                    WrapSelection = comboBox.WrapSelection,
                    IsEditable = comboBox.IsEditable,
                    Text = comboBox.Text ?? string.Empty,
                    PlaceholderText = comboBox.PlaceholderText ?? string.Empty,
                    MaxDropDownHeight = comboBox.MaxDropDownHeight,
                    HorizontalContentAlignment = comboBox.HorizontalContentAlignment,
                    VerticalContentAlignment = comboBox.VerticalContentAlignment,
                };
                error = string.Empty;
                return true;
            case ListBox listBox:
                values = CreateDefaults(DesignerSelectionControlKind.ListBox) with
                {
                    SelectedIndex = listBox.SelectedIndex,
                    IsTextSearchEnabled = listBox.IsTextSearchEnabled,
                    AutoScrollToSelectedItem = listBox.AutoScrollToSelectedItem,
                    WrapSelection = listBox.WrapSelection,
                    SelectionMode = listBox.SelectionMode,
                };
                error = string.Empty;
                return true;
            case TreeView treeView:
                values = CreateDefaults(DesignerSelectionControlKind.TreeView) with
                {
                    AutoScrollToSelectedItem = treeView.AutoScrollToSelectedItem,
                    SelectionMode = treeView.SelectionMode,
                };
                error = string.Empty;
                return true;
            default:
                values = default!;
                error = "Selection behavior editing is available for ComboBox, ListBox, and TreeView controls.";
                return false;
        }
    }

    public static bool TryParseValues(
        Control control,
        DesignerSelectionEditorInput input,
        out DesignerSelectionValues values,
        out string error)
    {
        if (!TryRead(control, out var current, out error))
        {
            values = default!;
            return false;
        }

        var selectionMode = CreateSelectionMode(
            input.AllowMultiple,
            input.ToggleSelection,
            input.AlwaysSelected);
        if (control is TreeView)
        {
            values = current with
            {
                AutoScrollToSelectedItem = input.AutoScrollToSelectedItem,
                SelectionMode = selectionMode,
            };
            error = string.Empty;
            return true;
        }

        if (!int.TryParse(
                input.SelectedIndex.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var selectedIndex))
        {
            values = default!;
            error = "Selected index must be a whole number.";
            return false;
        }

        var itemCount = control is ItemsControl itemsControl
            ? itemsControl.Items.Count
            : 0;
        if (selectedIndex < -1 || selectedIndex >= itemCount)
        {
            values = default!;
            error = itemCount == 0
                ? "Selected index must be -1 because the control has no design items."
                : $"Selected index must be between -1 and {itemCount - 1}.";
            return false;
        }

        if (control is ListBox)
        {
            if (HasFlag(selectionMode, SelectionMode.AlwaysSelected)
                && itemCount > 0
                && selectedIndex < 0)
            {
                values = default!;
                error = "Selected index must identify an item when AlwaysSelected is enabled.";
                return false;
            }

            values = current with
            {
                SelectedIndex = selectedIndex,
                IsTextSearchEnabled = input.IsTextSearchEnabled,
                AutoScrollToSelectedItem = input.AutoScrollToSelectedItem,
                WrapSelection = input.WrapSelection,
                SelectionMode = selectionMode,
            };
            error = string.Empty;
            return true;
        }

        if (!TryParsePositiveFinite(
                input.MaxDropDownHeight,
                "Maximum drop-down height",
                out var maxDropDownHeight,
                out error))
        {
            values = default!;
            return false;
        }

        if (!TryParseEnum(
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
            SelectedIndex = selectedIndex,
            IsTextSearchEnabled = input.IsTextSearchEnabled,
            AutoScrollToSelectedItem = input.AutoScrollToSelectedItem,
            WrapSelection = input.WrapSelection,
            IsEditable = input.IsEditable,
            Text = selectedIndex >= 0
                ? GetItemText((ComboBox)control, selectedIndex)
                : input.Text,
            PlaceholderText = input.PlaceholderText,
            MaxDropDownHeight = maxDropDownHeight,
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

        var input = new DesignerSelectionEditorInput(
            Get(properties, "SelectedIndex", current.SelectedIndex.ToString(CultureInfo.InvariantCulture)),
            GetBoolean(properties, "IsTextSearchEnabled", current.IsTextSearchEnabled),
            GetBoolean(properties, "AutoScrollToSelectedItem", current.AutoScrollToSelectedItem),
            GetBoolean(properties, "WrapSelection", current.WrapSelection),
            HasSelectionFlag(properties, current.SelectionMode, SelectionMode.Multiple),
            HasSelectionFlag(properties, current.SelectionMode, SelectionMode.Toggle),
            HasSelectionFlag(properties, current.SelectionMode, SelectionMode.AlwaysSelected),
            GetBoolean(properties, "IsEditable", current.IsEditable),
            Get(properties, "Text", current.Text),
            Get(properties, "PlaceholderText", current.PlaceholderText),
            Get(
                properties,
                "MaxDropDownHeight",
                Format(current.MaxDropDownHeight)),
            Get(
                properties,
                "HorizontalContentAlignment",
                current.HorizontalContentAlignment.ToString()),
            Get(
                properties,
                "VerticalContentAlignment",
                current.VerticalContentAlignment.ToString()));
        if (TryParseValues(control, input, out var values, out _))
        {
            Apply(control, values);
        }
    }

    public static void Apply(Control control, DesignerSelectionValues values)
    {
        switch (control)
        {
            case ComboBox comboBox when values.Kind == DesignerSelectionControlKind.ComboBox:
                comboBox.IsTextSearchEnabled = values.IsTextSearchEnabled;
                comboBox.AutoScrollToSelectedItem = values.AutoScrollToSelectedItem;
                comboBox.WrapSelection = values.WrapSelection;
                comboBox.IsEditable = values.IsEditable;
                comboBox.PlaceholderText = values.PlaceholderText;
                comboBox.MaxDropDownHeight = values.MaxDropDownHeight;
                comboBox.HorizontalContentAlignment = values.HorizontalContentAlignment;
                comboBox.VerticalContentAlignment = values.VerticalContentAlignment;
                comboBox.SelectedIndex = values.SelectedIndex;
                if (values.IsEditable && values.SelectedIndex < 0)
                {
                    comboBox.Text = values.Text;
                }

                break;
            case ListBox listBox when values.Kind == DesignerSelectionControlKind.ListBox:
                listBox.IsTextSearchEnabled = values.IsTextSearchEnabled;
                listBox.AutoScrollToSelectedItem = values.AutoScrollToSelectedItem;
                listBox.WrapSelection = values.WrapSelection;
                listBox.SelectionMode = values.SelectionMode;
                listBox.SelectedIndex = values.SelectedIndex;
                break;
            case TreeView treeView when values.Kind == DesignerSelectionControlKind.TreeView:
                treeView.AutoScrollToSelectedItem = values.AutoScrollToSelectedItem;
                treeView.SelectionMode = values.SelectionMode;
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
            error = $"{tagName}.{propertyName} is not a supported selection property.";
            return false;
        }

        switch (canonicalName)
        {
            case "SelectedIndex":
                if (!int.TryParse(
                        rawValue.Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var selectedIndex)
                    || selectedIndex < -1)
                {
                    error = "SelectedIndex must be a whole number greater than or equal to -1.";
                    return false;
                }

                normalizedValue = selectedIndex.ToString(CultureInfo.InvariantCulture);
                error = string.Empty;
                return true;
            case "IsTextSearchEnabled":
            case "AutoScrollToSelectedItem":
            case "WrapSelection":
            case "IsEditable":
                if (!bool.TryParse(rawValue.Trim(), out var boolean))
                {
                    error = $"{canonicalName} must be True or False.";
                    return false;
                }

                normalizedValue = boolean.ToString();
                error = string.Empty;
                return true;
            case "SelectionMode":
                if (!TryParseSelectionMode(rawValue, out var selectionMode))
                {
                    error = "SelectionMode may combine Multiple, Toggle, and AlwaysSelected; use Single for no flags.";
                    return false;
                }

                normalizedValue = FormatSelectionMode(selectionMode);
                error = string.Empty;
                return true;
            case "MaxDropDownHeight":
                if (!TryParsePositiveFinite(
                        rawValue,
                        "MaxDropDownHeight",
                        out var maxDropDownHeight,
                        out error))
                {
                    return false;
                }

                normalizedValue = Format(maxDropDownHeight);
                return true;
            case "HorizontalContentAlignment":
                if (!TryParseEnum(
                        rawValue,
                        canonicalName,
                        out HorizontalAlignment horizontalAlignment,
                        out error))
                {
                    return false;
                }

                normalizedValue = horizontalAlignment.ToString();
                return true;
            case "VerticalContentAlignment":
                if (!TryParseEnum(
                        rawValue,
                        canonicalName,
                        out VerticalAlignment verticalAlignment,
                        out error))
                {
                    return false;
                }

                normalizedValue = verticalAlignment.ToString();
                return true;
            case "Text":
            case "PlaceholderText":
                normalizedValue = rawValue;
                error = string.Empty;
                return true;
            default:
                error = $"{tagName}.{propertyName} is not a supported selection property.";
                return false;
        }
    }

    public static bool TryValidateProperties(
        string tagName,
        IReadOnlyDictionary<string, string> properties,
        int? itemCount,
        out string error)
    {
        if (itemCount is null
            || !string.Equals(tagName, "ComboBox", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(tagName, "ListBox", StringComparison.OrdinalIgnoreCase)
            || !TryGetValue(properties, "SelectedIndex", out var rawSelectedIndex)
            || !int.TryParse(
                rawSelectedIndex,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var selectedIndex))
        {
            error = string.Empty;
            return true;
        }

        if (selectedIndex >= itemCount.Value)
        {
            error = itemCount.Value == 0
                ? "SelectedIndex must be -1 because the control has no static items."
                : $"SelectedIndex must be between -1 and {itemCount.Value - 1}.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static void RemoveSelectedIndex(IDictionary<string, string> properties)
        => properties.Remove("SelectedIndex");

    public static IReadOnlyList<DesignerSelectionAttribute> GetAxamlAttributes(Control control)
    {
        if (!TryRead(control, out var values, out _))
        {
            return [];
        }

        var attributes = new List<DesignerSelectionAttribute>();
        switch (values.Kind)
        {
            case DesignerSelectionControlKind.ComboBox:
                attributes.Add(new("SelectedIndex", values.SelectedIndex.ToString(CultureInfo.InvariantCulture)));
                attributes.Add(new("IsTextSearchEnabled", values.IsTextSearchEnabled.ToString()));
                attributes.Add(new("AutoScrollToSelectedItem", values.AutoScrollToSelectedItem.ToString()));
                attributes.Add(new("WrapSelection", values.WrapSelection.ToString()));
                attributes.Add(new("IsEditable", values.IsEditable.ToString()));
                if (values.IsEditable && values.SelectedIndex < 0)
                {
                    attributes.Add(new("Text", values.Text));
                }

                attributes.Add(new("PlaceholderText", values.PlaceholderText));
                attributes.Add(new("MaxDropDownHeight", Format(values.MaxDropDownHeight)));
                attributes.Add(new(
                    "HorizontalContentAlignment",
                    values.HorizontalContentAlignment.ToString()));
                attributes.Add(new(
                    "VerticalContentAlignment",
                    values.VerticalContentAlignment.ToString()));
                break;
            case DesignerSelectionControlKind.ListBox:
                attributes.Add(new("SelectedIndex", values.SelectedIndex.ToString(CultureInfo.InvariantCulture)));
                attributes.Add(new("IsTextSearchEnabled", values.IsTextSearchEnabled.ToString()));
                attributes.Add(new("AutoScrollToSelectedItem", values.AutoScrollToSelectedItem.ToString()));
                attributes.Add(new("WrapSelection", values.WrapSelection.ToString()));
                attributes.Add(new("SelectionMode", FormatSelectionMode(values.SelectionMode)));
                break;
            case DesignerSelectionControlKind.TreeView:
                attributes.Add(new("AutoScrollToSelectedItem", values.AutoScrollToSelectedItem.ToString()));
                attributes.Add(new("SelectionMode", FormatSelectionMode(values.SelectionMode)));
                break;
        }

        return attributes;
    }

    public static bool HasFlag(SelectionMode mode, SelectionMode flag)
        => (mode & flag) == flag;

    private static DesignerSelectionValues CreateDefaults(DesignerSelectionControlKind kind)
        => new(
            kind,
            SelectedIndex: -1,
            IsTextSearchEnabled: false,
            AutoScrollToSelectedItem: true,
            WrapSelection: false,
            SelectionMode: SelectionMode.Single,
            IsEditable: false,
            Text: string.Empty,
            PlaceholderText: string.Empty,
            MaxDropDownHeight: 200,
            HorizontalContentAlignment: HorizontalAlignment.Stretch,
            VerticalContentAlignment: VerticalAlignment.Stretch);

    private static SelectionMode CreateSelectionMode(
        bool allowMultiple,
        bool toggleSelection,
        bool alwaysSelected)
    {
        var mode = SelectionMode.Single;
        if (allowMultiple)
        {
            mode |= SelectionMode.Multiple;
        }

        if (toggleSelection)
        {
            mode |= SelectionMode.Toggle;
        }

        if (alwaysSelected)
        {
            mode |= SelectionMode.AlwaysSelected;
        }

        return mode;
    }

    private static bool TryParseSelectionMode(string rawValue, out SelectionMode mode)
    {
        if (!Enum.TryParse(rawValue.Trim(), true, out mode)
            || (mode & ~SupportedSelectionModeFlags) != 0)
        {
            mode = SelectionMode.Single;
            return false;
        }

        return true;
    }

    private static string FormatSelectionMode(SelectionMode mode)
        => mode == SelectionMode.Single ? nameof(SelectionMode.Single) : mode.ToString();

    private static string[] GetPropertyNames(string tagName)
        => tagName.Trim().ToUpperInvariant() switch
        {
            "COMBOBOX" => ComboBoxProperties,
            "LISTBOX" => ListBoxProperties,
            "TREEVIEW" => TreeViewProperties,
            _ => [],
        };

    private static bool TryParsePositiveFinite(
        string rawValue,
        string label,
        out double value,
        out string error)
    {
        if (!double.TryParse(
                rawValue.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value)
            || !double.IsFinite(value)
            || value <= 0)
        {
            error = $"{label} must be a finite number greater than zero.";
            return false;
        }

        error = string.Empty;
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
            error = $"{label} must be one of: {string.Join(", ", Enum.GetNames<T>())}.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static string GetItemText(ComboBox comboBox, int index)
    {
        var item = comboBox.Items[index];
        return item is ComboBoxItem comboBoxItem
            ? comboBoxItem.Content?.ToString() ?? string.Empty
            : item?.ToString() ?? string.Empty;
    }

    private static bool HasSelectionFlag(
        IReadOnlyDictionary<string, string> properties,
        SelectionMode fallback,
        SelectionMode flag)
        => TryGetValue(properties, "SelectionMode", out var rawValue)
            && TryParseSelectionMode(rawValue, out var mode)
                ? HasFlag(mode, flag)
                : HasFlag(fallback, flag);

    private static bool GetBoolean(
        IReadOnlyDictionary<string, string> properties,
        string propertyName,
        bool fallback)
        => TryGetValue(properties, propertyName, out var rawValue)
            && bool.TryParse(rawValue, out var value)
                ? value
                : fallback;

    private static string Get(
        IReadOnlyDictionary<string, string> properties,
        string propertyName,
        string fallback)
        => TryGetValue(properties, propertyName, out var value) ? value : fallback;

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

    private static string Format(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);
}
