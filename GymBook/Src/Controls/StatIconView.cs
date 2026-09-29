namespace GymBook.Controls;

public enum StatIcon { Workouts, Sets, Volume, Target, Calendar, Trend, Trophy, Clock, Scale, Pie, Streak }

/// <summary>
/// A small animated icon over a stat on Home's week card: a dumbbell lifting (workouts), three bars filling in turn
/// like sets being ticked off (sets), and a flickering flame (volume). Also for the Progress tab: a calendar page
/// being ticked, a trend line drawing upwards, a shining trophy, a clock hand sweeping, a scale settling, a pie
/// filling round, and a streak of flames.
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
            case StatIcon.Calendar:
                DrawCalendar(canvas, cx, cy, s, t);
                break;
            case StatIcon.Trend:
                DrawTrend(canvas, cx, cy, s, t);
                break;
            case StatIcon.Trophy:
                DrawTrophy(canvas, cx, cy, s, t);
                break;
            case StatIcon.Clock:
                DrawClock(canvas, cx, cy, s, t);
                break;
            case StatIcon.Scale:
                DrawScale(canvas, cx, cy, s, t);
                break;
            case StatIcon.Pie:
                DrawPie(canvas, cx, cy, s, t);
                break;
            case StatIcon.Streak:
                DrawFlame(canvas, cx - s * 0.18f, cy + s * 0.06f, s * 0.7f, t + 0.7f);
                DrawFlame(canvas, cx + s * 0.1f, cy, s, t);
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

    static readonly Color Green = Color.FromArgb("#2ED47A"), Gold = Color.FromArgb("#FFB020"), Orange = Color.FromArgb("#FF8A3D"),
        Light = Color.FromArgb("#F4F6FB"), Dim = Color.FromArgb("#3A4152");

    // A calendar page whose days tick off one by one, then it clears for a fresh page.
    static void DrawCalendar(ICanvas canvas, float cx, float cy, float s, float t)
    {
        var w = s * 0.72f;
        var h = s * 0.66f;
        var x = cx - w / 2;
        var y = cy - h / 2 + s * 0.04f;
        var cycle = t % 3f;
        var clear = cycle > 2.6f ? (cycle - 2.6f) / 0.4f : 0;
        canvas.FillColor = Track;
        canvas.FillRoundedRectangle(x, y, w, h, s * 0.08f);
        canvas.FillColor = Blue;
        canvas.FillRoundedRectangle(x, y, w, h * 0.26f, s * 0.08f);
        canvas.FillRectangle(x, y + h * 0.14f, w, h * 0.12f);
        foreach (var side in new[] { 0.3f, 0.7f })
        {
            canvas.FillColor = Light;
            canvas.FillRoundedRectangle(x + w * side - s * 0.03f, y - s * 0.06f, s * 0.06f, s * 0.12f, s * 0.03f);
        }
        var cell = w / 3;
        for (var i = 0; i < 6; i++)
        {
            var dx = x + cell * (i % 3 + 0.5f);
            var dy = y + h * (0.47f + i / 3 * 0.3f);
            var on = Math.Clamp((cycle - i * 0.35f) / 0.25f, 0, 1) * (1 - clear);
            canvas.FillColor = on > 0 ? Green.WithAlpha(0.35f + 0.65f * on) : Dim;
            canvas.FillCircle(dx, dy, cell * 0.2f * (0.7f + 0.3f * on));
        }
    }

    // A line zig-zagging upwards, drawn in, with an arrowhead at its tip; then it fades and draws again.
    static void DrawTrend(ICanvas canvas, float cx, float cy, float s, float t)
    {
        var cycle = t % 2.8f;
        var draw = Ease(Math.Clamp(cycle / 1.4f, 0, 1));
        var fade = cycle > 2.4f ? 1 - (cycle - 2.4f) / 0.4f : 1;
        PointF[] pts = [new(cx - s * 0.36f, cy + s * 0.26f), new(cx - s * 0.12f, cy + s * 0.02f), new(cx + s * 0.04f, cy + s * 0.14f), new(cx + s * 0.34f, cy - s * 0.24f)];
        var total = 0f;
        for (var i = 1; i < pts.Length; i++)
            total += Distance(pts[i - 1], pts[i]);
        var left = total * draw;
        var path = new PathF();
        path.MoveTo(pts[0]);
        var tip = pts[0];
        var dir = new PointF(1, 0);
        for (var i = 1; i < pts.Length && left > 0; i++)
        {
            var len = Distance(pts[i - 1], pts[i]);
            var k = Math.Min(1, left / len);
            tip = new PointF(pts[i - 1].X + (pts[i].X - pts[i - 1].X) * k, pts[i - 1].Y + (pts[i].Y - pts[i - 1].Y) * k);
            dir = new PointF((pts[i].X - pts[i - 1].X) / len, (pts[i].Y - pts[i - 1].Y) / len);
            path.LineTo(tip);
            left -= len;
        }
        canvas.StrokeColor = Green.WithAlpha(fade);
        canvas.StrokeSize = s * 0.08f;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.DrawPath(path);
        if (draw > 0.05f)
        {
            var a = s * 0.16f;
            var arrow = new PathF();
            arrow.MoveTo(tip.X + dir.X * a * 0.4f, tip.Y + dir.Y * a * 0.4f);
            arrow.LineTo(tip.X - dir.X * a * 0.6f - dir.Y * a * 0.6f, tip.Y - dir.Y * a * 0.6f + dir.X * a * 0.6f);
            arrow.LineTo(tip.X - dir.X * a * 0.6f + dir.Y * a * 0.6f, tip.Y - dir.Y * a * 0.6f - dir.X * a * 0.6f);
            arrow.Close();
            canvas.FillColor = Green.WithAlpha(fade);
            canvas.FillPath(arrow);
        }
    }

    static float Distance(PointF a, PointF b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    // A gold trophy bobbing gently, with a glint of light sweeping across it and a sparkle now and then.
    static void DrawTrophy(ICanvas canvas, float cx, float cy, float s, float t)
    {
        var bob = MathF.Sin(t * 2) * s * 0.02f;
        var top = cy - s * 0.3f + bob;
        var cup = new PathF();
        cup.MoveTo(cx - s * 0.26f, top);
        cup.LineTo(cx + s * 0.26f, top);
        cup.CurveTo(cx + s * 0.26f, top + s * 0.3f, cx + s * 0.14f, top + s * 0.4f, cx, top + s * 0.42f);
        cup.CurveTo(cx - s * 0.14f, top + s * 0.4f, cx - s * 0.26f, top + s * 0.3f, cx - s * 0.26f, top);
        cup.Close();
        canvas.StrokeColor = Gold;
        canvas.StrokeSize = s * 0.06f;
        canvas.DrawEllipse(cx - s * 0.38f, top + s * 0.03f, s * 0.2f, s * 0.2f);
        canvas.DrawEllipse(cx + s * 0.18f, top + s * 0.03f, s * 0.2f, s * 0.2f);
        canvas.FillColor = Gold;
        canvas.FillPath(cup);
        canvas.FillRectangle(cx - s * 0.04f, top + s * 0.4f, s * 0.08f, s * 0.14f);
        canvas.FillRoundedRectangle(cx - s * 0.18f, top + s * 0.52f, s * 0.36f, s * 0.1f, s * 0.03f);
        // The glint: a pale band crossing the cup.
        var sweep = t % 2.5f / 2.5f;
        var gx = cx - s * 0.5f + sweep * s * 1.2f;
        canvas.SaveState();
        canvas.ClipPath(cup);
        var band = new PathF();
        band.MoveTo(gx, top - s * 0.1f);
        band.LineTo(gx + s * 0.1f, top - s * 0.1f);
        band.LineTo(gx - s * 0.1f, top + s * 0.5f);
        band.LineTo(gx - s * 0.2f, top + s * 0.5f);
        band.Close();
        canvas.FillColor = Colors.White.WithAlpha(0.45f);
        canvas.FillPath(band);
        canvas.RestoreState();
        var twinkle = MathF.Max(0, MathF.Sin(t * 3.3f));
        canvas.FillColor = Colors.White.WithAlpha(twinkle);
        canvas.FillCircle(cx + s * 0.3f, top - s * 0.04f, s * 0.04f * twinkle);
    }

    // A clock whose hand sweeps round, with a ring filling behind it.
    static void DrawClock(ICanvas canvas, float cx, float cy, float s, float t)
    {
        var r = s * 0.36f;
        canvas.StrokeColor = Track;
        canvas.StrokeSize = s * 0.08f;
        canvas.DrawCircle(cx, cy, r);
        var turn = t % 4f / 4f;
        canvas.StrokeColor = Violet;
        canvas.StrokeLineCap = LineCap.Round;
        if (turn > 0.01f)
            canvas.DrawArc(cx - r, cy - r, r * 2, r * 2, 90, 90 - 360 * turn, true, false);
        var angle = turn * MathF.PI * 2;
        canvas.StrokeColor = Light;
        canvas.StrokeSize = s * 0.06f;
        canvas.DrawLine(cx, cy, cx + MathF.Sin(angle) * r * 0.7f, cy - MathF.Cos(angle) * r * 0.7f);
        canvas.DrawLine(cx, cy, cx, cy - r * 0.4f);
        canvas.FillColor = Light;
        canvas.FillCircle(cx, cy, s * 0.05f);
    }

    // A bathroom scale whose needle swings and settles.
    static void DrawScale(ICanvas canvas, float cx, float cy, float s, float t)
    {
        var w = s * 0.7f;
        canvas.FillColor = Track;
        canvas.FillRoundedRectangle(cx - w / 2, cy - w / 2 + s * 0.04f, w, w, s * 0.14f);
        var r = s * 0.2f;
        var dialY = cy - s * 0.04f;
        canvas.StrokeColor = Green;
        canvas.StrokeSize = s * 0.05f;
        canvas.DrawArc(cx - r, dialY - r, r * 2, r * 2, 160, 20, true, false);
        var cycle = t % 3f;
        var angle = -0.2f + MathF.Exp(-cycle * 2.2f) * MathF.Sin(cycle * 14) * 0.9f;
        canvas.StrokeColor = Light;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawLine(cx, dialY + r * 0.2f, cx + MathF.Sin(angle) * r * 0.95f, dialY + r * 0.2f - MathF.Cos(angle) * r * 0.95f);
        canvas.FillColor = Dim;
        canvas.FillRoundedRectangle(cx - w * 0.3f, cy + s * 0.2f, w * 0.22f, s * 0.06f, s * 0.03f);
        canvas.FillRoundedRectangle(cx + w * 0.08f, cy + s * 0.2f, w * 0.22f, s * 0.06f, s * 0.03f);
    }

    // A ring whose slices fill round one after another.
    static void DrawPie(ICanvas canvas, float cx, float cy, float s, float t)
    {
        var r = s * 0.34f;
        canvas.StrokeSize = s * 0.14f;
        canvas.StrokeColor = Track;
        canvas.DrawCircle(cx, cy, r);
        var cycle = t % 3.2f;
        var fill = Ease(Math.Clamp(cycle / 1.8f, 0, 1)) * (cycle > 2.8f ? 1 - (cycle - 2.8f) / 0.4f : 1);
        Color[] colours = [Blue, Violet, Green, Orange];
        float[] shares = [0.35f, 0.25f, 0.25f, 0.15f];
        var start = 0f;
        canvas.StrokeLineCap = LineCap.Butt;
        for (var i = 0; i < shares.Length; i++)
        {
            var end = Math.Min(start + shares[i], fill);
            if (end > start)
            {
                canvas.StrokeColor = colours[i];
                canvas.DrawArc(cx - r, cy - r, r * 2, r * 2, 90 - start * 360, 90 - end * 360, true, false);
            }
            start += shares[i];
        }
    }

    static float Ease(float x) => x < 0.5f ? 2 * x * x : 1 - MathF.Pow(-2 * x + 2, 2) / 2;
}
