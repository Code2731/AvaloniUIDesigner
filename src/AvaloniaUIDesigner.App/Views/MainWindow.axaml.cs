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
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaUIDesigner.App.Designer.Services;
using AvaloniaUIDesigner.App.Models;
using AvaloniaUIDesigner.App.ViewModels;

namespace AvaloniaUIDesigner.App.Views;

public partial class MainWindow : Window
{
    private enum DragMode { None, Move, N, S, E, W, NE, NW, SE, SW }
    private enum UnsavedChoice { Save, Discard, Cancel }

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
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            Vm.StatusText = "Clipboard is unavailable.";
            return;
        }

        await clipboard.SetTextAsync(Vm.ExportFullAxaml());
        Vm.StatusText = "Copied generated AXAML to clipboard.";
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

    private async void OnEditItemsMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedItems(out var controlName, out var items))
        {
            return;
        }

        var updatedItems = await ShowItemsEditorDialogAsync(controlName, items);
        if (updatedItems is not null)
        {
            Vm.SetSelectedItems(updatedItems);
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
        Vm.StatusText = BuildOpenStatus(Path.GetFileName(path), warning);
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
        if (sender is not Control control || !IsUndoTrackedVisualProperty(control, e.Property.Name))
        {
            return;
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
        if (propertyName is "Opacity" or "IsEnabled" or "IsVisible" or "TabIndex" or "IsTabStop")
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
            return propertyName is "Text" or "FontSize" or "FontWeight" or "Foreground";
        }

        if (control is Label)
        {
            return propertyName == "Content";
        }

        if (control is Image)
        {
            return propertyName == "Stretch";
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

        if (control is Grid)
        {
            return propertyName == "ShowGridLines";
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

        PropGrid.IsEnabled = canEdit;
        ElementNameEditor.IsEnabled = canEdit;
        LayoutXEditor.IsEnabled = canEdit;
        LayoutYEditor.IsEnabled = canEdit;
        LayoutWidthEditor.IsEnabled = canEdit;
        LayoutHeightEditor.IsEnabled = canEdit;

        HandleNW.IsVisible = canEdit;
        HandleN.IsVisible = canEdit;
        HandleNE.IsVisible = canEdit;
        HandleE.IsVisible = canEdit;
        HandleSE.IsVisible = canEdit;
        HandleS.IsVisible = canEdit;
        HandleSW.IsVisible = canEdit;
        HandleW.IsVisible = canEdit;
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
            Vm.StatusText = BuildOpenStatus(Path.GetFileName(localPath), warning);
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
                    Vm.StatusText = $"Saved {Path.GetFileName(pickedPath)}";
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
            Vm.StatusText = $"Saved {Path.GetFileName(targetPath)}";
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Vm.StatusText = $"Could not save {Path.GetFileName(targetPath)}: {exception.Message}";
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

    private async Task<IReadOnlyList<string>?> ShowItemsEditorDialogAsync(string controlName, IReadOnlyList<string> items)
    {
        var editor = new TextBox
        {
            Text = string.Join(Environment.NewLine, items),
            AcceptsReturn = true,
            MinHeight = 200,
        };

        var dialog = new Window
        {
            Title = $"Edit Items - {controlName}",
            Width = 460,
            Height = 380,
            MinWidth = 360,
            MinHeight = 260,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var updatedItems = (editor.Text ?? string.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            dialog.Close<IReadOnlyList<string>?>(updatedItems);
        };

        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close<IReadOnlyList<string>?>(null);

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
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock { Text = "Enter one item per line. Empty lines are ignored." },
                editor,
                buttons,
            },
        };
        Grid.SetRow(editor, 1);
        Grid.SetRow(buttons, 2);
        dialog.Content = content;

        return await dialog.ShowDialog<IReadOnlyList<string>?>(this);
    }

    private async Task<string?> ShowTextEditorDialogAsync(string title, string content, string helpText)
    {
        var editor = new TextBox
        {
            Text = content,
            AcceptsReturn = true,
            MinHeight = 160,
        };

        var dialog = new Window
        {
            Title = title,
            Width = 460,
            Height = 330,
            MinWidth = 360,
            MinHeight = 240,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) => dialog.Close<string?>(editor.Text ?? string.Empty);

        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close<string?>(null);

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
