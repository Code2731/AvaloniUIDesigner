using System.Collections.Generic;

namespace AvaloniaUIDesigner.App.Designer.Core;

public sealed record DesignerCanvasDocument(IReadOnlyList<DesignerElementSnapshot> Elements);

public sealed record DesignerElementSnapshot(
    string DisplayName,
    string TypeName,
    double X,
    double Y,
    double Width,
    double Height,
    IReadOnlyDictionary<string, string>? VisualProperties = null);
