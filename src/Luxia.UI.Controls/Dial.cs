using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;

namespace Luxia.UI.Controls;

/// <summary>
/// Molette (doc 60 §5, composant commun) : un réglage continu (vitesse, taille, décalage…) tourné à la souris.
/// Toute la surface répond (essai P6 : seul le dessin recevait le clic) : cliquer n'importe où puis glisser vers le haut
/// augmente (toute la course en 200 pixels) ; molette de la souris ± un pas (Maj : × 10), flèches ± un pas ;
/// **double-clic ou chiffre tapé : saisie au clavier** (comme les faders) ; clic droit : saisie ou valeur par défaut.
/// La valeur est liée dans les deux sens.
/// </summary>
public sealed class Dial : Control
{
    /// <summary>Valeur.</summary>
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<Dial, double>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Minimum.</summary>
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<Dial, double>(nameof(Minimum));

    /// <summary>Maximum.</summary>
    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<Dial, double>(nameof(Maximum), 100);

    /// <summary>Pas de la molette et des flèches.</summary>
    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<Dial, double>(nameof(Step), 1);

    /// <summary>Valeur rendue par le double-clic.</summary>
    public static readonly StyledProperty<double> DefaultValueProperty =
        AvaloniaProperty.Register<Dial, double>(nameof(DefaultValue));

    /// <summary>Libellé sous la molette (« Vitesse »).</summary>
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<Dial, string?>(nameof(Label));

    /// <summary>Texte de la valeur ; vide : la valeur arrondie selon <see cref="Format"/>.</summary>
    public static readonly StyledProperty<string?> ValueTextProperty =
        AvaloniaProperty.Register<Dial, string?>(nameof(ValueText));

    /// <summary>Format numérique de la valeur (« 0 », « 0.0 »).</summary>
    public static readonly StyledProperty<string> FormatProperty =
        AvaloniaProperty.Register<Dial, string>(nameof(Format), "0");

    /// <summary>Couleur de l'arc (couleur du mode, doc 60 §4.4).</summary>
    public static readonly StyledProperty<IBrush?> AccentProperty =
        AvaloniaProperty.Register<Dial, IBrush?>(nameof(Accent));

    private const double StartAngle = 135;
    private const double Sweep = 270;
    private static readonly IPen TrackPen = new Pen(new SolidColorBrush(Color.Parse("#30363D")), 5, lineCap: PenLineCap.Round);
    private static readonly IBrush DefaultAccent = new SolidColorBrush(Color.Parse("#58A6FF"));
    private static readonly IBrush LabelBrush = new SolidColorBrush(Color.Parse("#8B949E"));
    private static readonly IPen FocusPen = new Pen(new SolidColorBrush(Color.Parse("#484F58")), 1);

    private Point? _dragStart;
    private double _dragStartValue;

    static Dial()
    {
        // IsFocused compris : sans lui, le cadre de focus restait dessiné après le passage à une autre molette (essai P6).
        AffectsRender<Dial>(ValueProperty, MinimumProperty, MaximumProperty, LabelProperty, ValueTextProperty, AccentProperty, IsFocusedProperty);
        FocusableProperty.OverrideDefaultValue<Dial>(true);
    }

    /// <inheritdoc cref="ValueProperty"/>
    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <inheritdoc cref="MinimumProperty"/>
    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <inheritdoc cref="MaximumProperty"/>
    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <inheritdoc cref="StepProperty"/>
    public double Step
    {
        get => GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    /// <inheritdoc cref="DefaultValueProperty"/>
    public double DefaultValue
    {
        get => GetValue(DefaultValueProperty);
        set => SetValue(DefaultValueProperty, value);
    }

    /// <inheritdoc cref="LabelProperty"/>
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <inheritdoc cref="ValueTextProperty"/>
    public string? ValueText
    {
        get => GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    /// <inheritdoc cref="FormatProperty"/>
    public string Format
    {
        get => GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    /// <inheritdoc cref="AccentProperty"/>
    public IBrush? Accent
    {
        get => GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    /// <summary>Position 0-1 d'une valeur dans la course (bornée).</summary>
    public static double Fraction(double value, double minimum, double maximum) =>
        maximum <= minimum ? 0 : Math.Clamp((value - minimum) / (maximum - minimum), 0, 1);

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize) => new(70, 78);

    /// <summary>
    /// Lit une valeur tapée (virgule ou point décimal, unité ou « % » tolérés) ; nulle si ce n'est pas un nombre.
    /// </summary>
    public static double? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var cleaned = new string([.. text.Trim().Replace(',', '.').TakeWhile(c => char.IsDigit(c) || c is '.' or '-' or '+')]);
        return double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Fond transparent : toute la surface reçoit le clic, pas seulement les pixels dessinés.
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));
        var size = Math.Min(Bounds.Width, Bounds.Height - 16);
        var center = new Point(Bounds.Width / 2, (size / 2) + 2);
        var radius = Math.Max(4, (size / 2) - 6);
        DrawArc(context, TrackPen, center, radius, StartAngle, Sweep);
        var fraction = Fraction(Value, Minimum, Maximum);
        if (fraction > 0.001)
        {
            DrawArc(context, new Pen(Accent ?? DefaultAccent, 5, lineCap: PenLineCap.Round), center, radius, StartAngle, Sweep * fraction);
        }

        var angle = (StartAngle + (Sweep * fraction)) * Math.PI / 180;
        context.DrawLine(new Pen(Brushes.White, 2), center, center + new Point(Math.Cos(angle) * (radius - 6), Math.Sin(angle) * (radius - 6)));

        var text = ValueText is { Length: > 0 } given ? given : Value.ToString(Format, CultureInfo.CurrentCulture);
        var value = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 11, Brushes.White);
        context.DrawText(value, new Point(center.X - (value.Width / 2), center.Y + (radius * 0.35)));
        if (Label is { Length: > 0 } label)
        {
            var caption = new FormattedText(label, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 11, LabelBrush);
            context.DrawText(caption, new Point((Bounds.Width - caption.Width) / 2, Bounds.Height - caption.Height));
        }

