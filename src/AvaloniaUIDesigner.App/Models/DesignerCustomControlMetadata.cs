using System.Collections.Generic;

namespace AvaloniaUIDesigner.App.Models;

public sealed record DesignerCustomControlMetadata(
    string TypeName,
    string PreviewText,
    IReadOnlyDictionary<string, string> DefaultProperties,
    IReadOnlyList<string> DeclaredProperties,
    IReadOnlyDictionary<string, string> StyleProperties);
