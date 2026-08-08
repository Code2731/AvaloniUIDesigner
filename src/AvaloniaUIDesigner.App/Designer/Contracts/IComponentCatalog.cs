using System.Collections.Generic;
using AvaloniaUIDesigner.App.Designer.Core;

namespace AvaloniaUIDesigner.App.Designer.Contracts;

public interface IComponentCatalog
{
    IReadOnlyList<DesignerComponentDefinition> GetAll();
    bool TryGet(string avaloniaTypeName, out DesignerComponentDefinition definition);
    bool TryRegister(DesignerComponentDefinition definition, out string error);
    bool TryUnregister(
        string sourceId,
        out IReadOnlyList<DesignerComponentDefinition> definitions,
        out string error);
}
