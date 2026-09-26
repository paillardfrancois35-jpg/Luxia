using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Luxia.UI.Controls;

/// <summary>Une cellule d'appareil à dessiner (segment de barre, tête d'un effet…).</summary>
/// <param name="OffsetM">Décalage le long de l'appareil, en mètres, depuis son centre.</param>
/// <param name="Color">Couleur (« #RRGGBB »).</param>
/// <param name="Intensity">Intensité 0-1.</param>
public sealed record SimulatorCellVisual(double OffsetM, string Color, double Intensity);

/// <summary>Un appareil à dessiner dans le simulateur (doc 14 §3).</summary>
/// <param name="Id">Identifiant de l'appareil patché.</param>
/// <param name="Name">Nom affiché.</param>
/// <param name="XM">Position X, en mètres.</param>
/// <param name="YM">Position Y, en mètres.</param>
/// <param name="OrientationDeg">Orientation au sol, en degrés.</param>
/// <param name="IsMovingHead">Dessine un faisceau orientable (lyre, effet).</param>
/// <param name="PanDeg">Angle Pan, si connu.</param>
/// <param name="Cells">Cellules de l'appareil (au moins une).</param>
/// <param name="HasError">Modèle introuvable dans la copie du projet (SIM-009).</param>
/// <param name="Strobing">Au moins un canal en strobe (SIM-012 : jamais animé, juste signalé).</param>
public sealed record SimulatorFixtureVisual(
    Guid Id,
    string Name,
    double XM,
    double YM,
    double OrientationDeg,
    bool IsMovingHead,
    double? PanDeg,
    IReadOnlyList<SimulatorCellVisual> Cells,
    bool HasError,
    bool Strobing);

/// <summary>
/// Simulateur 2D (doc 14, vue de dessus) : plan du lieu actif, appareils à leur position, faisceau des lyres,
/// segments des barres, strobe signalé par une icône plutôt qu'animé (SIM-012 : protection photosensible).
/// </summary>
public sealed class SimulatorCanvas : Control
{
    /// <summary>Largeur de la salle, en mètres.</summary>
    public static readonly StyledProperty<double> RoomWidthMProperty =
        AvaloniaProperty.Register<SimulatorCanvas, double>(nameof(RoomWidthM), 12);

    /// <summary>Profondeur de la salle, en mètres.</summary>
    public static readonly StyledProperty<double> RoomDepthMProperty =
        AvaloniaProperty.Register<SimulatorCanvas, double>(nameof(RoomDepthM), 8);

    /// <summary>Appareils à dessiner.</summary>
    public static readonly StyledProperty<IReadOnlyList<SimulatorFixtureVisual>> FixturesProperty =
        AvaloniaProperty.Register<SimulatorCanvas, IReadOnlyList<SimulatorFixtureVisual>>(nameof(Fixtures), []);

    private const double BeamLengthM = 1.6;
    private const double HitRadiusPx = 16;
    private static readonly IPen RoomPen = new Pen(new SolidColorBrush(Color.Parse("#30363D")), 1);
    private static readonly IPen ErrorPen = new Pen(new SolidColorBrush(Color.Parse("#F85149")), 2, dashStyle: DashStyle.Dash);
    private static readonly IPen BeamPen = new Pen(new SolidColorBrush(Color.Parse("#F6F8FA"), 0.5), 3);
    private static readonly SolidColorBrush RoomBrush = new(Color.Parse("#0D1117"));
    private static readonly SolidColorBrush BodyBrush = new(Color.Parse("#30363D"));

    private Guid _hovered;

    static SimulatorCanvas()
    {
        AffectsRender<SimulatorCanvas>(RoomWidthMProperty, RoomDepthMProperty, FixturesProperty);
    }

    /// <summary>Levé au survol d'un appareil (<see cref="Guid.Empty"/> quand la souris n'en survole aucun).</summary>
    public event EventHandler<Guid>? FixtureHovered;

    /// <summary>Levé au clic sur un appareil.</summary>
    public event EventHandler<Guid>? FixtureClicked;

    /// <inheritdoc cref="RoomWidthMProperty"/>
    public double RoomWidthM
    {
        get => GetValue(RoomWidthMProperty);
        set => SetValue(RoomWidthMProperty, value);
    }

    /// <inheritdoc cref="RoomDepthMProperty"/>
    public double RoomDepthM
    {
        get => GetValue(RoomDepthMProperty);
        set => SetValue(RoomDepthMProperty, value);
    }

