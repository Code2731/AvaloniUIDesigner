using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerDataGridBehaviorValues(
    bool AutoGenerateColumns,
    bool IsReadOnly,
    bool CanUserReorderColumns,
    bool CanUserResizeColumns,
    bool CanUserSortColumns,
    DataGridHeadersVisibility HeadersVisibility,
    DataGridGridLinesVisibility GridLinesVisibility,
    DataGridSelectionMode SelectionMode,
    DataGridClipboardCopyMode ClipboardCopyMode,
    bool AreRowDetailsFrozen,
    bool AreRowGroupHeadersFrozen,
    bool IsScrollInertiaEnabled,
    int FrozenColumnCount,
    double RowHeight,
    double RowHeaderWidth,
    double ColumnHeaderHeight,
    double MinColumnWidth,
    double MaxColumnWidth,
    DataGridLength ColumnWidth,
    ScrollBarVisibility HorizontalScrollBarVisibility,
    ScrollBarVisibility VerticalScrollBarVisibility);

public sealed record DesignerDataGridBehaviorEditorInput(
    bool AutoGenerateColumns,
    bool IsReadOnly,
    bool CanUserReorderColumns,
    bool CanUserResizeColumns,
    bool CanUserSortColumns,
    string HeadersVisibility,
    string GridLinesVisibility,
    string SelectionMode,
    string ClipboardCopyMode,
    bool AreRowDetailsFrozen,
    bool AreRowGroupHeadersFrozen,
    bool IsScrollInertiaEnabled,
    string FrozenColumnCount,
    string RowHeight,
    string RowHeaderWidth,
    string ColumnHeaderHeight,
    string MinColumnWidth,
    string MaxColumnWidth,
    string ColumnWidth,
    string HorizontalScrollBarVisibility,
    string VerticalScrollBarVisibility);

public sealed record DesignerDataGridBehaviorAttribute(string Name, string Value);

public static class DesignerDataGridBehaviorRuntime
{
    private static readonly string[] PropertyNames =
    [
        "AutoGenerateColumns",
        "IsReadOnly",
        "CanUserReorderColumns",
        "CanUserResizeColumns",
        "CanUserSortColumns",
        "HeadersVisibility",
        "GridLinesVisibility",
        "SelectionMode",
        "ClipboardCopyMode",
        "AreRowDetailsFrozen",
        "AreRowGroupHeadersFrozen",
        "IsScrollInertiaEnabled",
        "FrozenColumnCount",
        "RowHeight",
        "RowHeaderWidth",
        "ColumnHeaderHeight",
        "MinColumnWidth",
        "MaxColumnWidth",
        "ColumnWidth",
        "HorizontalScrollBarVisibility",
        "VerticalScrollBarVisibility",
    ];

    public static IReadOnlyList<string> HeadersVisibilityNames { get; } =
        Enum.GetNames<DataGridHeadersVisibility>();

    public static IReadOnlyList<string> GridLinesVisibilityNames { get; } =
        Enum.GetNames<DataGridGridLinesVisibility>();

    public static IReadOnlyList<string> SelectionModeNames { get; } =
        Enum.GetNames<DataGridSelectionMode>();

    public static IReadOnlyList<string> ClipboardCopyModeNames { get; } =
        Enum.GetNames<DataGridClipboardCopyMode>();

    public static IReadOnlyList<string> ScrollBarVisibilityNames { get; } =
        Enum.GetNames<ScrollBarVisibility>();

    public static bool IsSupportedControl(Control control)
        => control is DataGrid;

    public static bool TryRead(
        Control control,
        out DesignerDataGridBehaviorValues values,
        out string error)
    {
        if (control is not DataGrid dataGrid)
        {
            values = default!;
            error = "DataGrid behavior editing is available for DataGrid controls.";
            return false;
        }

        values = new DesignerDataGridBehaviorValues(
            dataGrid.AutoGenerateColumns,
            dataGrid.IsReadOnly,
            dataGrid.CanUserReorderColumns,
            dataGrid.CanUserResizeColumns,
            dataGrid.CanUserSortColumns,
            dataGrid.HeadersVisibility,
            dataGrid.GridLinesVisibility,
            dataGrid.SelectionMode,
            dataGrid.ClipboardCopyMode,
            dataGrid.AreRowDetailsFrozen,
            dataGrid.AreRowGroupHeadersFrozen,
            dataGrid.IsScrollInertiaEnabled,
            dataGrid.FrozenColumnCount,
            dataGrid.RowHeight,
            dataGrid.RowHeaderWidth,
            dataGrid.ColumnHeaderHeight,
            dataGrid.MinColumnWidth,
            dataGrid.MaxColumnWidth,
            dataGrid.ColumnWidth,
            dataGrid.HorizontalScrollBarVisibility,
            dataGrid.VerticalScrollBarVisibility);
        error = string.Empty;
        return true;
    }

