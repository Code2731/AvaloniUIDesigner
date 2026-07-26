using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using CalendarControl = Avalonia.Controls.Calendar;

namespace AvaloniaUIDesigner.App.Designer.Services;

public enum DesignerDateTimeControlKind
{
    DatePicker,
    CalendarDatePicker,
    Calendar,
    TimePicker,
}

public sealed record DesignerDateTimeValues(
    DesignerDateTimeControlKind Kind,
    DateTimeOffset? SelectedDate,
    DateTimeOffset MinYear,
    DateTimeOffset MaxYear,
    bool DayVisible,
    bool MonthVisible,
    bool YearVisible,
    string DayFormat,
    string MonthFormat,
    string YearFormat,
    DateTime? CalendarSelectedDate,
    DateTime DisplayDate,
    DateTime? DisplayDateStart,
    DateTime? DisplayDateEnd,
    DayOfWeek FirstDayOfWeek,
    bool IsTodayHighlighted,
    CalendarDatePickerFormat SelectedDateFormat,
    string CustomDateFormatString,
    string Watermark,
    bool UseFloatingWatermark,
    HorizontalAlignment HorizontalContentAlignment,
    VerticalAlignment VerticalContentAlignment,
    TimeSpan? SelectedTime,
    int MinuteIncrement,
    int SecondIncrement,
    string ClockIdentifier,
    bool UseSeconds,
    CalendarSelectionMode CalendarSelectionMode,
    CalendarMode CalendarDisplayMode,
    bool AllowTapRangeSelection);

public sealed record DesignerDateTimeEditorInput(
    string SelectedDate,
    string MinYear,
    string MaxYear,
    bool DayVisible,
    bool MonthVisible,
    bool YearVisible,
    string DayFormat,
    string MonthFormat,
    string YearFormat,
    string DisplayDate,
    string DisplayDateStart,
    string DisplayDateEnd,
    string FirstDayOfWeek,
    bool IsTodayHighlighted,
    string SelectedDateFormat,
    string CustomDateFormatString,
    string Watermark,
    bool UseFloatingWatermark,
    string HorizontalContentAlignment,
    string VerticalContentAlignment,
    string SelectedTime,
    string MinuteIncrement,
    string SecondIncrement,
    string ClockIdentifier,
    bool UseSeconds,
    string CalendarSelectionMode,
    string CalendarDisplayMode,
    bool AllowTapRangeSelection);

public sealed record DesignerDateTimeAttribute(string Name, string Value);

public static class DesignerDateTimeRuntime
{
    private const string DateFormat = "yyyy-MM-dd";
    private static readonly string[] DatePickerProperties =
    [
        "SelectedDate",
        "MinYear",
        "MaxYear",
        "DayVisible",
        "MonthVisible",
        "YearVisible",
        "DayFormat",
        "MonthFormat",
        "YearFormat",
    ];

    private static readonly string[] CalendarDatePickerProperties =
    [
        "SelectedDate",
        "DisplayDate",
        "DisplayDateStart",
        "DisplayDateEnd",
        "FirstDayOfWeek",
        "IsTodayHighlighted",
        "SelectedDateFormat",
        "CustomDateFormatString",
        "Watermark",
        "UseFloatingWatermark",
        "HorizontalContentAlignment",
        "VerticalContentAlignment",
    ];

    private static readonly string[] CalendarProperties =
    [
        "SelectedDate",
        "DisplayDate",
        "DisplayDateStart",
        "DisplayDateEnd",
        "FirstDayOfWeek",
        "IsTodayHighlighted",
        "SelectionMode",
        "DisplayMode",
        "AllowTapRangeSelection",
    ];

    private static readonly string[] TimePickerProperties =
    [
        "SelectedTime",
        "MinuteIncrement",
        "SecondIncrement",
        "ClockIdentifier",
        "UseSeconds",
    ];

    public static IReadOnlyList<string> DayOfWeekNames { get; } = Enum.GetNames<DayOfWeek>();

    public static IReadOnlyList<string> CalendarDateFormatNames { get; } =
        Enum.GetNames<CalendarDatePickerFormat>();

    public static IReadOnlyList<string> CalendarSelectionModeNames { get; } =
        Enum.GetNames<CalendarSelectionMode>();

    public static IReadOnlyList<string> CalendarDisplayModeNames { get; } =
        Enum.GetNames<CalendarMode>();

    public static IReadOnlyList<string> HorizontalAlignmentNames { get; } =
        Enum.GetNames<HorizontalAlignment>();

    public static IReadOnlyList<string> VerticalAlignmentNames { get; } =
        Enum.GetNames<VerticalAlignment>();

    public static IReadOnlyList<string> ClockIdentifiers { get; } =
        ["12HourClock", "24HourClock"];

    public static bool IsSupportedControl(Control control)
        => control is DatePicker or CalendarDatePicker or CalendarControl
            or TimePicker;

