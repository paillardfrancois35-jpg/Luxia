using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Luxia.UI.Controls;

/// <summary>
/// Barre 0-255 colorée par plage (BIB-022). Glisser une frontière entre deux plages adjacentes la déplace
/// (<see cref="BoundaryMoved"/>) ; un clic dans une plage la sélectionne (<see cref="SegmentClicked"/>) ;
/// un clic sur une frontière donne la valeur exacte de la borne (<see cref="BoundaryClicked"/>, BIB-061).
/// Un repère montre la valeur courante (test en direct, découverte).
/// </summary>
public sealed class RangeBar : Control
{
    /// <summary>Plages (remplacer la liste pour redessiner).</summary>
    public static readonly StyledProperty<IReadOnlyList<RangeSegment>> SegmentsProperty =
        AvaloniaProperty.Register<RangeBar, IReadOnlyList<RangeSegment>>(nameof(Segments), []);

    /// <summary>Plage sélectionnée (-1 = aucune).</summary>
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<RangeBar, int>(nameof(SelectedIndex), -1);

    /// <summary>Valeur courante (-1 = pas de repère).</summary>
    public static readonly StyledProperty<int> CurrentValueProperty =
        AvaloniaProperty.Register<RangeBar, int>(nameof(CurrentValue), -1);

    /// <summary>Autorise le déplacement des frontières.</summary>
    public static readonly StyledProperty<bool> IsEditableProperty =
        AvaloniaProperty.Register<RangeBar, bool>(nameof(IsEditable), true);

    private const double BoundaryTolerance = 4;
    private static readonly string[] Palette = ["#1F6FEB", "#8957E5", "#238636", "#D29922", "#DB61A2", "#3FB950", "#F85149", "#58A6FF"];
    private static readonly IPen BorderPen = new Pen(new SolidColorBrush(Color.Parse("#30363D")), 1);
    private static readonly IPen SelectedPen = new Pen(Brushes.White, 2);
    private static readonly IPen MarkerPen = new Pen(Brushes.White, 2);

    private int _dragBoundary = -1;

    static RangeBar()
    {
        AffectsRender<RangeBar>(SegmentsProperty, SelectedIndexProperty, CurrentValueProperty);
    }

    /// <summary>Plage cliquée : index et valeur sous la souris.</summary>
    public event EventHandler<(int Index, int Value)>? SegmentClicked;

    /// <summary>Frontière déplacée : index de la plage de gauche et sa nouvelle borne haute.</summary>
    public event EventHandler<(int Index, int NewMax)>? BoundaryMoved;

    /// <summary>Clic sur une frontière : valeur exacte de la borne basse de la plage de droite.</summary>
    public event EventHandler<int>? BoundaryClicked;

    /// <inheritdoc cref="SegmentsProperty"/>
    public IReadOnlyList<RangeSegment> Segments
    {
        get => GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    /// <inheritdoc cref="SelectedIndexProperty"/>
    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <inheritdoc cref="CurrentValueProperty"/>
    public int CurrentValue
    {
        get => GetValue(CurrentValueProperty);
        set => SetValue(CurrentValueProperty, value);
    }

    /// <inheritdoc cref="IsEditableProperty"/>
    public bool IsEditable
    {
        get => GetValue(IsEditableProperty);
        set => SetValue(IsEditableProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#0D1117")), bounds);

        for (var i = 0; i < Segments.Count; i++)
        {
            var s = Segments[i];
            var rect = new Rect(X(s.Min), 0, Math.Max(X(s.Max + 1) - X(s.Min), 1), bounds.Height);
            var color = Color.Parse(s.Color ?? Palette[i % Palette.Length]);
            context.FillRectangle(new SolidColorBrush(color, 0.8), rect);
            context.DrawRectangle(i == SelectedIndex ? SelectedPen : BorderPen, rect.Deflate(i == SelectedIndex ? 1 : 0));
        }

        if (CurrentValue >= 0)
        {
            var x = X(CurrentValue) + (Bounds.Width / 512);
            context.DrawLine(MarkerPen, new Point(x, 0), new Point(x, bounds.Height));
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerPressed(e);
        var x = e.GetPosition(this).X;
        var boundary = BoundaryAt(x);
        if (boundary >= 0)
        {
            BoundaryClicked?.Invoke(this, Segments[boundary + 1].Min);
            if (IsEditable)
            {
                _dragBoundary = boundary;
                e.Pointer.Capture(this);
            }

            e.Handled = true;
            return;
        }

        var value = ValueAt(x);
        var index = Segments.ToList().FindIndex(s => value >= s.Min && value <= s.Max);
        SegmentClicked?.Invoke(this, (index, value));
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerMoved(e);
        var x = e.GetPosition(this).X;
        if (_dragBoundary >= 0)
        {
            BoundaryMoved?.Invoke(this, (_dragBoundary, ValueAt(x) - 1));
            return;
        }

        Cursor = IsEditable && BoundaryAt(x) >= 0 ? new Cursor(StandardCursorType.SizeWestEast) : Cursor.Default;
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerReleased(e);
        _dragBoundary = -1;
        e.Pointer.Capture(null);
    }

    private double X(int value) => value * Bounds.Width / 256.0;

    private int ValueAt(double x) => Math.Clamp((int)(x * 256 / Math.Max(Bounds.Width, 1)), 0, 255);

    /// <summary>Frontière entre deux plages adjacentes sous la souris (index de la plage de gauche), -1 sinon.</summary>
    private int BoundaryAt(double x)
    {
        for (var i = 0; i < Segments.Count - 1; i++)
        {
            if (Segments[i + 1].Min == Segments[i].Max + 1 && Math.Abs(X(Segments[i + 1].Min) - x) <= BoundaryTolerance)
            {
                return i;
            }
        }

        return -1;
    }
}
