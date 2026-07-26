using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerImageValues(
    string Source,
    Stretch Stretch,
    StretchDirection StretchDirection,
    BitmapInterpolationMode BitmapInterpolationMode,
    EdgeMode EdgeMode,
    BitmapBlendingMode BitmapBlendingMode);

public sealed record DesignerImageEditorInput(
    string Source,
    string Stretch,
    string StretchDirection,
    string BitmapInterpolationMode,
    string EdgeMode,
    string BitmapBlendingMode);

public sealed record DesignerImageAttribute(string Name, string Value);

public static class DesignerImageRuntime
{
    private static readonly string[] PropertyNames =
    [
        "Source",
        "Stretch",
        "StretchDirection",
        "RenderOptions.BitmapInterpolationMode",
        "RenderOptions.EdgeMode",
        "RenderOptions.BitmapBlendingMode",
    ];

    public static IReadOnlyList<string> StretchNames { get; } =
        Enum.GetNames<Stretch>();

    public static IReadOnlyList<string> StretchDirectionNames { get; } =
        Enum.GetNames<StretchDirection>();

    public static IReadOnlyList<string> BitmapInterpolationModeNames { get; } =
        Enum.GetNames<BitmapInterpolationMode>();

    public static IReadOnlyList<string> EdgeModeNames { get; } =
        Enum.GetNames<EdgeMode>();

    public static IReadOnlyList<string> BitmapBlendingModeNames { get; } =
        Enum.GetNames<BitmapBlendingMode>();

    public static bool TryRead(
        Control control,
        out DesignerImageValues values,
        out string error)
    {
        if (control is not Image image)
        {
            values = default!;
            error = "Image source and rendering editing is available for Image controls.";
            return false;
        }

        values = new DesignerImageValues(
            image.Tag?.ToString() ?? string.Empty,
            image.Stretch,
            image.StretchDirection,
            RenderOptions.GetBitmapInterpolationMode(image),
            RenderOptions.GetEdgeMode(image),
            RenderOptions.GetBitmapBlendingMode(image));
        error = string.Empty;
        return true;
    }

    public static bool TryParseValues(
        Control control,
        DesignerImageEditorInput input,
        out DesignerImageValues values,
        out string error)
    {
        if (!TryRead(control, out _, out error))
        {
            values = default!;
            return false;
        }

        if (!TryParseEnum(
                input.Stretch,
                "Stretch",
                out Stretch stretch,
                out error)
            || !TryParseEnum(
                input.StretchDirection,
                "Stretch direction",
                out StretchDirection stretchDirection,
                out error)
            || !TryParseEnum(
                input.BitmapInterpolationMode,
                "Bitmap interpolation mode",
                out BitmapInterpolationMode interpolationMode,
                out error)
            || !TryParseEnum(
                input.EdgeMode,
                "Edge mode",
                out EdgeMode edgeMode,
                out error)
            || !TryParseEnum(
                input.BitmapBlendingMode,
                "Bitmap blending mode",
                out BitmapBlendingMode blendingMode,
                out error))
        {
            values = default!;
            return false;
        }

        values = new DesignerImageValues(
            input.Source.Trim(),
            stretch,
            stretchDirection,
            interpolationMode,
            edgeMode,
            blendingMode);
        error = string.Empty;
        return true;
    }

    public static void Capture(Control control, IDictionary<string, string> properties)
    {
        if (!TryRead(control, out var values, out _))
        {
            return;
        }

        if (values.Source.Length == 0)
        {
            properties.Remove("Source");
        }
        else
        {
            properties["Source"] = values.Source;
        }

        foreach (var attribute in GetAxamlAttributes(control))
        {
            properties[attribute.Name] = attribute.Value;
        }
    }

    public static void Apply(Image image, IReadOnlyDictionary<string, string> properties)
    {
        if (!TryRead(image, out var current, out _))
        {
            return;
        }

        var input = new DesignerImageEditorInput(
            Get(properties, "Source", current.Source),
            Get(properties, "Stretch", current.Stretch.ToString()),
            Get(
                properties,
                "StretchDirection",
                current.StretchDirection.ToString()),
            Get(
                properties,
                "RenderOptions.BitmapInterpolationMode",
                current.BitmapInterpolationMode.ToString()),
            Get(properties, "RenderOptions.EdgeMode", current.EdgeMode.ToString()),
            Get(
                properties,
                "RenderOptions.BitmapBlendingMode",
                current.BitmapBlendingMode.ToString()));
        if (TryParseValues(image, input, out var values, out _))
        {
            if (!string.Equals(
                    current.Source,
                    values.Source,
                    StringComparison.Ordinal))
            {
                TrySetSource(
                    image,
                    values.Source,
                    retainSourceOnFailure: true,
                    out _);
            }

            ApplyRendering(image, values);
        }
    }

