using AvaloniaUIDesigner.App.Designer.Core;

namespace AvaloniaUIDesigner.App.Designer.Contracts;

public interface IDesignerSerializer
{
    string Serialize(DesignerCanvasDocument document);
}