    public static bool TryParseValues(
        Control control,
        DesignerDataGridBehaviorEditorInput input,
        out DesignerDataGridBehaviorValues values,
        out string error)
    {
        if (!TryRead(control, out var current, out error))
        {
            values = default!;
            return false;
        }

        if (!TryParseEnum(
                input.HeadersVisibility,
                "Headers visibility",
                out DataGridHeadersVisibility headersVisibility,
                out error)
            || !TryParseEnum(
                input.GridLinesVisibility,
                "Grid lines visibility",
                out DataGridGridLinesVisibility gridLinesVisibility,
                out error)
            || !TryParseEnum(
                input.SelectionMode,
                "Selection mode",
                out DataGridSelectionMode selectionMode,
                out error)
            || !TryParseEnum(
                input.ClipboardCopyMode,
                "Clipboard copy mode",
                out DataGridClipboardCopyMode clipboardCopyMode,
                out error)
            || !TryParseEnum(
                input.HorizontalScrollBarVisibility,
                "Horizontal scrollbar visibility",
                out ScrollBarVisibility horizontalScrollBarVisibility,
                out error)
            || !TryParseEnum(
                input.VerticalScrollBarVisibility,
                "Vertical scrollbar visibility",
                out ScrollBarVisibility verticalScrollBarVisibility,
                out error)
            || !TryParseNonNegativeInt(
                input.FrozenColumnCount,
                "Frozen column count",
                out var frozenColumnCount,
                out error)
            || !TryParseDouble(
                input.RowHeight,
                "Row height",
                allowNaN: true,
                allowInfinity: false,
                out var rowHeight,
                out error)
            || !TryParseDouble(
                input.RowHeaderWidth,
                "Row header width",
                allowNaN: true,
                allowInfinity: false,
                out var rowHeaderWidth,
                out error)
            || !TryParseDouble(
                input.ColumnHeaderHeight,
                "Column header height",
                allowNaN: true,
                allowInfinity: false,
                out var columnHeaderHeight,
                out error)
            || !TryParseDouble(
                input.MinColumnWidth,
                "Minimum column width",
                allowNaN: false,
                allowInfinity: false,
                out var minColumnWidth,
                out error)
            || !TryParseDouble(
                input.MaxColumnWidth,
                "Maximum column width",
                allowNaN: false,
                allowInfinity: true,
                out var maxColumnWidth,
                out error)
            || !TryParseColumnWidth(
                input.ColumnWidth,
                out var columnWidth,
                out error))
        {
            values = default!;
            return false;
        }

        if (minColumnWidth > maxColumnWidth)
        {
            values = default!;
            error = "Minimum column width must not be greater than maximum column width.";
            return false;
        }

        values = current with
        {
            AutoGenerateColumns = input.AutoGenerateColumns,
            IsReadOnly = input.IsReadOnly,
            CanUserReorderColumns = input.CanUserReorderColumns,
            CanUserResizeColumns = input.CanUserResizeColumns,
            CanUserSortColumns = input.CanUserSortColumns,
            HeadersVisibility = headersVisibility,
            GridLinesVisibility = gridLinesVisibility,
            SelectionMode = selectionMode,
            ClipboardCopyMode = clipboardCopyMode,
            AreRowDetailsFrozen = input.AreRowDetailsFrozen,
            AreRowGroupHeadersFrozen = input.AreRowGroupHeadersFrozen,
            IsScrollInertiaEnabled = input.IsScrollInertiaEnabled,
            FrozenColumnCount = frozenColumnCount,
            RowHeight = rowHeight,
            RowHeaderWidth = rowHeaderWidth,
            ColumnHeaderHeight = columnHeaderHeight,
            MinColumnWidth = minColumnWidth,
            MaxColumnWidth = maxColumnWidth,
            ColumnWidth = columnWidth,
            HorizontalScrollBarVisibility = horizontalScrollBarVisibility,
            VerticalScrollBarVisibility = verticalScrollBarVisibility,
        };
        error = string.Empty;
        return true;
    }

