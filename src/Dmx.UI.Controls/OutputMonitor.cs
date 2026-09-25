using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Dmx.UI.Controls;

/// <summary>
/// Moniteur de sortie (CONS-040) : grille de 512 cases (32 × 16) dont la luminosité suit la valeur émise ;
/// canaux surchargés encadrés en orange, appareils délimités par un trait (CONS-043) ; case survolée encadrée
/// aussitôt (CONS-092) ; survol → <see cref="ChannelHovered"/> (CONS-041) ; clic → <see cref="ChannelClicked"/>.
/// </summary>
public sealed class OutputMonitor : Control
{
    /// <summary>Numéro de révision : l'incrémenter après avoir modifié <see cref="Values"/> redessine le moniteur.</summary>
    public static readonly StyledProperty<int> RevisionProperty =
        AvaloniaProperty.Register<OutputMonitor, int>(nameof(Revision));

    /// <summary>Afficher les valeurs numériques dans les cases.</summary>
    public static readonly StyledProperty<bool> ShowValuesProperty =
        AvaloniaProperty.Register<OutputMonitor, bool>(nameof(ShowValues));

    /// <summary>Nombre de colonnes.</summary>
    public const int Columns = 32;

    /// <summary>Nombre de lignes.</summary>
    public const int Rows = 16;

    private static readonly IPen GridPen = new Pen(new SolidColorBrush(Color.Parse("#30363D")), 1);
    private static readonly IPen OverriddenPen = new Pen(new SolidColorBrush(Color.Parse("#F0883E")), 2);
    private static readonly IPen HighlightPen = new Pen(new SolidColorBrush(Color.Parse("#58A6FF")), 2);
    private static readonly IPen FixtureBoundaryPen = new Pen(new SolidColorBrush(Color.Parse("#8B949E")), 1.5);
    private static readonly IPen HoverPen = new Pen(new SolidColorBrush(Color.Parse("#F6F8FA")), 2);

    private int _hovered;
    private int[] _owner = [];
    private IReadOnlyList<(int First, int Last)> _fixtureBoundaries = [];

    static OutputMonitor()
    {
        AffectsRender<OutputMonitor>(RevisionProperty, ShowValuesProperty);
    }

    /// <summary>Levé au survol d'un canal (0 quand la souris quitte la grille).</summary>
    public event EventHandler<int>? ChannelHovered;

    /// <summary>Levé au clic sur un canal.</summary>
    public event EventHandler<int>? ChannelClicked;

    /// <summary>Valeurs émises (indice 0 = canal 1).</summary>
    public byte[] Values { get; } = new byte[Columns * Rows];

    /// <summary>Canaux surchargés (indice 0 = canal 1).</summary>
    public bool[] Overridden { get; } = new bool[Columns * Rows];

    /// <summary>Premier et dernier canal mis en évidence (page affichée par la console), 0 si aucun.</summary>
    public (int First, int Last) Highlight { get; set; }

    /// <summary>
    /// Plages de canaux d'un même appareil patché (CONS-043) : délimitées par un trait entre appareils voisins.
    /// </summary>
    public IReadOnlyList<(int First, int Last)> FixtureBoundaries
    {
        get => _fixtureBoundaries;
        set
        {
            if (ReferenceEquals(_fixtureBoundaries, value))
            {
                return;
            }

            _fixtureBoundaries = value;
            _owner = new int[Values.Length];
            Array.Fill(_owner, -1);
            for (var r = 0; r < value.Count; r++)
            {
                for (var c = value[r].First; c <= value[r].Last && c <= Values.Length; c++)
                {
                    _owner[c - 1] = r;
                }
            }
        }
    }

    /// <inheritdoc cref="RevisionProperty"/>
    public int Revision
    {
        get => GetValue(RevisionProperty);
        set => SetValue(RevisionProperty, value);
    }

