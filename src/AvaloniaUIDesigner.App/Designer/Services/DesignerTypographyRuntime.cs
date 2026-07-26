using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerTypographyValues(
    string FontFamily,
    double FontSize,
    FontStyle FontStyle,
    FontWeight FontWeight,
    TextAlignment TextAlignment,
    TextWrapping TextWrapping);

public static class DesignerTypographyRuntime
{
    private static readonly string[] CommonPropertyNames =
    [
        "FontFamily",
        "FontSize",
        "FontStyle",
        "FontWeight",
    ];

    private static readonly string[] TemplatedTargetTypes =
    [
        "Button",
        "TextBox",
        "Label",
        "CheckBox",
        "RadioButton",
        "ToggleSwitch",
        "ToggleButton",
        "ComboBox",
        "ListBox",
        "TreeView",
        "Menu",
        "DataGrid",
        "DatePicker",
        "CalendarDatePicker",
        "Calendar",
        "TimePicker",
        "NumericUpDown",
        "TabControl",
        "Expander",
    ];

    public static IReadOnlyList<string> FontWeightNames { get; } =
    [
        "Thin",
        "ExtraLight",
        "Light",
        "SemiLight",
        "Normal",
        "Medium",
        "SemiBold",
        "Bold",
        "ExtraBold",
        "Black",
        "ExtraBlack",
    ];

    public static string FormatFontWeight(FontWeight value)
        => value == FontWeight.Thin ? "Thin"
            : value == FontWeight.ExtraLight ? "ExtraLight"
            : value == FontWeight.Light ? "Light"
            : value == FontWeight.SemiLight ? "SemiLight"
            : value == FontWeight.Normal ? "Normal"
            : value == FontWeight.Medium ? "Medium"
            : value == FontWeight.SemiBold ? "SemiBold"
            : value == FontWeight.Bold ? "Bold"
            : value == FontWeight.ExtraBold ? "ExtraBold"
            : value == FontWeight.Black ? "Black"
            : value == FontWeight.ExtraBlack ? "ExtraBlack"
            : value.ToString();

    public static bool SupportsTypography(Control control)
        => SupportsTypography(control.GetType().Name);

    public static bool SupportsTypography(string targetType)
        => string.Equals(targetType, "TextBlock", StringComparison.OrdinalIgnoreCase)
            || TemplatedTargetTypes.Contains(targetType, StringComparer.OrdinalIgnoreCase);

    public static bool SupportsTextAlignment(Control control)
        => control is TextBlock or TextBox;

    public static bool SupportsTextAlignment(string targetType)
        => string.Equals(targetType, "TextBlock", StringComparison.OrdinalIgnoreCase)
            || string.Equals(targetType, "TextBox", StringComparison.OrdinalIgnoreCase);

    public static bool SupportsTextWrapping(Control control)
        => control is TextBlock or TextBox;

    public static bool SupportsTextWrapping(string targetType)
        => string.Equals(targetType, "TextBlock", StringComparison.OrdinalIgnoreCase)
            || string.Equals(targetType, "TextBox", StringComparison.OrdinalIgnoreCase);

    public static bool IsSupportedProperty(string targetType, string propertyName)
        => SupportsTypography(targetType)
            && (CommonPropertyNames.Contains(propertyName, StringComparer.OrdinalIgnoreCase)
                || string.Equals(propertyName, "TextAlignment", StringComparison.OrdinalIgnoreCase)
                    && SupportsTextAlignment(targetType)
                || string.Equals(propertyName, "TextWrapping", StringComparison.OrdinalIgnoreCase)
                    && SupportsTextWrapping(targetType));

    public static DesignerTypographyValues Read(Control control)
    {
        var (fontFamily, fontSize, fontStyle, fontWeight) = control switch
        {
            TextBlock textBlock => (
                textBlock.FontFamily,
                textBlock.FontSize,
                textBlock.FontStyle,
                textBlock.FontWeight),
            TemplatedControl templated => (
                templated.FontFamily,
                templated.FontSize,
                templated.FontStyle,
                templated.FontWeight),
            _ => throw new InvalidOperationException(
                $"{control.GetType().Name} does not support typography properties."),
        };

        return new DesignerTypographyValues(
            fontFamily.ToString(),
            fontSize,
            fontStyle,
            fontWeight,
            control switch
            {
                TextBlock textBlock => textBlock.TextAlignment,
                TextBox textBox => textBox.TextAlignment,
                _ => TextAlignment.Left,
            },
            control switch
            {
                TextBlock textBlock => textBlock.TextWrapping,
                TextBox textBox => textBox.TextWrapping,
                _ => TextWrapping.NoWrap,
            });
    }

