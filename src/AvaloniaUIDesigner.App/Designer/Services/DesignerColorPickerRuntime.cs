using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerColorPickerValues(
    Color Color,
    ColorModel ColorModel,
    ColorSpectrumComponents ColorSpectrumComponents,
    ColorSpectrumShape ColorSpectrumShape,
    AlphaComponentPosition HexInputAlphaPosition,
    bool IsAccentColorsVisible,
    bool IsAlphaEnabled,
    bool IsAlphaVisible,
    bool IsColorComponentsVisible,
    bool IsColorModelVisible,
    bool IsColorPaletteVisible,
    bool IsColorPreviewVisible,
    bool IsColorSpectrumVisible,
    bool IsColorSpectrumSliderVisible,
    bool IsComponentSliderVisible,
    bool IsComponentTextInputVisible,
    bool IsHexInputVisible,
    int PaletteColumnCount);

public sealed record DesignerColorPickerEditorInput(
    string Color,
    string ColorModel,
    string ColorSpectrumComponents,
    string ColorSpectrumShape,
    string HexInputAlphaPosition,
    bool IsAccentColorsVisible,
    bool IsAlphaEnabled,
    bool IsAlphaVisible,
    bool IsColorComponentsVisible,
    bool IsColorModelVisible,
    bool IsColorPaletteVisible,
    bool IsColorPreviewVisible,
    bool IsColorSpectrumVisible,
    bool IsColorSpectrumSliderVisible,
    bool IsComponentSliderVisible,
    bool IsComponentTextInputVisible,
    bool IsHexInputVisible,
    string PaletteColumnCount);

public sealed record DesignerColorPickerAttribute(string Name, string Value);

public static class DesignerColorPickerRuntime
{
    private static readonly string[] Properties =
    [
        "Color",
        "ColorModel",
        "ColorSpectrumComponents",
        "ColorSpectrumShape",
        "HexInputAlphaPosition",
        "IsAccentColorsVisible",
        "IsAlphaEnabled",
        "IsAlphaVisible",
        "IsColorComponentsVisible",
        "IsColorModelVisible",
        "IsColorPaletteVisible",
        "IsColorPreviewVisible",
        "IsColorSpectrumVisible",
        "IsColorSpectrumSliderVisible",
        "IsComponentSliderVisible",
        "IsComponentTextInputVisible",
        "IsHexInputVisible",
        "PaletteColumnCount",
    ];

    public static IReadOnlyList<string> ColorModelNames { get; } =
        Enum.GetNames<ColorModel>();

    public static IReadOnlyList<string> ColorSpectrumComponentsNames { get; } =
        Enum.GetNames<ColorSpectrumComponents>();

    public static IReadOnlyList<string> ColorSpectrumShapeNames { get; } =
        Enum.GetNames<ColorSpectrumShape>();

    public static IReadOnlyList<string> AlphaComponentPositionNames { get; } =
        Enum.GetNames<AlphaComponentPosition>();

    public static bool IsSupportedControl(Control control)
        => control is ColorPicker;

    public static bool TryRead(
        Control control,
        out DesignerColorPickerValues values,
        out string error)
    {
        if (control is ColorPicker picker)
        {
            values = new DesignerColorPickerValues(
                picker.Color,
                picker.ColorModel,
                picker.ColorSpectrumComponents,
                picker.ColorSpectrumShape,
                picker.HexInputAlphaPosition,
                picker.IsAccentColorsVisible,
                picker.IsAlphaEnabled,
                picker.IsAlphaVisible,
                picker.IsColorComponentsVisible,
                picker.IsColorModelVisible,
                picker.IsColorPaletteVisible,
                picker.IsColorPreviewVisible,
                picker.IsColorSpectrumVisible,
                picker.IsColorSpectrumSliderVisible,
                picker.IsComponentSliderVisible,
                picker.IsComponentTextInputVisible,
                picker.IsHexInputVisible,
                picker.PaletteColumnCount);
            error = string.Empty;
            return true;
        }

        values = default!;
        error = "Color editing is available for ColorPicker controls.";
        return false;
    }

