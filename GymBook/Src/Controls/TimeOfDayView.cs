namespace GymBook.Controls;

public enum DayPart { Morning, Afternoon, Evening, Night }

/// <summary>
/// A small animated scene for the greeting on Home: a rising sun in the morning, a bright sun and a passing cloud in
/// the afternoon, a sunset in the evening, and a moon among twinkling stars at night.
/// </summary>
public class TimeOfDayView : AnimatedDrawingView
{
    public static readonly BindableProperty PartProperty =
        BindableProperty.Create(nameof(Part), typeof(DayPart), typeof(TimeOfDayView), DayPart.Morning,
            propertyChanged: (b, _, _) => ((TimeOfDayView)b).Invalidate());

    public DayPart Part
    {
        get => (DayPart)GetValue(PartProperty);
        set => SetValue(PartProperty, value);
    }

    /// <summary>Which scene fits the time of day.</summary>
    public static DayPart For(DateTime time) => time.Hour switch
    {
        >= 5 and < 12 => DayPart.Morning,
        >= 12 and < 18 => DayPart.Afternoon,
        >= 18 and < 22 => DayPart.Evening,
        _ => DayPart.Night,
    };

    static readonly Color Sun = Color.FromArgb("#FFC857"), SunHot = Color.FromArgb("#FFB020"), Sunset = Color.FromArgb("#FF7A59"),
        SunsetGlow = Color.FromArgb("#FF4D8B"), Moon = Color.FromArgb("#E6ECFF"), Star = Color.FromArgb("#F4F6FB"),
        Cloud = Color.FromArgb("#DDE3EE"), Water = Color.FromArgb("#3F7DFF");

    protected override void DrawFrame(ICanvas canvas, RectF rect, float t)
    {
        var s = Math.Min(rect.Width, rect.Height);
        var cx = rect.Center.X;
        var cy = rect.Center.Y;
        switch (Part)
        {
            case DayPart.Morning:
                // Rising gently, rays turning slowly, a cloud drifting by in front.
                var rise = MathF.Sin(t * 0.8f) * s * 0.03f;
                DrawSun(canvas, cx, cy + s * 0.08f + rise, s * 0.2f, Sun, t * 12, 1 + 0.06f * MathF.Sin(t * 2));
                DrawCloud(canvas, rect, s, t, 0.35f, s * 0.64f, 0.85f);
                break;
            case DayPart.Afternoon:
                // High and bright, rays breathing, a cloud passing.
                DrawSun(canvas, cx, cy - s * 0.04f, s * 0.22f, SunHot, t * 6, 1 + 0.12f * MathF.Sin(t * 1.6f));
                DrawCloud(canvas, rect, s, t, 0.5f, s * 0.62f, 0.95f);
                break;
            case DayPart.Evening:
                DrawSunset(canvas, rect, s, t);
                break;
            default:
                DrawNight(canvas, rect, s, t);
                break;
        }
    }

    static void DrawSun(ICanvas canvas, float x, float y, float r, Color color, float degrees, float rayScale)
    {
        // Soft glow, rays, then the disc.
        canvas.FillColor = color.WithAlpha(0.18f);
        canvas.FillCircle(x, y, r * 1.6f);
        canvas.StrokeColor = color;
        canvas.StrokeSize = Math.Max(1.5f, r * 0.14f);
        canvas.StrokeLineCap = LineCap.Round;
        for (var i = 0; i < 8; i++)
        {
            var a = (degrees + i * 45) * MathF.PI / 180;
            var inner = r * 1.3f;
            var outer = r * (1.3f + 0.45f * rayScale);
            canvas.DrawLine(x + MathF.Cos(a) * inner, y + MathF.Sin(a) * inner, x + MathF.Cos(a) * outer, y + MathF.Sin(a) * outer);
        }
        canvas.FillColor = color;
        canvas.FillCircle(x, y, r);
    }

