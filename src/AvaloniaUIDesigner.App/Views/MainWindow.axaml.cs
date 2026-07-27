using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Designer.Services;
using AvaloniaUIDesigner.App.Models;
using AvaloniaUIDesigner.App.ViewModels;

namespace AvaloniaUIDesigner.App.Views;

public partial class MainWindow : Window
{
    private enum DragMode { None, Move, N, S, E, W, NE, NW, SE, SW }
    private enum UnsavedChoice { Save, Discard, Cancel }
    private sealed record ComponentPackExportOptions(string PackName, string DisplayName, string NamePrefix);
    private sealed record ColorResourceApplicationOptions(string ResourceName, string PropertyName);
    private sealed record GridDefinitionOptions(string RowDefinitions, string ColumnDefinitions, bool ShowGridLines);
    private sealed record GridCellAssignmentOptions(
        string ParentName,
        int Row,
        int Column,
        int RowSpan,
        int ColumnSpan);
    private sealed record StackPanelAssignmentOptions(
        string ParentName,
        int ItemIndex,
        double ItemSize);
    private sealed record DockPanelAssignmentOptions(
        string ParentName,
        int ItemIndex,
        DesignerDockSide Dock,
        double ItemSize,
        bool LastChildFill);
    private sealed record WrapPanelAssignmentOptions(string ParentName, int ItemIndex);
    private sealed record UniformGridAssignmentOptions(string ParentName, int ItemIndex);
    private sealed record CanvasAssignmentOptions(
        string ParentName,
        int ItemIndex,
        double Left,
        double Top);
    private sealed record TabControlAssignmentOptions(string ParentName, int TabIndex);
    private sealed record SplitViewAssignmentOptions(string ParentName, DesignerSplitViewSlot Slot);
    private sealed record SplitViewSlotChoice(DesignerSplitViewSlot Slot, string? ChildName)
    {
        public override string ToString()
            => string.IsNullOrWhiteSpace(ChildName) ? Slot.ToString() : $"{Slot} ({ChildName})";
    }
    private sealed record ContentAssignmentOptions(string ParentName);

    private const double HandleHalf = 5;
    private const double MinSize = 10;
    private const double MarqueeThreshold = 3;
    private const double SmartSnapThreshold = 6;
    private const string ToolboxDragDataFormat = "AvaloniaUIDesigner.ToolboxItem";

    private DragMode _dragMode = DragMode.None;
    private Point _dragStart;
    private double _origX, _origY, _origW, _origH;
    private DesignElement? _dragTarget;
    private readonly System.Collections.Generic.Dictionary<DesignElement, Point> _dragOrigins = new();
    private bool _isMarqueeSelecting;
    private bool _marqueeAdditive;
    private Point _marqueeStart;
    private ToolboxItem? _pendingToolboxDragItem;
    private Point _toolboxDragStart;

    private CanvasViewModel? _boundCanvas;
    private DesignElement? _boundElement;
    private Control? _boundVisual;
    private MainWindowViewModel? _boundVm;

    private readonly DispatcherTimer _propertyEditTimer;
    private bool _hasPendingPropertyEdit;
    private bool _hasPendingLayoutEdit;
    private bool _allowCloseWithoutPrompt;

    public MainWindow()
    {
        InitializeComponent();
        _propertyEditTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(450)
        };
        _propertyEditTimer.Tick += OnPropertyEditTimerTick;

