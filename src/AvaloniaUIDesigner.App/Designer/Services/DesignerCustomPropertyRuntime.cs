using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaUIDesigner.App.Models;

namespace AvaloniaUIDesigner.App.Designer.Services;

public enum DesignerCustomPropertyValueSource
{
    Local,
    Binding,
    Style,
    Unset,
}

public sealed record DesignerCustomPropertyValueState(
    string PropertyName,
    string Value,
    DesignerCustomPropertyValueSource Source,
    DesignerCustomPropertyDefinition? Definition = null)
{
    public string DisplayValue => Source == DesignerCustomPropertyValueSource.Unset
        ? "(unset)"
        : Value;

    public string EditorValue => Source == DesignerCustomPropertyValueSource.Local
        ? Value
        : string.Empty;

    public string EditorWatermark => Source switch
    {
        DesignerCustomPropertyValueSource.Binding => $"Binding: {Value}",
        DesignerCustomPropertyValueSource.Style => $"Style: {Value}",
        DesignerCustomPropertyValueSource.Unset when Type == DesignerCustomPropertyType.String
            => "Set local value",
        DesignerCustomPropertyValueSource.Unset => $"Set {TypeLabel.ToLowerInvariant()} value",
        _ => string.Empty,
    };

    public DesignerCustomPropertyType Type => Definition?.Type
        ?? DesignerCustomPropertyType.String;

    public string TypeLabel => Type.ToString();

    public string DisplayName => string.IsNullOrWhiteSpace(Definition?.DisplayName)
        ? PropertyName
        : Definition.DisplayName;

    public string CategoryLabel => string.IsNullOrWhiteSpace(Definition?.Category)
        ? "Custom"
        : Definition.Category;

    public string Description => Definition?.Description ?? string.Empty;

    public string MetadataLabel => string.Equals(DisplayName, PropertyName, StringComparison.Ordinal)
        ? TypeLabel
        : $"{PropertyName} | {TypeLabel}";

    public string InspectorToolTip => Description.Length == 0
        ? $"{PropertyName} ({TypeLabel})"
        : $"{PropertyName} ({TypeLabel}){Environment.NewLine}{Description}";

    public bool ShowsCategoryHeader { get; init; }

    public bool UsesChoiceEditor => Type is DesignerCustomPropertyType.Boolean
        or DesignerCustomPropertyType.Enum;

    public bool UsesNumericEditor => TryGetNumericEditorSettings(out _, out _, out _);

    public bool UsesColorEditor => Type == DesignerCustomPropertyType.Color;

    public bool UsesFourValueEditor => Type is DesignerCustomPropertyType.Thickness
            or DesignerCustomPropertyType.CornerRadius
        && (Source is DesignerCustomPropertyValueSource.Binding
                or DesignerCustomPropertyValueSource.Unset
            || DesignerCustomPropertyRuntime.TryGetFourValueComponents(
                Type,
                Value,
                out var values,
                out _)
            && values.All(value => value >= (double)decimal.MinValue
                && value <= (double)decimal.MaxValue));

    public bool UsesTextEditor => !UsesChoiceEditor
        && !UsesNumericEditor
        && !UsesColorEditor
        && !UsesFourValueEditor;

    public decimal? NumericEditorValue
        => UsesNumericEditor
            && Source == DesignerCustomPropertyValueSource.Local
            && decimal.TryParse(
                Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var numericValue)
                ? numericValue
                : null;

    public decimal NumericEditorMinimum
        => TryGetNumericEditorSettings(out var minimum, out _, out _)
            ? minimum
            : decimal.MinValue;

    public decimal NumericEditorMaximum
        => TryGetNumericEditorSettings(out _, out var maximum, out _)
            ? maximum
            : decimal.MaxValue;

    public decimal NumericEditorIncrement
        => TryGetNumericEditorSettings(out _, out _, out var increment)
            ? increment
            : 1m;

    public string NumericEditorFormatString => Type == DesignerCustomPropertyType.Integer
        ? "0"
        : "0.###############";

    public bool HasColorPreview => Type == DesignerCustomPropertyType.Color
        && Color.TryParse(Value, out _);

    public IBrush ColorPreviewBrush => Color.TryParse(Value, out var color)
        ? new SolidColorBrush(color)
        : Brushes.Transparent;

    public IReadOnlyList<string> EditorChoices => Type switch
    {
        DesignerCustomPropertyType.Boolean => [bool.FalseString, bool.TrueString],
        DesignerCustomPropertyType.Enum => Definition?.Options ?? [],
        _ => [],
    };

    public string? EditorChoiceValue => Source == DesignerCustomPropertyValueSource.Local
        ? Value
        : null;

    public bool CanReset => Source is DesignerCustomPropertyValueSource.Local
        or DesignerCustomPropertyValueSource.Binding;

    public string BindingActionLabel => Source == DesignerCustomPropertyValueSource.Binding
        ? "Edit"
        : "Bind";

    public string SourceLabel => Source.ToString().ToUpperInvariant();

    private bool TryGetNumericEditorSettings(
        out decimal minimum,
        out decimal maximum,
        out decimal increment)
    {
        minimum = decimal.MinValue;
        maximum = decimal.MaxValue;
        increment = Type == DesignerCustomPropertyType.Integer ? 1m : 0.1m;
        if (Type is not DesignerCustomPropertyType.Integer
            and not DesignerCustomPropertyType.Double)
        {
            return false;
        }

        if (Definition?.Minimum is { } declaredMinimum)
        {
            if (!TryConvertNumericBoundary(declaredMinimum, isMinimum: true, out minimum))
            {
                return false;
            }
        }

        if (Definition?.Maximum is { } declaredMaximum)
        {
            if (!TryConvertNumericBoundary(declaredMaximum, isMinimum: false, out maximum))
            {
                return false;
            }
        }

        if (minimum > maximum)
        {
            return false;
        }

        return Source != DesignerCustomPropertyValueSource.Local
            || decimal.TryParse(
                Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var localValue)
            && localValue >= minimum
            && localValue <= maximum;
    }

    private static bool TryConvertNumericBoundary(
        double value,
        bool isMinimum,
        out decimal boundary)
    {
        try
        {
            boundary = (decimal)value;
            return true;
        }
        catch (OverflowException)
        {
            if (isMinimum && value < 0)
            {
                boundary = decimal.MinValue;
                return true;
            }

            if (!isMinimum && value > 0)
            {
                boundary = decimal.MaxValue;
                return true;
            }

            boundary = default;
            return false;
        }
    }
}

