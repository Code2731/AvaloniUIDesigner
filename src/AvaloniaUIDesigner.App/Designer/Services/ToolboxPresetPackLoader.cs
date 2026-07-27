using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Models;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed class ToolboxPresetPackLoader
{
    public bool TryLoad(
        string json,
        Func<string, bool> isDisplayNameAvailable,
        Func<string, bool> isTypeSupported,
        out ToolboxPresetPackLoadResult pack,
        out string error)
    {
        pack = default!;
        error = string.Empty;

        ToolboxPresetPackDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<ToolboxPresetPackDocument>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        }
        catch (JsonException exception)
        {
            error = $"Invalid Toolbox preset pack JSON: {exception.Message}";
            return false;
        }

        if (document?.Presets is not { Count: > 0 })
        {
            error = "Toolbox preset pack must contain at least one preset.";
            return false;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var presets = new List<ToolboxItem>();
        foreach (var definition in document.Presets)
        {
            if (definition is null)
            {
                error = "Every Toolbox preset entry must be an object.";
                return false;
            }

            var displayName = definition.DisplayName?.Trim();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                error = "Every Toolbox preset needs a DisplayName.";
                return false;
            }

            if (!names.Add(displayName) || !isDisplayNameAvailable(displayName))
            {
                error = $"A Toolbox item named '{displayName}' already exists.";
                return false;
            }

            if (definition.Elements is not { Count: > 0 })
            {
                error = $"Toolbox preset '{displayName}' must contain at least one control.";
                return false;
            }

            var elementNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in definition.Elements)
            {
                if (element is null)
                {
                    error = $"Toolbox preset '{displayName}' contains an invalid control entry.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(element.DisplayName)
                    || string.IsNullOrWhiteSpace(element.TypeName))
                {
                    error = $"Every control in Toolbox preset '{displayName}' needs DisplayName and TypeName.";
                    return false;
                }

                if (!isTypeSupported(element.TypeName))
                {
                    error = $"Toolbox preset '{displayName}' contains unsupported Avalonia type '{element.TypeName}'.";
                    return false;
                }

                if (!elementNames.Add(element.DisplayName))
                {
                    error = $"Toolbox preset '{displayName}' contains duplicate control name '{element.DisplayName}'.";
                    return false;
                }

                var hasParent = !string.IsNullOrWhiteSpace(element.ParentName);
                if ((!hasParent && element.ParentLayout != DesignerParentLayoutKind.None)
                    || (hasParent && element.ParentLayout != DesignerParentLayoutKind.Canvas))
                {
                    error = $"Toolbox preset '{displayName}' supports root controls and direct Canvas children only.";
                    return false;
                }

                if (!double.IsFinite(element.X)
                    || !double.IsFinite(element.Y)
                    || !double.IsFinite(element.Width)
                    || !double.IsFinite(element.Height)
                    || element.Width < 10
                    || element.Width > 3840
                    || element.Height < 10
                    || element.Height > 2160)
                {
                    error = $"Toolbox preset '{displayName}' contains a control with invalid bounds.";
                    return false;
                }
            }

            var rootElements = definition.Elements
                .Where(element => string.IsNullOrWhiteSpace(element.ParentName))
                .ToList();
            foreach (var child in definition.Elements.Where(element => !string.IsNullOrWhiteSpace(element.ParentName)))
            {
                var parent = rootElements.FirstOrDefault(element => string.Equals(
                    element.DisplayName,
                    child.ParentName,
                    StringComparison.OrdinalIgnoreCase));
                if (parent?.TypeName != "Avalonia.Controls.Canvas")
                {
                    error = $"Toolbox preset '{displayName}' can contain root controls or direct Canvas children only, and children must reference a root Canvas.";
                    return false;
                }

                if (child.CanvasChildIndex < 0
                    || !double.IsFinite(child.CanvasChildLeft)
                    || !double.IsFinite(child.CanvasChildTop))
                {
                    error = $"Toolbox preset '{displayName}' contains invalid Canvas child coordinates.";
                    return false;
                }
            }

            presets.Add(new ToolboxItem(
                displayName,
                string.IsNullOrWhiteSpace(definition.AvaloniaTypeName)
                    ? "Preset.Imported"
                    : definition.AvaloniaTypeName.Trim(),
                definition.Elements));
        }

        pack = new ToolboxPresetPackLoadResult(
            string.IsNullOrWhiteSpace(document.Name) ? "Toolbox preset pack" : document.Name.Trim(),
            presets);
        return true;
    }
}
