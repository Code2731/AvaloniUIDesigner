using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Input;

namespace AvaloniaUIDesigner.App.Designer.Services;

public enum DesignerMenuEntryKind
{
    Item,
    Separator,
}

public sealed record DesignerMenuEntryDefinition(
    DesignerMenuEntryKind Kind,
    string Header,
    MenuItemToggleType ToggleType,
    bool IsChecked,
    string InputGesture,
    string GroupName,
    List<DesignerMenuEntryDefinition> Children);

public static class DesignerMenuItemRuntime
{
    public static Menu CreateDefaultMenu()
    {
        var menu = new Menu();
        ReplaceItems(menu, CreateDefaultDefinitions());
        return menu;
    }

    public static List<DesignerMenuEntryDefinition> CreateDefaultDefinitions()
        =>
        [
            Item("File",
            [
                Item("New", inputGesture: "Ctrl+N"),
                Item("Open", inputGesture: "Ctrl+O"),
                Separator(),
                Item("Exit", inputGesture: "Alt+F4"),
            ]),
            Item("Edit",
            [
                Item("Undo", inputGesture: "Ctrl+Z"),
                Item("Redo", inputGesture: "Ctrl+Y"),
                Separator(),
                Item("Auto Save", MenuItemToggleType.CheckBox, isChecked: true),
            ]),
            Item("View",
            [
                Item("Light", MenuItemToggleType.Radio, isChecked: true, groupName: "Theme"),
                Item("Dark", MenuItemToggleType.Radio, groupName: "Theme"),
            ]),
        ];

    public static List<DesignerMenuEntryDefinition> ReadItems(Menu menu)
        => ReadItems(menu.Items);

    public static void ReplaceItems(
        Menu menu,
        IReadOnlyList<DesignerMenuEntryDefinition> definitions)
    {
        menu.Items.Clear();
        foreach (var definition in definitions)
        {
            menu.Items.Add(CreateControl(definition));
        }
    }

    public static string Serialize(Menu menu)
        => Serialize(ReadItems(menu));

    public static string Serialize(IReadOnlyList<DesignerMenuEntryDefinition> definitions)
        => JsonSerializer.Serialize(definitions);

    public static string Serialize(XElement menuElement)
        => Serialize(ReadItems(menuElement));

    public static bool TryDeserialize(
        string json,
        out List<DesignerMenuEntryDefinition> definitions)
    {
        try
        {
            definitions = JsonSerializer.Deserialize<List<DesignerMenuEntryDefinition>>(json)
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

    public static IReadOnlyList<string> FormatEditorLines(Menu menu)
    {
        var lines = new List<string>();
        AppendEditorLines(lines, ReadItems(menu), depth: 0);
        return lines;
    }

    public static bool TryParseEditorLines(
        IEnumerable<string> lines,
        out List<DesignerMenuEntryDefinition> definitions,
        out string error)
    {
        definitions = [];
        error = string.Empty;
        var parents = new List<DesignerMenuEntryDefinition>();
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
                return Fail(lineNumber, "use two spaces instead of tabs.", out definitions, out error);
            }

            var leadingSpaces = line.TakeWhile(character => character == ' ').Count();
            if (leadingSpaces % 2 != 0)
            {
                return Fail(
                    lineNumber,
                    "indentation must use groups of two spaces.",
                    out definitions,
                    out error);
            }

            var depth = leadingSpaces / 2;
            if (depth > parents.Count)
            {
                return Fail(
                    lineNumber,
                    "indentation skips a parent level or follows a separator.",
                    out definitions,
                    out error);
            }

            var content = line[leadingSpaces..].Trim();
            DesignerMenuEntryDefinition definition;
            if (string.Equals(content, "---", StringComparison.Ordinal))
            {
                definition = Separator();
            }
            else if (!TryParseItem(content, lineNumber, out definition, out error))
            {
                definitions = [];
                return false;
            }

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

            if (definition.Kind == DesignerMenuEntryKind.Item)
            {
                parents.Add(definition);
            }
        }

        return true;
    }

    public static bool AreEquivalent(
        IReadOnlyList<DesignerMenuEntryDefinition> left,
        IReadOnlyList<DesignerMenuEntryDefinition> right)
        => string.Equals(Serialize(left), Serialize(right), StringComparison.Ordinal);

    public static int CountEntries(IEnumerable<DesignerMenuEntryDefinition> definitions)
        => definitions.Sum(definition => 1 + CountEntries(definition.Children));