    public static bool TryApply(
        Image image,
        DesignerImageValues values,
        bool retainSourceOnFailure,
        out string error)
    {
        var currentSource = image.Tag?.ToString() ?? string.Empty;
        if (!string.Equals(currentSource, values.Source, StringComparison.Ordinal)
            && !TrySetSource(
                image,
                values.Source,
                retainSourceOnFailure,
                out error))
        {
            return false;
        }

        ApplyRendering(image, values);
        error = string.Empty;
        return true;
    }

    public static bool TrySetSource(
        Image image,
        string source,
        bool retainSourceOnFailure,
        out string error)
    {
        var normalized = source.Trim();
        if (normalized.Length == 0)
        {
            DisposeSource(image);
            image.Source = null;
            image.Tag = null;
            error = string.Empty;
            return true;
        }

        if (!TryResolveLocalPath(normalized, out var path)
            || !File.Exists(path))
        {
            if (retainSourceOnFailure)
            {
                DisposeSource(image);
                image.Source = null;
                image.Tag = normalized;
            }

            error = "Image source must be an existing local file or file URI.";
            return false;
        }

        try
        {
            var bitmap = new Bitmap(path);
            DisposeSource(image);
            image.Source = bitmap;
            image.Tag = normalized;
            error = string.Empty;
            return true;
        }
        catch
        {
            if (retainSourceOnFailure)
            {
                DisposeSource(image);
                image.Source = null;
                image.Tag = normalized;
            }

            error = "The selected file could not be decoded as an image.";
            return false;
        }
    }

    public static bool IsSupportedProperty(string tagName, string propertyName)
        => string.Equals(tagName.Trim(), "Image", StringComparison.OrdinalIgnoreCase)
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
        if (!string.Equals(tagName.Trim(), "Image", StringComparison.OrdinalIgnoreCase)
            || canonicalName.Length == 0)
        {
            error = $"{tagName}.{propertyName} is not a supported image property.";
            return false;
        }

        switch (canonicalName)
        {
            case "Stretch":
                return TryNormalizeEnum<Stretch>(
                    rawValue,
                    canonicalName,
                    out normalizedValue,
                    out error);
            case "StretchDirection":
                return TryNormalizeEnum<StretchDirection>(
                    rawValue,
                    canonicalName,
                    out normalizedValue,
                    out error);
            case "RenderOptions.BitmapInterpolationMode":
                return TryNormalizeEnum<BitmapInterpolationMode>(
                    rawValue,
                    canonicalName,
                    out normalizedValue,
                    out error);
            case "RenderOptions.EdgeMode":
                return TryNormalizeEnum<EdgeMode>(
                    rawValue,
                    canonicalName,
                    out normalizedValue,
                    out error);
            case "RenderOptions.BitmapBlendingMode":
                return TryNormalizeEnum<BitmapBlendingMode>(
                    rawValue,
                    canonicalName,
                    out normalizedValue,
                    out error);
            default:
                normalizedValue = rawValue.Trim();
                error = string.Empty;
                return true;
        }
    }

    public static IReadOnlyList<DesignerImageAttribute> GetAxamlAttributes(
        Control control)
    {
        if (!TryRead(control, out var values, out _))
        {
            return [];
        }

        var attributes = new List<DesignerImageAttribute>();
        if (values.Source.Length > 0)
        {
            attributes.Add(new("Source", values.Source));
        }

        attributes.Add(new("Stretch", values.Stretch.ToString()));
        attributes.Add(new("StretchDirection", values.StretchDirection.ToString()));
        attributes.Add(new(
            "RenderOptions.BitmapInterpolationMode",
            values.BitmapInterpolationMode.ToString()));
        attributes.Add(new("RenderOptions.EdgeMode", values.EdgeMode.ToString()));
        attributes.Add(new(
            "RenderOptions.BitmapBlendingMode",
            values.BitmapBlendingMode.ToString()));
        return attributes;
    }

    private static bool TryResolveLocalPath(string source, out string path)
    {
        try
        {
            if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
            {
                path = uri.IsFile ? uri.LocalPath : string.Empty;
                return uri.IsFile;
            }

            path = Path.GetFullPath(source);
            return true;
        }
        catch
        {
            path = string.Empty;
            return false;
        }
    }

    private static void DisposeSource(Image image)
    {
        if (image.Source is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private static void ApplyRendering(Image image, DesignerImageValues values)
    {
        image.Stretch = values.Stretch;
        image.StretchDirection = values.StretchDirection;
        RenderOptions.SetBitmapInterpolationMode(
            image,
            values.BitmapInterpolationMode);
        RenderOptions.SetEdgeMode(image, values.EdgeMode);
        RenderOptions.SetBitmapBlendingMode(image, values.BitmapBlendingMode);
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
    {
        foreach (var pair in properties)
        {
            if (string.Equals(pair.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        return fallback;
    }
}
