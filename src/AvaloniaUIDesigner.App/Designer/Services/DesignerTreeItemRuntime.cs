using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Avalonia.Controls;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerTreeItemDefinition(
    string Header,
    bool IsExpanded,
    List<DesignerTreeItemDefinition> Children);

public static class DesignerTreeItemRuntime
{
    public static TreeView CreateDefaultTreeView()
    {
        var treeView = new TreeView();
        ReplaceItems(treeView, CreateDefaultDefinitions());
        return treeView;
    }

    public static List<DesignerTreeItemDefinition> CreateDefaultDefinitions()
        =>
        [
            new(
                "Project",
                true,
                [
                    new("Views", true, []),
                    new("ViewModels", false, []),
                ]),
            new("Resources", false, []),
        ];

    public static List<DesignerTreeItemDefinition> ReadItems(TreeView treeView)
        => treeView.Items
            .OfType<TreeViewItem>()
            .Select(ReadItem)
            .ToList();

    public static void ReplaceItems(
        TreeView treeView,
        IReadOnlyList<DesignerTreeItemDefinition> definitions)
    {
        treeView.Items.Clear();
        foreach (var definition in definitions)
        {
            treeView.Items.Add(CreateItem(definition));
        }
    }

    public static string Serialize(TreeView treeView)
        => Serialize(ReadItems(treeView));

    public static string Serialize(IReadOnlyList<DesignerTreeItemDefinition> definitions)
        => JsonSerializer.Serialize(definitions);

    public static string Serialize(XElement treeViewElement)
        => Serialize(ReadItems(treeViewElement));

    public static bool TryDeserialize(
        string json,
        out List<DesignerTreeItemDefinition> definitions)
    {
        try
        {
            definitions = JsonSerializer.Deserialize<List<DesignerTreeItemDefinition>>(json)
                ?.Select(Normalize)
                .ToList()
                ?? [];
            return true;
        }
        catch (JsonException)
        {
            definitions = [];
            return false;
        }
    }

    public static IReadOnlyList<string> FormatEditorLines(TreeView treeView)
    {
        var lines = new List<string>();
        AppendEditorLines(lines, ReadItems(treeView), depth: 0);
        return lines;
    }

    public static bool TryParseEditorLines(
        IEnumerable<string> lines,
        out List<DesignerTreeItemDefinition> definitions,
        out string error)
    {
        definitions = [];
        error = string.Empty;
        var parents = new List<DesignerTreeItemDefinition>();
        var lineNumber = 0;

        foreach (var sourceLine in lines)
        {
            lineNumber++;
            var line = sourceLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.Contains('\t'))
            {
                error = $"Line {lineNumber}: use two spaces instead of tabs.";
                definitions = [];
                return false;
            }

            var leadingSpaces = line.TakeWhile(character => character == ' ').Count();
            if (leadingSpaces % 2 != 0)
            {
                error = $"Line {lineNumber}: indentation must use groups of two spaces.";
                definitions = [];
                return false;
            }

            var depth = leadingSpaces / 2;
            if (depth > parents.Count)
            {
                error = $"Line {lineNumber}: indentation skips a parent level.";
                definitions = [];
                return false;
            }

            var content = line[leadingSpaces..].Trim();
            var isExpanded = true;
            if (content.StartsWith("[-]", StringComparison.Ordinal))
            {
                content = content[3..].Trim();
            }
            else if (content.StartsWith("[+]", StringComparison.Ordinal))
            {
                isExpanded = false;
                content = content[3..].Trim();
            }

            if (content.Length == 0)
            {
                error = $"Line {lineNumber}: enter an item header after the state marker.";
                definitions = [];
                return false;
            }

            var definition = new DesignerTreeItemDefinition(content, isExpanded, []);
            if (depth == 0)
            {
                definitions.Add(definition);
            }
            else
            {
                parents[depth - 1].Children.Add(definition);
            }

            if (parents.Count > depth)
            {
                parents.RemoveRange(depth, parents.Count - depth);
            }

            parents.Add(definition);
        }

        return true;
    }

    public static bool AreEquivalent(
        IReadOnlyList<DesignerTreeItemDefinition> left,
        IReadOnlyList<DesignerTreeItemDefinition> right)
        => string.Equals(Serialize(left), Serialize(right), StringComparison.Ordinal);

    private static DesignerTreeItemDefinition ReadItem(TreeViewItem item)
        => new(
            item.Header?.ToString() ?? string.Empty,
            item.IsExpanded,
            item.Items.OfType<TreeViewItem>().Select(ReadItem).ToList());

    private static TreeViewItem CreateItem(DesignerTreeItemDefinition definition)
    {
        var item = new TreeViewItem
        {
            Header = definition.Header,
            IsExpanded = definition.IsExpanded,
        };
        foreach (var child in definition.Children)
        {
            item.Items.Add(CreateItem(child));
        }

        return item;
    }

    private static List<DesignerTreeItemDefinition> ReadItems(XElement treeViewElement)
    {
        var itemsHost = treeViewElement.Elements().FirstOrDefault(element =>
                string.Equals(element.Name.LocalName, "TreeView.Items", StringComparison.OrdinalIgnoreCase))
            ?? treeViewElement;
        return itemsHost.Elements()
            .Where(element => string.Equals(
                element.Name.LocalName,
                "TreeViewItem",
                StringComparison.OrdinalIgnoreCase))
            .Select(ReadItem)
            .ToList();
    }

    private static DesignerTreeItemDefinition ReadItem(XElement element)
    {
        var itemsHost = element.Elements().FirstOrDefault(child =>
                string.Equals(child.Name.LocalName, "TreeViewItem.Items", StringComparison.OrdinalIgnoreCase))
            ?? element;
        var children = itemsHost.Elements()
            .Where(child => string.Equals(
                child.Name.LocalName,
                "TreeViewItem",
                StringComparison.OrdinalIgnoreCase))
            .Select(ReadItem)
            .ToList();
        var isExpanded = bool.TryParse(element.Attribute("IsExpanded")?.Value, out var parsed)
            && parsed;
        return new DesignerTreeItemDefinition(
            element.Attribute("Header")?.Value ?? element.Attribute("Content")?.Value ?? string.Empty,
            isExpanded,
            children);
    }

    private static DesignerTreeItemDefinition Normalize(DesignerTreeItemDefinition definition)
        => new(
            definition.Header ?? string.Empty,
            definition.IsExpanded,
            definition.Children?.Select(Normalize).ToList() ?? []);

    private static void AppendEditorLines(
        ICollection<string> lines,
        IEnumerable<DesignerTreeItemDefinition> definitions,
        int depth)
    {
        foreach (var definition in definitions)
        {
            var marker = definition.IsExpanded ? "[-]" : "[+]";
            lines.Add($"{new string(' ', depth * 2)}{marker} {definition.Header}");
            AppendEditorLines(lines, definition.Children, depth + 1);
        }
    }
}
