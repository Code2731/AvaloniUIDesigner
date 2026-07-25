using System;
using System.Collections.Specialized;
using System.ComponentModel;
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
using AvaloniaUIDesigner.App.ViewModels;

namespace AvaloniaUIDesigner.App.Views;

public partial class MainWindow : Window
{
    private enum DragMode { None, Move, N, S, E, W, NE, NW, SE, SW }
    private enum UnsavedChoice { Save, Discard, Cancel }

    private const double HandleHalf = 5;
    private const double MinSize = 10;
    private const double MarqueeThreshold = 3;

    private DragMode _dragMode = DragMode.None;
    private Point _dragStart;
    private double _origX, _origY, _origW, _origH;
    private DesignElement? _dragTarget;
    private readonly System.Collections.Generic.Dictionary<DesignElement, Point> _dragOrigins = new();
    private bool _isMarqueeSelecting;
    private bool _marqueeAdditive;
    private Point _marqueeStart;

    private CanvasViewModel? _boundCanvas;
    private DesignElement? _boundElement;
    private Control? _boundVisual;
    private MainWindowViewModel? _boundVm;

    private readonly DispatcherTimer _propertyEditTimer;
    private bool _hasPendingPropertyEdit;
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

    private void OnBringToFrontMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => MoveSelectedElementsInLayerOrder(MainWindowViewModel.LayerOrderAction.BringToFront);

    private void OnSendToBackMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => MoveSelectedElementsInLayerOrder(MainWindowViewModel.LayerOrderAction.SendToBack);

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

    private void ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction action)
    {
        FlushPendingPropertyHistory();
        Vm?.ArrangeSelectedElements(action);
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

        if (e.Key == Key.Delete)
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
        if (control is Button)
        {
            return propertyName == "Content";
        }

        if (control is TextBox)
        {
            return propertyName is "Text" or "Watermark";
        }

        if (control is TextBlock)
        {
            return propertyName == "Text";
        }

        if (control is CheckBox)
        {
            return propertyName is "Content" or "IsChecked";
        }

        if (control is Slider)
        {
            return propertyName is "Minimum" or "Maximum" or "Value";
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
        if (e.PropertyName is nameof(DesignElement.X)
            or nameof(DesignElement.Y)
            or nameof(DesignElement.Width)
            or nameof(DesignElement.Height))
        {
            UpdateHandlePositions();
        }
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

    private void OnDesignHostPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm is null || sender is not Control host)
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
                foreach (var (element, origin) in _dragOrigins)
                {
                    element.X = Math.Max(0, SnapPosition(origin.X + dx));
                    element.Y = Math.Max(0, SnapPosition(origin.Y + dy));
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

            await using var stream = await file.OpenWriteAsync();
            stream.SetLength(0);
            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(Vm.ExportFullAxaml());
            await writer.FlushAsync();

            var pickedPath = file.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(pickedPath))
            {
                Vm.MarkDocumentSaved(pickedPath);
                Vm.StatusText = $"Saved {Path.GetFileName(pickedPath)}";
            }
            else
            {
                Vm.MarkCurrentStateSaved();
                Vm.StatusText = $"Saved {file.Name}";
            }

            return true;
        }

        await File.WriteAllTextAsync(targetPath, Vm.ExportFullAxaml());
        Vm.MarkDocumentSaved(targetPath);
        Vm.StatusText = $"Saved {Path.GetFileName(targetPath)}";
        return true;
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
