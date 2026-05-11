using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaUIDesigner.App.ViewModels;

namespace AvaloniaUIDesigner.App.Views;

public partial class MainWindow : Window
{
    private enum DragMode { None, Move, N, S, E, W, NE, NW, SE, SW }

    private const double HandleHalf = 5;
    private const double MinSize = 10;

    private DragMode _dragMode = DragMode.None;
    private Point _dragStart;
    private double _origX, _origY, _origW, _origH;
    private DesignElement? _dragTarget;

    private CanvasViewModel? _boundCanvas;
    private DesignElement? _boundElement;
    private Control? _boundVisual;
    private MainWindowViewModel? _boundVm;

    private readonly DispatcherTimer _propertyEditTimer;
    private bool _hasPendingPropertyEdit;

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
        if (Vm is null)
        {
            return;
        }

        FlushPendingPropertyHistory();

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
        await SaveDocumentAsync(forceSaveAs: false);
    }

    private async void OnSaveAsMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await SaveDocumentAsync(forceSaveAs: true);
    }

    private void OnNewMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.NewDocument();
    }

    private void OnExitMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Close();
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

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (Vm is null)
        {
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

        if (!File.Exists(path))
        {
            Vm.RemoveRecentFile(path);
            Vm.StatusText = $"Recent file not found: {path}";
            return;
        }

        var content = await File.ReadAllTextAsync(path);
        if (!Vm.TryImportDraftAxaml(content, out var error))
        {
            Vm.StatusText = $"Open failed: {error}";
            return;
        }

        Vm.MarkDocumentLoaded(path);
        Vm.StatusText = $"Opened {Path.GetFileName(path)}";
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
        Vm.PlaceFromToolbox(point.X, point.Y);
        e.Handled = true;
    }

    private void OnElementPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm is null || sender is not Control { DataContext: DesignElement element })
        {
            return;
        }

        Vm.SelectElement(element);
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

        Vm?.BeginCanvasMutation(MainWindowViewModel.HistoryActionType.TransformElement, "Updated element position/size.");
        e.Pointer.Capture(DesignHost);
    }

    private void OnDragPointerMoved(object? sender, PointerEventArgs e)
    {
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
                _dragTarget.X = _origX + dx;
                _dragTarget.Y = _origY + dy;
                break;
            case DragMode.E:
                _dragTarget.Width = Math.Max(MinSize, _origW + dx);
                break;
            case DragMode.S:
                _dragTarget.Height = Math.Max(MinSize, _origH + dy);
                break;
            case DragMode.W:
                ResizeLeft(dx);
                break;
            case DragMode.N:
                ResizeTop(dy);
                break;
            case DragMode.SE:
                _dragTarget.Width = Math.Max(MinSize, _origW + dx);
                _dragTarget.Height = Math.Max(MinSize, _origH + dy);
                break;
            case DragMode.NE:
                _dragTarget.Width = Math.Max(MinSize, _origW + dx);
                ResizeTop(dy);
                break;
            case DragMode.SW:
                ResizeLeft(dx);
                _dragTarget.Height = Math.Max(MinSize, _origH + dy);
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
            _dragTarget.X = _origX + (_origW - MinSize);
        }
        else
        {
            _dragTarget.Width = newW;
            _dragTarget.X = _origX + dx;
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
            _dragTarget.Y = _origY + (_origH - MinSize);
        }
        else
        {
            _dragTarget.Height = newH;
            _dragTarget.Y = _origY + dy;
        }
    }

    private void OnDragPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragMode == DragMode.None)
        {
            return;
        }

        _dragMode = DragMode.None;
        _dragTarget = null;
        e.Pointer.Capture(null);
        e.Handled = true;

        Vm?.CommitCanvasMutation();
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

        if (!Vm.TryImportDraftAxaml(content, out var error))
        {
            Vm.StatusText = $"Open failed: {error}";
            return;
        }

        var localPath = file.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            Vm.MarkDocumentLoaded(localPath);
            Vm.StatusText = $"Opened {Path.GetFileName(localPath)}";
        }
        else
        {
            Vm.StatusText = $"Opened {file.Name}";
        }
    }

    private async Task SaveDocumentAsync(bool forceSaveAs)
    {
        if (Vm is null)
        {
            return;
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
                return;
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
                Vm.StatusText = $"Saved {file.Name}";
            }

            return;
        }

        await File.WriteAllTextAsync(targetPath, Vm.ExportFullAxaml());
        Vm.MarkDocumentSaved(targetPath);
        Vm.StatusText = $"Saved {Path.GetFileName(targetPath)}";
    }
}