    public static bool TryRead(
        Control control,
        out DesignerDateTimeValues values,
        out string error)
    {
        switch (control)
        {
            case DatePicker datePicker:
                values = CreateDefaults(DesignerDateTimeControlKind.DatePicker) with
                {
                    SelectedDate = datePicker.SelectedDate,
                    MinYear = datePicker.MinYear,
                    MaxYear = datePicker.MaxYear,
                    DayVisible = datePicker.DayVisible,
                    MonthVisible = datePicker.MonthVisible,
                    YearVisible = datePicker.YearVisible,
                    DayFormat = datePicker.DayFormat,
                    MonthFormat = datePicker.MonthFormat,
                    YearFormat = datePicker.YearFormat,
                };
                error = string.Empty;
                return true;
            case CalendarDatePicker calendar:
                values = CreateDefaults(DesignerDateTimeControlKind.CalendarDatePicker) with
                {
                    CalendarSelectedDate = calendar.SelectedDate,
                    DisplayDate = calendar.DisplayDate,
                    DisplayDateStart = calendar.DisplayDateStart,
                    DisplayDateEnd = calendar.DisplayDateEnd,
                    FirstDayOfWeek = calendar.FirstDayOfWeek,
                    IsTodayHighlighted = calendar.IsTodayHighlighted,
                    SelectedDateFormat = calendar.SelectedDateFormat,
                    CustomDateFormatString = calendar.CustomDateFormatString,
                    Watermark = calendar.Watermark ?? string.Empty,
                    UseFloatingWatermark = calendar.UseFloatingWatermark,
                    HorizontalContentAlignment = calendar.HorizontalContentAlignment,
                    VerticalContentAlignment = calendar.VerticalContentAlignment,
                };
                error = string.Empty;
                return true;
            case CalendarControl calendar:
                values = CreateDefaults(DesignerDateTimeControlKind.Calendar) with
                {
                    CalendarSelectedDate = calendar.SelectedDate,
                    DisplayDate = calendar.DisplayDate,
                    DisplayDateStart = calendar.DisplayDateStart,
                    DisplayDateEnd = calendar.DisplayDateEnd,
                    FirstDayOfWeek = calendar.FirstDayOfWeek,
                    IsTodayHighlighted = calendar.IsTodayHighlighted,
                    CalendarSelectionMode = calendar.SelectionMode,
                    CalendarDisplayMode = calendar.DisplayMode,
                    AllowTapRangeSelection = calendar.AllowTapRangeSelection,
                };
                error = string.Empty;
                return true;
            case TimePicker timePicker:
                values = CreateDefaults(DesignerDateTimeControlKind.TimePicker) with
                {
                    SelectedTime = timePicker.SelectedTime,
                    MinuteIncrement = timePicker.MinuteIncrement,
                    SecondIncrement = timePicker.SecondIncrement,
                    ClockIdentifier = timePicker.ClockIdentifier,
                    UseSeconds = timePicker.UseSeconds,
                };
                error = string.Empty;
                return true;
            default:
                values = default!;
                error = "Date and time editing is available for DatePicker, CalendarDatePicker, Calendar, and TimePicker controls.";
                return false;
        }
    }

    public static bool TryParseValues(
        Control control,
        DesignerDateTimeEditorInput input,
        out DesignerDateTimeValues values,
        out string error)
    {
        if (!TryRead(control, out var current, out error))
        {
            values = default!;
            return false;
        }

        switch (control)
        {
            case DatePicker:
                return TryParseDatePicker(input, current, out values, out error);
            case CalendarDatePicker:
                return TryParseCalendarDatePicker(input, current, out values, out error);
            case CalendarControl:
                return TryParseCalendar(input, current, out values, out error);
            case TimePicker:
                return TryParseTimePicker(input, current, out values, out error);
            default:
                values = default!;
                error = "Date and time editing is available for DatePicker, CalendarDatePicker, Calendar, and TimePicker controls.";
                return false;
        }
    }

