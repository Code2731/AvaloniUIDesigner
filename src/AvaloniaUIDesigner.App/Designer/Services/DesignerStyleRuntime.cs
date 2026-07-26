using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Models;

namespace AvaloniaUIDesigner.App.Designer.Services;

public static class DesignerStyleRuntime
{
    private static readonly string[] BrushProperties = ["Background", "Foreground", "BorderBrush"];

    public static bool IsSupportedProperty(string targetType, string propertyName)
    {
        if (propertyName == "Opacity")
        {
            return true;
        }

        if (targetType == "TextBlock")
        {
            return propertyName is "Background" or "Foreground" or "FontSize" or "FontWeight";
        }

        if (targetType == "Border")
        {
            return propertyName is "Background" or "BorderBrush" or "BorderThickness" or "CornerRadius";
        }

        return SupportsTemplatedControl(targetType)
            && propertyName is "Background" or "Foreground" or "BorderBrush" or "BorderThickness"
                or "CornerRadius" or "FontSize" or "FontWeight";
    }

    public static bool IsBrushProperty(string propertyName)
        => BrushProperties.Contains(propertyName, StringComparer.Ordinal);

    public static bool HasLocalValue(Control control, string propertyName)
        => GetProperty(control, propertyName) is { } property
            && control.IsSet(property)
            && !DesignerStyleApplicationMetadata.IsApplied(control, propertyName);

    public static bool IsSupportedPseudoClass(string targetType, string pseudoClass)
        => pseudoClass switch
        {
            "pointerover" or "disabled" or "focus" or "focus-visible" => true,
            "pressed" => targetType is "Button" or "ToggleButton" or "CheckBox" or "RadioButton",
            "checked" or "unchecked" => targetType is "ToggleButton" or "CheckBox" or "RadioButton" or "ToggleSwitch",
            "expanded" or "collapsed" => targetType == "Expander",
            _ => false,
        };

    public static void ApplyStyles(
        Control control,
        IReadOnlyList<DesignerStyleDefinition>? styles,
        IReadOnlyDictionary<string, string>? colorResources,
        IEnumerable<string>? simulatedPseudoClasses = null)
    {
        DesignerStyleApplicationMetadata.BeginProgrammaticUpdate(control);
        try
        {
            ResetAppliedValues(control);
            if (styles is null || styles.Count == 0)
            {
                return;
            }

            var activePseudoClasses = GetActivePseudoClasses(control, simulatedPseudoClasses);
            foreach (var style in styles.Where(style => style.PseudoClass is null)
                         .Concat(styles.Where(style => style.PseudoClass is not null)))
            {
                if (!Matches(control, style, activePseudoClasses))
                {
                    continue;
                }

                foreach (var setter in style.Setters)
                {
                    if (HasLocalValue(control, setter.Key))
                    {
                        continue;
                    }

                    if (TryApplyValue(control, setter.Key, setter.Value, colorResources))
                    {
                        DesignerStyleApplicationMetadata.MarkApplied(control, setter.Key);
                    }
                }
            }
        }
        finally
        {
            DesignerStyleApplicationMetadata.EndProgrammaticUpdate(control);
        }
    }

    public static void ClearLocalValue(Control control, string propertyName)
    {
        ClearPropertyValue(control, propertyName);
        DesignerStyleApplicationMetadata.ClearApplied(control, propertyName);
        if (IsBrushProperty(propertyName))
        {
            DesignerResourceReferenceMetadata.SetReference(control, propertyName, null);
        }
    }

