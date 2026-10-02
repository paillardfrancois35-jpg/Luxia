using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Luxia.UI.Modules.Control.Sequencing;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>
/// Frise d'une séquence (SHOW-002, maquette 11) : règle des mesures et des temps, une ligne par piste, des blocs en couleur. Un clic
/// choisit un bloc ; glisser son corps le déplace dans sa piste, glisser son bord droit change sa durée ; la position est aimantée par
/// <see cref="Snap"/>. Le dépôt d'un élément de la bibliothèque est piloté par la fenêtre (<see cref="ShowGhost"/>, <see cref="HitTest"/>).
/// </summary>
public sealed class SequenceTimeline : Avalonia.Controls.Control
{
    /// <summary>Largeur des en-têtes de piste.</summary>
    public const double HeaderWidth = 150;

    private const double RulerHeight = 28;
    private const double TrackHeight = 54;
    private const double EdgeGrip = 8;

    /// <summary>Pistes.</summary>
    public static readonly StyledProperty<IReadOnlyList<TimelineTrack>?> TracksProperty = AvaloniaProperty.Register<SequenceTimeline, IReadOnlyList<TimelineTrack>?>(nameof(Tracks));

    /// <summary>Blocs.</summary>
    public static readonly StyledProperty<IReadOnlyList<TimelineBlock>?> BlocksProperty = AvaloniaProperty.Register<SequenceTimeline, IReadOnlyList<TimelineBlock>?>(nameof(Blocks));

    /// <summary>Longueur en mesures.</summary>
    public static readonly StyledProperty<double> BarsProperty = AvaloniaProperty.Register<SequenceTimeline, double>(nameof(Bars), 8);

    /// <summary>Largeur d'une mesure, en pixels (zoom).</summary>
    public static readonly StyledProperty<double> PixelsPerBarProperty = AvaloniaProperty.Register<SequenceTimeline, double>(nameof(PixelsPerBar), 44);

    /// <summary>Tête de lecture (en mesures) pendant l'essai, ou nulle.</summary>
    public static readonly StyledProperty<double?> PlayheadProperty = AvaloniaProperty.Register<SequenceTimeline, double?>(nameof(Playhead));

    private (TimelineBlock Block, bool Resize, double OffsetBars)? _drag;
    private double _dragValue;
    private (int Row, double Start, double Length, string Label)? _ghost;

    static SequenceTimeline()
    {
        AffectsRender<SequenceTimeline>(TracksProperty, BlocksProperty, BarsProperty, PixelsPerBarProperty, PlayheadProperty);
        AffectsMeasure<SequenceTimeline>(TracksProperty, BarsProperty, PixelsPerBarProperty);
    }

    /// <summary>Crée la frise.</summary>
    public SequenceTimeline()
    {
        Focusable = true;
        ClipToBounds = true;
    }

    /// <summary>Pistes.</summary>
    public IReadOnlyList<TimelineTrack>? Tracks
    {
        get => GetValue(TracksProperty);
        set => SetValue(TracksProperty, value);
    }

    /// <summary>Blocs.</summary>
    public IReadOnlyList<TimelineBlock>? Blocks
    {
        get => GetValue(BlocksProperty);
        set => SetValue(BlocksProperty, value);
    }

    /// <summary>Longueur en mesures.</summary>
    public double Bars
    {
        get => GetValue(BarsProperty);
        set => SetValue(BarsProperty, value);
    }

    /// <summary>Largeur d'une mesure en pixels.</summary>
    public double PixelsPerBar
    {
        get => GetValue(PixelsPerBarProperty);
        set => SetValue(PixelsPerBarProperty, value);
    }

    /// <summary>Tête de lecture en mesures, ou nulle.</summary>
    public double? Playhead
    {
        get => GetValue(PlayheadProperty);
        set => SetValue(PlayheadProperty, value);
    }

    /// <summary>Aimantation d'une position en mesures (fournie par la fenêtre).</summary>
    public Func<double, double> Snap { get; set; } = x => x;

    /// <summary>Un bloc est choisi (ou aucun : clic dans le vide).</summary>
    public event EventHandler<BlockRef?>? BlockChosen;

    /// <summary>Un bloc a été déplacé : nouveau début en mesures.</summary>
    public event EventHandler<(BlockRef Block, double Start)>? BlockMoved;

    /// <summary>Un bloc a été allongé ou raccourci : nouvelle durée en mesures.</summary>
    public event EventHandler<(BlockRef Block, double Length)>? BlockResized;

    /// <summary>Suppr : retirer le bloc choisi.</summary>
    public event EventHandler? DeleteRequested;

