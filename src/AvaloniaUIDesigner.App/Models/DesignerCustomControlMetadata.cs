using System.Collections.Generic;

namespace AvaloniaUIDesigner.App.Models;

public sealed record DesignerCustomControlMetadata(
    string PreviewText,
    IReadOnlyDictionary<string, string> DefaultProperties);
