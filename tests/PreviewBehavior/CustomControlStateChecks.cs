using Avalonia;
using Avalonia.Input;
using AvaloniaUIDesigner.App.Designer.Services;
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

        var sourceWithoutAccessibilityEdits = editor.ExportDraftAxaml();
        Assert(!sourceWithoutAccessibilityEdits.Contains("TabIndex=")
            && !sourceWithoutAccessibilityEdits.Contains("IsTabStop=")
            && !sourceWithoutAccessibilityEdits.Contains("Focusable="),
            "Unedited placeholder keyboard defaults must not leak into custom-control AXAML.");
        Assert(editor.SetSelectedAccessibilityProperties(
            "Current gauge value",
            "System status",
            "status-gauge",
            "Shows the current system status",
            "Content",
            "2",
            "Polite",
            true,
            "7",
            true,
            true), editor.StatusText);
        editor.Undo();
        var undoneSource = editor.ExportDraftAxaml();
        Assert(!undoneSource.Contains("ToolTip.Tip=") && !undoneSource.Contains("TabIndex=\"7\"")
            && undoneSource.Contains("IsVisible=\"False\""),
            "Undo must remove the accessibility edit without changing custom-control interaction state.");
        editor.Redo();

        var source = editor.ExportDraftAxaml();
        Assert(source.Contains("IsVisible=\"False\"")
            && source.Contains("IsEnabled=\"False\"")
            && source.Contains("IsHitTestVisible=\"False\"")
            && source.Contains("FlowDirection=\"RightToLeft\"")
            && source.Contains("Cursor=\"Help\""),
            "Draft AXAML must persist custom-control interaction state.");
        Assert(source.Contains("ToolTip.Tip=\"Current gauge value\"")
            && source.Contains("AutomationProperties.Name=\"System status\"")
            && source.Contains("AutomationProperties.AutomationId=\"status-gauge\"")
            && source.Contains("TabIndex=\"7\"")
            && source.Contains("IsTabStop=\"True\"")
            && source.Contains("Focusable=\"True\""),
            "Draft AXAML must persist accessibility values explicitly edited on a custom control.");
        Assert(!source.Contains("Background=\"#FFE0F2FE\"")
            && !source.Contains("Padding=\"10"),
            "Draft AXAML must not leak design-placeholder chrome.");

        Assert(editor.TryImportDraftAxaml(source, out var importError, out _), importError);
        var imported = editor.Canvas.Elements.Single().Visual;
        Assert(!imported.IsVisible && !imported.IsEnabled && !imported.IsHitTestVisible
            && imported.FlowDirection == Avalonia.Media.FlowDirection.RightToLeft
            && imported.Cursor?.ToString() == StandardCursorType.Help.ToString(),
            "Custom-control interaction state must survive draft import.");
        var accessibility = DesignerAccessibilityRuntime.Read(imported);
        Assert(accessibility.ToolTip == "Current gauge value"
            && accessibility.AccessibleName == "System status"
            && accessibility.AutomationId == "status-gauge"
            && accessibility.HelpText == "Shows the current system status"
            && accessibility.AccessibilityView.ToString() == "Content"
            && accessibility.HeadingLevel == 2
            && accessibility.LiveSetting.ToString() == "Polite"
            && accessibility.IsRequiredForForm
            && accessibility.TabIndex == 7
            && accessibility.IsTabStop
            && accessibility.Focusable,
            "Custom-control accessibility values must survive draft import.");
        editor.SelectElement(editor.Canvas.Elements.Single());
        Assert(editor.SetSelectedTransformProperties(
            "12", "-4", "30", "1.25", "0.75", "3", "-2", "40", "60"), editor.StatusText);
        Assert(editor.SetSelectedEffectProperties(
            "Drop Shadow", "5", "3", "4", "8", "#112233", "0.65"), editor.StatusText);
        editor.Undo();
        var afterEffectUndo = editor.Canvas.Elements.Single().Visual;
        var readUndoneEffect = DesignerEffectRuntime.TryRead(afterEffectUndo, out var undoneEffect, out var undoneEffectError);
        var readRetainedTransform = DesignerTransformRuntime.TryRead(
            afterEffectUndo, out var retainedTransform, out var retainedTransformError);
        var expectedTransform = new DesignerTransformValues(12, -4, 30, 1.25, 0.75, 3, -2, 40, 60);
        Assert(readUndoneEffect && undoneEffect.Kind == DesignerEffectKind.None
            && readRetainedTransform && DesignerTransformRuntime.AreEquivalent(retainedTransform, expectedTransform),
            $"Undoing the effect must retain the preceding custom-control transform. "
            + $"Effect={undoneEffect?.Kind}, EffectError={undoneEffectError}, "
            + $"Rotation={retainedTransform?.Rotation}, TransformError={retainedTransformError}");
        editor.Undo();
        var afterTransformUndo = editor.Canvas.Elements.Single().Visual;
        Assert(DesignerTransformRuntime.TryRead(afterTransformUndo, out var undoneTransform, out _)
            && DesignerTransformRuntime.AreEquivalent(undoneTransform, DesignerTransformValues.Default),
            "The second Undo must restore the default custom-control transform.");
        editor.Redo();
        editor.Redo();
        source = editor.ExportDraftAxaml();
        Assert(source.Contains("RenderTransform=") && source.Contains("RenderTransformOrigin=")
            && source.Contains("Effect="),
            "Draft AXAML must include edited custom-control transform and effect values.");
        Assert(editor.TryImportDraftAxaml(source, out importError, out _), importError);
        imported = editor.Canvas.Elements.Single().Visual;
        Assert(DesignerTransformRuntime.TryRead(imported, out var transform, out var transformError), transformError);
        Assert(DesignerTransformRuntime.AreEquivalent(transform, expectedTransform),
            "Custom-control transform values must survive draft import.");
        Assert(DesignerEffectRuntime.TryRead(imported, out var effect, out var effectError), effectError);
        Assert(effect.Kind == DesignerEffectKind.DropShadow
            && effect.OffsetX == 3 && effect.OffsetY == 4 && effect.ShadowBlurRadius == 8
            && Avalonia.Media.Color.Parse(effect.ShadowColor) == Avalonia.Media.Color.Parse("#112233")
            && Math.Abs(effect.ShadowOpacity - 0.65) <= 1d / byte.MaxValue,
            $"Custom-control effect values must survive draft import: {effect}");
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