    /// <summary>Piste et début (aimanté) sous un point, ou nul hors des pistes.</summary>
    public (int Row, double Start)? HitTest(Point point)
    {
        var tracks = Tracks ?? [];
        var row = (int)Math.Floor((point.Y - RulerHeight) / TrackHeight);
        if (point.X < HeaderWidth - 10 || row < 0 || row >= tracks.Count || point.X > Bounds.Width || point.Y > Bounds.Height)
        {
            return null;
        }

        return (row, Math.Clamp(Snap(Math.Max(0, (point.X - HeaderWidth) / PixelsPerBar)), 0, Math.Max(0, Bars - 0.0001)));
    }

    /// <summary>Montre (ou efface, point nul) l'ombre de l'élément qu'on dépose.</summary>
    public void ShowGhost(Point? point, string label)
    {
        _ghost = point is { } p && HitTest(p) is { } hit ? (hit.Row, hit.Start, 1, label) : null;
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize) =>
        new(HeaderWidth + (Math.Max(Bars, 1) * PixelsPerBar) + 24, RulerHeight + ((Tracks?.Count ?? 0) * TrackHeight) + 4);

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var tracks = Tracks ?? [];
        var width = HeaderWidth + (Bars * PixelsPerBar);
        var height = RulerHeight + (tracks.Count * TrackHeight);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#161B22")), new Rect(Bounds.Size));

        // Règle : numéro de chaque mesure (en gras toutes les 4), traits des mesures et des temps.
        var thick = new Pen(new SolidColorBrush(Color.Parse("#30363D")), 2);
        var thin = new Pen(new SolidColorBrush(Color.Parse("#30363D")), 1);
        var beat = new Pen(new SolidColorBrush(Color.FromArgb(70, 48, 54, 61)), 1);
        for (var bar = 0; bar <= Math.Ceiling(Bars); bar++)
        {
            var x = HeaderWidth + (bar * PixelsPerBar);
            context.DrawLine(bar % 4 == 0 ? thick : thin, new Point(x, RulerHeight - 6), new Point(x, height));
            if (bar < Bars)
            {
                context.DrawText(Text((bar + 1).ToString(CultureInfo.InvariantCulture), 12, bar % 4 == 0 ? "#E6EDF3" : "#8B949E", bar % 4 == 0), new Point(x + 4, 6));
                for (var b = 1; b < 4 && PixelsPerBar >= 32; b++)
                {
                    var bx = x + (b * PixelsPerBar / 4);
                    if (bx < width)
                    {
                        context.DrawLine(beat, new Point(bx, RulerHeight), new Point(bx, height));
                    }
                }
            }
        }

