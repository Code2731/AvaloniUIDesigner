using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace AvaloniaUIDesigner.App.Designer.Services;

public enum DesignerRangeControlKind
{
    Slider,
    ProgressBar,
    NumericUpDown,
}

public sealed record DesignerRangeValues(
    DesignerRangeControlKind Kind,
    double Minimum,
    double Maximum,
    double Value,
    double SmallChange,
    double LargeChange,
    Orientation Orientation,
    bool IsDirectionReversed,
    double TickFrequency,
    TickPlacement TickPlacement,
    bool IsSnapToTickEnabled,
    bool IsIndeterminate,
    bool ShowProgressText,
    string ProgressTextFormat,
    decimal NumericMinimum,
    decimal NumericMaximum,
    decimal? NumericValue,
    decimal Increment,
    string FormatString,
    bool ClipValueToMinMax,
    bool AllowSpin,
    bool ShowButtonSpinner,
    Location ButtonSpinnerLocation);

public sealed record DesignerRangeEditorInput(
    string Minimum,
    string Maximum,
    string Value,
    string SmallChange,
    string LargeChange,
    string Orientation,
    bool IsDirectionReversed,
    string TickFrequency,
    string TickPlacement,
    bool IsSnapToTickEnabled,
    bool IsIndeterminate,
    bool ShowProgressText,
    string ProgressTextFormat,
    string Increment,
    string FormatString,
    bool ClipValueToMinMax,
    bool AllowSpin,
    bool ShowButtonSpinner,
    string ButtonSpinnerLocation);

public sealed record DesignerRangeAttribute(string Name, string Value);

public static class DesignerRangeRuntime
{
    private static readonly string[] SliderProperties =
    [
        "Minimum",
        "Maximum",
        "Value",
        "SmallChange",
        "LargeChange",
        "Orientation",
        "IsDirectionReversed",
        "TickFrequency",
        "TickPlacement",
        "IsSnapToTickEnabled",
    ];

    private static readonly string[] ProgressBarProperties =
    [
        "Minimum",
        "Maximum",
        "Value",
        "Orientation",
        "IsIndeterminate",
        "ShowProgressText",
        "ProgressTextFormat",
    ];

    private static readonly string[] NumericUpDownProperties =
    [
        "Minimum",
        "Maximum",
        "Value",
        "Increment",
        "FormatString",
        "ClipValueToMinMax",
        "AllowSpin",
        "ShowButtonSpinner",
        "ButtonSpinnerLocation",
    ];

    public static IReadOnlyList<string> OrientationNames { get; } = Enum.GetNames<Orientation>();

    public static IReadOnlyList<string> TickPlacementNames { get; } = Enum.GetNames<TickPlacement>();

    public static IReadOnlyList<string> SpinnerLocationNames { get; } = Enum.GetNames<Location>();

    public static bool IsSupportedControl(Control control)
        => control is Slider or ProgressBar or NumericUpDown;