    public static void Capture(Control control, IDictionary<string, string> properties)
    {
        if (!SupportsTypography(control))
        {
            return;
        }

        switch (control)
        {
            case TextBlock textBlock:
                CaptureCommon(
                    properties,
                    textBlock.FontFamily,
                    textBlock.FontSize,
                    textBlock.FontStyle,
                    textBlock.FontWeight,
                    textBlock.IsSet(TextBlock.FontFamilyProperty),
                    captureFontSize: true,
                    textBlock.IsSet(TextBlock.FontStyleProperty),
                    captureFontWeight: true);
                if (textBlock.IsSet(TextBlock.TextAlignmentProperty))
                {
                    properties["TextAlignment"] = textBlock.TextAlignment.ToString();
                }

                if (textBlock.IsSet(TextBlock.TextWrappingProperty))
                {
                    properties["TextWrapping"] = textBlock.TextWrapping.ToString();
                }

                break;
            case TextBox textBox:
                CaptureTemplated(properties, textBox);
                if (textBox.IsSet(TextBox.TextAlignmentProperty))
                {
                    properties["TextAlignment"] = textBox.TextAlignment.ToString();
                }

                // Existing documents always preserve TextBox wrapping explicitly.
                properties["TextWrapping"] = textBox.TextWrapping.ToString();
                break;
            case TemplatedControl templated:
                CaptureTemplated(properties, templated);
                break;
        }
    }

    public static void Apply(Control control, IReadOnlyDictionary<string, string> properties)
    {
        if (!SupportsTypography(control))
        {
            return;
        }

        foreach (var pair in properties)
        {
            if (!TryNormalizeProperty(
                    control.GetType().Name,
                    pair.Key,
                    pair.Value,
                    out var propertyName,
                    out var normalizedValue,
                    out _))
            {
                continue;
            }

            ApplyProperty(control, propertyName, normalizedValue);
        }
    }

    public static void Apply(Control control, DesignerTypographyValues values)
    {
        if (!SupportsTypography(control))
        {
            return;
        }

        var family = FontFamily.Parse(values.FontFamily);
        switch (control)
        {
            case TextBlock textBlock:
                textBlock.FontFamily = family;
                textBlock.FontSize = values.FontSize;
                textBlock.FontStyle = values.FontStyle;
                textBlock.FontWeight = values.FontWeight;
                textBlock.TextAlignment = values.TextAlignment;
                textBlock.TextWrapping = values.TextWrapping;
                break;
            case TemplatedControl templated:
                templated.FontFamily = family;
                templated.FontSize = values.FontSize;
                templated.FontStyle = values.FontStyle;
                templated.FontWeight = values.FontWeight;
                if (templated is TextBox textBox)
                {
                    textBox.TextAlignment = values.TextAlignment;
                    textBox.TextWrapping = values.TextWrapping;
                }

                break;
        }
    }

    public static bool TryParseValues(
        Control control,
        string fontFamily,
        string fontSize,
        string fontStyle,
        string fontWeight,
        string textAlignment,
        string textWrapping,
        out DesignerTypographyValues values,
        out string error)
    {
        values = Read(control);
        if (!TryParseFontFamily(fontFamily, out var parsedFontFamily, out error))
        {
            return false;
        }

        if (!double.TryParse(
                fontSize.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsedFontSize)
            || !double.IsFinite(parsedFontSize)
            || parsedFontSize is < 1 or > 512)
        {
            error = "Font size must be a finite number from 1 to 512.";
            return false;
        }

        if (!Enum.TryParse<FontStyle>(fontStyle, true, out var parsedFontStyle)
            || !Enum.IsDefined(parsedFontStyle))
        {
            error = "Font style must be Normal, Italic, or Oblique.";
            return false;
        }

        if (!TryParseFontWeight(fontWeight, out var parsedFontWeight))
        {
            error = $"Font weight must be one of: {string.Join(", ", FontWeightNames)}.";
            return false;
        }

        if (!Enum.TryParse<TextAlignment>(
                textAlignment,
                true,
                out var parsedTextAlignment)
            || !Enum.IsDefined(parsedTextAlignment))
        {
            error = "Text alignment must be a supported Avalonia TextAlignment value.";
            return false;
        }

        if (!Enum.TryParse<TextWrapping>(textWrapping, true, out var parsedTextWrapping)
            || !Enum.IsDefined(parsedTextWrapping))
        {
            error = "Text wrapping must be NoWrap or Wrap.";
            return false;
        }

        values = new DesignerTypographyValues(
            parsedFontFamily,
            parsedFontSize,
            parsedFontStyle,
            parsedFontWeight,
            parsedTextAlignment,
            parsedTextWrapping);
        error = string.Empty;
        return true;
    }

