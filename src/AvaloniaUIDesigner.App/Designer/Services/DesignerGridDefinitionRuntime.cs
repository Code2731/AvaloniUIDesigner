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
}
