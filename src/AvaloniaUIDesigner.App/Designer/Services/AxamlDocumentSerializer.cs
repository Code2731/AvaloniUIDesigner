using System.Collections.Generic;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
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

        AppendVisualAttributes(sb, element.VisualProperties);
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
            || string.Equals(element.TypeName, "Avalonia.Controls.Border", StringComparison.Ordinal)
            || string.Equals(element.TypeName, "Avalonia.Controls.ScrollViewer", StringComparison.Ordinal)
            || string.Equals(element.TypeName, "Avalonia.Controls.Expander", StringComparison.Ordinal);

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
