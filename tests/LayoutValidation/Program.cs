using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Designer.Services;

foreach (var (type, layout) in new[]
{
    ("Border", DesignerParentLayoutKind.Content),
    ("TabControl", DesignerParentLayoutKind.TabControl),
    ("SplitView", DesignerParentLayoutKind.SplitView),
})
{
    var parent = new DesignerElementSnapshot("Host", $"Avalonia.Controls.{type}", 0, 0, 200, 100);
    var first = new DesignerElementSnapshot("First", "Avalonia.Controls.Button", 0, 0, 40, 20,
        ParentName: "Host", ParentLayout: layout, TabIndex: 0);
    var second = first with { DisplayName = "Second", ParentName = "host" };
    var document = new DesignerCanvasDocument([parent, first, second]);
    var conflicts = DesignerLayoutValidator.Validate(document).Where(d => d.Code == "OCCUPIED_SLOT").ToList();
    if (conflicts.Count != 2 || !conflicts.Select(d => d.ElementName).ToHashSet().SetEquals(["First", "Second"]))
        throw new Exception($"{type}: both conflicting children must be reported.");

    var separate = layout switch
    {
        DesignerParentLayoutKind.TabControl => second with { TabIndex = 1 },
        DesignerParentLayoutKind.SplitView => second with { SplitViewSlot = DesignerSplitViewSlot.Pane },
        _ => second with { ParentName = null, ParentLayout = DesignerParentLayoutKind.None },
    };
    if (DesignerLayoutValidator.Validate(document with { Elements = [parent, first, separate] }).Count != 0)
        throw new Exception($"{type}: distinct slots should pass.");
}

var grid = new DesignerElementSnapshot("Grid", "Avalonia.Controls.Grid", 0, 0, 200, 100);
var a = new DesignerElementSnapshot("A", "Avalonia.Controls.Button", 0, 0, 40, 20,
    ParentName: "Grid", ParentLayout: DesignerParentLayoutKind.Grid);
if (DesignerLayoutValidator.Validate(new([grid, a, a with { DisplayName = "B" }])).Count != 0)
    throw new Exception("Grid permits multiple children in the same cell.");
Console.WriteLine("Layout validation regression checks passed.");
