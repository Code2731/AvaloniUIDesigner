using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaloniaUIDesigner.App.Designer.Services;

public enum DesignerEffectKind
{
    None,
    Blur,
    DropShadow,
}

public sealed record DesignerEffectValues(
    DesignerEffectKind Kind,
    double BlurRadius,
    double OffsetX,
    double OffsetY,
    double ShadowBlurRadius,
    string ShadowColor,
    double ShadowOpacity);

public sealed record DesignerEffectAttribute(string Name, string Value);

public static class DesignerEffectRuntime
{
    private const string EffectKey = "__visualEffect";
    private const double MaximumRadius = 1000;
    private const double MaximumOffset = 10000;

    public static IReadOnlyList<string> EffectKinds { get; } =
        ["None", "Blur", "Drop Shadow"];

    public static DesignerEffectValues DefaultValues { get; } = new(
        DesignerEffectKind.None,
        5,
        3.536,
        3.536,
        5,
        "#000000",
        1);

    public static bool TryRead(
        Control control,
        out DesignerEffectValues values,
        out string error)
        => TryReadEffect(control.Effect, out values, out error);

    public static void Capture(Control control, IDictionary<string, string> properties)
    {
        if (control.Effect is null)
        {
            return;
        }

        if (TryReadEffect(control.Effect, out var values, out _))
        {
            properties[EffectKey] = FormatAxamlValue(values);
        }
    }

    public static void Apply(Control control, IReadOnlyDictionary<string, string> properties)
    {
        if (!TryGetValue(properties, EffectKey, out var expression)
            || !TryParseAxamlValue(expression, out var values, out _))
        {
            return;
        }

        Apply(control, values);
    }

