using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace AvaloniaUIDesigner.App.Models;

public static class DesignerResourceReferenceMetadata
{
    private static readonly ConditionalWeakTable<Control, Dictionary<string, string>> References = new();

    public static void SetReference(Control control, string propertyName, string? resourceKey)
    {
        var references = References.GetOrCreateValue(control);
        if (string.IsNullOrWhiteSpace(resourceKey))
        {
            references.Remove(propertyName);
            return;
        }

        references[propertyName] = resourceKey;
    }

    public static bool TryGetReference(Control control, string propertyName, out string resourceKey)
    {
        if (References.TryGetValue(control, out var references)
            && references.TryGetValue(propertyName, out var storedKey))
        {
            resourceKey = storedKey;
            return true;
        }

        resourceKey = string.Empty;
        return false;
    }

    public static IReadOnlyDictionary<string, string> GetReferences(Control control)
        => References.TryGetValue(control, out var references)
            ? new Dictionary<string, string>(references, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);

    public static string FormatExpression(string resourceKey)
        => $"{{DynamicResource {resourceKey}}}";

    public static bool TryParseExpression(string value, out string resourceKey)
    {
        const string prefix = "{DynamicResource ";
        var trimmed = value.Trim();
        if (trimmed.StartsWith(prefix, StringComparison.Ordinal)
            && trimmed.EndsWith('}'))
        {
            resourceKey = trimmed[prefix.Length..^1].Trim();
            return !string.IsNullOrWhiteSpace(resourceKey);
        }

        resourceKey = string.Empty;
        return false;
    }
}