        if (IsFocused)
        {
            context.DrawRectangle(FocusPen, new Rect(Bounds.Size).Deflate(0.5), 6);
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        Focus();
        if (e.ClickCount == 2)
        {
            _dragStart = null;
            OpenEntry(null);
            e.Handled = true;
            return;
        }

        _dragStart = e.GetPosition(this);
        _dragStartValue = Value;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerMoved(e);
        if (_dragStart is not { } start)
        {
            return;
        }

        // Glisser relatif, vers le haut pour augmenter ; 200 pixels = toute la course.
        var position = e.GetPosition(this);
        var moved = start.Y - position.Y;
        var value = _dragStartValue + (moved / 200 * (Maximum - Minimum));
        Set(Step > 0 ? Math.Round(value / Step) * Step : value);
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerWheelChanged(e);
        var step = Step * ((e.KeyModifiers & KeyModifiers.Shift) != 0 ? 10 : 1);
        var delta = e.Delta.Y != 0 ? e.Delta.Y : e.Delta.X;
        Set(Value + (Math.Sign(delta) * step));
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnTextInput(TextInputEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnTextInput(e);

        // Un chiffre tapé sur la molette ouvre la saisie, déjà commencée.
        if (e.Text is { Length: > 0 } text && (char.IsDigit(text[0]) || text[0] is ',' or '.' or '-'))
        {
            OpenEntry(text);
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerReleased(e);
        _dragStart = null;
        e.Pointer.Capture(null);
        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            var enter = new MenuItem { Header = "Saisir une valeur…" };
            enter.Click += (_, _) => OpenEntry(null);
            var reset = new MenuItem { Header = string.Create(CultureInfo.CurrentCulture, $"Valeur par défaut ({DefaultValue.ToString(Format, CultureInfo.CurrentCulture)})") };
            reset.Click += (_, _) => Set(DefaultValue);
            new ContextMenu { Items = { enter, reset } }.Open(this);
            e.Handled = true;
        }
    }

    /// <summary>Ouvre le champ de saisie sous la molette (Entrée valide, Échap annule).</summary>
    private void OpenEntry(string? start)
    {
        var box = new TextBox
        {
            Text = start ?? Value.ToString(Format, CultureInfo.CurrentCulture),
            Width = 90,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Right,
        };
        var flyout = new Flyout { Content = box, Placement = PlacementMode.Bottom };
        box.KeyDown += (_, k) =>
        {
            if (k.Key == Key.Enter)
            {
                if (Parse(box.Text) is { } value)
                {
                    Set(value);
                }

                flyout.Hide();
                k.Handled = true;
            }
            else if (k.Key == Key.Escape)
            {
                flyout.Hide();
                k.Handled = true;
            }
        };
        flyout.Opened += (_, _) =>
        {
            box.Focus();
            if (start is null)
            {
                box.SelectAll();
            }
            else
            {
                box.CaretIndex = box.Text?.Length ?? 0;
            }
        };
        flyout.ShowAt(this);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        double? target = e.Key switch
        {
            Key.Up or Key.Right => Value + Step,
            Key.Down or Key.Left => Value - Step,
            Key.PageUp => Value + (Step * 10),
            Key.PageDown => Value - (Step * 10),
            Key.Home => Maximum,
            Key.End => Minimum,
            _ => null,
        };
        if (target is { } value)
        {
            Set(value);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void Set(double value) => Value = Math.Clamp(value, Minimum, Maximum);

    private static void DrawArc(DrawingContext context, IPen pen, Point center, double radius, double startDegrees, double sweepDegrees)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var start = startDegrees * Math.PI / 180;
            var end = (startDegrees + sweepDegrees) * Math.PI / 180;
            ctx.BeginFigure(center + new Point(Math.Cos(start) * radius, Math.Sin(start) * radius), false);
            ctx.ArcTo(
                center + new Point(Math.Cos(end) * radius, Math.Sin(end) * radius),
                new Size(radius, radius),
                0,
                sweepDegrees > 180,
                SweepDirection.Clockwise);
            ctx.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geometry);
    }
}
