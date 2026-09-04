using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaUIDesigner.App.Designer.Core;

namespace AvaloniaUIDesigner.App.Designer.Services;

public enum DesignerLayoutDiagnosticSeverity
{
    Error,
    Warning,
}

public sealed record DesignerLayoutDiagnostic(
    DesignerLayoutDiagnosticSeverity Severity,
    string Code,
    string? ElementName,
    string Message)
{
    public string SeverityLabel => Severity.ToString().ToUpperInvariant();

    public override string ToString()
        => ElementName is null
            ? $"{SeverityLabel} {Code}: {Message}"
            : $"{SeverityLabel} {Code} - {ElementName}: {Message}";
}

public static class DesignerLayoutValidator
{
    public static IReadOnlyList<DesignerLayoutDiagnostic> Validate(
        DesignerCanvasDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var diagnostics = new List<DesignerLayoutDiagnostic>();
        var settings = document.Settings ?? new DesignerCanvasSettings();
        var elements = document.Elements ?? Array.Empty<DesignerElementSnapshot>();

        ValidateArtboard(settings, diagnostics);
        ValidateElementNames(elements, diagnostics);

        var elementsByName = new Dictionary<string, DesignerElementSnapshot>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var element in elements)
        {
            var name = NormalizeName(element.DisplayName);
            if (name is not null && !elementsByName.ContainsKey(name))
            {
                elementsByName.Add(name, element);
            }
        }

        foreach (var element in elements)
        {
            ValidateGeometry(element, settings, diagnostics);
            ValidateParentRelationship(element, elementsByName, diagnostics);
        }

        ValidateParentCycles(elements, elementsByName, diagnostics);

        return diagnostics
            .OrderBy(diagnostic => diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.ElementName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ToList();
    }

    private static void ValidateArtboard(
        DesignerCanvasSettings settings,
        ICollection<DesignerLayoutDiagnostic> diagnostics)
    {
        if (!IsFinitePositive(settings.Width) || !IsFinitePositive(settings.Height))
        {
            diagnostics.Add(new(
                DesignerLayoutDiagnosticSeverity.Error,
                "ARTBOARD_SIZE",
                null,
                "Artboard width and height must be finite values greater than zero."));
        }
    }

    private static void ValidateElementNames(
        IReadOnlyList<DesignerElementSnapshot> elements,
        ICollection<DesignerLayoutDiagnostic> diagnostics)
    {
        foreach (var group in elements
                     .GroupBy(element => NormalizeName(element.DisplayName) ?? string.Empty,
                         StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(group.Key))
            {
                foreach (var element in group)
                {
                    diagnostics.Add(new(
                        DesignerLayoutDiagnosticSeverity.Error,
                        "EMPTY_NAME",
                        null,
                        "Every design element must have a non-empty display name."));
                }

                continue;
            }

            if (group.Count() <= 1)
            {
                continue;
            }

            foreach (var element in group)
            {
                diagnostics.Add(new(
                    DesignerLayoutDiagnosticSeverity.Error,
                    "DUPLICATE_NAME",
                    element.DisplayName,
                    $"Display name '{group.Key}' is shared by {group.Count()} elements."));
            }
        }
    }

