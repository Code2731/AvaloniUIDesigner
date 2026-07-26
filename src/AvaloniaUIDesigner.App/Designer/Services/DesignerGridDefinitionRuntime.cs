using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUIDesigner.App.Designer.Services;

public static class DesignerGridDefinitionRuntime
{
    public static string Format(RowDefinitions definitions)
        => string.Join(",", definitions.Select(definition => Format(definition.Height)));

    public static string Format(ColumnDefinitions definitions)
        => string.Join(",", definitions.Select(definition => Format(definition.Width)));

    public static bool TryParse(
        string rowDefinitions,
        string columnDefinitions,
        out RowDefinitions rows,
        out ColumnDefinitions columns,
        out string error)
    {
        try
        {
            rows = string.IsNullOrWhiteSpace(rowDefinitions)
                ? new RowDefinitions()
                : new RowDefinitions(rowDefinitions.Trim());
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            rows = new RowDefinitions();
            columns = new ColumnDefinitions();
            error = $"Row definitions are invalid: {exception.Message}";
            return false;
        }

        try
        {
            columns = string.IsNullOrWhiteSpace(columnDefinitions)
                ? new ColumnDefinitions()
                : new ColumnDefinitions(columnDefinitions.Trim());
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            columns = new ColumnDefinitions();
            error = $"Column definitions are invalid: {exception.Message}";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryApply(
        Grid grid,
        IReadOnlyDictionary<string, string> properties,
        out string error)
    {
        var rowDefinitions = properties.TryGetValue("RowDefinitions", out var rows)
            ? rows
            : Format(grid.RowDefinitions);
        var columnDefinitions = properties.TryGetValue("ColumnDefinitions", out var columns)
            ? columns
            : Format(grid.ColumnDefinitions);
        if (!TryParse(rowDefinitions, columnDefinitions, out var parsedRows, out var parsedColumns, out error))
        {
            return false;
        }

        grid.RowDefinitions = parsedRows;
        grid.ColumnDefinitions = parsedColumns;
        return true;
    }

    public static Rect GetCellBounds(
        Grid grid,
        Rect gridBounds,
        int row,
        int column,
        int rowSpan,
        int columnSpan)
    {
        var rowSizes = CalculateTrackSizes(
            grid.RowDefinitions.Select(definition => definition.Height).ToList(),
            gridBounds.Height);
        var columnSizes = CalculateTrackSizes(
            grid.ColumnDefinitions.Select(definition => definition.Width).ToList(),
            gridBounds.Width);
        var normalizedRow = Math.Clamp(row, 0, rowSizes.Count - 1);
        var normalizedColumn = Math.Clamp(column, 0, columnSizes.Count - 1);
        var normalizedRowSpan = Math.Clamp(rowSpan, 1, rowSizes.Count - normalizedRow);
        var normalizedColumnSpan = Math.Clamp(columnSpan, 1, columnSizes.Count - normalizedColumn);

        return new Rect(
            gridBounds.X + columnSizes.Take(normalizedColumn).Sum(),
            gridBounds.Y + rowSizes.Take(normalizedRow).Sum(),
            columnSizes.Skip(normalizedColumn).Take(normalizedColumnSpan).Sum(),
            rowSizes.Skip(normalizedRow).Take(normalizedRowSpan).Sum());
    }

    public static int GetRowCount(Grid grid) => Math.Max(1, grid.RowDefinitions.Count);

    public static int GetColumnCount(Grid grid) => Math.Max(1, grid.ColumnDefinitions.Count);

    private static string Format(GridLength length)
    {
        if (length.IsAuto)
        {
            return "Auto";
        }

        var value = length.Value.ToString("0.###", CultureInfo.InvariantCulture);
        if (!length.IsStar)
        {
            return value;
        }

        return Math.Abs(length.Value - 1) < 0.0001 ? "*" : $"{value}*";
    }

    private static IReadOnlyList<double> CalculateTrackSizes(
        IReadOnlyList<GridLength> definitions,
        double available)
    {
        if (definitions.Count == 0)
        {
            return [Math.Max(0, available)];
        }

        var fixedSize = definitions.Where(length => !length.IsAuto && !length.IsStar).Sum(length => length.Value);
        var flexibleUnits = definitions.Sum(length =>
            length.IsAuto ? 1 : length.IsStar ? Math.Max(0, length.Value) : 0);
        var flexibleSize = flexibleUnits <= 0
            ? 0
            : Math.Max(0, available - fixedSize) / flexibleUnits;

        return definitions.Select(length =>
                length.IsAuto
                    ? flexibleSize
                    : length.IsStar
                        ? flexibleSize * Math.Max(0, length.Value)
                        : Math.Max(0, length.Value))
            .ToList();
    }
}
