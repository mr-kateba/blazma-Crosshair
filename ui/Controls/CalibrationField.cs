using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Blazma.Controls;

/// <summary>
/// The measurement surface the crosshair preview sits on: a tick rule around the
/// inner edge plus faint centre hairlines. It gives the preview a true centre to
/// register against, so size and gap changes are readable at a glance.
/// </summary>
public sealed class CalibrationField : Control
{
    private const double TickSpacing = 12;
    private const double MinorTick = 4;
    private const double MajorTick = 9;
    private const int MajorEvery = 5;

    public static readonly StyledProperty<IBrush> RuleBrushProperty =
        AvaloniaProperty.Register<CalibrationField, IBrush>(nameof(RuleBrush), Brushes.Gray);

    public static readonly StyledProperty<IBrush> HairlineBrushProperty =
        AvaloniaProperty.Register<CalibrationField, IBrush>(nameof(HairlineBrush), Brushes.DimGray);

    static CalibrationField()
    {
        AffectsRender<CalibrationField>(RuleBrushProperty, HairlineBrushProperty);
    }

    public IBrush RuleBrush
    {
        get => GetValue(RuleBrushProperty);
        set => SetValue(RuleBrushProperty, value);
    }

    public IBrush HairlineBrush
    {
        get => GetValue(HairlineBrushProperty);
        set => SetValue(HairlineBrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var cx = w / 2;
        var cy = h / 2;

        var hairPen = new Pen(HairlineBrush, 1, new DashStyle(new double[] { 2, 6 }, 0));
        context.DrawLine(hairPen, new Point(0, cy), new Point(w, cy));
        context.DrawLine(hairPen, new Point(cx, 0), new Point(cx, h));

        var tickPen = new Pen(RuleBrush, 1);

        // Ticks march outward from the centre so the centre mark always lands
        // exactly on the crosshair origin, whatever the panel size.
        DrawAxis(context, tickPen, cx, w, horizontal: true, h);
        DrawAxis(context, tickPen, cy, h, horizontal: false, w);
    }

    private static void DrawAxis(DrawingContext context, IPen pen, double centre, double extent,
                                 bool horizontal, double crossExtent)
    {
        int steps = (int)(extent / 2 / TickSpacing);

        for (int i = -steps; i <= steps; i++)
        {
            double pos = centre + i * TickSpacing;
            if (pos < 0 || pos > extent) continue;

            double len = Math.Abs(i) % MajorEvery == 0 ? MajorTick : MinorTick;

            if (horizontal)
            {
                context.DrawLine(pen, new Point(pos, 0), new Point(pos, len));
                context.DrawLine(pen, new Point(pos, crossExtent - len), new Point(pos, crossExtent));
            }
            else
            {
                context.DrawLine(pen, new Point(0, pos), new Point(len, pos));
                context.DrawLine(pen, new Point(crossExtent - len, pos), new Point(crossExtent, pos));
            }
        }
    }
}
