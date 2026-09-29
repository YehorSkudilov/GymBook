namespace GymBook.Controls;

/// <summary>
/// Confetti: a burst from the top when it appears, then a light, endless drift of pieces that flutter as they fall
/// (each one turning, and flipping so it catches the light). For the workout-complete screen.
/// </summary>
public class ConfettiView : AnimatedDrawingView
{
    static readonly Color[] Colours =
    [
        Color.FromArgb("#3F7DFF"), Color.FromArgb("#2ED47A"), Color.FromArgb("#FFB020"),
        Color.FromArgb("#FF4D5E"), Color.FromArgb("#A77BFF"), Color.FromArgb("#4FD1FF"),
    ];

    const int Count = 90;
    readonly Piece[] _pieces;

    public ConfettiView()
    {
        var random = new Random(7);
        _pieces = Enumerable.Range(0, Count).Select(i => new Piece(
            X: (float)random.NextDouble(),
            // The first half bursts out together; the rest join in over the next few seconds.
            Delay: i < Count / 2 ? (float)random.NextDouble() * 0.35f : 0.6f + (float)random.NextDouble() * 3f,
            Fall: 3.2f + (float)random.NextDouble() * 2.4f,
            Sway: 8 + (float)random.NextDouble() * 22,
            Phase: (float)(random.NextDouble() * Math.PI * 2),
            Spin: 90 + (float)random.NextDouble() * 270,
            Size: 6 + (float)random.NextDouble() * 6,
            Colour: Colours[i % Colours.Length])).ToArray();
    }

    protected override void DrawFrame(ICanvas canvas, RectF rect, float seconds)
    {
        foreach (var p in _pieces)
        {
            var t = seconds - p.Delay;
            if (t < 0)
                continue;
            // Round and round: each piece falls through, then starts again from the top.
            var local = t % p.Fall;
            var progress = local / p.Fall;
            var y = -20 + progress * (rect.Height + 40);
            var x = p.X * rect.Width + MathF.Sin(local * 2.2f + p.Phase) * p.Sway;
            // Faded in at the top and out near the bottom, so nothing pops.
            var alpha = Math.Clamp(Math.Min(progress * 8, (1 - progress) * 4), 0, 1) * 0.9f;
            var angle = local * p.Spin + p.Phase * 57.3f;
            // Flipping over: its width shrinks and grows like a turning card.
            var flip = MathF.Abs(MathF.Cos(local * 3.1f + p.Phase));

            canvas.SaveState();
            canvas.Translate(x, y);
            canvas.Rotate(angle);
            canvas.FillColor = p.Colour.WithAlpha(alpha);
            canvas.FillRoundedRectangle(-p.Size / 2 * flip, -p.Size * 0.3f, Math.Max(1, p.Size * flip), p.Size * 0.6f, 1.5f);
            canvas.RestoreState();
        }
    }

    readonly record struct Piece(float X, float Delay, float Fall, float Sway, float Phase, float Spin, float Size, Color Colour);
}
