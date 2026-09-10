using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml;
using Avalonia.Controls;
using AvaloniaUIDesigner.App.Models;

namespace AvaloniaUIDesigner.App.Designer.Services;

public static class DesignerCustomPropertyRuntime
{
    public const string DeclaredPropertiesMetadataKey = "__customPropertyNames";

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

    public static bool TryParseEditorLines(
        IEnumerable<string> lines,
        IEnumerable<string> editablePropertyNames,
        out IReadOnlyDictionary<string, string> properties,
        out string error)
    {
        var supportedNames = editablePropertyNames.ToDictionary(
            propertyName => propertyName,
            propertyName => propertyName,
            StringComparer.OrdinalIgnoreCase);
        var parsed = new Dictionary<string, string>(StringComparer.Ordinal);
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
            try
            {
                XmlConvert.VerifyXmlChars(value);
            }
            catch (XmlException)
            {
                properties = parsed;
                error = $"Line {lineNumber}: value for '{propertyName}' contains an invalid XML character.";
                return false;
            }

            parsed[propertyName] = value;
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
}
