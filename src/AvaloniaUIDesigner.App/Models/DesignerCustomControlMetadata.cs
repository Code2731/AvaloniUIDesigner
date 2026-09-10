using System.Collections.Generic;

namespace AvaloniaUIDesigner.App.Models;

public enum DesignerCustomPropertyType
{
    String,
    Boolean,
    Integer,
    Double,
    Color,
    Enum,
}

public sealed record DesignerCustomPropertyDefinition(
    string Name,
    DesignerCustomPropertyType Type = DesignerCustomPropertyType.String,
    IReadOnlyList<string>? Options = null,
    double? Minimum = null,
    double? Maximum = null);

public sealed record DesignerCustomControlMetadata(
    string TypeName,
    string PreviewText,
    IReadOnlyDictionary<string, string> DefaultProperties,
    IReadOnlyList<string> DeclaredProperties,
    IReadOnlyDictionary<string, string> StyleProperties,
    IReadOnlyList<DesignerCustomPropertyDefinition>? PropertyDefinitions = null);