    private static DesignerMenuEntryDefinition Item(
        string header,
        List<DesignerMenuEntryDefinition>? children = null,
        MenuItemToggleType toggleType = MenuItemToggleType.None,
        bool isChecked = false,
        string inputGesture = "",
        string groupName = "")
        => new(
            DesignerMenuEntryKind.Item,
            header,
            toggleType,
            isChecked,
            inputGesture,
            groupName,
            children ?? []);

    private static DesignerMenuEntryDefinition Item(
        string header,
        MenuItemToggleType toggleType,
        bool isChecked = false,
        string inputGesture = "",
        string groupName = "")
        => Item(header, null, toggleType, isChecked, inputGesture, groupName);

    private static DesignerMenuEntryDefinition Separator()
        => new(
            DesignerMenuEntryKind.Separator,
            string.Empty,
            MenuItemToggleType.None,
            false,
            string.Empty,
            string.Empty,
            []);

    private static List<DesignerMenuEntryDefinition> ReadItems(IEnumerable<object?> items)
        => items.Select(ReadItem).Where(definition => definition is not null).Cast<DesignerMenuEntryDefinition>().ToList();

    private static DesignerMenuEntryDefinition? ReadItem(object? item)
    {
        if (item is Separator)
        {
            return Separator();
        }

        if (item is not MenuItem menuItem)
        {
            return null;
        }

        return Item(
            menuItem.Header?.ToString() ?? string.Empty,
            ReadItems(menuItem.Items),
            menuItem.ToggleType,
            menuItem.IsChecked,
            menuItem.InputGesture?.ToString() ?? menuItem.HotKey?.ToString() ?? string.Empty,
            menuItem.GroupName ?? string.Empty);
    }

    private static Control CreateControl(DesignerMenuEntryDefinition definition)
    {
        if (definition.Kind == DesignerMenuEntryKind.Separator)
        {
            return new Separator();
        }

        var menuItem = new MenuItem
        {
            Header = definition.Header,
            ToggleType = definition.ToggleType,
            IsChecked = definition.IsChecked,
            GroupName = string.IsNullOrWhiteSpace(definition.GroupName)
                ? null
                : definition.GroupName,
        };
        if (!string.IsNullOrWhiteSpace(definition.InputGesture))
        {
            var gesture = KeyGesture.Parse(definition.InputGesture);
            menuItem.InputGesture = gesture;
            menuItem.HotKey = gesture;
        }

        foreach (var child in definition.Children)
        {
            menuItem.Items.Add(CreateControl(child));
        }

        return menuItem;
    }

    private static List<DesignerMenuEntryDefinition> ReadItems(XElement menuElement)
    {
        var itemsHost = menuElement.Elements().FirstOrDefault(element =>
                string.Equals(element.Name.LocalName, "Menu.Items", StringComparison.OrdinalIgnoreCase))
            ?? menuElement;
        return itemsHost.Elements()
            .Select(ReadItem)
            .Where(definition => definition is not null)
            .Cast<DesignerMenuEntryDefinition>()
            .ToList();
    }

