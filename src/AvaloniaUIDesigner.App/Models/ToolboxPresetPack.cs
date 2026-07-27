using System.Collections.Generic;
using AvaloniaUIDesigner.App.Designer.Core;

namespace AvaloniaUIDesigner.App.Models;

public sealed class ToolboxPresetPackDocument
{
    public string? Name { get; set; }
    public List<ToolboxPresetDefinition> Presets { get; set; } = [];
}

public sealed class ToolboxPresetDefinition
{
    public string? DisplayName { get; set; }
    public string? AvaloniaTypeName { get; set; }
    public List<DesignerElementSnapshot> Elements { get; set; } = [];
}

public sealed record ToolboxPresetPackLoadResult(
    string Name,
    IReadOnlyList<ToolboxItem> Presets);
