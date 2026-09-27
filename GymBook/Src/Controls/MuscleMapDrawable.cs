using GymBook.Models;
using GymBook.Services;

namespace GymBook.Controls;

/// <summary>
/// Front and back anatomical figures (traced from a reference muscle chart): a solid body with the muscles laid over it,
/// separated by thin grooves, and each tracked muscle group filled by a colour function.
/// </summary>
public class MuscleMapDrawable : IDrawable
{
    static readonly Color Body = Color.FromArgb("#262A35");
    static readonly Color Neutral = Color.FromArgb("#454C5D");
    static readonly Color Outline = Color.FromArgb("#12141A");
    static readonly Color Groove = Color.FromArgb("#1B1E26");

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
        const float figW = 70, figH = 200, gap = 6;
        var scale = Math.Min(rect.Width / (figW * 2 + gap), rect.Height / figH);
        var totalW = (figW * 2 + gap) * scale;
        var left = rect.X + (rect.Width - totalW) / 2;
        var top = rect.Y + (rect.Height - figH * scale) / 2;

        DrawFigure(canvas, left, top, scale, Front);
        DrawFigure(canvas, left + (figW + gap) * scale, top, scale, Back);
    }

    void DrawFigure(ICanvas c, float x, float y, float s, View view)
    {
        c.SaveState();
        c.Translate(x, y);
        c.Scale(s, s);
        c.StrokeLineJoin = LineJoin.Round;
        c.StrokeLineCap = LineCap.Round;

        // The body, stroked in its own colour so the two mirrored halves meet without a seam.
        c.FillColor = Body;
        c.StrokeColor = Body;
        c.StrokeSize = 0.8f;
        Mirrored(c, () =>
        {
            c.FillPath(view.Body);
            c.DrawPath(view.Body);
        });

        c.StrokeColor = Groove;
        foreach (var shape in view.Shapes)
        {
            c.FillColor = shape.Muscle is { } m ? _fill(m) : Neutral;
            c.StrokeSize = 0.35f;
            Mirrored(c, () =>
            {
                c.FillPath(shape.Path);
                foreach (var fibre in shape.Fibres)
                    c.DrawPath(fibre);
            });
        }

        c.StrokeSize = 0.4f;
        foreach (var line in view.Lines)
            Mirrored(c, () => c.DrawPath(line));

        c.StrokeColor = Outline;
        c.StrokeSize = 0.75f;
        Mirrored(c, () => c.DrawPath(view.Outline));

        c.RestoreState();
    }

    /// <summary>Draws once as given and once mirrored around the figure's centre line (x = 35).</summary>
    static void Mirrored(ICanvas c, Action draw)
    {
        draw();
        c.SaveState();
        c.Translate(70, 0);
        c.Scale(-1, 1);
        draw();
        c.RestoreState();
    }

    /// <summary>A muscle and the fibre lines drawn inside it. Muscles outside the tracked groups have no group and stay neutral.</summary>
    readonly record struct Shape(MuscleGroup? Muscle, PathF Path, PathF[] Fibres);

    /// <summary>
    /// One side of the body, as its left half on a 70 x 200 figure: the filled body, its outline (open along the centre line),
    /// the muscles laid over it, and grooves that belong to no single muscle.
    /// </summary>
    sealed record View(PathF Body, PathF Outline, Shape[] Shapes, PathF[] Lines);

    static Shape S(MuscleGroup? muscle, string path, params string[] fibres) =>
        new(muscle, PathBuilder.Build(path), fibres.Select(PathBuilder.Build).ToArray());

    // Generated from the traced reference chart; edit the paths as a set so the grooves between muscles stay even.
    static readonly View Front = new(
        Body: PathBuilder.Build(
            "M 35 2 C 31.7 2 28.3 4 27 6.7 C 26 8.7 25.7 11.3 25.8 13.7 C 24.7 13.7 24.5 15.3 24.9 17.3 C 25.1 19 26 19.9 " +
            "27 19.7 C 27.7 21.7 28.2 23.3 28.5 25 C 28.5 27.3 28.3 29.3 27.7 31 C 24.3 33 20.3 34.3 17 35.3 C 13 36 10 37 " +
            "8.3 39 C 6.7 41.3 5.9 44.3 5.8 47.3 C 5.8 50.7 6.1 53.3 6.2 55.3 C 6.5 60 6.6 65.3 6.2 69.3 C 5.9 71.3 5.4 " +
            "73.3 5.1 75.3 C 4.5 80 4.2 84.7 4.5 88.7 C 4.9 93.3 5.8 97.3 7 101.3 C 6.7 105.3 6 110 5.8 113.3 C 6.3 115.3 " +
            "7.7 117 9.3 117.9 C 11.3 118.5 13.7 118 15.3 116.7 C 16.7 115.3 17.1 113.3 16.9 110.7 C 16.5 108 16.1 105.7 " +
            "16.6 103.3 C 16.7 100 16.5 96 16.3 92 C 16.2 86.7 16.1 81.3 16.5 77.3 C 16.7 74.7 17 72 17.1 69.3 C 17.3 65.3 " +
            "17.5 60.7 17.7 57.3 C 18.7 61.3 20 66.7 20.9 72 C 21.3 76 21 80 20.5 83.3 C 19.7 88 18.5 93.3 17.9 98 C 17.4 " +
            "103.3 17.4 110 17.7 115.3 C 18 122 19.3 128 21.7 133.3 C 22.5 136 22.7 140 22.5 143.7 C 22.3 146 21.7 147.3 " +
            "21.2 149.3 C 20.5 153.3 20.5 158.7 21 163.3 C 21.7 168.7 24 175.3 26 180.7 C 26.9 184 27.2 187.3 26.7 192 C " +
            "25.7 193.7 23.3 194.3 21.3 195.3 C 19.3 196 19 197.7 20.3 198.5 C 23.7 199 28.3 198.9 32.3 198.5 C 34 198.3 " +
            "34.9 197.7 34.7 195.7 C 34.5 192.7 33.8 189.3 33.6 186 C 33.4 182 33.1 178 33.2 174 C 33.4 169.3 34.3 165.3 " +
            "34.6 160.7 C 34.8 157.3 34.5 154 34.2 151.3 C 33.7 148.7 33.3 146 33.2 143.3 C 33.3 140 33.7 137.3 33.9 134 C " +
            "34.3 128 34.7 121.3 34.8 113.3 L 34.9 105.3 L 35 105.3 Z"),
        Outline: PathBuilder.Build(
            "M 35 2 C 31.7 2 28.3 4 27 6.7 C 26 8.7 25.7 11.3 25.8 13.7 C 24.7 13.7 24.5 15.3 24.9 17.3 C 25.1 19 26 19.9 " +
            "27 19.7 C 27.7 21.7 28.2 23.3 28.5 25 C 28.5 27.3 28.3 29.3 27.7 31 C 24.3 33 20.3 34.3 17 35.3 C 13 36 10 37 " +
            "8.3 39 C 6.7 41.3 5.9 44.3 5.8 47.3 C 5.8 50.7 6.1 53.3 6.2 55.3 C 6.5 60 6.6 65.3 6.2 69.3 C 5.9 71.3 5.4 " +
            "73.3 5.1 75.3 C 4.5 80 4.2 84.7 4.5 88.7 C 4.9 93.3 5.8 97.3 7 101.3 C 6.7 105.3 6 110 5.8 113.3 C 6.3 115.3 " +
            "7.7 117 9.3 117.9 C 11.3 118.5 13.7 118 15.3 116.7 C 16.7 115.3 17.1 113.3 16.9 110.7 C 16.5 108 16.1 105.7 " +
            "16.6 103.3 C 16.7 100 16.5 96 16.3 92 C 16.2 86.7 16.1 81.3 16.5 77.3 C 16.7 74.7 17 72 17.1 69.3 C 17.3 65.3 " +
            "17.5 60.7 17.7 57.3 C 18.7 61.3 20 66.7 20.9 72 C 21.3 76 21 80 20.5 83.3 C 19.7 88 18.5 93.3 17.9 98 C 17.4 " +
            "103.3 17.4 110 17.7 115.3 C 18 122 19.3 128 21.7 133.3 C 22.5 136 22.7 140 22.5 143.7 C 22.3 146 21.7 147.3 " +
            "21.2 149.3 C 20.5 153.3 20.5 158.7 21 163.3 C 21.7 168.7 24 175.3 26 180.7 C 26.9 184 27.2 187.3 26.7 192 C " +
            "25.7 193.7 23.3 194.3 21.3 195.3 C 19.3 196 19 197.7 20.3 198.5 C 23.7 199 28.3 198.9 32.3 198.5 C 34 198.3 " +
            "34.9 197.7 34.7 195.7 C 34.5 192.7 33.8 189.3 33.6 186 C 33.4 182 33.1 178 33.2 174 C 33.4 169.3 34.3 165.3 " +
            "34.6 160.7 C 34.8 157.3 34.5 154 34.2 151.3 C 33.7 148.7 33.3 146 33.2 143.3 C 33.3 140 33.7 137.3 33.9 134 C " +
            "34.3 128 34.7 121.3 34.8 113.3 L 34.9 105.3"),
        Shapes:
        [
            S(MuscleGroup.Neck, "M 28.5 26.7 C 30 28.7 32.3 30 35 30.3 L 35 38.4 C 32.3 37.1 29.7 34.7 27.9 31.7 C 28.3 30.3 28.5 28.4 28.5 26.7 Z"),
            S(MuscleGroup.Traps, "M 27.4 32.4 C 25 33.7 21.7 34.8 18 35.7 C 15.7 36.3 13.7 36.6 12.1 37.1 C 16 36.7 20 36.7 24.1 37.1 C 25.4 35.7 26.6 34.1 27.4 32.4 Z"),
            S(MuscleGroup.Shoulders, "M 23.7 37.9 C 20.3 40 17.7 42.8 16.3 45.7 C 15.5 47.9 15.5 50.1 15.9 52.4 C 14.3 53.5 12.3 53.9 10.3 53.8 C 9 53.7 7.9 53.5 7.1 53.1 C 6.5 50.7 6.5 47.7 6.7 45 C 7.3 41.7 9 39.3 11.7 38.3 C 15 37.3 19.7 37.3 23.7 37.9 Z", "M 13 38.7 C 10.7 42.3 9.4 46.7 9 52"),
            S(MuscleGroup.Chest, "M 35 39.6 C 31.3 39 28 38.7 25 38.5 C 21.7 40.8 18.7 43.5 17.2 46.5 C 16.6 49.2 16.9 51.9 18.3 53.8 C 20.6 55.9 24.1 56.5 27.7 56.1 C 30.3 55.9 33 55.3 35 54.7 Z", "M 33.7 42.7 C 28.3 43.2 23 44.7 18 47.7", "M 33.7 49 C 28.3 49.6 23 50.3 18.2 51.7"),
            S(MuscleGroup.Triceps, "M 7 54.8 C 8 55.2 8.7 56.4 8.9 58.4 C 8.7 62.7 8.9 67.3 9.5 71.9 C 8.5 72.7 7.6 72.7 6.9 72.1 C 7.1 67.3 7.1 60.7 7 54.8 Z"),
            S(MuscleGroup.Biceps, "M 10.7 54.7 C 13.3 54.3 15.7 54.9 16.7 57.1 C 17 61.3 16.9 66.7 16.6 71.3 C 15.2 72.9 12.7 73.2 10.7 72.3 C 9.8 68 9.4 62.7 9.5 58 C 9.7 56.4 10.1 55.3 10.7 54.7 Z"),
            S(MuscleGroup.Forearms, "M 6.1 74.3 C 8.3 73.4 11.3 73.3 13.7 73.7 C 15.7 74.3 16.7 75.6 16.6 77.7 C 16.3 83.3 16.3 89.3 16.5 94.7 C 16.6 98 16.6 100.7 16.5 102.5 C 13.7 103.2 10.3 103.1 7.7 102.2 C 6.3 98 5.3 93.3 5.1 88 C 5.1 82.7 5.3 78 6.1 74.3 Z", "M 11.3 74.4 C 12.7 81.3 12.7 90.7 11 102"),
            S(MuscleGroup.Abs, "M 18.6 58.7 C 20.3 57.6 22.7 57.2 24.7 57.3 C 25.7 57.5 26.5 57.7 27 58 C 27 65.3 27 72.7 27.1 80 C 27.2 83.3 27.7 86.7 28.3 89.3 C 26.3 87.3 24 85.3 21.7 83.7 C 21.5 80 21.5 76 21.4 72.7 C 20.9 67.3 19.9 62.7 18.6 58.7 Z", "M 19.4 61 L 23.3 59.7", "M 20.2 65.1 L 24.2 63.6", "M 20.9 69.2 L 25 67.6"),
            S(MuscleGroup.Abs, "M 27.9 57.1 C 30.3 56.8 32.7 56.5 34.6 56.3 L 34.6 62.2 L 27.9 62.5 Z"),
            S(MuscleGroup.Abs, "M 27.9 63.3 L 34.6 63 L 34.6 68.5 L 27.9 68.7 Z"),
            S(MuscleGroup.Abs, "M 27.9 69.5 L 34.6 69.3 L 34.6 75 L 28 75.3 Z"),
            S(MuscleGroup.Abs, "M 28 76.1 L 34.6 75.8 L 34.6 98.7 C 32.3 96.7 30.3 92.7 29.1 88 C 28.3 84 28 80 28 76.1 Z"),
            S(MuscleGroup.Quads, "M 20.9 85.7 C 21.8 86.7 22.7 88 23.4 89.7 C 24.2 96.7 24.7 106.7 24.9 116.7 C 25 122.7 25 128 24.9 133 C 23.7 133.7 22.6 133.5 21.8 132.7 C 19.9 128 18.7 122 18.5 115.3 C 18.3 109.3 18.3 103.3 18.5 98 C 19 93.3 19.9 89 20.9 85.7 Z"),
            S(MuscleGroup.Quads, "M 24.1 87 C 25.3 87.9 26.7 89.7 27.8 92 C 28.9 95 29.7 98.7 30.1 102.3 C 30.3 106.7 30.2 111.3 29.8 116 C 29.3 122 28.2 128 26.9 133.2 L 25.9 133.2 C 25.9 126 25.8 117.3 25.7 108.7 C 25.5 101.3 25.1 93.7 24.1 87 Z"),
            S(null, "M 31 103.5 C 32.2 104.3 33.4 105.1 34.6 105.5 L 34.7 113.3 C 34.6 116 34.1 118 33.2 119.7 C 32.3 120.5 31.4 121.2 30.5 121.5 C 30.7 116 30.9 109.3 31 103.5 Z"),
            S(MuscleGroup.Quads, "M 31.4 122.4 C 32.7 121.3 33.5 120.5 34.1 119.9 C 34.2 124 34.1 128.3 33.7 132.7 C 32.3 134 30.3 134.3 28.5 133.6 C 29.1 130 30.1 126 31.4 122.4 Z"),
            S(MuscleGroup.Calves, "M 21.9 149.7 C 23.3 148.5 25.1 148 27 148 C 27.2 154.7 27.3 164 27.1 173.3 C 27 178 26.7 182 26.4 186 C 25.3 182 23.7 176.7 22.3 170.7 C 21.3 165.3 21.1 160 21.1 155.3 C 21.2 153 21.5 151 21.9 149.7 Z"),
            S(MuscleGroup.Calves, "M 30.1 148 C 31.7 148.2 32.9 149.2 33.5 151 C 34.1 154 34.2 157.3 34 160.7 C 33.7 166 33 170.7 32.3 175.3 C 31.5 173.3 30.7 170 30.3 166 C 30 160 29.9 154 30.1 148 Z"),
        ],
        Lines:
        [
            PathBuilder.Build("M 29.3 27.3 C 30.7 30.7 32.7 33.7 34.7 35.7"),
            PathBuilder.Build("M 25 135.7 C 24.3 139 24.7 142.7 26 145 C 28.3 146 30.7 145.3 32 143.3 C 32.7 140 32.3 137.3 31 135.7"),
        ]);

    static readonly View Back = new(
        Body: PathBuilder.Build(
            "M 35 2 C 32.3 2 30 3 28.3 4.7 C 27 6.3 26.4 9.3 26.3 12.3 L 26.3 14.2 C 25.2 14.3 25.1 16 25.3 17.9 C 25.5 " +
            "19.3 26 20.4 27.1 20.7 C 27.7 21.7 28.1 22.5 28.5 23.1 C 29 25.3 29.3 27.7 29.3 29.8 C 27.7 31.7 26.2 32.7 " +
            "24.5 33.5 C 23 34.3 21.5 34.9 20 35.3 C 18.2 35.8 16.5 36.1 14.8 36.5 C 13.1 37.3 11.5 38.7 10.4 40.1 C 8.9 " +
            "41.5 7.9 43.1 7.4 44.6 C 7.1 46 7 47.7 7.1 49.1 C 6.6 51 6.2 53 5.9 54.9 C 5.5 58 5.3 61 5.2 63.9 C 5.1 67 4.8 " +
            "70 4.5 72.7 C 4 75.3 3.6 77.7 3.3 80.1 C 3.2 82.7 3.2 85 3.3 87.5 C 3.7 90 4.3 92.5 5.2 94.9 C 5.9 97.5 6.6 " +
            "100 7.4 102.4 C 8.7 102.7 9.3 102.7 9.6 102.9 C 9.6 106 9.5 109 9.6 111.9 C 9.9 113 10.5 114 11.1 114.8 C 11.9 " +
            "115.9 12.8 116.7 13.7 117.1 C 14.6 116.8 15.3 116.5 15.9 115.9 C 16.6 114.4 16.9 112.8 17.1 111.1 C 17.1 108.3 " +
            "16.9 105.6 16.7 102.9 C 16.2 102.7 16 102.5 15.9 102.4 C 15.9 98.7 15.9 95 15.9 91.3 C 15.8 87.5 15.7 83.8 " +
            "15.5 80.1 C 15.4 77.7 15.3 75.2 15.2 72.7 C 14.9 71.7 14.7 70.8 14.5 69.8 C 14.2 67.1 13.9 64.3 13.7 61.6 C " +
            "13.6 58 13.5 54.3 13.3 50.5 C 14.2 51.1 15 51.7 15.7 52.4 C 16.5 56 17.3 59.7 18.1 63.3 C 18.9 66.7 19.9 70 " +
            "20.7 73.3 C 21 75.7 21.1 78 21 80.1 C 20.7 82.3 20.4 84.2 20 86 C 19.3 89 18.5 92 17.8 94.9 C 17.3 97.9 16.9 " +
            "100.9 16.9 103.9 C 16.9 105.3 17 106.7 17.1 107.3 C 17.5 112.3 17.9 117.3 18.5 122 C 19.3 125.3 20.3 128.7 " +
            "21.5 131.9 C 22 134 22.5 136.3 22.9 138.5 C 23.3 140.5 23.6 142.5 23.7 144.5 C 23.3 146 22.8 147.5 22.2 148.9 " +
            "C 21.7 151.3 21.3 153.8 21.1 156.3 C 21.1 158.7 21.2 161.2 21.5 163.7 C 22 167.2 22.8 170.7 23.7 174 C 24.4 " +
            "177 25 180 25.5 182.9 C 25.9 185.3 26.1 187.9 26.3 190.4 C 25.2 192 24 193.5 22.9 194.8 C 22.3 195.5 21.7 " +
            "196.2 21.5 197.1 C 21.6 197.7 21.9 198.2 22.2 198.5 C 25.7 198.8 29.1 198.8 32.6 198.5 C 33.3 198 33.9 197.3 " +
            "34.1 196.5 C 34.1 194 34 191.3 33.9 188.9 C 33.7 183.3 33.6 178 33.5 172.7 C 33.6 167.3 33.9 162 34.1 156.7 C " +
            "34.1 152 34.1 146.7 34.1 141.3 C 34.2 135 34.4 128.7 34.5 122 C 34.6 117.3 34.7 112.7 34.7 108 L 35 108 Z"),
        Outline: PathBuilder.Build(
            "M 35 2 C 32.3 2 30 3 28.3 4.7 C 27 6.3 26.4 9.3 26.3 12.3 L 26.3 14.2 C 25.2 14.3 25.1 16 25.3 17.9 C 25.5 " +
            "19.3 26 20.4 27.1 20.7 C 27.7 21.7 28.1 22.5 28.5 23.1 C 29 25.3 29.3 27.7 29.3 29.8 C 27.7 31.7 26.2 32.7 " +
            "24.5 33.5 C 23 34.3 21.5 34.9 20 35.3 C 18.2 35.8 16.5 36.1 14.8 36.5 C 13.1 37.3 11.5 38.7 10.4 40.1 C 8.9 " +
            "41.5 7.9 43.1 7.4 44.6 C 7.1 46 7 47.7 7.1 49.1 C 6.6 51 6.2 53 5.9 54.9 C 5.5 58 5.3 61 5.2 63.9 C 5.1 67 4.8 " +
            "70 4.5 72.7 C 4 75.3 3.6 77.7 3.3 80.1 C 3.2 82.7 3.2 85 3.3 87.5 C 3.7 90 4.3 92.5 5.2 94.9 C 5.9 97.5 6.6 " +
            "100 7.4 102.4 C 8.7 102.7 9.3 102.7 9.6 102.9 C 9.6 106 9.5 109 9.6 111.9 C 9.9 113 10.5 114 11.1 114.8 C 11.9 " +
            "115.9 12.8 116.7 13.7 117.1 C 14.6 116.8 15.3 116.5 15.9 115.9 C 16.6 114.4 16.9 112.8 17.1 111.1 C 17.1 108.3 " +
            "16.9 105.6 16.7 102.9 C 16.2 102.7 16 102.5 15.9 102.4 C 15.9 98.7 15.9 95 15.9 91.3 C 15.8 87.5 15.7 83.8 " +
            "15.5 80.1 C 15.4 77.7 15.3 75.2 15.2 72.7 C 14.9 71.7 14.7 70.8 14.5 69.8 C 14.2 67.1 13.9 64.3 13.7 61.6 C " +
            "13.6 58 13.5 54.3 13.3 50.5 C 14.2 51.1 15 51.7 15.7 52.4 C 16.5 56 17.3 59.7 18.1 63.3 C 18.9 66.7 19.9 70 " +
            "20.7 73.3 C 21 75.7 21.1 78 21 80.1 C 20.7 82.3 20.4 84.2 20 86 C 19.3 89 18.5 92 17.8 94.9 C 17.3 97.9 16.9 " +
            "100.9 16.9 103.9 C 16.9 105.3 17 106.7 17.1 107.3 C 17.5 112.3 17.9 117.3 18.5 122 C 19.3 125.3 20.3 128.7 " +
            "21.5 131.9 C 22 134 22.5 136.3 22.9 138.5 C 23.3 140.5 23.6 142.5 23.7 144.5 C 23.3 146 22.8 147.5 22.2 148.9 " +
            "C 21.7 151.3 21.3 153.8 21.1 156.3 C 21.1 158.7 21.2 161.2 21.5 163.7 C 22 167.2 22.8 170.7 23.7 174 C 24.4 " +
            "177 25 180 25.5 182.9 C 25.9 185.3 26.1 187.9 26.3 190.4 C 25.2 192 24 193.5 22.9 194.8 C 22.3 195.5 21.7 " +
            "196.2 21.5 197.1 C 21.6 197.7 21.9 198.2 22.2 198.5 C 25.7 198.8 29.1 198.8 32.6 198.5 C 33.3 198 33.9 197.3 " +
            "34.1 196.5 C 34.1 194 34 191.3 33.9 188.9 C 33.7 183.3 33.6 178 33.5 172.7 C 33.6 167.3 33.9 162 34.1 156.7 C " +
            "34.1 152 34.1 146.7 34.1 141.3 C 34.2 135 34.4 128.7 34.5 122 C 34.6 117.3 34.7 112.7 34.7 108"),
        Shapes:
        [
            S(MuscleGroup.Neck, "M 29.2 24.4 C 31 23.5 33 23 35 22.7 L 35 30.9 C 32.9 30.7 30.9 30.3 29.3 29.7 C 29.3 27.9 29.3 26.1 29.2 24.4 Z"),
            S(MuscleGroup.Traps, "M 35 31.9 L 35 70.3 C 32.7 66.3 30 61 27.7 55.7 C 25.7 51 23.7 45.7 21.3 41.7 C 19.7 39.9 17.7 38.6 15.7 37.5 C 19.3 36.3 23 34.7 26.3 32.3 C 27.2 31.7 27.9 31.3 28.5 31.1 C 30.7 31.5 32.8 31.7 35 31.9 Z", "M 29 30.7 C 30.7 32.7 32.7 34 34.7 34.7"),
            S(MuscleGroup.Shoulders, "M 15.1 38.5 C 17.3 39.4 19.3 40.7 20.7 42.6 C 20.4 45 19.3 47 17.5 48.7 C 15.6 50.3 13.3 51.7 11 52.9 C 9.3 53.6 7.9 53.9 6.8 53.9 C 6.7 50.7 6.7 47.5 7.4 45.1 C 8.2 42.5 9.7 40.5 11.7 39.3 C 12.9 38.7 14 38.5 15.1 38.5 Z", "M 12 40.7 C 11 44 10.7 47.7 11 51.7"),
            S(MuscleGroup.Back, "M 21.9 43.6 C 23.3 45.9 24.8 48.7 25.6 51.1 C 24 51.5 21.7 51.5 19.8 51 C 18.5 50.6 17.7 50.1 17.2 49.7 C 18.9 47.9 20.6 45.9 21.9 43.6 Z"),
            S(MuscleGroup.Back, "M 16.9 53.2 C 19.1 52.7 21.3 52.3 23.3 51.7 C 24.3 51.5 25 51.9 25.5 52.7 C 27.1 56 28.9 59.3 31 63.3 C 31.7 65.3 31.6 67.7 30.7 69.7 C 29 72.7 26.7 75 24.3 76.1 C 23.3 76.3 22.7 76 22.2 75.3 C 21 71.3 19.9 67.3 19 63.3 C 18.3 60 17.5 56.7 16.9 53.2 Z", "M 20 57.3 C 23.3 60.7 26 64.7 28 69.3"),
            S(MuscleGroup.Abs, "M 21.7 77.7 C 22.5 78.1 23.5 78.4 24.7 78.4 C 23.9 80.3 23 82.1 22.2 83.9 C 21.7 84.7 21.3 85.3 20.8 85.9 C 21.1 83.3 21.3 80.7 21.3 78.3 Z"),
            S(MuscleGroup.LowerBack, "M 35 71.9 L 35 87.6 C 31.3 87.3 27.7 86.7 24.1 85.7 C 23.1 85.4 22.7 84.7 22.8 84.1 C 23.8 82 25.1 79.7 26.5 78.1 C 28.4 76.1 30.4 73.7 31.9 71.8 C 32.9 71.3 34 71.3 35 71.9 Z", "M 31.3 76.7 C 30.7 80 30.7 83.3 31.3 86.7"),
            S(MuscleGroup.Triceps, "M 7 55.3 C 9 54.9 11.2 54.3 13.1 53.6 C 13.5 57.3 13.5 62 13.4 66 C 13.3 68 13.1 69.7 12.7 71 C 10.7 72 8 72.1 5.6 71.3 C 5.5 66.7 5.7 60.7 7 55.3 Z", "M 9.7 56 C 10 60.7 10 65.3 9.5 69.3"),
            S(MuscleGroup.Forearms, "M 4.8 74 C 7.3 72.9 10.7 72.7 13.2 73.1 C 14.5 73.5 15 74.7 15.1 76.7 C 15.1 83.3 15.3 90 15.4 96.7 C 15.4 99 15.3 100.7 15.2 101.9 C 12.3 102.5 9.3 102.5 7.1 101.9 C 5.4 98 4.1 93.3 3.9 88 C 3.9 82.7 4.1 78 4.8 74 Z", "M 9.7 73.5 C 9 81.3 9.2 90.7 10.3 101.3"),
            S(MuscleGroup.Glutes, "M 35 88.9 L 35 107.2 C 32 108.9 27.7 109.2 24 107.9 C 21.3 106.9 19.5 104.5 18.7 101.2 C 18.4 97.3 19 93.3 20.5 90.3 C 21.5 88.7 23.1 87.7 24.7 87.5 C 28 87.9 31.7 88.4 35 88.9 Z", "M 26 90.7 C 28.7 94.7 31.3 100 33.3 106"),
            S(MuscleGroup.Hamstrings, "M 19.9 109.7 C 21.3 110.3 23 110.5 24.7 110.5 C 24.9 117.3 24.9 124.7 24.7 132 C 24.5 134 24.3 135.7 23.9 137 C 23 136 22.3 134.3 21.6 132.3 C 20.5 128 19.9 122.7 19.7 117.3 C 19.6 114.7 19.7 112 19.9 109.7 Z"),
            S(MuscleGroup.Hamstrings, "M 25.7 110.4 C 28 110.4 30.7 110 34.2 109.2 C 34.2 116 34 122.7 33.5 129.3 C 33.3 132.3 33 134.7 32.6 136.7 C 30.7 137.1 28.3 137.1 26.5 136.7 C 26 132.7 25.7 125.3 25.7 118 Z"),
            S(MuscleGroup.Calves, "M 23.2 148.3 C 25 147.2 26.7 146.7 28.1 146.8 C 28.5 152 28.5 157.3 28.1 162.7 C 27.9 165.7 27.3 168 26.7 169.7 C 25.3 168.7 24 166 22.9 162.7 C 22 159 22 154.7 22.4 151.3 C 22.6 150.2 22.9 149.2 23.2 148.3 Z"),
            S(MuscleGroup.Calves, "M 29.1 146.8 C 31 146.8 32.7 147.3 33.9 148.3 C 34.2 152 34.1 156 33.9 160 C 33.7 164 33.2 168 32.5 171 C 31.5 170.5 30.5 169.3 29.9 167.7 C 29.3 164 29 158.7 29 153.3 Z"),
        ],
        Lines:
        [
            PathBuilder.Build("M 24.7 140 C 27 141.3 29.3 141.7 31.7 141"),
        ]);
}
