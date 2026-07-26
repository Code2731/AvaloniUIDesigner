using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Transformation;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerTransformValues(
    double TranslateX,
    double TranslateY,
    double Rotation,
    double ScaleX,
    double ScaleY,
    double SkewX,
    double SkewY,
    double OriginX,
    double OriginY)
{
    public static DesignerTransformValues Default { get; } =
        new(0, 0, 0, 1, 1, 0, 0, 50, 50);
}

public static class DesignerTransformRuntime
{
    private const double Epsilon = 0.0000001;

    public static bool IsSupportedProperty(string propertyName)
        => string.Equals(propertyName, "RenderTransform", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyName, "RenderTransformOrigin", StringComparison.OrdinalIgnoreCase);

    public static bool TryRead(
        Control control,
        out DesignerTransformValues values,
        out string error)
    {
        if (control.RenderTransformOrigin.Unit != RelativeUnit.Relative)
        {
            values = DesignerTransformValues.Default;
            error = "The existing absolute RenderTransformOrigin cannot be edited by the percentage-based transform editor.";
            return false;
        }

        var origin = ReadOrigin(control.RenderTransformOrigin);
        values = DesignerTransformValues.Default with
        {
            OriginX = origin.X,
            OriginY = origin.Y,
        };
        error = string.Empty;

        if (control.RenderTransform is null)
        {
            return true;
        }

        if (control.RenderTransform is not TransformOperations operations)
        {
            error = $"The existing {control.RenderTransform.GetType().Name} cannot be edited by the common transform editor.";
            return false;
        }

        return TryReadOperations(operations, origin.X, origin.Y, out values, out error);
    }

    public static void Capture(Control control, IDictionary<string, string> properties)
    {
        if (!TryRead(control, out var values, out _))
        {
            return;
        }

        var expression = FormatTransform(values);
        if (!string.IsNullOrEmpty(expression))
        {
            properties["RenderTransform"] = expression;
        }

        if (control.IsSet(Visual.RenderTransformOriginProperty)
            && (!NearlyEqual(values.OriginX, 50) || !NearlyEqual(values.OriginY, 50)))
        {
            properties["RenderTransformOrigin"] = FormatOrigin(values);
        }
    }

    public static void Apply(Control control, IReadOnlyDictionary<string, string> properties)
    {
        var hasTransform = TryGetValue(properties, "RenderTransform", out var transform);
        var hasOrigin = TryGetValue(properties, "RenderTransformOrigin", out var origin);
        if (!hasTransform && !hasOrigin)
        {
            return;
        }

        var values = DesignerTransformValues.Default;
        var originX = values.OriginX;
        var originY = values.OriginY;
        if (hasTransform
            && !TryParseTransform(transform, values.OriginX, values.OriginY, out values, out _))
        {
            return;
        }

        if (hasOrigin
            && !TryParseOrigin(origin, out originX, out originY, out _))
        {
            return;
        }

        if (hasOrigin)
        {
            values = values with { OriginX = originX, OriginY = originY };
        }

        Apply(control, values);
    }

    public static void Apply(Control control, DesignerTransformValues values)
    {
        var expression = FormatTransform(values);
        if (string.IsNullOrEmpty(expression))
        {
            control.ClearValue(Visual.RenderTransformProperty);
        }
        else
        {
            control.RenderTransform = TransformOperations.Parse(expression);
        }

        if (NearlyEqual(values.OriginX, 50) && NearlyEqual(values.OriginY, 50))
        {
            control.ClearValue(Visual.RenderTransformOriginProperty);
        }
        else
        {
            control.RenderTransformOrigin = new RelativePoint(
                values.OriginX / 100,
                values.OriginY / 100,
                RelativeUnit.Relative);
        }
    }