    public static bool TryReadCurrentValue(Control control, string propertyName, out string value)
    {
        value = propertyName switch
        {
            "Opacity" => control.Opacity.ToString("0.###", CultureInfo.InvariantCulture),
            "Background" when control is TemplatedControl templated && templated.Background is { } brush
                => FormatBrush(brush),
            "Background" when control is Border border && border.Background is { } brush
                => FormatBrush(brush),
            "Background" when control is TextBlock textBlock && textBlock.Background is { } brush
                => FormatBrush(brush),
            "Foreground" when control is TemplatedControl templated && templated.Foreground is { } brush
                => FormatBrush(brush),
            "Foreground" when control is TextBlock textBlock && textBlock.Foreground is { } brush
                => FormatBrush(brush),
            "BorderBrush" when control is TemplatedControl templated && templated.BorderBrush is { } brush
                => FormatBrush(brush),
            "BorderBrush" when control is Border border && border.BorderBrush is { } brush
                => FormatBrush(brush),
            "BorderThickness" when control is TemplatedControl templated => templated.BorderThickness.ToString(),
            "BorderThickness" when control is Border border => border.BorderThickness.ToString(),
            "CornerRadius" when control is TemplatedControl templated => templated.CornerRadius.ToString(),
            "CornerRadius" when control is Border border => border.CornerRadius.ToString(),
            "FontSize" when control is TemplatedControl templated
                => templated.FontSize.ToString("0.###", CultureInfo.InvariantCulture),
            "FontSize" when control is TextBlock textBlock
                => textBlock.FontSize.ToString("0.###", CultureInfo.InvariantCulture),
            "FontWeight" when control is TemplatedControl templated => templated.FontWeight.ToString(),
            "FontWeight" when control is TextBlock textBlock => textBlock.FontWeight.ToString(),
            _ => string.Empty,
        };

        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool Matches(
        Control control,
        DesignerStyleDefinition style,
        IReadOnlySet<string> activePseudoClasses)
        => string.Equals(control.GetType().Name, style.TargetType, StringComparison.Ordinal)
            && control.Classes.Contains(style.ClassName)
            && (style.PseudoClass is null || activePseudoClasses.Contains(style.PseudoClass));

    private static IReadOnlySet<string> GetActivePseudoClasses(
        Control control,
        IEnumerable<string>? simulatedPseudoClasses)
    {
        var active = simulatedPseudoClasses?
            .Where(pseudoClass => !string.IsNullOrWhiteSpace(pseudoClass))
            .ToHashSet(StringComparer.Ordinal)
            ?? new HashSet<string>(StringComparer.Ordinal);

        if (!control.IsEnabled)
        {
            active.Add("disabled");
        }

        if (control.IsPointerOver)
        {
            active.Add("pointerover");
        }

        if (control.IsFocused)
        {
            active.Add("focus");
        }

        if (control is Button { IsPressed: true })
        {
            active.Add("pressed");
        }

        if (control is ToggleButton toggleButton
            && !active.Contains("checked")
            && !active.Contains("unchecked"))
        {
            active.Add(toggleButton.IsChecked == true ? "checked" : "unchecked");
        }

        if (control is Expander expander
            && !active.Contains("expanded")
            && !active.Contains("collapsed"))
        {
            active.Add(expander.IsExpanded ? "expanded" : "collapsed");
        }

        return active;
    }

    private static void ResetAppliedValues(Control control)
    {
        foreach (var propertyName in DesignerStyleApplicationMetadata.GetAppliedProperties(control))
        {
            ClearPropertyValue(control, propertyName);
        }

        DesignerStyleApplicationMetadata.ClearAll(control);
    }

    private static AvaloniaProperty? GetProperty(Control control, string propertyName)
        => propertyName switch
        {
            "Opacity" => Visual.OpacityProperty,
            "Background" when control is TemplatedControl => TemplatedControl.BackgroundProperty,
            "Background" when control is Border => Border.BackgroundProperty,
            "Background" when control is TextBlock => TextBlock.BackgroundProperty,
            "Foreground" when control is TemplatedControl => TemplatedControl.ForegroundProperty,
            "Foreground" when control is TextBlock => TextBlock.ForegroundProperty,
            "BorderBrush" when control is TemplatedControl => TemplatedControl.BorderBrushProperty,
            "BorderBrush" when control is Border => Border.BorderBrushProperty,
            "BorderThickness" when control is TemplatedControl => TemplatedControl.BorderThicknessProperty,
            "BorderThickness" when control is Border => Border.BorderThicknessProperty,
            "CornerRadius" when control is TemplatedControl => TemplatedControl.CornerRadiusProperty,
            "CornerRadius" when control is Border => Border.CornerRadiusProperty,
            "FontSize" when control is TemplatedControl => TemplatedControl.FontSizeProperty,
            "FontSize" when control is TextBlock => TextBlock.FontSizeProperty,
            "FontWeight" when control is TemplatedControl => TemplatedControl.FontWeightProperty,
            "FontWeight" when control is TextBlock => TextBlock.FontWeightProperty,
            _ => null,
        };

    private static void ClearPropertyValue(Control control, string propertyName)
    {
        if (GetProperty(control, propertyName) is { } property)
        {
            control.ClearValue(property);
        }
    }

    private static bool TryApplyValue(
        Control control,
        string propertyName,
        string rawValue,
        IReadOnlyDictionary<string, string>? colorResources)
    {
        try
        {
            switch (propertyName)
            {
                case "Opacity" when double.TryParse(
                    rawValue,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var opacity):
                    control.Opacity = Math.Clamp(opacity, 0, 1);
                    return true;
                case "Background":
                    return TryApplyBrush(control, propertyName, rawValue, colorResources);
                case "Foreground":
                    return TryApplyBrush(control, propertyName, rawValue, colorResources);
                case "BorderBrush":
                    return TryApplyBrush(control, propertyName, rawValue, colorResources);
                case "BorderThickness":
                    return TryApplyBorderThickness(control, Thickness.Parse(rawValue));
                case "CornerRadius":
                    return TryApplyCornerRadius(control, CornerRadius.Parse(rawValue));
                case "FontSize" when double.TryParse(
                    rawValue,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var fontSize):
                    return TryApplyFontSize(control, Math.Clamp(fontSize, 8, 96));
                case "FontWeight":
                    return TryApplyFontWeight(control, ParseFontWeight(rawValue));
                default:
                    return false;
            }
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryApplyBrush(
        Control control,
        string propertyName,
        string rawValue,
        IReadOnlyDictionary<string, string>? colorResources)
    {
        if (DesignerResourceReferenceMetadata.TryParseExpression(rawValue, out var resourceKey))
        {
            if (colorResources is null
                || !colorResources.TryGetValue(resourceKey, out var resourceValue)
                || string.IsNullOrWhiteSpace(resourceValue))
            {
                return false;
            }

            rawValue = resourceValue;
        }

        var brush = Brush.Parse(rawValue);
        switch (propertyName)
        {
            case "Background" when control is TemplatedControl templated:
                templated.Background = brush;
                return true;
            case "Background" when control is Border border:
                border.Background = brush;
                return true;
            case "Background" when control is TextBlock textBlock:
                textBlock.Background = brush;
                return true;
            case "Foreground" when control is TemplatedControl templated:
                templated.Foreground = brush;
                return true;
            case "Foreground" when control is TextBlock textBlock:
                textBlock.Foreground = brush;
                return true;
            case "BorderBrush" when control is TemplatedControl templated:
                templated.BorderBrush = brush;
                return true;
            case "BorderBrush" when control is Border border:
                border.BorderBrush = brush;
                return true;
            default:
                return false;
        }
    }

    private static bool TryApplyBorderThickness(Control control, Thickness thickness)
    {
        switch (control)
        {
            case TemplatedControl templated:
                templated.BorderThickness = thickness;
                return true;
            case Border border:
                border.BorderThickness = thickness;
                return true;
            default:
                return false;
        }
    }

    private static bool TryApplyCornerRadius(Control control, CornerRadius cornerRadius)
    {
        switch (control)
        {
            case TemplatedControl templated:
                templated.CornerRadius = cornerRadius;
                return true;
            case Border border:
                border.CornerRadius = cornerRadius;
                return true;
            default:
                return false;
        }
    }

    private static bool TryApplyFontSize(Control control, double fontSize)
    {
        switch (control)
        {
            case TemplatedControl templated:
                templated.FontSize = fontSize;
                return true;
            case TextBlock textBlock:
                textBlock.FontSize = fontSize;
                return true;
            default:
                return false;
        }
    }

    private static bool TryApplyFontWeight(Control control, FontWeight fontWeight)
    {
        switch (control)
        {
            case TemplatedControl templated:
                templated.FontWeight = fontWeight;
                return true;
            case TextBlock textBlock:
                textBlock.FontWeight = fontWeight;
                return true;
            default:
                return false;
        }
    }

    private static FontWeight ParseFontWeight(string value)
        => value.Trim().ToLowerInvariant() switch
        {
            "normal" or "regular" or "400" => FontWeight.Normal,
            "semibold" or "semi-bold" or "600" => FontWeight.SemiBold,
            "bold" or "700" => FontWeight.Bold,
            _ => throw new FormatException("Unsupported font weight."),
        };

    private static bool SupportsTemplatedControl(string targetType)
        => targetType is "Button" or "TextBox" or "Label" or "CheckBox" or "RadioButton"
            or "ToggleSwitch" or "ToggleButton" or "ComboBox" or "ListBox" or "TreeView" or "Slider"
            or "ProgressBar" or "DatePicker" or "CalendarDatePicker" or "TimePicker"
            or "NumericUpDown" or "TabControl" or "Expander" or "ScrollViewer";

    private static string FormatBrush(IBrush brush)
        => brush is ISolidColorBrush solidColorBrush
            ? $"#{solidColorBrush.Color.A:x2}{solidColorBrush.Color.R:x2}{solidColorBrush.Color.G:x2}{solidColorBrush.Color.B:x2}"
            : brush.ToString() ?? string.Empty;
}