        DataContextChanged += OnDataContextChanged;
        OnDataContextChanged(this, EventArgs.Empty);
    }

    private MainWindowViewModel? Vm => DataContext as MainWindowViewModel;

    private async void OnOpenMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await HandleOpenCommandAsync();
    }

    private async void OnLoadComponentPackMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load Component Pack",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Component Pack JSON") { Patterns = ["*.json"] }
            ]
        });

        if (files.Count == 0)
        {
            return;
        }

        await using var stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        if (!Vm.TryLoadComponentPack(json, out var result))
        {
            Vm.StatusText = $"Could not load component pack: {result}";
        }
    }

    private async void OnExportSelectedComponentPackMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedComponentPackDefaults(out var packName, out var displayName, out var namePrefix))
        {
            return;
        }

        var options = await ShowComponentPackExportDialogAsync(packName, displayName, namePrefix);
        if (options is null)
        {
            return;
        }

        if (!Vm.TryExportSelectedComponentPack(
                options.PackName,
                options.DisplayName,
                options.NamePrefix,
                out var json,
                out var error))
        {
            Vm.StatusText = $"Could not export component pack: {error}";
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Component Pack",
            SuggestedFileName = $"{options.NamePrefix}.component-pack.json",
            DefaultExtension = "json",
            FileTypeChoices =
            [
                new FilePickerFileType("Component Pack JSON") { Patterns = ["*.json"] }
            ]
        });

        if (file is null)
        {
            return;
        }

        try
        {
            var localPath = file.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(localPath))
            {
                await AtomicFileWriter.WriteAllTextAsync(localPath, json);
            }
            else
            {
                await using var stream = await file.OpenWriteAsync();
                stream.SetLength(0);
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync(json);
                await writer.FlushAsync();
            }

            Vm.StatusText = $"Exported {options.DisplayName} component pack to {file.Name}.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Vm.StatusText = $"Could not export {file.Name}: {exception.Message}";
        }
    }

    private async Task HandleOpenCommandAsync()
    {
        if (Vm is null)
        {
            return;
        }

        FlushPendingPropertyHistory();
        if (!await EnsureCanContinueWithUnsavedChangesAsync())
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open AXAML",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("AXAML") { Patterns = ["*.axaml", "*.xaml"] }
            ]
        });

        if (files.Count == 0)
        {
            return;
        }

        await OpenStorageFileAsync(files[0]);
    }

    private async void OnSaveMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _ = await SaveDocumentAsync(forceSaveAs: false);
    }

    private async void OnSaveAsMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _ = await SaveDocumentAsync(forceSaveAs: true);
    }

    private async void OnCopyAxamlMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        FlushPendingPropertyHistory();
        await CopyAxamlToClipboardAsync(Vm.ExportFullAxaml(), "Copied Window AXAML to clipboard.");
    }

    private async void OnCopyUserControlAxamlMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        FlushPendingPropertyHistory();
        await CopyAxamlToClipboardAsync(Vm.ExportUserControlAxaml(), "Copied UserControl AXAML to clipboard.");
    }

    private async void OnExportUserControlAxamlMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        FlushPendingPropertyHistory();
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export UserControl AXAML",
            SuggestedFileName = "DesignView.axaml",
            DefaultExtension = "axaml",
            FileTypeChoices =
            [
                new FilePickerFileType("AXAML") { Patterns = ["*.axaml"] }
            ]
        });

        if (file is null)
        {
            return;
        }

        try
        {
            var axaml = Vm.ExportUserControlAxaml();
            var localPath = file.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(localPath))
            {
                await AtomicFileWriter.WriteAllTextAsync(localPath, axaml);
            }
            else
            {
                await using var stream = await file.OpenWriteAsync();
                stream.SetLength(0);
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync(axaml);
                await writer.FlushAsync();
            }

            Vm.StatusText = $"Exported UserControl AXAML to {file.Name}.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Vm.StatusText = $"Could not export {file.Name}: {exception.Message}";
        }
    }

    private async Task CopyAxamlToClipboardAsync(string axaml, string successStatus)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            if (Vm is not null)
            {
                Vm.StatusText = "Clipboard is unavailable.";
            }

            return;
        }

        await clipboard.SetTextAsync(axaml);
        if (Vm is not null)
        {
            Vm.StatusText = successStatus;
        }
    }

    private void OnValidateAxamlMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is not null)
        {
            Vm.TryValidateCurrentAxaml(out var result);
            Vm.StatusText = result;
        }
    }

    private async void OnNewMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await HandleNewCommandAsync();
    }

    private async void OnTemplateMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string templateName })
        {
            return;
        }

        FlushPendingPropertyHistory();
        if (await EnsureCanContinueWithUnsavedChangesAsync())
        {
            Vm?.CreateDocumentFromTemplate(templateName);
        }
    }

    private async Task HandleNewCommandAsync()
    {
        FlushPendingPropertyHistory();
        if (!await EnsureCanContinueWithUnsavedChangesAsync())
        {
            return;
        }

        Vm?.NewDocument();
    }

    private async void OnExitMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await RequestCloseAsync();
    }

    private void OnUndoMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.Undo();
    }

    private void OnRedoMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.Redo();
    }

    private void OnSelectAllMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is not null)
        {
            Vm.SelectElements(Vm.Canvas.Elements);
        }
    }

    private void OnToggleLockMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.ToggleSelectedLock();

    private void OnOpacity100MenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetSelectionOpacity(1);

    private void OnOpacity75MenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetSelectionOpacity(0.75);

    private void OnOpacity50MenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetSelectionOpacity(0.5);

    private void OnTextSize14MenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.SetSelectedTextSize(14);
    private void OnTextSize18MenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.SetSelectedTextSize(18);
    private void OnTextSize24MenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.SetSelectedTextSize(24);
    private void OnTextColorInkMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.SetSelectedTextColor("#111827");
    private void OnTextColorBlueMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.SetSelectedTextColor("#2563EB");
    private void OnTextColorRedMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.SetSelectedTextColor("#DC2626");
    private void OnTextWeightRegularMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.SetSelectedTextWeight("Regular");
    private void OnTextWeightSemiboldMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.SetSelectedTextWeight("Semibold");
    private void OnTextWeightBoldMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.SetSelectedTextWeight("Bold");

    private async void OnEditAppearanceMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedAppearance(out var controlName, out var appearance))
        {
            return;
        }

        var updatedAppearance = await ShowAppearanceEditorDialogAsync(controlName, appearance);
        if (updatedAppearance is not null)
        {
            Vm.SetSelectedAppearance(updatedAppearance);
        }
    }

    private async void OnEditColorResourcesMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null)
        {
            return;
        }

        var updatedResources = await ShowTextEditorDialogAsync(
            "Edit Color Resources",
            Vm.GetColorResourceEditorText(),
            "Enter one SolidColorBrush per line using Key = Brush, for example PrimaryBrush = #2563EB.");
        if (updatedResources is not null)
        {
            Vm.SetColorResourcesFromText(updatedResources);
        }
    }

    private async void OnEditDocumentStylesMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null)
        {
            return;
        }

        var updatedStyles = await ShowTextEditorDialogAsync(
            "Edit Document Styles",
            Vm.GetDocumentStyleEditorText(),
            "Use [Control.class] or [Control.class:pseudo] sections with Property = Value setters. Example: [Button.primary:pointerover].");
        if (updatedStyles is not null)
        {
            Vm.SetDocumentStylesFromText(updatedStyles);
        }
    }

    private async void OnEditSelectedClassesMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedStyleClasses(out var controlName, out var classes))
        {
            return;
        }

        var updatedClasses = await ShowTextEditorDialogAsync(
            $"Edit Style Classes - {controlName}",
            classes,
            "Enter space-separated Avalonia style classes. Remove all text to clear the classes.",
            multiline: false);
        if (updatedClasses is not null)
        {
            Vm.SetSelectedStyleClassesFromText(updatedClasses);
        }
    }

    private void OnPreviewStateNormalMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetStylePreviewState(null);

    private void OnPreviewStatePointerOverMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetStylePreviewState("pointerover");

    private void OnPreviewStatePressedMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetStylePreviewState("pressed");

    private void OnPreviewStateDisabledMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetStylePreviewState("disabled");

    private void OnPreviewStateFocusMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetStylePreviewState("focus");

    private void OnPreviewStateFocusVisibleMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetStylePreviewState("focus-visible");

    private void OnPreviewStateCheckedMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetStylePreviewState("checked");

    private void OnPreviewStateExpandedMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetStylePreviewState("expanded");

    private void SetStylePreviewState(string? pseudoClass)
    {
        FlushPendingPropertyHistory();
        Vm?.SetSelectedStylePreviewState(pseudoClass);
    }

    private async void OnApplyColorResourceMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetColorResourceApplicationOptions(
                out var controlName,
                out var resourceNames,
                out var propertyNames))
        {
            return;
        }

        var options = await ShowColorResourceApplicationDialogAsync(controlName, resourceNames, propertyNames);
        if (options is not null)
        {
            Vm.ApplyColorResource(options.ResourceName, options.PropertyName);
        }
    }

    private void SetSelectionOpacity(double opacity)
    {
        FlushPendingPropertyHistory();
        Vm?.SetSelectedOpacity(opacity);
    }

    private void OnDeleteMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.RemoveSelectedElement();
    }

    private void OnCopyMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.CopySelectedElement();
    }

    private void OnCutMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.CutSelectedElement();
    }

    private void OnPasteMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.PasteElement();
    }

    private void OnDuplicateMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.DuplicateSelectedElement();
    }

    private async void OnRenameSelectedControlMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedElementName(out var controlName, out var elementName))
        {
            return;
        }

        var updatedName = await ShowTextEditorDialogAsync(
            $"Rename Control - {controlName}",
            elementName,
            "Enter a unique x:Name. Names must start with a letter or underscore and contain only letters, numbers, or underscores.",
            multiline: false);
        if (updatedName is not null)
        {
            Vm.RenameSelectedElement(updatedName);
        }
    }

    private async void OnEditItemsMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedItemsEditor(out var state))
        {
            return;
        }

        var updatedItems = await ShowItemsEditorDialogAsync(state);
        if (updatedItems is not null)
        {
            Vm.SetSelectedItems(updatedItems);
        }
    }

    private async void OnEditBindingsMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedBindings(out var state))
        {
            return;
        }

        var updatedBindings = await ShowBindingEditorDialogAsync(state);
        if (updatedBindings is not null)
        {
            Vm.SetSelectedBindings(updatedBindings);
        }
    }

    private async void OnEditLayoutPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedLayoutProperties(out var state))
        {
            return;
        }

        await ShowLayoutPropertiesDialogAsync(state);
    }

    private async void OnEditTypographyPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedTypographyProperties(out var state))
        {
            return;
        }

        await ShowTypographyPropertiesDialogAsync(state);
    }

    private async void OnEditTransformPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedTransformProperties(out var state))
        {
            return;
        }

        await ShowTransformPropertiesDialogAsync(state);
    }

    private async void OnEditAccessibilityPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedAccessibilityProperties(out var state))
        {
            return;
        }

        await ShowAccessibilityPropertiesDialogAsync(state);
    }

    private async void OnEditInteractionPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedInteractionProperties(out var state))
        {
            return;
        }

        await ShowInteractionPropertiesDialogAsync(state);
    }

    private async void OnEditEffectPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedEffectProperties(out var state))
        {
            return;
        }

        await ShowEffectPropertiesDialogAsync(state);
    }

    private async void OnEditRangePropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedRangeProperties(out var state))
        {
            return;
        }

        await ShowRangePropertiesDialogAsync(state);
    }

    private async void OnEditTextInputPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedTextInputProperties(out var state))
        {
            return;
        }

        await ShowTextInputPropertiesDialogAsync(state);
    }

    private async void OnEditSelectionPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedSelectionProperties(out var state))
        {
            return;
        }

        await ShowSelectionPropertiesDialogAsync(state);
    }

    private async void OnEditMaskedTextBoxPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedMaskedTextBoxProperties(out var state))
        {
            return;
        }

        await ShowMaskedTextBoxPropertiesDialogAsync(state);
    }

    private async void OnEditSelectableTextBlockPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedSelectableTextBlockProperties(out var state))
        {
            return;
        }

        await ShowSelectableTextBlockPropertiesDialogAsync(state);
    }

    private async void OnEditDateTimePropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedDateTimeProperties(out var state))
        {
            return;
        }

        await ShowDateTimePropertiesDialogAsync(state);
    }

    private async void OnEditColorPickerPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null || !Vm.TryGetSelectedColorPickerProperties(out var state))
        {
            return;
        }

        await ShowColorPickerPropertiesDialogAsync(state);
    }

    private async void OnEditAutoCompleteBoxPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedAutoCompleteBoxProperties(out var state))
        {
            return;
        }

        await ShowAutoCompleteBoxPropertiesDialogAsync(state);
    }

    private async void OnEditTogglePropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedToggleProperties(out var state))
        {
            return;
        }

        await ShowTogglePropertiesDialogAsync(state);
    }

    private async void OnEditContainerBehaviorPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null
            || !Vm.TryGetSelectedContainerBehaviorProperties(out var state))
        {
            return;
        }

        await ShowContainerBehaviorPropertiesDialogAsync(state);
    }

    private async void OnEditImagePropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedImageProperties(out var state))
        {
            return;
        }

        await ShowImagePropertiesDialogAsync(state);
    }

    private async void OnEditButtonPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedButtonProperties(out var state))
        {
            return;
        }

        await ShowButtonPropertiesDialogAsync(state);
    }

    private async void OnEditGridDefinitionsMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedGridDefinitions(out var state))
        {
            return;
        }

        var updated = await ShowGridDefinitionsDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedGridDefinitions(
                updated.RowDefinitions,
                updated.ColumnDefinitions,
                updated.ShowGridLines);
        }
    }

    private async void OnAssignToGridCellMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedGridCellAssignment(out var state))
        {
            return;
        }

        var updated = await ShowGridCellAssignmentDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedGridCellAssignment(
                updated.ParentName,
                updated.Row,
                updated.Column,
                updated.RowSpan,
                updated.ColumnSpan);
        }
    }

    private async void OnAssignToStackPanelMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedStackPanelAssignment(out var state))
        {
            return;
        }

        var updated = await ShowStackPanelAssignmentDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedStackPanelAssignment(
                updated.ParentName,
                updated.ItemIndex,
                updated.ItemSize);
        }
    }

    private void OnMoveStackPanelItemEarlierMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedStackPanelItem(-1);

    private void OnMoveStackPanelItemLaterMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedStackPanelItem(1);

    private async void OnAssignToDockPanelMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedDockPanelAssignment(out var state))
        {
            return;
        }

        var updated = await ShowDockPanelAssignmentDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedDockPanelAssignment(
                updated.ParentName,
                updated.ItemIndex,
                updated.Dock,
                updated.ItemSize,
                updated.LastChildFill);
        }
    }

    private void OnMoveDockPanelItemEarlierMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedDockPanelItem(-1);

    private void OnMoveDockPanelItemLaterMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedDockPanelItem(1);

    private async void OnAssignToWrapPanelMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedWrapPanelAssignment(out var state))
        {
            return;
        }

        var updated = await ShowWrapPanelAssignmentDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedWrapPanelAssignment(updated.ParentName, updated.ItemIndex);
        }
    }

    private void OnMoveWrapPanelItemEarlierMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedWrapPanelItem(-1);

    private void OnMoveWrapPanelItemLaterMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedWrapPanelItem(1);

    private async void OnAssignToUniformGridMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedUniformGridAssignment(out var state))
        {
            return;
        }

        var updated = await ShowUniformGridAssignmentDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedUniformGridAssignment(updated.ParentName, updated.ItemIndex);
        }
    }

    private void OnMoveUniformGridItemEarlierMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedUniformGridItem(-1);

    private void OnMoveUniformGridItemLaterMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedUniformGridItem(1);

    private async void OnAssignToCanvasMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedCanvasAssignment(out var state))
        {
            return;
        }

        var updated = await ShowCanvasAssignmentDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedCanvasAssignment(
                updated.ParentName,
                updated.ItemIndex,
                updated.Left,
                updated.Top);
        }
    }

    private void OnMoveCanvasItemEarlierMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedCanvasItem(-1);

    private void OnMoveCanvasItemLaterMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedCanvasItem(1);

    private async void OnAssignToTabControlMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedTabControlAssignment(out var state))
        {
            return;
        }

        var updated = await ShowTabControlAssignmentDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedTabControlAssignment(updated.ParentName, updated.TabIndex);
        }
    }

    private async void OnAssignToSplitViewMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedSplitViewAssignment(out var state))
        {
            return;
        }

        var updated = await ShowSplitViewAssignmentDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedSplitViewAssignment(updated.ParentName, updated.Slot);
        }
    }

    private void OnRemoveFromContainerMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.RemoveSelectedFromContainer();
    }

    private async void OnAssignAsContainerContentMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedContentAssignment(out var state))
        {
            return;
        }

        var updated = await ShowContentAssignmentDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedContentAssignment(updated.ParentName);
        }
    }

    private async void OnChooseImageMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose image",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Image files")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp"]
                }
            ]
        });

        if (files.Count == 0 || !files[0].Path.IsFile)
        {
            return;
        }

        Vm.TrySetSelectedImageSource(files[0].Path.AbsoluteUri);
    }

    private async void OnEditPathDataMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedPathData(out var controlName, out var data))
        {
            return;
        }

        var updated = await ShowTextEditorDialogAsync(
            $"Edit Path Data - {controlName}",
            data,
            "Enter Avalonia geometry mini-language, for example M 10,70 L 50,10 90,70 Z.");
        if (updated is not null)
        {
            Vm.SetSelectedPathData(updated);
        }
    }

    private async void OnEditExpanderContentMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedExpanderContent(out var controlName, out var content))
        {
            return;
        }

        var updatedContent = await ShowTextEditorDialogAsync(
            $"Edit Content - {controlName}",
            content,
            "Enter the text shown inside the selected Expander, ScrollViewer, or Border.");
        if (updatedContent is not null)
        {
            Vm.SetSelectedExpanderContent(updatedContent);
        }
    }

    private async void OnEditToolTipMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedToolTip(out var controlName, out var toolTip))
        {
            return;
        }

        var updatedToolTip = await ShowTextEditorDialogAsync(
            $"Edit Tooltip - {controlName}",
            toolTip,
            "Enter a short hint shown when the pointer rests over this control.");
        if (updatedToolTip is not null)
        {
            Vm.SetSelectedToolTip(updatedToolTip);
        }
    }

    private async void OnEditButtonClickHandlerMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedButtonClickHandler(out var buttonName, out var handlerName))
        {
            return;
        }

        var updatedHandler = await ShowTextEditorDialogAsync(
            $"Edit Click Handler - {buttonName}",
            handlerName,
            "Enter a handler method name, for example SaveButton_Click. Leave blank to remove the handler.");
        if (updatedHandler is not null)
        {
            Vm.SetSelectedButtonClickHandler(updatedHandler);
        }
    }

    private async void OnEditAccessibleNameMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedAutomationName(out var controlName, out var automationName))
        {
            return;
        }

        var updatedName = await ShowTextEditorDialogAsync(
            $"Edit Accessible Name - {controlName}",
            automationName,
            "Enter the label announced by screen readers and used by UI automation.");
        if (updatedName is not null)
        {
            Vm.SetSelectedAutomationName(updatedName);
        }
    }

    private void OnToggleEnabledMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.ToggleSelectedEnabledState();
    }

    private void OnToggleVisibilityMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.ToggleSelectedVisibility();
    }

    private void OnToggleTextBoxMultilineMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.ToggleSelectedTextBoxMultiline();
    }

    private async void OnEditLabelTargetMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedLabelTarget(out var labelName, out var targetName))
        {
            return;
        }

        var updatedTargetName = await ShowTextEditorDialogAsync(
            $"Edit Label Target - {labelName}",
            targetName,
            "Enter another control's name, or leave blank to clear the focus association.");
        if (updatedTargetName is not null)
        {
            Vm.SetSelectedLabelTarget(updatedTargetName);
        }
    }

    private async void OnEditTabOrderMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedTabIndex(out var controlName, out var tabIndex))
        {
            return;
        }

        var updatedTabIndex = await ShowTextEditorDialogAsync(
            $"Edit Tab Order - {controlName}",
            tabIndex.ToString(CultureInfo.InvariantCulture),
            "Enter an integer. Lower values receive keyboard focus first.");
        if (updatedTabIndex is null)
        {
            return;
        }

        if (!int.TryParse(updatedTabIndex.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedTabIndex))
        {
            Vm.StatusText = "Tab order must be a whole number.";
            return;
        }

        Vm.SetSelectedTabIndex(parsedTabIndex);
    }

    private void OnToggleTabStopMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.ToggleSelectedTabStop();
    }

    private void OnBringToFrontMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => MoveSelectedElementsInLayerOrder(MainWindowViewModel.LayerOrderAction.BringToFront);

    private void OnSendToBackMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => MoveSelectedElementsInLayerOrder(MainWindowViewModel.LayerOrderAction.SendToBack);

    private void OnBringForwardMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => MoveSelectedElementsInLayerOrder(MainWindowViewModel.LayerOrderAction.BringForward);

    private void OnSendBackwardMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => MoveSelectedElementsInLayerOrder(MainWindowViewModel.LayerOrderAction.SendBackward);

    private void MoveSelectedElementsInLayerOrder(MainWindowViewModel.LayerOrderAction action)
    {
        FlushPendingPropertyHistory();
        Vm?.MoveSelectedElementsInLayerOrder(action);
    }

    private void OnPreviewMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        FlushPendingPropertyHistory();
        var preview = new PreviewWindow(Vm.CreatePreviewDocument());
        preview.Show(this);
        Vm.StatusText = "Opened runtime preview.";
    }

    private async void OnEditAxamlSourceMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null)
        {
            return;
        }

        await ShowAxamlSourceEditorDialogAsync(Vm.ExportFullAxaml());
    }

    private async void OnEditRootPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null)
        {
            return;
        }

        await ShowRootPropertiesDialogAsync(Vm.GetRootEditorState());
    }

    private async void OnEditSampleDataMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null)
        {
            return;
        }

        await ShowSampleDataEditorDialogAsync(Vm.GetSampleDataEditorText());
    }

    private void OnZoomInMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Vm?.Canvas.ZoomIn();
        UpdateZoomStatus();
    }

    private void OnZoomOutMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Vm?.Canvas.ZoomOut();
        UpdateZoomStatus();
    }

    private void OnResetZoomMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Vm?.Canvas.ResetZoom();
        UpdateZoomStatus();
    }

    private void OnFitToViewMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Vm?.Canvas.FitToViewport(DesignViewport.Bounds.Width, DesignViewport.Bounds.Height);
        UpdateZoomStatus();
    }

    private void OnGridSize4MenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.SetCanvasGridSize(4);

    private void OnGridSize8MenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.SetCanvasGridSize(8);

    private void OnGridSize16MenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.SetCanvasGridSize(16);

    private void OnShowGridMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.SetCanvasGridVisibility(!Vm.Canvas.IsGridVisible);

    private void OnSnapToGridMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.SetCanvasSnapToGrid(!Vm.Canvas.SnapToGrid);

    private void OnDesktopArtboardMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetArtboard(1280, 800);

    private void OnTabletArtboardMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetArtboard(1024, 768);

    private void OnMobileArtboardMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetArtboard(390, 844);

    private void OnRotateArtboardMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        if (Math.Abs(Vm.Canvas.ArtboardWidth - Vm.Canvas.ArtboardHeight) < double.Epsilon)
        {
            Vm.StatusText = "The square artboard orientation is unchanged.";
            return;
        }

        SetArtboard(Vm.Canvas.ArtboardHeight, Vm.Canvas.ArtboardWidth);
        Vm.StatusText = $"Rotated artboard: {Vm.Canvas.ArtboardWidth:0} x {Vm.Canvas.ArtboardHeight:0}";
    }

    private void OnWhiteArtboardBackgroundMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetArtboardBackground("#FFFFFF");

    private void OnGrayArtboardBackgroundMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetArtboardBackground("#F1F5F9");

    private void OnInkArtboardBackgroundMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetArtboardBackground("#1E293B");

    private void SetArtboard(double width, double height)
    {
        if (Vm is null)
        {
            return;
        }

        Vm.BeginCanvasMutation(MainWindowViewModel.HistoryActionType.TransformElement, "Updated artboard size.");
        Vm.Canvas.SetArtboard(width, height);
        Vm.CommitCanvasMutation();
        Vm.StatusText = $"Artboard: {Vm.Canvas.ArtboardWidth:0} x {Vm.Canvas.ArtboardHeight:0}";
    }

    private void SetArtboardBackground(string background)
    {
        if (Vm is null)
        {
            return;
        }

        Vm.BeginCanvasMutation(MainWindowViewModel.HistoryActionType.TransformElement, "Updated artboard background.");
        Vm.Canvas.SetArtboard(Vm.Canvas.ArtboardWidth, Vm.Canvas.ArtboardHeight, background);
        Vm.CommitCanvasMutation();
        Vm.StatusText = "Updated artboard background.";
    }

    private void UpdateZoomStatus()
    {
        if (Vm is not null)
        {
            Vm.StatusText = $"Zoom: {Vm.Canvas.ZoomPercentage}";
        }
    }

    private void OnAlignLeftMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.AlignLeft);

    private void OnAlignCenterMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.AlignCenter);

    private void OnAlignRightMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.AlignRight);

    private void OnAlignTopMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.AlignTop);

    private void OnAlignMiddleMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.AlignMiddle);

    private void OnAlignBottomMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.AlignBottom);

    private void OnDistributeHorizontallyMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.DistributeHorizontally);

    private void OnDistributeVerticallyMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.DistributeVertically);

    private void OnMakeSameWidthMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.MakeSameWidth);

    private void OnMakeSameHeightMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.MakeSameHeight);

    private void OnMakeSameSizeMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.MakeSameSize);

    private void OnCenterHorizontallyOnArtboardMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => CenterSelectedElementsOnArtboard(horizontally: true, vertically: false);

    private void OnCenterVerticallyOnArtboardMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => CenterSelectedElementsOnArtboard(horizontally: false, vertically: true);

    private void OnCenterOnArtboardMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => CenterSelectedElementsOnArtboard(horizontally: true, vertically: true);

    private void ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction action)
    {
        FlushPendingPropertyHistory();
        Vm?.ArrangeSelectedElements(action);
    }

    private void CenterSelectedElementsOnArtboard(bool horizontally, bool vertically)
    {
        FlushPendingPropertyHistory();
        Vm?.CenterSelectedElementsOnArtboard(horizontally, vertically);
    }

    private async void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        // Keep text editing inside the PropertyGrid native to the focused editor.
        if (e.Source is TextBox)
        {
            return;
        }

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        if (e.Key == Key.Escape)
        {
            _isMarqueeSelecting = false;
            MarqueeRectangle.IsVisible = false;
            Vm.Toolbox.SelectedItem = null;
            Vm.SelectElements(Array.Empty<DesignElement>());
            Vm.StatusText = "Selection tool active.";
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.S)
        {
            _ = await SaveDocumentAsync(forceSaveAs: shift);
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.O)
        {
            await HandleOpenCommandAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.F)
        {
            ObjectTreeSearch.Focus();
            ObjectTreeSearch.SelectAll();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.D0)
        {
            Vm.Canvas.ResetZoom();
            UpdateZoomStatus();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.N)
        {
            await HandleNewCommandAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.Z)
        {
            FlushPendingPropertyHistory();
            Vm.Undo();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.Y)
        {
            FlushPendingPropertyHistory();
            Vm.Redo();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.A)
        {
            Vm.SelectElements(Vm.Canvas.Elements);
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.D)
        {
            FlushPendingPropertyHistory();
            Vm.DuplicateSelectedElement();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.C)
        {
            FlushPendingPropertyHistory();
            Vm.CopySelectedElement();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.X)
        {
            FlushPendingPropertyHistory();
            Vm.CutSelectedElement();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.V)
        {
            FlushPendingPropertyHistory();
            Vm.PasteElement();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.R)
        {
            OnPreviewMenuClicked(this, e);
            e.Handled = true;
            return;
        }

        var nudgeDistance = shift ? 10 : 1;
        if (Vm.Canvas.IsSelectionActive && e.Key is (Key.Left or Key.Right or Key.Up or Key.Down))
        {
            FlushPendingPropertyHistory();

            switch (e.Key)
            {
                case Key.Left:
                    Vm.MoveSelectedElement(-nudgeDistance, 0);
                    break;
                case Key.Right:
                    Vm.MoveSelectedElement(nudgeDistance, 0);
                    break;
                case Key.Up:
                    Vm.MoveSelectedElement(0, -nudgeDistance);
                    break;
                case Key.Down:
                    Vm.MoveSelectedElement(0, nudgeDistance);
                    break;
            }

            e.Handled = true;
            return;
        }

        if (e.Key is Key.Delete or Key.Back)
        {
            FlushPendingPropertyHistory();
            Vm.RemoveSelectedElement();
            e.Handled = true;
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_boundCanvas is not null)
        {
            _boundCanvas.PropertyChanged -= OnCanvasPropertyChanged;
        }

        if (_boundVm is not null)
        {
            _boundVm.RecentFiles.CollectionChanged -= OnRecentFilesChanged;
        }

        _boundVm = Vm;
        _boundCanvas = _boundVm?.Canvas;

        if (_boundCanvas is not null)
        {
            _boundCanvas.PropertyChanged += OnCanvasPropertyChanged;
        }

        if (_boundVm is not null)
        {
            _boundVm.RecentFiles.CollectionChanged += OnRecentFilesChanged;
        }

        RebuildRecentFilesMenu();
        RebindSelection();
    }

    private void OnRecentFilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildRecentFilesMenu();
    }

    private void RebuildRecentFilesMenu()
    {
        if (_boundVm is null || _boundVm.RecentFiles.Count == 0)
        {
            OpenRecentMenu.IsEnabled = false;
            OpenRecentMenu.ItemsSource = null;
            return;
        }

        OpenRecentMenu.IsEnabled = true;
        var items = new System.Collections.Generic.List<MenuItem>();

        for (var i = 0; i < _boundVm.RecentFiles.Count; i++)
        {
            var path = _boundVm.RecentFiles[i];
            var item = new MenuItem
            {
                Header = $"{i + 1}. {path}",
                Tag = path,
            };
            item.Click += OnOpenRecentMenuItemClicked;
            items.Add(item);
        }

        OpenRecentMenu.ItemsSource = items;
    }

    private async void OnOpenRecentMenuItemClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null || sender is not MenuItem { Tag: string path })
        {
            return;
        }

        FlushPendingPropertyHistory();
        if (!await EnsureCanContinueWithUnsavedChangesAsync())
        {
            return;
        }

        if (!File.Exists(path))
        {
            Vm.RemoveRecentFile(path);
            Vm.StatusText = $"Recent file not found: {path}";
            return;
        }

        var content = await File.ReadAllTextAsync(path);
        if (!Vm.TryImportDraftAxaml(content, out var error, out var warning))
        {
            Vm.StatusText = $"Open failed: {error}";
            return;
        }

        Vm.MarkDocumentLoaded(path);
        Vm.StatusText = BuildOpenStatus(System.IO.Path.GetFileName(path), warning);
    }

    private void OnCanvasPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CanvasViewModel.SelectedElement))
        {
            RebindSelection();
        }
    }

    private void RebindSelection()
    {
        FlushPendingPropertyHistory();
        FlushPendingLayoutHistory();

        if (_boundVisual is not null)
        {
            _boundVisual.PropertyChanged -= OnSelectedVisualPropertyChanged;
        }

        if (_boundElement is not null)
        {
            _boundElement.PropertyChanged -= OnElementPropertyChanged;
        }

        _boundElement = _boundCanvas?.SelectedElement;
        _boundVisual = _boundElement?.Visual;

        if (_boundElement is not null)
        {
            _boundElement.PropertyChanged += OnElementPropertyChanged;
        }

        if (_boundVisual is not null)
        {
            _boundVisual.PropertyChanged += OnSelectedVisualPropertyChanged;
        }

        PropGrid.Content = _boundElement?.Visual;
        UpdateSelectionEditability();
        UpdateLayoutEditors();
        UpdateElementNameEditor();
        UpdateHandlePositions();
    }

    private void OnSelectedVisualPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not Control control
            || DesignerStyleApplicationMetadata.IsProgrammaticUpdate(control)
            || !IsUndoTrackedVisualProperty(control, e.Property.Name))
        {
            return;
        }

        if (DesignerStyleRuntime.IsSupportedProperty(control.GetType().Name, e.Property.Name))
        {
            DesignerStyleApplicationMetadata.ClearApplied(control, e.Property.Name);
            if (DesignerStyleRuntime.IsBrushProperty(e.Property.Name))
            {
                DesignerResourceReferenceMetadata.SetReference(control, e.Property.Name, null);
            }
        }

        if (e.Property.Name is "IsEnabled" or "IsChecked" or "IsExpanded")
        {
            Vm?.Canvas.RefreshDocumentStyles(control);
        }

        if (control is Grid
            && e.Property.Name is "RowDefinitions" or "ColumnDefinitions"
            && _boundElement is not null)
        {
            Vm?.Canvas.ReflowGridChildren(_boundElement);
        }

        if (control is StackPanel
            && e.Property.Name is "Orientation" or "Spacing"
            && _boundElement is not null)
        {
            Vm?.Canvas.ReflowContainerChildren(_boundElement);
        }

        if (control is DockPanel
            && e.Property.Name == "LastChildFill"
            && _boundElement is not null)
        {
            Vm?.Canvas.ReflowContainerChildren(_boundElement);
        }

        if (control is WrapPanel
            && e.Property.Name is "Orientation" or "ItemWidth" or "ItemHeight"
                or "ItemSpacing" or "LineSpacing" or "ItemsAlignment"
            && _boundElement is not null)
        {
            Vm?.Canvas.ReflowContainerChildren(_boundElement);
        }

        if (control is UniformGrid
            && e.Property.Name is "Rows" or "Columns" or "FirstColumn" or "RowSpacing" or "ColumnSpacing"
            && _boundElement is not null)
        {
            Vm?.Canvas.ReflowContainerChildren(_boundElement);
        }

        if (control is TabControl
            && e.Property.Name == "SelectedIndex"
            && _boundElement is not null)
        {
            Vm?.Canvas.ReflowContainerChildren(_boundElement);
        }

        if (control is SplitView
            && e.Property.Name is "DisplayMode" or "IsPaneOpen" or "OpenPaneLength"
                or "CompactPaneLength" or "PanePlacement"
            && _boundElement is not null)
        {
            Vm?.Canvas.ReflowContainerChildren(_boundElement);
        }

        if (control is Border
            && e.Property.Name == "BorderThickness"
            && _boundElement is not null)
        {
            Vm?.Canvas.ReflowContainerChildren(_boundElement);
        }

        if (!_hasPendingPropertyEdit)
        {
            Vm?.BeginCanvasMutation(MainWindowViewModel.HistoryActionType.EditProperty, "Updated control properties.");
            _hasPendingPropertyEdit = true;
        }

        _propertyEditTimer.Stop();
        _propertyEditTimer.Start();
    }

    private void OnPropertyEditTimerTick(object? sender, EventArgs e)
    {
        _propertyEditTimer.Stop();
        FlushPendingPropertyHistory();
    }

    private void FlushPendingPropertyHistory()
    {
        if (!_hasPendingPropertyEdit)
        {
            return;
        }

        _hasPendingPropertyEdit = false;
        Vm?.CommitCanvasMutation();
    }

    private static bool IsUndoTrackedVisualProperty(Control control, string propertyName)
    {
        if (DesignerButtonRuntime.IsSupportedProperty(
                control.GetType().Name,
                propertyName))
        {
            return true;
        }

        if (DesignerImageRuntime.IsSupportedProperty(
                control.GetType().Name,
                propertyName))
        {
            return true;
        }

        if (DesignerContainerBehaviorRuntime.IsSupportedProperty(
                control.GetType().Name,
                propertyName))
        {
            return true;
        }

        if (DesignerToggleRuntime.IsSupportedProperty(control.GetType().Name, propertyName))
        {
            return true;
        }

        if (DesignerDateTimeRuntime.IsSupportedProperty(control.GetType().Name, propertyName))
        {
            return true;
        }

        if (propertyName is "Opacity" or "IsEnabled" or "IsVisible" or "TabIndex" or "IsTabStop")
        {
            return true;
        }

        if (control is Avalonia.Controls.Primitives.TemplatedControl
            && (propertyName is "Background" or "Foreground" or "BorderBrush" or "BorderThickness"
                or "CornerRadius" or "FontSize" or "FontWeight"))
        {
            return true;
        }

        if (control is Button)
        {
            return propertyName == "Content";
        }

        if (control is TextBox)
        {
            return propertyName is "Text" or "Watermark" or "PasswordChar" or "RevealPassword" or "AcceptsReturn" or "TextWrapping";
        }

        if (control is TextBlock)
        {
            return propertyName is "Text" or "FontSize" or "FontWeight" or "Background" or "Foreground";
        }

        if (control is Label)
        {
            return propertyName == "Content";
        }

        if (control is CheckBox or ToggleSwitch)
        {
            return propertyName is "Content" or "IsChecked";
        }

        if (control is Avalonia.Controls.Primitives.ToggleButton)
        {
            return propertyName is "Content" or "IsChecked";
        }

        if (control is RadioButton)
        {
            return propertyName is "Content" or "IsChecked" or "GroupName";
        }

        if (control is ComboBox or ListBox)
        {
            return propertyName == "SelectedIndex";
        }

        if (control is Slider or ProgressBar)
        {
            return propertyName is "Minimum" or "Maximum" or "Value";
        }

        if (control is DatePicker)
        {
            return propertyName == "SelectedDate";
        }

        if (control is CalendarDatePicker)
        {
            return propertyName is "SelectedDate" or "Watermark";
        }

        if (control is TimePicker)
        {
            return propertyName == "SelectedTime";
        }

        if (control is NumericUpDown)
        {
            return propertyName is "Minimum" or "Maximum" or "Increment" or "Value";
        }

        if (control is TabControl)
        {
            return propertyName == "SelectedIndex";
        }

        if (control is SplitView)
        {
            return propertyName is "DisplayMode" or "IsPaneOpen" or "OpenPaneLength"
                or "CompactPaneLength" or "PanePlacement" or "PaneBackground"
                or "UseLightDismissOverlayMode";
        }

        if (control is Expander)
        {
            return propertyName is "Header" or "IsExpanded";
        }

        if (control is Border)
        {
            return propertyName is "Background" or "BorderBrush" or "BorderThickness" or "CornerRadius";
        }

        if (control is StackPanel)
        {
            return propertyName is "Orientation" or "Spacing";
        }

        if (control is DockPanel)
        {
            return propertyName == "LastChildFill";
        }

        if (control is WrapPanel)
        {
            return propertyName is "Orientation" or "ItemWidth" or "ItemHeight"
                or "ItemSpacing" or "LineSpacing" or "ItemsAlignment";
        }

        if (control is UniformGrid)
        {
            return propertyName is "Rows" or "Columns" or "FirstColumn" or "RowSpacing" or "ColumnSpacing";
        }

        if (control is Canvas)
        {
            return propertyName == "Background";
        }

        if (control is Grid)
        {
            return propertyName is "RowDefinitions" or "ColumnDefinitions" or "ShowGridLines";
        }

        return false;
    }

    private void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DesignElement.DisplayName))
        {
            UpdateElementNameEditor();
        }

        if (e.PropertyName == nameof(DesignElement.IsLocked))
        {
            UpdateSelectionEditability();
        }

        if (e.PropertyName is nameof(DesignElement.X)
            or nameof(DesignElement.Y)
            or nameof(DesignElement.Width)
            or nameof(DesignElement.Height))
        {
            UpdateLayoutEditors();
            UpdateHandlePositions();
        }
    }

    private void OnLayoutEditorGotFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_boundElement?.IsLocked == true)
        {
            Vm?.StatusText = "Selected control is locked.";
            return;
        }

        if (_boundElement is null || _hasPendingLayoutEdit)
        {
            return;
        }

        FlushPendingPropertyHistory();
        Vm?.BeginCanvasMutation(MainWindowViewModel.HistoryActionType.TransformElement, "Updated element layout values.");
        _hasPendingLayoutEdit = true;
    }

    private void OnElementNameEditorLostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_boundElement is null || _boundElement.IsLocked || sender is not TextBox editor)
        {
            UpdateElementNameEditor();
            return;
        }

        var name = editor.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            Vm?.TryRenameElement(_boundElement, name);
        }

        UpdateElementNameEditor();
    }

    private void UpdateElementNameEditor()
    {
        ElementNameEditor.Text = _boundElement?.DisplayName ?? string.Empty;
    }

    private void UpdateSelectionEditability()
    {
        var canEdit = _boundElement is { IsLocked: false };
        var canEditLayout = canEdit && _boundElement is not { IsContainerChild: true };

        PropGrid.IsEnabled = canEdit;
        ElementNameEditor.IsEnabled = canEdit;
        LayoutXEditor.IsEnabled = canEditLayout;
        LayoutYEditor.IsEnabled = canEditLayout;
        LayoutWidthEditor.IsEnabled = canEditLayout;
        LayoutHeightEditor.IsEnabled = canEditLayout;

        HandleNW.IsVisible = canEditLayout;
        HandleN.IsVisible = canEditLayout;
        HandleNE.IsVisible = canEditLayout;
        HandleE.IsVisible = canEditLayout;
        HandleSE.IsVisible = canEditLayout;
        HandleS.IsVisible = canEditLayout;
        HandleSW.IsVisible = canEditLayout;
        HandleW.IsVisible = canEditLayout;
    }

    private void OnLayoutEditorLostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_boundElement?.IsLocked == true)
        {
            UpdateLayoutEditors();
            FlushPendingLayoutHistory();
            return;
        }

        if (sender is not TextBox editor || _boundElement is null)
        {
            FlushPendingLayoutHistory();
            return;
        }

        if (double.TryParse(editor.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            switch (editor.Name)
            {
                case "LayoutXEditor":
                    _boundElement.X = Math.Max(0, value);
                    break;
                case "LayoutYEditor":
                    _boundElement.Y = Math.Max(0, value);
                    break;
                case "LayoutWidthEditor":
                    _boundElement.Width = Math.Max(MinSize, value);
                    break;
                case "LayoutHeightEditor":
                    _boundElement.Height = Math.Max(MinSize, value);
                    break;
            }
        }

        UpdateLayoutEditors();
        FlushPendingLayoutHistory();
    }

    private void UpdateLayoutEditors()
    {
        if (_boundElement is null)
        {
            LayoutXEditor.Text = string.Empty;
            LayoutYEditor.Text = string.Empty;
            LayoutWidthEditor.Text = string.Empty;
            LayoutHeightEditor.Text = string.Empty;
            return;
        }

        LayoutXEditor.Text = _boundElement.X.ToString("0.###", CultureInfo.InvariantCulture);
        LayoutYEditor.Text = _boundElement.Y.ToString("0.###", CultureInfo.InvariantCulture);
        LayoutWidthEditor.Text = _boundElement.Width.ToString("0.###", CultureInfo.InvariantCulture);
        LayoutHeightEditor.Text = _boundElement.Height.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private void FlushPendingLayoutHistory()
    {
        if (!_hasPendingLayoutEdit)
        {
            return;
        }

        _hasPendingLayoutEdit = false;
        Vm?.CommitCanvasMutation();
    }

    private void UpdateHandlePositions()
    {
        var el = _boundElement;
        if (el is null)
        {
            return;
        }

        var left = el.X;
        var top = el.Y;
        var right = el.X + el.Width;
        var bottom = el.Y + el.Height;
        var midX = el.X + el.Width / 2;
        var midY = el.Y + el.Height / 2;

        Place(HandleNW, left, top);
        Place(HandleN, midX, top);
        Place(HandleNE, right, top);
        Place(HandleE, right, midY);
        Place(HandleSE, right, bottom);
        Place(HandleS, midX, bottom);
        Place(HandleSW, left, bottom);
        Place(HandleW, left, midY);
    }

    private static void Place(Rectangle rectangle, double cx, double cy)
    {
        Canvas.SetLeft(rectangle, cx - HandleHalf);
        Canvas.SetTop(rectangle, cy - HandleHalf);
    }

    private void OnToolboxItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: ToolboxItem item } control
            || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _pendingToolboxDragItem = item;
        _toolboxDragStart = e.GetPosition(this);
        e.Pointer.Capture(control);
    }

    private async void OnToolboxItemPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pendingToolboxDragItem is not { } item)
        {
            return;
        }

        var point = e.GetPosition(this);
        if (Math.Abs(point.X - _toolboxDragStart.X) < MarqueeThreshold
            && Math.Abs(point.Y - _toolboxDragStart.Y) < MarqueeThreshold)
        {
            return;
        }

        _pendingToolboxDragItem = null;
        e.Pointer.Capture(null);
        var data = new DataObject();
        data.Set(ToolboxDragDataFormat, item.DisplayName);
        await DragDrop.DoDragDrop(e, data, DragDropEffects.Copy);
        e.Handled = true;
    }

    private void OnToolboxItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _pendingToolboxDragItem = null;
        e.Pointer.Capture(null);
    }

    private void OnDesignSurfaceDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(ToolboxDragDataFormat))
        {
            ToolboxDropHint.IsVisible = true;
            Vm?.StatusText = "Drop the Toolbox item onto the artboard.";
        }
    }

    private void OnDesignSurfaceDragLeave(object? sender, DragEventArgs e)
    {
        ToolboxDropHint.IsVisible = false;
    }

    private void OnDesignSurfaceDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(ToolboxDragDataFormat)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDesignSurfaceDrop(object? sender, DragEventArgs e)
    {
        ToolboxDropHint.IsVisible = false;
        if (Vm is null || sender is not Control host
            || e.Data.Get(ToolboxDragDataFormat) is not string displayName
            || Vm.Toolbox.FindItemByDisplayName(displayName) is not { } item)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        var point = e.GetPosition(host);
        Vm.PlaceToolboxItem(item, point.X, point.Y);
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void OnDesignHostPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm is null || sender is not Control host)
        {
            return;
        }

        if (e.GetCurrentPoint(host).Properties.IsRightButtonPressed)
        {
            return;
        }

        var point = e.GetPosition(host);
        if (Vm.Toolbox.SelectedItem is not null)
        {
            Vm.PlaceFromToolbox(point.X, point.Y);
            e.Handled = true;
            return;
        }

        _isMarqueeSelecting = true;
        _marqueeAdditive = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        _marqueeStart = point;
        UpdateMarquee(point);
        e.Pointer.Capture(DesignHost);
        e.Handled = true;
    }

    private void OnElementPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm is null || sender is not Control { DataContext: DesignElement element })
        {
            return;
        }

        if (e.GetCurrentPoint((Control)sender).Properties.IsRightButtonPressed)
        {
            if (!Vm.Canvas.SelectedElements.Contains(element))
            {
                Vm.SelectElement(element);
            }

            Vm.StatusText = $"Selected {element.DisplayName}.";
            return;
        }

        if (element.IsLocked)
        {
            Vm.SelectElement(element);
            Vm.StatusText = "Selected locked control.";
            e.Handled = true;
            return;
        }

        var toggleSelection = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        if (toggleSelection || !Vm.Canvas.SelectedElements.Contains(element))
        {
            Vm.SelectElement(element, toggleSelection);
        }

        if (toggleSelection)
        {
            e.Handled = true;
            return;
        }

        if (element.IsContainerChild)
        {
            Vm.StatusText = "Container child position and size are managed by its parent layout.";
            e.Handled = true;
            return;
        }

        BeginDrag(DragMode.Move, element, e);
        e.Handled = true;
    }

    private void OnHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Rectangle { Tag: string tag })
        {
            return;
        }

        var target = _boundElement;
        if (target is null)
        {
            return;
        }

        if (target.IsLocked)
        {
            Vm?.StatusText = "Selected control is locked.";
            e.Handled = true;
            return;
        }

        if (target.IsContainerChild)
        {
            Vm?.StatusText = "Container child position and size are managed by its parent layout.";
            e.Handled = true;
            return;
        }

        var mode = tag switch
        {
            "N" => DragMode.N,
            "S" => DragMode.S,
            "E" => DragMode.E,
            "W" => DragMode.W,
            "NE" => DragMode.NE,
            "NW" => DragMode.NW,
            "SE" => DragMode.SE,
            "SW" => DragMode.SW,
            _ => DragMode.None,
        };

        if (mode == DragMode.None)
        {
            return;
        }

        BeginDrag(mode, target, e);
        e.Handled = true;
    }

    private void BeginDrag(DragMode mode, DesignElement target, PointerPressedEventArgs e)
    {
        _dragMode = mode;
        _dragTarget = target;
        _dragStart = e.GetPosition(DesignHost);
        _origX = target.X;
        _origY = target.Y;
        _origW = target.Width;
        _origH = target.Height;
        _dragOrigins.Clear();
        if (mode == DragMode.Move && Vm is not null)
        {
            foreach (var selected in Vm.Canvas.SelectedElements)
            {
                _dragOrigins[selected] = new Point(selected.X, selected.Y);
            }
        }

        Vm?.BeginCanvasMutation(MainWindowViewModel.HistoryActionType.TransformElement, "Updated element position/size.");
        e.Pointer.Capture(DesignHost);
    }

    private void OnDragPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isMarqueeSelecting)
        {
            UpdateMarquee(e.GetPosition(DesignHost));
            return;
        }

        if (_dragMode == DragMode.None || _dragTarget is null)
        {
            return;
        }

        var p = e.GetPosition(DesignHost);
        var dx = p.X - _dragStart.X;
        var dy = p.Y - _dragStart.Y;

        ApplyDrag(dx, dy);
    }

    private void ApplyDrag(double dx, double dy)
    {
        if (_dragTarget is null)
        {
            return;
        }

        switch (_dragMode)
        {
            case DragMode.Move:
                var origin = _dragOrigins.TryGetValue(_dragTarget, out var dragOrigin)
                    ? dragOrigin
                    : new Point(_origX, _origY);
                var position = GetSmartSnappedPosition(
                    Math.Max(0, SnapPosition(origin.X + dx)),
                    Math.Max(0, SnapPosition(origin.Y + dy)));
                foreach (var (element, elementOrigin) in _dragOrigins)
                {
                    element.X = Math.Max(0, position.X + (elementOrigin.X - dragOrigin.X));
                    element.Y = Math.Max(0, position.Y + (elementOrigin.Y - dragOrigin.Y));
                }
                break;
            case DragMode.E:
                _dragTarget.Width = SnapSize(_origW + dx);
                break;
            case DragMode.S:
                _dragTarget.Height = SnapSize(_origH + dy);
                break;
            case DragMode.W:
                ResizeLeft(dx);
                break;
            case DragMode.N:
                ResizeTop(dy);
                break;
            case DragMode.SE:
                _dragTarget.Width = SnapSize(_origW + dx);
                _dragTarget.Height = SnapSize(_origH + dy);
                break;
            case DragMode.NE:
                _dragTarget.Width = SnapSize(_origW + dx);
                ResizeTop(dy);
                break;
            case DragMode.SW:
                ResizeLeft(dx);
                _dragTarget.Height = SnapSize(_origH + dy);
                break;
            case DragMode.NW:
                ResizeLeft(dx);
                ResizeTop(dy);
                break;
        }
    }

    private void ResizeLeft(double dx)
    {
        if (_dragTarget is null)
        {
            return;
        }

        var newW = _origW - dx;
        if (newW < MinSize)
        {
            _dragTarget.Width = MinSize;
            _dragTarget.X = Math.Max(0, SnapPosition(_origX + (_origW - MinSize)));
        }
        else
        {
            _dragTarget.Width = SnapSize(newW);
            _dragTarget.X = Math.Max(0, SnapPosition(_origX + (_origW - _dragTarget.Width)));
        }
    }

    private void ResizeTop(double dy)
    {
        if (_dragTarget is null)
        {
            return;
        }

        var newH = _origH - dy;
        if (newH < MinSize)
        {
            _dragTarget.Height = MinSize;
            _dragTarget.Y = Math.Max(0, SnapPosition(_origY + (_origH - MinSize)));
        }
        else
        {
            _dragTarget.Height = SnapSize(newH);
            _dragTarget.Y = Math.Max(0, SnapPosition(_origY + (_origH - _dragTarget.Height)));
        }
    }

    private double SnapPosition(double value) => Vm?.Canvas.SnapPosition(value) ?? value;

    private Point GetSmartSnappedPosition(double x, double y)
    {
        if (_dragTarget is null || Vm is null)
        {
            HideSmartSnapGuides();
            return new Point(x, y);
        }

        double? guideX = null;
        double? guideY = null;
        var bestX = SmartSnapThreshold + 1;
        var bestY = SmartSnapThreshold + 1;
        var snappedX = x;
        var snappedY = y;
        var movingX = new[] { x, x + _dragTarget.Width / 2, x + _dragTarget.Width };
        var movingY = new[] { y, y + _dragTarget.Height / 2, y + _dragTarget.Height };
        var candidatesX = new System.Collections.Generic.List<double> { 0, Vm.Canvas.ArtboardWidth / 2, Vm.Canvas.ArtboardWidth };
        var candidatesY = new System.Collections.Generic.List<double> { 0, Vm.Canvas.ArtboardHeight / 2, Vm.Canvas.ArtboardHeight };

        foreach (var element in Vm.Canvas.Elements.Where(element => !_dragOrigins.ContainsKey(element)))
        {
            candidatesX.AddRange([element.X, element.X + element.Width / 2, element.X + element.Width]);
            candidatesY.AddRange([element.Y, element.Y + element.Height / 2, element.Y + element.Height]);
        }

        foreach (var candidate in candidatesX)
        {
            foreach (var moving in movingX)
            {
                var distance = Math.Abs(candidate - moving);
                if (distance < bestX)
                {
                    bestX = distance;
                    snappedX = x + candidate - moving;
                    guideX = candidate;
                }
            }
        }

        foreach (var candidate in candidatesY)
        {
            foreach (var moving in movingY)
            {
                var distance = Math.Abs(candidate - moving);
                if (distance < bestY)
                {
                    bestY = distance;
                    snappedY = y + candidate - moving;
                    guideY = candidate;
                }
            }
        }

        UpdateSmartSnapGuides(guideX, guideY);
        return new Point(snappedX, snappedY);
    }

    private void UpdateSmartSnapGuides(double? x, double? y)
    {
        SmartSnapVertical.IsVisible = x.HasValue;
        SmartSnapHorizontal.IsVisible = y.HasValue;
        if (x.HasValue)
        {
            Canvas.SetLeft(SmartSnapVertical, x.Value);
            SmartSnapVertical.Height = Vm?.Canvas.ArtboardHeight ?? 0;
        }

        if (y.HasValue)
        {
            Canvas.SetTop(SmartSnapHorizontal, y.Value);
            SmartSnapHorizontal.Width = Vm?.Canvas.ArtboardWidth ?? 0;
        }
    }

    private void HideSmartSnapGuides()
    {
        SmartSnapVertical.IsVisible = false;
        SmartSnapHorizontal.IsVisible = false;
    }

    private double SnapSize(double value) => Vm?.Canvas.SnapSize(value, MinSize) ?? Math.Max(MinSize, value);

    private void OnDragPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isMarqueeSelecting)
        {
            CompleteMarquee(e.GetPosition(DesignHost));
            _isMarqueeSelecting = false;
            MarqueeRectangle.IsVisible = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }

        if (_dragMode == DragMode.None)
        {
            e.Pointer.Capture(null);
            return;
        }

        _dragMode = DragMode.None;
        _dragTarget = null;
        _dragOrigins.Clear();
        HideSmartSnapGuides();
        e.Pointer.Capture(null);
        e.Handled = true;

        Vm?.CommitCanvasMutation();
    }

    private void UpdateMarquee(Point current)
    {
        var area = GetMarqueeArea(current);
        Canvas.SetLeft(MarqueeRectangle, area.X);
        Canvas.SetTop(MarqueeRectangle, area.Y);
        MarqueeRectangle.Width = area.Width;
        MarqueeRectangle.Height = area.Height;
        MarqueeRectangle.IsVisible = area.Width >= MarqueeThreshold || area.Height >= MarqueeThreshold;
    }

    private void CompleteMarquee(Point current)
    {
        if (Vm is null)
        {
            return;
        }

        var area = GetMarqueeArea(current);
        if (area.Width < MarqueeThreshold && area.Height < MarqueeThreshold)
        {
            if (!_marqueeAdditive)
            {
                Vm.SelectElements(Array.Empty<DesignElement>());
            }

            return;
        }

        var selected = Vm.Canvas.Elements
            .Where(element => area.Intersects(new Rect(element.X, element.Y, element.Width, element.Height)))
            .ToList();
        Vm.SelectElements(selected, _marqueeAdditive);
    }

    private Rect GetMarqueeArea(Point current)
    {
        var x = Math.Min(_marqueeStart.X, current.X);
        var y = Math.Min(_marqueeStart.Y, current.Y);
        var width = Math.Abs(current.X - _marqueeStart.X);
        var height = Math.Abs(current.Y - _marqueeStart.Y);
        return new Rect(x, y, width, height);
    }

    private async Task OpenStorageFileAsync(IStorageFile file)
    {
        if (Vm is null)
        {
            return;
        }

        await using var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        if (!Vm.TryImportDraftAxaml(content, out var error, out var warning))
        {
            Vm.StatusText = $"Open failed: {error}";
            return;
        }

        var localPath = file.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            Vm.MarkDocumentLoaded(localPath);
            Vm.StatusText = BuildOpenStatus(System.IO.Path.GetFileName(localPath), warning);
        }
        else
        {
            Vm.MarkDocumentLoadedWithoutPath();
            Vm.StatusText = BuildOpenStatus(file.Name, warning);
        }
    }

    private static string BuildOpenStatus(string name, string warning)
        => string.IsNullOrEmpty(warning) ? $"Opened {name}" : $"Opened {name}. {warning}";

    private async Task<bool> SaveDocumentAsync(bool forceSaveAs)
    {
        if (Vm is null)
        {
            return false;
        }

        FlushPendingPropertyHistory();

        var axaml = Vm.ExportFullAxaml();
        var targetPath = forceSaveAs ? null : Vm.CurrentDocumentPath;
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save AXAML",
                SuggestedFileName = "design-draft.axaml",
                DefaultExtension = "axaml",
                FileTypeChoices =
                [
                    new FilePickerFileType("AXAML") { Patterns = ["*.axaml"] }
                ]
            });

            if (file is null)
            {
                return false;
            }

            var pickedPath = file.TryGetLocalPath();
            try
            {
                if (!string.IsNullOrWhiteSpace(pickedPath))
                {
                    await AtomicFileWriter.WriteAllTextAsync(pickedPath, axaml);
                    Vm.MarkDocumentSaved(pickedPath);
                    Vm.StatusText = $"Saved {System.IO.Path.GetFileName(pickedPath)}";
                }
                else
                {
                    await using var stream = await file.OpenWriteAsync();
                    stream.SetLength(0);
                    using var writer = new StreamWriter(stream);
                    await writer.WriteAsync(axaml);
                    await writer.FlushAsync();

                    Vm.MarkCurrentStateSaved();
                    Vm.StatusText = $"Saved {file.Name}";
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Vm.StatusText = $"Could not save {file.Name}: {exception.Message}";
                return false;
            }

            return true;
        }

        try
        {
            await AtomicFileWriter.WriteAllTextAsync(targetPath, axaml);
            Vm.MarkDocumentSaved(targetPath);
            Vm.StatusText = $"Saved {System.IO.Path.GetFileName(targetPath)}";
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Vm.StatusText = $"Could not save {System.IO.Path.GetFileName(targetPath)}: {exception.Message}";
            return false;
        }
    }

    private async Task<bool> EnsureCanContinueWithUnsavedChangesAsync()
    {
        if (Vm is null || !Vm.IsDirty)
        {
            return true;
        }

        var choice = await ShowUnsavedChangesDialogAsync();
        return choice switch
        {
            UnsavedChoice.Discard => true,
            UnsavedChoice.Save => await SaveDocumentAsync(forceSaveAs: false),
            _ => false,
        };
    }

    private async Task<UnsavedChoice> ShowUnsavedChangesDialogAsync()
    {
        var dialog = new Window
        {
            Title = "Unsaved Changes",
            Width = 420,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        var message = new TextBlock
        {
            Text = "You have unsaved changes. Save before continuing?",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12),
        };

        var saveButton = new Button { Content = "Save", MinWidth = 80, Margin = new Thickness(0, 0, 8, 0) };
        var discardButton = new Button { Content = "Discard", MinWidth = 80, Margin = new Thickness(0, 0, 8, 0) };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 80 };

        saveButton.Click += (_, _) => dialog.Close(UnsavedChoice.Save);
        discardButton.Click += (_, _) => dialog.Close(UnsavedChoice.Discard);
        cancelButton.Click += (_, _) => dialog.Close(UnsavedChoice.Cancel);

        dialog.Content = new DockPanel
        {
            Margin = new Thickness(16),
            Children =
            {
                new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        message,
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Children = { saveButton, discardButton, cancelButton }
                        }
                    }
                }
            }
        };

        return await dialog.ShowDialog<UnsavedChoice>(this);
    }

    private async Task<IReadOnlyList<string>?> ShowItemsEditorDialogAsync(ItemsEditorState state)
    {
        var editor = new TextBox
        {
            Text = string.Join(Environment.NewLine, state.Items),
            AcceptsReturn = true,
            MinHeight = 200,
        };

        var dialog = new Window
        {
            Title = state.Mode == ItemsEditorMode.DataGrid
                ? $"Edit DataGrid Columns - {state.ControlName}"
                : $"Edit Items - {state.ControlName}",
            Width = state.Mode is ItemsEditorMode.Menu or ItemsEditorMode.DataGrid ? 680 : 460,
            Height = state.Mode is ItemsEditorMode.Menu or ItemsEditorMode.DataGrid ? 430 : 380,
            MinWidth = 360,
            MinHeight = 260,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        applyButton.Click += (_, _) =>
        {
            var updatedItems = (editor.Text ?? string.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n', state.Mode != ItemsEditorMode.Flat
                    ? StringSplitOptions.None
                    : StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            if (state.Mode == ItemsEditorMode.TreeView
                && !DesignerTreeItemRuntime.TryParseEditorLines(updatedItems, out _, out var error))
            {
                errorText.Text = error;
                return;
            }

            if (state.Mode == ItemsEditorMode.Menu
                && !DesignerMenuItemRuntime.TryParseEditorLines(updatedItems, out _, out var menuError))
            {
                errorText.Text = menuError;
                return;
            }

            if (state.Mode == ItemsEditorMode.DataGrid
                && !DesignerDataGridRuntime.TryParseEditorLines(updatedItems, out _, out var dataGridError))
            {
                errorText.Text = dataGridError;
                return;
            }

            dialog.Close(updatedItems);
        };

        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };

        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = state.Mode switch
                    {
                        ItemsEditorMode.TreeView =>
                            "Use [-] for expanded, [+] for collapsed, and two spaces per child level.",
                        ItemsEditorMode.Menu =>
                            "Use two spaces per child level; --- separator; [x]/[ ] check; (x)/( ) radio with {Group}; | Ctrl+N shortcut.",
                        ItemsEditorMode.DataGrid =>
                            "One column per line: Type | Header | Binding | Width | ReadOnly. Type is Text or CheckBox; Width supports Auto, pixels, *, and N*.",
                        _ => "Enter one item per line. Empty lines are ignored.",
                    },
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                editor,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(editor, 1);
        Grid.SetRow(errorText, 2);
        Grid.SetRow(buttons, 3);
        dialog.Content = content;

        return await dialog.ShowDialog<IReadOnlyList<string>?>(this);
    }

    private async Task ShowTypographyPropertiesDialogAsync(TypographyEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var fontFamilyEditor = new TextBox
        {
            Text = state.FontFamily,
            Watermark = "Font family or avares URI",
        };
        var fontSizeEditor = new TextBox { Text = state.FontSize };
        var fontStyleEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<Avalonia.Media.FontStyle>(),
            SelectedItem = state.FontStyle,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var fontWeightEditor = new ComboBox
        {
            ItemsSource = DesignerTypographyRuntime.FontWeightNames,
            SelectedItem = state.FontWeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var textAlignmentEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<Avalonia.Media.TextAlignment>(),
            SelectedItem = state.TextAlignment,
            IsEnabled = state.SupportsTextAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var textWrappingEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<Avalonia.Media.TextWrapping>(),
            SelectedItem = state.TextWrapping,
            IsEnabled = state.SupportsTextWrapping,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Typography Properties - {state.ControlName}",
            Width = 620,
            Height = 440,
            MinWidth = 520,
            MinHeight = 390,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            if (!Vm.SetSelectedTypographyProperties(
                    fontFamilyEditor.Text ?? string.Empty,
                    fontSizeEditor.Text ?? string.Empty,
                    fontStyleEditor.SelectedItem?.ToString() ?? string.Empty,
                    fontWeightEditor.SelectedItem?.ToString() ?? string.Empty,
                    textAlignmentEditor.SelectedItem?.ToString() ?? state.TextAlignment,
                    textWrappingEditor.SelectedItem?.ToString() ?? state.TextWrapping))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField("Font family", fontFamilyEditor, 0, 0);
        AddField("Font size", fontSizeEditor, 0, 1);
        AddField("Font style", fontStyleEditor, 1, 0);
        AddField("Font weight", fontWeightEditor, 1, 1);
        AddField("Text alignment", textAlignmentEditor, 2, 0);
        AddField("Text wrapping", textWrappingEditor, 2, 1);

        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Font settings apply to text and font-aware controls. Alignment and wrapping are available for TextBlock and TextBox.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(errorText, 2);
        Grid.SetRow(buttons, 3);
        dialog.Content = content;
        await dialog.ShowDialog(this);
        return;

        void AddField(string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            fields.Children.Add(field);
        }
    }

    private async Task ShowTransformPropertiesDialogAsync(TransformEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var translateXEditor = new TextBox { Text = state.TranslateX };
        var translateYEditor = new TextBox { Text = state.TranslateY };
        var rotationEditor = new TextBox { Text = state.Rotation };
        var scaleXEditor = new TextBox { Text = state.ScaleX };
        var scaleYEditor = new TextBox { Text = state.ScaleY };
        var skewXEditor = new TextBox { Text = state.SkewX };
        var skewYEditor = new TextBox { Text = state.SkewY };
        var originXEditor = new TextBox { Text = state.OriginX };
        var originYEditor = new TextBox { Text = state.OriginY };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Transform Properties - {state.ControlName}",
            Width = 620,
            Height = 560,
            MinWidth = 520,
            MinHeight = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            if (!Vm.SetSelectedTransformProperties(
                    translateXEditor.Text ?? string.Empty,
                    translateYEditor.Text ?? string.Empty,
                    rotationEditor.Text ?? string.Empty,
                    scaleXEditor.Text ?? string.Empty,
                    scaleYEditor.Text ?? string.Empty,
                    skewXEditor.Text ?? string.Empty,
                    skewYEditor.Text ?? string.Empty,
                    originXEditor.Text ?? string.Empty,
                    originYEditor.Text ?? string.Empty))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var resetButton = new Button { Content = "Reset", MinWidth = 84 };
        resetButton.Click += (_, _) =>
        {
            translateXEditor.Text = "0";
            translateYEditor.Text = "0";
            rotationEditor.Text = "0";
            scaleXEditor.Text = "1";
            scaleYEditor.Text = "1";
            skewXEditor.Text = "0";
            skewYEditor.Text = "0";
            originXEditor.Text = "50";
            originYEditor.Text = "50";
            errorText.Text = string.Empty;
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
            ColumnSpacing = 8,
            Children = { resetButton, cancelButton, applyButton },
        };
        Grid.SetColumn(cancelButton, 2);
        Grid.SetColumn(applyButton, 3);

        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField("Translate X (px)", translateXEditor, 0, 0);
        AddField("Translate Y (px)", translateYEditor, 0, 1);
        AddField("Rotation (degrees)", rotationEditor, 1, 0);
        AddField("Scale X", scaleXEditor, 1, 1);
        AddField("Scale Y", scaleYEditor, 2, 0);
        AddField("Skew X (degrees)", skewXEditor, 2, 1);
        AddField("Skew Y (degrees)", skewYEditor, 3, 0);
        AddField("Origin X (%)", originXEditor, 3, 1);
        AddField("Origin Y (%)", originYEditor, 4, 0);

        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Transform the rendered control without changing its layout slot. Origin values are percentages from 0 to 100.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(errorText, 2);
        Grid.SetRow(buttons, 3);
        dialog.Content = content;
        await dialog.ShowDialog(this);
        return;

        void AddField(string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            fields.Children.Add(field);
        }
    }

    private async Task ShowAccessibilityPropertiesDialogAsync(AccessibilityEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var accessibleNameEditor = new TextBox
        {
            Text = state.AccessibleName,
            Watermark = "Name announced by assistive technology",
        };
        var automationIdEditor = new TextBox
        {
            Text = state.AutomationId,
            Watermark = "Stable UI automation identifier",
        };
        var helpTextEditor = new TextBox
        {
            Text = state.HelpText,
            AcceptsReturn = true,
            Height = 70,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Watermark = "Additional instructions for assistive technology",
        };
        var toolTipEditor = new TextBox
        {
            Text = state.ToolTip,
            AcceptsReturn = true,
            Height = 70,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Watermark = "Pointer tooltip",
        };
        var accessibilityViewEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<Avalonia.Automation.AccessibilityView>(),
            SelectedItem = state.AccessibilityView,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var headingLevelEditor = new TextBox
        {
            Text = state.HeadingLevel,
            Watermark = "0 means not a heading",
        };
        var liveSettingEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<Avalonia.Automation.AutomationLiveSetting>(),
            SelectedItem = state.LiveSetting,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var tabIndexEditor = new TextBox { Text = state.TabIndex };
        var requiredEditor = new CheckBox
        {
            Content = "Required for form",
            IsChecked = state.IsRequiredForForm,
        };
        var tabStopEditor = new CheckBox
        {
            Content = "Include in tab navigation",
            IsChecked = state.IsTabStop,
        };
        var focusableEditor = new CheckBox
        {
            Content = "Focusable",
            IsChecked = state.Focusable,
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Accessibility & Navigation - {state.ControlName}",
            Width = 720,
            Height = 650,
            MinWidth = 600,
            MinHeight = 580,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            if (!Vm.SetSelectedAccessibilityProperties(
                    toolTipEditor.Text ?? string.Empty,
                    accessibleNameEditor.Text ?? string.Empty,
                    automationIdEditor.Text ?? string.Empty,
                    helpTextEditor.Text ?? string.Empty,
                    accessibilityViewEditor.SelectedItem?.ToString() ?? string.Empty,
                    headingLevelEditor.Text ?? string.Empty,
                    liveSettingEditor.SelectedItem?.ToString() ?? string.Empty,
                    requiredEditor.IsChecked == true,
                    tabIndexEditor.Text ?? string.Empty,
                    tabStopEditor.IsChecked == true,
                    focusableEditor.IsChecked == true))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField("Accessible name", accessibleNameEditor, 0, 0);
        AddField("Automation ID", automationIdEditor, 0, 1);
        AddField("Help text", helpTextEditor, 1, 0);
        AddField("Tooltip", toolTipEditor, 1, 1);
        AddField("Accessibility view", accessibilityViewEditor, 2, 0);
        AddField("Heading level (0-9)", headingLevelEditor, 2, 1);
        AddField("Live setting", liveSettingEditor, 3, 0);
        AddField("Tab index", tabIndexEditor, 3, 1);
        var switches = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 20,
            Margin = new Thickness(0, 4, 0, 0),
            Children = { requiredEditor, tabStopEditor, focusableEditor },
        };
        Grid.SetRow(switches, 4);
        Grid.SetColumnSpan(switches, 2);
        fields.Children.Add(switches);

        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Configure assistive-technology metadata and keyboard focus behavior. Heading level 0 disables heading semantics.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(errorText, 2);
        Grid.SetRow(buttons, 3);
        dialog.Content = content;
        await dialog.ShowDialog(this);
        return;

        void AddField(string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            fields.Children.Add(field);
        }
    }

    private async Task ShowInteractionPropertiesDialogAsync(InteractionEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var opacityEditor = new TextBox
        {
            Text = state.Opacity,
            Watermark = "0 to 1",
        };
        var flowDirectionEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<Avalonia.Media.FlowDirection>(),
            SelectedItem = state.FlowDirection,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var cursorEditor = new ComboBox
        {
            ItemsSource = DesignerInteractionRuntime.CursorNames,
            SelectedItem = state.Cursor,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MaxDropDownHeight = 300,
        };
        var enabledEditor = new CheckBox
        {
            Content = "Enabled",
            IsChecked = state.IsEnabled,
        };
        var visibleEditor = new CheckBox
        {
            Content = "Visible",
            IsChecked = state.IsVisible,
        };
        var hitTestEditor = new CheckBox
        {
            Content = "Hit test visible",
            IsChecked = state.IsHitTestVisible,
        };
        var clipEditor = new CheckBox
        {
            Content = "Clip to bounds",
            IsChecked = state.ClipToBounds,
        };
        var layoutRoundingEditor = new CheckBox
        {
            Content = "Use layout rounding",
            IsChecked = state.UseLayoutRounding,
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Interaction & Rendering - {state.ControlName}",
            Width = 640,
            Height = 480,
            MinWidth = 540,
            MinHeight = 430,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            if (!Vm.SetSelectedInteractionProperties(
                    opacityEditor.Text ?? string.Empty,
                    enabledEditor.IsChecked == true,
                    visibleEditor.IsChecked == true,
                    hitTestEditor.IsChecked == true,
                    clipEditor.IsChecked == true,
                    layoutRoundingEditor.IsChecked == true,
                    flowDirectionEditor.SelectedItem?.ToString() ?? string.Empty,
                    cursorEditor.SelectedItem?.ToString() ?? string.Empty))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField("Opacity", opacityEditor, 0, 0);
        AddField("Flow direction", flowDirectionEditor, 0, 1);
        AddField("Cursor", cursorEditor, 1, 0);
        var switches = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 16,
            LineSpacing = 8,
            Margin = new Thickness(0, 8, 0, 0),
            Children =
            {
                enabledEditor,
                visibleEditor,
                hitTestEditor,
                clipEditor,
                layoutRoundingEditor,
            },
        };

        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Configure rendering visibility, input participation, clipping, right-to-left flow, and the pointer cursor.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                switches,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(switches, 2);
        Grid.SetRow(errorText, 4);
        Grid.SetRow(buttons, 5);
        dialog.Content = content;
        await dialog.ShowDialog(this);
        return;

        void AddField(string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            fields.Children.Add(field);
        }
    }

    private async Task ShowEffectPropertiesDialogAsync(EffectEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var kindEditor = new ComboBox
        {
            ItemsSource = DesignerEffectRuntime.EffectKinds,
            SelectedItem = state.Kind,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var blurEditor = new TextBox { Text = state.BlurRadius, Watermark = "0 to 1000" };
        var offsetXEditor = new TextBox { Text = state.OffsetX, Watermark = "-10000 to 10000" };
        var offsetYEditor = new TextBox { Text = state.OffsetY, Watermark = "-10000 to 10000" };
        var shadowBlurEditor = new TextBox { Text = state.ShadowBlurRadius, Watermark = "0 to 1000" };
        var colorEditor = new TextBox { Text = state.ShadowColor, Watermark = "#000000" };
        var shadowOpacityEditor = new TextBox { Text = state.ShadowOpacity, Watermark = "0 to 1" };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Visual Effects - {state.ControlName}",
            Width = 680,
            Height = 540,
            MinWidth = 560,
            MinHeight = 480,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            if (!Vm.SetSelectedEffectProperties(
                    kindEditor.SelectedItem?.ToString() ?? string.Empty,
                    blurEditor.Text ?? string.Empty,
                    offsetXEditor.Text ?? string.Empty,
                    offsetYEditor.Text ?? string.Empty,
                    shadowBlurEditor.Text ?? string.Empty,
                    colorEditor.Text ?? string.Empty,
                    shadowOpacityEditor.Text ?? string.Empty))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var blurFields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*"),
            RowDefinitions = new RowDefinitions("Auto"),
        };
        var shadowFields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField(blurFields, "Blur radius", blurEditor, 0, 0);
        AddField(shadowFields, "Horizontal offset", offsetXEditor, 0, 0);
        AddField(shadowFields, "Vertical offset", offsetYEditor, 0, 1);
        AddField(shadowFields, "Shadow blur radius", shadowBlurEditor, 1, 0);
        var colorAndOpacity = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2*,*"),
            ColumnSpacing = 8,
        };
        colorAndOpacity.Children.Add(colorEditor);
        Grid.SetColumn(shadowOpacityEditor, 1);
        colorAndOpacity.Children.Add(shadowOpacityEditor);
        AddField(shadowFields, "Color / opacity", colorAndOpacity, 1, 1);

        void RefreshMode()
        {
            var kind = kindEditor.SelectedItem?.ToString();
            blurFields.IsVisible = string.Equals(kind, "Blur", StringComparison.Ordinal);
            shadowFields.IsVisible = string.Equals(kind, "Drop Shadow", StringComparison.Ordinal);
        }

        kindEditor.SelectionChanged += (_, _) => RefreshMode();
        RefreshMode();
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Apply a blur or drop shadow without changing the control's layout size. Shadow opacity is exported through the AXAML color alpha channel.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                kindEditor,
                blurFields,
                shadowFields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(kindEditor, 1);
        Grid.SetRow(blurFields, 2);
        Grid.SetRow(shadowFields, 3);
        Grid.SetRow(errorText, 5);
        Grid.SetRow(buttons, 6);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static void AddField(Grid owner, string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            owner.Children.Add(field);
        }
    }

    private async Task ShowRangePropertiesDialogAsync(RangeEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var minimumEditor = new TextBox { Text = state.Minimum };
        var maximumEditor = new TextBox { Text = state.Maximum };
        var valueEditor = new TextBox
        {
            Text = state.Value,
            Watermark = state.ControlKind == "NumericUpDown" ? "Blank is allowed" : null,
        };
        var smallChangeEditor = new TextBox { Text = state.SmallChange };
        var largeChangeEditor = new TextBox { Text = state.LargeChange };
        var sliderOrientationEditor = CreateCombo(
            DesignerRangeRuntime.OrientationNames,
            state.Orientation);
        var directionReversedEditor = new CheckBox
        {
            Content = "Reverse direction",
            IsChecked = state.IsDirectionReversed,
        };
        var tickFrequencyEditor = new TextBox { Text = state.TickFrequency };
        var tickPlacementEditor = CreateCombo(
            DesignerRangeRuntime.TickPlacementNames,
            state.TickPlacement);
        var snapToTickEditor = new CheckBox
        {
            Content = "Snap value to ticks",
            IsChecked = state.IsSnapToTickEnabled,
        };
        var progressOrientationEditor = CreateCombo(
            DesignerRangeRuntime.OrientationNames,
            state.Orientation);
        var indeterminateEditor = new CheckBox
        {
            Content = "Indeterminate",
            IsChecked = state.IsIndeterminate,
        };
        var showProgressTextEditor = new CheckBox
        {
            Content = "Show progress text",
            IsChecked = state.ShowProgressText,
        };
        var progressTextFormatEditor = new TextBox
        {
            Text = state.ProgressTextFormat,
            Watermark = "{1:0}%",
        };
        var incrementEditor = new TextBox { Text = state.Increment };
        var formatStringEditor = new TextBox
        {
            Text = state.FormatString,
            Watermark = "Blank or N2",
        };
        var clipValueEditor = new CheckBox
        {
            Content = "Clip value to range",
            IsChecked = state.ClipValueToMinMax,
        };
        var allowSpinEditor = new CheckBox
        {
            Content = "Allow spin",
            IsChecked = state.AllowSpin,
        };
        var showSpinnerEditor = new CheckBox
        {
            Content = "Show spinner buttons",
            IsChecked = state.ShowButtonSpinner,
        };
        var spinnerLocationEditor = CreateCombo(
            DesignerRangeRuntime.SpinnerLocationNames,
            state.ButtonSpinnerLocation);

        var commonFields = CreateFieldGrid("*,*,*");
        AddField(commonFields, "Minimum", minimumEditor, 0, 0);
        AddField(commonFields, "Maximum", maximumEditor, 0, 1);
        AddField(commonFields, "Value", valueEditor, 0, 2);

        var sliderFields = CreateFieldGrid("*,*");
        sliderFields.RowDefinitions = new RowDefinitions("Auto,Auto,Auto");
        AddField(sliderFields, "Small change", smallChangeEditor, 0, 0);
        AddField(sliderFields, "Large change", largeChangeEditor, 0, 1);
        AddField(sliderFields, "Orientation", sliderOrientationEditor, 1, 0);
        AddField(sliderFields, "Tick frequency", tickFrequencyEditor, 1, 1);
        AddField(sliderFields, "Tick placement", tickPlacementEditor, 2, 0);
        var sliderSwitches = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { directionReversedEditor, snapToTickEditor },
        };
        Grid.SetRow(sliderSwitches, 2);
        Grid.SetColumn(sliderSwitches, 1);
        sliderFields.Children.Add(sliderSwitches);

        var progressFields = CreateFieldGrid("*,*");
        progressFields.RowDefinitions = new RowDefinitions("Auto,Auto");
        AddField(progressFields, "Orientation", progressOrientationEditor, 0, 0);
        AddField(progressFields, "Progress text format", progressTextFormatEditor, 0, 1);
        var progressSwitches = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            Children = { indeterminateEditor, showProgressTextEditor },
        };
        Grid.SetRow(progressSwitches, 1);
        Grid.SetColumnSpan(progressSwitches, 2);
        progressFields.Children.Add(progressSwitches);

        var numericFields = CreateFieldGrid("*,*");
        numericFields.RowDefinitions = new RowDefinitions("Auto,Auto");
        AddField(numericFields, "Increment", incrementEditor, 0, 0);
        AddField(numericFields, "Number format", formatStringEditor, 0, 1);
        AddField(numericFields, "Spinner location", spinnerLocationEditor, 1, 0);
        var numericSwitches = new WrapPanel
        {
            ItemSpacing = 16,
            LineSpacing = 8,
            Children = { clipValueEditor, allowSpinEditor, showSpinnerEditor },
        };
        Grid.SetRow(numericSwitches, 1);
        Grid.SetColumn(numericSwitches, 1);
        numericFields.Children.Add(numericSwitches);

        sliderFields.IsVisible = state.ControlKind == "Slider";
        progressFields.IsVisible = state.ControlKind == "ProgressBar";
        numericFields.IsVisible = state.ControlKind == "NumericUpDown";
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Range & Value - {state.ControlName}",
            Width = 720,
            Height = 570,
            MinWidth = 600,
            MinHeight = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerRangeEditorInput(
                minimumEditor.Text ?? string.Empty,
                maximumEditor.Text ?? string.Empty,
                valueEditor.Text ?? string.Empty,
                smallChangeEditor.Text ?? string.Empty,
                largeChangeEditor.Text ?? string.Empty,
                state.ControlKind == "ProgressBar"
                    ? progressOrientationEditor.SelectedItem?.ToString() ?? string.Empty
                    : sliderOrientationEditor.SelectedItem?.ToString() ?? string.Empty,
                directionReversedEditor.IsChecked == true,
                tickFrequencyEditor.Text ?? string.Empty,
                tickPlacementEditor.SelectedItem?.ToString() ?? string.Empty,
                snapToTickEditor.IsChecked == true,
                indeterminateEditor.IsChecked == true,
                showProgressTextEditor.IsChecked == true,
                progressTextFormatEditor.Text ?? string.Empty,
                incrementEditor.Text ?? string.Empty,
                formatStringEditor.Text ?? string.Empty,
                clipValueEditor.IsChecked == true,
                allowSpinEditor.IsChecked == true,
                showSpinnerEditor.IsChecked == true,
                spinnerLocationEditor.SelectedItem?.ToString() ?? string.Empty);
            if (!Vm.SetSelectedRangeProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = $"Configure validated range behavior for {state.ControlKind}. Minimum must be less than Maximum, and Value must stay inside the range.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                commonFields,
                sliderFields,
                progressFields,
                numericFields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(commonFields, 1);
        Grid.SetRow(sliderFields, 2);
        Grid.SetRow(progressFields, 3);
        Grid.SetRow(numericFields, 4);
        Grid.SetRow(errorText, 6);
        Grid.SetRow(buttons, 7);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static ComboBox CreateCombo(IEnumerable<string> items, string selected)
            => new()
            {
                ItemsSource = items,
                SelectedItem = selected,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

        static Grid CreateFieldGrid(string columns)
            => new()
            {
                ColumnDefinitions = new ColumnDefinitions(columns),
                RowDefinitions = new RowDefinitions("Auto"),
                ColumnSpacing = 12,
                RowSpacing = 10,
            };

        static void AddField(Grid owner, string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            owner.Children.Add(field);
        }
    }

    private async Task ShowTextInputPropertiesDialogAsync(TextInputEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var textEditor = new TextBox
        {
            Text = state.Text,
            AcceptsReturn = true,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MinHeight = 82,
            Watermark = "Design-time text",
        };
        var watermarkEditor = new TextBox { Text = state.Watermark };
        var wrappingEditor = new ComboBox
        {
            ItemsSource = DesignerTextInputRuntime.TextWrappingNames,
            SelectedItem = state.TextWrapping,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var alignmentEditor = new ComboBox
        {
            ItemsSource = DesignerTextInputRuntime.TextAlignmentNames,
            SelectedItem = state.TextAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var maxLengthEditor = new TextBox { Text = state.MaxLength, Watermark = "0 = unlimited" };
        var minLinesEditor = new TextBox { Text = state.MinLines, Watermark = "0 = automatic" };
        var maxLinesEditor = new TextBox { Text = state.MaxLines, Watermark = "0 = automatic" };
        var passwordCharEditor = new TextBox
        {
            Text = state.PasswordChar,
            MaxLength = 1,
            Watermark = "Blank or one character",
        };
        var undoLimitEditor = new TextBox { Text = state.UndoLimit };
        var acceptsReturnEditor = new CheckBox
        {
            Content = "Accept Return",
            IsChecked = state.AcceptsReturn,
        };
        var acceptsTabEditor = new CheckBox
        {
            Content = "Accept Tab",
            IsChecked = state.AcceptsTab,
        };
        var readOnlyEditor = new CheckBox
        {
            Content = "Read only",
            IsChecked = state.IsReadOnly,
        };
        var revealPasswordEditor = new CheckBox
        {
            Content = "Reveal password",
            IsChecked = state.RevealPassword,
        };
        var floatingWatermarkEditor = new CheckBox
        {
            Content = "Floating watermark",
            IsChecked = state.UseFloatingWatermark,
        };
        var undoEnabledEditor = new CheckBox
        {
            Content = "Enable undo",
            IsChecked = state.IsUndoEnabled,
        };
        var clearSelectionEditor = new CheckBox
        {
            Content = "Clear selection on lost focus",
            IsChecked = state.ClearSelectionOnLostFocus,
        };
        var inactiveHighlightEditor = new CheckBox
        {
            Content = "Highlight inactive selection",
            IsChecked = state.IsInactiveSelectionHighlightEnabled,
        };

        void RefreshPasswordMode()
        {
            var isPassword = !string.IsNullOrEmpty(passwordCharEditor.Text);
            textEditor.IsEnabled = !isPassword;
            revealPasswordEditor.IsEnabled = isPassword;
            if (isPassword)
            {
                textEditor.Watermark = "Password text is not stored by the designer";
            }
            else
            {
                textEditor.Watermark = "Design-time text";
            }
        }

        passwordCharEditor.TextChanged += (_, _) => RefreshPasswordMode();
        RefreshPasswordMode();

        var primaryFields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField(primaryFields, "Design text", textEditor, 0, 0);
        AddField(primaryFields, "Watermark", watermarkEditor, 0, 1);
        AddField(primaryFields, "Text wrapping", wrappingEditor, 1, 0);
        AddField(primaryFields, "Text alignment", alignmentEditor, 1, 1);

        var limitsFields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField(limitsFields, "Max length", maxLengthEditor, 0, 0);
        AddField(limitsFields, "Min lines", minLinesEditor, 0, 1);
        AddField(limitsFields, "Max lines", maxLinesEditor, 0, 2);
        AddField(limitsFields, "Password character", passwordCharEditor, 1, 0);
        AddField(limitsFields, "Undo limit", undoLimitEditor, 1, 1);

        var switches = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 16,
            LineSpacing = 8,
            Children =
            {
                acceptsReturnEditor,
                acceptsTabEditor,
                readOnlyEditor,
                revealPasswordEditor,
                floatingWatermarkEditor,
                undoEnabledEditor,
                clearSelectionEditor,
                inactiveHighlightEditor,
            },
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Text Input - {state.ControlName}",
            Width = 780,
            Height = 690,
            MinWidth = 640,
            MinHeight = 580,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerTextInputEditorInput(
                textEditor.Text ?? string.Empty,
                watermarkEditor.Text ?? string.Empty,
                acceptsReturnEditor.IsChecked == true,
                acceptsTabEditor.IsChecked == true,
                wrappingEditor.SelectedItem?.ToString() ?? string.Empty,
                alignmentEditor.SelectedItem?.ToString() ?? string.Empty,
                readOnlyEditor.IsChecked == true,
                maxLengthEditor.Text ?? string.Empty,
                minLinesEditor.Text ?? string.Empty,
                maxLinesEditor.Text ?? string.Empty,
                passwordCharEditor.Text ?? string.Empty,
                revealPasswordEditor.IsChecked == true,
                floatingWatermarkEditor.IsChecked == true,
                undoEnabledEditor.IsChecked == true,
                undoLimitEditor.Text ?? string.Empty,
                clearSelectionEditor.IsChecked == true,
                inactiveHighlightEditor.IsChecked == true);
            if (!Vm.SetSelectedTextInputProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Configure TextBox input, line limits, password behavior, undo, and selection policies. A password character suppresses design text from snapshots and AXAML.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                primaryFields,
                limitsFields,
                switches,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(primaryFields, 1);
        Grid.SetRow(limitsFields, 2);
        Grid.SetRow(switches, 3);
        Grid.SetRow(errorText, 5);
        Grid.SetRow(buttons, 6);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static void AddField(Grid owner, string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            owner.Children.Add(field);
        }
    }

    private async Task ShowSelectableTextBlockPropertiesDialogAsync(
        SelectableTextBlockEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var textEditor = new TextBox
        {
            Text = state.Text,
            AcceptsReturn = true,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MinHeight = 72,
        };
        var selectionBrushEditor = new TextBox
        {
            Text = state.SelectionBrush,
            Watermark = "Example: #663B82F6 or Transparent",
        };
        var selectionForegroundEditor = new TextBox
        {
            Text = state.SelectionForegroundBrush,
            Watermark = "Example: #FFFFFFFF or Transparent",
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var fields = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            RowSpacing = 10,
            Children =
            {
                CreateField("Text", textEditor, 0),
                CreateField("Selection brush", selectionBrushEditor, 1),
                CreateField("Selection foreground", selectionForegroundEditor, 2),
            },
        };
        var dialog = new Window
        {
            Title = $"Edit SelectableTextBlock - {state.ControlName}",
            Width = 680,
            Height = 460,
            MinWidth = 560,
            MinHeight = 380,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerSelectableTextBlockEditorInput(
                textEditor.Text ?? string.Empty,
                selectionBrushEditor.Text ?? string.Empty,
                selectionForegroundEditor.Text ?? string.Empty);
            if (!Vm.SetSelectedSelectableTextBlockProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Configure selectable text content and solid selection colors. SelectionStart and SelectionEnd remain runtime interaction state and are not persisted in the document.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(errorText, 2);
        Grid.SetRow(buttons, 3);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static Control CreateField(string label, Control editor, int row)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            return field;
        }
    }

    private async Task ShowMaskedTextBoxPropertiesDialogAsync(MaskedTextBoxEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var maskEditor = new TextBox
        {
            Text = state.Mask,
            Watermark = "Example: 000-0000 or 00/00/0000",
        };
        var promptCharEditor = new TextBox
        {
            Text = state.PromptChar,
            Watermark = "Exactly one character",
        };
        var hidePromptEditor = new CheckBox
        {
            Content = "Hide prompt characters when leaving the field",
            IsChecked = state.HidePromptOnLeave,
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto"),
            ColumnSpacing = 12,
            Children =
            {
                CreateField("Mask", maskEditor, 0),
                CreateField("Prompt character", promptCharEditor, 1),
            },
        };
        var dialog = new Window
        {
            Title = $"Edit MaskedTextBox - {state.ControlName}",
            Width = 680,
            Height = 340,
            MinWidth = 560,
            MinHeight = 280,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerMaskedTextBoxEditorInput(
                maskEditor.Text ?? string.Empty,
                promptCharEditor.Text ?? string.Empty,
                hidePromptEditor.IsChecked == true);
            if (!Vm.SetSelectedMaskedTextBoxProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Mask syntax follows .NET MaskedTextProvider. Common tokens: 0 required digit, 9 optional digit, L required letter, ? optional letter, and literals such as '-' or '/'.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                hidePromptEditor,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(hidePromptEditor, 2);
        Grid.SetRow(errorText, 3);
        Grid.SetRow(buttons, 4);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static Control CreateField(string label, Control editor, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetColumn(field, column);
            return field;
        }
    }

    private async Task ShowSelectionPropertiesDialogAsync(SelectionEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var isComboBox = state.ControlKind == nameof(DesignerSelectionControlKind.ComboBox);
        var isListBox = state.ControlKind == nameof(DesignerSelectionControlKind.ListBox);
        var supportsIndex = isComboBox || isListBox;
        var supportsSelectionMode = !isComboBox;
        var selectedIndexEditor = new TextBox
        {
            Text = state.SelectedIndex,
            IsEnabled = supportsIndex,
            Watermark = supportsIndex ? "-1 = no selection" : "Not used by TreeView",
        };
        var textEditor = new TextBox
        {
            Text = state.Text,
            IsEnabled = isComboBox && state.IsEditable,
            Watermark = "Editable ComboBox text",
        };
        var placeholderEditor = new TextBox
        {
            Text = state.PlaceholderText,
            IsEnabled = isComboBox,
        };
        var maxDropDownHeightEditor = new TextBox
        {
            Text = state.MaxDropDownHeight,
            IsEnabled = isComboBox,
        };
        var horizontalAlignmentEditor = new ComboBox
        {
            ItemsSource = DesignerSelectionRuntime.HorizontalAlignmentNames,
            SelectedItem = state.HorizontalContentAlignment,
            IsEnabled = isComboBox,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var verticalAlignmentEditor = new ComboBox
        {
            ItemsSource = DesignerSelectionRuntime.VerticalAlignmentNames,
            SelectedItem = state.VerticalContentAlignment,
            IsEnabled = isComboBox,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var textSearchEditor = new CheckBox
        {
            Content = "Type-to-search",
            IsChecked = state.IsTextSearchEnabled,
            IsEnabled = supportsIndex,
        };
        var autoScrollEditor = new CheckBox
        {
            Content = "Auto-scroll to selection",
            IsChecked = state.AutoScrollToSelectedItem,
        };
        var wrapSelectionEditor = new CheckBox
        {
            Content = "Wrap keyboard selection",
            IsChecked = state.WrapSelection,
            IsEnabled = supportsIndex,
        };
        var multipleEditor = new CheckBox
        {
            Content = "Allow multiple selection",
            IsChecked = state.AllowMultiple,
            IsEnabled = supportsSelectionMode,
        };
        var toggleEditor = new CheckBox
        {
            Content = "Toggle selection on tap / Space",
            IsChecked = state.ToggleSelection,
            IsEnabled = supportsSelectionMode,
        };
        var alwaysSelectedEditor = new CheckBox
        {
            Content = "Always keep an item selected",
            IsChecked = state.AlwaysSelected,
            IsEnabled = supportsSelectionMode,
        };
        var editableEditor = new CheckBox
        {
            Content = "Editable ComboBox",
            IsChecked = state.IsEditable,
            IsEnabled = isComboBox,
        };

        void RefreshEditableText()
        {
            var hasSelection = int.TryParse(
                    selectedIndexEditor.Text,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var selectedIndex)
                && selectedIndex >= 0;
            textEditor.IsEnabled = isComboBox
                && editableEditor.IsChecked == true
                && !hasSelection;
            textEditor.Watermark = hasSelection
                ? "Text is supplied by the selected item"
                : "Editable ComboBox text";
        }

        editableEditor.IsCheckedChanged += (_, _) => RefreshEditableText();
        selectedIndexEditor.TextChanged += (_, _) => RefreshEditableText();
        RefreshEditableText();

        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField(fields, "Selected index", selectedIndexEditor, 0, 0);
        AddField(fields, "Editable text", textEditor, 0, 1);
        AddField(fields, "Placeholder text", placeholderEditor, 1, 0);
        AddField(fields, "Maximum drop-down height", maxDropDownHeightEditor, 1, 1);
        AddField(fields, "Horizontal content alignment", horizontalAlignmentEditor, 2, 0);
        AddField(fields, "Vertical content alignment", verticalAlignmentEditor, 2, 1);

        var switches = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 16,
            LineSpacing = 8,
            Children =
            {
                textSearchEditor,
                autoScrollEditor,
                wrapSelectionEditor,
                multipleEditor,
                toggleEditor,
                alwaysSelectedEditor,
                editableEditor,
            },
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Selection Behavior - {state.ControlName}",
            Width = 720,
            Height = 530,
            MinWidth = 620,
            MinHeight = 470,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerSelectionEditorInput(
                selectedIndexEditor.Text ?? string.Empty,
                textSearchEditor.IsChecked == true,
                autoScrollEditor.IsChecked == true,
                wrapSelectionEditor.IsChecked == true,
                multipleEditor.IsChecked == true,
                toggleEditor.IsChecked == true,
                alwaysSelectedEditor.IsChecked == true,
                editableEditor.IsChecked == true,
                textEditor.Text ?? string.Empty,
                placeholderEditor.Text ?? string.Empty,
                maxDropDownHeightEditor.Text ?? string.Empty,
                horizontalAlignmentEditor.SelectedItem?.ToString() ?? string.Empty,
                verticalAlignmentEditor.SelectedItem?.ToString() ?? string.Empty);
            if (!Vm.SetSelectedSelectionProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = $"Configure {state.ControlKind} selection, keyboard navigation, and type-specific presentation behavior.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                switches,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(switches, 2);
        Grid.SetRow(errorText, 4);
        Grid.SetRow(buttons, 5);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static void AddField(Grid owner, string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            owner.Children.Add(field);
        }
    }

    private async Task ShowDateTimePropertiesDialogAsync(DateTimeEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var isDatePicker = state.ControlKind == nameof(DesignerDateTimeControlKind.DatePicker);
        var isCalendarDatePicker =
            state.ControlKind
            == nameof(DesignerDateTimeControlKind.CalendarDatePicker);
        var isCalendarControl =
            state.ControlKind == nameof(DesignerDateTimeControlKind.Calendar);
        var selectedDateEditor = new TextBox
        {
            Text = state.SelectedDate,
            Watermark = "Optional: yyyy-MM-dd",
        };
        var minYearEditor = new TextBox { Text = state.MinYear, Watermark = "yyyy-MM-dd" };
        var maxYearEditor = new TextBox { Text = state.MaxYear, Watermark = "yyyy-MM-dd" };
        var dayVisibleEditor = new CheckBox { Content = "Show day", IsChecked = state.DayVisible };
        var monthVisibleEditor = new CheckBox { Content = "Show month", IsChecked = state.MonthVisible };
        var yearVisibleEditor = new CheckBox { Content = "Show year", IsChecked = state.YearVisible };
        var dayFormatEditor = new TextBox { Text = state.DayFormat };
        var monthFormatEditor = new TextBox { Text = state.MonthFormat };
        var yearFormatEditor = new TextBox { Text = state.YearFormat };

        var displayDateEditor = new TextBox { Text = state.DisplayDate, Watermark = "yyyy-MM-dd" };
        var displayDateStartEditor = new TextBox
        {
            Text = state.DisplayDateStart,
            Watermark = "Optional: yyyy-MM-dd",
        };
        var displayDateEndEditor = new TextBox
        {
            Text = state.DisplayDateEnd,
            Watermark = "Optional: yyyy-MM-dd",
        };
        var firstDayEditor = new ComboBox
        {
            ItemsSource = DesignerDateTimeRuntime.DayOfWeekNames,
            SelectedItem = state.FirstDayOfWeek,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var selectedDateFormatEditor = new ComboBox
        {
            ItemsSource = DesignerDateTimeRuntime.CalendarDateFormatNames,
            SelectedItem = state.SelectedDateFormat,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var customDateFormatEditor = new TextBox { Text = state.CustomDateFormatString };
        var todayHighlightedEditor = new CheckBox
        {
            Content = "Highlight today",
            IsChecked = state.IsTodayHighlighted,
        };
        var calendarSelectionModeEditor = new ComboBox
        {
            ItemsSource = DesignerDateTimeRuntime.CalendarSelectionModeNames,
            SelectedItem = state.CalendarSelectionMode,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var calendarDisplayModeEditor = new ComboBox
        {
            ItemsSource = DesignerDateTimeRuntime.CalendarDisplayModeNames,
            SelectedItem = state.CalendarDisplayMode,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var allowTapRangeEditor = new CheckBox
        {
            Content = "Allow tap range selection",
            IsChecked = state.AllowTapRangeSelection,
        };
        var calendarControlSelectedDateEditor = new TextBox
        {
            Text = state.SelectedDate,
            Watermark = "Optional: yyyy-MM-dd",
        };
        var calendarControlDisplayDateEditor = new TextBox
        {
            Text = state.DisplayDate,
            Watermark = "yyyy-MM-dd",
        };
        var calendarControlDisplayStartEditor = new TextBox
        {
            Text = state.DisplayDateStart,
            Watermark = "Optional: yyyy-MM-dd",
        };
        var calendarControlDisplayEndEditor = new TextBox
        {
            Text = state.DisplayDateEnd,
            Watermark = "Optional: yyyy-MM-dd",
        };
        var calendarControlFirstDayEditor = new ComboBox
        {
            ItemsSource = DesignerDateTimeRuntime.DayOfWeekNames,
            SelectedItem = state.FirstDayOfWeek,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var calendarControlTodayHighlightedEditor = new CheckBox
        {
            Content = "Highlight today",
            IsChecked = state.IsTodayHighlighted,
        };
        var watermarkEditor = new TextBox { Text = state.Watermark };
        var floatingWatermarkEditor = new CheckBox
        {
            Content = "Use floating watermark",
            IsChecked = state.UseFloatingWatermark,
        };
        var horizontalAlignmentEditor = new ComboBox
        {
            ItemsSource = DesignerDateTimeRuntime.HorizontalAlignmentNames,
            SelectedItem = state.HorizontalContentAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var verticalAlignmentEditor = new ComboBox
        {
            ItemsSource = DesignerDateTimeRuntime.VerticalAlignmentNames,
            SelectedItem = state.VerticalContentAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        var selectedTimeEditor = new TextBox
        {
            Text = state.SelectedTime,
            Watermark = "Optional: HH:mm or HH:mm:ss",
        };
        var minuteIncrementEditor = new TextBox
        {
            Text = state.MinuteIncrement,
            Watermark = "1-59",
        };
        var secondIncrementEditor = new TextBox
        {
            Text = state.SecondIncrement,
            Watermark = "1-59",
        };
        var clockIdentifierEditor = new ComboBox
        {
            ItemsSource = DesignerDateTimeRuntime.ClockIdentifiers,
            SelectedItem = state.ClockIdentifier,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var useSecondsEditor = new CheckBox
        {
            Content = "Show seconds",
            IsChecked = state.UseSeconds,
        };

        void RefreshCustomFormat()
            => customDateFormatEditor.IsEnabled =
                selectedDateFormatEditor.SelectedItem?.ToString() ==
                nameof(CalendarDatePickerFormat.Custom);

        selectedDateFormatEditor.SelectionChanged += (_, _) => RefreshCustomFormat();
        RefreshCustomFormat();

        var datePickerFields = CreateFieldGrid(3);
        if (isDatePicker)
        {
            AddField(datePickerFields, "Selected date", selectedDateEditor, 0, 0);
        }

        AddField(datePickerFields, "Minimum year", minYearEditor, 0, 1);
        AddField(datePickerFields, "Maximum year", maxYearEditor, 1, 0);
        AddField(datePickerFields, "Day format", dayFormatEditor, 1, 1);
        AddField(datePickerFields, "Month format", monthFormatEditor, 2, 0);
        AddField(datePickerFields, "Year format", yearFormatEditor, 2, 1);
        var datePickerSwitches = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 18,
            LineSpacing = 8,
            Children = { dayVisibleEditor, monthVisibleEditor, yearVisibleEditor },
        };

        var calendarFields = CreateFieldGrid(6);
        if (isCalendarDatePicker)
        {
            AddField(calendarFields, "Selected date", selectedDateEditor, 0, 0);
        }

        AddField(calendarFields, "Displayed month", displayDateEditor, 0, 1);
        AddField(calendarFields, "Display range start", displayDateStartEditor, 1, 0);
        AddField(calendarFields, "Display range end", displayDateEndEditor, 1, 1);
        AddField(calendarFields, "First day of week", firstDayEditor, 2, 0);
        AddField(calendarFields, "Selected date format", selectedDateFormatEditor, 2, 1);
        AddField(calendarFields, "Custom date format", customDateFormatEditor, 3, 0);
        AddField(calendarFields, "Watermark", watermarkEditor, 3, 1);
        AddField(calendarFields, "Horizontal content alignment", horizontalAlignmentEditor, 4, 0);
        AddField(calendarFields, "Vertical content alignment", verticalAlignmentEditor, 4, 1);
        var calendarSwitches = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 18,
            LineSpacing = 8,
            Children = { todayHighlightedEditor, floatingWatermarkEditor },
        };
        Grid.SetRow(calendarSwitches, 5);
        Grid.SetColumnSpan(calendarSwitches, 2);
        calendarFields.Children.Add(calendarSwitches);

        var calendarControlFields = CreateFieldGrid(5);
        AddField(
            calendarControlFields,
            "Selected date",
            calendarControlSelectedDateEditor,
            0,
            0);
        AddField(
            calendarControlFields,
            "Displayed date",
            calendarControlDisplayDateEditor,
            0,
            1);
        AddField(
            calendarControlFields,
            "Display range start",
            calendarControlDisplayStartEditor,
            1,
            0);
        AddField(
            calendarControlFields,
            "Display range end",
            calendarControlDisplayEndEditor,
            1,
            1);
        AddField(
            calendarControlFields,
            "First day of week",
            calendarControlFirstDayEditor,
            2,
            0);
        AddField(
            calendarControlFields,
            "Selection mode",
            calendarSelectionModeEditor,
            2,
            1);
        AddField(
            calendarControlFields,
            "Display mode",
            calendarDisplayModeEditor,
            3,
            0);
        var calendarControlSwitches = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 18,
            LineSpacing = 8,
            Children =
            {
                calendarControlTodayHighlightedEditor,
                allowTapRangeEditor,
            },
        };
        Grid.SetRow(calendarControlSwitches, 4);
        Grid.SetColumnSpan(calendarControlSwitches, 2);
        calendarControlFields.Children.Add(calendarControlSwitches);

        var timePickerFields = CreateFieldGrid(3);
        AddField(timePickerFields, "Selected time", selectedTimeEditor, 0, 0);
        AddField(timePickerFields, "Clock", clockIdentifierEditor, 0, 1);
        AddField(timePickerFields, "Minute increment", minuteIncrementEditor, 1, 0);
        AddField(timePickerFields, "Second increment", secondIncrementEditor, 1, 1);
        Grid.SetRow(useSecondsEditor, 2);
        Grid.SetColumnSpan(useSecondsEditor, 2);
        timePickerFields.Children.Add(useSecondsEditor);

        Control editorContent;
        if (isDatePicker)
        {
            editorContent = new StackPanel
            {
                Spacing = 12,
                Children = { datePickerFields, datePickerSwitches },
            };
        }
        else if (isCalendarDatePicker)
        {
            editorContent = calendarFields;
        }
        else if (isCalendarControl)
        {
            editorContent = calendarControlFields;
        }
        else
        {
            editorContent = timePickerFields;
        }

        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Date & Time Input - {state.ControlName}",
            Width = 760,
            Height = isCalendarDatePicker ? 690
                : isCalendarControl ? 600
                : 520,
            MinWidth = 640,
            MinHeight = 460,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerDateTimeEditorInput(
                isCalendarControl
                    ? calendarControlSelectedDateEditor.Text ?? string.Empty
                    : selectedDateEditor.Text ?? string.Empty,
                minYearEditor.Text ?? string.Empty,
                maxYearEditor.Text ?? string.Empty,
                dayVisibleEditor.IsChecked == true,
                monthVisibleEditor.IsChecked == true,
                yearVisibleEditor.IsChecked == true,
                dayFormatEditor.Text ?? string.Empty,
                monthFormatEditor.Text ?? string.Empty,
                yearFormatEditor.Text ?? string.Empty,
                isCalendarControl
                    ? calendarControlDisplayDateEditor.Text ?? string.Empty
                    : displayDateEditor.Text ?? string.Empty,
                isCalendarControl
                    ? calendarControlDisplayStartEditor.Text ?? string.Empty
                    : displayDateStartEditor.Text ?? string.Empty,
                isCalendarControl
                    ? calendarControlDisplayEndEditor.Text ?? string.Empty
                    : displayDateEndEditor.Text ?? string.Empty,
                isCalendarControl
                    ? calendarControlFirstDayEditor.SelectedItem?.ToString()
                        ?? string.Empty
                    : firstDayEditor.SelectedItem?.ToString() ?? string.Empty,
                isCalendarControl
                    ? calendarControlTodayHighlightedEditor.IsChecked == true
                    : todayHighlightedEditor.IsChecked == true,
                selectedDateFormatEditor.SelectedItem?.ToString() ?? string.Empty,
                customDateFormatEditor.Text ?? string.Empty,
                watermarkEditor.Text ?? string.Empty,
                floatingWatermarkEditor.IsChecked == true,
                horizontalAlignmentEditor.SelectedItem?.ToString() ?? string.Empty,
                verticalAlignmentEditor.SelectedItem?.ToString() ?? string.Empty,
                selectedTimeEditor.Text ?? string.Empty,
                minuteIncrementEditor.Text ?? string.Empty,
                secondIncrementEditor.Text ?? string.Empty,
                clockIdentifierEditor.SelectedItem?.ToString() ?? string.Empty,
                useSecondsEditor.IsChecked == true,
                calendarSelectionModeEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                calendarDisplayModeEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                allowTapRangeEditor.IsChecked == true);
            if (!Vm.SetSelectedDateTimeProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = $"Configure {state.ControlKind} values, limits, and presentation behavior.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                new ScrollViewer { Content = editorContent },
                errorText,
                buttons,
            },
        };
        Grid.SetRow(content.Children[1], 1);
        Grid.SetRow(errorText, 2);
        Grid.SetRow(buttons, 3);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static Grid CreateFieldGrid(int rowCount)
            => new()
            {
                ColumnDefinitions = new ColumnDefinitions("*,*"),
                RowDefinitions = new RowDefinitions(
                    string.Join(",", Enumerable.Repeat("Auto", rowCount))),
                ColumnSpacing = 12,
                RowSpacing = 10,
            };

        static void AddField(Grid owner, string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            owner.Children.Add(field);
        }
    }

    private async Task ShowColorPickerPropertiesDialogAsync(ColorPickerEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var colorEditor = new TextBox
        {
            Text = state.Color,
            Watermark = "#AARRGGBB or named color",
        };
        var colorModelEditor = CreateCombo(
            DesignerColorPickerRuntime.ColorModelNames,
            state.ColorModel);
        var spectrumComponentsEditor = CreateCombo(
            DesignerColorPickerRuntime.ColorSpectrumComponentsNames,
            state.ColorSpectrumComponents);
        var spectrumShapeEditor = CreateCombo(
            DesignerColorPickerRuntime.ColorSpectrumShapeNames,
            state.ColorSpectrumShape);
        var alphaPositionEditor = CreateCombo(
            DesignerColorPickerRuntime.AlphaComponentPositionNames,
            state.HexInputAlphaPosition);
        var paletteColumnCountEditor = new TextBox
        {
            Text = state.PaletteColumnCount,
            Watermark = "1-32",
        };

        var accentColorsEditor = new CheckBox
        {
            Content = "Show accent colors",
            IsChecked = state.IsAccentColorsVisible,
        };
        var alphaEnabledEditor = new CheckBox
        {
            Content = "Enable alpha",
            IsChecked = state.IsAlphaEnabled,
        };
        var alphaVisibleEditor = new CheckBox
        {
            Content = "Show alpha",
            IsChecked = state.IsAlphaVisible,
        };
        var previewVisibleEditor = new CheckBox
        {
            Content = "Show color preview",
            IsChecked = state.IsColorPreviewVisible,
        };
        var paletteVisibleEditor = new CheckBox
        {
            Content = "Show palette",
            IsChecked = state.IsColorPaletteVisible,
        };
        var spectrumVisibleEditor = new CheckBox
        {
            Content = "Show spectrum",
            IsChecked = state.IsColorSpectrumVisible,
        };
        var spectrumSliderVisibleEditor = new CheckBox
        {
            Content = "Show spectrum slider",
            IsChecked = state.IsColorSpectrumSliderVisible,
        };
        var componentSliderVisibleEditor = new CheckBox
        {
            Content = "Show component slider",
            IsChecked = state.IsComponentSliderVisible,
        };
        var componentsVisibleEditor = new CheckBox
        {
            Content = "Show color components",
            IsChecked = state.IsColorComponentsVisible,
        };
        var modelVisibleEditor = new CheckBox
        {
            Content = "Show color model",
            IsChecked = state.IsColorModelVisible,
        };
        var componentTextVisibleEditor = new CheckBox
        {
            Content = "Show component inputs",
            IsChecked = state.IsComponentTextInputVisible,
        };
        var hexVisibleEditor = new CheckBox
        {
            Content = "Show hex input",
            IsChecked = state.IsHexInputVisible,
        };

        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField(fields, "Color", colorEditor, 0, 0);
        AddField(fields, "Color model", colorModelEditor, 0, 1);
        AddField(fields, "Hex alpha position", alphaPositionEditor, 0, 2);
        AddField(fields, "Spectrum components", spectrumComponentsEditor, 1, 0);
        AddField(fields, "Spectrum shape", spectrumShapeEditor, 1, 1);
        AddField(fields, "Palette columns", paletteColumnCountEditor, 1, 2);

        var appearanceOptions = CreateOptionPanel(
            accentColorsEditor,
            alphaEnabledEditor,
            alphaVisibleEditor,
            previewVisibleEditor);
        var spectrumOptions = CreateOptionPanel(
            paletteVisibleEditor,
            spectrumVisibleEditor,
            spectrumSliderVisibleEditor,
            componentSliderVisibleEditor);
        var inputOptions = CreateOptionPanel(
            componentsVisibleEditor,
            modelVisibleEditor,
            componentTextVisibleEditor,
            hexVisibleEditor);

        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit ColorPicker - {state.ControlName}",
            Width = 860,
            Height = 650,
            MinWidth = 720,
            MinHeight = 560,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerColorPickerEditorInput(
                colorEditor.Text ?? string.Empty,
                colorModelEditor.SelectedItem?.ToString() ?? string.Empty,
                spectrumComponentsEditor.SelectedItem?.ToString() ?? string.Empty,
                spectrumShapeEditor.SelectedItem?.ToString() ?? string.Empty,
                alphaPositionEditor.SelectedItem?.ToString() ?? string.Empty,
                accentColorsEditor.IsChecked == true,
                alphaEnabledEditor.IsChecked == true,
                alphaVisibleEditor.IsChecked == true,
                componentsVisibleEditor.IsChecked == true,
                modelVisibleEditor.IsChecked == true,
                paletteVisibleEditor.IsChecked == true,
                previewVisibleEditor.IsChecked == true,
                spectrumVisibleEditor.IsChecked == true,
                spectrumSliderVisibleEditor.IsChecked == true,
                componentSliderVisibleEditor.IsChecked == true,
                componentTextVisibleEditor.IsChecked == true,
                hexVisibleEditor.IsChecked == true,
                paletteColumnCountEditor.Text ?? string.Empty);
            if (!Vm.SetSelectedColorPickerProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,*,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Edit the color value and the ColorPicker flyout sections. PaletteColors is kept as a collection and is not changed by this editor.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                new TextBlock { Text = "Preview and alpha" },
                appearanceOptions,
                spectrumOptions,
                inputOptions,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(content.Children[2], 2);
        Grid.SetRow(appearanceOptions, 3);
        Grid.SetRow(spectrumOptions, 4);
        Grid.SetRow(inputOptions, 5);
        Grid.SetRow(errorText, 6);
        Grid.SetRow(buttons, 7);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static ComboBox CreateCombo(IEnumerable<string> items, string selected)
            => new()
            {
                ItemsSource = items,
                SelectedItem = selected,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

        static WrapPanel CreateOptionPanel(params Control[] options)
        {
            var panel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                ItemSpacing = 18,
                LineSpacing = 8,
            };
            foreach (var option in options)
            {
                panel.Children.Add(option);
            }

            return panel;
        }

        static void AddField(Grid owner, string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            owner.Children.Add(field);
        }
    }

    private async Task ShowAutoCompleteBoxPropertiesDialogAsync(AutoCompleteBoxEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var textEditor = new TextBox { Text = state.Text };
        var watermarkEditor = new TextBox { Text = state.Watermark, Watermark = "Search hint" };
        var prefixEditor = new TextBox { Text = state.MinimumPrefixLength, Watermark = "0 or greater" };
        var delayEditor = new TextBox { Text = state.MinimumPopulateDelay, Watermark = "milliseconds or hh:mm:ss" };
        var filterModeEditor = new ComboBox
        {
            ItemsSource = DesignerAutoCompleteBoxRuntime.FilterModeNames,
            SelectedItem = state.FilterMode,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var maxHeightEditor = new TextBox { Text = state.MaxDropDownHeight, Watermark = "Infinity or pixels" };
        var completionEditor = new CheckBox
        {
            Content = "Enable text completion",
            IsChecked = state.IsTextCompletionEnabled,
        };
        var openEditor = new CheckBox
        {
            Content = "Open drop-down initially",
            IsChecked = state.IsDropDownOpen,
        };

        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField(fields, "Text", textEditor, 0, 0);
        AddField(fields, "Watermark", watermarkEditor, 0, 1);
        AddField(fields, "Filter mode", filterModeEditor, 0, 2);
        AddField(fields, "Minimum prefix", prefixEditor, 1, 0);
        AddField(fields, "Populate delay", delayEditor, 1, 1);
        AddField(fields, "Max drop-down height", maxHeightEditor, 1, 2);

        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit AutoCompleteBox - {state.ControlName}",
            Width = 760,
            Height = 430,
            MinWidth = 640,
            MinHeight = 360,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerAutoCompleteBoxEditorInput(
                textEditor.Text ?? string.Empty,
                watermarkEditor.Text ?? string.Empty,
                completionEditor.IsChecked == true,
                prefixEditor.Text ?? string.Empty,
                delayEditor.Text ?? string.Empty,
                filterModeEditor.SelectedItem?.ToString() ?? string.Empty,
                maxHeightEditor.Text ?? string.Empty,
                openEditor.IsChecked == true);
            if (!Vm.SetSelectedAutoCompleteBoxProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var options = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 18,
            LineSpacing = 8,
            Children = { completionEditor, openEditor },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Configure static autocomplete behavior. AsyncPopulator and selector delegates remain code-owned and are not synthesized by the designer.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                options,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(options, 2);
        Grid.SetRow(errorText, 3);
        Grid.SetRow(buttons, 4);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static void AddField(Grid owner, string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            owner.Children.Add(field);
        }
    }

    private async Task ShowTogglePropertiesDialogAsync(ToggleEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var isRadioButton = state.ControlKind == nameof(DesignerToggleControlKind.RadioButton);
        var isToggleSwitch = state.ControlKind == nameof(DesignerToggleControlKind.ToggleSwitch);
        var contentEditor = new TextBox { Text = state.Content };
        var stateEditor = new ComboBox
        {
            ItemsSource = DesignerToggleRuntime.StateNames,
            SelectedItem = state.State,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var threeStateEditor = new CheckBox
        {
            Content = "Enable indeterminate state",
            IsChecked = state.IsThreeState,
        };
        var clickModeEditor = new ComboBox
        {
            ItemsSource = DesignerToggleRuntime.ClickModeNames,
            SelectedItem = state.ClickMode,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var groupNameEditor = new TextBox
        {
            Text = state.GroupName,
            IsEnabled = isRadioButton,
            Watermark = isRadioButton ? "Radio group name" : "RadioButton only",
        };
        var onContentEditor = new TextBox
        {
            Text = state.OnContent,
            IsEnabled = isToggleSwitch,
            Watermark = isToggleSwitch ? "Checked label" : "ToggleSwitch only",
        };
        var offContentEditor = new TextBox
        {
            Text = state.OffContent,
            IsEnabled = isToggleSwitch,
            Watermark = isToggleSwitch ? "Unchecked label" : "ToggleSwitch only",
        };
        var horizontalAlignmentEditor = new ComboBox
        {
            ItemsSource = DesignerToggleRuntime.HorizontalAlignmentNames,
            SelectedItem = state.HorizontalContentAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var verticalAlignmentEditor = new ComboBox
        {
            ItemsSource = DesignerToggleRuntime.VerticalAlignmentNames,
            SelectedItem = state.VerticalContentAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField(fields, "Content", contentEditor, 0, 0);
        AddField(fields, "State", stateEditor, 0, 1);
        AddField(fields, "Click mode", clickModeEditor, 1, 0);
        AddField(fields, "Radio group", groupNameEditor, 1, 1);
        AddField(fields, "On content", onContentEditor, 2, 0);
        AddField(fields, "Off content", offContentEditor, 2, 1);
        AddField(fields, "Horizontal content alignment", horizontalAlignmentEditor, 3, 0);
        AddField(fields, "Vertical content alignment", verticalAlignmentEditor, 3, 1);

        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Toggle & Choice Behavior - {state.ControlName}",
            Width = 720,
            Height = 590,
            MinWidth = 620,
            MinHeight = 520,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerToggleEditorInput(
                contentEditor.Text ?? string.Empty,
                stateEditor.SelectedItem?.ToString() ?? string.Empty,
                threeStateEditor.IsChecked == true,
                clickModeEditor.SelectedItem?.ToString() ?? string.Empty,
                groupNameEditor.Text ?? string.Empty,
                onContentEditor.Text ?? string.Empty,
                offContentEditor.Text ?? string.Empty,
                horizontalAlignmentEditor.SelectedItem?.ToString() ?? string.Empty,
                verticalAlignmentEditor.SelectedItem?.ToString() ?? string.Empty);
            if (!Vm.SetSelectedToggleProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = $"Configure {state.ControlKind} state, activation, and type-specific content.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                threeStateEditor,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(threeStateEditor, 2);
        Grid.SetRow(errorText, 4);
        Grid.SetRow(buttons, 5);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static void AddField(Grid owner, string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            owner.Children.Add(field);
        }
    }

    private async Task ShowContainerBehaviorPropertiesDialogAsync(
        ContainerBehaviorEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var isExpander =
            state.ControlKind == nameof(DesignerContainerBehaviorKind.Expander);
        var headerEditor = new TextBox { Text = state.Header };
        var expandedEditor = new CheckBox
        {
            Content = "Expanded in the design and at startup",
            IsChecked = state.IsExpanded,
        };
        var expandDirectionEditor = new ComboBox
        {
            ItemsSource = DesignerContainerBehaviorRuntime.ExpandDirectionNames,
            SelectedItem = state.ExpandDirection,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var horizontalContentAlignmentEditor = new ComboBox
        {
            ItemsSource =
                DesignerContainerBehaviorRuntime.HorizontalAlignmentNames,
            SelectedItem = state.HorizontalContentAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var verticalContentAlignmentEditor = new ComboBox
        {
            ItemsSource =
                DesignerContainerBehaviorRuntime.VerticalAlignmentNames,
            SelectedItem = state.VerticalContentAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var horizontalScrollBarEditor = new ComboBox
        {
            ItemsSource =
                DesignerContainerBehaviorRuntime.ScrollBarVisibilityNames,
            SelectedItem = state.HorizontalScrollBarVisibility,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var verticalScrollBarEditor = new ComboBox
        {
            ItemsSource =
                DesignerContainerBehaviorRuntime.ScrollBarVisibilityNames,
            SelectedItem = state.VerticalScrollBarVisibility,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var horizontalSnapTypeEditor = new ComboBox
        {
            ItemsSource = DesignerContainerBehaviorRuntime.SnapPointsTypeNames,
            SelectedItem = state.HorizontalSnapPointsType,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var verticalSnapTypeEditor = new ComboBox
        {
            ItemsSource = DesignerContainerBehaviorRuntime.SnapPointsTypeNames,
            SelectedItem = state.VerticalSnapPointsType,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var horizontalSnapAlignmentEditor = new ComboBox
        {
            ItemsSource =
                DesignerContainerBehaviorRuntime.SnapPointsAlignmentNames,
            SelectedItem = state.HorizontalSnapPointsAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var verticalSnapAlignmentEditor = new ComboBox
        {
            ItemsSource =
                DesignerContainerBehaviorRuntime.SnapPointsAlignmentNames,
            SelectedItem = state.VerticalSnapPointsAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var autoHideEditor = new CheckBox
        {
            Content = "Auto-hide scrollbars",
            IsChecked = state.AllowAutoHide,
        };
        var chainingEditor = new CheckBox
        {
            Content = "Chain scrolling to parent",
            IsChecked = state.IsScrollChainingEnabled,
        };
        var deferredEditor = new CheckBox
        {
            Content = "Use deferred scrolling",
            IsChecked = state.IsDeferredScrollingEnabled,
        };
        var bringIntoViewEditor = new CheckBox
        {
            Content = "Bring focused child into view",
            IsChecked = state.BringIntoViewOnFocusChange,
        };

        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        var switches = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 18,
            LineSpacing = 8,
        };
        if (isExpander)
        {
            AddField(fields, "Header", headerEditor, 0, 0);
            AddField(fields, "Expand direction", expandDirectionEditor, 0, 1);
            AddField(
                fields,
                "Horizontal content alignment",
                horizontalContentAlignmentEditor,
                1,
                0);
            AddField(
                fields,
                "Vertical content alignment",
                verticalContentAlignmentEditor,
                1,
                1);
            switches.Children.Add(expandedEditor);
        }
        else
        {
            AddField(
                fields,
                "Horizontal scrollbar",
                horizontalScrollBarEditor,
                0,
                0);
            AddField(
                fields,
                "Vertical scrollbar",
                verticalScrollBarEditor,
                0,
                1);
            AddField(
                fields,
                "Horizontal snap type",
                horizontalSnapTypeEditor,
                1,
                0);
            AddField(
                fields,
                "Vertical snap type",
                verticalSnapTypeEditor,
                1,
                1);
            AddField(
                fields,
                "Horizontal snap alignment",
                horizontalSnapAlignmentEditor,
                2,
                0);
            AddField(
                fields,
                "Vertical snap alignment",
                verticalSnapAlignmentEditor,
                2,
                1);
            switches.Children.Add(autoHideEditor);
            switches.Children.Add(chainingEditor);
            switches.Children.Add(deferredEditor);
            switches.Children.Add(bringIntoViewEditor);
        }

        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Disclosure & Scrolling - {state.ControlName}",
            Width = 740,
            Height = 590,
            MinWidth = 640,
            MinHeight = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerContainerBehaviorEditorInput(
                headerEditor.Text ?? string.Empty,
                expandedEditor.IsChecked == true,
                expandDirectionEditor.SelectedItem?.ToString() ?? string.Empty,
                horizontalContentAlignmentEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                verticalContentAlignmentEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                horizontalScrollBarEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                verticalScrollBarEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                autoHideEditor.IsChecked == true,
                chainingEditor.IsChecked == true,
                deferredEditor.IsChecked == true,
                bringIntoViewEditor.IsChecked == true,
                horizontalSnapTypeEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                verticalSnapTypeEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                horizontalSnapAlignmentEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                verticalSnapAlignmentEditor.SelectedItem?.ToString()
                    ?? string.Empty);
            if (!Vm.SetSelectedContainerBehaviorProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = isExpander
                        ? "Configure disclosure state, direction, and content alignment without changing the assigned child."
                        : "Configure scrollbar, chaining, focus, and snap behavior without changing the assigned child.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                switches,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(switches, 2);
        Grid.SetRow(errorText, 4);
        Grid.SetRow(buttons, 5);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static void AddField(
            Grid owner,
            string label,
            Control editor,
            int row,
            int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            owner.Children.Add(field);
        }
    }

    private async Task ShowImagePropertiesDialogAsync(ImageEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var sourceEditor = new TextBox
        {
            Text = state.Source,
            Watermark = "Local file path or file URI",
        };
        var browseButton = new Button { Content = "Browse...", MinWidth = 88 };
        browseButton.Click += async (_, _) =>
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Choose image",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Image files")
                    {
                        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp"]
                    }
                ]
            });
            if (files.Count > 0 && files[0].Path.IsFile)
            {
                sourceEditor.Text = files[0].Path.AbsoluteUri;
            }
        };
        var clearButton = new Button { Content = "Clear", MinWidth = 72 };
        clearButton.Click += (_, _) => sourceEditor.Text = string.Empty;
        var sourceButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { browseButton, clearButton },
        };
        var sourceField = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 8,
            Children = { sourceEditor, sourceButtons },
        };
        Grid.SetColumn(sourceButtons, 1);

        var stretchEditor = CreateComboBox(
            DesignerImageRuntime.StretchNames,
            state.Stretch);
        var stretchDirectionEditor = CreateComboBox(
            DesignerImageRuntime.StretchDirectionNames,
            state.StretchDirection);
        var interpolationEditor = CreateComboBox(
            DesignerImageRuntime.BitmapInterpolationModeNames,
            state.BitmapInterpolationMode);
        var edgeModeEditor = CreateComboBox(
            DesignerImageRuntime.EdgeModeNames,
            state.EdgeMode);
        var blendingEditor = CreateComboBox(
            DesignerImageRuntime.BitmapBlendingModeNames,
            state.BitmapBlendingMode);
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField(fields, "Source", sourceField, 0, 0, 2);
        AddField(fields, "Stretch", stretchEditor, 1, 0);
        AddField(fields, "Stretch direction", stretchDirectionEditor, 1, 1);
        AddField(fields, "Bitmap interpolation", interpolationEditor, 2, 0);
        AddField(fields, "Edge mode", edgeModeEditor, 2, 1);
        AddField(fields, "Bitmap blending", blendingEditor, 3, 0, 2);

        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Image Source & Rendering - {state.ControlName}",
            Width = 740,
            Height = 520,
            MinWidth = 640,
            MinHeight = 460,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerImageEditorInput(
                sourceEditor.Text ?? string.Empty,
                stretchEditor.SelectedItem?.ToString() ?? string.Empty,
                stretchDirectionEditor.SelectedItem?.ToString() ?? string.Empty,
                interpolationEditor.SelectedItem?.ToString() ?? string.Empty,
                edgeModeEditor.SelectedItem?.ToString() ?? string.Empty,
                blendingEditor.SelectedItem?.ToString() ?? string.Empty);
            if (!Vm.SetSelectedImageProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Choose a local image and configure scaling, interpolation, edge, and blending behavior.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(errorText, 3);
        Grid.SetRow(buttons, 4);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static ComboBox CreateComboBox(
            IReadOnlyList<string> items,
            string selected)
            => new()
            {
                ItemsSource = items,
                SelectedItem = selected,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

        static void AddField(
            Grid owner,
            string label,
            Control editor,
            int row,
            int column,
            int columnSpan = 1)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            Grid.SetColumnSpan(field, columnSpan);
            owner.Children.Add(field);
        }
    }

    private async Task ShowButtonPropertiesDialogAsync(ButtonEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var contentEditor = new TextBox { Text = state.Content };
        var clickModeEditor = new ComboBox
        {
            ItemsSource = DesignerButtonRuntime.ClickModeNames,
            SelectedItem = state.ClickMode,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var hotKeyEditor = new TextBox
        {
            Text = state.HotKey,
            Watermark = "For example Ctrl+S",
        };
        var commandParameterEditor = new TextBox
        {
            Text = state.CommandParameter,
            Watermark = "Optional static parameter",
        };
        var clickHandlerEditor = new TextBox
        {
            Text = state.ClickHandler,
            Watermark = "For example SaveButton_Click",
        };
        var defaultEditor = new CheckBox
        {
            Content = "Default action for the host Window",
            IsChecked = state.IsDefault,
        };
        var cancelEditor = new CheckBox
        {
            Content = "Cancel action for the host Window",
            IsChecked = state.IsCancel,
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField(fields, "Content", contentEditor, 0, 0);
        AddField(fields, "Click mode", clickModeEditor, 0, 1);
        AddField(fields, "HotKey", hotKeyEditor, 1, 0);
        AddField(
            fields,
            "Command parameter",
            commandParameterEditor,
            1,
            1);
        AddField(fields, "Click handler", clickHandlerEditor, 2, 0, 2);

        var switches = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 18,
            LineSpacing = 8,
            Children = { defaultEditor, cancelEditor },
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Button Actions & Commands - {state.ControlName}",
            Width = 740,
            Height = 580,
            MinWidth = 640,
            MinHeight = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerButtonEditorInput(
                contentEditor.Text ?? string.Empty,
                clickModeEditor.SelectedItem?.ToString() ?? string.Empty,
                hotKeyEditor.Text ?? string.Empty,
                defaultEditor.IsChecked == true,
                cancelEditor.IsChecked == true,
                commandParameterEditor.Text ?? string.Empty,
                clickHandlerEditor.Text ?? string.Empty);
            if (!Vm.SetSelectedButtonProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var bindingHint = new TextBlock
        {
            Text = "Use Edit Bindings... to connect Command or CommandParameter to the application ViewModel. A Click handler and Command may be declared together.",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Opacity = 0.72,
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions =
                new RowDefinitions("Auto,Auto,Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Configure pointer activation, keyboard activation, Window default/cancel behavior, command data, and the generated Click event declaration.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                switches,
                bindingHint,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(switches, 2);
        Grid.SetRow(bindingHint, 3);
        Grid.SetRow(errorText, 5);
        Grid.SetRow(buttons, 6);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static void AddField(
            Grid owner,
            string label,
            Control editor,
            int row,
            int column,
            int columnSpan = 1)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            Grid.SetColumnSpan(field, columnSpan);
            owner.Children.Add(field);
        }
    }

    private async Task ShowLayoutPropertiesDialogAsync(LayoutEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var marginEditor = new TextBox { Text = state.Margin };
        var paddingEditor = new TextBox
        {
            Text = state.Padding,
            IsEnabled = state.SupportsPadding,
            Watermark = state.SupportsPadding ? null : "Not supported by this control",
        };
        var horizontalEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<HorizontalAlignment>(),
            SelectedItem = state.HorizontalAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var verticalEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<VerticalAlignment>(),
            SelectedItem = state.VerticalAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var minWidthEditor = new TextBox { Text = state.MinWidth };
        var minHeightEditor = new TextBox { Text = state.MinHeight };
        var maxWidthEditor = new TextBox
        {
            Text = state.MaxWidth,
            Watermark = "No maximum",
        };
        var maxHeightEditor = new TextBox
        {
            Text = state.MaxHeight,
            Watermark = "No maximum",
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Layout Properties - {state.ControlName}",
            Width = 620,
            Height = 500,
            MinWidth = 520,
            MinHeight = 430,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            if (!Vm.SetSelectedLayoutProperties(
                    marginEditor.Text ?? string.Empty,
                    paddingEditor.Text ?? string.Empty,
                    horizontalEditor.SelectedItem?.ToString() ?? string.Empty,
                    verticalEditor.SelectedItem?.ToString() ?? string.Empty,
                    minWidthEditor.Text ?? string.Empty,
                    minHeightEditor.Text ?? string.Empty,
                    maxWidthEditor.Text ?? string.Empty,
                    maxHeightEditor.Text ?? string.Empty))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField("Margin", marginEditor, 0, 0);
        AddField("Padding", paddingEditor, 0, 1);
        AddField("Horizontal alignment", horizontalEditor, 1, 0);
        AddField("Vertical alignment", verticalEditor, 1, 1);
        AddField("Minimum width", minWidthEditor, 2, 0);
        AddField("Minimum height", minHeightEditor, 2, 1);
        AddField("Maximum width", maxWidthEditor, 3, 0);
        AddField("Maximum height", maxHeightEditor, 3, 1);

        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Thickness accepts 1, 2, or 4 comma-separated values. Leave a maximum blank for no limit.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(errorText, 2);
        Grid.SetRow(buttons, 3);
        dialog.Content = content;
        await dialog.ShowDialog(this);
        return;

        void AddField(string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            fields.Children.Add(field);
        }
    }

    private async Task ShowRootPropertiesDialogAsync(RootEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var rootKindEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<DesignerRootKind>(),
            SelectedItem = state.RootKind,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var titleEditor = new TextBox
        {
            Text = state.Title,
            Watermark = "Application window title",
        };
        var canResizeEditor = new CheckBox
        {
            Content = "Allow the user to resize the window",
            IsChecked = state.CanResize,
        };
        var startupEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<DesignerWindowStartupLocation>(),
            SelectedItem = state.StartupLocation,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var minWidthEditor = new TextBox { Text = state.MinWidth };
        var minHeightEditor = new TextBox { Text = state.MinHeight };
        var maxWidthEditor = new TextBox
        {
            Text = state.MaxWidth,
            Watermark = "No maximum",
        };
        var maxHeightEditor = new TextBox
        {
            Text = state.MaxHeight,
            Watermark = "No maximum",
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = "Edit Document Root Properties",
            Width = 620,
            Height = 520,
            MinWidth = 520,
            MinHeight = 460,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            if (!Vm.SetRootProperties(
                    rootKindEditor.SelectedItem?.ToString() ?? string.Empty,
                    titleEditor.Text ?? string.Empty,
                    canResizeEditor.IsChecked == true,
                    startupEditor.SelectedItem?.ToString() ?? string.Empty,
                    minWidthEditor.Text ?? string.Empty,
                    minHeightEditor.Text ?? string.Empty,
                    maxWidthEditor.Text ?? string.Empty,
                    maxHeightEditor.Text ?? string.Empty))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField("Root type", rootKindEditor, 0, 0);
        AddField("Window startup location", startupEditor, 0, 1);
        AddField("Window title", titleEditor, 1, 0, 2);
        AddField("Minimum width", minWidthEditor, 2, 0);
        AddField("Minimum height", minHeightEditor, 2, 1);
        AddField("Maximum width", maxWidthEditor, 3, 0);
        AddField("Maximum height", maxHeightEditor, 3, 1);

        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Full AXAML saves with the selected root type. Window-only settings are disabled for UserControl.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                canResizeEditor,
                fields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(canResizeEditor, 1);
        Grid.SetRow(fields, 2);
        Grid.SetRow(errorText, 3);
        Grid.SetRow(buttons, 4);
        dialog.Content = content;

        rootKindEditor.SelectionChanged += (_, _) => UpdateWindowFields();
        UpdateWindowFields();
        await dialog.ShowDialog(this);
        return;

        void UpdateWindowFields()
        {
            var isWindow = string.Equals(
                rootKindEditor.SelectedItem?.ToString(),
                nameof(DesignerRootKind.Window),
                StringComparison.Ordinal);
            titleEditor.IsEnabled = isWindow;
            canResizeEditor.IsEnabled = isWindow;
            startupEditor.IsEnabled = isWindow;
        }

        void AddField(string label, Control editor, int row, int column, int columnSpan = 1)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            Grid.SetColumnSpan(field, columnSpan);
            fields.Children.Add(field);
        }
    }

    private async Task ShowSampleDataEditorDialogAsync(string source)
    {
        if (Vm is null)
        {
            return;
        }

        var editor = new TextBox
        {
            Text = source,
            AcceptsReturn = true,
            AcceptsTab = true,
            FontFamily = new Avalonia.Media.FontFamily("Consolas"),
            FontSize = 13,
            TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(editor, ScrollBarVisibility.Auto);
        ScrollViewer.SetVerticalScrollBarVisibility(editor, ScrollBarVisibility.Auto);
        var resultText = new TextBlock
        {
            Text = "JSON properties resolve binding paths such as User.Name. Arrays can preview ItemsSource bindings.",
            Foreground = Avalonia.Media.Brushes.SlateGray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = "Edit Sample DataContext",
            Width = 760,
            Height = 620,
            MinWidth = 560,
            MinHeight = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var validateButton = new Button { Content = "Validate", MinWidth = 84 };
        validateButton.Click += (_, _) =>
        {
            var isValid = Vm.TryValidateSampleDataJson(editor.Text ?? string.Empty, out var result);
            resultText.Foreground = isValid
                ? Avalonia.Media.Brushes.SeaGreen
                : Avalonia.Media.Brushes.IndianRed;
            resultText.Text = result;
        };
        var clearButton = new Button
        {
            Content = "Clear Sample",
            MinWidth = 100,
            IsEnabled = Vm.HasSampleData,
        };
        clearButton.Click += (_, _) =>
        {
            if (!Vm.TrySetSampleDataJson(string.Empty, out var result))
            {
                resultText.Foreground = Avalonia.Media.Brushes.IndianRed;
                resultText.Text = result;
                return;
            }

            dialog.Close();
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            if (!Vm.TrySetSampleDataJson(editor.Text ?? string.Empty, out var result))
            {
                resultText.Foreground = Avalonia.Media.Brushes.IndianRed;
                resultText.Text = result;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, clearButton, validateButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            RowSpacing = 10,
            Children = { resultText, editor, buttons },
        };
        Grid.SetRow(editor, 1);
        Grid.SetRow(buttons, 2);
        dialog.Content = content;
        await dialog.ShowDialog(this);
    }

    private async Task ShowAxamlSourceEditorDialogAsync(string source)
    {
        if (Vm is null)
        {
            return;
        }

        var editor = new TextBox
        {
            Text = source,
            AcceptsReturn = true,
            AcceptsTab = true,
            FontFamily = new Avalonia.Media.FontFamily("Consolas"),
            FontSize = 13,
            TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(editor, ScrollBarVisibility.Auto);
        ScrollViewer.SetVerticalScrollBarVisibility(editor, ScrollBarVisibility.Auto);
        var resultText = new TextBlock
        {
            Text = "Edit the complete Window or UserControl AXAML, then validate or preview before applying.",
            Foreground = Avalonia.Media.Brushes.SlateGray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = "Edit AXAML Source",
            Width = 960,
            Height = 720,
            MinWidth = 640,
            MinHeight = 440,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var validateButton = new Button { Content = "Validate", MinWidth = 84 };
        validateButton.Click += (_, _) =>
        {
            var isValid = Vm.TryValidateAxamlSource(editor.Text ?? string.Empty, out var result);
            resultText.Foreground = isValid
                ? Avalonia.Media.Brushes.SeaGreen
                : Avalonia.Media.Brushes.IndianRed;
            resultText.Text = result;
        };
        var previewButton = new Button { Content = "Preview", MinWidth = 84 };
        previewButton.Click += (_, _) =>
        {
            if (!Vm.TryCreatePreviewDocumentFromAxaml(
                    editor.Text ?? string.Empty,
                    out var document,
                    out var result))
            {
                resultText.Foreground = Avalonia.Media.Brushes.IndianRed;
                resultText.Text = result;
                return;
            }

            resultText.Foreground = Avalonia.Media.Brushes.SeaGreen;
            resultText.Text = result;
            try
            {
                var preview = new PreviewWindow(document);
                preview.Show(dialog);
            }
            catch (Exception ex)
            {
                resultText.Foreground = Avalonia.Media.Brushes.IndianRed;
                resultText.Text = $"AXAML preview failed: {ex.Message}";
            }
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            if (!Vm.TryApplyAxamlSource(editor.Text ?? string.Empty, out var result))
            {
                resultText.Foreground = Avalonia.Media.Brushes.IndianRed;
                resultText.Text = result;
                return;
            }

            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, validateButton, previewButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            RowSpacing = 10,
            Children = { resultText, editor, buttons },
        };
        Grid.SetRow(editor, 1);
        Grid.SetRow(buttons, 2);
        dialog.Content = content;
        await dialog.ShowDialog(this);
    }

    private async Task<IReadOnlyList<string>?> ShowBindingEditorDialogAsync(BindingEditorState state)
    {
        var editor = new TextBox
        {
            Text = string.Join(Environment.NewLine, state.Lines),
            AcceptsReturn = true,
            MinHeight = 220,
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Bindings - {state.ControlName}",
            Width = 720,
            Height = 450,
            MinWidth = 520,
            MinHeight = 320,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var lines = (editor.Text ?? string.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n', StringSplitOptions.None)
                .ToList();
            if (!DesignerBindingRuntime.TryParseEditorLines(
                    state.TargetType,
                    lines,
                    out _,
                    out var error))
            {
                errorText.Text = error;
                return;
            }

            dialog.Close(lines);
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto,Auto"),
            RowSpacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = "One binding per line: Property | Path | Mode | Fallback. Mode and Fallback are optional.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                new TextBlock
                {
                    Text = $"Supported: {string.Join(", ", state.SupportedProperties)}",
                    Foreground = Avalonia.Media.Brushes.SlateGray,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                editor,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(content.Children[1], 1);
        Grid.SetRow(editor, 2);
        Grid.SetRow(errorText, 3);
        Grid.SetRow(buttons, 4);
        dialog.Content = content;
        return await dialog.ShowDialog<IReadOnlyList<string>?>(this);
    }

    private async Task<GridDefinitionOptions?> ShowGridDefinitionsDialogAsync(GridDefinitionEditorState state)
    {
        var rowsEditor = new TextBox { Text = state.RowDefinitions };
        var columnsEditor = new TextBox { Text = state.ColumnDefinitions };
        var showLinesEditor = new CheckBox
        {
            Content = "Show grid lines on the design surface and preview",
            IsChecked = state.ShowGridLines,
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Grid Definitions - {state.ControlName}",
            Width = 500,
            Height = 350,
            MinWidth = 400,
            MinHeight = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var rowDefinitions = rowsEditor.Text ?? string.Empty;
            var columnDefinitions = columnsEditor.Text ?? string.Empty;
            if (!DesignerGridDefinitionRuntime.TryParse(
                    rowDefinitions,
                    columnDefinitions,
                    out _,
                    out _,
                    out var error))
            {
                errorText.Text = error;
                return;
            }

            dialog.Close(new GridDefinitionOptions(
                rowDefinitions,
                columnDefinitions,
                showLinesEditor.IsChecked == true));
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Rows" },
                rowsEditor,
                new TextBlock { Text = "Columns" },
                columnsEditor,
                showLinesEditor,
                errorText,
            },
        };
        var help = new TextBlock
        {
            Text = "Use comma-separated Avalonia GridLength values such as Auto,*,2*,96. Leave a field empty for one implicit cell.",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            RowSpacing = 12,
            Children = { help, fields, buttons },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(buttons, 2);
        dialog.Content = content;

        return await dialog.ShowDialog<GridDefinitionOptions?>(this);
    }

    private async Task<GridCellAssignmentOptions?> ShowGridCellAssignmentDialogAsync(
        GridCellAssignmentEditorState state)
    {
        var parentSelector = new ComboBox
        {
            ItemsSource = state.Parents,
            SelectedItem = state.Parents.First(parent => string.Equals(
                parent.DisplayName,
                state.SelectedParentName,
                StringComparison.OrdinalIgnoreCase)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var rowEditor = new NumericUpDown { Minimum = 1, Value = state.GridRow + 1 };
        var columnEditor = new NumericUpDown { Minimum = 1, Value = state.GridColumn + 1 };
        var rowSpanEditor = new NumericUpDown { Minimum = 1, Value = state.GridRowSpan };
        var columnSpanEditor = new NumericUpDown { Minimum = 1, Value = state.GridColumnSpan };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Assign to Grid Cell - {state.ControlName}",
            Width = 460,
            Height = 440,
            MinWidth = 380,
            MinHeight = 380,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void UpdateLimits()
        {
            if (parentSelector.SelectedItem is not GridCellParentOption parent)
            {
                return;
            }

            rowEditor.Maximum = parent.RowCount;
            columnEditor.Maximum = parent.ColumnCount;
            rowSpanEditor.Maximum = parent.RowCount;
            columnSpanEditor.Maximum = parent.ColumnCount;
        }

        parentSelector.SelectionChanged += (_, _) => UpdateLimits();
        UpdateLimits();

        var applyButton = new Button { Content = "Assign", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            if (parentSelector.SelectedItem is not GridCellParentOption parent)
            {
                errorText.Text = "Choose a Grid.";
                return;
            }

            var row = (int)(rowEditor.Value ?? 1) - 1;
            var column = (int)(columnEditor.Value ?? 1) - 1;
            var rowSpan = (int)(rowSpanEditor.Value ?? 1);
            var columnSpan = (int)(columnSpanEditor.Value ?? 1);
            if (row + rowSpan > parent.RowCount || column + columnSpan > parent.ColumnCount)
            {
                errorText.Text = $"The cell span must fit within {parent.RowCount} row(s) and {parent.ColumnCount} column(s).";
                return;
            }

            dialog.Close(new GridCellAssignmentOptions(
                parent.DisplayName,
                row,
                column,
                rowSpan,
                columnSpan));
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Parent Grid" },
                parentSelector,
                new TextBlock { Text = "Row (1-based)" },
                rowEditor,
                new TextBlock { Text = "Column (1-based)" },
                columnEditor,
                new TextBlock { Text = "Row span" },
                rowSpanEditor,
                new TextBlock { Text = "Column span" },
                columnSpanEditor,
                errorText,
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<GridCellAssignmentOptions?>(this);
    }

    private async Task<StackPanelAssignmentOptions?> ShowStackPanelAssignmentDialogAsync(
        StackPanelAssignmentEditorState state)
    {
        var parentSelector = new ComboBox
        {
            ItemsSource = state.Parents,
            SelectedItem = state.Parents.First(parent => string.Equals(
                parent.DisplayName,
                state.SelectedParentName,
                StringComparison.OrdinalIgnoreCase)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var positionEditor = new NumericUpDown
        {
            Minimum = 1,
            Value = state.ItemIndex + 1,
        };
        var sizeEditor = new NumericUpDown
        {
            Minimum = 10,
            Maximum = 2000,
            Increment = 4,
            Value = (decimal)state.ItemSize,
        };
        var orientationHint = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.Gray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Assign to StackPanel - {state.ControlName}",
            Width = 440,
            Height = 350,
            MinWidth = 380,
            MinHeight = 320,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void UpdateParentState()
        {
            if (parentSelector.SelectedItem is not StackPanelParentOption parent)
            {
                return;
            }

            positionEditor.Maximum = parent.ChildCount + 1;
            orientationHint.Text = parent.Orientation == Orientation.Vertical
                ? "Item size controls Height. Width stretches to the StackPanel."
                : "Item size controls Width. Height stretches to the StackPanel.";
        }

        parentSelector.SelectionChanged += (_, _) => UpdateParentState();
        UpdateParentState();

        var assignButton = new Button { Content = "Assign", MinWidth = 84 };
        assignButton.Click += (_, _) =>
        {
            if (parentSelector.SelectedItem is not StackPanelParentOption parent)
            {
                return;
            }

            dialog.Close(new StackPanelAssignmentOptions(
                parent.DisplayName,
                (int)(positionEditor.Value ?? 1) - 1,
                (double)(sizeEditor.Value ?? 40)));
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, assignButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Parent StackPanel" },
                parentSelector,
                new TextBlock { Text = "Position (1-based)" },
                positionEditor,
                new TextBlock { Text = "Item size" },
                sizeEditor,
                orientationHint,
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<StackPanelAssignmentOptions?>(this);
    }

    private async Task<DockPanelAssignmentOptions?> ShowDockPanelAssignmentDialogAsync(
        DockPanelAssignmentEditorState state)
    {
        var parentSelector = new ComboBox
        {
            ItemsSource = state.Parents,
            SelectedItem = state.Parents.First(parent => string.Equals(
                parent.DisplayName,
                state.SelectedParentName,
                StringComparison.OrdinalIgnoreCase)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var positionEditor = new NumericUpDown
        {
            Minimum = 1,
            Value = state.ItemIndex + 1,
        };
        var dockSelector = new ComboBox
        {
            ItemsSource = Enum.GetValues<DesignerDockSide>(),
            SelectedItem = state.Dock,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var sizeEditor = new NumericUpDown
        {
            Minimum = 10,
            Maximum = 2000,
            Increment = 4,
            Value = (decimal)state.ItemSize,
        };
        var lastChildFillCheck = new CheckBox
        {
            Content = "Last child fills remaining space",
            IsChecked = state.LastChildFill,
        };
        var sizeHint = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.Gray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Assign to DockPanel - {state.ControlName}",
            Width = 460,
            Height = 440,
            MinWidth = 400,
            MinHeight = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void UpdateParentState()
        {
            if (parentSelector.SelectedItem is not DockPanelParentOption parent)
            {
                return;
            }

            positionEditor.Maximum = parent.ChildCount + 1;
            lastChildFillCheck.IsChecked = parent.LastChildFill;
        }

        void UpdateSizeHint()
        {
            sizeHint.Text = dockSelector.SelectedItem is DesignerDockSide.Top or DesignerDockSide.Bottom
                ? "Item size controls Height unless this is the LastChildFill item."
                : "Item size controls Width unless this is the LastChildFill item.";
        }

        parentSelector.SelectionChanged += (_, _) => UpdateParentState();
        dockSelector.SelectionChanged += (_, _) => UpdateSizeHint();
        UpdateParentState();
        UpdateSizeHint();

        var assignButton = new Button { Content = "Assign", MinWidth = 84 };
        assignButton.Click += (_, _) =>
        {
            if (parentSelector.SelectedItem is not DockPanelParentOption parent
                || dockSelector.SelectedItem is not DesignerDockSide dock)
            {
                return;
            }

            dialog.Close(new DockPanelAssignmentOptions(
                parent.DisplayName,
                (int)(positionEditor.Value ?? 1) - 1,
                dock,
                (double)(sizeEditor.Value ?? 40),
                lastChildFillCheck.IsChecked == true));
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, assignButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Parent DockPanel" },
                parentSelector,
                new TextBlock { Text = "Position (1-based)" },
                positionEditor,
                new TextBlock { Text = "Dock side" },
                dockSelector,
                new TextBlock { Text = "Item size" },
                sizeEditor,
                lastChildFillCheck,
                sizeHint,
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<DockPanelAssignmentOptions?>(this);
    }

    private async Task<WrapPanelAssignmentOptions?> ShowWrapPanelAssignmentDialogAsync(
        WrapPanelAssignmentEditorState state)
    {
        var parentSelector = new ComboBox
        {
            ItemsSource = state.Parents,
            SelectedItem = state.Parents.First(parent => string.Equals(
                parent.DisplayName,
                state.SelectedParentName,
                StringComparison.OrdinalIgnoreCase)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var positionEditor = new NumericUpDown
        {
            Minimum = 1,
            Value = state.ItemIndex + 1,
        };
        var layoutHint = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.Gray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Text = "Item size, spacing, orientation, and alignment are edited on the parent WrapPanel in the Property Inspector.",
        };
        var dialog = new Window
        {
            Title = $"Assign to WrapPanel - {state.ControlName}",
            Width = 460,
            Height = 320,
            MinWidth = 400,
            MinHeight = 290,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void UpdateParentState()
        {
            if (parentSelector.SelectedItem is WrapPanelParentOption parent)
            {
                positionEditor.Maximum = parent.ChildCount + 1;
            }
        }

        parentSelector.SelectionChanged += (_, _) => UpdateParentState();
        UpdateParentState();

        var assignButton = new Button { Content = "Assign", MinWidth = 84 };
        assignButton.Click += (_, _) =>
        {
            if (parentSelector.SelectedItem is not WrapPanelParentOption parent)
            {
                return;
            }

            dialog.Close(new WrapPanelAssignmentOptions(
                parent.DisplayName,
                (int)(positionEditor.Value ?? 1) - 1));
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, assignButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Parent WrapPanel" },
                parentSelector,
                new TextBlock { Text = "Position (1-based)" },
                positionEditor,
                layoutHint,
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<WrapPanelAssignmentOptions?>(this);
    }

    private async Task<UniformGridAssignmentOptions?> ShowUniformGridAssignmentDialogAsync(
        UniformGridAssignmentEditorState state)
    {
        var parentSelector = new ComboBox
        {
            ItemsSource = state.Parents,
            SelectedItem = state.Parents.First(parent => string.Equals(
                parent.DisplayName,
                state.SelectedParentName,
                StringComparison.OrdinalIgnoreCase)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var positionEditor = new NumericUpDown
        {
            Minimum = 1,
            Value = state.ItemIndex + 1,
        };
        var layoutHint = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.Gray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Text = "Rows, columns, first column, and spacing are edited on the parent UniformGrid in the Property Inspector.",
        };
        var dialog = new Window
        {
            Title = $"Assign to UniformGrid - {state.ControlName}",
            Width = 460,
            Height = 320,
            MinWidth = 400,
            MinHeight = 290,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void UpdateParentState()
        {
            if (parentSelector.SelectedItem is UniformGridParentOption parent)
            {
                positionEditor.Maximum = parent.ChildCount + 1;
            }
        }

        parentSelector.SelectionChanged += (_, _) => UpdateParentState();
        UpdateParentState();

        var assignButton = new Button { Content = "Assign", MinWidth = 84 };
        assignButton.Click += (_, _) =>
        {
            if (parentSelector.SelectedItem is not UniformGridParentOption parent)
            {
                return;
            }

            dialog.Close(new UniformGridAssignmentOptions(
                parent.DisplayName,
                (int)(positionEditor.Value ?? 1) - 1));
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, assignButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Parent UniformGrid" },
                parentSelector,
                new TextBlock { Text = "Position (1-based)" },
                positionEditor,
                layoutHint,
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<UniformGridAssignmentOptions?>(this);
    }

    private async Task<CanvasAssignmentOptions?> ShowCanvasAssignmentDialogAsync(
        CanvasAssignmentEditorState state)
    {
        var parentSelector = new ComboBox
        {
            ItemsSource = state.Parents,
            SelectedItem = state.Parents.First(parent => string.Equals(
                parent.DisplayName,
                state.SelectedParentName,
                StringComparison.OrdinalIgnoreCase)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var positionEditor = new NumericUpDown { Minimum = 1, Value = state.ItemIndex + 1 };
        var leftEditor = new NumericUpDown
        {
            Minimum = -5000,
            Maximum = 5000,
            Increment = 4,
            Value = (decimal)state.Left,
        };
        var topEditor = new NumericUpDown
        {
            Minimum = -5000,
            Maximum = 5000,
            Increment = 4,
            Value = (decimal)state.Top,
        };
        var dialog = new Window
        {
            Title = $"Assign to Canvas - {state.ControlName}",
            Width = 460,
            Height = 390,
            MinWidth = 400,
            MinHeight = 350,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void UpdateParentState()
        {
            if (parentSelector.SelectedItem is CanvasParentOption parent)
            {
                positionEditor.Maximum = parent.ChildCount + 1;
            }
        }

        parentSelector.SelectionChanged += (_, _) => UpdateParentState();
        UpdateParentState();

        var assignButton = new Button { Content = "Assign", MinWidth = 84 };
        assignButton.Click += (_, _) =>
        {
            if (parentSelector.SelectedItem is not CanvasParentOption parent)
            {
                return;
            }

            dialog.Close(new CanvasAssignmentOptions(
                parent.DisplayName,
                (int)(positionEditor.Value ?? 1) - 1,
                (double)(leftEditor.Value ?? 0),
                (double)(topEditor.Value ?? 0)));
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, assignButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Parent Canvas" },
                parentSelector,
                new TextBlock { Text = "Z-order position (1-based)" },
                positionEditor,
                new TextBlock { Text = "Local left" },
                leftEditor,
                new TextBlock { Text = "Local top" },
                topEditor,
                new TextBlock
                {
                    Text = "After assignment, drag or resize the child directly inside the Canvas.",
                    Foreground = Avalonia.Media.Brushes.Gray,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<CanvasAssignmentOptions?>(this);
    }

    private async Task<TabControlAssignmentOptions?> ShowTabControlAssignmentDialogAsync(
        TabControlAssignmentEditorState state)
    {
        var initialParent = state.Parents.First(parent => string.Equals(
            parent.DisplayName,
            state.SelectedParentName,
            StringComparison.OrdinalIgnoreCase));
        var parentSelector = new ComboBox
        {
            ItemsSource = state.Parents,
            SelectedItem = initialParent,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var tabSelector = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var dialog = new Window
        {
            Title = $"Assign to TabControl - {state.ControlName}",
            Width = 460,
            Height = 300,
            MinWidth = 400,
            MinHeight = 270,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void UpdateTabs()
        {
            if (parentSelector.SelectedItem is not TabControlParentOption parent)
            {
                tabSelector.ItemsSource = null;
                return;
            }

            tabSelector.ItemsSource = parent.Tabs;
            var preferredIndex = ReferenceEquals(parent, initialParent)
                ? state.TabIndex
                : parent.Tabs.FirstOrDefault(tab => tab.ChildName is null)?.Index ?? 0;
            tabSelector.SelectedItem = parent.Tabs.FirstOrDefault(tab => tab.Index == preferredIndex)
                ?? parent.Tabs[0];
        }

        parentSelector.SelectionChanged += (_, _) => UpdateTabs();
        UpdateTabs();

        var assignButton = new Button { Content = "Assign", MinWidth = 84 };
        assignButton.Click += (_, _) =>
        {
            if (parentSelector.SelectedItem is TabControlParentOption parent
                && tabSelector.SelectedItem is TabSlotOption tab)
            {
                dialog.Close(new TabControlAssignmentOptions(parent.DisplayName, tab.Index));
            }
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, assignButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Parent TabControl" },
                parentSelector,
                new TextBlock { Text = "Tab slot" },
                tabSelector,
                new TextBlock
                {
                    Text = "Each tab accepts one designer child. Assigning to an occupied tab moves its current child back to the Canvas root.",
                    Foreground = Avalonia.Media.Brushes.Gray,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<TabControlAssignmentOptions?>(this);
    }

    private async Task<SplitViewAssignmentOptions?> ShowSplitViewAssignmentDialogAsync(
        SplitViewAssignmentEditorState state)
    {
        var initialParent = state.Parents.First(parent => string.Equals(
            parent.DisplayName,
            state.SelectedParentName,
            StringComparison.OrdinalIgnoreCase));
        var parentSelector = new ComboBox
        {
            ItemsSource = state.Parents,
            SelectedItem = initialParent,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var slotSelector = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var dialog = new Window
        {
            Title = $"Assign to SplitView - {state.ControlName}",
            Width = 460,
            Height = 300,
            MinWidth = 400,
            MinHeight = 270,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void UpdateSlots()
        {
            if (parentSelector.SelectedItem is not SplitViewParentOption parent)
            {
                slotSelector.ItemsSource = null;
                return;
            }

            var slots = new[]
            {
                new SplitViewSlotChoice(DesignerSplitViewSlot.Pane, parent.PaneChildName),
                new SplitViewSlotChoice(DesignerSplitViewSlot.Content, parent.ContentChildName),
            };
            slotSelector.ItemsSource = slots;
            var preferredSlot = ReferenceEquals(parent, initialParent)
                ? state.Slot
                : parent.ContentChildName is null
                    ? DesignerSplitViewSlot.Content
                    : DesignerSplitViewSlot.Pane;
            slotSelector.SelectedItem = slots.First(slot => slot.Slot == preferredSlot);
        }

        parentSelector.SelectionChanged += (_, _) => UpdateSlots();
        UpdateSlots();

        var assignButton = new Button { Content = "Assign", MinWidth = 84 };
        assignButton.Click += (_, _) =>
        {
            if (parentSelector.SelectedItem is SplitViewParentOption parent
                && slotSelector.SelectedItem is SplitViewSlotChoice slot)
            {
                dialog.Close(new SplitViewAssignmentOptions(parent.DisplayName, slot.Slot));
            }
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, assignButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Parent SplitView" },
                parentSelector,
                new TextBlock { Text = "Slot" },
                slotSelector,
                new TextBlock
                {
                    Text = "Pane and Content each accept one designer child. Assigning to an occupied slot moves its current child back to the Canvas root.",
                    Foreground = Avalonia.Media.Brushes.Gray,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<SplitViewAssignmentOptions?>(this);
    }

    private async Task<ContentAssignmentOptions?> ShowContentAssignmentDialogAsync(
        ContentAssignmentEditorState state)
    {
        var parentSelector = new ComboBox
        {
            ItemsSource = state.Parents,
            SelectedItem = state.Parents.First(parent => string.Equals(
                parent.DisplayName,
                state.SelectedParentName,
                StringComparison.OrdinalIgnoreCase)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var dialog = new Window
        {
            Title = $"Assign as Container Content - {state.ControlName}",
            Width = 440,
            Height = 230,
            MinWidth = 380,
            MinHeight = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var assignButton = new Button { Content = "Assign", MinWidth = 84 };
        assignButton.Click += (_, _) =>
        {
            if (parentSelector.SelectedItem is ContentParentOption parent)
            {
                dialog.Close(new ContentAssignmentOptions(parent.DisplayName));
            }
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, assignButton },
        };
        var fields = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Parent content container" },
                parentSelector,
                new TextBlock
                {
                    Text = "Border, ScrollViewer, and Expander accept one designer child. Existing designer content is moved back to the Canvas root.",
                    Foreground = Avalonia.Media.Brushes.Gray,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<ContentAssignmentOptions?>(this);
    }

    private async Task<ComponentPackExportOptions?> ShowComponentPackExportDialogAsync(
        string packName,
        string displayName,
        string namePrefix)
    {
        var packNameEditor = new TextBox { Text = packName };
        var displayNameEditor = new TextBox { Text = displayName };
        var namePrefixEditor = new TextBox { Text = namePrefix };
        var dialog = new Window
        {
            Title = "Export Component Pack",
            Width = 480,
            Height = 340,
            MinWidth = 400,
            MinHeight = 280,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var applyButton = new Button { Content = "Export", MinWidth = 84 };
        applyButton.Click += (_, _) => dialog.Close(new ComponentPackExportOptions(
            packNameEditor.Text ?? string.Empty,
            displayNameEditor.Text ?? string.Empty,
            namePrefixEditor.Text ?? string.Empty));
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);

        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Pack name" },
                packNameEditor,
                new TextBlock { Text = "Toolbox display name" },
                displayNameEditor,
                new TextBlock { Text = "Name prefix (letters, numbers, underscores)" },
                namePrefixEditor,
            },
        };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var layout = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 16,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = layout;

        return await dialog.ShowDialog<ComponentPackExportOptions?>(this);
    }

    private async Task<IReadOnlyDictionary<string, string>?> ShowAppearanceEditorDialogAsync(
        string controlName,
        IReadOnlyDictionary<string, string> appearance)
    {
        var editors = new Dictionary<string, TextBox>(StringComparer.Ordinal);
        var fields = new StackPanel { Spacing = 6 };
        foreach (var pair in appearance)
        {
            var editor = new TextBox { Text = pair.Value };
            editors[pair.Key] = editor;
            fields.Children.Add(new TextBlock { Text = pair.Key });
            fields.Children.Add(editor);
        }

        var dialog = new Window
        {
            Title = $"Edit Appearance - {controlName}",
            Width = 500,
            Height = Math.Clamp(190 + appearance.Count * 58, 280, 560),
            MinWidth = 400,
            MinHeight = 280,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) => dialog.Close(
            editors
                .Where(pair => !string.Equals(
                    pair.Value.Text ?? string.Empty,
                    appearance[pair.Key],
                    StringComparison.Ordinal))
                .ToDictionary(pair => pair.Key, pair => pair.Value.Text ?? string.Empty, StringComparer.Ordinal));
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var help = new TextBlock
        {
            Text = "Brushes accept Avalonia values such as #2563EB. Leave a brush blank to clear it.",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            RowSpacing = 12,
            Children = { help, fields, buttons },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(buttons, 2);
        dialog.Content = content;

        return await dialog.ShowDialog<IReadOnlyDictionary<string, string>?>(this);
    }

    private async Task<ColorResourceApplicationOptions?> ShowColorResourceApplicationDialogAsync(
        string controlName,
        IReadOnlyList<string> resourceNames,
        IReadOnlyList<string> propertyNames)
    {
        var resourceSelector = new ComboBox
        {
            ItemsSource = resourceNames,
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var propertySelector = new ComboBox
        {
            ItemsSource = propertyNames,
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var dialog = new Window
        {
            Title = $"Apply Color Resource - {controlName}",
            Width = 460,
            Height = 260,
            MinWidth = 380,
            MinHeight = 230,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) => dialog.Close(new ColorResourceApplicationOptions(
            resourceSelector.SelectedItem?.ToString() ?? string.Empty,
            propertySelector.SelectedItem?.ToString() ?? string.Empty));
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Color resource" },
                resourceSelector,
                new TextBlock { Text = "Target property" },
                propertySelector,
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<ColorResourceApplicationOptions?>(this);
    }

    private async Task<string?> ShowTextEditorDialogAsync(string title, string content, string helpText, bool multiline = true)
    {
        var editor = new TextBox
        {
            Text = content,
            AcceptsReturn = multiline,
            MinHeight = multiline ? 160 : 32,
        };

        var dialog = new Window
        {
            Title = title,
            Width = 460,
            Height = multiline ? 330 : 190,
            MinWidth = 360,
            MinHeight = multiline ? 240 : 170,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) => dialog.Close(editor.Text ?? string.Empty);

        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };

        var layout = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock { Text = helpText },
                editor,
                buttons,
            },
        };
        Grid.SetRow(editor, 1);
        Grid.SetRow(buttons, 2);
        dialog.Content = layout;

        return await dialog.ShowDialog<string?>(this);
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowCloseWithoutPrompt)
        {
            return;
        }

        e.Cancel = true;
        _ = RequestCloseAsync();
    }

    private async Task RequestCloseAsync()
    {
        FlushPendingPropertyHistory();
        if (!await EnsureCanContinueWithUnsavedChangesAsync())
        {
            return;
        }

        _allowCloseWithoutPrompt = true;
        Close();
    }
}
