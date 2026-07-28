using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaloniaUIDesigner.App.Views;

public sealed class DesignerRuler : Control
{
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<DesignerRuler, Orientation>(nameof(Orientation), Orientation.Horizontal);

    public static readonly StyledProperty<double> ZoomScaleProperty =
        AvaloniaProperty.Register<DesignerRuler, double>(nameof(ZoomScale), 1);

    public static readonly StyledProperty<double> ScrollOffsetProperty =
        AvaloniaProperty.Register<DesignerRuler, double>(nameof(ScrollOffset));

    public static readonly StyledProperty<double> CursorPositionProperty =
        AvaloniaProperty.Register<DesignerRuler, double>(nameof(CursorPosition), double.NaN);

    private static readonly IBrush SurfaceBrush = Brush.Parse("#2D2D30");
    private static readonly IBrush LabelBrush = Brush.Parse("#CBD5E1");
    private static readonly Pen MinorPen = new(Brush.Parse("#64748B"), 1);
    private static readonly Pen MajorPen = new(Brush.Parse("#CBD5E1"), 1);
    private static readonly Pen CursorPen = new(Brush.Parse("#22D3EE"), 1.5);
    private static readonly Typeface LabelTypeface = new("Segoe UI");

    static DesignerRuler()
    {
        AffectsRender<DesignerRuler>(
            OrientationProperty,
            ZoomScaleProperty,
            ScrollOffsetProperty,
            CursorPositionProperty);
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public double ZoomScale
    {
        get => GetValue(ZoomScaleProperty);
        set => SetValue(ZoomScaleProperty, value);
    }

    public double ScrollOffset
    {
        get => GetValue(ScrollOffsetProperty);
        set => SetValue(ScrollOffsetProperty, value);
    }

    public double CursorPosition
    {
        get => GetValue(CursorPositionProperty);
        set => SetValue(CursorPositionProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(SurfaceBrush, new Rect(Bounds.Size));

        var scale = Math.Clamp(ZoomScale, 0.01, 100);
        var viewportLength = Orientation == Orientation.Horizontal ? Bounds.Width : Bounds.Height;
        if (viewportLength <= 0)
        {
            return;
        }

        var majorStep = SelectMajorStep(scale);
        var minorStep = majorStep / 5;
        var firstTick = Math.Floor(ScrollOffset / scale / minorStep) * minorStep;
        var lastTick = (ScrollOffset + viewportLength) / scale + minorStep;

        for (var value = firstTick; value <= lastTick; value += minorStep)
        {
            var screenPosition = value * scale - ScrollOffset;
            var isMajor = IsMajorTick(value, majorStep);
            var pen = isMajor ? MajorPen : MinorPen;
            var tickLength = isMajor ? GetMajorTickLength() : GetMinorTickLength();

            if (Orientation == Orientation.Horizontal)
            {
                context.DrawLine(
                    pen,
                    new Point(screenPosition, Bounds.Height),
                    new Point(screenPosition, Math.Max(0, Bounds.Height - tickLength)));
                if (isMajor)
                {
                    DrawLabel(context, FormatValue(value), new Point(screenPosition + 3, 2));
                }
            }
            else
            {
                context.DrawLine(
                    pen,
                    new Point(Bounds.Width, screenPosition),
                    new Point(Math.Max(0, Bounds.Width - tickLength), screenPosition));
                if (isMajor)
                {
                    var label = CreateLabel(FormatValue(value));
                    context.DrawText(label, new Point(2, screenPosition - label.Height / 2));
                }
            }
        }

        if (!double.IsFinite(CursorPosition))
        {
            return;
        }

        var cursorScreenPosition = CursorPosition * scale - ScrollOffset;
        if (Orientation == Orientation.Horizontal)
        {
            context.DrawLine(CursorPen, new Point(cursorScreenPosition, 0), new Point(cursorScreenPosition, Bounds.Height));
        }
        else
        {
            context.DrawLine(CursorPen, new Point(0, cursorScreenPosition), new Point(Bounds.Width, cursorScreenPosition));
        }
    }

    private double GetMajorTickLength()
        => Orientation == Orientation.Horizontal ? Bounds.Height * 0.65 : Bounds.Width * 0.65;

    private double GetMinorTickLength()
        => Orientation == Orientation.Horizontal ? Bounds.Height * 0.35 : Bounds.Width * 0.35;

    private static double SelectMajorStep(double scale)
    {
        var rawStep = 80 / scale;
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(Math.Max(1, rawStep))));
        var normalized = rawStep / magnitude;
        var step = normalized >= 5 ? 5 : normalized >= 2 ? 2 : 1;
        return step * magnitude;
    }

    private static bool IsMajorTick(double value, double majorStep)
        => Math.Abs(value / majorStep - Math.Round(value / majorStep)) < 0.0001;

    private static string FormatValue(double value)
        => Math.Abs(value) < 0.0001
            ? "0"
            : value.ToString(value % 1 == 0 ? "0" : "0.#", CultureInfo.InvariantCulture);

    private static FormattedText CreateLabel(string value)
        => new(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, LabelTypeface, 9, LabelBrush);

    private static void DrawLabel(DrawingContext context, string value, Point position)
        => context.DrawText(CreateLabel(value), position);
}