    public static void Apply(Control control, DesignerEffectValues values)
    {
        switch (values.Kind)
        {
            case DesignerEffectKind.None:
                control.ClearValue(Visual.EffectProperty);
                break;
            case DesignerEffectKind.Blur:
                control.Effect = new BlurEffect { Radius = values.BlurRadius };
                break;
            case DesignerEffectKind.DropShadow:
                Color.TryParse(values.ShadowColor, out var color);
                control.Effect = new DropShadowEffect
                {
                    OffsetX = values.OffsetX,
                    OffsetY = values.OffsetY,
                    BlurRadius = values.ShadowBlurRadius,
                    Color = Color.FromArgb(byte.MaxValue, color.R, color.G, color.B),
                    Opacity = values.ShadowOpacity,
                };
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(values));
        }
    }

    public static bool TryParseValues(
        string kind,
        string blurRadius,
        string offsetX,
        string offsetY,
        string shadowBlurRadius,
        string shadowColor,
        string shadowOpacity,
        out DesignerEffectValues values,
        out string error)
    {
        values = DefaultValues;
        if (!TryParseKind(kind, out var parsedKind))
        {
            error = $"Effect must be one of: {string.Join(", ", EffectKinds)}.";
            return false;
        }

        if (parsedKind == DesignerEffectKind.None)
        {
            error = string.Empty;
            return true;
        }

        if (parsedKind == DesignerEffectKind.Blur)
        {
            if (!TryParseFiniteNumber(
                    blurRadius,
                    0,
                    MaximumRadius,
                    "Blur radius",
                    out var parsedBlur,
                    out error))
            {
                return false;
            }

            values = DefaultValues with
            {
                Kind = DesignerEffectKind.Blur,
                BlurRadius = parsedBlur,
            };
            error = string.Empty;
            return true;
        }

        if (!TryParseFiniteNumber(offsetX, -MaximumOffset, MaximumOffset, "Horizontal offset", out var parsedX, out error)
            || !TryParseFiniteNumber(offsetY, -MaximumOffset, MaximumOffset, "Vertical offset", out var parsedY, out error)
            || !TryParseFiniteNumber(
                shadowBlurRadius,
                0,
                MaximumRadius,
                "Shadow blur radius",
                out var parsedShadowBlur,
                out error)
            || !TryParseFiniteNumber(shadowOpacity, 0, 1, "Shadow opacity", out var parsedOpacity, out error))
        {
            return false;
        }

        if (!Color.TryParse(shadowColor.Trim(), out var parsedColor))
        {
            error = "Shadow color must be an Avalonia color such as #000000 or #FF3366.";
            return false;
        }

        var combinedOpacity = parsedOpacity * parsedColor.A / byte.MaxValue;
        values = new DesignerEffectValues(
            parsedKind,
            DefaultValues.BlurRadius,
            parsedX,
            parsedY,
            parsedShadowBlur,
            FormatOpaqueColor(parsedColor),
            combinedOpacity);
        error = string.Empty;
        return true;
    }

    public static bool IsSupportedAxamlProperty(string propertyName)
        => string.Equals(propertyName.Trim(), "Effect", StringComparison.OrdinalIgnoreCase);

    public static bool TryNormalizeAxamlProperty(
        string propertyName,
        string rawValue,
        out string internalKey,
        out string normalizedValue,
        out string error)
    {
        internalKey = string.Empty;
        normalizedValue = string.Empty;
        if (!IsSupportedAxamlProperty(propertyName))
        {
            error = $"{propertyName} is not a supported visual effect property.";
            return false;
        }

        if (!TryParseAxamlValue(rawValue, out var values, out error))
        {
            return false;
        }

        internalKey = EffectKey;
        normalizedValue = FormatAxamlValue(values);
        return true;
    }

    public static IReadOnlyList<DesignerEffectAttribute> GetAxamlAttributes(
        IReadOnlyDictionary<string, string> properties)
    {
        if (!TryGetValue(properties, EffectKey, out var expression)
            || !TryParseAxamlValue(expression, out var values, out _))
        {
            return [];
        }

        return [new DesignerEffectAttribute("Effect", FormatAxamlValue(values))];
    }

    public static string GetDisplayKind(DesignerEffectKind kind)
        => kind switch
        {
            DesignerEffectKind.None => "None",
            DesignerEffectKind.Blur => "Blur",
            DesignerEffectKind.DropShadow => "Drop Shadow",
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    private static bool TryReadEffect(
        IEffect? effect,
        out DesignerEffectValues values,
        out string error)
    {
        switch (effect)
        {
            case null:
                values = DefaultValues;
                error = string.Empty;
                return true;
            case IBlurEffect blur:
                values = DefaultValues with
                {
                    Kind = DesignerEffectKind.Blur,
                    BlurRadius = blur.Radius,
                };
                error = string.Empty;
                return true;
            case IDropShadowEffect shadow:
                values = DefaultValues with
                {
                    Kind = DesignerEffectKind.DropShadow,
                    OffsetX = shadow.OffsetX,
                    OffsetY = shadow.OffsetY,
                    ShadowBlurRadius = shadow.BlurRadius,
                    ShadowColor = FormatOpaqueColor(shadow.Color),
                    ShadowOpacity = Math.Clamp(
                        shadow.Opacity * shadow.Color.A / byte.MaxValue,
                        0,
                        1),
                };
                error = string.Empty;
                return true;
            default:
                values = DefaultValues;
                error = $"The existing {effect.GetType().Name} is not a supported Blur or Drop Shadow effect.";
                return false;
        }
    }

    private static bool TryParseAxamlValue(
        string rawValue,
        out DesignerEffectValues values,
        out string error)
    {
        var candidate = rawValue.Trim();
        if (TryReadFunction(candidate, "blur", out var blurBody))
        {
            if (!TryParseFiniteNumber(blurBody, 0, MaximumRadius, "Blur radius", out var radius, out error))
            {
                values = DefaultValues;
                return false;
            }

            values = DefaultValues with
            {
                Kind = DesignerEffectKind.Blur,
                BlurRadius = radius,
            };
            error = string.Empty;
            return true;
        }

        if (TryReadFunction(candidate, "drop-shadow", out var shadowBody))
        {
            var parts = shadowBody.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 4)
            {
                values = DefaultValues;
                error = "Drop shadow must use: drop-shadow(offsetX offsetY blurRadius color).";
                return false;
            }

            if (!TryParseFiniteNumber(parts[0], -MaximumOffset, MaximumOffset, "Horizontal offset", out var offsetX, out error)
                || !TryParseFiniteNumber(parts[1], -MaximumOffset, MaximumOffset, "Vertical offset", out var offsetY, out error)
                || !TryParseFiniteNumber(parts[2], 0, MaximumRadius, "Shadow blur radius", out var radius, out error))
            {
                values = DefaultValues;
                return false;
            }

            if (!Color.TryParse(parts[3], out var color))
            {
                values = DefaultValues;
                error = "Drop shadow color must be an Avalonia color such as #80000000.";
                return false;
            }

            values = DefaultValues with
            {
                Kind = DesignerEffectKind.DropShadow,
                OffsetX = offsetX,
                OffsetY = offsetY,
                ShadowBlurRadius = radius,
                ShadowColor = FormatOpaqueColor(color),
                ShadowOpacity = color.A / (double)byte.MaxValue,
            };
            error = string.Empty;
            return true;
        }

        values = DefaultValues;
        error = "Effect must use blur(radius) or drop-shadow(offsetX offsetY blurRadius color).";
        return false;
    }

    private static bool TryReadFunction(string value, string functionName, out string body)
    {
        var prefix = functionName + "(";
        if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && value.EndsWith(')'))
        {
            body = value[prefix.Length..^1].Trim();
            return true;
        }

        body = string.Empty;
        return false;
    }

    private static bool TryParseKind(string value, out DesignerEffectKind kind)
    {
        var normalized = value.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
        return Enum.TryParse(normalized, true, out kind) && Enum.IsDefined(kind);
    }

    private static bool TryParseFiniteNumber(
        string value,
        double minimum,
        double maximum,
        string label,
        out double result,
        out string error)
    {
        if (!double.TryParse(
                value.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out result)
            || !double.IsFinite(result)
            || result < minimum
            || result > maximum)
        {
            error = $"{label} must be a finite number from {Format(minimum)} to {Format(maximum)}.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static string FormatAxamlValue(DesignerEffectValues values)
        => values.Kind switch
        {
            DesignerEffectKind.None => "none",
            DesignerEffectKind.Blur => $"blur({Format(values.BlurRadius)})",
            DesignerEffectKind.DropShadow =>
                $"drop-shadow({Format(values.OffsetX)} {Format(values.OffsetY)} {Format(values.ShadowBlurRadius)} {FormatShadowColor(values)})",
            _ => throw new ArgumentOutOfRangeException(nameof(values)),
        };

    private static string FormatShadowColor(DesignerEffectValues values)
    {
        Color.TryParse(values.ShadowColor, out var color);
        var alpha = (byte)Math.Round(
            color.A * Math.Clamp(values.ShadowOpacity, 0, 1),
            MidpointRounding.AwayFromZero);
        return $"#{alpha:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static string FormatOpaqueColor(Color color)
        => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

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
