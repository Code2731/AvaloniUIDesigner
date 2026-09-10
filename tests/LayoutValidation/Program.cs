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
var cycleA = grid with { DisplayName = "A", ParentName = "C", ParentLayout = DesignerParentLayoutKind.Grid };
var cycleB = cycleA with { DisplayName = "B", ParentName = "A" };
var cycleC = cycleA with { DisplayName = "C", ParentName = "B" };
var descendant = cycleA with { DisplayName = "Descendant", ParentName = "C" };
foreach (var elements in new[]
{
    new[] { cycleA, cycleB, cycleC, descendant },
    new[] { descendant, cycleC, cycleB, cycleA },
})
{
    var cycles = DesignerLayoutValidator.Validate(new(elements)).Where(d => d.Code == "PARENT_CYCLE").ToList();
    if (cycles.Count != 1 || !cycles[0].Message.Contains("A -> C -> B -> A", StringComparison.Ordinal))
        throw new Exception("Cycle diagnostics must preserve parent edges and report each cycle once.");
}
var self = cycleA with { ParentName = "A" };
if (!DesignerLayoutValidator.Validate(new([self])).Single(d => d.Code == "PARENT_CYCLE").Message.Contains("A -> A"))
    throw new Exception("Self-parent cycles must show the closing edge.");
var chain = Enumerable.Range(0, 10000).Select(index => grid with
{
    DisplayName = $"Node{index}",
    ParentName = index == 9999 ? null : $"Node{index + 1}",
    ParentLayout = index == 9999 ? DesignerParentLayoutKind.None : DesignerParentLayoutKind.Grid,
}).ToArray();
if (DesignerLayoutValidator.Validate(new(chain)).Count != 0)
    throw new Exception("Deep acyclic hierarchies must pass without recursive traversal.");
Console.WriteLine("Layout validation regression checks passed.");