    public static bool TryRead(
        Control control,
        out DesignerRangeValues values,
        out string error)
    {
        switch (control)
        {
            case Slider slider:
                values = CreateDefaults(DesignerRangeControlKind.Slider) with
                {
                    Minimum = slider.Minimum,
                    Maximum = slider.Maximum,
                    Value = slider.Value,
                    SmallChange = slider.SmallChange,
                    LargeChange = slider.LargeChange,
                    Orientation = slider.Orientation,
                    IsDirectionReversed = slider.IsDirectionReversed,
                    TickFrequency = slider.TickFrequency,
                    TickPlacement = slider.TickPlacement,
                    IsSnapToTickEnabled = slider.IsSnapToTickEnabled,
                };
                error = string.Empty;
                return true;
            case ProgressBar progressBar:
                values = CreateDefaults(DesignerRangeControlKind.ProgressBar) with
                {
                    Minimum = progressBar.Minimum,
                    Maximum = progressBar.Maximum,
                    Value = progressBar.Value,
                    Orientation = progressBar.Orientation,
                    IsIndeterminate = progressBar.IsIndeterminate,
                    ShowProgressText = progressBar.ShowProgressText,
                    ProgressTextFormat = progressBar.ProgressTextFormat,
                };
                error = string.Empty;
                return true;
            case NumericUpDown numeric:
                values = CreateDefaults(DesignerRangeControlKind.NumericUpDown) with
                {
                    NumericMinimum = numeric.Minimum,
                    NumericMaximum = numeric.Maximum,
                    NumericValue = numeric.Value,
                    Increment = numeric.Increment,
                    FormatString = numeric.FormatString ?? string.Empty,
                    ClipValueToMinMax = numeric.ClipValueToMinMax,
                    AllowSpin = numeric.AllowSpin,
                    ShowButtonSpinner = numeric.ShowButtonSpinner,
                    ButtonSpinnerLocation = numeric.ButtonSpinnerLocation,
                };
                error = string.Empty;
                return true;
            default:
                values = default!;
                error = "Range and value editing is available for Slider, ProgressBar, and NumericUpDown.";
                return false;
        }
    }

