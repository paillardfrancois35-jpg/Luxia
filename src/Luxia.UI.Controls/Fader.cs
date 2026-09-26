using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Luxia.UI.Controls;

/// <summary>Demande de changement de valeur émise par un fader.</summary>
/// <param name="NewValue">Valeur demandée (0-255).</param>
/// <param name="Delta">Écart par rapport à la valeur affichée au moment de la demande (mode relatif, CONS-006).</param>
public sealed record FaderValueRequest(int NewValue, int Delta);

/// <summary>
/// Fader vertical 0-255 dessiné à la main (léger : 48 faders rafraîchis 20 fois par seconde).
/// Il n'impose pas sa valeur : il <b>demande</b> une valeur (<see cref="ValueRequested"/>), le modèle de vue envoie
/// la commande, et <see cref="Value"/> affiche ensuite la valeur réellement émise (CONS-005).
/// Saisie : glisser (relatif), molette ±1 (Maj ±10), flèches ±1, Page ±10, Début = 255, Fin = 0 (CONS-002).
/// </summary>
public sealed class Fader : Control
{
    /// <summary>Valeur affichée (0-255).</summary>
    public static readonly StyledProperty<int> ValueProperty =
        AvaloniaProperty.Register<Fader, int>(nameof(Value));

    /// <summary>Le fader est pris (surcharge active) : bordure orange (CONS-003).</summary>
    public static readonly StyledProperty<bool> IsOverriddenProperty =
        AvaloniaProperty.Register<Fader, bool>(nameof(IsOverridden));

    /// <summary>Le fader fait partie de la sélection (CONS-006).</summary>
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<Fader, bool>(nameof(IsSelected));

    /// <summary>Couleur de remplissage (par défaut bleu ; les couleurs d'émetteur peuvent la remplacer).</summary>
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<Fader, IBrush?>(nameof(Fill));

    private static readonly IBrush TrackBrush = new SolidColorBrush(Color.Parse("#0D1117"));
    private static readonly IBrush DefaultFill = new SolidColorBrush(Color.Parse("#1F6FEB"));
    private static readonly IBrush OverriddenFill = new SolidColorBrush(Color.Parse("#D29922"));
    private static readonly IPen BorderPen = new Pen(new SolidColorBrush(Color.Parse("#30363D")), 1);
    private static readonly IPen OverriddenPen = new Pen(new SolidColorBrush(Color.Parse("#F0883E")), 2);
    private static readonly IPen SelectedPen = new Pen(new SolidColorBrush(Color.Parse("#58A6FF")), 2);
    private static readonly IPen FocusPen = new Pen(new SolidColorBrush(Color.Parse("#C9D1D9")), 1, new DashStyle([2, 2], 0));

    private Point? _dragStart;
    private int _dragStartValue;

    static Fader()
    {
        AffectsRender<Fader>(ValueProperty, IsOverriddenProperty, IsSelectedProperty, FillProperty);
        FocusableProperty.OverrideDefaultValue<Fader>(true);
    }

    /// <summary>Levé quand l'utilisateur demande une nouvelle valeur.</summary>
    public event EventHandler<FaderValueRequest>? ValueRequested;

    /// <summary>Levé au clic, avec les touches de modification (sélection multiple, CONS-006).</summary>
    public event EventHandler<KeyModifiers>? Pressed;

    /// <summary>Levé au double-clic (saisie directe de la valeur).</summary>
    public event EventHandler? EditRequested;

    /// <inheritdoc cref="ValueProperty"/>
    public int Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <inheritdoc cref="IsOverriddenProperty"/>
    public bool IsOverridden
    {
        get => GetValue(IsOverriddenProperty);
        set => SetValue(IsOverriddenProperty, value);
    }

    /// <inheritdoc cref="IsSelectedProperty"/>
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <inheritdoc cref="FillProperty"/>
    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var bounds = new Rect(Bounds.Size).Deflate(1);
        context.FillRectangle(TrackBrush, bounds, 3);

        var level = Math.Clamp(Value, 0, 255) / 255.0;
        var fillHeight = bounds.Height * level;
        var fill = new Rect(bounds.X, bounds.Bottom - fillHeight, bounds.Width, fillHeight);
        context.FillRectangle(IsOverridden ? OverriddenFill : Fill ?? DefaultFill, fill, 3);

        var pen = IsSelected ? SelectedPen : IsOverridden ? OverriddenPen : BorderPen;
        context.DrawRectangle(pen, bounds, 3);
        if (IsFocused)
        {
            context.DrawRectangle(FocusPen, bounds.Deflate(3), 2);
        }

        var text = new FormattedText(
            Value.ToString(CultureInfo.CurrentCulture),
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            11,
            Brushes.White);
        context.DrawText(text, new Point(bounds.X + ((bounds.Width - text.Width) / 2), bounds.Y + 2));
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
        Pressed?.Invoke(this, e.KeyModifiers);
        if (e.ClickCount == 2)
        {
            EditRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }

        // Ctrl / Maj + clic : sélection seulement. Clic simple : on « prend » le fader à sa valeur actuelle (CONS-003).
        if ((e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Shift)) == 0)
        {
            _dragStart = e.GetPosition(this);
            _dragStartValue = Value;
            e.Pointer.Capture(this);
            Request(Value);
        }

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

        // Glisser relatif : toute la hauteur = 255 pas, pas de saut au point cliqué.
        var dy = start.Y - e.GetPosition(this).Y;
        var height = Math.Max(Bounds.Height - 2, 1);
        Request(_dragStartValue + (int)Math.Round(dy / height * 255));
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerReleased(e);
        _dragStart = null;
        e.Pointer.Capture(null);
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerWheelChanged(e);
        var step = (e.KeyModifiers & KeyModifiers.Shift) != 0 ? 10 : 1;
        var delta = e.Delta.Y != 0 ? e.Delta.Y : e.Delta.X;
        Request(Value + (Math.Sign(delta) * step));
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        int? target = e.Key switch
        {
            Key.Up => Value + 1,
            Key.Down => Value - 1,
            Key.PageUp => Value + 10,
            Key.PageDown => Value - 10,
            Key.Home => 255,
            Key.End => 0,
            _ => null,
        };

        if (target is { } value)
        {
            Request(value);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc />
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        InvalidateVisual();
    }

    private void Request(int value)
    {
        var clamped = Math.Clamp(value, 0, 255);
        var delta = clamped - Value;

        // Affichage immédiat, sans attendre le retour du moteur (au plus un tick plus tard).
        Value = clamped;
        ValueRequested?.Invoke(this, new FaderValueRequest(clamped, delta));
    }
}
