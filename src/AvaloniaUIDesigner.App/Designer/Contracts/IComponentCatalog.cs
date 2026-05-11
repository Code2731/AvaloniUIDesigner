using System.Collections.Generic;
using AvaloniaUIDesigner.App.Designer.Core;

namespace AvaloniaUIDesigner.App.Designer.Contracts;

public interface IComponentCatalog
{
    IReadOnlyList<DesignerComponentDefinition> GetAll();
    bool TryGet(string avaloniaTypeName, out DesignerComponentDefinition definition);
}