    public static bool TryParseValues(
        string translateX,
        string translateY,
        string rotation,
        string scaleX,
        string scaleY,
        string skewX,
        string skewY,
        string originX,
        string originY,
        out DesignerTransformValues values,
        out string error)
    {
        values = DesignerTransformValues.Default;
        if (!TryParseNumber(translateX, -100000, 100000, "Translate X", out var parsedTranslateX, out error)
            || !TryParseNumber(translateY, -100000, 100000, "Translate Y", out var parsedTranslateY, out error)
            || !TryParseNumber(rotation, -3600, 3600, "Rotation", out var parsedRotation, out error)
            || !TryParseNumber(scaleX, -100, 100, "Scale X", out var parsedScaleX, out error)
            || !TryParseNumber(scaleY, -100, 100, "Scale Y", out var parsedScaleY, out error)
            || !TryParseNumber(skewX, -89, 89, "Skew X", out var parsedSkewX, out error)
            || !TryParseNumber(skewY, -89, 89, "Skew Y", out var parsedSkewY, out error)
            || !TryParseNumber(originX, 0, 100, "Origin X", out var parsedOriginX, out error)
            || !TryParseNumber(originY, 0, 100, "Origin Y", out var parsedOriginY, out error))
        {
            return false;
        }

        values = new DesignerTransformValues(
            parsedTranslateX,
            parsedTranslateY,
            parsedRotation,
            parsedScaleX,
            parsedScaleY,
            parsedSkewX,
            parsedSkewY,
            parsedOriginX,
            parsedOriginY);
        error = string.Empty;
        return true;
    }

    public static bool TryNormalizeProperty(
        string propertyName,
        string rawValue,
        out string canonicalName,
        out string normalizedValue,
        out string error)
    {
        canonicalName = string.Equals(
            propertyName,
            "RenderTransformOrigin",
            StringComparison.OrdinalIgnoreCase)
                ? "RenderTransformOrigin"
                : "RenderTransform";
        normalizedValue = string.Empty;
        error = string.Empty;
        if (!IsSupportedProperty(propertyName))
        {
            error = $"{propertyName} is not a supported common transform property.";
            return false;
        }

        if (canonicalName == "RenderTransformOrigin")
        {
            if (!TryParseOrigin(rawValue, out var originX, out var originY, out error))
            {
                return false;
            }

            normalizedValue = FormatOrigin(originX, originY);
            return true;
        }

        if (!TryParseTransform(rawValue, 50, 50, out var values, out error))
        {
            return false;
        }

        normalizedValue = FormatTransform(values);
        if (string.IsNullOrEmpty(normalizedValue))
        {
            normalizedValue = "scale(1,1)";
        }

        return true;
    }

    public static string FormatTransform(DesignerTransformValues values)
    {
        var parts = new List<string>(4);
        if (!NearlyEqual(values.TranslateX, 0) || !NearlyEqual(values.TranslateY, 0))
        {
            parts.Add($"translate({Format(values.TranslateX)}px,{Format(values.TranslateY)}px)");
        }

        if (!NearlyEqual(values.Rotation, 0))
        {
            parts.Add($"rotate({Format(values.Rotation)}deg)");
        }

        if (!NearlyEqual(values.ScaleX, 1) || !NearlyEqual(values.ScaleY, 1))
        {
            parts.Add($"scale({Format(values.ScaleX)},{Format(values.ScaleY)})");
        }

        if (!NearlyEqual(values.SkewX, 0) || !NearlyEqual(values.SkewY, 0))
        {
            parts.Add($"skew({Format(values.SkewX)}deg,{Format(values.SkewY)}deg)");
        }

        return string.Join(" ", parts);
    }

    public static string FormatOrigin(DesignerTransformValues values)
        => FormatOrigin(values.OriginX, values.OriginY);

    public static bool AreEquivalent(
        DesignerTransformValues left,
        DesignerTransformValues right)
        => NearlyEqual(left.TranslateX, right.TranslateX)
            && NearlyEqual(left.TranslateY, right.TranslateY)
            && NearlyEqual(left.Rotation, right.Rotation)
            && NearlyEqual(left.ScaleX, right.ScaleX)
            && NearlyEqual(left.ScaleY, right.ScaleY)
            && NearlyEqual(left.SkewX, right.SkewX)
            && NearlyEqual(left.SkewY, right.SkewY)
            && NearlyEqual(left.OriginX, right.OriginX)
            && NearlyEqual(left.OriginY, right.OriginY);

    private static bool TryParseTransform(
        string rawValue,
        double originX,
        double originY,
        out DesignerTransformValues values,
        out string error)
    {
        values = DesignerTransformValues.Default with { OriginX = originX, OriginY = originY };
        error = string.Empty;
        TransformOperations operations;
        try
        {
            operations = TransformOperations.Parse(rawValue.Trim());
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            error = $"RenderTransform is invalid: {exception.Message}";
            return false;
        }

        return TryReadOperations(operations, originX, originY, out values, out error);
    }