    /// <inheritdoc cref="FixturesProperty"/>
    public IReadOnlyList<SimulatorFixtureVisual> Fixtures
    {
        get => GetValue(FixturesProperty);
        set => SetValue(FixturesProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var (scale, offsetX, offsetY) = Layout();
        context.FillRectangle(RoomBrush, new Rect(Bounds.Size));
        context.DrawRectangle(RoomPen, new Rect(offsetX, offsetY, RoomWidthM * scale, RoomDepthM * scale));

        foreach (var fixture in Fixtures)
        {
            var center = new Point(offsetX + (fixture.XM * scale), offsetY + (fixture.YM * scale));
            if (fixture.IsMovingHead)
            {
                var angle = (fixture.PanDeg ?? fixture.OrientationDeg) * Math.PI / 180;
                var tip = center + new Point(Math.Sin(angle) * BeamLengthM * scale, -Math.Cos(angle) * BeamLengthM * scale);
                var color = fixture.Cells.Count > 0 ? fixture.Cells[0] : new SimulatorCellVisual(0, "#58A6FF", 0);
                context.DrawLine(new Pen(new SolidColorBrush(ParseOrDefault(color.Color), Math.Max(0.15, color.Intensity)), 4), center, tip);
                context.DrawEllipse(BodyBrush, RoomPen, center, 6, 6);
            }
            else if (fixture.Cells.Count > 1)
            {
                var angle = fixture.OrientationDeg * Math.PI / 180;
                var direction = new Point(Math.Sin(angle), -Math.Cos(angle));
                foreach (var cell in fixture.Cells)
                {
                    var p = center + new Point(direction.X * cell.OffsetM * scale, direction.Y * cell.OffsetM * scale);

                    // Corps toujours visible (SIM-001 : l'appareil se voit au plan même éteint), halo de couleur par-dessus si allumé.
                    context.DrawEllipse(BodyBrush, RoomPen, p, 5, 5);
                    if (cell.Intensity > 0)
                    {
                        context.DrawEllipse(new SolidColorBrush(ParseOrDefault(cell.Color), Math.Max(0.4, cell.Intensity)), null, p, 5, 5);
                    }
                }
            }
            else
            {
                var cell = fixture.Cells.Count > 0 ? fixture.Cells[0] : new SimulatorCellVisual(0, "#58A6FF", 0);

                // Corps toujours visible (SIM-001 : l'appareil se voit au plan même éteint), halo de couleur par-dessus si allumé.
                context.DrawEllipse(BodyBrush, RoomPen, center, 8, 8);
                if (cell.Intensity > 0)
                {
                    var radius = 8 + (cell.Intensity * 10);
                    context.DrawEllipse(new SolidColorBrush(ParseOrDefault(cell.Color), Math.Max(0.4, cell.Intensity)), null, center, radius, radius);
                }
            }

            if (fixture.Strobing)
            {
                var text = new FormattedText("⚡", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.White);
                context.DrawText(text, center + new Point(8, -18));
            }

            if (fixture.HasError)
            {
                context.DrawEllipse(null, ErrorPen, center, 12, 12);
            }

            if (fixture.Id == _hovered)
            {
                context.DrawEllipse(null, new Pen(Brushes.White, 1.5), center, 14, 14);
            }
        }
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerMoved(e);
        var id = FixtureAt(e.GetPosition(this));
        if (id != _hovered)
        {
            _hovered = id;
            InvalidateVisual();
            FixtureHovered?.Invoke(this, id);
        }
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hovered = Guid.Empty;
        InvalidateVisual();
        FixtureHovered?.Invoke(this, Guid.Empty);
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerPressed(e);
        var id = FixtureAt(e.GetPosition(this));
        if (id != Guid.Empty)
        {
            FixtureClicked?.Invoke(this, id);
        }
    }

    private Guid FixtureAt(Point point)
    {
        var (scale, offsetX, offsetY) = Layout();
        foreach (var fixture in Fixtures)
        {
            var center = new Point(offsetX + (fixture.XM * scale), offsetY + (fixture.YM * scale));
            if (Distance(point, center) <= HitRadiusPx)
            {
                return fixture.Id;
            }
        }

        return Guid.Empty;
    }

    private static double Distance(Point a, Point b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    private (double Scale, double OffsetX, double OffsetY) Layout()
    {
        var width = Math.Max(RoomWidthM, 0.1);
        var depth = Math.Max(RoomDepthM, 0.1);
        var scale = Math.Min((Bounds.Width - 20) / width, (Bounds.Height - 20) / depth);
        scale = double.IsFinite(scale) && scale > 0 ? scale : 1;
        var offsetX = (Bounds.Width - (width * scale)) / 2;
        var offsetY = (Bounds.Height - (depth * scale)) / 2;
        return (scale, offsetX, offsetY);
    }

    private static Color ParseOrDefault(string hex)
    {
        try
        {
            return Color.Parse(hex);
        }
        catch (FormatException)
        {
            return Color.Parse("#58A6FF");
        }
    }
}