    public static bool TryParseValues(
        DesignerColorPickerEditorInput input,
        DesignerColorPickerValues current,
        out DesignerColorPickerValues values,
        out string error)
    {
        if (!Color.TryParse(input.Color.Trim(), out var color))
        {
            values = default!;
            error = "Color must be a valid Avalonia color such as #FF3B82F6.";
            return false;
        }

        if (!TryParseEnum(input.ColorModel, "Color model", out ColorModel colorModel, out error)
            || !TryParseEnum(
                input.ColorSpectrumComponents,
                "Color spectrum components",
                out ColorSpectrumComponents spectrumComponents,
                out error)
            || !TryParseEnum(
                input.ColorSpectrumShape,
                "Color spectrum shape",
                out ColorSpectrumShape spectrumShape,
                out error)
            || !TryParseEnum(
                input.HexInputAlphaPosition,
                "Hex input alpha position",
                out AlphaComponentPosition alphaPosition,
                out error))
        {
            values = default!;
            return false;
        }

        if (!int.TryParse(input.PaletteColumnCount.Trim(), out var paletteColumnCount)
            || paletteColumnCount < 1
            || paletteColumnCount > 32)
        {
            values = default!;
            error = "Palette column count must be an integer from 1 to 32.";
            return false;
        }

        values = current with
        {
            Color = color,
            ColorModel = colorModel,
            ColorSpectrumComponents = spectrumComponents,
            ColorSpectrumShape = spectrumShape,
            HexInputAlphaPosition = alphaPosition,
            IsAccentColorsVisible = input.IsAccentColorsVisible,
            IsAlphaEnabled = input.IsAlphaEnabled,
            IsAlphaVisible = input.IsAlphaVisible,
            IsColorComponentsVisible = input.IsColorComponentsVisible,
            IsColorModelVisible = input.IsColorModelVisible,
            IsColorPaletteVisible = input.IsColorPaletteVisible,
            IsColorPreviewVisible = input.IsColorPreviewVisible,
            IsColorSpectrumVisible = input.IsColorSpectrumVisible,
            IsColorSpectrumSliderVisible = input.IsColorSpectrumSliderVisible,
            IsComponentSliderVisible = input.IsComponentSliderVisible,
            IsComponentTextInputVisible = input.IsComponentTextInputVisible,
            IsHexInputVisible = input.IsHexInputVisible,
            PaletteColumnCount = paletteColumnCount,
        };
        error = string.Empty;
        return true;
    }

    public static void Capture(Control control, IDictionary<string, string> properties)
    {
        if (!TryRead(control, out var values, out _))
        {
            return;
        }

        foreach (var attribute in GetAxamlAttributes(values))
        {
            properties[attribute.Name] = attribute.Value;
        }
    }

    public static void Apply(Control control, IReadOnlyDictionary<string, string> properties)
    {
        if (control is not ColorPicker picker
            || !TryRead(picker, out var current, out _))
        {
            return;
        }

        var input = new DesignerColorPickerEditorInput(
            Get(properties, "Color", FormatColor(current.Color)),
            Get(properties, "ColorModel", current.ColorModel.ToString()),
            Get(properties, "ColorSpectrumComponents", current.ColorSpectrumComponents.ToString()),
            Get(properties, "ColorSpectrumShape", current.ColorSpectrumShape.ToString()),
            Get(properties, "HexInputAlphaPosition", current.HexInputAlphaPosition.ToString()),
            GetBoolean(properties, "IsAccentColorsVisible", current.IsAccentColorsVisible),
            GetBoolean(properties, "IsAlphaEnabled", current.IsAlphaEnabled),
            GetBoolean(properties, "IsAlphaVisible", current.IsAlphaVisible),
            GetBoolean(properties, "IsColorComponentsVisible", current.IsColorComponentsVisible),
            GetBoolean(properties, "IsColorModelVisible", current.IsColorModelVisible),
            GetBoolean(properties, "IsColorPaletteVisible", current.IsColorPaletteVisible),
            GetBoolean(properties, "IsColorPreviewVisible", current.IsColorPreviewVisible),
            GetBoolean(properties, "IsColorSpectrumVisible", current.IsColorSpectrumVisible),
            GetBoolean(properties, "IsColorSpectrumSliderVisible", current.IsColorSpectrumSliderVisible),
            GetBoolean(properties, "IsComponentSliderVisible", current.IsComponentSliderVisible),
            GetBoolean(properties, "IsComponentTextInputVisible", current.IsComponentTextInputVisible),
            GetBoolean(properties, "IsHexInputVisible", current.IsHexInputVisible),
            Get(properties, "PaletteColumnCount", current.PaletteColumnCount.ToString()));
        if (TryParseValues(input, current, out var values, out _))
        {
            Apply(picker, values);
        }
    }

