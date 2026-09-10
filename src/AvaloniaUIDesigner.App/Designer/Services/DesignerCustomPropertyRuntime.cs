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

    public bool UsesChoiceEditor => Type is DesignerCustomPropertyType.Boolean
        or DesignerCustomPropertyType.Enum;

    public bool UsesTextEditor => !UsesChoiceEditor;

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

        normalized = definition with
        {
            Name = normalizedName,
            Options = options,
        };
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
            default:
                normalizedValue = string.Empty;
                error = "value uses an unsupported custom property type.";
                return false;
        }
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
        return NormalizeDeclaredPropertyNames(editablePropertyNames)
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
            .ToList();
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
