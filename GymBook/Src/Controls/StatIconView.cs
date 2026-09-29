namespace GymBook.Controls;

public enum StatIcon { Workouts, Sets, Volume, Target }

/// <summary>
/// A small animated icon over a stat on Home's week card: a dumbbell lifting (workouts), three bars filling in turn
/// like sets being ticked off (sets), and a flickering flame (volume).
/// </summary>
public class StatIconView : AnimatedDrawingView
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(StatIcon), typeof(StatIconView), StatIcon.Workouts,
            propertyChanged: (b, _, _) => ((StatIconView)b).Invalidate());

    public StatIcon Icon
    {
        get => (StatIcon)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    static readonly Color Blue = Color.FromArgb("#3F7DFF"), Violet = Color.FromArgb("#8B5CF6"), Flame = Color.FromArgb("#FF7A59"),
        FlameCore = Color.FromArgb("#FFC857"), Track = Color.FromArgb("#272C39");

    protected override void DrawFrame(ICanvas canvas, RectF rect, float t)
    {
        var s = Math.Min(rect.Width, rect.Height);
        var cx = rect.Center.X;
        var cy = rect.Center.Y;
        switch (Icon)
        {
            case StatIcon.Workouts:
                DrawDumbbell(canvas, cx, cy, s, t);
                break;
            case StatIcon.Sets:
                DrawSets(canvas, cx, cy, s, t);
                break;
            case StatIcon.Target:
                DrawTarget(canvas, cx, cy, s, t);
                break;
            default:
                DrawFlame(canvas, cx, cy, s, t);
                break;
        }
    }

    // A dumbbell lifted and lowered, with a little pause at the top, like a rep.
    static void DrawDumbbell(ICanvas canvas, float cx, float cy, float s, float t)
    {
        var phase = (t % 2.2f) / 2.2f;
        var lift = phase < 0.4f ? Ease(phase / 0.4f) : phase < 0.55f ? 1 : phase < 0.95f ? 1 - Ease((phase - 0.55f) / 0.4f) : 0;
        var y = cy + s * 0.12f - lift * s * 0.24f;
        var tilt = MathF.Sin(t * 3) * lift * 0.06f;
        canvas.SaveState();
        canvas.Rotate(tilt * 57.3f, cx, y);
        canvas.FillColor = Blue;
        var bar = s * 0.5f;
        canvas.FillRoundedRectangle(cx - bar / 2, y - s * 0.04f, bar, s * 0.08f, s * 0.04f);
        foreach (var side in new[] { -1f, 1f })
        {
            var x = cx + side * bar * 0.42f;
            canvas.FillRoundedRectangle(x - s * 0.07f, y - s * 0.2f, s * 0.14f, s * 0.4f, s * 0.05f);
            canvas.FillRoundedRectangle(x + side * s * 0.11f - s * 0.04f, y - s * 0.13f, s * 0.08f, s * 0.26f, s * 0.04f);
        }
        canvas.RestoreState();
        // The floor it comes up from, fading as it rises.
        canvas.FillColor = Blue.WithAlpha(0.25f * (1 - lift) + 0.08f);
        canvas.FillEllipse(cx - s * 0.3f, cy + s * 0.34f, s * 0.6f, s * 0.08f);
    }

    // Three bars filling one after another, then starting over: sets ticked off.
    static void DrawSets(ICanvas canvas, float cx, float cy, float s, float t)
    {
        var width = s * 0.16f;
        var gap = s * 0.08f;
        var heights = new[] { 0.36f, 0.52f, 0.68f };
        var cycle = t % 3.6f;
        for (var i = 0; i < 3; i++)
        {
            var x = cx - (width * 1.5f + gap) + i * (width + gap);
            var h = s * heights[i];
            var bottom = cy + s * 0.34f;
            canvas.FillColor = Track;
            canvas.FillRoundedRectangle(x, bottom - h, width, h, width / 2);
            // Each bar fills during its own 0.8 s, and they all stay full until the cycle restarts.
            var fill = Math.Clamp((cycle - i * 0.8f) / 0.8f, 0, 1);
            if (cycle > 3.2f)
                fill *= 1 - (cycle - 3.2f) / 0.4f;
            if (fill <= 0)
                continue;
            canvas.FillColor = Violet;
            var filled = Math.Max(width, h * Ease(fill));
            canvas.FillRoundedRectangle(x, bottom - filled, width, filled, width / 2);
        }
    }

    // A target: a ring rippling out from a bullseye that beats, like hitting the week's goal.
    static void DrawTarget(ICanvas canvas, float cx, float cy, float s, float t)
    {
        var r = s * 0.4f;
        canvas.StrokeColor = Blue;
        canvas.StrokeSize = s * 0.07f;
        canvas.DrawCircle(cx, cy, r);
        canvas.DrawCircle(cx, cy, r * 0.62f);
        // A ripple running outwards, fading as it goes.
        var ripple = t % 1.6f / 1.6f;
        canvas.StrokeColor = Blue.WithAlpha(0.5f * (1 - ripple));
        canvas.DrawCircle(cx, cy, r * (0.3f + 0.75f * ripple));
        var beat = 1 + 0.18f * MathF.Max(0, MathF.Sin(t * 5));
        canvas.FillColor = Blue;
        canvas.FillCircle(cx, cy, r * 0.26f * beat);
    }

    // A flame that flickers and sways: the work put in.
    static void DrawFlame(ICanvas canvas, float cx, float cy, float s, float t)
    {
        var flicker = 1 + 0.08f * MathF.Sin(t * 9) + 0.05f * MathF.Sin(t * 13.7f + 1);
        var sway = MathF.Sin(t * 2.3f) * s * 0.04f;
        var bottom = cy + s * 0.34f;
        canvas.FillColor = Flame.WithAlpha(0.18f);
        canvas.FillCircle(cx, bottom - s * 0.22f, s * 0.34f * flicker);
        canvas.FillColor = Flame;
        canvas.FillPath(Drop(cx, bottom, s * 0.3f, s * 0.64f * flicker, sway));
        canvas.FillColor = FlameCore;
        canvas.FillPath(Drop(cx, bottom, s * 0.15f, s * 0.36f * (2 - flicker), sway * 0.6f));
    }

    /// <summary>A flame shape standing on (<paramref name="x"/>, <paramref name="bottom"/>), its tip leaning by <paramref name="lean"/>.</summary>
    static PathF Drop(float x, float bottom, float width, float height, float lean)
    {
        var path = new PathF();
        var tipX = x + lean;
        var tipY = bottom - height;
        path.MoveTo(tipX, tipY);
        path.CurveTo(x + width * 0.9f, bottom - height * 0.55f, x + width, bottom - width * 0.2f, x, bottom);
        path.CurveTo(x - width, bottom - width * 0.2f, x - width * 0.9f, bottom - height * 0.55f, tipX, tipY);
        path.Close();
        return path;
    }

    static float Ease(float x) => x < 0.5f ? 2 * x * x : 1 - MathF.Pow(-2 * x + 2, 2) / 2;
}