    private static DesignerMenuEntryDefinition? ReadItem(XElement element)
    {
        if (string.Equals(element.Name.LocalName, "Separator", StringComparison.OrdinalIgnoreCase))
        {
            return Separator();
        }

        if (!string.Equals(element.Name.LocalName, "MenuItem", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var itemsHost = element.Elements().FirstOrDefault(child =>
                string.Equals(child.Name.LocalName, "MenuItem.Items", StringComparison.OrdinalIgnoreCase))
            ?? element;
        var children = itemsHost.Elements()
            .Select(ReadItem)
            .Where(definition => definition is not null)
            .Cast<DesignerMenuEntryDefinition>()
            .ToList();
        var toggleType = Enum.TryParse<MenuItemToggleType>(
            element.Attribute("ToggleType")?.Value,
            ignoreCase: true,
            out var parsedToggleType)
            ? parsedToggleType
            : MenuItemToggleType.None;
        var isChecked = bool.TryParse(element.Attribute("IsChecked")?.Value, out var parsedIsChecked)
            && parsedIsChecked;
        return Item(
            element.Attribute("Header")?.Value ?? string.Empty,
            children,
            toggleType,
            isChecked,
            element.Attribute("InputGesture")?.Value
                ?? element.Attribute("HotKey")?.Value
                ?? string.Empty,
            element.Attribute("GroupName")?.Value ?? string.Empty);
    }

    private static DesignerMenuEntryDefinition Normalize(DesignerMenuEntryDefinition definition)
        => new(
            definition.Kind,
            definition.Header ?? string.Empty,
            definition.ToggleType,
            definition.IsChecked,
            definition.InputGesture ?? string.Empty,
            definition.GroupName ?? string.Empty,
            definition.Children?.Select(Normalize).ToList() ?? []);

    private static void AppendEditorLines(
        ICollection<string> lines,
        IEnumerable<DesignerMenuEntryDefinition> definitions,
        int depth)
    {
        foreach (var definition in definitions)
        {
            var indent = new string(' ', depth * 2);
            if (definition.Kind == DesignerMenuEntryKind.Separator)
            {
                lines.Add($"{indent}---");
                continue;
            }

            var marker = definition.ToggleType switch
            {
                MenuItemToggleType.CheckBox => definition.IsChecked ? "[x] " : "[ ] ",
                MenuItemToggleType.Radio => definition.IsChecked ? "(x) " : "( ) ",
                _ => string.Empty,
            };
            var gesture = string.IsNullOrWhiteSpace(definition.InputGesture)
                ? string.Empty
                : $" | {definition.InputGesture}";
            var group = definition.ToggleType == MenuItemToggleType.Radio
                && !string.IsNullOrWhiteSpace(definition.GroupName)
                    ? $" {{{definition.GroupName}}}"
                    : string.Empty;
            lines.Add($"{indent}{marker}{definition.Header}{gesture}{group}");
            AppendEditorLines(lines, definition.Children, depth + 1);
        }
    }

    private static bool TryParseItem(
        string content,
        int lineNumber,
        out DesignerMenuEntryDefinition definition,
        out string error)
    {
        var toggleType = MenuItemToggleType.None;
        var isChecked = false;
        if (content.StartsWith("[x]", StringComparison.OrdinalIgnoreCase))
        {
            toggleType = MenuItemToggleType.CheckBox;
            isChecked = true;
            content = content[3..].Trim();
        }
        else if (content.StartsWith("[ ]", StringComparison.Ordinal))
        {
            toggleType = MenuItemToggleType.CheckBox;
            content = content[3..].Trim();
        }
        else if (content.StartsWith("(x)", StringComparison.OrdinalIgnoreCase))
        {
            toggleType = MenuItemToggleType.Radio;
            isChecked = true;
            content = content[3..].Trim();
        }
        else if (content.StartsWith("( )", StringComparison.Ordinal))
        {
            toggleType = MenuItemToggleType.Radio;
            content = content[3..].Trim();
        }

        var groupName = string.Empty;
        if (content.EndsWith('}'))
        {
            var groupStart = content.LastIndexOf(" {", StringComparison.Ordinal);
            if (groupStart >= 0)
            {
                groupName = content[(groupStart + 2)..^1].Trim();
                content = content[..groupStart].TrimEnd();
            }
        }

        if (toggleType == MenuItemToggleType.Radio && string.IsNullOrWhiteSpace(groupName))
        {
            definition = Separator();
            error = $"Line {lineNumber}: radio items require a {{GroupName}} suffix.";
            return false;
        }

        var inputGesture = string.Empty;
        var gestureStart = content.LastIndexOf(" | ", StringComparison.Ordinal);
        if (gestureStart >= 0)
        {
            inputGesture = content[(gestureStart + 3)..].Trim();
            content = content[..gestureStart].TrimEnd();
            if (inputGesture.Length == 0)
            {
                definition = Separator();
                error = $"Line {lineNumber}: enter a shortcut after '|'.";
                return false;
            }

            try
            {
                _ = KeyGesture.Parse(inputGesture);
            }
            catch (Exception exception) when (
                exception is FormatException or ArgumentException)
            {
                definition = Separator();
                error = $"Line {lineNumber}: '{inputGesture}' is not a valid Avalonia key gesture.";
                return false;
            }
        }

        if (content.Length == 0)
        {
            definition = Separator();
            error = $"Line {lineNumber}: enter a menu item header.";
            return false;
        }

        definition = Item(content, toggleType, isChecked, inputGesture, groupName);
        error = string.Empty;
        return true;
    }

    private static bool Fail(
        int lineNumber,
        string message,
        out List<DesignerMenuEntryDefinition> definitions,
        out string error)
    {
        definitions = [];
        error = $"Line {lineNumber}: {message}";
        return false;
    }
}
