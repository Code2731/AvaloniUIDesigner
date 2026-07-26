using System.Collections.Generic;

namespace AvaloniaUIDesigner.App.Designer.Core;

public sealed record DesignerCanvasDocument(
    IReadOnlyList<DesignerElementSnapshot> Elements,
    DesignerCanvasSettings? Settings = null,
    IReadOnlyDictionary<string, string>? ColorResources = null,
    IReadOnlyList<DesignerStyleDefinition>? Styles = null);

public sealed record DesignerStyleDefinition(
    string TargetType,
    string ClassName,
    IReadOnlyDictionary<string, string> Setters,
    string? PseudoClass = null)
{
    public string Selector => $"{TargetType}.{ClassName}{(PseudoClass is null ? string.Empty : $":{PseudoClass}")}";
}

public sealed record DesignerCanvasSettings(
    double Width = 1280,
    double Height = 800,
    string Background = "#FFFFFF",
    double GridSize = 8,
    bool IsGridVisible = true,
    bool SnapToGrid = true);

public sealed record DesignerElementSnapshot(
    string DisplayName,
    string TypeName,
    double X,
    double Y,
    double Width,
    double Height,
    IReadOnlyDictionary<string, string>? VisualProperties = null,
    bool IsLocked = false,
    string? ParentName = null,
    int GridRow = 0,
    int GridColumn = 0,
    int GridRowSpan = 1,
    int GridColumnSpan = 1);