    /// <summary>A puffy cloud crossing the scene; <paramref name="speed"/> in scene widths per 10 s.</summary>
    static void DrawCloud(ICanvas canvas, RectF rect, float s, float t, float speed, float y, float alpha)
    {
        var span = rect.Width + s * 0.6f;
        var x = rect.Left - s * 0.3f + (t * speed * rect.Width / 10 % span);
        var r = s * 0.1f;
        canvas.FillColor = Cloud.WithAlpha(alpha);
        canvas.FillCircle(x, y, r);
        canvas.FillCircle(x + r * 1.1f, y - r * 0.6f, r * 1.25f);
        canvas.FillCircle(x + r * 2.3f, y, r * 1.05f);
        canvas.FillRoundedRectangle(x - r * 0.2f, y - r * 0.2f, r * 2.7f, r * 1.2f, r * 0.6f);
    }

    static void DrawSunset(ICanvas canvas, RectF rect, float s, float t)
    {
        var horizon = rect.Top + rect.Height * 0.62f;
        var x = rect.Center.X;
        var r = s * 0.26f;
        // A glow that breathes, the sun half down, then the horizon and the water shimmering below it.
        canvas.FillColor = SunsetGlow.WithAlpha(0.16f + 0.06f * MathF.Sin(t * 1.4f));
        canvas.FillCircle(x, horizon, r * 1.7f);
        canvas.SaveState();
        canvas.ClipRectangle(rect.Left, rect.Top, rect.Width, horizon - rect.Top);
        canvas.FillColor = Sunset;
        canvas.FillCircle(x, horizon + MathF.Sin(t * 0.6f) * s * 0.02f, r);
        canvas.RestoreState();
        canvas.StrokeColor = Sunset.WithAlpha(0.9f);
        canvas.StrokeSize = Math.Max(1.5f, s * 0.035f);
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawLine(rect.Left + s * 0.08f, horizon, rect.Right - s * 0.08f, horizon);
        for (var i = 0; i < 3; i++)
        {
            var y = horizon + s * (0.1f + i * 0.09f);
            var half = r * (0.9f - i * 0.25f) * (1 + 0.15f * MathF.Sin(t * 2 + i));
            canvas.StrokeColor = Water.WithAlpha(0.55f - i * 0.12f);
            canvas.DrawLine(x - half, y, x + half, y);
        }
    }

    static void DrawNight(ICanvas canvas, RectF rect, float s, float t)
    {
        // Stars twinkling at their own pace.
        ReadOnlySpan<(float X, float Y, float Size, float Phase)> stars =
            [(0.18f, 0.22f, 0.035f, 0), (0.8f, 0.18f, 0.03f, 1.7f), (0.72f, 0.72f, 0.028f, 3.1f), (0.2f, 0.74f, 0.025f, 4.4f), (0.5f, 0.1f, 0.022f, 2.3f)];
        foreach (var (sx, sy, size, phase) in stars)
        {
            var twinkle = 0.35f + 0.65f * (0.5f + 0.5f * MathF.Sin(t * 2.2f + phase));
            DrawStar(canvas, rect.Left + rect.Width * sx, rect.Top + rect.Height * sy, s * size * (0.8f + 0.4f * twinkle), Star.WithAlpha(twinkle));
        }
        // A crescent rocking gently: the full moon, with a circle of the page's colour taken out of it.
        var r = s * 0.24f;
        var x = rect.Center.X;
        var y = rect.Center.Y;
        var tilt = MathF.Sin(t * 0.7f) * 0.08f;
        canvas.FillColor = Moon.WithAlpha(0.14f);
        canvas.FillCircle(x, y, r * 1.5f);
        canvas.FillColor = Moon;
        canvas.FillCircle(x, y, r);
        canvas.FillColor = Color.FromArgb("#0B0D12");
        canvas.FillCircle(x + r * (0.45f + tilt), y - r * (0.3f - tilt), r * 0.86f);
    }

    static void DrawStar(ICanvas canvas, float x, float y, float r, Color color)
    {
        var path = new PathF();
        path.MoveTo(x, y - r);
        path.QuadTo(x, y, x + r, y);
        path.QuadTo(x, y, x, y + r);
        path.QuadTo(x, y, x - r, y);
        path.QuadTo(x, y, x, y - r);
        path.Close();
        canvas.FillColor = color;
        canvas.FillPath(path);
    }
}