    public static bool TryNormalizeProperty(
        string targetType,
        string propertyName,
        string rawValue,
        out string canonicalName,
        out string normalizedValue,
        out string error)
    {
        canonicalName = GetCanonicalPropertyName(propertyName);
        normalizedValue = string.Empty;
        error = string.Empty;
        if (!IsSupportedProperty(targetType, canonicalName))
        {
            error = $"{targetType}.{propertyName} is not a supported typography property.";
            return false;
        }

        switch (canonicalName)
        {
            case "FontFamily":
                return TryParseFontFamily(rawValue, out normalizedValue, out error);
            case "FontSize":
                if (!double.TryParse(
                        rawValue.Trim(),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var fontSize)
                    || !double.IsFinite(fontSize)
                    || fontSize is < 1 or > 512)
                {
                    error = "FontSize must be a finite number from 1 to 512.";
                    return false;
                }

                normalizedValue = fontSize.ToString("0.###", CultureInfo.InvariantCulture);
                return true;
            case "FontStyle":
                if (!Enum.TryParse<FontStyle>(rawValue, true, out var fontStyle)
                    || !Enum.IsDefined(fontStyle))
                {
                    error = "FontStyle must be Normal, Italic, or Oblique.";
                    return false;
                }

                normalizedValue = fontStyle.ToString();
                return true;
            case "FontWeight":
                if (!TryParseFontWeight(rawValue, out var fontWeight))
                {
                    error = $"FontWeight must be one of: {string.Join(", ", FontWeightNames)}.";
                    return false;
                }

                normalizedValue = FormatFontWeight(fontWeight);
                return true;
            case "TextAlignment":
                if (!Enum.TryParse<TextAlignment>(rawValue, true, out var textAlignment)
                    || !Enum.IsDefined(textAlignment))
                {
                    error = "TextAlignment must be a supported Avalonia TextAlignment value.";
                    return false;
                }

                normalizedValue = textAlignment.ToString();
                return true;
            case "TextWrapping":
                if (!Enum.TryParse<TextWrapping>(rawValue, true, out var textWrapping)
                    || !Enum.IsDefined(textWrapping))
                {
                    error = "TextWrapping must be NoWrap or Wrap.";
                    return false;
                }

                normalizedValue = textWrapping.ToString();
                return true;
            default:
                error = $"Unsupported typography property {canonicalName}.";
                return false;
        }
    }

    private static void CaptureTemplated(
        IDictionary<string, string> properties,
        TemplatedControl control)
        => CaptureCommon(
            properties,
            control.FontFamily,
            control.FontSize,
            control.FontStyle,
            control.FontWeight,
            control.IsSet(TemplatedControl.FontFamilyProperty),
            control.IsSet(TemplatedControl.FontSizeProperty),
            control.IsSet(TemplatedControl.FontStyleProperty),
            control.IsSet(TemplatedControl.FontWeightProperty));

    private static void CaptureCommon(
        IDictionary<string, string> properties,
        FontFamily fontFamily,
        double fontSize,
        FontStyle fontStyle,
        FontWeight fontWeight,
        bool captureFontFamily,
        bool captureFontSize,
        bool captureFontStyle,
        bool captureFontWeight)
    {
        if (captureFontFamily)
        {
            properties["FontFamily"] = fontFamily.ToString();
        }

        if (captureFontSize)
        {
            properties["FontSize"] = fontSize.ToString("0.###", CultureInfo.InvariantCulture);
        }

        if (captureFontStyle)
        {
            properties["FontStyle"] = fontStyle.ToString();
        }

        if (captureFontWeight)
        {
            properties["FontWeight"] = FormatFontWeight(fontWeight);
        }
    }

