using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaUIDesigner.App.Designer.Services;
using AvaloniaUIDesigner.App.Models;
using AvaloniaUIDesigner.App.ViewModels;
using AvaloniaUIDesigner.App.Views;

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
        editor.SelectElement(editor.Canvas.Elements.Single());
        var customName = editor.Canvas.Elements.Single().DisplayName;
        Assert(editor.SetEventHandlerMapFromText($"{customName} | GotFocus | OnGaugeFocus"), editor.StatusText);
        editor.Undo();
        Assert(!editor.ExportDraftAxaml().Contains("GotFocus=\"OnGaugeFocus\""),
            "Undo must remove a custom-control event handler.");
        editor.Redo();
        source = editor.ExportDraftAxaml();
        Assert(source.Contains("GotFocus=\"OnGaugeFocus\""),
            "Draft AXAML must include custom-control common event handlers.");
        Assert(editor.TryImportDraftAxaml(source, out importError, out _), importError);
        imported = editor.Canvas.Elements.Single().Visual;
        Assert(DesignerEventHandlerRuntime.Read(imported).TryGetValue("GotFocus", out var handlerName)
            && handlerName == "OnGaugeFocus",
            "Custom-control event handlers must survive draft import.");
        var previewInteractions = new List<DesignerPreviewInteraction>();
        Assert(DesignerPreviewInteractionRuntime.Wire(
            imported, customName, previewInteractions.Add) == 1,
            "Preview must wire the restored custom-control event handler.");
        imported.RaiseEvent(new GotFocusEventArgs { RoutedEvent = InputElement.GotFocusEvent });
        Assert(previewInteractions.SingleOrDefault() is
            { EventName: "GotFocus", HandlerName: "OnGaugeFocus" },
            "Preview must report the restored custom-control event handler.");
        var snapshot = editor.CreatePreviewDocument().Elements.Single();
        Assert(snapshot.VisualProperties!["Caption"] == "Ready"
            && snapshot.VisualProperties["Opacity"] == "0.85",
            "Capturing interaction state must retain declared custom properties.");
        editor.SelectElement(editor.Canvas.Elements.Single());
        Assert(editor.SetSelectedBindings(["Opacity | Preview.Opacity | OneWay | 0.35"]), editor.StatusText);
        Assert(DesignerBindingRuntime.ReadBindings(editor.Canvas.Elements.Single().Visual).SingleOrDefault() is
            {
                PropertyName: "Opacity",
                Path: "Preview.Opacity",
                Mode: DesignerBindingMode.OneWay,
                FallbackValue: "0.35",
            }, "The binding editor must apply a common-property binding to a custom control.");
        editor.Undo();
        Assert(DesignerBindingRuntime.ReadBindings(editor.Canvas.Elements.Single().Visual).Count == 0,
            "Undo must remove a custom-control binding.");
        editor.Redo();
        Assert(DesignerBindingRuntime.ReadBindings(editor.Canvas.Elements.Single().Visual).Count == 1,
            "Redo must restore a custom-control binding.");
        Assert(editor.TrySetSampleDataJson("""
            {
              "Preview": {
                "Opacity": 0.42
              }
            }
            """, out var sampleResult), sampleResult);
        Assert(Math.Abs(editor.Canvas.Elements.Single().Visual.Opacity - 0.42) < 0.001,
            "Sample data must apply the custom-control binding on the design surface.");
        source = editor.ExportDraftAxaml();
        Assert(source.Contains(
                "Opacity=\"{ReflectionBinding Preview.Opacity, Mode=OneWay, FallbackValue='0.35'}\""),
            "Draft AXAML must serialize a custom-control binding instead of its sampled value.");
        Assert(editor.TryImportDraftAxaml(source, out importError, out _), importError);
        imported = editor.Canvas.Elements.Single().Visual;
        Assert(DesignerBindingRuntime.ReadBindings(imported).SingleOrDefault() is
            {
                PropertyName: "Opacity",
                Path: "Preview.Opacity",
                Mode: DesignerBindingMode.OneWay,
                FallbackValue: "0.35",
            } && Math.Abs(imported.Opacity - 0.42) < 0.001,
            "A custom-control binding and its sample value must survive Draft re-import.");
        var previewDocument = editor.CreatePreviewDocument();
        Assert(previewDocument.Elements.Single().VisualProperties!.ContainsKey("__bindings"),
            "The Preview snapshot must retain custom-control binding metadata.");
        var preview = new PreviewWindow(previewDocument);
        try
        {
            var previewLayout = (Grid)preview.Content!;
            var previewSurface = previewLayout.Children.OfType<Border>().Single();
            var previewViewport = (ScrollViewer)previewSurface.Child!;
            var previewCanvas = (Canvas)previewViewport.Content!;
            var previewControl = previewCanvas.Children.Single();
            Assert(Math.Abs(previewControl.Opacity - 0.42) < 0.001,
                "Preview must apply sample data through the restored custom-control binding.");
            previewControl.Opacity = 0.1;
            preview.ResetPreview();
            previewCanvas = (Canvas)previewViewport.Content!;
            Assert(Math.Abs(previewCanvas.Children.Single().Opacity - 0.42) < 0.001,
                "Reset Preview must reapply the custom-control binding and sample data.");
        }
        finally
        {
            preview.Close();
        }

        editor.SelectElement(editor.Canvas.Elements.Single());
        Assert(editor.SetColorResourcesFromText("GaugeAccent = #345678"), editor.StatusText);
        Assert(editor.ApplyColorResource("GaugeAccent", "Background"), editor.StatusText);
        Assert(DesignerResourceReferenceMetadata.TryGetReference(
                editor.Canvas.Elements.Single().Visual, "Background", out var resourceKey)
            && resourceKey == "GaugeAccent",
            "Applying a color resource must attach it to a custom-control brush property.");
        editor.Undo();
        Assert(!DesignerResourceReferenceMetadata.TryGetReference(
                editor.Canvas.Elements.Single().Visual, "Background", out _),
            "Undo must remove a custom-control resource reference.");
        editor.Redo();
        Assert(DesignerResourceReferenceMetadata.TryGetReference(
                editor.Canvas.Elements.Single().Visual, "Background", out resourceKey)
            && resourceKey == "GaugeAccent",
            "Redo must restore a custom-control resource reference.");
        editor.SelectElement(editor.Canvas.Elements.Single());
        Assert(editor.SetSelectedStyleClassesFromText("status-primary focus-ring"), editor.StatusText);
        editor.Undo();
        Assert(CanvasViewModel.GetUserStyleClasses(editor.Canvas.Elements.Single().Visual).Count == 0,
            "Undo must remove custom-control style classes without losing its resource reference.");
        Assert(DesignerResourceReferenceMetadata.TryGetReference(
                editor.Canvas.Elements.Single().Visual, "Background", out resourceKey)
            && resourceKey == "GaugeAccent",
            "Undoing style classes must retain the preceding custom-control resource reference.");
        editor.Redo();
        Assert(CanvasViewModel.GetUserStyleClasses(editor.Canvas.Elements.Single().Visual)
                .SequenceEqual(["status-primary", "focus-ring"]),
            "Redo must restore custom-control style classes in editor order.");
        source = editor.ExportDraftAxaml();
        Assert(source.Contains("Classes=\"status-primary focus-ring\"")
            && source.Contains("Background=\"{DynamicResource GaugeAccent}\""),
            "Draft AXAML must include custom-control style classes and explicit resource references.");
        Assert(source.Contains("Caption=\"Ready\""),
            "Draft AXAML must include declared custom-control properties.");
        Assert(!source.Contains("Background=\"#FFE0F2FE\""),
            "Persisting an explicit resource must not reintroduce placeholder-only background values.");
        source = source.Replace("Caption=\"Ready\"", "Caption=\"Attention\"", StringComparison.Ordinal);
        Assert(editor.TryImportDraftAxaml(source, out importError, out _), importError);
        imported = editor.Canvas.Elements.Single().Visual;
        Assert(CanvasViewModel.GetUserStyleClasses(imported)
                .SequenceEqual(["status-primary", "focus-ring"]),
            "Custom-control style classes must survive Draft re-import.");
        Assert(DesignerResourceReferenceMetadata.TryGetReference(
                imported, "Background", out resourceKey)
            && resourceKey == "GaugeAccent",
            "A custom-control resource reference must survive Draft re-import.");
        previewDocument = editor.CreatePreviewDocument();
        Assert(previewDocument.Elements.Single().VisualProperties!["Caption"] == "Attention",
            "An edited custom-control property must survive Draft re-import and snapshot capture.");
        preview = new PreviewWindow(previewDocument);
        try
        {
            var previewLayout = (Grid)preview.Content!;
            var previewSurface = previewLayout.Children.OfType<Border>().Single();
            var previewViewport = (ScrollViewer)previewSurface.Child!;
            var previewCanvas = (Canvas)previewViewport.Content!;
            var previewControl = previewCanvas.Children.Single();
            var previewClasses = CanvasViewModel.GetUserStyleClasses(previewControl);
            Assert(previewClasses.SequenceEqual(["status-primary", "focus-ring"]),
                $"Preview must restore custom-control classes. Actual: {string.Join(", ", previewClasses)}");
            Assert(previewControl is Border { Background: { } previewBrush }
                && Avalonia.Media.Color.Parse(previewBrush.ToString()!) == Avalonia.Media.Color.Parse("#345678"),
                $"Preview must resolve the custom-control color resource. Actual: "
                + $"{(previewControl as Border)?.Background}");
            preview.ResetPreview();
            previewCanvas = (Canvas)previewViewport.Content!;
            previewControl = previewCanvas.Children.Single();
            var resetClasses = CanvasViewModel.GetUserStyleClasses(previewControl);
            Assert(resetClasses.SequenceEqual(["status-primary", "focus-ring"]),
                $"Reset Preview must restore custom-control classes. Actual: {string.Join(", ", resetClasses)}");
            Assert(previewControl is Border { Background: { } resetBrush }
                && Avalonia.Media.Color.Parse(resetBrush.ToString()!) == Avalonia.Media.Color.Parse("#345678"),
                $"Reset Preview must resolve the custom-control color resource. Actual: "
                + $"{(previewControl as Border)?.Background}");
        }
        finally
        {
            preview.Close();
        }

        editor.SelectElement(editor.Canvas.Elements.Single());
        Assert(editor.TryGetSelectedBindings(out var bindingState), editor.StatusText);
        Assert(bindingState.TargetType == "Acme.Controls.StatusGauge"
            && bindingState.SupportedProperties.Contains("Caption")
            && bindingState.SupportedProperties.Contains("Opacity"),
            "The binding editor must identify a design-only type and expose its declared properties.");
        var bindingsBeforeRejection = DesignerBindingRuntime.Serialize(
            DesignerBindingRuntime.ReadBindings(editor.Canvas.Elements.Single().Visual));
        Assert(!editor.SetSelectedBindings(["UnknownProperty | Preview.Unknown | OneWay"])
            && DesignerBindingRuntime.Serialize(
                DesignerBindingRuntime.ReadBindings(editor.Canvas.Elements.Single().Visual))
                == bindingsBeforeRejection,
            "The binding editor must reject undeclared custom properties without changing existing bindings.");
        Assert(editor.SetSelectedBindings(
        [
            "Caption | Preview.Caption | OneWay | Standby",
            "Opacity | Preview.Opacity | OneWay | 0.35",
        ]), editor.StatusText);
        var customBindings = DesignerBindingRuntime.ReadBindings(editor.Canvas.Elements.Single().Visual);
        Assert(customBindings.Count == 2
            && customBindings.Any(binding => binding is
            {
                PropertyName: "Caption",
                Path: "Preview.Caption",
                Mode: DesignerBindingMode.OneWay,
                FallbackValue: "Standby",
            }), "The binding editor must retain a declared custom-property binding.");
        editor.Undo();
        Assert(DesignerBindingRuntime.ReadBindings(editor.Canvas.Elements.Single().Visual) is
            [{ PropertyName: "Opacity" }],
            "Undo must remove only the newly added custom-property binding.");
        editor.Redo();
        customBindings = DesignerBindingRuntime.ReadBindings(editor.Canvas.Elements.Single().Visual);
        Assert(customBindings.Count == 2
            && customBindings.Any(binding => binding.PropertyName == "Caption"),
            "Redo must restore the custom-property binding.");
        source = editor.ExportDraftAxaml();
        Assert(source.Contains(
                "Caption=\"{ReflectionBinding Preview.Caption, Mode=OneWay, FallbackValue='Standby'}\"")
            && !source.Contains("Caption=\"Attention\""),
            "Draft AXAML must serialize the custom-property binding instead of its previous static value.");
        Assert(editor.TryImportDraftAxaml(source, out importError, out _), importError);
        imported = editor.Canvas.Elements.Single().Visual;
        customBindings = DesignerBindingRuntime.ReadBindings(imported);
        Assert(customBindings.Count == 2
            && customBindings.Any(binding => binding is
            {
                PropertyName: "Caption",
                Path: "Preview.Caption",
                FallbackValue: "Standby",
            }), "A custom-property binding must survive Draft re-import.");
        previewDocument = editor.CreatePreviewDocument();
        preview = new PreviewWindow(previewDocument);
        try
        {
            var previewLayout = (Grid)preview.Content!;
            var previewSurface = previewLayout.Children.OfType<Border>().Single();
            var previewViewport = (ScrollViewer)previewSurface.Child!;
            var previewCanvas = (Canvas)previewViewport.Content!;
            var previewControl = previewCanvas.Children.Single();
            Assert(DesignerBindingRuntime.ReadBindings(previewControl).Count == 2
                && Math.Abs(previewControl.Opacity - 0.42) < 0.001,
                "Preview must retain the custom binding while applying supported common bindings.");
            preview.ResetPreview();
            previewCanvas = (Canvas)previewViewport.Content!;
            previewControl = previewCanvas.Children.Single();
            Assert(DesignerBindingRuntime.ReadBindings(previewControl).Any(binding =>
                    binding.PropertyName == "Caption")
                && Math.Abs(previewControl.Opacity - 0.42) < 0.001,
                "Reset Preview must retain custom bindings and reapply supported sample values.");
        }
        finally
        {
            preview.Close();
        }

        Console.WriteLine("Custom control state checks passed.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
