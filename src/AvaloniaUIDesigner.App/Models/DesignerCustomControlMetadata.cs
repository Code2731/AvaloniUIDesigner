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
    Thickness,
    CornerRadius,
}

public sealed record DesignerCustomPropertyDefinition(
    string Name,
    DesignerCustomPropertyType Type = DesignerCustomPropertyType.String,
    IReadOnlyList<string>? Options = null,
    double? Minimum = null,
    double? Maximum = null)
{
    public string? DisplayName { get; init; }

    public string? Category { get; init; }

    public string? Description { get; init; }
}

public sealed record DesignerCustomControlMetadata(
    string TypeName,
    string PreviewText,
    IReadOnlyDictionary<string, string> DefaultProperties,
    IReadOnlyList<string> DeclaredProperties,
    IReadOnlyDictionary<string, string> StyleProperties,
    IReadOnlyList<DesignerCustomPropertyDefinition>? PropertyDefinitions = null);