    private static bool TryReadOperations(
        TransformOperations operations,
        double originX,
        double originY,
        out DesignerTransformValues values,
        out string error)
    {
        values = DesignerTransformValues.Default with { OriginX = originX, OriginY = originY };
        error = string.Empty;
        var sawTranslate = false;
        var sawRotate = false;
        var sawScale = false;
        var sawSkew = false;

        foreach (var operation in operations.Operations)
        {
            switch (operation.Type)
            {
                case TransformOperation.OperationType.Identity:
                    break;
                case TransformOperation.OperationType.Translate when !sawTranslate:
                    values = values with
                    {
                        TranslateX = operation.Data.Translate.X,
                        TranslateY = operation.Data.Translate.Y,
                    };
                    sawTranslate = true;
                    break;
                case TransformOperation.OperationType.Rotate when !sawRotate:
                    values = values with
                    {
                        Rotation = RadiansToDegrees(operation.Data.Rotate.Angle),
                    };
                    sawRotate = true;
                    break;
                case TransformOperation.OperationType.Scale when !sawScale:
                    values = values with
                    {
                        ScaleX = operation.Data.Scale.X,
                        ScaleY = operation.Data.Scale.Y,
                    };
                    sawScale = true;
                    break;
                case TransformOperation.OperationType.Skew when !sawSkew:
                    values = values with
                    {
                        SkewX = RadiansToDegrees(operation.Data.Skew.X),
                        SkewY = RadiansToDegrees(operation.Data.Skew.Y),
                    };
                    sawSkew = true;
                    break;
                case TransformOperation.OperationType.Matrix:
                    error = "Matrix transforms are not supported by the common transform editor.";
                    return false;
                default:
                    error = $"Multiple or unsupported {operation.Type} operations cannot be represented by the common transform editor.";
                    return false;
            }
        }

        return Validate(values, out error);
    }

    private static bool Validate(DesignerTransformValues values, out string error)
    {
        foreach (var (value, minimum, maximum, label) in new[]
                 {
                     (values.TranslateX, -100000d, 100000d, "Translate X"),
                     (values.TranslateY, -100000d, 100000d, "Translate Y"),
                     (values.Rotation, -3600d, 3600d, "Rotation"),
                     (values.ScaleX, -100d, 100d, "Scale X"),
                     (values.ScaleY, -100d, 100d, "Scale Y"),
                     (values.SkewX, -89d, 89d, "Skew X"),
                     (values.SkewY, -89d, 89d, "Skew Y"),
                     (values.OriginX, 0d, 100d, "Origin X"),
                     (values.OriginY, 0d, 100d, "Origin Y"),
                 })
        {
            if (!double.IsFinite(value) || value < minimum || value > maximum)
            {
                error = $"{label} must be a finite number from {Format(minimum)} to {Format(maximum)}.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static bool TryParseOrigin(
        string rawValue,
        out double originX,
        out double originY,
        out string error)
    {
        originX = 50;
        originY = 50;
        try
        {
            var origin = RelativePoint.Parse(rawValue.Trim());
            if (origin.Unit != RelativeUnit.Relative)
            {
                error = "RenderTransformOrigin must use relative percentages, for example 50%,50%.";
                return false;
            }

            originX = origin.Point.X * 100;
            originY = origin.Point.Y * 100;
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            error = $"RenderTransformOrigin is invalid: {exception.Message}";
            return false;
        }

        return Validate(
            DesignerTransformValues.Default with { OriginX = originX, OriginY = originY },
            out error);
    }

    private static bool TryParseNumber(
        string rawValue,
        double minimum,
        double maximum,
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
            || value < minimum
            || value > maximum)
        {
            error = $"{label} must be a finite number from {Format(minimum)} to {Format(maximum)}.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static (double X, double Y) ReadOrigin(RelativePoint origin)
        => origin.Unit == RelativeUnit.Relative
            ? (origin.Point.X * 100, origin.Point.Y * 100)
            : (50, 50);

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

    private static string FormatOrigin(double originX, double originY)
        => $"{Format(originX)}%,{Format(originY)}%";

    private static string Format(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static double RadiansToDegrees(double value)
        => value * 180 / Math.PI;

    private static bool NearlyEqual(double left, double right)
        => Math.Abs(left - right) < Epsilon;
}