    /// <inheritdoc cref="ShowValuesProperty"/>
    public bool ShowValues
    {
        get => GetValue(ShowValuesProperty);
        set => SetValue(ShowValuesProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var cellWidth = Bounds.Width / Columns;
        var cellHeight = Bounds.Height / Rows;
        for (var i = 0; i < Values.Length; i++)
        {
            var rect = CellRect(i, cellWidth, cellHeight);
            var level = Values[i];

            // Gris très sombre à 0, bleu clair à 255 : lisible dans le noir (GEN-101).
            var color = Color.FromRgb((byte)(22 + (level * 0.55)), (byte)(27 + (level * 0.6)), (byte)(34 + (level * 0.85)));
            context.FillRectangle(new SolidColorBrush(color), rect);
            context.DrawRectangle(Overridden[i] ? OverriddenPen : GridPen, rect.Deflate(Overridden[i] ? 1 : 0));

            if (ShowValues && cellWidth >= 22)
            {
                var text = new FormattedText(
                    level.ToString(CultureInfo.CurrentCulture),
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    Math.Min(10, cellHeight * 0.55),
                    level > 150 ? Brushes.Black : Brushes.White);
                context.DrawText(text, new Point(rect.X + ((rect.Width - text.Width) / 2), rect.Y + ((rect.Height - text.Height) / 2)));
            }
        }

        if (Highlight is { First: > 0 } h)
        {
            for (var c = h.First; c <= h.Last; c++)
            {
                var rect = CellRect(c - 1, cellWidth, cellHeight);
                context.DrawLine(HighlightPen, rect.BottomLeft, rect.BottomRight);
            }
        }

        // CONS-043 : un trait entre deux appareils voisins (haut/bas/gauche/droite selon le voisin réellement différent).
        for (var i = 0; i < _owner.Length && i < Values.Length; i++)
        {
            var owner = _owner[i];
            if (owner < 0)
            {
                continue;
            }

            var rect = CellRect(i, cellWidth, cellHeight);
            var column = i % Columns;
            if (column == 0 || _owner[i - 1] != owner)
            {
                context.DrawLine(FixtureBoundaryPen, rect.TopLeft, rect.BottomLeft);
            }

            if (column == Columns - 1 || _owner[i + 1] != owner)
            {
                context.DrawLine(FixtureBoundaryPen, rect.TopRight, rect.BottomRight);
            }

            if (i < Columns || _owner[i - Columns] != owner)
            {
                context.DrawLine(FixtureBoundaryPen, rect.TopLeft, rect.TopRight);
            }

            if (i + Columns >= Values.Length || _owner[i + Columns] != owner)
            {
                context.DrawLine(FixtureBoundaryPen, rect.BottomLeft, rect.BottomRight);
            }
        }

        // CONS-092 : cadre immédiat autour de la case survolée (pas d'info-bulle standard, trop lente).
        if (_hovered is > 0 and <= Rows * Columns)
        {
            context.DrawRectangle(HoverPen, CellRect(_hovered - 1, cellWidth, cellHeight).Deflate(1));
        }
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerMoved(e);
        var channel = ChannelAt(e.GetPosition(this));
        if (channel != _hovered)
        {
            _hovered = channel;
            InvalidateVisual();
            ChannelHovered?.Invoke(this, channel);
        }
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hovered = 0;
        InvalidateVisual();
        ChannelHovered?.Invoke(this, 0);
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerPressed(e);
        var channel = ChannelAt(e.GetPosition(this));
        if (channel > 0)
        {
            ChannelClicked?.Invoke(this, channel);
        }
    }

    private static Rect CellRect(int index, double cellWidth, double cellHeight) =>
        new((index % Columns) * cellWidth, (index / Columns) * cellHeight, cellWidth, cellHeight);

    private int ChannelAt(Point point)
    {
        if (point.X < 0 || point.Y < 0 || point.X >= Bounds.Width || point.Y >= Bounds.Height)
        {
            return 0;
        }

        var column = (int)(point.X / (Bounds.Width / Columns));
        var row = (int)(point.Y / (Bounds.Height / Rows));
        return (row * Columns) + column + 1;
    }
}
