namespace GymBook.Controls;

/// <summary>
/// A ring showing how recovered a workout's muscles are: it fills up to <see cref="Readiness"/> when shown, with a
/// lightning bolt in the middle that pulses faster (and brighter) the readier you are.
/// </summary>
public class ReadinessRingView : AnimatedDrawingView
{
    public static readonly BindableProperty ReadinessProperty =
        BindableProperty.Create(nameof(Readiness), typeof(double), typeof(ReadinessRingView), 1.0,
            propertyChanged: (b, _, _) => ((ReadinessRingView)b)._filledAt = null);

    public static readonly BindableProperty TintProperty =
        BindableProperty.Create(nameof(Tint), typeof(Color), typeof(ReadinessRingView), Color.FromArgb("#3F7DFF"));

    /// <summary>0 to 1.</summary>
    public double Readiness
    {
        get => (double)GetValue(ReadinessProperty);
        set => SetValue(ReadinessProperty, value);
    }

    public Color Tint
    {
        get => (Color)GetValue(TintProperty);
        set => SetValue(TintProperty, value);
    }

    const float FillSeconds = 0.9f;
    float? _filledAt;

    protected override void DrawFrame(ICanvas canvas, RectF rect, float t)
    {
        // The fill animates from empty each time the value changes.
        _filledAt ??= t;
        var progress = Math.Clamp((t - _filledAt.Value) / FillSeconds, 0, 1);
        var eased = 1 - MathF.Pow(1 - progress, 3);
        var value = (float)Math.Clamp(Readiness, 0, 1) * eased;

        var s = Math.Min(rect.Width, rect.Height);
        var stroke = s * 0.11f;
        var box = new RectF(rect.Center.X - s / 2 + stroke / 2, rect.Center.Y - s / 2 + stroke / 2, s - stroke, s - stroke);
        canvas.StrokeSize = stroke;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeColor = Color.FromArgb("#272C39");
        canvas.DrawEllipse(box);
        if (value > 0.005f)
        {
            canvas.StrokeColor = Tint;
            // Clockwise from the top.
            canvas.DrawArc(box, 90, 90 - 360 * value, true, false);
        }

        // The bolt beats faster, and glows more, when ready.
        var ready = (float)Math.Clamp(Readiness, 0, 1);
        var beat = 0.5f + 0.5f * MathF.Sin(t * (2 + 5 * ready));
        var scale = 1 + 0.08f * beat * ready;
        canvas.FillColor = Tint.WithAlpha(0.14f + 0.18f * beat * ready);
        canvas.FillCircle(rect.Center.X, rect.Center.Y, s * 0.26f * scale);
        canvas.FillColor = Tint;
        canvas.FillPath(Bolt(rect.Center.X, rect.Center.Y, s * 0.3f * scale));
    }

    static PathF Bolt(float x, float y, float size)
    {
        var path = new PathF();
        path.MoveTo(x + size * 0.12f, y - size * 0.55f);
        path.LineTo(x - size * 0.32f, y + size * 0.08f);
        path.LineTo(x - size * 0.02f, y + size * 0.08f);
        path.LineTo(x - size * 0.12f, y + size * 0.55f);
        path.LineTo(x + size * 0.32f, y - size * 0.08f);
        path.LineTo(x + size * 0.02f, y - size * 0.08f);
        path.Close();
        return path;
    }
}

public enum GlowShape { Card, Button }

/// <summary>
/// A soft animated glow in <see cref="Tint"/>, stronger and livelier the higher <see cref="Readiness"/>: drifting
/// light behind a card, or a pulse around a button (sized to the view, drawn under the button).
/// </summary>
public class ReadinessGlowView : AnimatedDrawingView
{
    public static readonly BindableProperty ReadinessProperty =
        BindableProperty.Create(nameof(Readiness), typeof(double), typeof(ReadinessGlowView), 1.0);

    public static readonly BindableProperty TintProperty =
        BindableProperty.Create(nameof(Tint), typeof(Color), typeof(ReadinessGlowView), Color.FromArgb("#3F7DFF"));

    public static readonly BindableProperty ShapeProperty =
        BindableProperty.Create(nameof(Shape), typeof(GlowShape), typeof(ReadinessGlowView), GlowShape.Card);

    public double Readiness
    {
        get => (double)GetValue(ReadinessProperty);
        set => SetValue(ReadinessProperty, value);
    }

    public Color Tint
    {
        get => (Color)GetValue(TintProperty);
        set => SetValue(TintProperty, value);
    }

    public GlowShape Shape
    {
        get => (GlowShape)GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    protected override void DrawFrame(ICanvas canvas, RectF rect, float t)
    {
        var ready = (float)Math.Clamp(Readiness, 0, 1);
        if (Shape == GlowShape.Button)
            DrawButtonGlow(canvas, rect, t, ready);
        else
            DrawCardGlow(canvas, rect, t, ready);
    }

    // Two soft blobs of light drifting slowly across the card; faint when tired, rich when fresh.
    void DrawCardGlow(ICanvas canvas, RectF rect, float t, float ready)
    {
        var strength = 0.06f + 0.16f * ready;
        var speed = 0.25f + 0.35f * ready;
        for (var i = 0; i < 2; i++)
        {
            var phase = t * speed + i * 2.4f;
            var x = rect.Left + rect.Width * (0.3f + 0.4f * (0.5f + 0.5f * MathF.Sin(phase)));
            var y = rect.Top + rect.Height * (0.35f + 0.3f * (0.5f + 0.5f * MathF.Cos(phase * 0.8f)));
            var r = Math.Max(rect.Width, rect.Height) * (0.45f - i * 0.1f);
            var paint = new RadialGradientPaint([new PaintGradientStop(0, Tint.WithAlpha(strength)), new PaintGradientStop(1, Tint.WithAlpha(0))]);
            canvas.SetFillPaint(paint, new RectF(x - r, y - r, r * 2, r * 2));
            canvas.FillCircle(x, y, r);
        }
    }

    // A glow pulsing out around the button: nothing when tired, a strong, quick beat when fresh.
    void DrawButtonGlow(ICanvas canvas, RectF rect, float t, float ready)
    {
        var vivid = Math.Clamp((ready - 0.4f) / 0.5f, 0, 1);
        if (vivid <= 0)
            return;
        var beat = 0.5f + 0.5f * MathF.Sin(t * (1.5f + 2.5f * vivid));
        var inset = rect.Height * 0.18f * (1 - beat);
        var glow = new RectF(rect.Left + inset, rect.Top + inset, rect.Width - inset * 2, rect.Height - inset * 2);
        canvas.FillColor = Tint.WithAlpha(0.22f * vivid * (0.4f + 0.6f * beat));
        canvas.FillRoundedRectangle(glow, glow.Height / 2);
    }
}
