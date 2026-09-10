using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Designer.Services;
using AvaloniaUIDesigner.App.ViewModels;

internal static class AxamlRoundTripChecks
{
    public static void Run()
    {
        const string text = "A & B <tag> \"quoted\"\r\nnext\t\uD55C\uAE00";
        var document = new DesignerCanvasDocument(
            [
                new("Panel", "Avalonia.Controls.Canvas", 12, 24, 400, 300),
                new("Input", "Avalonia.Controls.TextBox", 16, 32, 180, 60,
                    new Dictionary<string, string> { ["Text"] = text },
                    ParentName: "Panel", ParentLayout: DesignerParentLayoutKind.Canvas,
                    CanvasChildIndex: 0, CanvasChildLeft: 16, CanvasChildTop: 32),
            ],
            Settings: new DesignerCanvasSettings(640, 480, "#123456"),
            ColorResources: new Dictionary<string, string> { ["Accent"] = "#ABCDEF" },
            SampleDataJson: "{\"Name\":\"sample\"}",
            RootSettings: new DesignerRootSettings(Title: "Round trip", CanResize: false, MinWidth: 200));
        var serializer = new AxamlDocumentSerializer();
        var editor = new MainWindowViewModel();
        var source = serializer.Serialize(document);
        for (var pass = 0; pass < 2; pass++)
        {
            Assert(editor.TryCreatePreviewDocumentFromAxaml(source, out var loaded, out var result), result);
            Assert(loaded.Elements.Count == 2, "Round trip must preserve the control count.");
            var child = loaded.Elements.Single(element => element.DisplayName == "Input");
            Assert(child.ParentName == "Panel" && child.ParentLayout == DesignerParentLayoutKind.Canvas,
                "Round trip must retain nested Canvas ownership.");
            Assert((child.X, child.Y, child.Width, child.Height) == (16, 32, 180, 60),
                "Round trip must retain nested geometry.");
            Assert((child.CanvasChildIndex, child.CanvasChildLeft, child.CanvasChildTop) == (0, 16, 32),
                "Round trip must retain Canvas child placement fields.");
            Assert(child.VisualProperties!["Text"] == text, "Round trip must preserve exact text and whitespace.");
            Assert(loaded.Settings!.Width == 640 && loaded.Settings.Height == 480
                && loaded.Settings.Background == "#123456", "Round trip must retain artboard settings.");
            Assert(Avalonia.Media.Color.Parse(loaded.ColorResources!["Accent"]) == Avalonia.Media.Color.Parse("#ABCDEF"),
                "Round trip must retain resource colors, allowing canonical ARGB formatting.");
            Assert(System.Text.Json.Nodes.JsonNode.DeepEquals(
                System.Text.Json.Nodes.JsonNode.Parse(loaded.SampleDataJson!),
                System.Text.Json.Nodes.JsonNode.Parse(document.SampleDataJson!)),
                "Round trip must retain sample data values.");
            Assert(loaded.RootSettings == document.RootSettings, "Round trip must retain root settings.");
            source = serializer.Serialize(loaded);
        }
        var lockDocument = document with
        {
            Elements =
            [
                new("LockedRoot", "Avalonia.Controls.Canvas", 0, 0, 300, 200, IsLocked: true),
                new("LockedChild", "Avalonia.Controls.Button", 8, 16, 100, 30,
                    IsLocked: true, ParentName: "LockedRoot", ParentLayout: DesignerParentLayoutKind.Canvas,
                    CanvasChildIndex: 0, CanvasChildLeft: 8, CanvasChildTop: 16),
                new("UnlockedRoot", "Avalonia.Controls.Button", 320, 0, 100, 30),
            ],
        };
        var lockSource = serializer.Serialize(lockDocument);
        for (var pass = 0; pass < 2; pass++)
        {
            Assert(editor.TryCreatePreviewDocumentFromAxaml(lockSource, out var loaded, out var lockResult), lockResult);
            Assert(loaded.Elements.Single(element => element.DisplayName == "LockedRoot").IsLocked,
                "Draft round trip must retain a locked root control.");
            Assert(loaded.Elements.Single(element => element.DisplayName == "LockedChild").IsLocked,
                "Draft round trip must retain a locked nested control.");
            Assert(!loaded.Elements.Single(element => element.DisplayName == "UnlockedRoot").IsLocked,
                "Lock metadata must not leak into the following control.");
            lockSource = serializer.Serialize(loaded);
        }
        foreach (var invalidText in new[] { "before\0after", "\u0001", "\uD800", "\uFFFF" })
        {
            var rejected = false;
            try
            {
                serializer.Serialize(document with
                {
                    Elements = [new("Invalid", "Avalonia.Controls.TextBox", 0, 0, 100, 30,
                        new Dictionary<string, string> { ["Text"] = invalidText })],
                });
            }
            catch (System.Xml.XmlException) { rejected = true; }
            Assert(rejected, "XML-invalid text must fail serialization rather than produce an unreadable document.");
        }
        Assert(editor.TryCreatePreviewDocumentFromAxaml(serializer.Serialize(document), out _, out _),
            "A rejected value must not prevent a later valid serialization.");
        Assert(editor.TryImportDraftAxaml(serializer.Serialize(document), out var importError, out _), importError);
        var inputControl = (Avalonia.Controls.TextBox)editor.Canvas.Elements.Single(element => element.DisplayName == "Input").Visual;
        inputControl.Text = "invalid\0text";
        var sessionRejected = false;
        try { editor.ExportSessionJson(); }
        catch (System.Xml.XmlException) { sessionRejected = true; }
        Assert(sessionRejected, "Session export must reject unreadable recovery documents.");
        Assert(!editor.TryExportAxamlForSave(out var invalidOutput, out var saveError)
            && invalidOutput == string.Empty && !string.IsNullOrEmpty(saveError),
            "Save preflight must report invalid XML without returning output to write.");
        Assert(inputControl.Text == "invalid\0text", "Failed save preflight must not modify the user's input.");
        inputControl.Text = text;
        var recoveredSession = editor.ExportSessionJson();
        var restoredEditor = new MainWindowViewModel();
        Assert(restoredEditor.TryRestoreSessionJson(recoveredSession, out var sessionError), sessionError);
        var restoredInput = (Avalonia.Controls.TextBox)restoredEditor.Canvas.Elements.Single(element => element.DisplayName == "Input").Visual;
        Assert(restoredInput.Text == text, "Session recovery must preserve corrected input after an export failure.");
        Assert(editor.TryExportAxamlForSave(out var validOutput, out saveError)
            && !string.IsNullOrEmpty(validOutput) && saveError == string.Empty,
            "Corrected input must pass save preflight.");
        Console.WriteLine("AXAML round-trip checks passed.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