    public static void Apply(ColorPicker picker, DesignerColorPickerValues values)
    {
        picker.Color = values.Color;
        picker.ColorModel = values.ColorModel;
        picker.ColorSpectrumComponents = values.ColorSpectrumComponents;
        picker.ColorSpectrumShape = values.ColorSpectrumShape;
        picker.HexInputAlphaPosition = values.HexInputAlphaPosition;
        picker.IsAccentColorsVisible = values.IsAccentColorsVisible;
        picker.IsAlphaEnabled = values.IsAlphaEnabled;
        picker.IsAlphaVisible = values.IsAlphaVisible;
        picker.IsColorComponentsVisible = values.IsColorComponentsVisible;
        picker.IsColorModelVisible = values.IsColorModelVisible;
        picker.IsColorPaletteVisible = values.IsColorPaletteVisible;
        picker.IsColorPreviewVisible = values.IsColorPreviewVisible;
        picker.IsColorSpectrumVisible = values.IsColorSpectrumVisible;
        picker.IsColorSpectrumSliderVisible = values.IsColorSpectrumSliderVisible;
        picker.IsComponentSliderVisible = values.IsComponentSliderVisible;
        picker.IsComponentTextInputVisible = values.IsComponentTextInputVisible;
        picker.IsHexInputVisible = values.IsHexInputVisible;
        picker.PaletteColumnCount = values.PaletteColumnCount;
    }

    public static bool IsSupportedProperty(string tagName, string propertyName)
        => string.Equals(tagName, "ColorPicker", StringComparison.OrdinalIgnoreCase)
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
        if (!string.Equals(tagName, "ColorPicker", StringComparison.OrdinalIgnoreCase)
            || canonicalName.Length == 0)
        {
            error = $"{tagName}.{propertyName} is not a supported ColorPicker property.";
            return false;
        }

        if (canonicalName == "Color")
        {
            if (!Color.TryParse(rawValue.Trim(), out var color))
            {
                error = "Color must be a valid Avalonia color.";
                return false;
            }

            normalizedValue = FormatColor(color);
            error = string.Empty;
            return true;
        }

        if (canonicalName == "PaletteColumnCount")
        {
            if (!int.TryParse(rawValue.Trim(), out var count) || count is < 1 or > 32)
            {
                error = "Palette column count must be an integer from 1 to 32.";
                return false;
            }

            normalizedValue = count.ToString();
            error = string.Empty;
            return true;
        }

        if (canonicalName.StartsWith("Is", StringComparison.Ordinal))
        {
            if (!bool.TryParse(rawValue.Trim(), out var boolean))
            {
                error = $"{canonicalName} must be True or False.";
                return false;
            }

            normalizedValue = boolean.ToString();
            error = string.Empty;
            return true;
        }

