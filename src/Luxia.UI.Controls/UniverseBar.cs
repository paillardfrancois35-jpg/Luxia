using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Luxia.UI.Controls;

/// <summary>Un appareil affiché par <see cref="UniverseBar"/> (INST-003).</summary>
/// <param name="First">Premier canal (1-512).</param>
/// <param name="Last">Dernier canal (inclus).</param>
/// <param name="Color">Couleur d'affichage (« #RRGGBB »).</param>
/// <param name="Name">Nom affiché au survol.</param>
public sealed record UniverseBarSegment(int First, int Last, string Color, string Name);

/// <summary>
/// Barre d'univers (INST-003) : 512 cases colorées par appareil patché, nom au survol, canaux libres visibles.
/// </summary>
public sealed class UniverseBar : Control
{
    /// <summary>Nombre de canaux d'un univers DMX.</summary>
    public const int ChannelCount = 512;

    /// <summary>Appareils patchés de l'univers affiché.</summary>
    public static readonly StyledProperty<IReadOnlyList<UniverseBarSegment>> SegmentsProperty =
        AvaloniaProperty.Register<UniverseBar, IReadOnlyList<UniverseBarSegment>>(nameof(Segments), []);

    private static readonly IPen BorderPen = new Pen(new SolidColorBrush(Color.Parse("#30363D")), 1);
    private static readonly IPen OverlapPen = new Pen(new SolidColorBrush(Color.Parse("#F85149")), 2);
    private static readonly SolidColorBrush FreeBrush = new(Color.Parse("#161B22"));

    private int _hovered;

    static UniverseBar()
    {
        AffectsRender<UniverseBar>(SegmentsProperty);
    }

    /// <summary>Levé au survol d'un canal (0 quand la souris quitte la barre).</summary>
    public event EventHandler<int>? ChannelHovered;

    /// <inheritdoc cref="SegmentsProperty"/>
    public IReadOnlyList<UniverseBarSegment> Segments
    {
        get => GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var width = Bounds.Width;
        var height = Bounds.Height;
        context.FillRectangle(FreeBrush, new Rect(0, 0, width, height));

        var seen = new bool[ChannelCount + 1];
        foreach (var segment in Segments)
        {
            var rect = new Rect(X(segment.First - 1, width), 0, X(segment.Last, width) - X(segment.First - 1, width), height);
            var overlaps = false;
            for (var c = segment.First; c <= segment.Last; c++)
            {
                if (c <= seen.Length - 1 && seen[c])
                {
                    overlaps = true;
                }

                if (c <= seen.Length - 1)
                {
                    seen[c] = true;
                }
            }

            context.FillRectangle(new SolidColorBrush(Color.Parse(segment.Color), 0.85), rect);
            context.DrawRectangle(overlaps ? OverlapPen : BorderPen, rect.Deflate(overlaps ? 1 : 0.5));
        }
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerMoved(e);
        var channel = ChannelAt(e.GetPosition(this).X);
        if (channel != _hovered)
        {
            _hovered = channel;
            ChannelHovered?.Invoke(this, channel);
        }
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hovered = 0;
        ChannelHovered?.Invoke(this, 0);
    }

    private static double X(int channel, double width) => channel * width / ChannelCount;

    private int ChannelAt(double x)
    {
        if (x < 0 || x >= Bounds.Width)
        {
            return 0;
        }

        return Math.Clamp((int)(x * ChannelCount / Math.Max(Bounds.Width, 1)) + 1, 1, ChannelCount);
    }
}