    public static void Capture(Control control, IDictionary<string, string> properties)
    {
        foreach (var attribute in GetAxamlAttributes(control, escapeMarkupLiterals: false))
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

        var input = new DesignerDateTimeEditorInput(
            control is CalendarDatePicker or CalendarControl
                ? FormatDate(current.CalendarSelectedDate)
                : FormatDate(current.SelectedDate),
            FormatDate(current.MinYear),
            FormatDate(current.MaxYear),
            GetBoolean(properties, "DayVisible", current.DayVisible),
            GetBoolean(properties, "MonthVisible", current.MonthVisible),
            GetBoolean(properties, "YearVisible", current.YearVisible),
            Get(properties, "DayFormat", current.DayFormat),
            Get(properties, "MonthFormat", current.MonthFormat),
            Get(properties, "YearFormat", current.YearFormat),
            Get(properties, "DisplayDate", FormatDate(current.DisplayDate)),
            Get(properties, "DisplayDateStart", FormatDate(current.DisplayDateStart)),
            Get(properties, "DisplayDateEnd", FormatDate(current.DisplayDateEnd)),
            Get(properties, "FirstDayOfWeek", current.FirstDayOfWeek.ToString()),
            GetBoolean(properties, "IsTodayHighlighted", current.IsTodayHighlighted),
            Get(properties, "SelectedDateFormat", current.SelectedDateFormat.ToString()),
            Get(properties, "CustomDateFormatString", current.CustomDateFormatString),
            Get(properties, "Watermark", current.Watermark),
            GetBoolean(properties, "UseFloatingWatermark", current.UseFloatingWatermark),
            Get(
                properties,
                "HorizontalContentAlignment",
                current.HorizontalContentAlignment.ToString()),
            Get(
                properties,
                "VerticalContentAlignment",
                current.VerticalContentAlignment.ToString()),
            Get(properties, "SelectedTime", FormatTime(current.SelectedTime)),
            Get(
                properties,
                "MinuteIncrement",
                current.MinuteIncrement.ToString(CultureInfo.InvariantCulture)),
            Get(
                properties,
                "SecondIncrement",
                current.SecondIncrement.ToString(CultureInfo.InvariantCulture)),
            Get(properties, "ClockIdentifier", current.ClockIdentifier),
            GetBoolean(properties, "UseSeconds", current.UseSeconds),
            Get(
                properties,
                "SelectionMode",
                current.CalendarSelectionMode.ToString()),
            Get(
                properties,
                "DisplayMode",
                current.CalendarDisplayMode.ToString()),
            GetBoolean(
                properties,
                "AllowTapRangeSelection",
                current.AllowTapRangeSelection));

        input = input with
        {
            SelectedDate = Get(properties, "SelectedDate", input.SelectedDate),
            MinYear = Get(properties, "MinYear", input.MinYear),
            MaxYear = Get(properties, "MaxYear", input.MaxYear),
        };
        if (TryParseValues(control, input, out var values, out _))
        {
            Apply(control, values);
        }
    }

