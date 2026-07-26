using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Data;

namespace AvaloniaUIDesigner.App.Designer.Services;

public enum DesignerDataGridColumnKind
{
    Text,
    CheckBox,
}

public sealed record DesignerDataGridColumnDefinition(
    string Header,
    string BindingPath,
    DesignerDataGridColumnKind Kind,
    string Width,
    bool IsReadOnly);

public static class DesignerDataGridRuntime
{
    private static readonly ConditionalWeakTable<DataGrid, List<DesignerDataGridColumnDefinition>> Definitions = new();

    public static DataGrid CreateDefaultDataGrid()
    {
        var dataGrid = new DataGrid
        {
            AutoGenerateColumns = false,
            GridLinesVisibility = DataGridGridLinesVisibility.All,
        };
        ReplaceColumns(dataGrid, CreateDefaultDefinitions());
        return dataGrid;
    }

    public static List<DesignerDataGridColumnDefinition> CreateDefaultDefinitions()
        =>
        [
            new("Name", "Name", DesignerDataGridColumnKind.Text, "2*", false),
            new("Category", "Category", DesignerDataGridColumnKind.Text, "*", false),
            new("Active", "IsActive", DesignerDataGridColumnKind.CheckBox, "90", false),
        ];

    public static List<DesignerDataGridColumnDefinition> ReadColumns(DataGrid dataGrid)
    {
        if (Definitions.TryGetValue(dataGrid, out var definitions))
        {
            return definitions.Select(Clone).ToList();
        }

        var discovered = dataGrid.Columns
            .Select(ReadColumn)
            .Where(definition => definition is not null)
            .Select(definition => definition!)
            .ToList();
        SetDefinitions(dataGrid, discovered);
        return discovered.Select(Clone).ToList();
    }

    public static void ReplaceColumns(
        DataGrid dataGrid,
        IReadOnlyList<DesignerDataGridColumnDefinition> definitions)
    {
        var normalized = definitions.Select(Normalize).ToList();
        SetDefinitions(dataGrid, normalized);
        dataGrid.AutoGenerateColumns = false;
        dataGrid.Columns.Clear();
        foreach (var definition in normalized)
        {
            var binding = new Binding($"[{definition.BindingPath}]");
            DataGridColumn column = definition.Kind switch
            {
                DesignerDataGridColumnKind.CheckBox => new DataGridCheckBoxColumn
                {
                    Binding = binding,
                },
                _ => new DataGridTextColumn
                {
                    Binding = binding,
                },
            };
            column.Header = definition.Header;
            column.Width = ParseWidth(definition.Width);
            column.IsReadOnly = definition.IsReadOnly;
            dataGrid.Columns.Add(column);
        }

        dataGrid.ItemsSource = CreateSampleRows(normalized);
    }

    public static string Serialize(DataGrid dataGrid)
        => Serialize(ReadColumns(dataGrid));

    public static string Serialize(IReadOnlyList<DesignerDataGridColumnDefinition> definitions)
        => JsonSerializer.Serialize(definitions.Select(Normalize));

    public static string Serialize(XElement dataGridElement)
        => Serialize(ReadColumns(dataGridElement));

    public static bool TryDeserialize(
        string json,
        out List<DesignerDataGridColumnDefinition> definitions)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<List<DesignerDataGridColumnDefinition>>(json) ?? [];
            definitions = [];
            foreach (var definition in parsed)
            {
                if (!TryNormalizeDefinition(definition, out var normalized))
                {
                    definitions = [];
                    return false;
                }

                definitions.Add(normalized);
            }