        return canonicalName switch
        {
            "ColorModel" => TryNormalizeEnum<ColorModel>(rawValue, canonicalName, out normalizedValue, out error),
            "ColorSpectrumComponents" => TryNormalizeEnum<ColorSpectrumComponents>(rawValue, canonicalName, out normalizedValue, out error),
            "ColorSpectrumShape" => TryNormalizeEnum<ColorSpectrumShape>(rawValue, canonicalName, out normalizedValue, out error),
            "HexInputAlphaPosition" => TryNormalizeEnum<AlphaComponentPosition>(rawValue, canonicalName, out normalizedValue, out error),
            _ => FailUnsupported(canonicalName, out error),
        };
    }

    public static bool TryValidateProperties(
        string tagName,
        IReadOnlyDictionary<string, string> properties,
        out string error)
    {
        if (!string.Equals(tagName, "ColorPicker", StringComparison.OrdinalIgnoreCase))
        {
            error = string.Empty;
            return true;
        }

        if (TryGetValue(properties, "Color", out var rawColor)
            && !Color.TryParse(rawColor.Trim(), out _))
        {
            error = "Color must be a valid Avalonia color.";
            return false;
        }

        if (TryGetValue(properties, "PaletteColumnCount", out var rawCount)
            && (!int.TryParse(rawCount.Trim(), out var count) || count is < 1 or > 32))
        {
            error = "Palette column count must be an integer from 1 to 32.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static void RemoveProperties(string tagName, IDictionary<string, string> properties)
    {
        if (string.Equals(tagName, "ColorPicker", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var propertyName in Properties)
            {
                properties.Remove(propertyName);
            }
        }
    }

    public static IReadOnlyList<DesignerColorPickerAttribute> GetAxamlAttributes(Control control)
        => TryRead(control, out var values, out _)
            ? GetAxamlAttributes(values)
            : [];

    private static IReadOnlyList<DesignerColorPickerAttribute> GetAxamlAttributes(
        DesignerColorPickerValues values)
        =>
        [
            new("Color", FormatColor(values.Color)),
            new("ColorModel", values.ColorModel.ToString()),
            new("ColorSpectrumComponents", values.ColorSpectrumComponents.ToString()),
            new("ColorSpectrumShape", values.ColorSpectrumShape.ToString()),
            new("HexInputAlphaPosition", values.HexInputAlphaPosition.ToString()),
            new("IsAccentColorsVisible", values.IsAccentColorsVisible.ToString()),
            new("IsAlphaEnabled", values.IsAlphaEnabled.ToString()),
            new("IsAlphaVisible", values.IsAlphaVisible.ToString()),
            new("IsColorComponentsVisible", values.IsColorComponentsVisible.ToString()),
            new("IsColorModelVisible", values.IsColorModelVisible.ToString()),
            new("IsColorPaletteVisible", values.IsColorPaletteVisible.ToString()),
            new("IsColorPreviewVisible", values.IsColorPreviewVisible.ToString()),
            new("IsColorSpectrumVisible", values.IsColorSpectrumVisible.ToString()),
            new("IsColorSpectrumSliderVisible", values.IsColorSpectrumSliderVisible.ToString()),
            new("IsComponentSliderVisible", values.IsComponentSliderVisible.ToString()),
            new("IsComponentTextInputVisible", values.IsComponentTextInputVisible.ToString()),
            new("IsHexInputVisible", values.IsHexInputVisible.ToString()),
            new("PaletteColumnCount", values.PaletteColumnCount.ToString()),
        ];

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
        string label,
        out string normalizedValue,
        out string error)
        where T : struct, Enum
    {
        if (!TryParseEnum(rawValue, label, out T value, out error))
        {
            normalizedValue = string.Empty;
            return false;
        }

        normalizedValue = value.ToString();
        return true;
    }

    private static bool FailUnsupported(string propertyName, out string error)
    {
        error = $"{propertyName} is not a supported ColorPicker property.";
        return false;
    }

    private static string Get(
        IReadOnlyDictionary<string, string> properties,
        string key,
        string fallback)
    {
        foreach (var pair in properties)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        return fallback;
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

    private static bool GetBoolean(
        IReadOnlyDictionary<string, string> properties,
        string key,
        bool fallback)
        => bool.TryParse(Get(properties, key, fallback.ToString()), out var value)
            ? value
            : fallback;

    private static string FormatColor(Color color)
        => $"#{color.A:x2}{color.R:x2}{color.G:x2}{color.B:x2}";
}
