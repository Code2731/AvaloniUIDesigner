using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaloniaUIDesigner.App.Views;

public sealed class DesignerGridOverlay : Control
{
    public static readonly StyledProperty<double> GridSizeProperty =
        AvaloniaProperty.Register<DesignerGridOverlay, double>(nameof(GridSize), 8);

    public static readonly StyledProperty<bool> IsGridVisibleProperty =
        AvaloniaProperty.Register<DesignerGridOverlay, bool>(nameof(IsGridVisible), true);

    private static readonly Pen GridPen = new(new SolidColorBrush(Color.FromArgb(58, 120, 140, 160)), 1);

    static DesignerGridOverlay()
    {
        AffectsRender<DesignerGridOverlay>(GridSizeProperty, IsGridVisibleProperty);
    }

    public double GridSize
    {
        get => GetValue(GridSizeProperty);
        set => SetValue(GridSizeProperty, value);
    }

    public bool IsGridVisible
    {
        get => GetValue(IsGridVisibleProperty);
        set => SetValue(IsGridVisibleProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (!IsGridVisible || GridSize <= 0)
        {
            return;
        }

        for (var x = 0d; x <= Bounds.Width; x += GridSize)
        {
            context.DrawLine(GridPen, new Point(x, 0), new Point(x, Bounds.Height));
        }

        for (var y = 0d; y <= Bounds.Height; y += GridSize)
        {
            context.DrawLine(GridPen, new Point(0, y), new Point(Bounds.Width, y));
        }
    }
}
