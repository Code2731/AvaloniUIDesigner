using System.Collections.Generic;
using System;
using System.Globalization;
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

        foreach (var element in document.Elements)
        {
            sb.Append("  <");
            sb.Append(MapToTagName(element.TypeName));
            sb.Append(" Canvas.Left=\"");
            sb.Append(ToInvariantString(element.X));
            sb.Append("\" Canvas.Top=\"");
            sb.Append(ToInvariantString(element.Y));
            sb.Append("\" Width=\"");
            sb.Append(ToInvariantString(element.Width));
            sb.Append("\" Height=\"");
            sb.Append(ToInvariantString(element.Height));
            sb.Append("\"");
            AppendVisualAttributes(sb, element.VisualProperties);
            sb.AppendLine(" />");
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
