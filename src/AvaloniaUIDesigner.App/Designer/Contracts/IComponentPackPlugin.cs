using AvaloniaUIDesigner.App.Models;

namespace AvaloniaUIDesigner.App.Designer.Contracts;

public interface IComponentPackPlugin
{
    string Name { get; }
    ComponentPackDocument CreatePack();
}
