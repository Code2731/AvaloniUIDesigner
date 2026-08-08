using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AvaloniaUIDesigner.App.Designer.Contracts;
using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Models;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed class ComponentPackLoader
{
    public bool TryLoad(
        string json,
        IComponentCatalog catalog,
        Func<string, bool> isDisplayNameAvailable,
        out ComponentPackLoadResult pack,
        out string error)
    {
        pack = default!;
        error = string.Empty;

        ComponentPackDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<ComponentPackDocument>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        }
        catch (JsonException exception)
        {
            error = $"Invalid component pack JSON: {exception.Message}";
            return false;
        }

        if (document?.Components is not { Count: > 0 })
        {
            error = "Component pack must contain at least one component.";
            return false;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var definitions = new List<DesignerComponentDefinition>();
        foreach (var component in document.Components)
        {
            if (string.IsNullOrWhiteSpace(component.DisplayName)
                || string.IsNullOrWhiteSpace(component.AvaloniaTypeName))
            {
                error = "Every component needs DisplayName and AvaloniaTypeName.";
                return false;
            }

            if (!names.Add(component.DisplayName) || !isDisplayNameAvailable(component.DisplayName))
            {
                error = $"A Toolbox item named '{component.DisplayName}' already exists.";
                return false;
            }

            var typeName = component.AvaloniaTypeName.Trim();
            var hasBaseDefinition = catalog.TryGet(typeName, out var baseDefinition);
            var isDesignOnly = !hasBaseDefinition || baseDefinition.IsDesignOnly;
            if (isDesignOnly && !component.DesignOnly)
            {
                error = $"Unsupported Avalonia type: {typeName}. Set DesignOnly to true to use a design-time placeholder.";
                return false;
            }

            var width = component.DefaultWidth ?? (hasBaseDefinition ? baseDefinition.DefaultWidth : 240);
            var height = component.DefaultHeight ?? (hasBaseDefinition ? baseDefinition.DefaultHeight : 96);
            if (!double.IsFinite(width) || width < 10 || width > 3840
                || !double.IsFinite(height) || height < 10 || height > 2160)
            {
                error = $"'{component.DisplayName}' must use a width between 10 and 3840 and a height between 10 and 2160.";
                return false;
            }

            var properties = component.DefaultProperties?
                .Where(property => !string.IsNullOrWhiteSpace(property.Key))
                .ToDictionary(property => property.Key, property => property.Value ?? string.Empty, StringComparer.Ordinal);
            var namePrefix = string.IsNullOrWhiteSpace(component.NamePrefix)
                ? CreateNamePrefix(component.DisplayName)
                : component.NamePrefix;
            if (!IsValidNamePrefix(namePrefix))
            {
                error = $"'{component.DisplayName}' has an invalid NamePrefix. Use letters, numbers, and underscores, starting with a letter or underscore.";
                return false;
            }

            var previewText = string.IsNullOrWhiteSpace(component.PreviewText)
                ? component.DisplayName.Trim()
                : component.PreviewText.Trim();
            var visualFactory = isDesignOnly
                ? () => DesignerCustomControlRuntime.CreatePlaceholder(
                    typeName,
                    previewText,
                    properties ?? new Dictionary<string, string>(StringComparer.Ordinal))
                : baseDefinition.VisualFactory;

            definitions.Add(new DesignerComponentDefinition(
                component.DisplayName,
                typeName,
                width,
                height,
                visualFactory,
                properties,
                namePrefix,
                isDesignOnly,
                previewText));
        }

        foreach (var definition in definitions)
        {
            if (!catalog.TryRegister(definition, out error))
            {
                return false;
            }
        }

        pack = new ComponentPackLoadResult(
            string.IsNullOrWhiteSpace(document.Name) ? "component pack" : document.Name,
            definitions);
        return true;
    }

    private static string CreateNamePrefix(string displayName)
    {
        var characters = displayName.Where(char.IsLetterOrDigit).ToArray();
        if (characters.Length == 0 || !char.IsLetter(characters[0]))
        {
            return "Component";
        }

        return new string(characters);
    }

    private static bool IsValidNamePrefix(string value)
        => !string.IsNullOrWhiteSpace(value)
            && (char.IsLetter(value[0]) || value[0] == '_')
            && value.All(character => char.IsLetterOrDigit(character) || character == '_');
}

public sealed record ComponentPackLoadResult(
    string Name,
    IReadOnlyList<DesignerComponentDefinition> Definitions);
