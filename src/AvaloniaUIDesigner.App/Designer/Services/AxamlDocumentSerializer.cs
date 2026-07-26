using System.Collections.Generic;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using AvaloniaUIDesigner.App.Designer.Contracts;
using AvaloniaUIDesigner.App.Designer.Core;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed class AxamlDocumentSerializer : IDesignerSerializer
{
    public string Serialize(DesignerCanvasDocument document)
    {
        var sb = new StringBuilder();
        var settings = document.Settings ?? new DesignerCanvasSettings();
        sb.Append("<Canvas xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" Width=\"");
        sb.Append(ToInvariantString(settings.Width));
        sb.Append("\" Height=\"");
        sb.Append(ToInvariantString(settings.Height));
        sb.Append("\" Background=\"");
        sb.Append(EscapeXmlAttribute(settings.Background));
        sb.AppendLine("\">");

        if (document.ColorResources is { Count: > 0 })
        {
            sb.AppendLine("  <Canvas.Resources>");
            foreach (var pair in document.ColorResources)
            {
                sb.Append("    <SolidColorBrush x:Key=\"");
                sb.Append(EscapeXmlAttribute(pair.Key));
                sb.Append("\" Color=\"");
                sb.Append(EscapeXmlAttribute(pair.Value));
                sb.AppendLine("\" />");
            }

            sb.AppendLine("  </Canvas.Resources>");
        }

        if (document.Styles is { Count: > 0 })
        {
            sb.AppendLine("  <Canvas.Styles>");
            foreach (var style in document.Styles)
            {
                sb.Append("    <Style Selector=\"");
                sb.Append(EscapeXmlAttribute(style.Selector));
                sb.AppendLine("\">");
                foreach (var setter in style.Setters)
                {
                    sb.Append("      <Setter Property=\"");
                    sb.Append(EscapeXmlAttribute(setter.Key));
                    sb.Append("\" Value=\"");
                    sb.Append(EscapeXmlAttribute(setter.Value));
                    sb.AppendLine("\" />");
                }

                sb.AppendLine("    </Style>");
            }

            sb.AppendLine("  </Canvas.Styles>");
        }

        var containersByName = document.Elements
            .Where(IsContainer)
            .ToDictionary(element => element.DisplayName, StringComparer.OrdinalIgnoreCase);
        var childrenByParent = document.Elements
            .Where(element => element.ParentName is not null && containersByName.ContainsKey(element.ParentName))
            .GroupBy(element => element.ParentName!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        foreach (var element in document.Elements.Where(element =>
                     element.ParentName is null || !containersByName.ContainsKey(element.ParentName)))
        {
            AppendElement(sb, element, "  ", childrenByParent, parent: null);
        }

        sb.AppendLine("</Canvas>");
        return sb.ToString();
    }

    private static string ToInvariantString(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string MapToTagName(string typeName)
    {
        var index = typeName.LastIndexOf('.');
        return index >= 0 ? typeName[(index + 1)..] : typeName;
    }

    private static void AppendElement(
        StringBuilder sb,
        DesignerElementSnapshot element,
        string indent,
        IReadOnlyDictionary<string, List<DesignerElementSnapshot>> childrenByParent,
        DesignerElementSnapshot? parent)
    {
        sb.Append(indent);
        sb.Append('<');
        sb.Append(MapToTagName(element.TypeName));
        sb.Append(" x:Name=\"");
        sb.Append(EscapeXmlAttribute(element.DisplayName));
        sb.Append('"');
        if (parent is null)
        {
            sb.Append(" Canvas.Left=\"");
            sb.Append(ToInvariantString(element.X));
            sb.Append("\" Canvas.Top=\"");
            sb.Append(ToInvariantString(element.Y));
            sb.Append("\" Width=\"");
            sb.Append(ToInvariantString(element.Width));
            sb.Append("\" Height=\"");
            sb.Append(ToInvariantString(element.Height));
            sb.Append('"');
        }
        else if (string.Equals(parent.TypeName, "Avalonia.Controls.Grid", StringComparison.Ordinal))
        {
            AppendGridCellAttributes(sb, element);
        }
        else if (string.Equals(
                     parent.TypeName,
                     "Avalonia.Controls.StackPanel",
                     StringComparison.Ordinal))
        {
            AppendStackPanelItemSize(sb, element, parent);
        }
        else if (string.Equals(
                     parent.TypeName,
                     "Avalonia.Controls.DockPanel",
                     StringComparison.Ordinal))
        {
            AppendDockPanelAttributes(sb, element, parent, childrenByParent);
        }
        else if (string.Equals(
                     parent.TypeName,
                     "Avalonia.Controls.Canvas",
                     StringComparison.Ordinal))
        {
            sb.Append(" Canvas.Left=\"");
            sb.Append(ToInvariantString(element.CanvasChildLeft));
            sb.Append("\" Canvas.Top=\"");
            sb.Append(ToInvariantString(element.CanvasChildTop));
            sb.Append("\" Width=\"");
            sb.Append(ToInvariantString(element.Width));
            sb.Append("\" Height=\"");
            sb.Append(ToInvariantString(element.Height));
            sb.Append('"');
        }

        AppendVisualAttributes(sb, element.VisualProperties);
        if (string.Equals(element.TypeName, "Avalonia.Controls.Menu", StringComparison.Ordinal))
        {
            sb.AppendLine(">");
            AppendMenuEntries(sb, ReadMenuEntries(element), indent + "  ");
            sb.Append(indent);
            sb.AppendLine("</Menu>");
            return;
        }

        if (string.Equals(element.TypeName, "Avalonia.Controls.TreeView", StringComparison.Ordinal))
        {
            sb.AppendLine(">");
            AppendTreeItems(sb, ReadTreeItems(element), indent + "  ");
            sb.Append(indent);
            sb.AppendLine("</TreeView>");
            return;
        }

        if (string.Equals(element.TypeName, "Avalonia.Controls.TabControl", StringComparison.Ordinal))
        {
            sb.AppendLine(">");
            var tabHeaders = ReadTabHeaders(element);
            childrenByParent.TryGetValue(element.DisplayName, out var tabChildren);
            for (var tabIndex = 0; tabIndex < tabHeaders.Count; tabIndex++)
            {
                sb.Append(indent);
                sb.Append("  <TabItem Header=\"");
                sb.Append(EscapeXmlAttribute(tabHeaders[tabIndex]));
                sb.AppendLine("\">");
                var tabChild = tabChildren?.FirstOrDefault(child =>
                    child.ParentLayout == DesignerParentLayoutKind.TabControl
                    && child.TabIndex == tabIndex);
                if (tabChild is not null)
                {
                    AppendElement(sb, tabChild, indent + "    ", childrenByParent, element);
                }
                else
                {
                    sb.Append(indent);
                    sb.Append("    <TextBlock Text=\"");
                    sb.Append(EscapeXmlAttribute($"{tabHeaders[tabIndex]} content"));
                    sb.AppendLine("\" />");
                }

                sb.Append(indent);
                sb.AppendLine("  </TabItem>");
            }

            sb.Append(indent);
            sb.AppendLine("</TabControl>");
            return;
        }

        if (string.Equals(element.TypeName, "Avalonia.Controls.SplitView", StringComparison.Ordinal))
        {
            sb.AppendLine(">");
            childrenByParent.TryGetValue(element.DisplayName, out var splitChildren);
            var paneChild = splitChildren?.FirstOrDefault(child =>
                child.ParentLayout == DesignerParentLayoutKind.SplitView
                && child.SplitViewSlot == DesignerSplitViewSlot.Pane);
            var contentChild = splitChildren?.FirstOrDefault(child =>
                child.ParentLayout == DesignerParentLayoutKind.SplitView
                && child.SplitViewSlot == DesignerSplitViewSlot.Content);

            sb.Append(indent);
            sb.AppendLine("  <SplitView.Pane>");
            if (paneChild is not null)
            {
                AppendElement(sb, paneChild, indent + "    ", childrenByParent, element);
            }
            else
            {
                sb.Append(indent);
                sb.Append("    <TextBlock Text=\"");
                sb.Append(EscapeXmlAttribute(ReadInternalText(element, "__paneText", "Navigation pane")));
                sb.AppendLine("\" />");
            }

            sb.Append(indent);
            sb.AppendLine("  </SplitView.Pane>");
            if (contentChild is not null)
            {
                AppendElement(sb, contentChild, indent + "  ", childrenByParent, element);
            }
            else
            {
                sb.Append(indent);
                sb.Append("  <TextBlock Text=\"");
                sb.Append(EscapeXmlAttribute(ReadInternalText(element, "__contentText", "Main content")));
                sb.AppendLine("\" />");
            }

            sb.Append(indent);
            sb.AppendLine("</SplitView>");
            return;
        }

        if (!childrenByParent.TryGetValue(element.DisplayName, out var children) || children.Count == 0)
        {
            sb.AppendLine(" />");
            return;
        }

        sb.AppendLine(">");
        var orderedChildren = string.Equals(
                element.TypeName,
                "Avalonia.Controls.StackPanel",
                StringComparison.Ordinal)
            ? children.OrderBy(child => child.StackPanelIndex).ToList()
            : string.Equals(element.TypeName, "Avalonia.Controls.DockPanel", StringComparison.Ordinal)
                ? children.OrderBy(child => child.DockPanelIndex).ToList()
                : string.Equals(element.TypeName, "Avalonia.Controls.WrapPanel", StringComparison.Ordinal)
                    ? children.OrderBy(child => child.WrapPanelIndex).ToList()
                    : string.Equals(
                        element.TypeName,
                        "Avalonia.Controls.Primitives.UniformGrid",
                        StringComparison.Ordinal)
                        ? children.OrderBy(child => child.UniformGridIndex).ToList()
                        : string.Equals(element.TypeName, "Avalonia.Controls.Canvas", StringComparison.Ordinal)
                            ? children.OrderBy(child => child.CanvasChildIndex).ToList()
            : children;
        foreach (var child in orderedChildren)
        {
            AppendElement(sb, child, indent + "  ", childrenByParent, element);
        }

        sb.Append(indent);
        sb.Append("</");
        sb.Append(MapToTagName(element.TypeName));
        sb.AppendLine(">");
    }

    private static void AppendGridCellAttributes(StringBuilder sb, DesignerElementSnapshot element)
    {
        if (element.GridRow > 0)
        {
            sb.Append($" Grid.Row=\"{element.GridRow}\"");
        }

        if (element.GridColumn > 0)
        {
            sb.Append($" Grid.Column=\"{element.GridColumn}\"");
        }

        if (element.GridRowSpan > 1)
        {
            sb.Append($" Grid.RowSpan=\"{element.GridRowSpan}\"");
        }

        if (element.GridColumnSpan > 1)
        {
            sb.Append($" Grid.ColumnSpan=\"{element.GridColumnSpan}\"");
        }
    }

    private static void AppendStackPanelItemSize(
        StringBuilder sb,
        DesignerElementSnapshot element,
        DesignerElementSnapshot parent)
    {
        var orientation = parent.VisualProperties is not null
            && parent.VisualProperties.TryGetValue("Orientation", out var value)
            ? value
            : "Vertical";
        var size = element.StackPanelItemSize > 0
            ? element.StackPanelItemSize
            : string.Equals(orientation, "Horizontal", StringComparison.OrdinalIgnoreCase)
                ? element.Width
                : element.Height;
        sb.Append(string.Equals(orientation, "Horizontal", StringComparison.OrdinalIgnoreCase)
            ? " Width=\""
            : " Height=\"");
        sb.Append(ToInvariantString(size));
        sb.Append('"');
    }

    private static bool IsContainer(DesignerElementSnapshot element)
        => string.Equals(element.TypeName, "Avalonia.Controls.Grid", StringComparison.Ordinal)
            || string.Equals(element.TypeName, "Avalonia.Controls.StackPanel", StringComparison.Ordinal)
            || string.Equals(element.TypeName, "Avalonia.Controls.DockPanel", StringComparison.Ordinal)
            || string.Equals(element.TypeName, "Avalonia.Controls.WrapPanel", StringComparison.Ordinal)
            || string.Equals(
                element.TypeName,
                "Avalonia.Controls.Primitives.UniformGrid",
                StringComparison.Ordinal)
            || string.Equals(element.TypeName, "Avalonia.Controls.Canvas", StringComparison.Ordinal)
            || string.Equals(element.TypeName, "Avalonia.Controls.TabControl", StringComparison.Ordinal)
            || string.Equals(element.TypeName, "Avalonia.Controls.SplitView", StringComparison.Ordinal)
            || string.Equals(element.TypeName, "Avalonia.Controls.Border", StringComparison.Ordinal)
            || string.Equals(element.TypeName, "Avalonia.Controls.ScrollViewer", StringComparison.Ordinal)
            || string.Equals(element.TypeName, "Avalonia.Controls.Expander", StringComparison.Ordinal);

    private static string ReadInternalText(
        DesignerElementSnapshot element,
        string propertyName,
        string fallback)
        => element.VisualProperties is not null
            && element.VisualProperties.TryGetValue(propertyName, out var value)
                ? value
                : fallback;

    private static IReadOnlyList<string> ReadTabHeaders(DesignerElementSnapshot element)
    {
        if (element.VisualProperties is null
            || !element.VisualProperties.TryGetValue("__tabs", out var json)
            || string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json)
                ?.Where(header => !string.IsNullOrWhiteSpace(header))
                .Select(header => header.Trim())
                .ToList()
                ?? new List<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static IReadOnlyList<DesignerTreeItemDefinition> ReadTreeItems(DesignerElementSnapshot element)
    {
        if (element.VisualProperties is null
            || !element.VisualProperties.TryGetValue("__treeItems", out var json)
            || !DesignerTreeItemRuntime.TryDeserialize(json, out var definitions))
        {
            return [];
        }

        return definitions;
    }

    private static IReadOnlyList<DesignerMenuEntryDefinition> ReadMenuEntries(
        DesignerElementSnapshot element)
    {
        if (element.VisualProperties is null
            || !element.VisualProperties.TryGetValue("__menuItems", out var json)
            || !DesignerMenuItemRuntime.TryDeserialize(json, out var definitions))
        {
            return [];
        }

        return definitions;
    }

    private static void AppendMenuEntries(
        StringBuilder sb,
        IEnumerable<DesignerMenuEntryDefinition> definitions,
        string indent)
    {
        foreach (var definition in definitions)
        {
            sb.Append(indent);
            if (definition.Kind == DesignerMenuEntryKind.Separator)
            {
                sb.AppendLine("<Separator />");
                continue;
            }

            sb.Append("<MenuItem Header=\"");
            sb.Append(EscapeXmlAttribute(definition.Header));
            sb.Append('"');
            AppendMenuEntryAttribute(sb, "InputGesture", definition.InputGesture);
            AppendMenuEntryAttribute(sb, "HotKey", definition.InputGesture);
            if (definition.ToggleType != Avalonia.Controls.MenuItemToggleType.None)
            {
                AppendMenuEntryAttribute(sb, "ToggleType", definition.ToggleType.ToString());
                AppendMenuEntryAttribute(sb, "IsChecked", definition.IsChecked.ToString());
            }

            AppendMenuEntryAttribute(sb, "GroupName", definition.GroupName);
            if (definition.Children.Count == 0)
            {
                sb.AppendLine(" />");
                continue;
            }

            sb.AppendLine(">");
            AppendMenuEntries(sb, definition.Children, indent + "  ");
            sb.Append(indent);
            sb.AppendLine("</MenuItem>");
        }
    }

    private static void AppendMenuEntryAttribute(StringBuilder sb, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        sb.Append(' ');
        sb.Append(name);
        sb.Append("=\"");
        sb.Append(EscapeXmlAttribute(value));
        sb.Append('"');
    }

    private static void AppendTreeItems(
        StringBuilder sb,
        IEnumerable<DesignerTreeItemDefinition> definitions,
        string indent)
    {
        foreach (var definition in definitions)
        {
            sb.Append(indent);
            sb.Append("<TreeViewItem Header=\"");
            sb.Append(EscapeXmlAttribute(definition.Header));
            sb.Append("\" IsExpanded=\"");
            sb.Append(definition.IsExpanded);
            if (definition.Children.Count == 0)
            {
                sb.AppendLine("\" />");
                continue;
            }

            sb.AppendLine("\">");
            AppendTreeItems(sb, definition.Children, indent + "  ");
            sb.Append(indent);
            sb.AppendLine("</TreeViewItem>");
        }
    }

    private static void AppendDockPanelAttributes(
        StringBuilder sb,
        DesignerElementSnapshot element,
        DesignerElementSnapshot parent,
        IReadOnlyDictionary<string, List<DesignerElementSnapshot>> childrenByParent)
    {
        sb.Append(" DockPanel.Dock=\"");
        sb.Append(element.DockPanelDock);
        sb.Append('"');
        var isLastFill = parent.VisualProperties is not null
            && parent.VisualProperties.TryGetValue("LastChildFill", out var value)
            && bool.TryParse(value, out var lastChildFill)
            && lastChildFill
            && childrenByParent.TryGetValue(parent.DisplayName, out var siblings)
            && ReferenceEquals(
                siblings.OrderBy(child => child.DockPanelIndex).LastOrDefault(),
                element);
        if (isLastFill)
        {
            return;
        }

        sb.Append(element.DockPanelDock is DesignerDockSide.Top or DesignerDockSide.Bottom
            ? " Height=\""
            : " Width=\"");
        sb.Append(ToInvariantString(element.DockPanelItemSize));
        sb.Append('"');
    }

    private static void AppendVisualAttributes(StringBuilder sb, IReadOnlyDictionary<string, string>? properties)
    {
        if (properties is null)
        {
            return;
        }

        foreach (var pair in properties)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Key.StartsWith("__", StringComparison.Ordinal))
            {
                continue;
            }

            sb.Append(" ");
            sb.Append(pair.Key);
            sb.Append("=\"");
            sb.Append(EscapeXmlAttribute(pair.Value));
            sb.Append("\"");
        }
    }

    private static string EscapeXmlAttribute(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
