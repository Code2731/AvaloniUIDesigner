using Avalonia.Controls;
using AvaloniaUIDesigner.App.Designer.Core;

namespace AvaloniaUIDesigner.App.Designer.Contracts;

public interface IControlRenderer
{
    Control CreateControl(DesignerComponentDefinition definition);
}
