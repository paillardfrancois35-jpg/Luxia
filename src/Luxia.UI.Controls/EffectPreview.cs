using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Luxia.UI.Controls;

/// <summary>
/// Dessin d'un effet (doc 60 §5, modèle Daslight « Curve FX / Move FX / Colour FX ») : la forme d'un cycle et la
/// position courante de chaque membre. Trois présentations : courbe (intensité : temps en abscisse, niveau en
/// ordonnée), plan (position : Pan en abscisse, Tilt en ordonnée, croix au centre), bande de couleurs.
/// Sans dépendance métier : le modèle de vue fournit les points, en coordonnées 0-1 (ordonnée 1 = en haut).
/// </summary>
public sealed class EffectPreview : Control
{
    /// <summary>Forme d'un cycle (points 0-1).</summary>
    public static readonly StyledProperty<IReadOnlyList<Point>?> CurveProperty =
        AvaloniaProperty.Register<EffectPreview, IReadOnlyList<Point>?>(nameof(Curve));

    /// <summary>Position courante de chaque membre (points 0-1), dans l'ordre des membres.</summary>
    public static readonly StyledProperty<IReadOnlyList<Point>?> DotsProperty =
        AvaloniaProperty.Register<EffectPreview, IReadOnlyList<Point>?>(nameof(Dots));

    /// <summary>Couleurs d'un cycle (effet de couleur) : une bande en dégradé ou en escalier.</summary>
    public static readonly StyledProperty<IReadOnlyList<Color>?> ColorsProperty =
        AvaloniaProperty.Register<EffectPreview, IReadOnlyList<Color>?>(nameof(Colors));

    /// <summary>Bande de couleurs en escalier (alternance) plutôt qu'en dégradé.</summary>
    public static readonly StyledProperty<bool> SteppedProperty =
        AvaloniaProperty.Register<EffectPreview, bool>(nameof(Stepped));

    /// <summary>Présentation en plan Pan / Tilt.</summary>
    public static readonly StyledProperty<bool> IsPlaneProperty =
        AvaloniaProperty.Register<EffectPreview, bool>(nameof(IsPlane));

    /// <summary>Couleur de la forme (couleur du mode).</summary>
    public static readonly StyledProperty<IBrush?> AccentProperty =
        AvaloniaProperty.Register<EffectPreview, IBrush?>(nameof(Accent));

    private static readonly IBrush Background = new SolidColorBrush(Color.Parse("#0D1117"));
    private static readonly IPen GridPen = new Pen(new SolidColorBrush(Color.Parse("#21262D")), 1);
    private static readonly IPen BorderPen = new Pen(new SolidColorBrush(Color.Parse("#30363D")), 1);
    private static readonly IBrush DefaultAccent = new SolidColorBrush(Color.Parse("#58A6FF"));
    private static readonly IPen DotPen = new Pen(Brushes.Black, 1);

    static EffectPreview()
    {
        AffectsRender<EffectPreview>(CurveProperty, DotsProperty, ColorsProperty, SteppedProperty, IsPlaneProperty, AccentProperty);
    }

    /// <inheritdoc cref="CurveProperty"/>
    public IReadOnlyList<Point>? Curve
    {
        get => GetValue(CurveProperty);
        set => SetValue(CurveProperty, value);
    }

    /// <inheritdoc cref="DotsProperty"/>
    public IReadOnlyList<Point>? Dots
    {
        get => GetValue(DotsProperty);
        set => SetValue(DotsProperty, value);
    }

    /// <inheritdoc cref="ColorsProperty"/>
    public IReadOnlyList<Color>? Colors
    {
        get => GetValue(ColorsProperty);
        set => SetValue(ColorsProperty, value);
    }

    /// <inheritdoc cref="SteppedProperty"/>
    public bool Stepped
    {
        get => GetValue(SteppedProperty);
        set => SetValue(SteppedProperty, value);
    }

    /// <inheritdoc cref="IsPlaneProperty"/>
    public bool IsPlane
    {
        get => GetValue(IsPlaneProperty);
        set => SetValue(IsPlaneProperty, value);
    }

    /// <inheritdoc cref="AccentProperty"/>
    public IBrush? Accent
    {
        get => GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var area = new Rect(Bounds.Size).Deflate(1);
        context.FillRectangle(Background, area, 6);
        context.DrawRectangle(BorderPen, area, 6);
        var inner = area.Deflate(10);
        if (inner.Width <= 0 || inner.Height <= 0)
        {
            return;
        }

        if (IsPlane && Colors is not { Count: > 0 })
        {
            // Plan Pan / Tilt carré : un cercle reste un cercle.
            var side = Math.Min(inner.Width, inner.Height);
            inner = new Rect(inner.Center.X - (side / 2), inner.Center.Y - (side / 2), side, side);
            context.DrawRectangle(GridPen, inner);
        }

        Point ToScreen(Point p) => new(inner.X + (p.X * inner.Width), inner.Bottom - (p.Y * inner.Height));

        if (Colors is { Count: > 0 } colors)
        {
            DrawBand(context, inner, colors);
        }
        else if (IsPlane)
        {
            context.DrawLine(GridPen, new Point(inner.Center.X, inner.Top), new Point(inner.Center.X, inner.Bottom));
            context.DrawLine(GridPen, new Point(inner.Left, inner.Center.Y), new Point(inner.Right, inner.Center.Y));
        }
        else
        {
            for (var i = 0; i <= 4; i++)
            {
                var y = inner.Top + (inner.Height * i / 4);
                context.DrawLine(GridPen, new Point(inner.Left, y), new Point(inner.Right, y));
            }
        }

        var accent = Accent ?? DefaultAccent;
        if (Curve is { Count: > 1 } curve && Colors is not { Count: > 0 })
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(ToScreen(curve[0]), false);
                for (var i = 1; i < curve.Count; i++)
                {
                    ctx.LineTo(ToScreen(curve[i]));
                }

                ctx.EndFigure(false);
            }

            context.DrawGeometry(null, new Pen(accent, 2), geometry);
        }

        if (Dots is { } dots)
        {
            for (var i = 0; i < dots.Count; i++)
            {
                // Le premier membre en blanc : on voit d'où part la vague et dans quel sens elle va.
                var point = ToScreen(dots[i]);
                context.DrawEllipse(i == 0 ? Brushes.White : accent, DotPen, point, 5, 5);
            }
        }
    }

    private void DrawBand(DrawingContext context, Rect inner, IReadOnlyList<Color> colors)
    {
        var band = new Rect(inner.X, inner.Center.Y - 12, inner.Width, 24);
        if (Stepped || colors.Count == 1)
        {
            var width = band.Width / colors.Count;
            for (var i = 0; i < colors.Count; i++)
            {
                context.FillRectangle(new SolidColorBrush(colors[i]), new Rect(band.X + (i * width), band.Y, width, band.Height));
            }

            return;
        }

        // Dégradé qui se referme sur la première couleur (le cycle recommence sans saut).
        var stops = new GradientStops();
        for (var i = 0; i < colors.Count; i++)
        {
            stops.Add(new GradientStop(colors[i], (double)i / colors.Count));
        }

        stops.Add(new GradientStop(colors[0], 1));
        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops = stops,
        };
        context.FillRectangle(brush, band);
    }
}
