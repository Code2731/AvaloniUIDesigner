using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaloniaUIDesigner.App.Views;

public sealed class DesignerGuideOverlay : Control
{
    public static readonly StyledProperty<IReadOnlyList<double>> HorizontalGuidesProperty =
        AvaloniaProperty.Register<DesignerGuideOverlay, IReadOnlyList<double>>(
            nameof(HorizontalGuides),
            []);

    public static readonly StyledProperty<IReadOnlyList<double>> VerticalGuidesProperty =
        AvaloniaProperty.Register<DesignerGuideOverlay, IReadOnlyList<double>>(
            nameof(VerticalGuides),
            []);

    public static readonly StyledProperty<double> ArtboardWidthProperty =
        AvaloniaProperty.Register<DesignerGuideOverlay, double>(nameof(ArtboardWidth), 1280);

    public static readonly StyledProperty<double> ArtboardHeightProperty =
        AvaloniaProperty.Register<DesignerGuideOverlay, double>(nameof(ArtboardHeight), 800);

    private static readonly Pen GuidePen = new(Brush.Parse("#F97316"), 1.5);

    static DesignerGuideOverlay()
    {
        AffectsRender<DesignerGuideOverlay>(
            HorizontalGuidesProperty,
            VerticalGuidesProperty,
            ArtboardWidthProperty,
            ArtboardHeightProperty);
    }

    public IReadOnlyList<double> HorizontalGuides
    {
        get => GetValue(HorizontalGuidesProperty);
        set => SetValue(HorizontalGuidesProperty, value);
    }

    public IReadOnlyList<double> VerticalGuides
    {
        get => GetValue(VerticalGuidesProperty);
        set => SetValue(VerticalGuidesProperty, value);
    }

    public double ArtboardWidth
    {
        get => GetValue(ArtboardWidthProperty);
        set => SetValue(ArtboardWidthProperty, value);
    }

    public double ArtboardHeight
    {
        get => GetValue(ArtboardHeightProperty);
        set => SetValue(ArtboardHeightProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        foreach (var x in VerticalGuides)
        {
            if (x >= 0 && x <= ArtboardWidth)
            {
                context.DrawLine(GuidePen, new Point(x, 0), new Point(x, ArtboardHeight));
            }
        }

        foreach (var y in HorizontalGuides)
        {
            if (y >= 0 && y <= ArtboardHeight)
            {
                context.DrawLine(GuidePen, new Point(0, y), new Point(ArtboardWidth, y));
            }
        }
    }
}
