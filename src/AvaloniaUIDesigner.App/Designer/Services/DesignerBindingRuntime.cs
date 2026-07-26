using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Avalonia.Controls;

namespace AvaloniaUIDesigner.App.Designer.Services;

public enum DesignerBindingMode
{
    Default,
    OneWay,
    TwoWay,
    OneTime,
    OneWayToSource,
}

public sealed record DesignerBindingDefinition(
    string PropertyName,
    string Path,
    DesignerBindingMode Mode,
    string? FallbackValue);

public static class DesignerBindingRuntime
{
    private static readonly ConditionalWeakTable<Control, List<DesignerBindingDefinition>> Bindings = new();

    public static IReadOnlyList<string> GetSupportedProperties(string targetType)
    {
        var properties = new List<string> { "IsEnabled", "IsVisible", "Opacity" };
        properties.AddRange(targetType switch
        {
            "Button" => ["Content", "Command", "CommandParameter"],
            "TextBox" => ["Text", "Watermark"],
            "TextBlock" => ["Text"],
            "Label" => ["Content"],
            "Image" => ["Source"],
            "CheckBox" or "RadioButton" or "ToggleSwitch" or "ToggleButton"
                => ["Content", "IsChecked"],
            "ComboBox" or "ListBox" => ["ItemsSource", "SelectedIndex", "SelectedItem"],
            "TreeView" => ["ItemsSource", "SelectedItem"],
            "DataGrid" => ["ItemsSource", "SelectedIndex", "SelectedItem", "IsReadOnly"],
            "Slider" or "ProgressBar" => ["Minimum", "Maximum", "Value"],
            "DatePicker" or "CalendarDatePicker" => ["SelectedDate"],
            "TimePicker" => ["SelectedTime"],
            "NumericUpDown" => ["Value"],
            "TabControl" => ["SelectedIndex"],
            "SplitView" => ["IsPaneOpen"],
            "Expander" => ["IsExpanded"],
            _ => [],
        });
        return properties.Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToList();
    }

    public static bool IsSupportedProperty(string targetType, string propertyName)
        => GetSupportedProperties(targetType).Contains(propertyName, StringComparer.Ordinal);

    public static List<DesignerBindingDefinition> ReadBindings(Control control)
        => Bindings.TryGetValue(control, out var definitions)
            ? definitions.Select(Clone).ToList()
            : [];

    public static bool HasBinding(Control control, string propertyName)
        => Bindings.TryGetValue(control, out var definitions)
            && definitions.Any(definition =>
                string.Equals(definition.PropertyName, propertyName, StringComparison.Ordinal));

    public static void ReplaceBindings(
        Control control,
        IReadOnlyList<DesignerBindingDefinition> definitions)
    {
        var targetType = control.GetType().Name;
        var normalized = definitions
            .Select(Normalize)
            .Where(definition => IsSupportedProperty(targetType, definition.PropertyName))
            .ToList();
        Bindings.Remove(control);
        if (normalized.Count > 0)
        {
            Bindings.Add(control, normalized);
        }
    }

    public static string Serialize(Control control)
        => Serialize(ReadBindings(control));

    public static string Serialize(IReadOnlyList<DesignerBindingDefinition> definitions)
        => JsonSerializer.Serialize(definitions.Select(Normalize));

    public static bool TryDeserialize(
        string json,
        out List<DesignerBindingDefinition> definitions)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<List<DesignerBindingDefinition>>(json) ?? [];
            definitions = [];
            foreach (var definition in parsed)
            {
                if (!TryNormalize(definition, out var normalized))
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

    public static IReadOnlyList<string> FormatEditorLines(Control control)
        => ReadBindings(control)
            .OrderBy(definition => definition.PropertyName, StringComparer.Ordinal)
            .Select(definition =>
                $"{definition.PropertyName} | {definition.Path} | {definition.Mode}"
                + (string.IsNullOrWhiteSpace(definition.FallbackValue)
                    ? string.Empty
                    : $" | {definition.FallbackValue}"))
            .ToList();

    public static bool TryParseEditorLines(
        string targetType,
        IEnumerable<string> lines,
        out List<DesignerBindingDefinition> definitions,
        out string error)
    {
        definitions = [];
        error = string.Empty;
        var supported = GetSupportedProperties(targetType);
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
            if (parts.Length is < 2 or > 4)
            {
                error = $"Line {lineNumber}: use Property | Path | Mode | Fallback.";
                definitions = [];
                return false;
            }

            var propertyName = supported.FirstOrDefault(property =>
                string.Equals(property, parts[0], StringComparison.OrdinalIgnoreCase));
            if (propertyName is null)
            {
                error = $"Line {lineNumber}: property '{parts[0]}' is not supported for {targetType}.";
                definitions = [];
                return false;
            }

            if (definitions.Any(definition =>
                    string.Equals(definition.PropertyName, propertyName, StringComparison.Ordinal)))
            {
                error = $"Line {lineNumber}: property '{propertyName}' is already bound.";
                definitions = [];
                return false;
            }

            if (!IsValidPath(parts[1]))
            {
                error = $"Line {lineNumber}: enter a property path such as User.Name.";
                definitions = [];
                return false;
            }

            var mode = DesignerBindingMode.Default;
            if (parts.Length >= 3 && parts[2].Length > 0
                && !Enum.TryParse(parts[2], ignoreCase: true, out mode))
            {
                error = $"Line {lineNumber}: Mode must be Default, OneWay, TwoWay, OneTime, or OneWayToSource.";
                definitions = [];
                return false;
            }

            var fallback = parts.Length == 4 && parts[3].Length > 0 ? parts[3] : null;
            if (!IsValidFallback(fallback))
            {
                error = $"Line {lineNumber}: Fallback cannot contain comma, braces, quotes, or a pipe.";
                definitions = [];
                return false;
            }

            definitions.Add(new DesignerBindingDefinition(propertyName, parts[1], mode, fallback));
        }

        return true;
    }