    public static void Capture(
        Control control,
        IDictionary<string, string> properties)
    {
        foreach (var attribute in GetAxamlAttributes(control))
        {
            properties[attribute.Name] = attribute.Value;
        }
    }

    public static void Apply(
        DataGrid dataGrid,
        DesignerDataGridBehaviorValues values)
    {
        dataGrid.AutoGenerateColumns = values.AutoGenerateColumns;
        dataGrid.IsReadOnly = values.IsReadOnly;
        dataGrid.CanUserReorderColumns = values.CanUserReorderColumns;
        dataGrid.CanUserResizeColumns = values.CanUserResizeColumns;
        dataGrid.CanUserSortColumns = values.CanUserSortColumns;
        dataGrid.HeadersVisibility = values.HeadersVisibility;
        dataGrid.GridLinesVisibility = values.GridLinesVisibility;
        dataGrid.SelectionMode = values.SelectionMode;
        dataGrid.ClipboardCopyMode = values.ClipboardCopyMode;
        dataGrid.AreRowDetailsFrozen = values.AreRowDetailsFrozen;
        dataGrid.AreRowGroupHeadersFrozen = values.AreRowGroupHeadersFrozen;
        dataGrid.IsScrollInertiaEnabled = values.IsScrollInertiaEnabled;
        dataGrid.FrozenColumnCount = values.FrozenColumnCount;
        dataGrid.ColumnWidth = values.ColumnWidth;
        if (dataGrid.MaxColumnWidth != values.MaxColumnWidth
            || dataGrid.MinColumnWidth != values.MinColumnWidth)
        {
            ApplyColumnBounds(dataGrid, values.MinColumnWidth, values.MaxColumnWidth);
        }

        dataGrid.RowHeight = values.RowHeight;
        dataGrid.RowHeaderWidth = values.RowHeaderWidth;
        dataGrid.ColumnHeaderHeight = values.ColumnHeaderHeight;
        dataGrid.HorizontalScrollBarVisibility = values.HorizontalScrollBarVisibility;
        dataGrid.VerticalScrollBarVisibility = values.VerticalScrollBarVisibility;
    }

    public static void Apply(
        DataGrid dataGrid,
        IReadOnlyDictionary<string, string> properties)
    {
        if (!TryRead(dataGrid, out var current, out _))
        {
            return;
        }

        var input = new DesignerDataGridBehaviorEditorInput(
            GetBoolean(properties, "AutoGenerateColumns", current.AutoGenerateColumns),
            GetBoolean(properties, "IsReadOnly", current.IsReadOnly),
            GetBoolean(properties, "CanUserReorderColumns", current.CanUserReorderColumns),
            GetBoolean(properties, "CanUserResizeColumns", current.CanUserResizeColumns),
            GetBoolean(properties, "CanUserSortColumns", current.CanUserSortColumns),
            Get(properties, "HeadersVisibility", current.HeadersVisibility.ToString()),
            Get(properties, "GridLinesVisibility", current.GridLinesVisibility.ToString()),
            Get(properties, "SelectionMode", current.SelectionMode.ToString()),
            Get(properties, "ClipboardCopyMode", current.ClipboardCopyMode.ToString()),
            GetBoolean(properties, "AreRowDetailsFrozen", current.AreRowDetailsFrozen),
            GetBoolean(properties, "AreRowGroupHeadersFrozen", current.AreRowGroupHeadersFrozen),
            GetBoolean(properties, "IsScrollInertiaEnabled", current.IsScrollInertiaEnabled),
            Get(properties, "FrozenColumnCount", current.FrozenColumnCount.ToString(CultureInfo.InvariantCulture)),
            Get(properties, "RowHeight", FormatEditorDouble(current.RowHeight)),
            Get(properties, "RowHeaderWidth", FormatEditorDouble(current.RowHeaderWidth)),
            Get(properties, "ColumnHeaderHeight", FormatEditorDouble(current.ColumnHeaderHeight)),
            Get(properties, "MinColumnWidth", FormatEditorDouble(current.MinColumnWidth)),
            Get(properties, "MaxColumnWidth", FormatEditorDouble(current.MaxColumnWidth)),
            Get(properties, "ColumnWidth", FormatColumnWidth(current.ColumnWidth)),
            Get(properties, "HorizontalScrollBarVisibility", current.HorizontalScrollBarVisibility.ToString()),
            Get(properties, "VerticalScrollBarVisibility", current.VerticalScrollBarVisibility.ToString()));

        if (TryParseValues(dataGrid, input, out var values, out _))
        {
            Apply(dataGrid, values);
        }
    }

