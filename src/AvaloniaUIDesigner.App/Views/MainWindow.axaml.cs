using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using AvaloniaUIDesigner.App.ViewModels;

namespace AvaloniaUIDesigner.App.Views;

public partial class MainWindow : Window
{
    // 드래그 모드 = Move(본체 이동) + 8방향 리사이즈
    private enum DragMode { None, Move, N, S, E, W, NE, NW, SE, SW }

    private const double HandleHalf = 5; // 핸들 10x10 → 중심 정렬 오프셋
    private const double MinSize = 10;

    private DragMode _dragMode = DragMode.None;
    private Point _dragStart;
    private double _origX, _origY, _origW, _origH;
    private DesignElement? _dragTarget;

    private CanvasViewModel? _boundCanvas;
    private DesignElement? _boundElement;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        // 생성 시점에 이미 DataContext가 세팅된 경우 대비
        OnDataContextChanged(this, EventArgs.Empty);
    }

    private MainWindowViewModel? Vm => DataContext as MainWindowViewModel;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_boundCanvas is not null)
            _boundCanvas.PropertyChanged -= OnCanvasPropertyChanged;

        _boundCanvas = Vm?.Canvas;

        if (_boundCanvas is not null)
            _boundCanvas.PropertyChanged += OnCanvasPropertyChanged;

        RebindSelection();
    }

    private void OnCanvasPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CanvasViewModel.SelectedElement))
            RebindSelection();
    }

    // SelectedElement 교체 시 기즈모 + PropertyGrid 모두 갱신
    private void RebindSelection()
    {
        if (_boundElement is not null)
            _boundElement.PropertyChanged -= OnElementPropertyChanged;

        _boundElement = _boundCanvas?.SelectedElement;

        if (_boundElement is not null)
            _boundElement.PropertyChanged += OnElementPropertyChanged;

        // PropertyGrid에 실제 Avalonia Control 전달 (선택 해제 시 null → 빈 상태)
        PropGrid.Content = _boundElement?.Visual;

        UpdateHandlePositions();
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
        if (el is null) return;

        double left = el.X;
        double top = el.Y;
        double right = el.X + el.Width;
        double bottom = el.Y + el.Height;
        double midX = el.X + el.Width / 2;
        double midY = el.Y + el.Height / 2;

        Place(HandleNW, left, top);
        Place(HandleN,  midX, top);
        Place(HandleNE, right, top);
        Place(HandleE,  right, midY);
        Place(HandleSE, right, bottom);
        Place(HandleS,  midX, bottom);
        Place(HandleSW, left, bottom);
        Place(HandleW,  left, midY);
    }

    private static void Place(Rectangle r, double cx, double cy)
    {
        Canvas.SetLeft(r, cx - HandleHalf);
        Canvas.SetTop(r, cy - HandleHalf);
    }

    // 빈 캔버스 클릭 → 툴박스에서 컨트롤 배치
    private void OnDesignHostPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm is null) return;
        if (sender is not Control host) return;

        var point = e.GetPosition(host);
        Vm.PlaceFromToolbox(point.X, point.Y);
        e.Handled = true;
    }

    // 요소 본체 클릭 → 선택 + Move 드래그 시작
    private void OnElementPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm is null) return;
        if (sender is not Control { DataContext: DesignElement element }) return;

        Vm.SelectElement(element);
        BeginDrag(DragMode.Move, element, e);
        e.Handled = true;
    }

    // 핸들 클릭 → 해당 방향 Resize 드래그 시작
    private void OnHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Rectangle { Tag: string tag }) return;
        var target = _boundElement;
        if (target is null) return;

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
        if (mode == DragMode.None) return;

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
        // DesignHost에 포인터 캡처 → 이후 Moved/Released가 항상 DesignHost로 라우팅
        e.Pointer.Capture(DesignHost);
    }

    private void OnDragPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragMode == DragMode.None || _dragTarget is null) return;

        var p = e.GetPosition(DesignHost);
        double dx = p.X - _dragStart.X;
        double dy = p.Y - _dragStart.Y;

        ApplyDrag(dx, dy);
    }

    private void ApplyDrag(double dx, double dy)
    {
        if (_dragTarget is null) return;

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

    // 왼쪽 엣지 드래그: 폭은 줄이고 X는 늘려 오른쪽 엣지 고정. 최소 크기에 닿으면 X를 클램프
    private void ResizeLeft(double dx)
    {
        if (_dragTarget is null) return;
        double newW = _origW - dx;
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
        if (_dragTarget is null) return;
        double newH = _origH - dy;
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
        if (_dragMode == DragMode.None) return;
        _dragMode = DragMode.None;
        _dragTarget = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }
}
