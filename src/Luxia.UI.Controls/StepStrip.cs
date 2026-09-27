using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Luxia.UI.Controls;

/// <summary>
/// Bande d'étapes (doc 60 §5) : une case par étape, de largeur proportionnelle à sa durée (fondu en dégradé, puis
/// maintien plein), l'étape choisie entourée, celle qui joue marquée ▶. Clic = choisir l'étape ; flèches gauche /
/// droite = étape voisine.
/// </summary>
public sealed class StepStrip : Control
{
    /// <summary>Étapes.</summary>
    public static readonly StyledProperty<IReadOnlyList<StepStripItem>?> ItemsProperty =
        AvaloniaProperty.Register<StepStrip, IReadOnlyList<StepStripItem>?>(nameof(Items));

    /// <summary>Étape choisie.</summary>
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<StepStrip, int>(nameof(SelectedIndex));

    /// <summary>Couleur du contour de l'étape choisie (couleur du mode d'édition).</summary>
    public static readonly StyledProperty<IBrush?> AccentProperty =
        AvaloniaProperty.Register<StepStrip, IBrush?>(nameof(Accent));

    /// <summary>Largeur minimale d'une case, pour qu'une étape très courte reste cliquable.</summary>
    public const double MinCellWidth = 56;

    private const double Gap = 3;
    private static readonly IBrush CellBrush = new SolidColorBrush(Color.Parse("#21262D"));
    private static readonly IPen CellPen = new Pen(new SolidColorBrush(Color.Parse("#30363D")), 1);
    private static readonly IBrush TextBrush = new SolidColorBrush(Color.Parse("#E6EDF3"));
    private static readonly IBrush SecondaryBrush = new SolidColorBrush(Color.Parse("#8B949E"));
    private static readonly IBrush DefaultAccent = new SolidColorBrush(Color.Parse("#58A6FF"));

    static StepStrip()
    {
        AffectsRender<StepStrip>(ItemsProperty, SelectedIndexProperty, AccentProperty);
        FocusableProperty.OverrideDefaultValue<StepStrip>(true);
    }

    /// <summary>Levé quand l'utilisateur choisit une étape.</summary>
    public event EventHandler<int>? StepSelected;

    /// <inheritdoc cref="ItemsProperty"/>
    public IReadOnlyList<StepStripItem>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    /// <inheritdoc cref="SelectedIndexProperty"/>
    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <inheritdoc cref="AccentProperty"/>
    public IBrush? Accent
    {
        get => GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    /// <summary>
    /// Largeurs des cases : proportionnelles aux durées, au moins <paramref name="minWidth"/>, le tout tenant dans
    /// <paramref name="available"/> quand c'est possible (sinon chaque case a sa largeur minimale).
    /// </summary>
    public static IReadOnlyList<double> Widths(IReadOnlyList<double> durations, double available, double minWidth = MinCellWidth)
    {
        ArgumentNullException.ThrowIfNull(durations);
        var count = durations.Count;
        if (count == 0)
        {
            return [];
        }

        var usable = Math.Max(available - (Gap * (count - 1)), 0);
        if (usable <= minWidth * count)
        {
            return [.. Enumerable.Repeat(minWidth, count)];
        }

        // Les durées nulles comptent comme un minimum ; les cases trop petites sont fixées au minimum et le reste
        // est réparti entre les autres, jusqu'à ce que tout tienne.
        var weights = durations.Select(d => Math.Max(d, 0.001)).ToArray();
        var fixedCells = new bool[count];
        while (true)
        {
            var free = usable - (fixedCells.Count(f => f) * minWidth);
            var total = weights.Where((_, i) => !fixedCells[i]).Sum();
            var changed = false;
            for (var i = 0; i < count; i++)
            {
                if (!fixedCells[i] && free * weights[i] / total < minWidth)
                {
                    fixedCells[i] = true;
                    changed = true;
                }
            }

            if (!changed)
            {
                return [.. weights.Select((w, i) => fixedCells[i] ? minWidth : free * w / total)];
            }
        }
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var count = Items?.Count ?? 0;
        var width = double.IsFinite(availableSize.Width) ? availableSize.Width : Math.Max(count, 1) * (MinCellWidth + Gap);
        return new Size(Math.Max(width, Math.Max(count, 1) * (MinCellWidth + Gap)), 58);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var items = Items ?? [];
        var widths = Widths([.. items.Select(i => i.FadeSeconds + i.HoldSeconds)], Bounds.Width);
        var x = 0.0;
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var cell = new Rect(x, 0, widths[i], Bounds.Height);
            var selected = i == SelectedIndex;
            context.FillRectangle(CellBrush, cell, 5);
            context.DrawRectangle(selected ? new Pen(Accent ?? DefaultAccent, 2) : CellPen, cell.Deflate(selected ? 1 : 0.5), 5);

            var label = Text((item.IsPlaying ? "▶ " : string.Empty) + item.Label, 11, TextBrush, selected);
            using (context.PushClip(cell.Deflate(4)))
            {
                context.DrawText(label, new Point(cell.X + 6, cell.Y + 4));
            }

            // Barre : fondu en dégradé vers la couleur de l'étape, puis maintien plein.
            var color = ParseOr(item.Color, Color.Parse("#58A6FF"));
            var bar = new Rect(cell.X + 6, cell.Y + 22, Math.Max(cell.Width - 12, 1), 12);
            var total = Math.Max(item.FadeSeconds + item.HoldSeconds, 0.001);
            var fadeWidth = bar.Width * Math.Clamp(item.FadeSeconds / total, 0, 1);
            var gradient = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops = { new GradientStop(Color.FromArgb(40, color.R, color.G, color.B), 0), new GradientStop(color, 1) },
            };
            context.FillRectangle(gradient, new Rect(bar.X, bar.Y, fadeWidth, bar.Height));
            context.FillRectangle(new SolidColorBrush(color), new Rect(bar.X + fadeWidth, bar.Y, bar.Width - fadeWidth, bar.Height));

            var times = Text(string.Create(CultureInfo.CurrentCulture, $"{item.FadeSeconds:0.##} + {item.HoldSeconds:0.##} s"), 10, SecondaryBrush, false);
            using (context.PushClip(cell.Deflate(4)))
            {
                context.DrawText(times, new Point(cell.X + 6, cell.Y + 38));
            }

            x += widths[i] + Gap;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerPressed(e);
        Focus();
        var items = Items ?? [];
        var widths = Widths([.. items.Select(i => i.FadeSeconds + i.HoldSeconds)], Bounds.Width);
        var px = e.GetPosition(this).X;
        var x = 0.0;
        for (var i = 0; i < widths.Count; i++)
        {
            if (px >= x && px <= x + widths[i])
            {
                StepSelected?.Invoke(this, i);
                e.Handled = true;
                return;
            }

            x += widths[i] + Gap;
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        var count = Items?.Count ?? 0;
        var target = e.Key switch
        {
            Key.Left => SelectedIndex - 1,
            Key.Right => SelectedIndex + 1,
            _ => -1,
        };
        if (target >= 0 && target < count)
        {
            StepSelected?.Invoke(this, target);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private static FormattedText Text(string value, double size, IBrush brush, bool bold) =>
        new(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(FontFamily.Default, FontStyle.Normal, bold ? FontWeight.SemiBold : FontWeight.Normal), size, brush);

    private static Color ParseOr(string? hex, Color fallback) =>
        hex is { Length: 7 } && Color.TryParse(hex, out var color) ? color : fallback;
}