    public static bool IsSupportedProperty(string tagName, string propertyName)
        => string.Equals(tagName, "DataGrid", StringComparison.OrdinalIgnoreCase)
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
        if (!IsSupportedProperty(tagName, canonicalName))
        {
            error = $"{tagName}.{propertyName} is not a supported DataGrid behavior property.";
            return false;
        }

        switch (canonicalName)
        {
            case "AutoGenerateColumns":
            case "IsReadOnly":
            case "CanUserReorderColumns":
            case "CanUserResizeColumns":
            case "CanUserSortColumns":
            case "AreRowDetailsFrozen":
            case "AreRowGroupHeadersFrozen":
            case "IsScrollInertiaEnabled":
                if (!bool.TryParse(rawValue.Trim(), out var boolean))
                {
                    error = $"{canonicalName} must be True or False.";
                    return false;
                }

                normalizedValue = boolean.ToString();
                error = string.Empty;
                return true;
            case "HeadersVisibility":
                return TryNormalizeEnum(
                    rawValue,
                    "Headers visibility",
                    out normalizedValue,
                    out error,
                    out DataGridHeadersVisibility _);
            case "GridLinesVisibility":
                return TryNormalizeEnum(
                    rawValue,
                    "Grid lines visibility",
                    out normalizedValue,
                    out error,
                    out DataGridGridLinesVisibility _);
            case "SelectionMode":
                return TryNormalizeEnum(
                    rawValue,
                    "Selection mode",
                    out normalizedValue,
                    out error,
                    out DataGridSelectionMode _);
            case "ClipboardCopyMode":
                return TryNormalizeEnum(
                    rawValue,
                    "Clipboard copy mode",
                    out normalizedValue,
                    out error,
                    out DataGridClipboardCopyMode _);
            case "HorizontalScrollBarVisibility":
            case "VerticalScrollBarVisibility":
                return TryNormalizeEnum(
                    rawValue,
                    canonicalName,
                    out normalizedValue,
                    out error,
                    out ScrollBarVisibility _);
            case "FrozenColumnCount":
                if (!TryParseNonNegativeInt(
                        rawValue,
                        "Frozen column count",
                        out var frozenColumnCount,
                        out error))
                {
                    return false;
                }

                normalizedValue = frozenColumnCount.ToString(CultureInfo.InvariantCulture);
                return true;
            case "RowHeight":
            case "RowHeaderWidth":
            case "ColumnHeaderHeight":
                if (!TryParseDouble(
                        rawValue,
                        canonicalName,
                        allowNaN: true,
                        allowInfinity: false,
                        out var optionalLength,
                        out error))
                {
                    return false;
                }

                normalizedValue = FormatAxamlDouble(optionalLength);
                return true;
            case "MinColumnWidth":
                if (!TryParseDouble(
                        rawValue,
                        canonicalName,
                        allowNaN: false,
                        allowInfinity: false,
                        out var minimumWidth,
                        out error))
                {
                    return false;
                }

                normalizedValue = FormatAxamlDouble(minimumWidth);
                return true;
            case "MaxColumnWidth":
                if (!TryParseDouble(
                        rawValue,
                        canonicalName,
                        allowNaN: false,
                        allowInfinity: true,
                        out var maximumWidth,
                        out error))
                {
                    return false;
                }

                normalizedValue = FormatAxamlDouble(maximumWidth);
                return true;
            case "ColumnWidth":
                if (!TryParseColumnWidth(rawValue, out var columnWidth, out error))
                {
                    return false;
                }

                normalizedValue = FormatColumnWidth(columnWidth);
                return true;
            default:
                error = $"{canonicalName} is not a supported DataGrid behavior property.";
                return false;
        }
    }

    public static bool TryValidateProperties(
        string tagName,
        IReadOnlyDictionary<string, string> properties,
        out string error)
    {
        if (!string.Equals(tagName, "DataGrid", StringComparison.OrdinalIgnoreCase))
        {
            error = string.Empty;
            return true;
        }

        foreach (var propertyName in PropertyNames)
        {
            if (TryGetValue(properties, propertyName, out var value)
                && !TryNormalizeProperty(
                    tagName,
                    propertyName,
                    value,
                    out _,
                    out _,
                    out error))
            {
                return false;
            }
        }

        if (TryGetValue(properties, "MinColumnWidth", out var minimumRaw)
            && TryGetValue(properties, "MaxColumnWidth", out var maximumRaw)
            && TryParseDouble(
                minimumRaw,
                "Minimum column width",
                allowNaN: false,
                allowInfinity: false,
                out var minimumWidth,
                out error)
            && TryParseDouble(
                maximumRaw,
                "Maximum column width",
                allowNaN: false,
                allowInfinity: true,
                out var maximumWidth,
                out error)
            && minimumWidth > maximumWidth)
        {
            error = "Minimum column width must not be greater than maximum column width.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static void RemoveProperties(
        string tagName,
        IDictionary<string, string> properties)
    {
        if (!string.Equals(tagName, "DataGrid", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var propertyName in PropertyNames)
        {
            properties.Remove(propertyName);
        }
    }

    public static IReadOnlyList<DesignerDataGridBehaviorAttribute> GetAxamlAttributes(Control control)
    {
        if (!TryRead(control, out var values, out _))
        {
            return [];
        }

        return
        [
            new("AutoGenerateColumns", values.AutoGenerateColumns.ToString()),
            new("IsReadOnly", values.IsReadOnly.ToString()),
            new("CanUserReorderColumns", values.CanUserReorderColumns.ToString()),
            new("CanUserResizeColumns", values.CanUserResizeColumns.ToString()),
            new("CanUserSortColumns", values.CanUserSortColumns.ToString()),
            new("HeadersVisibility", values.HeadersVisibility.ToString()),
            new("GridLinesVisibility", values.GridLinesVisibility.ToString()),
            new("SelectionMode", values.SelectionMode.ToString()),
            new("ClipboardCopyMode", values.ClipboardCopyMode.ToString()),
            new("AreRowDetailsFrozen", values.AreRowDetailsFrozen.ToString()),
            new("AreRowGroupHeadersFrozen", values.AreRowGroupHeadersFrozen.ToString()),
            new("IsScrollInertiaEnabled", values.IsScrollInertiaEnabled.ToString()),
            new("FrozenColumnCount", values.FrozenColumnCount.ToString(CultureInfo.InvariantCulture)),
            new("RowHeight", FormatAxamlDouble(values.RowHeight)),
            new("RowHeaderWidth", FormatAxamlDouble(values.RowHeaderWidth)),
            new("ColumnHeaderHeight", FormatAxamlDouble(values.ColumnHeaderHeight)),
            new("MinColumnWidth", FormatAxamlDouble(values.MinColumnWidth)),
            new("MaxColumnWidth", FormatAxamlDouble(values.MaxColumnWidth)),
            new("ColumnWidth", FormatColumnWidth(values.ColumnWidth)),
            new("HorizontalScrollBarVisibility", values.HorizontalScrollBarVisibility.ToString()),
            new("VerticalScrollBarVisibility", values.VerticalScrollBarVisibility.ToString()),
        ];
    }

    public static string FormatEditorDouble(double value)
        => double.IsNaN(value)
            ? "Auto"
            : FormatAxamlDouble(value);

    public static string FormatColumnWidth(DataGridLength value)
        => value.UnitType switch
        {
            DataGridLengthUnitType.Auto => "Auto",
            DataGridLengthUnitType.SizeToCells => "SizeToCells",
            DataGridLengthUnitType.SizeToHeader => "SizeToHeader",
            DataGridLengthUnitType.Star when Math.Abs(value.Value - 1) < 0.0001 => "*",
            DataGridLengthUnitType.Star => $"{value.Value.ToString("0.###", CultureInfo.InvariantCulture)}*",
            _ => value.Value.ToString("0.###", CultureInfo.InvariantCulture),
        };

    private static bool TryNormalizeEnum<T>(
        string rawValue,
        string displayName,
        out string normalizedValue,
        out string error,
        out T value)
        where T : struct, Enum
    {
        if (!TryParseEnum(rawValue, displayName, out value, out error))
        {
            normalizedValue = string.Empty;
            return false;
        }

        normalizedValue = value.ToString();
        return true;
    }

    private static bool TryParseEnum<T>(
        string rawValue,
        string displayName,
        out T value,
        out string error)
        where T : struct, Enum
    {
        if (!Enum.TryParse(rawValue.Trim(), true, out value))
        {
            error = $"{displayName} must be one of {string.Join(", ", Enum.GetNames<T>())}.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryParseNonNegativeInt(
        string rawValue,
        string propertyName,
        out int value,
        out string error)
    {
        if (!int.TryParse(
                rawValue.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value)
            || value < 0)
        {
            value = 0;
            error = $"{propertyName} must be a whole number greater than or equal to 0.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryParseDouble(
        string rawValue,
        string propertyName,
        bool allowNaN,
        bool allowInfinity,
        out double value,
        out string error)
    {
        var candidate = rawValue.Trim();
        if (string.Equals(candidate, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            candidate = "NaN";
        }

        if (!double.TryParse(
                candidate,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value)
            || (!allowNaN && double.IsNaN(value))
            || (!allowInfinity && double.IsInfinity(value))
            || (!double.IsNaN(value) && !double.IsInfinity(value) && value < 0))
        {
            value = 0;
            var suffix = allowNaN ? " or Auto" : string.Empty;
            if (allowInfinity)
            {
                suffix += " or Infinity";
            }

            error = $"{propertyName} must be a finite non-negative number{suffix}.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryParseColumnWidth(
        string rawValue,
        out DataGridLength value,
        out string error)
    {
        var candidate = rawValue.Trim();
        if (string.Equals(candidate, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            value = DataGridLength.Auto;
            error = string.Empty;
            return true;
        }

        if (string.Equals(candidate, "SizeToCells", StringComparison.OrdinalIgnoreCase))
        {
            value = DataGridLength.SizeToCells;
            error = string.Empty;
            return true;
        }

        if (string.Equals(candidate, "SizeToHeader", StringComparison.OrdinalIgnoreCase))
        {
            value = DataGridLength.SizeToHeader;
            error = string.Empty;
            return true;
        }

        var isStar = candidate.EndsWith('*');
        var number = isStar ? candidate[..^1].Trim() : candidate;
        if (isStar && number.Length == 0)
        {
            number = "1";
        }

        if (!double.TryParse(
                number,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsed)
            || !double.IsFinite(parsed)
            || parsed <= 0)
        {
            value = default;
            error = "Column width must be Auto, SizeToCells, SizeToHeader, a positive number, *, or N*.";
            return false;
        }

        value = isStar
            ? new DataGridLength(parsed, DataGridLengthUnitType.Star)
            : new DataGridLength(parsed, DataGridLengthUnitType.Pixel);
        error = string.Empty;
        return true;
    }

    private static string FormatAxamlDouble(double value)
        => double.IsNaN(value)
            ? "NaN"
            : double.IsPositiveInfinity(value)
                ? "Infinity"
                : value.ToString("0.###", CultureInfo.InvariantCulture);

    private static void ApplyColumnBounds(
        DataGrid dataGrid,
        double minimumWidth,
        double maximumWidth)
    {
        if (dataGrid.Columns.Count == 0)
        {
            dataGrid.MaxColumnWidth = maximumWidth;
            dataGrid.MinColumnWidth = minimumWidth;
            return;
        }

        var columns = new List<(DataGridColumn Column, int DisplayIndex)>();
        foreach (var column in dataGrid.Columns)
        {
            columns.Add((column, column.DisplayIndex));
        }

        dataGrid.Columns.Clear();
        try
        {
            dataGrid.MaxColumnWidth = maximumWidth;
            dataGrid.MinColumnWidth = minimumWidth;
        }
        finally
        {
            foreach (var (column, _) in columns)
            {
                dataGrid.Columns.Add(column);
            }

            foreach (var (column, displayIndex) in columns)
            {
                column.DisplayIndex = displayIndex;
            }
        }
    }

    private static bool GetBoolean(
        IReadOnlyDictionary<string, string> properties,
        string key,
        bool fallback)
        => bool.TryParse(Get(properties, key, fallback.ToString()), out var value)
            ? value
            : fallback;

    private static string Get(
        IReadOnlyDictionary<string, string> properties,
        string key,
        string fallback)
        => TryGetValue(properties, key, out var value) ? value : fallback;

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