            return true;
        }
        catch (JsonException)
        {
            definitions = [];
            return false;
        }
    }

    public static IReadOnlyList<string> FormatEditorLines(DataGrid dataGrid)
        => ReadColumns(dataGrid)
            .Select(definition =>
                $"{definition.Kind} | {definition.Header} | {definition.BindingPath} | {definition.Width} | {definition.IsReadOnly.ToString().ToLowerInvariant()}")
            .ToList();

    public static bool TryParseEditorLines(
        IEnumerable<string> lines,
        out List<DesignerDataGridColumnDefinition> definitions,
        out string error)
    {
        definitions = [];
        error = string.Empty;
        var lineNumber = 0;

        foreach (var sourceLine in lines)
        {
            lineNumber++;
            var line = sourceLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var parts = line.Split('|').Select(part => part.Trim()).ToArray();
            if (parts.Length is < 3 or > 5)
            {
                error = $"Line {lineNumber}: use Type | Header | Binding | Width | ReadOnly.";
                definitions = [];
                return false;
            }

            if (!TryParseKind(parts[0], out var kind))
            {
                error = $"Line {lineNumber}: type must be Text or CheckBox.";
                definitions = [];
                return false;
            }

            if (parts[1].Length == 0)
            {
                error = $"Line {lineNumber}: header cannot be empty.";
                definitions = [];
                return false;
            }

            if (!IsValidBindingPath(parts[2]))
            {
                error = $"Line {lineNumber}: binding must be a property path such as Name or Customer.Name.";
                definitions = [];
                return false;
            }

            var width = parts.Length >= 4 && parts[3].Length > 0 ? parts[3] : "Auto";
            if (!TryNormalizeWidth(width, out width))
            {
                error = $"Line {lineNumber}: width must be Auto, SizeToCells, SizeToHeader, a positive number, *, or N*.";
                definitions = [];
                return false;
            }

            var isReadOnly = false;
            if (parts.Length == 5 && parts[4].Length > 0
                && !bool.TryParse(parts[4], out isReadOnly))
            {
                error = $"Line {lineNumber}: ReadOnly must be true or false.";
                definitions = [];
                return false;
            }

            definitions.Add(new DesignerDataGridColumnDefinition(
                parts[1],
                parts[2],
                kind,
                width,
                isReadOnly));
        }

        if (definitions.Count == 0)
        {
            error = "Add at least one DataGrid column.";
            return false;
        }

        return true;
    }

    public static bool AreEquivalent(
        IReadOnlyList<DesignerDataGridColumnDefinition> left,
        IReadOnlyList<DesignerDataGridColumnDefinition> right)
        => string.Equals(Serialize(left), Serialize(right), StringComparison.Ordinal);

    private static List<Dictionary<string, object>> CreateSampleRows(
        IReadOnlyList<DesignerDataGridColumnDefinition> definitions)
    {
        var rows = new List<Dictionary<string, object>>();
        for (var index = 0; index < 4; index++)
        {
            var row = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                row[definition.BindingPath] = definition.Kind == DesignerDataGridColumnKind.CheckBox
                    ? index % 2 == 0
                    : $"{definition.Header} {index + 1}";
            }

            rows.Add(row);
        }

        return rows;
    }

    private static List<DesignerDataGridColumnDefinition> ReadColumns(XElement dataGridElement)
    {
        var columnsHost = dataGridElement.Elements().FirstOrDefault(element =>
            string.Equals(element.Name.LocalName, "DataGrid.Columns", StringComparison.OrdinalIgnoreCase));
        if (columnsHost is null)
        {
            return [];
        }

        var definitions = new List<DesignerDataGridColumnDefinition>();
        foreach (var column in columnsHost.Elements())
        {
            var kind = column.Name.LocalName switch
            {
                "DataGridTextColumn" => DesignerDataGridColumnKind.Text,
                "DataGridCheckBoxColumn" => DesignerDataGridColumnKind.CheckBox,
                _ => (DesignerDataGridColumnKind?)null,
            };
            if (kind is null)
            {
                continue;
            }

            var bindingPath = ParseBindingPath(column.Attribute("Binding")?.Value);
            if (!IsValidBindingPath(bindingPath))
            {
                continue;
            }

            var width = column.Attribute("Width")?.Value ?? "Auto";
            if (!TryNormalizeWidth(width, out width))
            {
                width = "Auto";
            }

            definitions.Add(new DesignerDataGridColumnDefinition(
                column.Attribute("Header")?.Value ?? bindingPath,
                bindingPath,
                kind.Value,
                width,
                bool.TryParse(column.Attribute("IsReadOnly")?.Value, out var isReadOnly) && isReadOnly));
        }

        return definitions;
    }

    private static DesignerDataGridColumnDefinition? ReadColumn(DataGridColumn column)
    {
        var kind = column switch
        {
            DataGridCheckBoxColumn => DesignerDataGridColumnKind.CheckBox,
            DataGridTextColumn => DesignerDataGridColumnKind.Text,
            _ => (DesignerDataGridColumnKind?)null,
        };
        if (kind is null)
        {
            return null;
        }

        var bindingPath = column is DataGridBoundColumn { Binding: Binding binding }
            ? ParseBindingPath(binding.Path)
            : string.Empty;
        if (!IsValidBindingPath(bindingPath))
        {
            return null;
        }

        return new DesignerDataGridColumnDefinition(
            column.Header?.ToString() ?? bindingPath,
            bindingPath,
            kind.Value,
            FormatWidth(column.Width),
            column.IsReadOnly);
    }

    private static string ParseBindingPath(string? expression)
    {
        var value = expression?.Trim() ?? string.Empty;
        if (value.StartsWith('[') && value.EndsWith(']'))
        {
            return value[1..^1].Trim();
        }

        const string bindingPrefix = "{Binding";
        if (!value.StartsWith(bindingPrefix, StringComparison.OrdinalIgnoreCase)
            || !value.EndsWith('}'))
        {
            return value;
        }

        value = value[bindingPrefix.Length..^1].Trim();
        if (value.StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
        {
            value = value[5..].Trim();
        }

        var comma = value.IndexOf(',');
        return (comma >= 0 ? value[..comma] : value).Trim();
    }

    private static bool TryParseKind(string value, out DesignerDataGridColumnKind kind)
    {
        if (string.Equals(value, "Text", StringComparison.OrdinalIgnoreCase))
        {
            kind = DesignerDataGridColumnKind.Text;
            return true;
        }

        if (string.Equals(value, "CheckBox", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "Bool", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "Boolean", StringComparison.OrdinalIgnoreCase))
        {
            kind = DesignerDataGridColumnKind.CheckBox;
            return true;
        }

        kind = default;
        return false;
    }

    private static bool IsValidBindingPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var segment in value.Split('.'))
        {
            if (segment.Length == 0 || !(char.IsLetter(segment[0]) || segment[0] == '_'))
            {
                return false;
            }

            if (segment.Skip(1).Any(character => !(char.IsLetterOrDigit(character) || character == '_')))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryNormalizeDefinition(
        DesignerDataGridColumnDefinition? definition,
        out DesignerDataGridColumnDefinition normalized)
    {
        if (definition is null
            || string.IsNullOrWhiteSpace(definition.Header)
            || !IsValidBindingPath(definition.BindingPath)
            || !Enum.IsDefined(definition.Kind)
            || !TryNormalizeWidth(definition.Width, out var width))
        {
            normalized = new DesignerDataGridColumnDefinition(
                string.Empty,
                string.Empty,
                DesignerDataGridColumnKind.Text,
                "Auto",
                false);
            return false;
        }

        normalized = definition with
        {
            Header = definition.Header.Trim(),
            BindingPath = definition.BindingPath.Trim(),
            Width = width,
        };
        return true;
    }

    private static DesignerDataGridColumnDefinition Normalize(DesignerDataGridColumnDefinition definition)
        => TryNormalizeDefinition(definition, out var normalized)
            ? normalized
            : throw new ArgumentException("Invalid DataGrid column definition.", nameof(definition));

    private static DesignerDataGridColumnDefinition Clone(DesignerDataGridColumnDefinition definition)
        => definition with { };

    private static void SetDefinitions(
        DataGrid dataGrid,
        IReadOnlyList<DesignerDataGridColumnDefinition> definitions)
    {
        Definitions.Remove(dataGrid);
        Definitions.Add(dataGrid, definitions.Select(Clone).ToList());
    }

    private static bool TryNormalizeWidth(string? value, out string normalized)
    {
        var candidate = value?.Trim() ?? string.Empty;
        if (candidate.Equals("Auto", StringComparison.OrdinalIgnoreCase)
            || candidate.Equals("SizeToCells", StringComparison.OrdinalIgnoreCase)
            || candidate.Equals("SizeToHeader", StringComparison.OrdinalIgnoreCase))
        {
            normalized = candidate.Equals("Auto", StringComparison.OrdinalIgnoreCase)
                ? "Auto"
                : candidate.Equals("SizeToCells", StringComparison.OrdinalIgnoreCase)
                    ? "SizeToCells"
                    : "SizeToHeader";
            return true;
        }

        if (candidate.EndsWith('*'))
        {
            var factorText = candidate[..^1].Trim();
            var factor = factorText.Length == 0
                ? 1
                : double.TryParse(
                    factorText,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var parsedFactor)
                    ? parsedFactor
                    : double.NaN;
            if (double.IsFinite(factor) && factor > 0)
            {
                normalized = factor == 1
                    ? "*"
                    : $"{factor.ToString("0.###", CultureInfo.InvariantCulture)}*";
                return true;
            }
        }
        else if (double.TryParse(
                     candidate,
                     NumberStyles.Float,
                     CultureInfo.InvariantCulture,
                     out var pixels)
                 && double.IsFinite(pixels)
                 && pixels > 0)
        {
            normalized = pixels.ToString("0.###", CultureInfo.InvariantCulture);
            return true;
        }

        normalized = string.Empty;
        return false;
    }

    private static DataGridLength ParseWidth(string value)
    {
        TryNormalizeWidth(value, out var normalized);
        return normalized switch
        {
            "Auto" => DataGridLength.Auto,
            "SizeToCells" => DataGridLength.SizeToCells,
            "SizeToHeader" => DataGridLength.SizeToHeader,
            _ when normalized.EndsWith('*') => new DataGridLength(
                normalized == "*"
                    ? 1
                    : double.Parse(normalized[..^1], CultureInfo.InvariantCulture),
                DataGridLengthUnitType.Star),
            _ => new DataGridLength(double.Parse(normalized, CultureInfo.InvariantCulture)),
        };
    }

    private static string FormatWidth(DataGridLength width)
    {
        if (width.IsAuto)
        {
            return "Auto";
        }

        if (width.IsSizeToCells)
        {
            return "SizeToCells";
        }

        if (width.IsSizeToHeader)
        {
            return "SizeToHeader";
        }

        if (width.IsStar)
        {
            return width.Value == 1
                ? "*"
                : $"{width.Value.ToString("0.###", CultureInfo.InvariantCulture)}*";
        }

        return width.Value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
