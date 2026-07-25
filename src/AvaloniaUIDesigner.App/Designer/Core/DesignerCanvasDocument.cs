using System.Collections.Generic;

namespace AvaloniaUIDesigner.App.Designer.Core;

public sealed record DesignerCanvasDocument(
    IReadOnlyList<DesignerElementSnapshot> Elements,
    DesignerCanvasSettings? Settings = null);

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
    bool IsLocked = false);
