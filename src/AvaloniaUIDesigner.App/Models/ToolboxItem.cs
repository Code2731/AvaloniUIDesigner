using System.Collections.Generic;
using AvaloniaUIDesigner.App.Designer.Core;

namespace AvaloniaUIDesigner.App.Models;

public sealed record ToolboxItem(
    string DisplayName,
    string AvaloniaTypeName,
    IReadOnlyList<DesignerElementSnapshot>? PresetElements = null,
    double? DefaultWidth = null,
    double? DefaultHeight = null,
    IReadOnlyDictionary<string, string>? DefaultProperties = null,
    string? NamePrefix = null,
    string? SourceId = null)
{
    public bool IsPreset => PresetElements is { Count: > 0 };
}
