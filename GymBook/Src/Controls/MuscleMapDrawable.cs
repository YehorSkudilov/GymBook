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

    /// <summary>Muscles a workout trains: primary movers solid, secondary ones faded.</summary>
    public static MuscleMapDrawable ForWorkout(IEnumerable<Exercise> exercises)
    {
        var list = exercises.ToList();
        var primary = list.Select(e => e.PrimaryMuscle).ToHashSet();
        var secondary = list.SelectMany(e => e.SecondaryMuscles).ToHashSet();
        var accent = Color.FromArgb("#3F7DFF");
        return new(m => primary.Contains(m) ? accent : secondary.Contains(m) ? accent.WithAlpha(0.45f) : Neutral);
    }

    /// <summary>Shades muscles by load relative to the most-worked one, so any amount of work reads clearly.</summary>
    public static MuscleMapDrawable ForLoad(IReadOnlyDictionary<MuscleGroup, double> load)
    {
        var max = load.Values.DefaultIfEmpty(0).Max();
        var accent = Color.FromArgb("#3F7DFF");
        return new(m =>
        {
            var v = load.GetValueOrDefault(m);
            return v <= 0 || max <= 0 ? Neutral : accent.WithAlpha((float)(0.3 + 0.7 * v / max));
        });
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

    // Shapes are SVG paths on a 100 x 200 figure, drawn for the figure's left half and mirrored around x = 50.
    static readonly PathF Silhouette = PathBuilder.Build(
        "M 50.5 21 L 45.5 21 C 45.5 25 44.5 27 42 28 C 37 29 30 30 26 32 C 21 34 19 39 18 45 C 17 52 17 58 17 64 " +
        "C 15 70 13 78 13 86 C 13 92 14 96 14 99 C 13 103 14 108 17 109 C 20 109 22 105 22 99 C 23 95 24 88 25 80 " +
        "C 26 74 28 68 29 62 C 30 58 31 55 32 52 C 33 60 34 70 34 80 C 33 86 32 92 32 100 C 31 115 32 130 34 147 " +
        "C 34 152 33 158 34 166 C 35 175 36 182 36 189 C 34 192 33 195 35 197 L 44 197 C 46 195 45 191 45 188 " +
        "C 46 180 47 170 47 162 C 47 157 47 153 47 149 C 48 135 49 120 49.5 106 L 50.5 106 Z");

    static readonly (MuscleGroup Muscle, PathF Path)[] Shared = Shapes(
        (MuscleGroup.Shoulders, "M 34 31 C 27 31 22 35 21 42 C 20 47 21 50 22 52 C 25 47 28 43 33 41 C 34 37 35 34 34 31 Z"),
        (MuscleGroup.Forearms, "M 18.5 66.5 C 22.5 65.5 26 67.5 26 71 C 25.5 78 23 88 21.5 96 L 17.5 96 C 15.5 88 14 79 14.5 73 C 15 69 16.5 67 18.5 66.5 Z"));

    static readonly (MuscleGroup Muscle, PathF Path)[] Front = Shapes(
        (MuscleGroup.Traps, "M 45 25 C 42 28 38 30 34 31 L 44 31 Z"),
        (MuscleGroup.Chest, "M 49 34 L 49 50 C 44 52 38 51 34 48 C 32 45 32 41 34 37 C 38 34 44 33 49 34 Z"),
        (MuscleGroup.Biceps, "M 23.5 44 C 27.5 44 29.5 49 29.5 55 C 29.5 61 27.5 65 25 66 C 22 65 19.5 60 19.5 54.5 C 19.5 49 21 44.5 23.5 44 Z"),
        // Rectus abdominis blocks, then the oblique
        (MuscleGroup.Abs, "M 43 53 L 49 53 L 49 60 L 43 60 Z"),
        (MuscleGroup.Abs, "M 43 61.5 L 49 61.5 L 49 68.5 L 43 68.5 Z"),
        (MuscleGroup.Abs, "M 43 70 L 49 70 L 49 77 L 43 77 Z"),
        (MuscleGroup.Abs, "M 43 78.5 L 49 78.5 L 49 90 C 46 89 44 86 43 83 Z"),
        (MuscleGroup.Abs, "M 35 50 C 38 53 41 55 42 58 L 42 82 C 39 84 37 86 36 88 C 35 80 35 70 34 60 Z"),
        // Vastus lateralis, rectus femoris, vastus medialis
        (MuscleGroup.Quads, "M 33.5 103 C 31.5 116 32 132 35.5 147 C 37.5 145 38.5 140 38.5 132 C 38.5 121 37.5 110 35.5 101 Z"),
        (MuscleGroup.Quads, "M 39.5 100 C 43 104 45.5 114 45.5 124 C 45.5 132 43.5 140 41.5 145 C 40 139 39.5 130 39.5 120 C 39.5 112 39 105 39.5 100 Z"),
        (MuscleGroup.Quads, "M 47.5 121 C 49.5 128 50 138 47.5 147 C 44 148 42.5 146 43 141 C 44 134 45.5 127 47.5 121 Z"),
        (MuscleGroup.Calves, "M 34.5 151 C 32.5 158 33 168 36 180 C 38.5 176 39.5 166 39.5 155 C 38 152 36 151 34.5 151 Z"),
        (MuscleGroup.Calves, "M 42 152 C 46.5 154 48 164 46 178 C 43.5 174 42 164 42 152 Z"));

    static readonly (MuscleGroup Muscle, PathF Path)[] Back = Shapes(
        (MuscleGroup.Traps, "M 50 24 C 46 26 42 29 35 31 C 40 34 45 42 48 55 L 50 58 L 50 24 Z"),
        // Teres / infraspinatus, then the lat
        (MuscleGroup.Back, "M 36 34 C 40 35 44 38 46 44 C 42 44 38 42 35 40 Z"),
        (MuscleGroup.Back, "M 34 40 C 38 42 44 46 48 56 L 48 62 C 45 66 41 72 38 78 C 36 70 34 60 33 48 Z"),
        (MuscleGroup.LowerBack, "M 49.3 62 L 49.3 90 C 47 90 44.5 89 43 87 C 42.5 80 42.5 72 44.5 66 C 46 64 47.5 63 49.3 62 Z"),
        (MuscleGroup.Triceps, "M 22 44 C 26 43 29.5 47 29.5 54 C 29.5 58 28.5 62 27 64 L 24.5 60 L 22 64 C 20 61 19 57 19.2 52 C 19.4 48 20 45 22 44 Z"),
        (MuscleGroup.Glutes, "M 49.5 90 C 49.5 97 49.5 104 47 108.5 C 43 111 37 110 34 106 C 31.5 101 32 94 35 90 C 39 87 45 87.5 49.5 90 Z"),
        // Biceps femoris, semitendinosus
        (MuscleGroup.Hamstrings, "M 33.5 111 C 32 122 33.5 136 38 148 C 40 146 40.5 140 40.5 130 C 40.5 122 40 116 39.5 112 C 37.5 110.5 35.5 110.5 33.5 111 Z"),
        (MuscleGroup.Hamstrings, "M 41.5 112 C 44 110.5 47 110.5 48.5 112.5 C 48.8 124 47.5 138 44 148 C 42.5 146 41.5 138 41.5 130 Z"),
        // Gastrocnemius heads
        (MuscleGroup.Calves, "M 35 151 C 32.5 158 33 168 37 178 C 39.5 174 40.5 164 40.5 152 Z"),
        (MuscleGroup.Calves, "M 41.5 152 C 47 155 48 166 44.5 180 C 42 174 41.5 162 41.5 152 Z"));

    static (MuscleGroup, PathF)[] Shapes(params (MuscleGroup Muscle, string Path)[] shapes) =>
        shapes.Select(s => (s.Muscle, PathBuilder.Build(s.Path))).ToArray();

    void DrawFigure(ICanvas c, float x, float y, float s, bool front)
    {
        c.SaveState();
        c.Translate(x, y);
        c.Scale(s, s);

        c.FillColor = Body;
        c.FillEllipse(41.5f, 2, 17, 20);
        Mirrored(c, () => c.FillPath(Silhouette));

        c.StrokeColor = Separator;
        c.StrokeSize = 0.6f;
        c.StrokeLineJoin = LineJoin.Round;
        foreach (var (muscle, path) in Shared.Concat(front ? Front : Back))
        {
            c.FillColor = _fill(muscle);
            Mirrored(c, () =>
            {
                c.FillPath(path);
                c.DrawPath(path);
            });
        }

        c.RestoreState();
    }

    /// <summary>Draws once as given and once mirrored around x = 50.</summary>
    static void Mirrored(ICanvas c, Action draw)
    {
        draw();
        c.SaveState();
        c.Translate(100, 0);
        c.Scale(-1, 1);
        draw();
        c.RestoreState();
    }
}
