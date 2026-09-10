using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using AvaloniaUIDesigner.App.Designer.Controls;
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
              "declaredProperties": ["Caption", "Opacity", "Unit"],
              "defaultProperties": {
                "Caption": "Ready",
                "Opacity": "0.85"
              }
            }
          ]
        }
        """;

    private const string TypedComponentPack = """
        {
          "name": "Typed state checks",
          "components": [
            {
              "displayName": "Typed Gauge",
              "avaloniaTypeName": "Acme.Controls.TypedGauge",
              "designOnly": true,
              "propertyDefinitions": [
                {
                  "name": "IsActive",
                  "type": "Boolean",
                  "displayName": "Active",
                  "category": "Behavior",
                  "description": "Toggles the active visual state."
                },
                {
                  "name": "Count",
                  "type": "Integer",
                  "displayName": " Item count ",
                  "category": " Data ",
                  "description": " Number of visible items. ",
                  "minimum": 0,
                  "maximum": 10
                },
                {
                  "name": "Ratio",
                  "type": "Double",
                  "displayName": "Fill ratio",
                  "category": "Data",
                  "minimum": 0,
                  "maximum": 1
                },
                {
                  "name": "Accent",
                  "type": "Color",
                  "displayName": "Accent color",
                  "category": "Appearance",
                  "description": "Primary gauge highlight color."
                },
                {
                  "name": "Corners",
                  "type": "CornerRadius",
                  "displayName": "Corner shape",
                  "category": "Appearance",
                  "description": "Four preview corner radii."
                },
                {
                  "name": "Insets",
                  "type": "Thickness",
                  "displayName": "Content inset",
                  "category": "Layout",
                  "description": "Spacing around the gauge content."
                },
                {
                  "name": "State",
                  "type": "Enum",
                  "displayName": "Operating mode",
                  "category": "Behavior",
                  "options": ["Idle", "Busy"]
                },
                { "name": "Note" }
              ],
              "defaultProperties": {
                "IsActive": "true",
                "Count": "02",
                "Ratio": ".5",
                "Accent": "#3B82F6",
                "Corners": "3,4,5,6",
                "Insets": "1, 2",
                "State": "idle",
                "Note": "Ready"
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

        RunCustomPropertyEditorChecks();
        RunCustomStyleChecks();
        RunCustomDeclaredStyleChecks();
        RunTypedCustomPropertyChecks();
        RunStructuredCustomPropertyEditorChecks();
        RunCustomPropertyInspectorSummaryChecks();
        Console.WriteLine("Custom control state checks passed.");
    }

    private static void RunCustomPropertyEditorChecks()
    {
        var invalidEditor = new MainWindowViewModel();
        Assert(!invalidEditor.TryLoadComponentPack("""
            {
              "components": [
                {
                  "displayName": "Invalid Gauge",
                  "avaloniaTypeName": "Acme.Controls.InvalidGauge",
                  "designOnly": true,
                  "declaredProperties": ["Bad.Name"]
                }
              ]
            }
            """, out var invalidResult)
            && invalidResult.Contains("invalid declared property", StringComparison.OrdinalIgnoreCase),
            "A component pack must reject declared property names that cannot name CLR properties.");

        var editor = new MainWindowViewModel();
        Assert(editor.TryLoadComponentPack(ComponentPack, out var result), result);
        var item = editor.Toolbox.FindItemByDisplayName("Status Gauge")
            ?? throw new Exception("Loaded custom control was not added to Toolbox.");
        editor.PlaceToolboxItem(item, 32, 48);
        Assert(editor.TryGetSelectedCustomProperties(out var state), editor.StatusText);
        Assert(state.TargetType == "Acme.Controls.StatusGauge"
            && state.EditableProperties.SequenceEqual(["Caption", "Unit"])
            && state.Lines.SequenceEqual(["Caption = Ready"]),
            "The custom-property editor must expose declarations without defaults and leave common properties to their dedicated editors.");

        var unchangedSource = editor.ExportDraftAxaml();
        Assert(!editor.SetSelectedCustomProperties(["Unknown = value"])
            && editor.ExportDraftAxaml() == unchangedSource,
            "An undeclared custom property must be rejected without changing the document.");
        Assert(editor.SetSelectedCustomProperties(["caption = Attention"]), editor.StatusText);
        var custom = editor.Canvas.Elements.Single();
        Assert(custom.Visual.Tag is DesignerCustomControlMetadata metadata
            && metadata.DefaultProperties["Caption"] == "Attention",
            "The custom-property editor must preserve canonical declaration casing and update its value.");
        editor.Undo();
        Assert(editor.Canvas.Elements.Single().Visual.Tag is DesignerCustomControlMetadata undoneMetadata
            && undoneMetadata.DefaultProperties["Caption"] == "Ready",
            "Undo must restore the preceding declared custom-property value.");
        editor.Redo();
        Assert(editor.Canvas.Elements.Single().Visual.Tag is DesignerCustomControlMetadata redoneMetadata
            && redoneMetadata.DefaultProperties["Caption"] == "Attention",
            "Redo must restore the edited declared custom-property value.");

        editor.SelectElement(editor.Canvas.Elements.Single());
        Assert(editor.SetSelectedBindings(
        [
            "Unit | Preview.Unit | OneWay | percent",
            "Opacity | Preview.Opacity | OneWay | 0.35",
        ]), editor.StatusText);
        Assert(editor.TryGetSelectedCustomProperties(out state)
            && state.EditableProperties.SequenceEqual(["Caption", "Unit"])
            && state.Lines.SequenceEqual(["Caption = Attention"]),
            "A bound declaration without a default must remain editable but must not appear as an effective local value.");
        Assert(editor.SetSelectedCustomProperties(["Caption = Alert", "Unit = ms"]), editor.StatusText);
        var bindings = DesignerBindingRuntime.ReadBindings(editor.Canvas.Elements.Single().Visual);
        Assert(bindings is [{ PropertyName: "Opacity" }],
            "Setting a custom local value must replace only that property's binding.");
        editor.Undo();
        Assert(DesignerBindingRuntime.ReadBindings(editor.Canvas.Elements.Single().Visual).Count == 2,
            "Undo must restore the custom binding replaced by a local value.");
        editor.Redo();
        custom = editor.Canvas.Elements.Single();
        Assert(custom.Visual.Tag is DesignerCustomControlMetadata alertMetadata
            && alertMetadata.DefaultProperties["Caption"] == "Alert"
            && alertMetadata.DefaultProperties["Unit"] == "ms"
            && DesignerBindingRuntime.ReadBindings(custom.Visual) is [{ PropertyName: "Opacity" }],
            "Redo must restore the local custom value while retaining unrelated bindings.");

        var source = editor.ExportDraftAxaml();
        Assert(source.Contains("Caption=\"Alert\"")
            && source.Contains("Unit=\"ms\"")
            && source.Contains("Opacity=\"{ReflectionBinding Preview.Opacity")
            && !source.Contains("Unit=\"{ReflectionBinding"),
            "Draft AXAML must serialize default-free custom values and only the retained binding.");
        Assert(editor.TryImportDraftAxaml(source, out var importError, out _), importError);
        custom = editor.Canvas.Elements.Single();
        Assert(custom.Visual.Tag is DesignerCustomControlMetadata importedMetadata
            && importedMetadata.DefaultProperties["Caption"] == "Alert"
            && importedMetadata.DefaultProperties["Unit"] == "ms",
            "A declared custom-property edit must survive Draft re-import.");
        var preview = new PreviewWindow(editor.CreatePreviewDocument());
        try
        {
            var previewLayout = (Grid)preview.Content!;
            var previewSurface = previewLayout.Children.OfType<Border>().Single();
            var previewViewport = (ScrollViewer)previewSurface.Child!;
            var previewCanvas = (Canvas)previewViewport.Content!;
            Assert(previewCanvas.Children.Single().Tag is DesignerCustomControlMetadata previewMetadata
                && previewMetadata.DefaultProperties["Caption"] == "Alert"
                && previewMetadata.DefaultProperties["Unit"] == "ms"
                && previewMetadata.DeclaredProperties.Contains("Caption")
                && previewMetadata.DeclaredProperties.Contains("Opacity")
                && previewMetadata.DeclaredProperties.Contains("Unit"),
                "Preview must retain the edited declared custom-property value for the external control.");
        }
        finally
        {
            preview.Close();
        }

        editor.SelectElement(editor.Canvas.Elements.Single());
        Assert(editor.SetSelectedCustomProperties([]), editor.StatusText);
        Assert(!editor.ExportDraftAxaml().Contains("Caption=\"", StringComparison.Ordinal)
            && !editor.ExportDraftAxaml().Contains("Unit=\"", StringComparison.Ordinal)
            && DesignerBindingRuntime.ReadBindings(editor.Canvas.Elements.Single().Visual) is
                [{ PropertyName: "Opacity" }],
            "Removing a custom-property line must omit its local AXAML value without removing unrelated bindings.");
        Assert(editor.TryExportSelectedComponentPack(
            "Exported controls",
            "Exported Gauge",
            "ExportedGauge",
            out var exportedJson,
            out var exportError), exportError);
        var exportedPack = JsonSerializer.Deserialize<ComponentPackDocument>(
            exportedJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var exportedComponent = exportedPack?.Components.Single()
            ?? throw new Exception("The exported component pack could not be read.");
        Assert(exportedComponent.DeclaredProperties is { } exportedDeclarations
            && exportedDeclarations.Contains("Caption")
            && exportedDeclarations.Contains("Unit")
            && exportedComponent.DefaultProperties is { } exportedDefaults
            && !exportedDefaults.ContainsKey("Caption")
            && !exportedDefaults.ContainsKey("Unit"),
            "Component Pack export must retain declarations whose local values are unset.");
        var exportedEditor = new MainWindowViewModel();
        Assert(exportedEditor.TryLoadComponentPack(exportedJson, out var exportedResult), exportedResult);
        var exportedItem = exportedEditor.Toolbox.FindItemByDisplayName("Exported Gauge")
            ?? throw new Exception("The exported custom control was not added to Toolbox.");
        exportedEditor.PlaceToolboxItem(exportedItem, 20, 20);
        Assert(exportedEditor.TryGetSelectedCustomProperties(out var exportedState)
            && exportedState.EditableProperties.SequenceEqual(["Caption", "Unit"])
            && exportedState.Lines.Count == 0,
            "A reloaded Component Pack must expose declarations that have no default values.");
        var sourceId = editor.ComponentPacks.Single().SourceId;
        Assert(editor.TryRemoveComponentPack(sourceId, out var removeResult), removeResult);
        Assert(editor.TryGetSelectedCustomProperties(out state)
            && state.EditableProperties.SequenceEqual(["Caption", "Unit"])
            && state.Lines.Count == 0,
            "A removed component pack must not erase declarations retained by an in-use placeholder.");
        editor.Undo();
        Assert(editor.ExportDraftAxaml().Contains("Caption=\"Alert\"")
            && editor.ExportDraftAxaml().Contains("Unit=\"ms\"")
            && editor.Canvas.Elements.Single().Visual.Tag is DesignerCustomControlMetadata restoredMetadata
            && restoredMetadata.DeclaredProperties.Contains("Caption")
            && restoredMetadata.DeclaredProperties.Contains("Unit"),
            "Undo must restore a removed declared custom-property value and its declaration after pack removal.");
    }

    private static void RunCustomStyleChecks()
    {
        var editor = new MainWindowViewModel();
        Assert(editor.TryLoadComponentPack(ComponentPack, out var result), result);
        var item = editor.Toolbox.FindItemByDisplayName("Status Gauge")
            ?? throw new Exception("Loaded custom control was not added to Toolbox.");
        editor.PlaceToolboxItem(item, 32, 48);
        var custom = editor.Canvas.Elements.Single();
        Assert(custom.Visual.Tag is DesignerCustomControlMetadata
            {
                TypeName: "Acme.Controls.StatusGauge",
            }, "A design-only placeholder must retain its original Avalonia type name.");
        Assert(editor.SetDocumentStylesFromText("""
            [StatusGauge.styled]
            Opacity = 0.6

            [StatusGauge.styled:disabled]
            Opacity = 0.3
            """), editor.StatusText);
        Assert(editor.SetSelectedStyleClassesFromText("styled"), editor.StatusText);
        custom = editor.Canvas.Elements.Single();
        Assert(Math.Abs(custom.Visual.Opacity - 0.6) < 0.001,
            "A custom-control style must match the original type on the Design Surface.");
        Assert(custom.Visual.Tag is DesignerCustomControlMetadata metadata
            && !metadata.DefaultProperties.ContainsKey("Opacity"),
            "Applying a custom-control style must clear the conflicting local default value.");
        Assert(editor.SetSelectedStylePreviewState("disabled"), editor.StatusText);
        Assert(Math.Abs(custom.Visual.Opacity - 0.3) < 0.001,
            "A custom-control pseudo-class must be available in the style-state preview.");
        Assert(editor.SetSelectedStylePreviewState(null), editor.StatusText);
        Assert(Math.Abs(custom.Visual.Opacity - 0.6) < 0.001,
            "Resetting the style-state preview must restore the custom-control base style.");

        editor.Undo();
        custom = editor.Canvas.Elements.Single();
        Assert(CanvasViewModel.GetUserStyleClasses(custom.Visual).Count == 0
            && Math.Abs(custom.Visual.Opacity - 0.85) < 0.001,
            "Undo must restore the custom-control local value that preceded its style class.");
        editor.Redo();
        custom = editor.Canvas.Elements.Single();
        Assert(CanvasViewModel.GetUserStyleClasses(custom.Visual).SequenceEqual(["styled"])
            && Math.Abs(custom.Visual.Opacity - 0.6) < 0.001,
            "Redo must restore the custom-control style without reviving its local override.");
        editor.SelectElement(custom);
        Assert(editor.TryGetSelectedBindings(out var bindingState)
            && bindingState.SupportedProperties.Contains("Caption")
            && bindingState.SupportedProperties.Contains("Opacity")
            && bindingState.SupportedProperties.Contains("Unit"),
            "Declared binding properties must remain available after a style clears their local values.");

        var source = editor.ExportDraftAxaml();
        Assert(source.Contains("Selector=\"StatusGauge.styled\"")
            && source.Contains("Selector=\"StatusGauge.styled:disabled\"")
            && source.Contains("Classes=\"styled\""),
            "Draft AXAML must retain custom-control style selectors and classes.");
        Assert(!source.Contains("Opacity=\"0.85\"") && !source.Contains("Opacity=\"0.6\""),
            "Draft AXAML must not serialize a style-managed custom-control opacity as a local value.");
        Assert(editor.TryImportDraftAxaml(source, out var importError, out _), importError);
        custom = editor.Canvas.Elements.Single();
        Assert(Math.Abs(custom.Visual.Opacity - 0.6) < 0.001
            && custom.Visual.Tag is DesignerCustomControlMetadata importedMetadata
            && importedMetadata.TypeName == "Acme.Controls.StatusGauge"
            && !importedMetadata.DefaultProperties.ContainsKey("Opacity"),
            "Draft re-import must reapply the custom style using the preserved original type.");

        var preview = new PreviewWindow(editor.CreatePreviewDocument());
        try
        {
            var previewLayout = (Grid)preview.Content!;
            var previewSurface = previewLayout.Children.OfType<Border>().Single();
            var previewViewport = (ScrollViewer)previewSurface.Child!;
            var previewCanvas = (Canvas)previewViewport.Content!;
            var previewControl = previewCanvas.Children.Single();
            Assert(Math.Abs(previewControl.Opacity - 0.6) < 0.001,
                "Preview must apply a custom-control style using its original type.");
            previewControl.IsEnabled = false;
            Assert(Math.Abs(previewControl.Opacity - 0.3) < 0.001,
                "Preview must react to a custom-control pseudo-class state change.");
            preview.ResetPreview();
            previewCanvas = (Canvas)previewViewport.Content!;
            Assert(Math.Abs(previewCanvas.Children.Single().Opacity - 0.6) < 0.001,
                "Reset Preview must restore the custom-control base style.");
        }
        finally
        {
            preview.Close();
        }
    }

    private static void RunCustomDeclaredStyleChecks()
    {
        var editor = new MainWindowViewModel();
        Assert(editor.TryLoadComponentPack(ComponentPack, out var result), result);
        var item = editor.Toolbox.FindItemByDisplayName("Status Gauge")
            ?? throw new Exception("Loaded custom control was not added to Toolbox.");
        editor.PlaceToolboxItem(item, 32, 48);
        var unchangedSource = editor.ExportDraftAxaml();
        Assert(!editor.SetDocumentStylesFromText("""
            [StatusGauge.invalid]
            Unknown = value
            """) && editor.ExportDraftAxaml() == unchangedSource,
            "The style editor must reject undeclared custom setters without changing the document.");
        Assert(editor.SetDocumentStylesFromText("""
            [StatusGauge.units]
            Unit = percent

            [StatusGauge.units:disabled]
            Unit = unavailable
            """), editor.StatusText);
        Assert(editor.SetSelectedStyleClassesFromText("units"), editor.StatusText);
        var custom = editor.Canvas.Elements.Single();
        Assert(custom.Visual.Tag is DesignerCustomControlMetadata baseMetadata
            && baseMetadata.StyleProperties.TryGetValue("Unit", out var baseUnit)
            && baseUnit == "percent"
            && !baseMetadata.DefaultProperties.ContainsKey("Unit"),
            "A declared custom setter must be calculated separately from local values.");
        var propertyStates = editor.GetSelectedCustomPropertyValueStates();
        Assert(propertyStates.Single(state => state.PropertyName == "Caption") is
                {
                    Value: "Ready",
                    EditorValue: "Ready",
                    CanReset: true,
                    Source: DesignerCustomPropertyValueSource.Local,
                }
            && propertyStates.Single(state => state.PropertyName == "Unit") is
                {
                    Value: "percent",
                    EditorValue: "",
                    EditorWatermark: "Style: percent",
                    CanReset: false,
                    Source: DesignerCustomPropertyValueSource.Style,
                },
            "The custom-property summary must distinguish local and calculated style values.");
        Assert(editor.SetSelectedStylePreviewState("disabled"), editor.StatusText);
        Assert(custom.Visual.Tag is DesignerCustomControlMetadata disabledMetadata
            && disabledMetadata.StyleProperties.TryGetValue("Unit", out var disabledUnit)
            && disabledUnit == "unavailable",
            "A simulated pseudo-class must override the base custom setter.");
        Assert(editor.GetSelectedCustomPropertyValueStates().Single(state => state.PropertyName == "Unit") is
            { Value: "unavailable", Source: DesignerCustomPropertyValueSource.Style },
            "The custom-property summary must follow the simulated pseudo-class state.");
        Assert(editor.SetSelectedStylePreviewState(null), editor.StatusText);
        Assert(custom.Visual.Tag is DesignerCustomControlMetadata resetMetadata
            && resetMetadata.StyleProperties.TryGetValue("Unit", out var resetUnit)
            && resetUnit == "percent",
            "Resetting style-state preview must restore the base custom setter.");

        var source = editor.ExportDraftAxaml();
        Assert(source.Contains("<Setter Property=\"Unit\" Value=\"percent\" />")
            && source.Contains("<Setter Property=\"Unit\" Value=\"unavailable\" />")
            && !source.Contains(" Unit=\"percent\"", StringComparison.Ordinal),
            "Draft AXAML must retain custom setters without writing their calculated values locally.");
        Assert(editor.TryImportDraftAxaml(source, out var importError, out _), importError);
        custom = editor.Canvas.Elements.Single();
        Assert(custom.Visual.Tag is DesignerCustomControlMetadata importedMetadata
            && importedMetadata.StyleProperties.TryGetValue("Unit", out var importedUnit)
            && importedUnit == "percent",
            "Draft re-import must restore a calculated declared custom setter.");

        var preview = new PreviewWindow(editor.CreatePreviewDocument());
        try
        {
            var previewLayout = (Grid)preview.Content!;
            var previewSurface = previewLayout.Children.OfType<Border>().Single();
            var previewViewport = (ScrollViewer)previewSurface.Child!;
            var previewCanvas = (Canvas)previewViewport.Content!;
            var previewControl = previewCanvas.Children.Single();
            Assert(previewControl.Tag is DesignerCustomControlMetadata previewMetadata
                && previewMetadata.StyleProperties.TryGetValue("Unit", out var previewUnit)
                && previewUnit == "percent",
                "Preview must calculate a declared custom setter.");
            previewControl.IsEnabled = false;
            Assert(previewControl.Tag is DesignerCustomControlMetadata previewDisabledMetadata
                && previewDisabledMetadata.StyleProperties.TryGetValue("Unit", out var previewDisabledUnit)
                && previewDisabledUnit == "unavailable",
                "Preview must recalculate a declared custom setter after a pseudo-class state change.");
            preview.ResetPreview();
            previewCanvas = (Canvas)previewViewport.Content!;
            Assert(previewCanvas.Children.Single().Tag is DesignerCustomControlMetadata previewResetMetadata
                && previewResetMetadata.StyleProperties.TryGetValue("Unit", out var previewResetUnit)
                && previewResetUnit == "percent",
                "Reset Preview must restore the base declared custom setter.");
        }
        finally
        {
            preview.Close();
        }

        editor.SelectElement(custom);
        var beforeInlineEditSource = editor.ExportDraftAxaml();
        Assert(!editor.SetSelectedCustomPropertyValue("Unknown", "value")
            && editor.ExportDraftAxaml() == beforeInlineEditSource,
            "Inline editing must reject an undeclared custom property without changing the document.");
        Assert(!editor.SetSelectedCustomPropertyValue("Unit", "invalid\u0001value")
            && editor.ExportDraftAxaml() == beforeInlineEditSource,
            "Inline editing must reject invalid XML characters without changing the document.");
        Assert(editor.SetSelectedCustomPropertyValue("unit", "ms"), editor.StatusText);
        custom = editor.Canvas.Elements.Single();
        Assert(custom.Visual.Tag is DesignerCustomControlMetadata localMetadata
            && localMetadata.DefaultProperties["Unit"] == "ms"
            && !localMetadata.StyleProperties.ContainsKey("Unit"),
            "A local custom value must override and hide the calculated style value.");
        Assert(editor.GetSelectedCustomPropertyValueStates().Single(state => state.PropertyName == "Unit") is
            {
                Value: "ms",
                BindingActionLabel: "Bind",
                Source: DesignerCustomPropertyValueSource.Local,
            },
            "The custom-property summary must report a local override.");
        Assert(editor.ExportDraftAxaml().Contains("Unit=\"ms\""),
            "A local custom value must remain in Draft AXAML even when a style targets the same property.");
        Assert(editor.SetSelectedCustomPropertyValue("Unit", "ms"), editor.StatusText);
        editor.Undo();
        custom = editor.Canvas.Elements.Single();
        Assert(custom.Visual.Tag is DesignerCustomControlMetadata styleUndoMetadata
            && styleUndoMetadata.StyleProperties.TryGetValue("Unit", out var styleUndoUnit)
            && styleUndoUnit == "percent",
            "Undo must restore the calculated style value after removing a local override.");
        editor.Redo();
        custom = editor.Canvas.Elements.Single();
        Assert(custom.Visual.Tag is DesignerCustomControlMetadata styleRedoMetadata
            && styleRedoMetadata.DefaultProperties["Unit"] == "ms"
            && !styleRedoMetadata.StyleProperties.ContainsKey("Unit"),
            "Redo must restore the local custom override.");

        editor.SelectElement(custom);
        Assert(editor.SetSelectedBindings([
            "Opacity | Preview.Opacity | OneWay | 0.35",
        ]), editor.StatusText);
        Assert(editor.TryGetSelectedCustomPropertyBinding("unit", out var newBindingState)
            && newBindingState is
            {
                PropertyName: "Unit",
                Path: "",
                Mode: DesignerBindingMode.Default,
                FallbackValue: "",
                HasBinding: false,
            }, "The property binding editor must initialize an unbound declaration.");
        var beforeInvalidPropertyBinding = editor.ExportDraftAxaml();
        Assert(!editor.SetSelectedCustomPropertyBinding(
                "Unit",
                "invalid path",
                nameof(DesignerBindingMode.OneWay),
                "percent")
            && editor.ExportDraftAxaml() == beforeInvalidPropertyBinding,
            "A property binding with an invalid path must not change the document.");
        Assert(editor.SetSelectedCustomPropertyBinding(
            "unit",
            "Preview.Unit",
            nameof(DesignerBindingMode.OneWay),
            "percent"), editor.StatusText);
        custom = editor.Canvas.Elements.Single();
        Assert(custom.Visual.Tag is DesignerCustomControlMetadata bindingMetadata
            && bindingMetadata.DefaultProperties["Unit"] == "ms"
            && DesignerBindingRuntime.ReadBindings(custom.Visual).Count == 2
            && DesignerBindingRuntime.ReadBindings(custom.Visual).Single(binding =>
                binding.PropertyName == "Unit") is
            {
                PropertyName: "Unit",
                Path: "Preview.Unit",
                Mode: DesignerBindingMode.OneWay,
                FallbackValue: "percent",
            }, "Adding a property binding must preserve its lower-precedence local value.");
        Assert(editor.TryGetSelectedCustomPropertyBinding("Unit", out var existingBindingState)
            && existingBindingState is
            {
                Path: "Preview.Unit",
                Mode: DesignerBindingMode.OneWay,
                FallbackValue: "percent",
                HasBinding: true,
            }, "The property binding editor must load the existing binding fields.");
        Assert(editor.GetSelectedCustomPropertyValueStates().Single(state => state.PropertyName == "Unit") is
            {
                BindingActionLabel: "Edit",
                Source: DesignerCustomPropertyValueSource.Binding,
            }, "A bound custom property must expose an Edit binding action.");
        Assert(editor.SetSelectedCustomPropertyBinding(
            "Unit",
            "Preview.Unit",
            nameof(DesignerBindingMode.OneWay),
            "percent"), editor.StatusText);
        editor.Undo();
        custom = editor.Canvas.Elements.Single();
        Assert(DesignerBindingRuntime.ReadBindings(custom.Visual) is [{ PropertyName: "Opacity" }]
            && custom.Visual.Tag is DesignerCustomControlMetadata bindingUndoMetadata
            && bindingUndoMetadata.DefaultProperties["Unit"] == "ms",
            "An unchanged property binding must not add history, and Undo must retain unrelated bindings while revealing the local value.");
        editor.Redo();
        custom = editor.Canvas.Elements.Single();
        Assert(DesignerBindingRuntime.ReadBindings(custom.Visual).Count == 2
            && DesignerBindingRuntime.ReadBindings(custom.Visual).Any(binding =>
                binding.PropertyName == "Unit"),
            "Redo must restore the property-specific binding beside unrelated bindings.");
        editor.SelectElement(custom);
        Assert(editor.RemoveSelectedCustomPropertyBinding("Unit"), editor.StatusText);
        custom = editor.Canvas.Elements.Single();
        Assert(DesignerBindingRuntime.ReadBindings(custom.Visual) is [{ PropertyName: "Opacity" }]
            && custom.Visual.Tag is DesignerCustomControlMetadata bindingRemovedMetadata
            && bindingRemovedMetadata.DefaultProperties["Unit"] == "ms"
            && editor.GetSelectedCustomPropertyValueStates().Single(state => state.PropertyName == "Unit") is
                { Value: "ms", Source: DesignerCustomPropertyValueSource.Local },
            "Removing only the binding must reveal the preserved local value.");
        editor.Undo();
        Assert(DesignerBindingRuntime.ReadBindings(editor.Canvas.Elements.Single().Visual).Count == 2
            && DesignerBindingRuntime.ReadBindings(editor.Canvas.Elements.Single().Visual).Any(binding =>
                binding.PropertyName == "Unit"),
            "Undo must restore a removed property-specific binding.");
        editor.Redo();
        custom = editor.Canvas.Elements.Single();
        Assert(DesignerBindingRuntime.ReadBindings(custom.Visual) is [{ PropertyName: "Opacity" }],
            "Redo must remove only the property-specific binding again while preserving unrelated bindings.");

        editor.SelectElement(custom);
        Assert(editor.ResetSelectedCustomPropertyValue("Unit"), editor.StatusText);
        Assert(editor.Canvas.Elements.Single().Visual.Tag is DesignerCustomControlMetadata clearedMetadata
            && clearedMetadata.StyleProperties["Unit"] == "percent"
            && clearedMetadata.DefaultProperties["Caption"] == "Ready",
            "Removing a local custom value must immediately reveal its style value.");
        Assert(editor.SetSelectedBindings([
            "Opacity | Preview.Opacity | OneWay | 0.35",
            "Unit | Preview.Unit | OneWay | percent",
        ]), editor.StatusText);
        Assert(editor.Canvas.Elements.Single().Visual.Tag is DesignerCustomControlMetadata boundMetadata
            && !boundMetadata.StyleProperties.ContainsKey("Unit"),
            "A custom binding must take precedence over a declared custom setter.");
        Assert(editor.GetSelectedCustomPropertyValueStates().Single(state => state.PropertyName == "Unit") is
            {
                Value: "Preview.Unit | OneWay | fallback percent",
                EditorValue: "",
                EditorWatermark: "Binding: Preview.Unit | OneWay | fallback percent",
                CanReset: true,
                BindingActionLabel: "Edit",
                Source: DesignerCustomPropertyValueSource.Binding,
            }, "The custom-property summary must report the binding path, mode, and fallback.");
        source = editor.ExportDraftAxaml();
        Assert(source.Contains("Unit=\"{ReflectionBinding Preview.Unit")
            && source.Contains("<Setter Property=\"Unit\" Value=\"percent\" />"),
            "Draft AXAML must retain both a custom binding and the lower-precedence custom style.");
        Assert(editor.ResetSelectedCustomPropertyValue("Unit"), editor.StatusText);
        Assert(editor.GetSelectedCustomPropertyValueStates().Single(state => state.PropertyName == "Unit") is
                { Value: "percent", Source: DesignerCustomPropertyValueSource.Style }
            && DesignerBindingRuntime.ReadBindings(editor.Canvas.Elements.Single().Visual) is
                [{ PropertyName: "Opacity" }],
            "Resetting a custom binding must reveal the lower-precedence style value.");
        Assert(editor.SetDocumentStylesFromText(string.Empty), editor.StatusText);
        Assert(editor.GetSelectedCustomPropertyValueStates().Single(state => state.PropertyName == "Unit") is
            {
                Source: DesignerCustomPropertyValueSource.Unset,
                DisplayValue: "(unset)",
                EditorValue: "",
                EditorWatermark: "Set local value",
                CanReset: false,
                BindingActionLabel: "Bind",
            },
            "The custom-property summary must identify an unset declaration.");
    }

    private static void RunTypedCustomPropertyChecks()
    {
        var invalidEditor = new MainWindowViewModel();
        Assert(!invalidEditor.TryLoadComponentPack("""
            {
              "components": [
                {
                  "displayName": "Invalid Typed Gauge",
                  "avaloniaTypeName": "Acme.Controls.InvalidTypedGauge",
                  "designOnly": true,
                  "propertyDefinitions": [
                    { "name": "State", "type": "Enum" }
                  ]
                }
              ]
            }
            """, out var invalidDefinitionResult)
            && invalidDefinitionResult.Contains("at least one option", StringComparison.OrdinalIgnoreCase),
            "An Enum custom property must declare at least one option.");
        Assert(!invalidEditor.TryLoadComponentPack("""
            {
              "components": [
                {
                  "displayName": "Invalid Typed Default",
                  "avaloniaTypeName": "Acme.Controls.InvalidTypedDefault",
                  "designOnly": true,
                  "propertyDefinitions": [
                    { "name": "Count", "type": "Integer", "minimum": 0, "maximum": 4 }
                  ],
                  "defaultProperties": { "Count": "5" }
                }
              ]
            }
            """, out var invalidDefaultResult)
            && invalidDefaultResult.Contains("whole number", StringComparison.OrdinalIgnoreCase),
            "A typed default outside its declared range must reject the component pack.");
        Assert(!invalidEditor.TryLoadComponentPack("""
            {
              "components": [
                {
                  "displayName": "Invalid Metadata",
                  "avaloniaTypeName": "Acme.Controls.InvalidMetadata",
                  "designOnly": true,
                  "propertyDefinitions": [
                    { "name": "Count", "category": "Data\nInput" }
                  ]
                }
              ]
            }
            """, out var invalidMetadataResult)
            && invalidMetadataResult.Contains("single line", StringComparison.OrdinalIgnoreCase),
            "A custom-property category containing a line break must reject the component pack.");
        Assert(!invalidEditor.TryLoadComponentPack("""
            {
              "components": [
                {
                  "displayName": "Invalid Corner Default",
                  "avaloniaTypeName": "Acme.Controls.InvalidCornerDefault",
                  "designOnly": true,
                  "propertyDefinitions": [
                    { "name": "Corners", "type": "CornerRadius" }
                  ],
                  "defaultProperties": { "Corners": "2,-1" }
                }
              ]
            }
            """, out var invalidCornerResult)
            && invalidCornerResult.Contains("not negative", StringComparison.OrdinalIgnoreCase),
            "A CornerRadius default containing a negative value must reject the component pack.");

        var editor = new MainWindowViewModel();
        Assert(editor.TryLoadComponentPack(TypedComponentPack, out var result), result);
        var item = editor.Toolbox.FindItemByDisplayName("Typed Gauge")
            ?? throw new Exception("The typed custom control was not added to Toolbox.");
        editor.PlaceToolboxItem(item, 40, 56);
        var custom = editor.Canvas.Elements.Single();
        Assert(custom.Visual.Tag is DesignerCustomControlMetadata metadata
            && metadata.DeclaredProperties.Count == 8
            && metadata.PropertyDefinitions is { Count: 8 }
            && metadata.DefaultProperties["IsActive"] == "True"
            && metadata.DefaultProperties["Count"] == "2"
            && metadata.DefaultProperties["Ratio"] == "0.5"
            && metadata.DefaultProperties["Accent"] == "#ff3b82f6"
            && metadata.DefaultProperties["Corners"] == "3,4,5,6"
            && metadata.DefaultProperties["Insets"] == "1,2,1,2"
            && metadata.DefaultProperties["State"] == "Idle"
            && metadata.PropertyDefinitions.Single(definition => definition.Name == "Count") is
            {
                DisplayName: "Item count",
                Category: "Data",
                Description: "Number of visible items.",
            }, "Loading a typed pack must normalize declarations, defaults, and Inspector metadata.");

        var states = editor.GetSelectedCustomPropertyValueStates();
        var orderedPropertyNames = states.Select(state => state.PropertyName).ToList();
        var categoryHeaders = states.Where(state => state.ShowsCategoryHeader)
            .Select(state => state.CategoryLabel)
            .ToList();
        Assert(orderedPropertyNames.SequenceEqual(
                ["Accent", "Corners", "IsActive", "State", "Ratio", "Count", "Insets", "Note"])
            && categoryHeaders.SequenceEqual(["Appearance", "Behavior", "Data", "Layout", "Custom"]),
            $"Typed custom properties must be grouped by explicit category with uncategorized values last. Got {string.Join(", ", states.Select(state => $"{state.PropertyName}:{state.CategoryLabel}:{state.ShowsCategoryHeader}"))}.");
        Assert(states.Single(state => state.PropertyName == "IsActive") is
            {
                Type: DesignerCustomPropertyType.Boolean,
                DisplayName: "Active",
                CategoryLabel: "Behavior",
                Description: "Toggles the active visual state.",
                MetadataLabel: "IsActive | Boolean",
                ShowsCategoryHeader: true,
                UsesChoiceEditor: true,
                EditorChoiceValue: "True",
            } booleanState
            && booleanState.EditorChoices.SequenceEqual(["False", "True"]),
            "A Boolean declaration must expose canonical choices and its local selection.");
        Assert(states.Single(state => state.PropertyName == "State") is
            {
                Type: DesignerCustomPropertyType.Enum,
                DisplayName: "Operating mode",
                CategoryLabel: "Behavior",
                ShowsCategoryHeader: false,
                UsesChoiceEditor: true,
                TypeLabel: "Enum",
            } enumState
            && enumState.EditorChoices.SequenceEqual(["Idle", "Busy"]),
            "An Enum declaration must expose the component pack options.");
        Assert(states.Single(state => state.PropertyName == "Count") is
            {
                Type: DesignerCustomPropertyType.Integer,
                DisplayName: "Item count",
                CategoryLabel: "Data",
                ShowsCategoryHeader: false,
                UsesNumericEditor: true,
                UsesTextEditor: false,
                NumericEditorValue: 2,
                NumericEditorMinimum: 0,
                NumericEditorMaximum: 10,
                NumericEditorIncrement: 1,
            }, "An Integer declaration must expose its value and range to a spin editor.");
        Assert(states.Single(state => state.PropertyName == "Ratio") is
            {
                Type: DesignerCustomPropertyType.Double,
                DisplayName: "Fill ratio",
                CategoryLabel: "Data",
                ShowsCategoryHeader: true,
                UsesNumericEditor: true,
                NumericEditorValue: 0.5m,
                NumericEditorMinimum: 0,
                NumericEditorMaximum: 1,
                NumericEditorIncrement: 0.1m,
            }, "A Double declaration must expose fractional spin-editor settings.");
        Assert(states.Single(state => state.PropertyName == "Accent") is
            {
                Type: DesignerCustomPropertyType.Color,
                DisplayName: "Accent color",
                CategoryLabel: "Appearance",
                Description: "Primary gauge highlight color.",
                ShowsCategoryHeader: true,
                UsesColorEditor: true,
                UsesTextEditor: false,
                HasColorPreview: true,
            } colorState
            && colorState.ColorPreviewBrush is Avalonia.Media.SolidColorBrush
            {
                Color: { A: 255, R: 59, G: 130, B: 246 },
            }, "A Color declaration must expose a canonical preview brush and color editor.");
        Assert(states.Single(state => state.PropertyName == "Insets") is
            {
                Type: DesignerCustomPropertyType.Thickness,
                DisplayName: "Content inset",
                CategoryLabel: "Layout",
                UsesFourValueEditor: true,
                UsesTextEditor: false,
            } thicknessState
            && DesignerCustomPropertyRuntime.TryGetFourValueComponents(
                thicknessState.Type,
                thicknessState.Value,
                out var thicknessValues,
                out _)
            && thicknessValues.SequenceEqual([1d, 2d, 1d, 2d]),
            "A Thickness declaration must expand Avalonia horizontal/vertical shorthand for four-value editing.");
        Assert(states.Single(state => state.PropertyName == "Corners") is
            {
                Type: DesignerCustomPropertyType.CornerRadius,
                DisplayName: "Corner shape",
                CategoryLabel: "Appearance",
                UsesFourValueEditor: true,
                UsesTextEditor: false,
            } cornerState
            && DesignerCustomPropertyRuntime.TryGetFourValueComponents(
                cornerState.Type,
                "2,3",
                out var cornerValues,
                out _)
            && cornerValues.SequenceEqual([2d, 2d, 3d, 3d]),
            "A CornerRadius declaration must expand Avalonia top/bottom shorthand for four-value editing.");
        Assert(states.Single(state => state.PropertyName == "Note") is
            {
                DisplayName: "Note",
                CategoryLabel: "Custom",
                Description: "",
                MetadataLabel: "String",
                ShowsCategoryHeader: true,
            }, "A legacy declaration must retain its property name and use the final Custom category.");
        var extremeDoubleState = new DesignerCustomPropertyValueState(
            "Huge",
            "1E+100",
            DesignerCustomPropertyValueSource.Local,
            new DesignerCustomPropertyDefinition("Huge", DesignerCustomPropertyType.Double));
        Assert(!extremeDoubleState.UsesNumericEditor && extremeDoubleState.UsesTextEditor,
            "A valid Double outside decimal range must fall back to text editing without data loss.");

        var beforeInvalidEdit = editor.ExportDraftAxaml();
        Assert(!editor.SetSelectedCustomPropertyValue("Count", "11")
            && editor.ExportDraftAxaml() == beforeInvalidEdit,
            "An inline integer outside its range must not change the document.");
        Assert(!editor.SetSelectedCustomProperties(["Accent = not-a-color"])
            && editor.ExportDraftAxaml() == beforeInvalidEdit,
            "The bulk custom-property editor must enforce typed color validation.");
        Assert(!editor.SetSelectedCustomPropertyValue("Corners", "1,-1,2,3")
            && editor.ExportDraftAxaml() == beforeInvalidEdit,
            "A negative CornerRadius edit must leave the document unchanged.");
        Assert(editor.SetSelectedCustomPropertyValue("Count", "07"), editor.StatusText);
        Assert(editor.SetSelectedCustomPropertyValue("State", "busy"), editor.StatusText);
        Assert(editor.SetSelectedCustomPropertyValue("IsActive", "false"), editor.StatusText);
        Assert(editor.SetSelectedCustomPropertyValue("Insets", "5, 10"), editor.StatusText);
        custom = editor.Canvas.Elements.Single();
        Assert(custom.Visual.Tag is DesignerCustomControlMetadata editedMetadata
            && editedMetadata.DefaultProperties["Count"] == "7"
            && editedMetadata.DefaultProperties["State"] == "Busy"
            && editedMetadata.DefaultProperties["IsActive"] == "False"
            && editedMetadata.DefaultProperties["Insets"] == "5,10,5,10",
            "Typed inline edits must store canonical AXAML values.");

        Assert(editor.ResetSelectedCustomPropertyValue("Count"), editor.StatusText);
        Assert(editor.ResetSelectedCustomPropertyValue("Corners"), editor.StatusText);
        var beforeInvalidStyle = editor.ExportDraftAxaml();
        Assert(!editor.SetDocumentStylesFromText("""
            [TypedGauge.limit]
            Count = 12
            """) && editor.ExportDraftAxaml() == beforeInvalidStyle,
            "A typed custom style setter outside its range must not change the document.");
        Assert(editor.SetDocumentStylesFromText("""
            [TypedGauge.limit]
            Count = 04
            Corners = 2,3
            """), editor.StatusText);
        Assert(editor.SetSelectedStyleClassesFromText("limit"), editor.StatusText);
        Assert(editor.GetSelectedCustomPropertyValueStates().Single(state => state.PropertyName == "Count") is
            {
                Value: "4",
                Source: DesignerCustomPropertyValueSource.Style,
                Type: DesignerCustomPropertyType.Integer,
            }
            && editor.GetSelectedCustomPropertyValueStates().Single(state =>
                state.PropertyName == "Corners") is
                {
                    Value: "2,2,3,3",
                    Source: DesignerCustomPropertyValueSource.Style,
                    Type: DesignerCustomPropertyType.CornerRadius,
                }, "Valid typed custom setters must be normalized and exposed as style values.");

        var beforeInvalidFallback = editor.ExportDraftAxaml();
        Assert(!editor.SetSelectedCustomPropertyBinding(
                "Count",
                "Preview.Count",
                nameof(DesignerBindingMode.OneWay),
                "20")
            && editor.ExportDraftAxaml() == beforeInvalidFallback,
            "A typed binding fallback outside its range must not change the document.");
        Assert(editor.SetSelectedCustomPropertyBinding(
            "Count",
            "Preview.Count",
            nameof(DesignerBindingMode.OneWay),
            "03"), editor.StatusText);
        Assert(DesignerBindingRuntime.ReadBindings(editor.Canvas.Elements.Single().Visual).Single() is
            { PropertyName: "Count", FallbackValue: "3" },
            "A typed binding fallback must be normalized before storage.");
        Assert(editor.RemoveSelectedCustomPropertyBinding("Count"), editor.StatusText);
        Assert(editor.GetSelectedCustomPropertyValueStates().Single(state => state.PropertyName == "Count") is
            { Value: "4", Source: DesignerCustomPropertyValueSource.Style },
            "Removing a typed binding must reveal the validated style value.");
        Assert(editor.SetSelectedCustomPropertyBinding(
            "Insets",
            "Preview.Insets",
            nameof(DesignerBindingMode.OneWay),
            "4,6"), editor.StatusText);
        Assert(DesignerBindingRuntime.ReadBindings(editor.Canvas.Elements.Single().Visual).Single(binding =>
                binding.PropertyName == "Insets") is { FallbackValue: "4,6,4,6" },
            "A Thickness binding fallback must be expanded to a canonical four-value form.");
        var boundInsetsSource = editor.ExportDraftAxaml();
        Assert(boundInsetsSource.Contains("FallbackValue='4,6,4,6'", StringComparison.Ordinal),
            "A comma-containing four-value fallback must be quoted in generated AXAML.");
        var boundInsetsImportEditor = new MainWindowViewModel();
        Assert(boundInsetsImportEditor.TryLoadComponentPack(
            TypedComponentPack,
            out var boundInsetsPackResult), boundInsetsPackResult);
        Assert(boundInsetsImportEditor.TryImportDraftAxaml(
            boundInsetsSource,
            out var boundInsetsImportError,
            out _), boundInsetsImportError);
        Assert(DesignerBindingRuntime.ReadBindings(
                boundInsetsImportEditor.Canvas.Elements.Single().Visual).Single(binding =>
                binding.PropertyName == "Insets") is { FallbackValue: "4,6,4,6" },
            "Draft import must parse commas inside a quoted four-value binding fallback.");
        Assert(editor.RemoveSelectedCustomPropertyBinding("Insets"), editor.StatusText);
        Assert(editor.GetSelectedCustomPropertyValueStates().Single(state => state.PropertyName == "Insets") is
            { Value: "5,10,5,10", Source: DesignerCustomPropertyValueSource.Local },
            "Removing a Thickness binding must reveal its preserved local four-value state.");

        var source = editor.ExportDraftAxaml();
        Assert(source.Contains("IsActive=\"False\"")
            && source.Contains("State=\"Busy\"")
            && source.Contains("Insets=\"5,10,5,10\"")
            && source.Contains("<Setter Property=\"Count\" Value=\"4\" />")
            && source.Contains("<Setter Property=\"Corners\" Value=\"2,2,3,3\" />"),
            "Draft AXAML must use canonical typed values for local attributes and style setters.");
        var invalidImportEditor = new MainWindowViewModel();
        Assert(invalidImportEditor.TryLoadComponentPack(TypedComponentPack, out var importPackResult),
            importPackResult);
        var invalidTypedSource = source.Replace(
            "State=\"Busy\"",
            "State=\"Unknown\"",
            StringComparison.Ordinal);
        Assert(invalidImportEditor.TryImportDraftAxaml(
                invalidTypedSource,
                out var invalidImportError,
                out var invalidImportWarning), invalidImportError);
        Assert(invalidImportWarning.Contains("State", StringComparison.Ordinal)
            && invalidImportEditor.Canvas.Elements.Single().Visual.Tag is
                DesignerCustomControlMetadata invalidImportMetadata
            && !invalidImportMetadata.DefaultProperties.ContainsKey("State"),
            "Draft import must warn and omit an invalid typed custom-property value.");
        var bindingImportEditor = new MainWindowViewModel();
        Assert(bindingImportEditor.TryLoadComponentPack(TypedComponentPack, out var bindingPackResult),
            bindingPackResult);
        var typedBindingSource = source.Replace(
            "State=\"Busy\"",
            "state=\"{ReflectionBinding Preview.State}\"",
            StringComparison.Ordinal);
        Assert(bindingImportEditor.TryImportDraftAxaml(
            typedBindingSource,
            out var bindingImportError,
            out _), bindingImportError);
        Assert(DesignerBindingRuntime.ReadBindings(
                bindingImportEditor.Canvas.Elements.Single().Visual).Single(binding =>
                binding.PropertyName == "State") is { Path: "Preview.State" },
            "Draft import must canonicalize a typed binding property name even without a fallback.");
        Assert(editor.TryImportDraftAxaml(source, out var importError, out _), importError);
        custom = editor.Canvas.Elements.Single();
        Assert(custom.Visual.Tag is DesignerCustomControlMetadata importedMetadata
            && importedMetadata.PropertyDefinitions?.Single(definition => definition.Name == "Count") is
            {
                Type: DesignerCustomPropertyType.Integer,
                Minimum: 0,
                Maximum: 10,
                DisplayName: "Item count",
                Category: "Data",
                Description: "Number of visible items.",
            }
            && importedMetadata.DefaultProperties["Insets"] == "5,10,5,10"
            && importedMetadata.StyleProperties["Corners"] == "2,2,3,3",
            "Draft re-import with the component pack must restore typed property values and Inspector metadata.");
        var preview = new PreviewWindow(editor.CreatePreviewDocument());
        try
        {
            var previewLayout = (Grid)preview.Content!;
            var previewSurface = previewLayout.Children.OfType<Border>().Single();
            var previewViewport = (ScrollViewer)previewSurface.Child!;
            var previewCanvas = (Canvas)previewViewport.Content!;
            Assert(previewCanvas.Children.Single().Tag is DesignerCustomControlMetadata previewMetadata
                && previewMetadata.PropertyDefinitions?.Single(definition => definition.Name == "State") is
                    {
                        Type: DesignerCustomPropertyType.Enum,
                        DisplayName: "Operating mode",
                        Category: "Behavior",
                    } previewStateDefinition
                && previewStateDefinition.Options?.SequenceEqual(["Idle", "Busy"]) == true
                && previewMetadata.DefaultProperties["Insets"] == "5,10,5,10"
                && previewMetadata.StyleProperties["Corners"] == "2,2,3,3",
                "Headless Preview must retain typed custom-property definitions, values, and Inspector metadata.");
        }
        finally
        {
            preview.Close();
        }

        editor.SelectElement(custom);
        Assert(editor.TryExportSelectedComponentPack(
            "Typed export",
            "Typed Gauge Export",
            "TypedGaugeExport",
            out var exportedJson,
            out var exportError), exportError);
        var exportedPack = JsonSerializer.Deserialize<ComponentPackDocument>(
            exportedJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var exportedDefinitions = exportedPack?.Components.Single().PropertyDefinitions
            ?? throw new Exception("Typed definitions were not exported with the selected component.");
        Assert(exportedDefinitions.Single(definition => definition.Name == "Count") is
            {
                Type: "Integer",
                Minimum: 0,
                Maximum: 10,
                DisplayName: "Item count",
                Category: "Data",
                Description: "Number of visible items.",
            }
            && exportedDefinitions.Single(definition => definition.Name == "State").Options?
                .SequenceEqual(["Idle", "Busy"]) == true
            && exportedDefinitions.Single(definition => definition.Name == "Insets").Type == "Thickness"
            && exportedDefinitions.Single(definition => definition.Name == "Corners").Type == "CornerRadius",
            "Selected Component Pack export must retain typed ranges, Enum options, four-value types, and Inspector metadata.");

        var sourceId = editor.ComponentPacks.Single().SourceId;
        Assert(editor.TryRemoveComponentPack(sourceId, out var removeResult), removeResult);
        Assert(editor.GetSelectedCustomPropertyValueStates().Single(state => state.PropertyName == "IsActive") is
            {
                Type: DesignerCustomPropertyType.Boolean,
                DisplayName: "Active",
                CategoryLabel: "Behavior",
                Description: "Toggles the active visual state.",
                EditorChoiceValue: "False",
            }, "An in-use placeholder must retain typed Inspector metadata after its pack is removed.");

        var sourceBeforeInspectorRendering = editor.ExportDraftAxaml();
        var window = new MainWindow { DataContext = editor };
        window.Show();
        window.Measure(new Size(window.Width, window.Height));
        window.Arrange(new Rect(0, 0, window.Width, window.Height));
        try
        {
            var items = window.FindControl<ItemsControl>("DeclaredCustomPropertyItems")
                ?? throw new Exception("Custom-property Inspector list was not created.");
            var filter = window.FindControl<TextBox>("PropertyInspectorFilter")
                ?? throw new Exception("Property Inspector filter was not created.");
            filter.Text = "boolean";
            filter.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));
            Assert(items.ItemsSource?.Cast<DesignerCustomPropertyValueState>().Single() is
                { PropertyName: "IsActive", Type: DesignerCustomPropertyType.Boolean },
                "Property Inspector filtering must include typed custom-property labels.");
            filter.Text = "behavior";
            filter.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));
            var behaviorStates = items.ItemsSource?.Cast<DesignerCustomPropertyValueState>().ToList()
                ?? [];
            Assert(behaviorStates.Select(state => state.PropertyName).SequenceEqual(["IsActive", "State"])
                && behaviorStates[0].ShowsCategoryHeader
                && !behaviorStates[1].ShowsCategoryHeader,
                "Property Inspector filtering must match categories and rebuild their visible header.");
            filter.Text = "toggles";
            filter.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));
            Assert(items.ItemsSource?.Cast<DesignerCustomPropertyValueState>().Single() is
                { PropertyName: "IsActive", ShowsCategoryHeader: true },
                "Property Inspector filtering must match descriptions and keep a category header.");
            filter.Text = "operating mode";
            filter.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));
            Assert(items.ItemsSource?.Cast<DesignerCustomPropertyValueState>().Single() is
                { PropertyName: "State", ShowsCategoryHeader: true },
                "Property Inspector filtering must match display names and promote a surviving row to category header.");
            filter.Text = string.Empty;
            filter.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));
            Assert(editor.ExportDraftAxaml() == sourceBeforeInspectorRendering,
                "Rendering typed Inspector editors must not create a document edit.");
            Grid BuildTypedRow(string propertyName)
            {
                var state = items.ItemsSource?.Cast<DesignerCustomPropertyValueState>()
                    .Single(candidate => candidate.PropertyName == propertyName)
                    ?? throw new Exception($"Typed {propertyName} state was not shown in the Inspector.");
                var typedRow = items.ItemTemplate?.Build(state) as Grid
                    ?? throw new Exception("Typed custom-property Inspector row could not be built.");
                typedRow.DataContext = state;
                return typedRow;
            }

            var activeRow = BuildTypedRow("IsActive");
            var textEditor = activeRow.GetVisualDescendants().OfType<TextBox>().Single();
            var choiceEditor = activeRow.GetVisualDescendants().OfType<ComboBox>().Single();
            Assert(!textEditor.IsVisible
                && choiceEditor.IsVisible
                && activeRow.GetVisualDescendants().OfType<TextBlock>().Any(text =>
                    Equals(text.Text, "Active"))
                && activeRow.GetVisualDescendants().OfType<TextBlock>().Any(text =>
                    Equals(text.Text, "IsActive | Boolean"))
                && activeRow.GetVisualDescendants().OfType<TextBlock>().Any(text =>
                    Equals(text.Text, "Behavior"))
                && activeRow.GetVisualDescendants().OfType<StackPanel>().Any(panel =>
                    Equals(
                        ToolTip.GetTip(panel),
                        $"IsActive (Boolean){Environment.NewLine}Toggles the active visual state."))
                && choiceEditor.ItemsSource?.Cast<string>().SequenceEqual(["False", "True"]) == true,
                "The actual Inspector template must show category metadata and use a choice editor for Boolean properties.");

            var countRow = BuildTypedRow("Count");
            var countEditor = countRow.GetVisualDescendants().OfType<NumericUpDown>().Single();
            Assert(countEditor.IsVisible
                && !countRow.GetVisualDescendants().OfType<TextBox>().Single().IsVisible
                && countEditor.Value is null
                && countEditor.Minimum == 0
                && countEditor.Maximum == 10
                && countEditor.Increment == 1,
                "The actual Inspector template must use the declared Integer range and show style values as a watermark.");
            countEditor.Value = 6.5m;
            Assert(editor.GetSelectedCustomPropertyValueStates().Single(state =>
                    state.PropertyName == "Count") is
                { Value: "4", Source: DesignerCustomPropertyValueSource.Style }
                && countEditor.Value is null,
                "An invalid fractional Integer spinner value must be rejected and restored without changing the document.");
            countEditor.Value = 6;
            Avalonia.Threading.Dispatcher.UIThread.RunJobs(Avalonia.Threading.DispatcherPriority.Background);
            Assert(editor.GetSelectedCustomPropertyValueStates().Single(state =>
                    state.PropertyName == "Count") is
                { Value: "6", Source: DesignerCustomPropertyValueSource.Local },
                "Changing an Integer spinner in the Inspector must commit a canonical local value.");
            editor.Undo();
            editor.SelectElement(editor.Canvas.Elements.Single());
            Assert(editor.GetSelectedCustomPropertyValueStates().Single(state =>
                    state.PropertyName == "Count") is
                { Value: "4", Source: DesignerCustomPropertyValueSource.Style },
                "Undoing an Integer spinner edit must reveal its previous style value.");

            var ratioRow = BuildTypedRow("Ratio");
            var ratioEditor = ratioRow.GetVisualDescendants().OfType<NumericUpDown>().Single();
            Assert(ratioEditor.IsVisible
                && ratioEditor.Value == 0.5m
                && ratioEditor.Minimum == 0
                && ratioEditor.Maximum == 1
                && ratioEditor.Increment == 0.1m,
                "The actual Inspector template must use fractional Double spinner settings.");

            var accentRow = BuildTypedRow("Accent");
            var colorEditor = accentRow.GetVisualDescendants().OfType<Button>().Single(button =>
                Equals(button.Tag, "Accent") && button.Content is Grid);
            var swatch = ((Grid)colorEditor.Content!).Children.OfType<Border>().Single();
            Assert(colorEditor.IsVisible
                && swatch.Background is Avalonia.Media.SolidColorBrush
                {
                    Color: { A: 255, R: 59, G: 130, B: 246 },
                }, "The actual Inspector template must render the effective Color as an editable swatch.");
            var insetsRow = BuildTypedRow("Insets");
            var fourValueButton = insetsRow.GetVisualDescendants().OfType<Button>().Single(button =>
                Equals(button.Tag, "Insets") && button.Content is StackPanel);
            var insetsTemplateState = (DesignerCustomPropertyValueState)insetsRow.DataContext!;
            var fourValueContent = (StackPanel)fourValueButton.Content!;
            Assert(fourValueButton.IsVisible
                && insetsTemplateState is
                    { Type: DesignerCustomPropertyType.Thickness, UsesFourValueEditor: true }
                && !insetsRow.GetVisualDescendants().OfType<TextBox>().Single().IsVisible
                && fourValueContent.Children.OfType<TextBlock>().Any(text =>
                    Equals(text.Text, "5,10,5,10")),
                "The actual Inspector template must expose a dedicated four-value action for Thickness properties.");

            choiceEditor.SelectedItem = "True";
            Avalonia.Threading.Dispatcher.UIThread.RunJobs(Avalonia.Threading.DispatcherPriority.Background);
            Assert(editor.GetSelectedCustomPropertyValueStates().Single(state =>
                    state.PropertyName == "IsActive") is
                { Value: "True", Source: DesignerCustomPropertyValueSource.Local },
                "Selecting a Boolean choice in the Inspector must commit the typed value.");
            editor.Undo();
            editor.SelectElement(editor.Canvas.Elements.Single());
            Assert(editor.GetSelectedCustomPropertyValueStates().Single(state =>
                    state.PropertyName == "IsActive") is
                {
                    Value: "False",
                    Type: DesignerCustomPropertyType.Boolean,
                    EditorChoiceValue: "False",
                }, "Undo after pack removal must restore the typed Boolean state from snapshot metadata.");
            editor.Redo();
            editor.SelectElement(editor.Canvas.Elements.Single());
            Assert(editor.GetSelectedCustomPropertyValueStates().Single(state =>
                    state.PropertyName == "IsActive") is
                { Value: "True", Type: DesignerCustomPropertyType.Boolean },
                "Redo after pack removal must retain the typed definition and edited Boolean value.");
        }
        finally
        {
            window.DataContext = null;
            window.Close();
        }
    }

    private static void RunStructuredCustomPropertyEditorChecks()
    {
        var editor = new MainWindowViewModel();
        Assert(editor.TryLoadComponentPack(TypedComponentPack, out var result), result);
        var item = editor.Toolbox.FindItemByDisplayName("Typed Gauge")
            ?? throw new Exception("The structured editor test component was not added to Toolbox.");
        editor.PlaceToolboxItem(item, 40, 56);
        Assert(editor.TryGetSelectedCustomProperties(out var state)
            && state.ValueStates.Count == 8,
            "The bulk custom-property state must include source-aware typed rows.");

        var panel = new DesignerCustomPropertyEditorPanel(state.ValueStates);
        var categoryHeaders = panel.GetLogicalDescendants()
            .OfType<TextBlock>()
            .Select(text => text.Text)
            .Where(text => text is "Appearance" or "Behavior" or "Data" or "Layout" or "Custom")
            .ToList();
        var localToggles = panel.GetLogicalDescendants()
            .OfType<CheckBox>()
            .Where(toggle => toggle.Classes.Contains("custom-property-local-toggle"))
            .ToList();
        var propertyRows = panel.GetLogicalDescendants()
            .OfType<Border>()
            .Where(row => row.Classes.Contains("custom-property-editor-row"))
            .ToList();
        var categoryHeaderBorders = panel.GetLogicalDescendants()
            .OfType<Border>()
            .Where(header => header.Classes.Contains("custom-property-category-header"))
            .ToList();
        var countToggle = localToggles.Single(toggle => Equals(toggle.Tag, "Count"));
        var ratioToggle = localToggles.Single(toggle => Equals(toggle.Tag, "Ratio"));
        var ratioRow = propertyRows.Single(row => Equals(row.Tag, "Ratio"));
        var countEditor = panel.GetLogicalDescendants()
            .OfType<NumericUpDown>()
            .Single(control => Equals(control.Tag, "Count"));
        var stateEditor = panel.GetLogicalDescendants()
            .OfType<ComboBox>()
            .Single(control => Equals(control.Tag, "State"));
        var accentEditor = panel.GetLogicalDescendants()
            .OfType<ColorPicker>()
            .Single(control => Equals(control.Tag, "Accent"));
        var noteEditor = panel.GetLogicalDescendants()
            .OfType<TextBox>()
            .Single(control => Equals(control.Tag, "Note"));
        var insetsEditor = panel.GetLogicalDescendants()
            .OfType<DesignerCustomPropertyFourValueEditor>()
            .Single(control => Equals(control.Tag, "Insets"));
        var cornersEditor = panel.GetLogicalDescendants()
            .OfType<DesignerCustomPropertyFourValueEditor>()
            .Single(control => Equals(control.Tag, "Corners"));
        Assert(categoryHeaders.SequenceEqual(["Appearance", "Behavior", "Data", "Layout", "Custom"])
            && localToggles.Count == 8
            && propertyRows.Count == 8
            && categoryHeaderBorders.Count == 5
            && panel.VisibleRowCount == 8
            && localToggles.All(toggle => toggle.IsChecked == true)
            && countEditor.IsEnabled
            && countEditor.Value == 2
            && countEditor.Minimum == 0
            && countEditor.Maximum == 10
            && AutomationProperties.GetName(countToggle) == "Write Item count as a local value"
            && AutomationProperties.GetName(countEditor) == "Item count value"
            && stateEditor.SelectedItem?.ToString() == "Idle"
            && accentEditor.Color == Avalonia.Media.Color.Parse("#FF3B82F6")
            && noteEditor.Text == "Ready"
            && insetsEditor.TryGetValue(out var initialInsets, out _)
            && initialInsets == "1,2,1,2"
            && cornersEditor.TryGetValue(out var initialCorners, out _)
            && initialCorners == "3,4,5,6"
            && AutomationProperties.GetName(insetsEditor.ValueEditors[0])
                == "Content inset left value"
            && AutomationProperties.GetName(cornersEditor.ValueEditors[3])
                == "Corner shape bottom-left value",
            "The structured editor must render grouped, enabled, type-specific controls for local values.");

        panel.SetFilter("data", false);
        Assert(panel.VisibleRowCount == 2
            && propertyRows.Where(row => row.IsVisible)
                .Select(row => row.Tag?.ToString())
                .OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(["Count", "Ratio"])
            && categoryHeaderBorders.Single(header => Equals(header.Tag, "Data")).IsVisible
            && categoryHeaderBorders.Count(header => header.IsVisible) == 1,
            "Category filtering must show matching rows and recalculate category-header visibility.");
        Assert(panel.TryCreateEditorLines(out var filteredLines, out var filteredError)
            && filteredLines.Count == 8,
            $"Filtering must not discard hidden local values: {filteredError}");
        panel.SetFilter("gauge highlight", false);
        Assert(panel.VisibleRowCount == 1
            && propertyRows.Single(row => row.IsVisible).Tag?.ToString() == "Accent"
            && categoryHeaderBorders.Single(header => Equals(header.Tag, "Appearance")).IsVisible,
            "Description metadata must participate in structured-editor filtering.");
        panel.SetFilter("no property matches", false);
        Assert(panel.VisibleRowCount == 0
            && categoryHeaderBorders.All(header => !header.IsVisible),
            "An empty filter result must hide every orphaned category header.");
        panel.SetFilter(string.Empty, true);
        ratioToggle.IsChecked = false;
        Assert(panel.VisibleRowCount == 7 && !ratioRow.IsVisible,
            "Local-only filtering must immediately hide a row whose local override is unchecked.");
        panel.SetFilter("reset", false);
        Assert(panel.VisibleRowCount == 1 && ratioRow.IsVisible,
            "Filtering by the pending source label must find a local value marked for reset.");
        panel.SetFilter(string.Empty, false);
        Assert(panel.VisibleRowCount == 8 && ratioRow.IsVisible,
            "Clearing the filter must restore all rows without changing their local-override state.");

        countEditor.Value = 2.5m;
        Assert(!panel.TryCreateEditorLines(out _, out var invalidError)
            && invalidError.Contains("whole number", StringComparison.OrdinalIgnoreCase),
            "The structured editor must block an invalid fractional Integer before applying.");
        countEditor.Value = 3;
        stateEditor.SelectedItem = "Busy";
        accentEditor.Color = Avalonia.Media.Color.Parse("#803B82F6");
        noteEditor.Text = "Updated";
        insetsEditor.SetValues(8, 10, 12, 14);
        cornersEditor.SetValues(4, 5, 6, 7);
        var ratioSource = panel.GetLogicalDescendants()
            .OfType<TextBlock>()
            .Single(text => text.Classes.Contains("custom-property-source-label")
                && Equals(text.Tag, "Ratio"));
        Assert(!panel.GetLogicalDescendants()
                .OfType<NumericUpDown>()
                .Single(control => Equals(control.Tag, "Ratio"))
                .IsEnabled
            && ratioSource.Text == "RESET",
            "Unchecking Local must disable that property's editor and mark the pending reset.");
        Assert(panel.TryCreateEditorLines(out var updatedLines, out var updateError), updateError);
        Assert(DesignerCustomPropertyRuntime.TryParseEditorLines(
                updatedLines,
                state.EditableProperties,
                out var updatedValues,
                out var parseError,
                state.PropertyDefinitions), parseError);
        Assert(!updatedValues.ContainsKey("Ratio")
            && updatedValues["Count"] == "3"
            && updatedValues["State"] == "Busy"
            && updatedValues["Accent"] == "#803b82f6"
            && updatedValues["Insets"] == "8,10,12,14"
            && updatedValues["Corners"] == "4,5,6,7"
            && updatedValues["Note"] == "Updated",
            "The structured editor must emit canonical lines only for checked local overrides.");
        Assert(editor.SetSelectedCustomProperties(updatedLines), editor.StatusText);
        var updatedStates = editor.GetSelectedCustomPropertyValueStates();
        Assert(updatedStates.Single(value => value.PropertyName == "Ratio") is
                { Source: DesignerCustomPropertyValueSource.Unset }
            && updatedStates.Single(value => value.PropertyName == "Count") is
                { Value: "3", Source: DesignerCustomPropertyValueSource.Local }
            && updatedStates.Single(value => value.PropertyName == "Accent") is
                { Value: "#803b82f6", Source: DesignerCustomPropertyValueSource.Local }
            && updatedStates.Single(value => value.PropertyName == "Insets") is
                { Value: "8,10,12,14", Source: DesignerCustomPropertyValueSource.Local }
            && updatedStates.Single(value => value.PropertyName == "Corners") is
                { Value: "4,5,6,7", Source: DesignerCustomPropertyValueSource.Local },
            "Applying structured values must update and unset all rows atomically.");
        editor.Undo();
        editor.SelectElement(editor.Canvas.Elements.Single());
        Assert(editor.GetSelectedCustomPropertyValueStates().Single(value =>
                value.PropertyName == "Ratio") is
                { Value: "0.5", Source: DesignerCustomPropertyValueSource.Local }
            && editor.GetSelectedCustomPropertyValueStates().Single(value =>
                value.PropertyName == "Count") is
                { Value: "2", Source: DesignerCustomPropertyValueSource.Local },
            "Undo must restore every value changed by one structured bulk apply.");

        Assert(editor.SetSelectedCustomPropertyBinding(
            "Count",
            "Preview.Count",
            nameof(DesignerBindingMode.OneWay),
            "4"), editor.StatusText);
        Assert(editor.TryGetSelectedCustomProperties(out var boundState), editor.StatusText);
        var clearPanel = new DesignerCustomPropertyEditorPanel(boundState.ValueStates);
        var boundCountToggle = clearPanel.GetLogicalDescendants()
            .OfType<CheckBox>()
            .Single(toggle => Equals(toggle.Tag, "Count"));
        var boundCountEditor = clearPanel.GetLogicalDescendants()
            .OfType<NumericUpDown>()
            .Single(control => Equals(control.Tag, "Count"));
        Assert(boundCountToggle.IsChecked == false
            && !boundCountEditor.IsEnabled,
            "A bound property must start as a disabled, unchecked local override.");
        clearPanel.ClearLocalValues();
        Assert(clearPanel.GetLogicalDescendants()
                .OfType<CheckBox>()
                .Where(toggle => toggle.Classes.Contains("custom-property-local-toggle"))
                .All(toggle => toggle.IsChecked == false),
            "Clear local must uncheck every structured editor row.");
        Assert(clearPanel.TryCreateEditorLines(out var clearedLines, out var clearError), clearError);
        Assert(clearedLines.Count == 0,
            "An editor with every local override cleared must produce an empty value set.");
        Assert(editor.SetSelectedCustomProperties(clearedLines), editor.StatusText);
        var clearedStates = editor.GetSelectedCustomPropertyValueStates();
        Assert(clearedStates.Single(value => value.PropertyName == "Count") is
                { Source: DesignerCustomPropertyValueSource.Binding }
            && clearedStates.Where(value => value.PropertyName != "Count")
                .All(value => value.Source == DesignerCustomPropertyValueSource.Unset)
            && editor.Canvas.Elements.Single().Visual.Tag is DesignerCustomControlMetadata clearedMetadata
            && clearedMetadata.DefaultProperties["Count"] == "2",
            "Clear local must remove visible local values while retaining a binding and its lower local value.");
        editor.Undo();
        editor.SelectElement(editor.Canvas.Elements.Single());

        Assert(editor.TryGetSelectedCustomProperties(out boundState), editor.StatusText);
        var replacementPanel = new DesignerCustomPropertyEditorPanel(boundState.ValueStates);
        boundCountToggle = replacementPanel.GetLogicalDescendants()
            .OfType<CheckBox>()
            .Single(toggle => Equals(toggle.Tag, "Count"));
        boundCountEditor = replacementPanel.GetLogicalDescendants()
            .OfType<NumericUpDown>()
            .Single(control => Equals(control.Tag, "Count"));
        boundCountToggle.IsChecked = true;
        boundCountEditor.Value = 4;
        var replacementSource = replacementPanel.GetLogicalDescendants()
            .OfType<TextBlock>()
            .Single(text => text.Classes.Contains("custom-property-source-label")
                && Equals(text.Tag, "Count"));
        Assert(boundCountEditor.IsEnabled,
            "Checking a local override must enable its value editor.");
        Assert(replacementSource.Text == "LOCAL",
            "Checking a bound row must show that it will become a local value.");
        Assert(replacementPanel.TryCreateEditorLines(
            out var replacementLines,
            out var replacementError), replacementError);
        Assert(editor.SetSelectedCustomProperties(replacementLines), editor.StatusText);
        Assert(editor.GetSelectedCustomPropertyValueStates().Single(value =>
                value.PropertyName == "Count") is
                { Value: "4", Source: DesignerCustomPropertyValueSource.Local }
            && DesignerBindingRuntime.ReadBindings(editor.Canvas.Elements.Single().Visual)
                .All(binding => binding.PropertyName != "Count"),
            "Checking a bound property must replace that binding with one local value.");
        editor.Undo();
        editor.SelectElement(editor.Canvas.Elements.Single());
        Assert(editor.GetSelectedCustomPropertyValueStates().Single(value =>
                value.PropertyName == "Count") is
                { Source: DesignerCustomPropertyValueSource.Binding },
            "Undo must restore a binding replaced through the structured bulk editor.");
    }

    private static void RunCustomPropertyInspectorSummaryChecks()
    {
        var editor = new MainWindowViewModel();
        Assert(editor.TryLoadComponentPack(ComponentPack, out var result), result);
        var item = editor.Toolbox.FindItemByDisplayName("Status Gauge")
            ?? throw new Exception("Loaded custom control was not added to Toolbox.");
        editor.PlaceToolboxItem(item, 32, 48);
        Assert(editor.SetDocumentStylesFromText("""
            [StatusGauge.units]
            Unit = percent
            """), editor.StatusText);
        Assert(editor.SetSelectedStyleClassesFromText("units"), editor.StatusText);
        Assert(editor.SetSelectedCustomPropertyValue("Unit", "ms"), editor.StatusText);

        var window = new MainWindow { DataContext = editor };
        window.Show();
        window.Measure(new Size(window.Width, window.Height));
        window.Arrange(new Rect(0, 0, window.Width, window.Height));
        try
        {
            var panel = window.FindControl<Border>("DeclaredCustomPropertyPanel")
                ?? throw new Exception("Custom-property Inspector panel was not created.");
            var items = window.FindControl<ItemsControl>("DeclaredCustomPropertyItems")
                ?? throw new Exception("Custom-property Inspector list was not created.");
            var filter = window.FindControl<TextBox>("PropertyInspectorFilter")
                ?? throw new Exception("Property Inspector filter was not created.");
            var editButton = window.FindControl<Button>("DeclaredCustomPropertyEditButton")
                ?? throw new Exception("Custom-property Inspector edit button was not created.");
            IReadOnlyList<DesignerCustomPropertyValueState> VisibleStates()
                => items.ItemsSource?.Cast<DesignerCustomPropertyValueState>().ToList() ?? [];
            Grid BuildUnitRow()
            {
                var state = VisibleStates().Single(candidate => candidate.PropertyName == "Unit");
                var row = items.ItemTemplate?.Build(state) as Grid
                    ?? throw new Exception("Custom-property Inspector row template could not be built.");
                row.DataContext = state;
                return row;
            }

            Assert(panel.IsVisible
                && VisibleStates().Any(state => state is
                    { PropertyName: "Unit", Value: "ms", Source: DesignerCustomPropertyValueSource.Local }),
                "Property Inspector must show the local custom-property state.");
            var unitRow = BuildUnitRow();
            var unitEditor = unitRow.GetVisualDescendants().OfType<TextBox>().Single();
            var bindingButton = unitRow.Children.OfType<Button>().Single(button =>
                Equals(button.Content, "Bind"));
            var resetButton = unitRow.Children.OfType<Button>().Single(button =>
                Equals(button.Content, "Reset"));
            Assert(unitEditor.Text == "ms"
                && bindingButton.IsVisible
                && Equals(bindingButton.Tag, "Unit")
                && resetButton.IsVisible,
                "A local custom property must render editable, Bind, and Reset actions.");
            unitEditor.Text = "seconds";
            unitEditor.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(InputElement.LostFocusEvent));
            Avalonia.Threading.Dispatcher.UIThread.RunJobs(Avalonia.Threading.DispatcherPriority.Background);
            Assert(VisibleStates().Single(state => state.PropertyName == "Unit") is
                { Value: "seconds", Source: DesignerCustomPropertyValueSource.Local },
                "Leaving an inline custom-property editor must commit its local value.");
            resetButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Avalonia.Threading.Dispatcher.UIThread.RunJobs(Avalonia.Threading.DispatcherPriority.Background);
            Assert(VisibleStates().Single(state => state.PropertyName == "Unit") is
                {
                    Value: "percent",
                    EditorWatermark: "Style: percent",
                    BindingActionLabel: "Bind",
                    Source: DesignerCustomPropertyValueSource.Style,
                }, "The inline Reset action must reveal the calculated style value.");
            filter.Text = "style";
            filter.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));
            Assert(panel.IsVisible && VisibleStates().All(state => state.SourceLabel == "STYLE"),
                "Property Inspector filtering must include custom-property source labels.");
            filter.Text = "missing custom value";
            filter.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));
            Assert(!panel.IsVisible,
                "Property Inspector must hide the custom summary when no custom property matches its filter.");
            filter.Text = string.Empty;
            filter.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));
            var selected = editor.Canvas.Elements.Single();
            selected.IsLocked = true;
            Assert(panel.IsVisible && !items.IsEnabled && !editButton.IsEnabled,
                "A locked custom control must keep its summary visible while disabling custom-property editing.");
            selected.IsLocked = false;
            Assert(items.IsEnabled && editButton.IsEnabled,
                "Unlocking the custom control must re-enable its Inspector edit actions.");
        }
        finally
        {
            window.DataContext = null;
            window.Close();
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