    private static void ApplyProperty(Control control, string propertyName, string value)
    {
        switch (propertyName)
        {
            case "FontFamily":
                WriteCommon(
                    control,
                    fontFamily: FontFamily.Parse(value));
                break;
            case "FontSize":
                WriteCommon(
                    control,
                    fontSize: double.Parse(value, CultureInfo.InvariantCulture));
                break;
            case "FontStyle":
                WriteCommon(
                    control,
                    fontStyle: Enum.Parse<FontStyle>(value));
                break;
            case "FontWeight":
                TryParseFontWeight(value, out var fontWeight);
                WriteCommon(control, fontWeight: fontWeight);
                break;
            case "TextAlignment" when control is TextBlock textBlock:
                textBlock.TextAlignment = Enum.Parse<TextAlignment>(value);
                break;
            case "TextAlignment" when control is TextBox textBox:
                textBox.TextAlignment = Enum.Parse<TextAlignment>(value);
                break;
            case "TextWrapping" when control is TextBlock textBlock:
                textBlock.TextWrapping = Enum.Parse<TextWrapping>(value);
                break;
            case "TextWrapping" when control is TextBox textBox:
                textBox.TextWrapping = Enum.Parse<TextWrapping>(value);
                break;
        }
    }

    private static void WriteCommon(
        Control control,
        FontFamily? fontFamily = null,
        double? fontSize = null,
        FontStyle? fontStyle = null,
        FontWeight? fontWeight = null)
    {
        switch (control)
        {
            case TextBlock textBlock:
                if (fontFamily is not null) textBlock.FontFamily = fontFamily;
                if (fontSize is not null) textBlock.FontSize = fontSize.Value;
                if (fontStyle is not null) textBlock.FontStyle = fontStyle.Value;
                if (fontWeight is not null) textBlock.FontWeight = fontWeight.Value;
                break;
            case TemplatedControl templated:
                if (fontFamily is not null) templated.FontFamily = fontFamily;
                if (fontSize is not null) templated.FontSize = fontSize.Value;
                if (fontStyle is not null) templated.FontStyle = fontStyle.Value;
                if (fontWeight is not null) templated.FontWeight = fontWeight.Value;
                break;
        }
    }

    private static bool TryParseFontFamily(
        string value,
        out string fontFamily,
        out string error)
    {
        fontFamily = string.Empty;
        error = string.Empty;
        var normalized = value.Trim();
        if (normalized.Length == 0 || normalized.Any(char.IsControl))
        {
            error = "Font family cannot be empty or contain control characters.";
            return false;
        }

        try
        {
            fontFamily = FontFamily.Parse(normalized).ToString();
            return true;
        }
        catch (ArgumentException)
        {
            error = "Font family is not a valid Avalonia FontFamily value.";
            return false;
        }
    }

    private static bool TryParseFontWeight(string value, out FontWeight fontWeight)
    {
        fontWeight = value.Trim().ToLowerInvariant() switch
        {
            "thin" or "100" => FontWeight.Thin,
            "extralight" or "extra-light" or "ultralight" or "ultra-light" or "200" => FontWeight.ExtraLight,
            "light" or "300" => FontWeight.Light,
            "semilight" or "semi-light" or "350" => FontWeight.SemiLight,
            "normal" or "regular" or "400" => FontWeight.Normal,
            "medium" or "500" => FontWeight.Medium,
            "semibold" or "semi-bold" or "demibold" or "600" => FontWeight.SemiBold,
            "bold" or "700" => FontWeight.Bold,
            "extrabold" or "extra-bold" or "ultrabold" or "ultra-bold" or "800" => FontWeight.ExtraBold,
            "black" or "heavy" or "solid" or "900" => FontWeight.Black,
            "extrablack" or "extra-black" or "ultrablack" or "ultra-black" or "950" => FontWeight.ExtraBlack,
            _ => default,
        };
        return fontWeight != default;
    }

    private static string GetCanonicalPropertyName(string propertyName)
        => CommonPropertyNames
            .Concat(["TextAlignment", "TextWrapping"])
            .FirstOrDefault(name =>
                string.Equals(name, propertyName, StringComparison.OrdinalIgnoreCase))
            ?? propertyName;
}