    public static string FormatExpression(DesignerBindingDefinition definition)
    {
        var normalized = Normalize(definition);
        var parts = new List<string> { normalized.Path };
        if (normalized.Mode != DesignerBindingMode.Default)
        {
            parts.Add($"Mode={normalized.Mode}");
        }

        if (!string.IsNullOrWhiteSpace(normalized.FallbackValue))
        {
            parts.Add($"FallbackValue='{normalized.FallbackValue}'");
        }

        return $"{{ReflectionBinding {string.Join(", ", parts)}}}";
    }

    public static bool IsBindingExpression(string expression)
    {
        var value = expression.TrimStart();
        return value.StartsWith("{Binding", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("{ReflectionBinding", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryParseExpression(
        string propertyName,
        string expression,
        out DesignerBindingDefinition definition)
    {
        var value = expression.Trim();
        var prefix = value.StartsWith("{ReflectionBinding", StringComparison.OrdinalIgnoreCase)
            ? "{ReflectionBinding"
            : value.StartsWith("{Binding", StringComparison.OrdinalIgnoreCase)
                ? "{Binding"
                : string.Empty;
        if (prefix.Length == 0 || !value.EndsWith('}'))
        {
            definition = EmptyDefinition();
            return false;
        }

        var parts = value[prefix.Length..^1]
            .Split(',')
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .ToList();
        if (parts.Count == 0)
        {
            definition = EmptyDefinition();
            return false;
        }

        var path = parts[0].StartsWith("Path=", StringComparison.OrdinalIgnoreCase)
            ? parts[0][5..].Trim()
            : parts[0];
        var mode = DesignerBindingMode.Default;
        string? fallback = null;
        foreach (var part in parts.Skip(1))
        {
            if (part.StartsWith("Mode=", StringComparison.OrdinalIgnoreCase))
            {
                if (!Enum.TryParse(part[5..].Trim(), ignoreCase: true, out mode))
                {
                    definition = EmptyDefinition();
                    return false;
                }

                continue;
            }

            if (part.StartsWith("FallbackValue=", StringComparison.OrdinalIgnoreCase))
            {
                fallback = part[14..].Trim().Trim('\'', '"');
                continue;
            }

            definition = EmptyDefinition();
            return false;
        }

        var candidate = new DesignerBindingDefinition(propertyName, path, mode, fallback);
        if (!TryNormalize(candidate, out definition))
        {
            definition = EmptyDefinition();
            return false;
        }

        return true;
    }

    private static bool TryNormalize(
        DesignerBindingDefinition? definition,
        out DesignerBindingDefinition normalized)
    {
        if (definition is null
            || string.IsNullOrWhiteSpace(definition.PropertyName)
            || !IsValidPath(definition.Path)
            || !Enum.IsDefined(definition.Mode)
            || !IsValidFallback(definition.FallbackValue))
        {
            normalized = EmptyDefinition();
            return false;
        }

        normalized = definition with
        {
            PropertyName = definition.PropertyName.Trim(),
            Path = definition.Path.Trim(),
            FallbackValue = string.IsNullOrWhiteSpace(definition.FallbackValue)
                ? null
                : definition.FallbackValue.Trim(),
        };
        return true;
    }

    private static DesignerBindingDefinition Normalize(DesignerBindingDefinition definition)
        => TryNormalize(definition, out var normalized)
            ? normalized
            : throw new ArgumentException("Invalid designer binding.", nameof(definition));

    private static DesignerBindingDefinition Clone(DesignerBindingDefinition definition)
        => definition with { };

    private static DesignerBindingDefinition EmptyDefinition()
        => new(string.Empty, string.Empty, DesignerBindingMode.Default, null);

    private static bool IsValidPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var segment in value.Trim().Split('.'))
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

    private static bool IsValidFallback(string? value)
        => string.IsNullOrWhiteSpace(value)
            || value.IndexOfAny([',', '{', '}', '\'', '"', '|']) < 0;
}
