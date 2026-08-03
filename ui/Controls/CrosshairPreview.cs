using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Blazma.Models;

namespace Blazma.Controls;

/// <summary>
/// Live, pixel-faithful preview of the crosshair. The drawing here mirrors
/// src/CrosshairRenderer.cpp so what the user sees is what the overlay paints.
/// </summary>
public sealed class CrosshairPreview : Control
{
    /// <summary>Extra width the dark outline adds on each side, as in the renderer.</summary>
    private const double OutlineWidth = 2;

    public static readonly StyledProperty<CrosshairType> TypeProperty =
        AvaloniaProperty.Register<CrosshairPreview, CrosshairType>(nameof(Type));

    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<CrosshairPreview, Color>(nameof(Color), Colors.Lime);

    public static readonly StyledProperty<int> SizeProperty =
        AvaloniaProperty.Register<CrosshairPreview, int>(nameof(Size), 20);

    public static readonly StyledProperty<int> ThicknessProperty =
        AvaloniaProperty.Register<CrosshairPreview, int>(nameof(Thickness), 2);

    public static readonly StyledProperty<int> GapProperty =
        AvaloniaProperty.Register<CrosshairPreview, int>(nameof(Gap));

    public static readonly StyledProperty<bool> OutlineProperty =
        AvaloniaProperty.Register<CrosshairPreview, bool>(nameof(Outline));

    public static readonly StyledProperty<int> CrosshairOpacityProperty =
        AvaloniaProperty.Register<CrosshairPreview, int>(nameof(CrosshairOpacity), 255);

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<CrosshairPreview, double>(nameof(Zoom), 1.0);

    static CrosshairPreview()
    {
        AffectsRender<CrosshairPreview>(
            TypeProperty, ColorProperty, SizeProperty, ThicknessProperty,
            GapProperty, OutlineProperty, CrosshairOpacityProperty, ZoomProperty);
    }

    public CrosshairType Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public int Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public int Thickness
    {
        get => GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    public int Gap
    {
        get => GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    public bool Outline
    {
        get => GetValue(OutlineProperty);
        set => SetValue(OutlineProperty, value);
    }

    public int CrosshairOpacity
    {
        get => GetValue(CrosshairOpacityProperty);
        set => SetValue(CrosshairOpacityProperty, value);
    }

    /// <summary>Magnification of the preview. 1.0 shows true on-screen size.</summary>
    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

        var centre = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var zoom = Zoom <= 0 ? 1.0 : Zoom;

        var colour = Color;
        var brush = new SolidColorBrush(colour);

        double size = Size * zoom;
        double thickness = Thickness * zoom;
        double gap = Math.Clamp(Gap, 0, Math.Max(0, Size - 1)) * zoom;

        // Centre dot of the combined shapes tracks line thickness, as in the renderer.
        double centreDot = (Thickness < 4 ? 2 : Thickness / 2) * zoom;

        using (context.PushOpacity(Math.Clamp(CrosshairOpacity, 0, 255) / 255.0))
        {
            switch (Type)
            {
                case CrosshairType.Cross:
                    DrawCross(context, centre, size, thickness, gap, brush);
                    break;
                case CrosshairType.Dot:
                    DrawDot(context, centre, size, thickness, colour, brush);
                    break;
                case CrosshairType.Circle:
                    DrawCircle(context, centre, size, thickness, brush);
                    break;
                case CrosshairType.CrossDot:
                    DrawCross(context, centre, size, thickness, gap, brush);
                    DrawDot(context, centre, centreDot, thickness, colour, brush);
                    break;
                case CrosshairType.CircleDot:
                    DrawCircle(context, centre, size, thickness, brush);
                    DrawDot(context, centre, centreDot, thickness, colour, brush);
                    break;
            }
        }
    }

    private void DrawCross(DrawingContext context, Point c, double size, double thickness,
                           double gap, IBrush brush)
    {
        double half = gap / 2;

        void Arms(IPen pen)
        {
            context.DrawLine(pen, new Point(c.X - size, c.Y), new Point(c.X - half, c.Y));
            context.DrawLine(pen, new Point(c.X + half, c.Y), new Point(c.X + size, c.Y));
            context.DrawLine(pen, new Point(c.X, c.Y - size), new Point(c.X, c.Y - half));
            context.DrawLine(pen, new Point(c.X, c.Y + half), new Point(c.X, c.Y + size));
        }

        if (Outline)
            Arms(new Pen(Brushes.Black, thickness + OutlineWidth, lineCap: PenLineCap.Round));

        Arms(new Pen(brush, thickness, lineCap: PenLineCap.Round));
    }

    private void DrawDot(DrawingContext context, Point c, double radius, double thickness,
                         Color colour, IBrush brush)
    {
        if (radius <= 0) return;

        double glow = Math.Clamp(4 + thickness / 2, 1, radius);

        // Radial fade from the crosshair colour out to fully transparent, matching
        // the PathGradientBrush the renderer uses.
        var glowBrush = new RadialGradientBrush
        {
            Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(colour, 0.0),
                new GradientStop(Color.FromArgb(0, colour.R, colour.G, colour.B), 1.0)
            }
        };
        context.DrawEllipse(glowBrush, null, c, radius + glow, radius + glow);

        if (Outline)
        {
            double outR = radius + (thickness < 4 ? OutlineWidth : thickness / 2);
            context.DrawEllipse(Brushes.Black, null, c, outR, outR);
        }

        context.DrawEllipse(brush, null, c, radius, radius);

        // Small specular highlight near the top of the dot.
        double hs = radius > 4 ? radius / 3 : 2;
        var highlight = new Point(c.X - hs * 0.6 + hs / 2, c.Y - radius + 2 + hs / 2);
        context.DrawEllipse(new SolidColorBrush(Color.FromArgb(130, 255, 255, 255)), null,
                            highlight, hs / 2, hs / 2);
    }

    private void DrawCircle(DrawingContext context, Point c, double size, double thickness, IBrush brush)
    {
        if (Outline)
            context.DrawEllipse(null, new Pen(Brushes.Black, thickness + OutlineWidth), c, size, size);

        context.DrawEllipse(null, new Pen(brush, thickness), c, size, size);
    }
}
