using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Designer.Services;
using AvaloniaUIDesigner.App.ViewModels;

internal static class EditingHistoryChecks
{
    public static void Run()
    {
        var editor = new MainWindowViewModel();
        var source = new AxamlDocumentSerializer().Serialize(new DesignerCanvasDocument(
            [new("Action", "Avalonia.Controls.Button", 40, 50, 120, 32)]));
        Assert(editor.TryImportDraftAxaml(source, out var error, out _), error);
        Assert(!editor.CanUndo && !editor.CanRedo, "Import must start with empty history.");
        editor.SelectElement(editor.Canvas.Elements.Single());
        editor.MoveSelectedElement(16, 24);
        Assert(Position(editor) == (56, 74) && editor.CanUndo && !editor.CanRedo,
            "Move must update geometry and create an undo entry.");
        editor.Undo();
        Assert(Position(editor) == (40, 50) && !editor.CanUndo && editor.CanRedo,
            "Undo must restore geometry and expose redo.");

        var restored = new MainWindowViewModel();
        Assert(restored.TryRestoreSessionJson(editor.ExportSessionJson(), out error), error);
        Assert(Position(restored) == (40, 50) && restored.CanRedo,
            "Session restore must retain the undone state and redo history.");
        restored.Redo();
        Assert(Position(restored) == (56, 74) && restored.CanUndo && !restored.CanRedo,
            "Restored redo must apply the original movement.");
        restored.Undo();
        restored.SelectElement(restored.Canvas.Elements.Single());
        restored.MoveSelectedElement(0, 0);
        Assert(!restored.CanUndo && restored.CanRedo, "No-op movement must not invalidate redo.");
        restored.MoveSelectedElement(8, 0);
        Assert(Position(restored) == (48, 50) && !restored.CanRedo,
            "A new edit after Undo must discard the old redo branch.");
        restored.Undo();
        Assert(Position(restored) == (40, 50), "Undo on the new branch must return to the original state.");
        var groupSource = new AxamlDocumentSerializer().Serialize(new DesignerCanvasDocument(
            [new("First", "Avalonia.Controls.Button", 10, 20, 80, 30),
             new("Second", "Avalonia.Controls.Button", 100, 80, 80, 30),
             new("Locked", "Avalonia.Controls.Button", 0, 0, 80, 30, IsLocked: true)]));
        Assert(editor.TryImportDraftAxaml(groupSource, out error, out _), error);
        editor.Canvas.Elements.Single(element => element.DisplayName == "Locked").IsLocked = true;
        editor.SelectElements(editor.Canvas.Elements.ToList());
        editor.MoveSelectedElement(-50, -50);
        var moved = editor.CreatePreviewDocument().Elements;
        Assert(moved.Single(element => element.DisplayName == "First") is { X: 0, Y: 0 }
            && moved.Single(element => element.DisplayName == "Second") is { X: 90, Y: 60 }
            && moved.Single(element => element.DisplayName == "Locked") is { X: 0, Y: 0 },
            "Boundary movement must preserve group spacing and ignore locked controls: "
            + string.Join("; ", moved.Select(element => $"{element.DisplayName}: {element.X},{element.Y}, locked={element.IsLocked}")));
        editor.Undo();
        Assert(editor.CreatePreviewDocument().Elements.Single(element => element.DisplayName == "First") is { X: 10, Y: 20 },
            "Group movement must undo in a single step.");
        editor.Redo();
        Assert(editor.CreatePreviewDocument().Elements.Single(element => element.DisplayName == "Second") is { X: 90, Y: 60 },
            "Group movement must redo without compressing the layout.");
        Console.WriteLine("Editing history checks passed.");
    }

    private static (double, double) Position(MainWindowViewModel editor)
    {
        var element = editor.Canvas.Elements.Single();
        return (element.X, element.Y);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