public static class DesignerCustomPropertyRuntime
{
    public const string DeclaredPropertiesMetadataKey = "__customPropertyNames";
    public const string PropertyDefinitionsMetadataKey = "__customPropertyDefinitions";

    private static readonly JsonSerializerOptions DefinitionSerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static bool IsValidDeclaredPropertyName(string propertyName)
        => !string.IsNullOrWhiteSpace(propertyName)
            && (char.IsLetter(propertyName[0]) || propertyName[0] == '_')
            && propertyName.All(character => char.IsLetterOrDigit(character) || character == '_')
            && !propertyName.StartsWith("__", StringComparison.Ordinal)
            && !string.Equals(propertyName, "Classes", StringComparison.Ordinal);

    public static IReadOnlyList<string> NormalizeDeclaredPropertyNames(
        IEnumerable<string> declaredPropertyNames)
        => declaredPropertyNames
            .Where(IsValidDeclaredPropertyName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(propertyName => propertyName, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<DesignerCustomPropertyDefinition> NormalizePropertyDefinitions(
        IEnumerable<DesignerCustomPropertyDefinition>? definitions,
        IEnumerable<string>? fallbackPropertyNames = null)
    {
        var normalized = new Dictionary<string, DesignerCustomPropertyDefinition>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var propertyName in NormalizeDeclaredPropertyNames(fallbackPropertyNames ?? []))
        {
            normalized[propertyName] = new DesignerCustomPropertyDefinition(propertyName);
        }

        foreach (var definition in definitions ?? [])
        {
            if (TryNormalizePropertyDefinition(definition, out var candidate, out _))
            {
                normalized[candidate.Name] = candidate;
            }
        }

        return normalized.Values
            .OrderBy(definition => definition.Name, StringComparer.Ordinal)
            .ToList();
    }

    public static bool TryNormalizePropertyDefinition(
        DesignerCustomPropertyDefinition? definition,
        out DesignerCustomPropertyDefinition normalized,
        out string error)
    {
        var normalizedName = definition?.Name?.Trim() ?? string.Empty;
        if (definition is null || !IsValidDeclaredPropertyName(normalizedName))
        {
            normalized = new DesignerCustomPropertyDefinition(string.Empty);
            error = "Property name must be a valid CLR property name without dots or spaces.";
            return false;
        }

        if (!Enum.IsDefined(definition.Type))
        {
            normalized = new DesignerCustomPropertyDefinition(string.Empty);
            error = $"Property '{definition.Name}' uses an unsupported type.";
            return false;
        }

        var proposedOptions = (definition.Options ?? [])
            .Select(option => option?.Trim() ?? string.Empty)
            .Where(option => option.Length > 0)
            .ToList();
        var options = proposedOptions
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (options.Count != proposedOptions.Count)
        {
            normalized = new DesignerCustomPropertyDefinition(string.Empty);
            error = $"Enum property '{definition.Name}' cannot contain duplicate options.";
            return false;
        }

        if (definition.Type == DesignerCustomPropertyType.Enum && options.Count == 0)
        {
            normalized = new DesignerCustomPropertyDefinition(string.Empty);
            error = $"Enum property '{definition.Name}' must declare at least one option.";
            return false;
        }

        if (definition.Type != DesignerCustomPropertyType.Enum && options.Count > 0)
        {
            normalized = new DesignerCustomPropertyDefinition(string.Empty);
            error = $"Only Enum property '{definition.Name}' can declare options.";
            return false;
        }

        var hasRange = definition.Minimum.HasValue || definition.Maximum.HasValue;
        if (hasRange && definition.Type is not DesignerCustomPropertyType.Integer
            and not DesignerCustomPropertyType.Double)
        {
            normalized = new DesignerCustomPropertyDefinition(string.Empty);
            error = $"Only numeric property '{definition.Name}' can declare a range.";
            return false;
        }

        if (definition.Minimum is { } minimum && !double.IsFinite(minimum)
            || definition.Maximum is { } maximum && !double.IsFinite(maximum)
            || definition.Minimum > definition.Maximum)
        {
            normalized = new DesignerCustomPropertyDefinition(string.Empty);
            error = $"Property '{definition.Name}' must use a finite range with minimum no greater than maximum.";
            return false;
        }

        if (definition.Type == DesignerCustomPropertyType.Integer
            && (definition.Minimum is { } integerMinimum && integerMinimum != Math.Truncate(integerMinimum)
                || definition.Maximum is { } integerMaximum && integerMaximum != Math.Truncate(integerMaximum)))
        {
            normalized = new DesignerCustomPropertyDefinition(string.Empty);
            error = $"Integer property '{definition.Name}' must use whole-number range limits.";
            return false;
        }

        if (!TryNormalizeDefinitionText(
                definition.DisplayName,
                "display name",
                allowLineBreaks: false,
                out var displayName,
                out error)
            || !TryNormalizeDefinitionText(
                definition.Category,
                "category",
                allowLineBreaks: false,
                out var category,
                out error)
            || !TryNormalizeDefinitionText(
                definition.Description,
                "description",
                allowLineBreaks: true,
                out var description,
                out error))
        {
            normalized = new DesignerCustomPropertyDefinition(string.Empty);
            error = $"Property '{definition.Name}' {error}";
            return false;
        }

        normalized = definition with
        {
            Name = normalizedName,
            Options = options,
            DisplayName = displayName,
            Category = category,
            Description = description,
        };
        error = string.Empty;
        return true;
    }

    private static bool TryNormalizeDefinitionText(
        string? value,
        string fieldName,
        bool allowLineBreaks,
        out string? normalized,
        out string error)
    {
        normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized is null)
        {
            error = string.Empty;
            return true;
        }

        try
        {
            XmlConvert.VerifyXmlChars(normalized);
        }
        catch (XmlException)
        {
            normalized = null;
            error = $"{fieldName} contains an invalid XML character.";
            return false;
        }

        if (!allowLineBreaks && normalized.Any(char.IsControl))
        {
            normalized = null;
            error = $"{fieldName} must use a single line.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryNormalizeValue(
        DesignerCustomPropertyDefinition? definition,
        string rawValue,
        out string normalizedValue,
        out string error)
    {
        definition ??= new DesignerCustomPropertyDefinition(string.Empty);
        try
        {
            XmlConvert.VerifyXmlChars(rawValue);
        }
        catch (XmlException)
        {
            normalizedValue = string.Empty;
            error = "value contains an invalid XML character.";
            return false;
        }

        var value = rawValue.Trim();
        switch (definition.Type)
        {
            case DesignerCustomPropertyType.String:
                normalizedValue = rawValue;
                error = string.Empty;
                return true;
            case DesignerCustomPropertyType.Boolean:
                if (bool.TryParse(value, out var booleanValue))
                {
                    normalizedValue = booleanValue.ToString();
                    error = string.Empty;
                    return true;
                }

                normalizedValue = string.Empty;
                error = "value must be True or False.";
                return false;
            case DesignerCustomPropertyType.Integer:
                if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue)
                    && IsWithinRange(integerValue, definition))
                {
                    normalizedValue = integerValue.ToString(CultureInfo.InvariantCulture);
                    error = string.Empty;
                    return true;
                }

                normalizedValue = string.Empty;
                error = $"value must be a whole number{FormatRange(definition)}.";
                return false;
            case DesignerCustomPropertyType.Double:
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue)
                    && double.IsFinite(doubleValue)
                    && IsWithinRange(doubleValue, definition))
                {
                    normalizedValue = doubleValue.ToString("G", CultureInfo.InvariantCulture);
                    error = string.Empty;
                    return true;
                }

                normalizedValue = string.Empty;
                error = $"value must be a finite number{FormatRange(definition)}.";
                return false;
            case DesignerCustomPropertyType.Color:
                if (Color.TryParse(value, out var color))
                {
                    normalizedValue = $"#{color.A:x2}{color.R:x2}{color.G:x2}{color.B:x2}";
                    error = string.Empty;
                    return true;
                }

                normalizedValue = string.Empty;
                error = "value must be a valid Avalonia color such as #FF3B82F6.";
                return false;
            case DesignerCustomPropertyType.Enum:
                var option = definition.Options?.FirstOrDefault(candidate => string.Equals(
                    candidate,
                    value,
                    StringComparison.OrdinalIgnoreCase));
                if (option is not null)
                {
                    normalizedValue = option;
                    error = string.Empty;
                    return true;
                }

                normalizedValue = string.Empty;
                error = $"value must be one of: {string.Join(", ", definition.Options ?? [])}.";
                return false;
            case DesignerCustomPropertyType.Thickness:
            case DesignerCustomPropertyType.CornerRadius:
                if (TryGetFourValueComponents(
                        definition.Type,
                        value,
                        out var components,
                        out error))
                {
                    normalizedValue = string.Join(",", components.Select(component =>
                        component.ToString("G", CultureInfo.InvariantCulture)));
                    return true;
                }

                normalizedValue = string.Empty;
                return false;
            default:
                normalizedValue = string.Empty;
                error = "value uses an unsupported custom property type.";
                return false;
        }
    }

    public static bool TryGetFourValueComponents(
        DesignerCustomPropertyType type,
        string rawValue,
        out IReadOnlyList<double> components,
        out string error)
    {
        if (type is not DesignerCustomPropertyType.Thickness
            and not DesignerCustomPropertyType.CornerRadius)
        {
            components = [];
            error = "value does not use a four-direction custom property type.";
            return false;
        }

        var parts = rawValue.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length is not 1 and not 2 and not 4
            || parts.Any(part => !double.TryParse(
                part,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out _)))
        {
            components = [];
            error = type == DesignerCustomPropertyType.Thickness
                ? "value must use 1, 2, or 4 finite numbers for left, top, right, and bottom."
                : "value must use 1, 2, or 4 non-negative finite numbers for top-left, top-right, bottom-right, and bottom-left.";
            return false;
        }

        var values = parts
            .Select(part => double.Parse(part, NumberStyles.Float, CultureInfo.InvariantCulture))
            .ToArray();
        if (values.Any(value => !double.IsFinite(value))
            || type == DesignerCustomPropertyType.CornerRadius
                && values.Any(value => value < 0))
        {
            components = [];
            error = type == DesignerCustomPropertyType.Thickness
                ? "value must use finite thickness numbers."
                : "value must use finite corner radii that are not negative.";
            return false;
        }

        components = values.Length switch
        {
            1 => [values[0], values[0], values[0], values[0]],
            2 when type == DesignerCustomPropertyType.Thickness
                => [values[0], values[1], values[0], values[1]],
            2 => [values[0], values[0], values[1], values[1]],
            _ => values,
        };
        error = string.Empty;
        return true;
    }

    public static string SerializePropertyDefinitions(
        IEnumerable<DesignerCustomPropertyDefinition> definitions)
        => JsonSerializer.Serialize(
            NormalizePropertyDefinitions(definitions),
            DefinitionSerializerOptions);

    public static bool TryDeserializePropertyDefinitions(
        string json,
        out IReadOnlyList<DesignerCustomPropertyDefinition> definitions)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<List<DesignerCustomPropertyDefinition>>(
                json,
                DefinitionSerializerOptions) ?? [];
            var normalized = new List<DesignerCustomPropertyDefinition>();
            foreach (var definition in parsed)
            {
                if (!TryNormalizePropertyDefinition(definition, out var candidate, out _)
                    || normalized.Any(existing => string.Equals(
                        existing.Name,
                        candidate.Name,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    definitions = [];
                    return false;
                }

                normalized.Add(candidate);
            }

            definitions = normalized.OrderBy(definition => definition.Name, StringComparer.Ordinal).ToList();
            return true;
        }
        catch (JsonException)
        {
            definitions = [];
            return false;
        }
    }

    public static string SerializeDeclaredPropertyNames(IEnumerable<string> declaredPropertyNames)
        => JsonSerializer.Serialize(NormalizeDeclaredPropertyNames(declaredPropertyNames));

    public static bool TryDeserializeDeclaredPropertyNames(
        string json,
        out IReadOnlyList<string> declaredPropertyNames)
    {
        try
        {
            declaredPropertyNames = NormalizeDeclaredPropertyNames(
                JsonSerializer.Deserialize<List<string>>(json) ?? []);
            return true;
        }
        catch (JsonException)
        {
            declaredPropertyNames = [];
            return false;
        }
    }

    public static IReadOnlyList<string> GetEditablePropertyNames(
        Control control,
        IEnumerable<string> declaredPropertyNames)
        => NormalizeDeclaredPropertyNames(declaredPropertyNames)
            .Where(propertyName => !DesignerBindingRuntime.IsSupportedProperty(
                control.GetType().Name,
                propertyName))
            .ToList();

    public static IReadOnlyList<string> FormatEditorLines(
        Control control,
        IEnumerable<string> editablePropertyNames)
        => ReadEditorValues(control, editablePropertyNames)
            .Select(pair => $"{pair.Key} = {pair.Value}")
            .ToList();

    public static IReadOnlyDictionary<string, string> ReadEditorValues(
        Control control,
        IEnumerable<string> editablePropertyNames)
    {
        if (control.Tag is not DesignerCustomControlMetadata metadata)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var boundProperties = DesignerBindingRuntime.ReadBindings(control)
            .Select(binding => binding.PropertyName)
            .ToHashSet(StringComparer.Ordinal);
        return editablePropertyNames
            .Where(propertyName => !boundProperties.Contains(propertyName)
                && metadata.DefaultProperties.ContainsKey(propertyName))
            .ToDictionary(
                propertyName => propertyName,
                propertyName => metadata.DefaultProperties[propertyName],
                StringComparer.Ordinal);
    }

    public static IReadOnlyList<DesignerCustomPropertyValueState> ReadValueStates(
        Control control,
        IEnumerable<string> editablePropertyNames,
        IEnumerable<DesignerCustomPropertyDefinition>? propertyDefinitions = null)
    {
        if (control.Tag is not DesignerCustomControlMetadata metadata)
        {
            return [];
        }

        var bindings = DesignerBindingRuntime.ReadBindings(control)
            .GroupBy(binding => binding.PropertyName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
        var definitions = NormalizePropertyDefinitions(propertyDefinitions, editablePropertyNames)
            .ToDictionary(definition => definition.Name, StringComparer.OrdinalIgnoreCase);
        var states = NormalizeDeclaredPropertyNames(editablePropertyNames)
            .Select(propertyName =>
            {
                var definition = definitions[propertyName];
                if (bindings.TryGetValue(propertyName, out var binding))
                {
                    return new DesignerCustomPropertyValueState(
                        propertyName,
                        FormatBindingValue(binding),
                        DesignerCustomPropertyValueSource.Binding,
                        definition);
                }

                if (metadata.DefaultProperties.TryGetValue(propertyName, out var localValue))
                {
                    return new DesignerCustomPropertyValueState(
                        propertyName,
                        localValue,
                        DesignerCustomPropertyValueSource.Local,
                        definition);
                }

                if (metadata.StyleProperties.TryGetValue(propertyName, out var styleValue))
                {
                    return new DesignerCustomPropertyValueState(
                        propertyName,
                        styleValue,
                        DesignerCustomPropertyValueSource.Style,
                        definition);
                }

                return new DesignerCustomPropertyValueState(
                    propertyName,
                    string.Empty,
                    DesignerCustomPropertyValueSource.Unset,
                    definition);
            })
            .OrderBy(state => string.Equals(
                state.CategoryLabel,
                "Custom",
                StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .ThenBy(state => state.CategoryLabel, StringComparer.OrdinalIgnoreCase)
            .ThenBy(state => state.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(state => state.PropertyName, StringComparer.Ordinal)
            .ToList();
        return ApplyCategoryHeaders(states);
    }

    public static IReadOnlyList<DesignerCustomPropertyValueState> ApplyCategoryHeaders(
        IEnumerable<DesignerCustomPropertyValueState> states)
    {
        string? previousCategory = null;
        var result = new List<DesignerCustomPropertyValueState>();
        foreach (var state in states)
        {
            var showsCategoryHeader = !string.Equals(
                state.CategoryLabel,
                previousCategory,
                StringComparison.OrdinalIgnoreCase);
            result.Add(state with { ShowsCategoryHeader = showsCategoryHeader });
            previousCategory = state.CategoryLabel;
        }

        return result;
    }

    public static bool TryParseEditorLines(
        IEnumerable<string> lines,
        IEnumerable<string> editablePropertyNames,
        out IReadOnlyDictionary<string, string> properties,
        out string error,
        IEnumerable<DesignerCustomPropertyDefinition>? propertyDefinitions = null)
    {
        var supportedNames = editablePropertyNames.ToDictionary(
            propertyName => propertyName,
            propertyName => propertyName,
            StringComparer.OrdinalIgnoreCase);
        var parsed = new Dictionary<string, string>(StringComparer.Ordinal);
        var definitions = NormalizePropertyDefinitions(propertyDefinitions, editablePropertyNames)
            .ToDictionary(definition => definition.Name, StringComparer.OrdinalIgnoreCase);
        var lineNumber = 0;
        foreach (var rawLine in lines)
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                properties = parsed;
                error = $"Line {lineNumber} must use Property = Value format.";
                return false;
            }

            var proposedName = line[..separator].Trim();
            if (!supportedNames.TryGetValue(proposedName, out var propertyName))
            {
                properties = parsed;
                error = $"Line {lineNumber}: property '{proposedName}' is not declared by this custom control.";
                return false;
            }

            if (parsed.ContainsKey(propertyName))
            {
                properties = parsed;
                error = $"Line {lineNumber} duplicates property '{propertyName}'.";
                return false;
            }

            var value = line[(separator + 1)..].Trim();
            if (!TryNormalizeValue(definitions[propertyName], value, out var normalizedValue, out var valueError))
            {
                properties = parsed;
                error = $"Line {lineNumber}: '{propertyName}' {valueError}";
                return false;
            }

            parsed[propertyName] = normalizedValue;
        }

        properties = parsed;
        error = string.Empty;
        return true;
    }

    public static void ReplaceValues(
        Control control,
        IEnumerable<string> editablePropertyNames,
        IReadOnlyDictionary<string, string> properties)
    {
        if (control.Tag is not DesignerCustomControlMetadata metadata)
        {
            return;
        }

        var currentProperties = new Dictionary<string, string>(
            metadata.DefaultProperties,
            StringComparer.Ordinal);
        foreach (var propertyName in editablePropertyNames)
        {
            currentProperties.Remove(propertyName);
        }

        foreach (var pair in properties)
        {
            currentProperties[pair.Key] = pair.Value;
        }

        control.Tag = metadata with { DefaultProperties = currentProperties };
    }

    private static string FormatBindingValue(DesignerBindingDefinition binding)
    {
        var parts = new List<string> { binding.Path };
        if (binding.Mode != DesignerBindingMode.Default)
        {
            parts.Add(binding.Mode.ToString());
        }

        if (!string.IsNullOrEmpty(binding.FallbackValue))
        {
            parts.Add($"fallback {binding.FallbackValue}");
        }

        return string.Join(" | ", parts);
    }

    private static bool IsWithinRange(
        double value,
        DesignerCustomPropertyDefinition definition)
        => (!definition.Minimum.HasValue || value >= definition.Minimum.Value)
            && (!definition.Maximum.HasValue || value <= definition.Maximum.Value);

    private static string FormatRange(DesignerCustomPropertyDefinition definition)
        => definition switch
        {
            { Minimum: { } minimum, Maximum: { } maximum }
                => $" from {minimum.ToString(CultureInfo.InvariantCulture)} to {maximum.ToString(CultureInfo.InvariantCulture)}",
            { Minimum: { } minimum }
                => $" greater than or equal to {minimum.ToString(CultureInfo.InvariantCulture)}",
            { Maximum: { } maximum }
                => $" less than or equal to {maximum.ToString(CultureInfo.InvariantCulture)}",
            _ => string.Empty,
        };
}
