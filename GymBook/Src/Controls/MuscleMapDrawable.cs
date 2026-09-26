using GymBook.Models;
using GymBook.Services;

namespace GymBook.Controls;

/// <summary>Stylised front and back body figures with each muscle group filled by a colour function.</summary>
public class MuscleMapDrawable : IDrawable
{
    static readonly Color Body = Color.FromArgb("#1F232D");
    static readonly Color Neutral = Color.FromArgb("#353B49");
    static readonly Color Separator = Color.FromArgb("#151821");

    readonly Func<MuscleGroup, Color> _fill;

    MuscleMapDrawable(Func<MuscleGroup, Color> fill) => _fill = fill;

    public static MuscleMapDrawable Empty { get; } = new(_ => Neutral);

    public static MuscleMapDrawable ForRecovery(IReadOnlyDictionary<MuscleGroup, double> recovery) =>
        new(m => RecoveryService.ColorFor(recovery.GetValueOrDefault(m, 1)));

    public static MuscleMapDrawable ForExercise(Exercise ex)
    {
        var accent = Color.FromArgb("#3F7DFF");
        return new(m => m == ex.PrimaryMuscle ? accent : ex.SecondaryMuscles.Contains(m) ? accent.WithAlpha(0.45f) : Neutral);
    }

    /// <summary>Colours muscles by weekly set count relative to the 10–20 sets hypertrophy range.</summary>
    public static MuscleMapDrawable ForVolume(IReadOnlyDictionary<MuscleGroup, double> sets)
    {
        var accent = Color.FromArgb("#3F7DFF");
        return new(m =>
        {
            var s = sets.GetValueOrDefault(m);
            return s <= 0 ? Neutral : accent.WithAlpha((float)Math.Clamp(0.25 + s / 20 * 0.75, 0.25, 1));
        });
    }

    public void Draw(ICanvas canvas, RectF rect)
    {
        const float figW = 100, figH = 200, gap = 16;
        var scale = Math.Min(rect.Width / (figW * 2 + gap), rect.Height / figH);
        var totalW = (figW * 2 + gap) * scale;
        var left = rect.X + (rect.Width - totalW) / 2;
        var top = rect.Y + (rect.Height - figH * scale) / 2;

        DrawFigure(canvas, left, top, scale, front: true);
        DrawFigure(canvas, left + (figW + gap) * scale, top, scale, front: false);
    }

    void DrawFigure(ICanvas c, float x, float y, float s, bool front)
    {
        c.SaveState();
        c.Translate(x, y);
        c.Scale(s, s);

        // Silhouette
        c.FillColor = Body;
        c.FillCircle(50, 13, 10);
        c.FillRoundedRectangle(45, 21, 10, 9, 3);
        c.FillRoundedRectangle(30, 28, 40, 60, 12);
        c.FillRoundedRectangle(33, 82, 34, 22, 9);
        c.FillRoundedRectangle(17, 30, 12, 36, 6);
        c.FillRoundedRectangle(71, 30, 12, 36, 6);
        c.FillRoundedRectangle(13, 63, 11, 34, 5);
        c.FillRoundedRectangle(76, 63, 11, 34, 5);
        c.FillCircle(18.5f, 101, 4.5f);
        c.FillCircle(81.5f, 101, 4.5f);
        c.FillRoundedRectangle(33, 100, 16, 52, 8);
        c.FillRoundedRectangle(51, 100, 16, 52, 8);
        c.FillRoundedRectangle(34.5f, 150, 13, 42, 6);
        c.FillRoundedRectangle(52.5f, 150, 13, 42, 6);
        c.FillRoundedRectangle(33, 190, 15, 7, 3);
        c.FillRoundedRectangle(52, 190, 15, 7, 3);

        // Shared: shoulders and forearms
        Mirror(c, MuscleGroup.Shoulders, (cx, fill) => Ellipse(c, fill, cx, 37, 7.5f, 7));
        Mirror(c, MuscleGroup.Forearms, (cx, fill) => Ellipse(c, fill, cx, 78, 4.5f, 12), 18.5f);

        if (front)
        {
            Poly(c, _fill(MuscleGroup.Traps), (40, 29), (46, 25), (54, 25), (60, 29), (50, 31));
            Mirror(c, MuscleGroup.Chest, (cx, fill) => { c.FillColor = fill; c.FillRoundedRectangle(cx - 8, 34, 16, 16, 6); }, 41);
            Mirror(c, MuscleGroup.Biceps, (cx, fill) => Ellipse(c, fill, cx, 50, 5, 11), 23);

            c.FillColor = _fill(MuscleGroup.Abs);
            c.FillRoundedRectangle(41, 52, 18, 32, 5);
            c.StrokeColor = Separator;
            c.StrokeSize = 1;
            c.DrawLine(50, 53, 50, 83);
            c.DrawLine(42, 62, 58, 62);
            c.DrawLine(42, 72, 58, 72);

            Mirror(c, MuscleGroup.Quads, (cx, fill) => Ellipse(c, fill, cx, 123, 7.5f, 21), 41);
            Mirror(c, MuscleGroup.Calves, (cx, fill) => Ellipse(c, fill, cx, 168, 4.5f, 13), 41);
        }
        else
        {
            Poly(c, _fill(MuscleGroup.Traps), (39, 29), (50, 24), (61, 29), (56, 42), (50, 48), (44, 42));
            var back = _fill(MuscleGroup.Back);
            Poly(c, back, (32, 38), (43, 44), (48, 51), (48, 70), (38, 64), (33, 52));
            Poly(c, back, (68, 38), (57, 44), (52, 51), (52, 70), (62, 64), (67, 52));
            c.FillColor = _fill(MuscleGroup.LowerBack);
            c.FillRoundedRectangle(42, 69, 16, 15, 5);
            Mirror(c, MuscleGroup.Triceps, (cx, fill) => Ellipse(c, fill, cx, 50, 5, 11), 23);
            Mirror(c, MuscleGroup.Glutes, (cx, fill) => Ellipse(c, fill, cx, 98, 8.5f, 9), 42);
            Mirror(c, MuscleGroup.Hamstrings, (cx, fill) => Ellipse(c, fill, cx, 128, 7.5f, 18), 41);
            Mirror(c, MuscleGroup.Calves, (cx, fill) => Ellipse(c, fill, cx, 166, 5.5f, 14), 41);
        }

        c.RestoreState();
    }

    /// <summary>Draws a left/right pair; <paramref name="leftX"/> is the left centre, mirrored around x = 50.</summary>
    void Mirror(ICanvas c, MuscleGroup m, Action<float, Color> draw, float leftX = 27)
    {
        var fill = _fill(m);
        draw(leftX, fill);
        draw(100 - leftX, fill);
    }

    static void Ellipse(ICanvas c, Color fill, float cx, float cy, float rx, float ry)
    {
        c.FillColor = fill;
        c.FillEllipse(cx - rx, cy - ry, rx * 2, ry * 2);
    }

    static void Poly(ICanvas c, Color fill, params (float X, float Y)[] points)
    {
        var path = new PathF();
        path.MoveTo(points[0].X, points[0].Y);
        foreach (var p in points.Skip(1))
            path.LineTo(p.X, p.Y);
        path.Close();
        c.FillColor = fill;
        c.FillPath(path);
    }
}
