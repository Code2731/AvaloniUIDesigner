using System.Collections.Generic;
using AvaloniaUIDesigner.App.Designer.Core;

namespace AvaloniaUIDesigner.App.Models;

public sealed record ToolboxItem(
    string DisplayName,
    string AvaloniaTypeName,
    IReadOnlyList<DesignerElementSnapshot>? PresetElements = null)
{
    public bool IsPreset => PresetElements is { Count: > 0 };
}