        context.DrawText(Text("Mesure", 12, "#8B949E"), new Point(8, 6));
        for (var t = 0; t < tracks.Count; t++)
        {
            var y = RulerHeight + (t * TrackHeight);
            var head = new Rect(0, y + 4, HeaderWidth - 8, TrackHeight - 8);
            context.FillRectangle(new SolidColorBrush(Color.Parse("#21262D")), head, 4);
            context.FillRectangle(new SolidColorBrush(Parse(tracks[t].Color)), new Rect(head.X, head.Y, 3, head.Height));
            context.DrawText(Text(tracks[t].Title, 13, "#E6EDF3", true), new Point(10, y + 10));
            context.DrawText(Text(tracks[t].Subtitle, 11, "#8B949E"), new Point(10, y + 29));
            context.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(90, 48, 54, 61)), 1), new Point(0, y + TrackHeight), new Point(width, y + TrackHeight));
        }

        foreach (var block in Blocks ?? [])
        {
            var (start, length) = Geometry(block);
            DrawBlock(context, block.Row, start, length, block.Label, block.Color, block.IsAction, block.IsSelected, block.Ramp, block.Keeps);
        }

        if (_ghost is { } ghost)
        {
            DrawBlock(context, ghost.Row, ghost.Start, ghost.Length, ghost.Label, "#58A6FF", false, true, null, false, 0.45);
        }

        if (Playhead is { } playhead && playhead >= 0)
        {
            var px = HeaderWidth + (playhead * PixelsPerBar);
            context.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#D29922")), 2), new Point(px, RulerHeight - 8), new Point(px, height));
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerPressed(e);
        Focus();
        var point = e.GetPosition(this);
        var hit = (Blocks ?? []).LastOrDefault(b => Rect(b).Contains(point));
        if (hit is null)
        {
            BlockChosen?.Invoke(this, null);
            return;
        }

        var rect = Rect(hit);
        var resize = point.X >= rect.Right - EdgeGrip;
        _drag = (hit, resize, ((point.X - HeaderWidth) / PixelsPerBar) - hit.Start);
        _dragValue = resize ? hit.Length : hit.Start;
        e.Pointer.Capture(this);
        BlockChosen?.Invoke(this, hit.Ref);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerMoved(e);
        var point = e.GetPosition(this);
        if (_drag is not { } drag)
        {
            // Curseur : flèche double sur le bord droit d'un bloc (durée), main sur son corps (déplacer).
            var over = (Blocks ?? []).LastOrDefault(b => Rect(b).Contains(point));
            Cursor = over is null ? Cursor.Default : point.X >= Rect(over).Right - EdgeGrip ? new Cursor(StandardCursorType.SizeWestEast) : new Cursor(StandardCursorType.Hand);
            return;
        }

        var bars = (point.X - HeaderWidth) / PixelsPerBar;
        _dragValue = drag.Resize
            ? Math.Max(Snap(bars - drag.Block.Start), 0.125)
            : Math.Clamp(Snap(bars - drag.OffsetBars), 0, Math.Max(0, Bars - 0.125));
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerReleased(e);
        if (_drag is not { } drag)
        {
            return;
        }

        _drag = null;
        e.Pointer.Capture(null);
        if (drag.Resize && Math.Abs(_dragValue - drag.Block.Length) > 1e-9)
        {
            BlockResized?.Invoke(this, (drag.Block.Ref, _dragValue));
        }
        else if (!drag.Resize && Math.Abs(_dragValue - drag.Block.Start) > 1e-9)
        {
            BlockMoved?.Invoke(this, (drag.Block.Ref, _dragValue));
        }

        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (e.Key is Key.Delete or Key.Back)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private (double Start, double Length) Geometry(TimelineBlock block)
    {
        if (_drag is { } drag && drag.Block.Ref == block.Ref)
        {
            return drag.Resize ? (block.Start, _dragValue) : (_dragValue, block.Length);
        }

        return (block.Start, block.Length);
    }

    private Rect Rect(TimelineBlock block)
    {
        var (start, length) = Geometry(block);
        return new Rect(HeaderWidth + (start * PixelsPerBar) + 1, RulerHeight + (block.Row * TrackHeight) + 7, Math.Max((length * PixelsPerBar) - 3, 6), TrackHeight - 14);
    }

    private void DrawBlock(DrawingContext context, int row, double start, double length, string label, string colorText, bool action, bool selected, (double? From, double To)? ramp, bool keeps, double opacity = 1)
    {
        var rect = new Rect(HeaderWidth + (start * PixelsPerBar) + 1, RulerHeight + (row * TrackHeight) + 7, Math.Max((length * PixelsPerBar) - 3, 6), TrackHeight - 14);
        var color = Parse(colorText);
        var fill = Color.FromArgb((byte)((action ? 110 : 215) * opacity), color.R, color.G, color.B);
        context.FillRectangle(new SolidColorBrush(fill), rect, 5);
        context.DrawRectangle(new Pen(new SolidColorBrush(selected ? Colors.White : color), selected ? 2 : 1), rect, 5);
        var light = ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) / 255 > 0.6 && !action;
        using (context.PushClip(rect.Deflate(4)))
        {
            context.DrawText(Text(label, 12, light ? "#0D1117" : "#E6EDF3", true), new Point(rect.X + 7, rect.Y + 3));
            var sub = string.Create(CultureInfo.CurrentCulture, $"{length:0.##} mesure{(length > 1 ? "s" : string.Empty)}{(keeps ? " · continue après" : string.Empty)}");
            context.DrawText(Text(sub, 11, light ? "#0D1117" : "#C9D1D9"), new Point(rect.X + 7, rect.Y + 20));
            if (ramp is { } r)
            {
                var from = r.From ?? 1;
                context.DrawLine(new Pen(Brushes.White, 2), new Point(rect.X + 2, rect.Bottom - 3 - (from * (rect.Height - 8))), new Point(rect.Right - 2, rect.Bottom - 3 - (r.To * (rect.Height - 8))));
            }
        }

        // Poignée de durée sur le bord droit.
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), new Rect(rect.Right - 4, rect.Y + 8, 2, rect.Height - 16));
    }

    private static Color Parse(string hex) => Color.TryParse(hex, out var color) ? color : Color.Parse("#8B949E");

    private static FormattedText Text(string text, double size, string color, bool bold = false) =>
        new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(FontFamily.Default, FontStyle.Normal, bold ? FontWeight.SemiBold : FontWeight.Normal), size, new SolidColorBrush(Color.Parse(color)));
}
