using Avalonia.Controls;
using AvaloniaUIDesigner.App.Designer.Contracts;
using AvaloniaUIDesigner.App.Designer.Core;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed class DefaultControlRenderer : IControlRenderer
{
    public Control CreateControl(DesignerComponentDefinition definition) => definition.VisualFactory();
}