    private static void ValidateGeometry(
        DesignerElementSnapshot element,
        DesignerCanvasSettings settings,
        ICollection<DesignerLayoutDiagnostic> diagnostics)
    {
        var name = NormalizeName(element.DisplayName);
        if (!IsFinite(element.X)
            || !IsFinite(element.Y)
            || !IsFinite(element.Width)
            || !IsFinite(element.Height))
        {
            diagnostics.Add(new(
                DesignerLayoutDiagnosticSeverity.Error,
                "NON_FINITE_BOUNDS",
                name,
                "X, Y, width, and height must all be finite values."));
            return;
        }

        if (element.Width <= 0 || element.Height <= 0)
        {
            diagnostics.Add(new(
                DesignerLayoutDiagnosticSeverity.Error,
                "NON_POSITIVE_SIZE",
                name,
                "Width and height must both be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(element.ParentName)
            && IsFinitePositive(settings.Width)
            && IsFinitePositive(settings.Height)
            && (element.X < 0
                || element.Y < 0
                || element.X + element.Width > settings.Width
                || element.Y + element.Height > settings.Height))
        {
            diagnostics.Add(new(
                DesignerLayoutDiagnosticSeverity.Warning,
                "OUTSIDE_ARTBOARD",
                name,
                $"Root bounds ({element.X:0.###}, {element.Y:0.###}, {element.Width:0.###}, {element.Height:0.###}) extend outside the {settings.Width:0.###} x {settings.Height:0.###} artboard."));
        }
    }

    private static void ValidateParentRelationship(
        DesignerElementSnapshot element,
        IReadOnlyDictionary<string, DesignerElementSnapshot> elementsByName,
        ICollection<DesignerLayoutDiagnostic> diagnostics)
    {
        var elementName = NormalizeName(element.DisplayName);
        if (string.IsNullOrWhiteSpace(element.ParentName))
        {
            if (element.ParentLayout != DesignerParentLayoutKind.None)
            {
                diagnostics.Add(new(
                    DesignerLayoutDiagnosticSeverity.Error,
                    "ROOT_LAYOUT",
                    elementName,
                    "A root element cannot retain a parent layout assignment."));
            }

            return;
        }

        var parentName = NormalizeName(element.ParentName);
        if (parentName is null || !elementsByName.TryGetValue(parentName, out var parent))
        {
            diagnostics.Add(new(
                DesignerLayoutDiagnosticSeverity.Error,
                "MISSING_PARENT",
                elementName,
                $"Parent '{parentName ?? element.ParentName}' does not exist in the document."));
            return;
        }

        var expectedLayout = GetExpectedParentLayout(parent.TypeName);
        if (expectedLayout is null)
        {
            return;
        }

        if (element.ParentLayout == DesignerParentLayoutKind.None)
        {
            diagnostics.Add(new(
                DesignerLayoutDiagnosticSeverity.Error,
                "MISSING_PARENT_LAYOUT",
                elementName,
                $"Parent '{parent.DisplayName}' requires a {expectedLayout} child layout assignment."));
        }
        else if (element.ParentLayout != expectedLayout)
        {
            diagnostics.Add(new(
                DesignerLayoutDiagnosticSeverity.Error,
                "PARENT_LAYOUT_MISMATCH",
                elementName,
                $"Parent '{parent.DisplayName}' is a {expectedLayout}, but the child is marked as {element.ParentLayout}."));
        }
    }

    private static void ValidateParentCycles(
        IReadOnlyList<DesignerElementSnapshot> elements,
        IReadOnlyDictionary<string, DesignerElementSnapshot> elementsByName,
        ICollection<DesignerLayoutDiagnostic> diagnostics)
    {
        var reportedCycles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in elements)
        {
            var path = new List<string>();
            var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var current = element;
            while (true)
            {
                var currentName = NormalizeName(current.DisplayName);
                if (currentName is null)
                {
                    break;
                }

                if (indexes.TryGetValue(currentName, out var cycleStart))
                {
                    var cycle = path
                        .Skip(cycleStart)
                        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    var signature = string.Join("|", cycle);
                    if (cycle.Count > 0 && reportedCycles.Add(signature))
                    {
                        diagnostics.Add(new(
                            DesignerLayoutDiagnosticSeverity.Error,
                            "PARENT_CYCLE",
                            cycle[0],
                            $"Parent relationship forms a cycle: {string.Join(" -> ", cycle)}."));
                    }

                    break;
                }

                indexes.Add(currentName, path.Count);
                path.Add(currentName);
                var parentName = NormalizeName(current.ParentName);
                if (parentName is null
                    || !elementsByName.TryGetValue(parentName, out var parent))
                {
                    break;
                }

                current = parent;
            }
        }
    }

    private static DesignerParentLayoutKind? GetExpectedParentLayout(string typeName)
        => typeName switch
        {
            "Avalonia.Controls.Grid" => DesignerParentLayoutKind.Grid,
            "Avalonia.Controls.StackPanel" => DesignerParentLayoutKind.StackPanel,
            "Avalonia.Controls.DockPanel" => DesignerParentLayoutKind.DockPanel,
            "Avalonia.Controls.WrapPanel" => DesignerParentLayoutKind.WrapPanel,
            "Avalonia.Controls.Primitives.UniformGrid" => DesignerParentLayoutKind.UniformGrid,
            "Avalonia.Controls.Canvas" => DesignerParentLayoutKind.Canvas,
            "Avalonia.Controls.TabControl" => DesignerParentLayoutKind.TabControl,
            "Avalonia.Controls.SplitView" => DesignerParentLayoutKind.SplitView,
            "Avalonia.Controls.Border" => DesignerParentLayoutKind.Content,
            "Avalonia.Controls.ContentControl" => DesignerParentLayoutKind.Content,
            "Avalonia.Controls.UserControl" => DesignerParentLayoutKind.Content,
            "Avalonia.Controls.ScrollViewer" => DesignerParentLayoutKind.Content,
            "Avalonia.Controls.Expander" => DesignerParentLayoutKind.Content,
            _ => null,
        };

    private static string? NormalizeName(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsFinite(double value)
        => !double.IsNaN(value) && !double.IsInfinity(value);

    private static bool IsFinitePositive(double value)
        => IsFinite(value) && value > 0;
}