    public static bool TryParseValues(
        Control control,
        DesignerRangeEditorInput input,
        out DesignerRangeValues values,
        out string error)
    {
        if (control is NumericUpDown)
        {
            return TryParseNumericValues(input, out values, out error);
        }

        if (control is not (Slider or ProgressBar))
        {
            values = default!;
            error = "Range and value editing is available for Slider, ProgressBar, and NumericUpDown.";
            return false;
        }

        if (!TryParseFinite(input.Minimum, "Minimum", out var minimum, out error)
            || !TryParseFinite(input.Maximum, "Maximum", out var maximum, out error)
            || !TryParseFinite(input.Value, "Value", out var value, out error))
        {
            values = default!;
            return false;
        }

        if (minimum >= maximum)
        {
            values = default!;
            error = "Minimum must be less than Maximum.";
            return false;
        }

        if (value < minimum || value > maximum)
        {
            values = default!;
            error = "Value must be between Minimum and Maximum.";
            return false;
        }

        if (!Enum.TryParse<Orientation>(input.Orientation.Trim(), true, out var orientation)
            || !Enum.IsDefined(orientation))
        {
            values = default!;
            error = $"Orientation must be one of: {string.Join(", ", OrientationNames)}.";
            return false;
        }

        var kind = control is Slider
            ? DesignerRangeControlKind.Slider
            : DesignerRangeControlKind.ProgressBar;
        var parsed = CreateDefaults(kind) with
        {
            Minimum = minimum,
            Maximum = maximum,
            Value = value,
            Orientation = orientation,
        };

        if (control is Slider)
        {
            if (!TryParsePositive(input.SmallChange, "Small change", out var smallChange, out error)
                || !TryParsePositive(input.LargeChange, "Large change", out var largeChange, out error)
                || !TryParseNonNegative(input.TickFrequency, "Tick frequency", out var tickFrequency, out error))
            {
                values = default!;
                return false;
            }

            if (!Enum.TryParse<TickPlacement>(input.TickPlacement.Trim(), true, out var tickPlacement)
                || !Enum.IsDefined(tickPlacement))
            {
                values = default!;
                error = $"Tick placement must be one of: {string.Join(", ", TickPlacementNames)}.";
                return false;
            }

            values = parsed with
            {
                SmallChange = smallChange,
                LargeChange = largeChange,
                IsDirectionReversed = input.IsDirectionReversed,
                TickFrequency = tickFrequency,
                TickPlacement = tickPlacement,
                IsSnapToTickEnabled = input.IsSnapToTickEnabled,
            };
            error = string.Empty;
            return true;
        }

        if (!TryValidateCompositeFormat(input.ProgressTextFormat, out error))
        {
            values = default!;
            return false;
        }

        values = parsed with
        {
            IsIndeterminate = input.IsIndeterminate,
            ShowProgressText = input.ShowProgressText,
            ProgressTextFormat = input.ProgressTextFormat,
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

        foreach (var attribute in GetAxamlAttributes(values, escapeMarkupLiterals: false))
        {
            properties[attribute.Name] = attribute.Value;
        }
    }

    public static void Apply(Control control, IReadOnlyDictionary<string, string> properties)
    {
        if (!TryCreateInput(control, properties, out var input)
            || !TryParseValues(control, input, out var values, out _))
        {
            return;
        }

        Apply(control, values);
    }

    public static void Apply(Control control, DesignerRangeValues values)
    {
        switch (control)
        {
            case Slider slider when values.Kind == DesignerRangeControlKind.Slider:
                ApplyRange(slider, values.Minimum, values.Maximum, values.Value);
                slider.SmallChange = values.SmallChange;
                slider.LargeChange = values.LargeChange;
                slider.Orientation = values.Orientation;
                slider.IsDirectionReversed = values.IsDirectionReversed;
                slider.TickFrequency = values.TickFrequency;
                slider.TickPlacement = values.TickPlacement;
                slider.IsSnapToTickEnabled = values.IsSnapToTickEnabled;
                break;
            case ProgressBar progressBar when values.Kind == DesignerRangeControlKind.ProgressBar:
                ApplyRange(progressBar, values.Minimum, values.Maximum, values.Value);
                progressBar.Orientation = values.Orientation;
                progressBar.IsIndeterminate = values.IsIndeterminate;
                progressBar.ShowProgressText = values.ShowProgressText;
                progressBar.ProgressTextFormat = values.ProgressTextFormat;
                break;
            case NumericUpDown numeric when values.Kind == DesignerRangeControlKind.NumericUpDown:
                ApplyNumericRange(
                    numeric,
                    values.NumericMinimum,
                    values.NumericMaximum,
                    values.NumericValue);
                numeric.Increment = values.Increment;
                numeric.FormatString = values.FormatString;
                numeric.ClipValueToMinMax = values.ClipValueToMinMax;
                numeric.AllowSpin = values.AllowSpin;
                numeric.ShowButtonSpinner = values.ShowButtonSpinner;
                numeric.ButtonSpinnerLocation = values.ButtonSpinnerLocation;
                break;
        }
    }

    public static bool IsSupportedProperty(string tagName, string propertyName)
    {
        var properties = GetPropertyNames(tagName);
        return Array.Exists(
            properties,
            candidate => string.Equals(candidate, propertyName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

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
            candidate => string.Equals(candidate, propertyName.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? string.Empty;
        normalizedValue = string.Empty;
        if (canonicalName.Length == 0)
        {
            error = $"{tagName}.{propertyName} is not a supported range property.";
            return false;
        }

        if (string.Equals(tagName, "NumericUpDown", StringComparison.OrdinalIgnoreCase)
            && canonicalName is "Minimum" or "Maximum" or "Value" or "Increment")
        {
            if (canonicalName == "Value" && string.IsNullOrWhiteSpace(rawValue))
            {
                normalizedValue = string.Empty;
                error = string.Empty;
                return true;
            }

            if (!decimal.TryParse(
                    rawValue.Trim(),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var decimalValue))
            {
                error = $"{canonicalName} must be a decimal number.";
                return false;
            }

            if (canonicalName == "Increment" && decimalValue <= 0)
            {
                error = "Increment must be greater than zero.";
                return false;
            }

            normalizedValue = Format(decimalValue);
            error = string.Empty;
            return true;
        }

        switch (canonicalName)
        {
            case "Minimum":
            case "Maximum":
            case "Value":
            case "SmallChange":
            case "LargeChange":
            case "TickFrequency":
                if (!TryParseFinite(rawValue, canonicalName, out var number, out error))
                {
                    return false;
                }

                if (canonicalName is "SmallChange" or "LargeChange" && number <= 0)
                {
                    error = $"{canonicalName} must be greater than zero.";
                    return false;
                }

                if (canonicalName == "TickFrequency" && number < 0)
                {
                    error = "TickFrequency must be zero or greater.";
                    return false;
                }

                normalizedValue = Format(number);
                error = string.Empty;
                return true;
            case "Orientation":
                return TryNormalizeEnum<Orientation>(rawValue, canonicalName, out normalizedValue, out error);
            case "TickPlacement":
                return TryNormalizeEnum<TickPlacement>(rawValue, canonicalName, out normalizedValue, out error);
            case "ButtonSpinnerLocation":
                return TryNormalizeEnum<Location>(rawValue, canonicalName, out normalizedValue, out error);
            case "IsDirectionReversed":
            case "IsSnapToTickEnabled":
            case "IsIndeterminate":
            case "ShowProgressText":
            case "ClipValueToMinMax":
            case "AllowSpin":
            case "ShowButtonSpinner":
                if (!bool.TryParse(rawValue.Trim(), out var boolean))
                {
                    error = $"{canonicalName} must be True or False.";
                    return false;
                }

                normalizedValue = boolean.ToString();
                error = string.Empty;
                return true;
            case "ProgressTextFormat":
                var progressFormat = UnescapeMarkupLiteral(rawValue);
                if (!TryValidateCompositeFormat(progressFormat, out error))
                {
                    return false;
                }

                normalizedValue = progressFormat;
                return true;
            case "FormatString":
                var numericFormat = UnescapeMarkupLiteral(rawValue);
                if (!TryValidateNumericFormat(numericFormat, out error))
                {
                    return false;
                }

                normalizedValue = numericFormat;
                return true;
            default:
                error = $"{tagName}.{propertyName} is not a supported range property.";
                return false;
        }
    }

    public static bool TryValidateProperties(
        string tagName,
        IReadOnlyDictionary<string, string> properties,
        out string error)
    {
        if (string.Equals(tagName, "NumericUpDown", StringComparison.OrdinalIgnoreCase))
        {
            var minimum = TryGetValue(properties, "Minimum", out var rawMinimum)
                && decimal.TryParse(rawMinimum, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedMinimum)
                    ? parsedMinimum
                    : decimal.MinValue;
            var maximum = TryGetValue(properties, "Maximum", out var rawMaximum)
                && decimal.TryParse(rawMaximum, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedMaximum)
                    ? parsedMaximum
                    : decimal.MaxValue;
            if (minimum >= maximum)
            {
                error = "Minimum must be less than Maximum.";
                return false;
            }

            if (TryGetValue(properties, "Value", out var rawValue)
                && !string.IsNullOrWhiteSpace(rawValue)
                && decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                && (value < minimum || value > maximum))
            {
                error = "Value must be between Minimum and Maximum.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        if (!string.Equals(tagName, "Slider", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(tagName, "ProgressBar", StringComparison.OrdinalIgnoreCase))
        {
            error = string.Empty;
            return true;
        }

        var rangeMinimum = TryGetValue(properties, "Minimum", out var rangeRawMinimum)
            && double.TryParse(rangeRawMinimum, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedRangeMinimum)
                ? parsedRangeMinimum
                : 0;
        var rangeMaximum = TryGetValue(properties, "Maximum", out var rangeRawMaximum)
            && double.TryParse(rangeRawMaximum, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedRangeMaximum)
                ? parsedRangeMaximum
                : 100;
        if (rangeMinimum >= rangeMaximum)
        {
            error = "Minimum must be less than Maximum.";
            return false;
        }

        if (TryGetValue(properties, "Value", out var rangeRawValue)
            && double.TryParse(rangeRawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var rangeValue)
            && (rangeValue < rangeMinimum || rangeValue > rangeMaximum))
        {
            error = "Value must be between Minimum and Maximum.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static void RemoveProperties(string tagName, IDictionary<string, string> properties)
    {
        foreach (var propertyName in GetPropertyNames(tagName))
        {
            properties.Remove(propertyName);
        }
    }

    public static IReadOnlyList<DesignerRangeAttribute> GetAxamlAttributes(Control control)
        => TryRead(control, out var values, out _)
            ? GetAxamlAttributes(values, escapeMarkupLiterals: true)
            : [];

    public static string FormatAxamlAttributeValue(string propertyName, string value)
        => propertyName is "ProgressTextFormat" or "FormatString"
            && value.StartsWith('{')
                ? "{}" + value
                : value;

    private static IReadOnlyList<DesignerRangeAttribute> GetAxamlAttributes(
        DesignerRangeValues values,
        bool escapeMarkupLiterals)
    {
        var attributes = new List<DesignerRangeAttribute>();
        if (values.Kind is DesignerRangeControlKind.Slider or DesignerRangeControlKind.ProgressBar)
        {
            attributes.Add(new("Minimum", Format(values.Minimum)));
            attributes.Add(new("Maximum", Format(values.Maximum)));
            attributes.Add(new("Value", Format(values.Value)));
            attributes.Add(new("Orientation", values.Orientation.ToString()));
        }

        if (values.Kind == DesignerRangeControlKind.Slider)
        {
            attributes.Add(new("SmallChange", Format(values.SmallChange)));
            attributes.Add(new("LargeChange", Format(values.LargeChange)));
            attributes.Add(new("IsDirectionReversed", values.IsDirectionReversed.ToString()));
            attributes.Add(new("TickFrequency", Format(values.TickFrequency)));
            attributes.Add(new("TickPlacement", values.TickPlacement.ToString()));
            attributes.Add(new("IsSnapToTickEnabled", values.IsSnapToTickEnabled.ToString()));
        }
        else if (values.Kind == DesignerRangeControlKind.ProgressBar)
        {
            attributes.Add(new("IsIndeterminate", values.IsIndeterminate.ToString()));
            attributes.Add(new("ShowProgressText", values.ShowProgressText.ToString()));
            attributes.Add(new(
                "ProgressTextFormat",
                escapeMarkupLiterals
                    ? FormatAxamlAttributeValue("ProgressTextFormat", values.ProgressTextFormat)
                    : values.ProgressTextFormat));
        }
        else
        {
            attributes.Add(new("Minimum", Format(values.NumericMinimum)));
            attributes.Add(new("Maximum", Format(values.NumericMaximum)));
            if (values.NumericValue is { } numericValue)
            {
                attributes.Add(new("Value", Format(numericValue)));
            }

            attributes.Add(new("Increment", Format(values.Increment)));
            attributes.Add(new(
                "FormatString",
                escapeMarkupLiterals
                    ? FormatAxamlAttributeValue("FormatString", values.FormatString)
                    : values.FormatString));
            attributes.Add(new("ClipValueToMinMax", values.ClipValueToMinMax.ToString()));
            attributes.Add(new("AllowSpin", values.AllowSpin.ToString()));
            attributes.Add(new("ShowButtonSpinner", values.ShowButtonSpinner.ToString()));
            attributes.Add(new("ButtonSpinnerLocation", values.ButtonSpinnerLocation.ToString()));
        }

        return attributes;
    }

    private static bool TryParseNumericValues(
        DesignerRangeEditorInput input,
        out DesignerRangeValues values,
        out string error)
    {
        if (!decimal.TryParse(input.Minimum.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var minimum)
            || !decimal.TryParse(input.Maximum.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var maximum))
        {
            values = default!;
            error = "Minimum and Maximum must be decimal numbers.";
            return false;
        }

        if (minimum >= maximum)
        {
            values = default!;
            error = "Minimum must be less than Maximum.";
            return false;
        }

        decimal? value = null;
        if (!string.IsNullOrWhiteSpace(input.Value))
        {
            if (!decimal.TryParse(input.Value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedValue))
            {
                values = default!;
                error = "Value must be a decimal number or blank.";
                return false;
            }

            value = parsedValue;
            if (value < minimum || value > maximum)
            {
                values = default!;
                error = "Value must be between Minimum and Maximum.";
                return false;
            }
        }

        if (!decimal.TryParse(input.Increment.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var increment)
            || increment <= 0)
        {
            values = default!;
            error = "Increment must be a decimal number greater than zero.";
            return false;
        }

        if (!TryValidateNumericFormat(input.FormatString, out error))
        {
            values = default!;
            return false;
        }

        if (!Enum.TryParse<Location>(
                input.ButtonSpinnerLocation.Trim(),
                true,
                out var spinnerLocation)
            || !Enum.IsDefined(spinnerLocation))
        {
            values = default!;
            error = $"Button spinner location must be one of: {string.Join(", ", SpinnerLocationNames)}.";
            return false;
        }

        values = CreateDefaults(DesignerRangeControlKind.NumericUpDown) with
        {
            NumericMinimum = minimum,
            NumericMaximum = maximum,
            NumericValue = value,
            Increment = increment,
            FormatString = input.FormatString,
            ClipValueToMinMax = input.ClipValueToMinMax,
            AllowSpin = input.AllowSpin,
            ShowButtonSpinner = input.ShowButtonSpinner,
            ButtonSpinnerLocation = spinnerLocation,
        };
        error = string.Empty;
        return true;
    }

    private static bool TryCreateInput(
        Control control,
        IReadOnlyDictionary<string, string> properties,
        out DesignerRangeEditorInput input)
    {
        if (!TryRead(control, out var current, out _))
        {
            input = default!;
            return false;
        }

        var isNumeric = control is NumericUpDown;
        var minimum = Get(
            properties,
            "Minimum",
            isNumeric ? Format(current.NumericMinimum) : Format(current.Minimum));
        var maximum = Get(
            properties,
            "Maximum",
            isNumeric ? Format(current.NumericMaximum) : Format(current.Maximum));
        string value;
        if (TryGetValue(properties, "Value", out var explicitValue))
        {
            value = explicitValue;
        }
        else if (isNumeric
            && decimal.TryParse(minimum, NumberStyles.Number, CultureInfo.InvariantCulture, out var numericMinimum)
            && decimal.TryParse(maximum, NumberStyles.Number, CultureInfo.InvariantCulture, out var numericMaximum))
        {
            value = current.NumericValue is { } numericValue
                ? Format(Math.Clamp(numericValue, numericMinimum, numericMaximum))
                : string.Empty;
        }
        else if (double.TryParse(minimum, NumberStyles.Float, CultureInfo.InvariantCulture, out var rangeMinimum)
            && double.TryParse(maximum, NumberStyles.Float, CultureInfo.InvariantCulture, out var rangeMaximum))
        {
            value = Format(Math.Clamp(current.Value, rangeMinimum, rangeMaximum));
        }
        else
        {
            value = isNumeric
                ? current.NumericValue is { } numericValue ? Format(numericValue) : string.Empty
                : Format(current.Value);
        }

        input = new DesignerRangeEditorInput(
            minimum,
            maximum,
            value,
            Get(properties, "SmallChange", Format(current.SmallChange)),
            Get(properties, "LargeChange", Format(current.LargeChange)),
            Get(properties, "Orientation", current.Orientation.ToString()),
            GetBoolean(properties, "IsDirectionReversed", current.IsDirectionReversed),
            Get(properties, "TickFrequency", Format(current.TickFrequency)),
            Get(properties, "TickPlacement", current.TickPlacement.ToString()),
            GetBoolean(properties, "IsSnapToTickEnabled", current.IsSnapToTickEnabled),
            GetBoolean(properties, "IsIndeterminate", current.IsIndeterminate),
            GetBoolean(properties, "ShowProgressText", current.ShowProgressText),
            Get(properties, "ProgressTextFormat", current.ProgressTextFormat),
            Get(properties, "Increment", Format(current.Increment)),
            Get(properties, "FormatString", current.FormatString),
            GetBoolean(properties, "ClipValueToMinMax", current.ClipValueToMinMax),
            GetBoolean(properties, "AllowSpin", current.AllowSpin),
            GetBoolean(properties, "ShowButtonSpinner", current.ShowButtonSpinner),
            Get(properties, "ButtonSpinnerLocation", current.ButtonSpinnerLocation.ToString()));
        return true;
    }

    private static DesignerRangeValues CreateDefaults(DesignerRangeControlKind kind)
        => new(
            kind,
            0,
            100,
            0,
            1,
            10,
            Orientation.Horizontal,
            false,
            0,
            TickPlacement.None,
            false,
            false,
            false,
            "{1:0}%",
            decimal.MinValue,
            decimal.MaxValue,
            null,
            1,
            string.Empty,
            false,
            true,
            true,
            Location.Right);

    private static void ApplyRange(RangeBase control, double minimum, double maximum, double value)
    {
        if (minimum > control.Maximum)
        {
            control.Maximum = maximum;
        }
        else if (maximum < control.Minimum)
        {
            control.Minimum = minimum;
        }

        control.Minimum = minimum;
        control.Maximum = maximum;
        control.Value = value;
    }

    private static void ApplyNumericRange(
        NumericUpDown control,
        decimal minimum,
        decimal maximum,
        decimal? value)
    {
        if (minimum > control.Maximum)
        {
            control.Maximum = maximum;
        }
        else if (maximum < control.Minimum)
        {
            control.Minimum = minimum;
        }

        control.Minimum = minimum;
        control.Maximum = maximum;
        control.Value = value;
    }

    private static bool TryParseFinite(
        string raw,
        string label,
        out double value,
        out string error)
    {
        if (!double.TryParse(
                raw.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value)
            || !double.IsFinite(value))
        {
            error = $"{label} must be a finite number.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryParsePositive(
        string raw,
        string label,
        out double value,
        out string error)
    {
        if (!TryParseFinite(raw, label, out value, out error) || value <= 0)
        {
            error = $"{label} must be greater than zero.";
            return false;
        }

        return true;
    }

    private static bool TryParseNonNegative(
        string raw,
        string label,
        out double value,
        out string error)
    {
        if (!TryParseFinite(raw, label, out value, out error) || value < 0)
        {
            error = $"{label} must be zero or greater.";
            return false;
        }

        return true;
    }

    private static bool TryValidateCompositeFormat(string value, out string error)
    {
        try
        {
            _ = string.Format(CultureInfo.InvariantCulture, value, 50d, 50d);
            error = string.Empty;
            return true;
        }
        catch (FormatException)
        {
            error = "Progress text format must be a valid composite format such as {1:0}%.";
            return false;
        }
    }

    private static bool TryValidateNumericFormat(string value, out string error)
    {
        try
        {
            _ = 123.45m.ToString(value, CultureInfo.InvariantCulture);
            error = string.Empty;
            return true;
        }
        catch (FormatException)
        {
            error = "Numeric format must be blank or a valid .NET number format such as N2.";
            return false;
        }
    }

    private static bool TryNormalizeEnum<T>(
        string rawValue,
        string label,
        out string normalizedValue,
        out string error)
        where T : struct, Enum
    {
        if (!Enum.TryParse<T>(rawValue.Trim(), true, out var value)
            || !Enum.IsDefined(value))
        {
            normalizedValue = string.Empty;
            error = $"{label} must be one of: {string.Join(", ", Enum.GetNames<T>())}.";
            return false;
        }

        normalizedValue = value.ToString();
        error = string.Empty;
        return true;
    }

    private static string UnescapeMarkupLiteral(string value)
        => value.StartsWith("{}", StringComparison.Ordinal)
            ? value[2..]
            : value;

    private static string[] GetPropertyNames(string tagName)
        => tagName.Trim().ToLowerInvariant() switch
        {
            "slider" => SliderProperties,
            "progressbar" => ProgressBarProperties,
            "numericupdown" => NumericUpDownProperties,
            _ => [],
        };

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

    private static string Format(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Format(decimal value)
        => value.ToString(CultureInfo.InvariantCulture);
}