    public static void Apply(Control control, DesignerDateTimeValues values)
    {
        switch (control)
        {
            case DatePicker datePicker when values.Kind == DesignerDateTimeControlKind.DatePicker:
                datePicker.SelectedDate = null;
                ApplyDatePickerRange(datePicker, values.MinYear, values.MaxYear);
                datePicker.DayVisible = values.DayVisible;
                datePicker.MonthVisible = values.MonthVisible;
                datePicker.YearVisible = values.YearVisible;
                datePicker.DayFormat = values.DayFormat;
                datePicker.MonthFormat = values.MonthFormat;
                datePicker.YearFormat = values.YearFormat;
                datePicker.SelectedDate = values.SelectedDate;
                break;
            case CalendarDatePicker calendar when values.Kind == DesignerDateTimeControlKind.CalendarDatePicker:
                calendar.SelectedDate = null;
                calendar.DisplayDateStart = null;
                calendar.DisplayDateEnd = null;
                calendar.DisplayDateStart = values.DisplayDateStart;
                calendar.DisplayDateEnd = values.DisplayDateEnd;
                calendar.DisplayDate = values.DisplayDate;
                calendar.FirstDayOfWeek = values.FirstDayOfWeek;
                calendar.IsTodayHighlighted = values.IsTodayHighlighted;
                calendar.SelectedDateFormat = values.SelectedDateFormat;
                calendar.CustomDateFormatString = values.CustomDateFormatString;
                calendar.Watermark = values.Watermark;
                calendar.UseFloatingWatermark = values.UseFloatingWatermark;
                calendar.HorizontalContentAlignment = values.HorizontalContentAlignment;
                calendar.VerticalContentAlignment = values.VerticalContentAlignment;
                calendar.SelectedDate = values.CalendarSelectedDate;
                break;
            case CalendarControl calendar
                when values.Kind == DesignerDateTimeControlKind.Calendar:
                calendar.SelectedDate = null;
                calendar.DisplayDateStart = null;
                calendar.DisplayDateEnd = null;
                calendar.DisplayDateStart = values.DisplayDateStart;
                calendar.DisplayDateEnd = values.DisplayDateEnd;
                calendar.DisplayDate = values.DisplayDate;
                calendar.FirstDayOfWeek = values.FirstDayOfWeek;
                calendar.IsTodayHighlighted = values.IsTodayHighlighted;
                calendar.DisplayMode = values.CalendarDisplayMode;
                calendar.AllowTapRangeSelection = values.AllowTapRangeSelection;
                calendar.SelectionMode = values.CalendarSelectionMode;
                if (values.CalendarSelectionMode != CalendarSelectionMode.None)
                {
                    calendar.SelectedDate = values.CalendarSelectedDate;
                }

                break;
            case TimePicker timePicker when values.Kind == DesignerDateTimeControlKind.TimePicker:
                timePicker.MinuteIncrement = values.MinuteIncrement;
                timePicker.SecondIncrement = values.SecondIncrement;
                timePicker.ClockIdentifier = values.ClockIdentifier;
                timePicker.UseSeconds = values.UseSeconds;
                timePicker.SelectedTime = values.SelectedTime;
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
            error = $"{tagName}.{propertyName} is not a supported date/time property.";
            return false;
        }

        switch (canonicalName)
        {
            case "SelectedDate":
            case "DisplayDateStart":
            case "DisplayDateEnd":
                if (!TryParseNullableDate(rawValue, canonicalName, out var nullableDate, out error))
                {
                    return false;
                }

                normalizedValue = FormatDate(nullableDate);
                return true;
            case "MinYear":
            case "MaxYear":
            case "DisplayDate":
                if (!TryParseDate(rawValue, canonicalName, out var date, out error))
                {
                    return false;
                }

                normalizedValue = FormatDate(date);
                return true;
            case "SelectedTime":
                if (!TryParseNullableTime(rawValue, out var selectedTime, out error))
                {
                    return false;
                }

                normalizedValue = FormatTime(selectedTime);
                return true;
            case "DayVisible":
            case "MonthVisible":
            case "YearVisible":
            case "IsTodayHighlighted":
            case "UseFloatingWatermark":
            case "UseSeconds":
            case "AllowTapRangeSelection":
                if (!bool.TryParse(rawValue.Trim(), out var boolean))
                {
                    error = $"{canonicalName} must be True or False.";
                    return false;
                }

                normalizedValue = boolean.ToString();
                error = string.Empty;
                return true;
            case "MinuteIncrement":
            case "SecondIncrement":
                if (!TryParseIncrement(rawValue, canonicalName, out var increment, out error))
                {
                    return false;
                }

                normalizedValue = increment.ToString(CultureInfo.InvariantCulture);
                return true;
            case "FirstDayOfWeek":
                return TryNormalizeEnum<DayOfWeek>(
                    rawValue,
                    canonicalName,
                    out normalizedValue,
                    out error);
            case "SelectedDateFormat":
                return TryNormalizeEnum<CalendarDatePickerFormat>(
                    rawValue,
                    canonicalName,
                    out normalizedValue,
                    out error);
            case "SelectionMode":
                return TryNormalizeEnum<CalendarSelectionMode>(
                    rawValue,
                    canonicalName,
                    out normalizedValue,
                    out error);
            case "DisplayMode":
                return TryNormalizeEnum<CalendarMode>(
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
            case "ClockIdentifier":
                if (!ClockIdentifiers.Contains(rawValue.Trim(), StringComparer.Ordinal))
                {
                    error = "ClockIdentifier must be 12HourClock or 24HourClock.";
                    return false;
                }

                normalizedValue = rawValue.Trim();
                error = string.Empty;
                return true;
            case "DayFormat":
            case "MonthFormat":
            case "YearFormat":
            case "CustomDateFormatString":
                var format = UnescapeMarkupLiteral(rawValue);
                if (!TryValidateDateFormat(format, canonicalName, out error))
                {
                    return false;
                }

                normalizedValue = format;
                return true;
            case "Watermark":
                normalizedValue = rawValue;
                error = string.Empty;
                return true;
            default:
                error = $"{tagName}.{propertyName} is not a supported date/time property.";
                return false;
        }
    }

    public static bool TryValidateProperties(
        string tagName,
        IReadOnlyDictionary<string, string> properties,
        out string error)
    {
        if (string.Equals(tagName, "DatePicker", StringComparison.OrdinalIgnoreCase))
        {
            var defaults = CreateDefaults(DesignerDateTimeControlKind.DatePicker);
            var minYear = ReadDate(properties, "MinYear", defaults.MinYear);
            var maxYear = ReadDate(properties, "MaxYear", defaults.MaxYear);
            var selectedDate = ReadNullableDate(properties, "SelectedDate");
            if (minYear > maxYear)
            {
                error = "MinYear must not be later than MaxYear.";
                return false;
            }

            if (selectedDate is { } selected && (selected < minYear || selected > maxYear))
            {
                error = "SelectedDate must be between MinYear and MaxYear.";
                return false;
            }

            var dayVisible = ReadBoolean(properties, "DayVisible", true);
            var monthVisible = ReadBoolean(properties, "MonthVisible", true);
            var yearVisible = ReadBoolean(properties, "YearVisible", true);
            if (!dayVisible && !monthVisible && !yearVisible)
            {
                error = "At least one of DayVisible, MonthVisible, or YearVisible must be enabled.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        if (string.Equals(
                tagName,
                "CalendarDatePicker",
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                tagName,
                "Calendar",
                StringComparison.OrdinalIgnoreCase))
        {
            var start = ReadNullableDate(properties, "DisplayDateStart");
            var end = ReadNullableDate(properties, "DisplayDateEnd");
            var selected = ReadNullableDate(properties, "SelectedDate");
            var displayDate = ReadNullableDate(properties, "DisplayDate");
            if (start is { } rangeStart && end is { } rangeEnd && rangeStart > rangeEnd)
            {
                error = "DisplayDateStart must not be later than DisplayDateEnd.";
                return false;
            }

            if (selected is { } selectedDate
                && (start is { } startDate && selectedDate < startDate
                    || end is { } endDate && selectedDate > endDate))
            {
                error = "SelectedDate must be inside the display date range.";
                return false;
            }

            if (displayDate is { } displayed
                && (start is { } displayStart && displayed < displayStart
                    || end is { } displayEnd && displayed > displayEnd))
            {
                error = "DisplayDate must be inside the display date range.";
                return false;
            }

            if (string.Equals(
                    tagName,
                    "Calendar",
                    StringComparison.OrdinalIgnoreCase)
                && TryGetValue(
                    properties,
                    "SelectionMode",
                    out var rawSelectionMode)
                && Enum.TryParse<CalendarSelectionMode>(
                    rawSelectionMode,
                    true,
                    out var selectionMode)
                && selectionMode == CalendarSelectionMode.None
                && selected is not null)
            {
                error =
                    "SelectedDate must be empty when SelectionMode is None.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        error = string.Empty;
        return true;
    }

    public static void RemoveConstraintProperties(
        string tagName,
        IDictionary<string, string> properties)
    {
        var names = string.Equals(tagName, "DatePicker", StringComparison.OrdinalIgnoreCase)
            ? new[] { "SelectedDate", "MinYear", "MaxYear", "DayVisible", "MonthVisible", "YearVisible" }
            : string.Equals(
                    tagName,
                    "CalendarDatePicker",
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    tagName,
                    "Calendar",
                    StringComparison.OrdinalIgnoreCase)
                ? new[] { "SelectedDate", "DisplayDate", "DisplayDateStart", "DisplayDateEnd" }
                : [];
        foreach (var name in names)
        {
            properties.Remove(name);
        }
    }

    public static IReadOnlyList<DesignerDateTimeAttribute> GetAxamlAttributes(Control control)
        => GetAxamlAttributes(control, escapeMarkupLiterals: true);

    public static string FormatAxamlAttributeValue(string propertyName, string value)
        => propertyName is "DayFormat" or "MonthFormat" or "YearFormat" or "CustomDateFormatString"
            ? EscapeMarkupLiteral(value)
            : value;

    private static IReadOnlyList<DesignerDateTimeAttribute> GetAxamlAttributes(
        Control control,
        bool escapeMarkupLiterals)
    {
        if (!TryRead(control, out var values, out _))
        {
            return [];
        }

        var attributes = new List<DesignerDateTimeAttribute>();
        switch (values.Kind)
        {
            case DesignerDateTimeControlKind.DatePicker:
                AddNullable(attributes, "SelectedDate", FormatDate(values.SelectedDate));
                attributes.Add(new("MinYear", FormatDate(values.MinYear)));
                attributes.Add(new("MaxYear", FormatDate(values.MaxYear)));
                attributes.Add(new("DayVisible", values.DayVisible.ToString()));
                attributes.Add(new("MonthVisible", values.MonthVisible.ToString()));
                attributes.Add(new("YearVisible", values.YearVisible.ToString()));
                attributes.Add(new(
                    "DayFormat",
                    FormatMarkupLiteral(values.DayFormat, escapeMarkupLiterals)));
                attributes.Add(new(
                    "MonthFormat",
                    FormatMarkupLiteral(values.MonthFormat, escapeMarkupLiterals)));
                attributes.Add(new(
                    "YearFormat",
                    FormatMarkupLiteral(values.YearFormat, escapeMarkupLiterals)));
                break;
            case DesignerDateTimeControlKind.CalendarDatePicker:
                AddNullable(
                    attributes,
                    "SelectedDate",
                    FormatDate(values.CalendarSelectedDate));
                attributes.Add(new("DisplayDate", FormatDate(values.DisplayDate)));
                AddNullable(
                    attributes,
                    "DisplayDateStart",
                    FormatDate(values.DisplayDateStart));
                AddNullable(
                    attributes,
                    "DisplayDateEnd",
                    FormatDate(values.DisplayDateEnd));
                attributes.Add(new("FirstDayOfWeek", values.FirstDayOfWeek.ToString()));
                attributes.Add(new("IsTodayHighlighted", values.IsTodayHighlighted.ToString()));
                attributes.Add(new("SelectedDateFormat", values.SelectedDateFormat.ToString()));
                attributes.Add(new(
                    "CustomDateFormatString",
                    FormatMarkupLiteral(values.CustomDateFormatString, escapeMarkupLiterals)));
                attributes.Add(new("Watermark", values.Watermark));
                attributes.Add(new("UseFloatingWatermark", values.UseFloatingWatermark.ToString()));
                attributes.Add(new(
                    "HorizontalContentAlignment",
                    values.HorizontalContentAlignment.ToString()));
                attributes.Add(new(
                    "VerticalContentAlignment",
                    values.VerticalContentAlignment.ToString()));
                break;
            case DesignerDateTimeControlKind.Calendar:
                AddNullable(
                    attributes,
                    "SelectedDate",
                    FormatDate(values.CalendarSelectedDate));
                attributes.Add(new(
                    "DisplayDate",
                    FormatDate(values.DisplayDate)));
                AddNullable(
                    attributes,
                    "DisplayDateStart",
                    FormatDate(values.DisplayDateStart));
                AddNullable(
                    attributes,
                    "DisplayDateEnd",
                    FormatDate(values.DisplayDateEnd));
                attributes.Add(new(
                    "FirstDayOfWeek",
                    values.FirstDayOfWeek.ToString()));
                attributes.Add(new(
                    "IsTodayHighlighted",
                    values.IsTodayHighlighted.ToString()));
                attributes.Add(new(
                    "SelectionMode",
                    values.CalendarSelectionMode.ToString()));
                attributes.Add(new(
                    "DisplayMode",
                    values.CalendarDisplayMode.ToString()));
                attributes.Add(new(
                    "AllowTapRangeSelection",
                    values.AllowTapRangeSelection.ToString()));
                break;
            case DesignerDateTimeControlKind.TimePicker:
                AddNullable(attributes, "SelectedTime", FormatTime(values.SelectedTime));
                attributes.Add(new(
                    "MinuteIncrement",
                    values.MinuteIncrement.ToString(CultureInfo.InvariantCulture)));
                attributes.Add(new(
                    "SecondIncrement",
                    values.SecondIncrement.ToString(CultureInfo.InvariantCulture)));
                attributes.Add(new("ClockIdentifier", values.ClockIdentifier));
                attributes.Add(new("UseSeconds", values.UseSeconds.ToString()));
                break;
        }

        return attributes;
    }

    private static bool TryParseDatePicker(
        DesignerDateTimeEditorInput input,
        DesignerDateTimeValues current,
        out DesignerDateTimeValues values,
        out string error)
    {
        if (!TryParseNullableDate(input.SelectedDate, "Selected date", out var selectedDate, out error)
            || !TryParseDate(input.MinYear, "Minimum year", out var minYear, out error)
            || !TryParseDate(input.MaxYear, "Maximum year", out var maxYear, out error))
        {
            values = default!;
            return false;
        }

        if (minYear > maxYear)
        {
            values = default!;
            error = "Minimum year must not be later than maximum year.";
            return false;
        }

        if (selectedDate is { } selected && (selected < minYear || selected > maxYear))
        {
            values = default!;
            error = "Selected date must be between minimum and maximum year.";
            return false;
        }

        if (!input.DayVisible && !input.MonthVisible && !input.YearVisible)
        {
            values = default!;
            error = "At least one date component must remain visible.";
            return false;
        }

        if (!TryValidateDateFormat(input.DayFormat, "Day format", out error)
            || !TryValidateDateFormat(input.MonthFormat, "Month format", out error)
            || !TryValidateDateFormat(input.YearFormat, "Year format", out error))
        {
            values = default!;
            return false;
        }

        values = current with
        {
            SelectedDate = selectedDate is { } date
                ? new DateTimeOffset(date, TimeZoneInfo.Local.GetUtcOffset(date))
                : null,
            MinYear = new DateTimeOffset(minYear, TimeZoneInfo.Local.GetUtcOffset(minYear)),
            MaxYear = new DateTimeOffset(maxYear, TimeZoneInfo.Local.GetUtcOffset(maxYear)),
            DayVisible = input.DayVisible,
            MonthVisible = input.MonthVisible,
            YearVisible = input.YearVisible,
            DayFormat = input.DayFormat,
            MonthFormat = input.MonthFormat,
            YearFormat = input.YearFormat,
        };
        error = string.Empty;
        return true;
    }

    private static bool TryParseCalendarDatePicker(
        DesignerDateTimeEditorInput input,
        DesignerDateTimeValues current,
        out DesignerDateTimeValues values,
        out string error)
    {
        if (!TryParseNullableDate(input.SelectedDate, "Selected date", out var selectedDate, out error)
            || !TryParseDate(input.DisplayDate, "Display date", out var displayDate, out error)
            || !TryParseNullableDate(input.DisplayDateStart, "Display date start", out var start, out error)
            || !TryParseNullableDate(input.DisplayDateEnd, "Display date end", out var end, out error))
        {
            values = default!;
            return false;
        }

        if (start is { } startDate && end is { } endDate && startDate > endDate)
        {
            values = default!;
            error = "Display date start must not be later than display date end.";
            return false;
        }

        if (selectedDate is { } selected
            && (start is { } rangeStart && selected < rangeStart
                || end is { } rangeEnd && selected > rangeEnd))
        {
            values = default!;
            error = "Selected date must be inside the display date range.";
            return false;
        }

        if (displayDate < (start ?? DateTime.MinValue)
            || displayDate > (end ?? DateTime.MaxValue))
        {
            values = default!;
            error = "Display date must be inside the display date range.";
            return false;
        }

        if (!TryParseEnum(input.FirstDayOfWeek, "First day of week", out DayOfWeek firstDay, out error)
            || !TryParseEnum(
                input.SelectedDateFormat,
                "Selected date format",
                out CalendarDatePickerFormat dateFormat,
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

        if (dateFormat == CalendarDatePickerFormat.Custom
            && !TryValidateDateFormat(
                input.CustomDateFormatString,
                "Custom date format",
                out error))
        {
            values = default!;
            return false;
        }

        values = current with
        {
            CalendarSelectedDate = selectedDate,
            DisplayDate = displayDate,
            DisplayDateStart = start,
            DisplayDateEnd = end,
            FirstDayOfWeek = firstDay,
            IsTodayHighlighted = input.IsTodayHighlighted,
            SelectedDateFormat = dateFormat,
            CustomDateFormatString = input.CustomDateFormatString,
            Watermark = input.Watermark,
            UseFloatingWatermark = input.UseFloatingWatermark,
            HorizontalContentAlignment = horizontalAlignment,
            VerticalContentAlignment = verticalAlignment,
        };
        error = string.Empty;
        return true;
    }

    private static bool TryParseTimePicker(
        DesignerDateTimeEditorInput input,
        DesignerDateTimeValues current,
        out DesignerDateTimeValues values,
        out string error)
    {
        if (!TryParseNullableTime(input.SelectedTime, out var selectedTime, out error)
            || !TryParseIncrement(
                input.MinuteIncrement,
                "Minute increment",
                out var minuteIncrement,
                out error)
            || !TryParseIncrement(
                input.SecondIncrement,
                "Second increment",
                out var secondIncrement,
                out error))
        {
            values = default!;
            return false;
        }

        var clockIdentifier = input.ClockIdentifier.Trim();
        if (!ClockIdentifiers.Contains(clockIdentifier, StringComparer.Ordinal))
        {
            values = default!;
            error = "Clock identifier must be 12HourClock or 24HourClock.";
            return false;
        }

        values = current with
        {
            SelectedTime = selectedTime,
            MinuteIncrement = minuteIncrement,
            SecondIncrement = secondIncrement,
            ClockIdentifier = clockIdentifier,
            UseSeconds = input.UseSeconds,
        };
        error = string.Empty;
        return true;
    }

    private static bool TryParseCalendar(
        DesignerDateTimeEditorInput input,
        DesignerDateTimeValues current,
        out DesignerDateTimeValues values,
        out string error)
    {
        if (!TryParseNullableDate(
                input.SelectedDate,
                "Selected date",
                out var selectedDate,
                out error)
            || !TryParseDate(
                input.DisplayDate,
                "Display date",
                out var displayDate,
                out error)
            || !TryParseNullableDate(
                input.DisplayDateStart,
                "Display date start",
                out var start,
                out error)
            || !TryParseNullableDate(
                input.DisplayDateEnd,
                "Display date end",
                out var end,
                out error))
        {
            values = default!;
            return false;
        }

        if (start is { } startDate
            && end is { } endDate
            && startDate > endDate)
        {
            values = default!;
            error =
                "Display date start must not be later than display date end.";
            return false;
        }

        if (selectedDate is { } selected
            && (start is { } rangeStart && selected < rangeStart
                || end is { } rangeEnd && selected > rangeEnd))
        {
            values = default!;
            error = "Selected date must be inside the display date range.";
            return false;
        }

        if (displayDate < (start ?? DateTime.MinValue)
            || displayDate > (end ?? DateTime.MaxValue))
        {
            values = default!;
            error = "Display date must be inside the display date range.";
            return false;
        }

        if (!TryParseEnum(
                input.FirstDayOfWeek,
                "First day of week",
                out DayOfWeek firstDay,
                out error)
            || !TryParseEnum(
                input.CalendarSelectionMode,
                "Selection mode",
                out CalendarSelectionMode selectionMode,
                out error)
            || !TryParseEnum(
                input.CalendarDisplayMode,
                "Display mode",
                out CalendarMode displayMode,
                out error))
        {
            values = default!;
            return false;
        }

        if (selectionMode == CalendarSelectionMode.None
            && selectedDate is not null)
        {
            values = default!;
            error =
                "Selected date must be empty when selection mode is None.";
            return false;
        }

        values = current with
        {
            CalendarSelectedDate = selectedDate,
            DisplayDate = displayDate,
            DisplayDateStart = start,
            DisplayDateEnd = end,
            FirstDayOfWeek = firstDay,
            IsTodayHighlighted = input.IsTodayHighlighted,
            CalendarSelectionMode = selectionMode,
            CalendarDisplayMode = displayMode,
            AllowTapRangeSelection = input.AllowTapRangeSelection,
        };
        error = string.Empty;
        return true;
    }

    private static DesignerDateTimeValues CreateDefaults(DesignerDateTimeControlKind kind)
    {
        var today = DateTime.Today;
        var minYear = new DateTime(today.Year - 100, 1, 1);
        var maxYear = new DateTime(today.Year + 100, 12, 31);
        return new DesignerDateTimeValues(
            kind,
            SelectedDate: null,
            MinYear: new DateTimeOffset(minYear, TimeZoneInfo.Local.GetUtcOffset(minYear)),
            MaxYear: new DateTimeOffset(maxYear, TimeZoneInfo.Local.GetUtcOffset(maxYear)),
            DayVisible: true,
            MonthVisible: true,
            YearVisible: true,
            DayFormat: "%d",
            MonthFormat: "MMMM",
            YearFormat: "yyyy",
            CalendarSelectedDate: null,
            DisplayDate: today,
            DisplayDateStart: null,
            DisplayDateEnd: null,
            FirstDayOfWeek: DayOfWeek.Sunday,
            IsTodayHighlighted: false,
            SelectedDateFormat: CalendarDatePickerFormat.Short,
            CustomDateFormatString: "d",
            Watermark: string.Empty,
            UseFloatingWatermark: false,
            HorizontalContentAlignment: HorizontalAlignment.Stretch,
            VerticalContentAlignment: VerticalAlignment.Stretch,
            SelectedTime: null,
            MinuteIncrement: 1,
            SecondIncrement: 1,
            ClockIdentifier: "12HourClock",
            UseSeconds: false,
            CalendarSelectionMode: CalendarSelectionMode.SingleDate,
            CalendarDisplayMode: CalendarMode.Month,
            AllowTapRangeSelection: true);
    }

    private static void ApplyDatePickerRange(
        DatePicker datePicker,
        DateTimeOffset minYear,
        DateTimeOffset maxYear)
    {
        if (minYear > datePicker.MaxYear)
        {
            datePicker.MaxYear = maxYear;
            datePicker.MinYear = minYear;
        }
        else if (maxYear < datePicker.MinYear)
        {
            datePicker.MinYear = minYear;
            datePicker.MaxYear = maxYear;
        }
        else
        {
            datePicker.MinYear = minYear;
            datePicker.MaxYear = maxYear;
        }
    }

    private static bool TryParseDate(
        string rawValue,
        string label,
        out DateTime value,
        out string error)
    {
        if (!DateTime.TryParseExact(
                rawValue.Trim(),
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out value))
        {
            error = $"{label} must use yyyy-MM-dd.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryParseNullableDate(
        string rawValue,
        string label,
        out DateTime? value,
        out string error)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            value = null;
            error = string.Empty;
            return true;
        }

        if (!TryParseDate(rawValue, label, out var date, out error))
        {
            value = null;
            return false;
        }

        value = date;
        return true;
    }

    private static bool TryParseNullableTime(
        string rawValue,
        out TimeSpan? value,
        out string error)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            value = null;
            error = string.Empty;
            return true;
        }

        if (!TimeSpan.TryParseExact(
                rawValue.Trim(),
                ["h\\:mm", "hh\\:mm", "h\\:mm\\:ss", "hh\\:mm\\:ss"],
                CultureInfo.InvariantCulture,
                out var time)
            || time < TimeSpan.Zero
            || time >= TimeSpan.FromDays(1))
        {
            value = null;
            error = "Selected time must use HH:mm or HH:mm:ss within a single day.";
            return false;
        }

        value = time;
        error = string.Empty;
        return true;
    }

    private static bool TryParseIncrement(
        string rawValue,
        string label,
        out int value,
        out string error)
    {
        if (!int.TryParse(
                rawValue.Trim(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out value)
            || value is < 1 or > 59)
        {
            error = $"{label} must be a whole number from 1 to 59.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryValidateDateFormat(
        string format,
        string label,
        out string error)
    {
        if (string.IsNullOrEmpty(format))
        {
            error = $"{label} must not be empty.";
            return false;
        }

        try
        {
            _ = new DateTime(2026, 7, 27).ToString(format, CultureInfo.InvariantCulture);
            error = string.Empty;
            return true;
        }
        catch (FormatException)
        {
            error = $"{label} is not a valid .NET date format string.";
            return false;
        }
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

    private static string[] GetPropertyNames(string tagName)
        => tagName.Trim().ToUpperInvariant() switch
        {
            "DATEPICKER" => DatePickerProperties,
            "CALENDARDATEPICKER" => CalendarDatePickerProperties,
            "CALENDAR" => CalendarProperties,
            "TIMEPICKER" => TimePickerProperties,
            _ => [],
        };

    private static void AddNullable(
        ICollection<DesignerDateTimeAttribute> attributes,
        string name,
        string value)
    {
        if (value.Length > 0)
        {
            attributes.Add(new(name, value));
        }
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

    private static DateTimeOffset ReadDate(
        IReadOnlyDictionary<string, string> properties,
        string propertyName,
        DateTimeOffset fallback)
        => TryGetValue(properties, propertyName, out var rawValue)
            && DateTime.TryParseExact(
                rawValue,
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date)
            ? new DateTimeOffset(date, TimeZoneInfo.Local.GetUtcOffset(date))
            : fallback;

    private static DateTimeOffset? ReadNullableDate(
        IReadOnlyDictionary<string, string> properties,
        string propertyName)
        => TryGetValue(properties, propertyName, out var rawValue)
            && DateTime.TryParseExact(
                rawValue,
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date)
            ? new DateTimeOffset(date, TimeZoneInfo.Local.GetUtcOffset(date))
            : null;

    private static bool ReadBoolean(
        IReadOnlyDictionary<string, string> properties,
        string propertyName,
        bool fallback)
        => TryGetValue(properties, propertyName, out var rawValue)
            && bool.TryParse(rawValue, out var value)
                ? value
                : fallback;

    private static string FormatDate(DateTimeOffset? value)
        => value?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? string.Empty;

    private static string FormatDate(DateTime? value)
        => value?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? string.Empty;

    private static string FormatTime(TimeSpan? value)
        => value?.ToString("hh\\:mm\\:ss", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string FormatMarkupLiteral(string value, bool escape)
        => escape ? EscapeMarkupLiteral(value) : value;

    private static string EscapeMarkupLiteral(string value)
        => value.StartsWith('{') ? "{}" + value : value;

    private static string UnescapeMarkupLiteral(string value)
        => value.StartsWith("{}", StringComparison.Ordinal) ? value[2..] : value;
}
