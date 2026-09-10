using Avalonia;
using Avalonia.Input;
using AvaloniaUIDesigner.App.ViewModels;

internal static class CustomControlStateChecks
{
    private const string ComponentPack = """
        {
          "name": "State checks",
          "components": [
            {
              "displayName": "Status Gauge",
              "avaloniaTypeName": "Acme.Controls.StatusGauge",
              "designOnly": true,
              "previewText": "Status Gauge",
              "defaultWidth": 220,
              "defaultHeight": 96,
              "defaultProperties": {
                "Caption": "Ready",
                "Opacity": "0.85"
              }
            }
          ]
        }
        """;

    public static void Run()
    {
        var editor = new MainWindowViewModel();
        Assert(editor.TryLoadComponentPack(ComponentPack, out var result), result);
        var item = editor.Toolbox.FindItemByDisplayName("Status Gauge")
            ?? throw new Exception("Loaded custom control was not added to Toolbox.");
        editor.PlaceToolboxItem(item, 32, 48);
        var custom = editor.Canvas.Elements.Single();
        custom.Visual.IsVisible = false;
        custom.Visual.IsEnabled = false;
        custom.Visual.IsHitTestVisible = false;
        custom.Visual.FlowDirection = Avalonia.Media.FlowDirection.RightToLeft;
        custom.Visual.Cursor = new Cursor(StandardCursorType.Help);

        var source = editor.ExportDraftAxaml();
        Assert(source.Contains("IsVisible=\"False\"")
            && source.Contains("IsEnabled=\"False\"")
            && source.Contains("IsHitTestVisible=\"False\"")
            && source.Contains("FlowDirection=\"RightToLeft\"")
            && source.Contains("Cursor=\"Help\""),
            "Draft AXAML must persist custom-control interaction state.");
        Assert(!source.Contains("Background=\"#FFE0F2FE\"")
            && !source.Contains("Padding=\"10"),
            "Draft AXAML must not leak design-placeholder chrome.");

        Assert(editor.TryImportDraftAxaml(source, out var importError, out _), importError);
        var imported = editor.Canvas.Elements.Single().Visual;
        Assert(!imported.IsVisible && !imported.IsEnabled && !imported.IsHitTestVisible
            && imported.FlowDirection == Avalonia.Media.FlowDirection.RightToLeft
            && imported.Cursor?.ToString() == StandardCursorType.Help.ToString(),
            "Custom-control interaction state must survive draft import.");
        var snapshot = editor.CreatePreviewDocument().Elements.Single();
        Assert(snapshot.VisualProperties!["Caption"] == "Ready"
            && snapshot.VisualProperties["Opacity"] == "0.85",
            "Capturing interaction state must retain declared custom properties.");
        Console.WriteLine("Custom control state checks passed.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
