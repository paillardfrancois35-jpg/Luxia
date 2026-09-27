using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Luxia.UI.Controls;

/// <summary>
/// Sélecteur de couleur (ERG-004, doc 60 §5) : carré teinte × saturation, barre d'intensité, favoris.
/// Comme le <see cref="Fader"/>, il n'impose pas sa couleur : il la <b>demande</b> (<see cref="ColorRequested"/>) et
/// affiche ensuite <see cref="SelectedColor"/>, la couleur réellement retenue par le modèle de vue.
/// Saisie : cliquer / glisser dans le carré (teinte, saturation) ou la barre (intensité) ; molette = teinte ±1°
/// sur le carré, intensité ±1 % sur la barre (Maj : ×10) ; clic sur un favori = le reprendre, clic droit = le retirer,
/// « + » = ajouter la couleur courante.
/// </summary>
public sealed class ColorPicker : Control
{
    /// <summary>Couleur affichée.</summary>
    public static readonly StyledProperty<LightColor> SelectedColorProperty =
        AvaloniaProperty.Register<ColorPicker, LightColor>(nameof(SelectedColor), LightColor.White);

    /// <summary>Couleurs favorites (dans l'ordre d'affichage).</summary>
    public static readonly StyledProperty<IReadOnlyList<LightColor>?> FavoritesProperty =
        AvaloniaProperty.Register<ColorPicker, IReadOnlyList<LightColor>?>(nameof(Favorites));

    private static readonly IBrush TrackBrush = new SolidColorBrush(Color.Parse("#0D1117"));
    private static readonly IPen BorderPen = new Pen(new SolidColorBrush(Color.Parse("#30363D")), 1);
    private static readonly IPen MarkerOuterPen = new Pen(Brushes.Black, 3);
    private static readonly IPen MarkerInnerPen = new Pen(Brushes.White, 1.5);
    private static readonly IPen SelectedSwatchPen = new Pen(new SolidColorBrush(Color.Parse("#58A6FF")), 2);
    private static readonly IBrush SecondaryText = new SolidColorBrush(Color.Parse("#8B949E"));

    private enum DragTarget
    {
        None,
        Square,
        Bar,
    }

    private WriteableBitmap? _squareBitmap;
    private DragTarget _drag;

    static ColorPicker()
    {
        AffectsRender<ColorPicker>(SelectedColorProperty, FavoritesProperty);
        FocusableProperty.OverrideDefaultValue<ColorPicker>(true);
    }

    /// <summary>Levé quand l'utilisateur demande une couleur.</summary>
    public event EventHandler<LightColor>? ColorRequested;

    /// <summary>Levé quand l'utilisateur demande d'ajouter la couleur courante aux favoris.</summary>
    public event EventHandler? FavoriteAddRequested;

    /// <summary>Levé quand l'utilisateur demande de retirer le favori d'indice donné.</summary>
    public event EventHandler<int>? FavoriteRemoveRequested;

    /// <inheritdoc cref="SelectedColorProperty"/>
    public LightColor SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    /// <inheritdoc cref="FavoritesProperty"/>
    public IReadOnlyList<LightColor>? Favorites
    {
        get => GetValue(FavoritesProperty);
        set => SetValue(FavoritesProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var layout = ColorPickerLayout.For(Bounds.Size);
        var color = SelectedColor.Normalized();

        context.DrawImage(SquareBitmap(layout.Square.Size), layout.Square);
        context.DrawRectangle(BorderPen, layout.Square);
        var marker = layout.SquarePoint(color);
        context.DrawEllipse(null, MarkerOuterPen, marker, 6, 6);
        context.DrawEllipse(null, MarkerInnerPen, marker, 6, 6);

        var full = new LightColor(color.Hue, color.Saturation, 1).ToColor();
        var gradient = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops = { new GradientStop(full, 0), new GradientStop(Colors.Black, 1) },
        };
        context.FillRectangle(gradient, layout.Bar);
        context.DrawRectangle(BorderPen, layout.Bar);
        var barY = layout.BarY(color.Brightness);
        context.DrawRectangle(MarkerOuterPen, new Rect(layout.Bar.X - 2, barY - 3, layout.Bar.Width + 4, 6));
        context.DrawRectangle(MarkerInnerPen, new Rect(layout.Bar.X - 2, barY - 3, layout.Bar.Width + 4, 6));

        RenderSwatches(context, layout, color);
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerPressed(e);
        Focus();
        var layout = ColorPickerLayout.For(Bounds.Size);
        var point = e.GetPosition(this);
        var properties = e.GetCurrentPoint(this).Properties;

        if (layout.SwatchRow.Contains(point))
        {
            OnSwatchPressed(layout, point, properties.IsRightButtonPressed);
            e.Handled = true;
            return;
        }

        if (!properties.IsLeftButtonPressed)
        {
            return;
        }

        _drag = layout.Bar.Inflate(4).Contains(point) ? DragTarget.Bar
            : layout.Square.Contains(point) ? DragTarget.Square
            : DragTarget.None;
        if (_drag != DragTarget.None)
        {
            e.Pointer.Capture(this);
            RequestAt(layout, point);
        }

        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerMoved(e);
        if (_drag != DragTarget.None)
        {
            RequestAt(ColorPickerLayout.For(Bounds.Size), e.GetPosition(this));
        }
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerReleased(e);
        _drag = DragTarget.None;
        e.Pointer.Capture(null);
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerWheelChanged(e);
        var layout = ColorPickerLayout.For(Bounds.Size);
        var point = e.GetPosition(this);
        var sign = Math.Sign(e.Delta.Y != 0 ? e.Delta.Y : e.Delta.X);
        var factor = (e.KeyModifiers & KeyModifiers.Shift) != 0 ? 10 : 1;
        var color = SelectedColor;
        if (layout.Bar.Inflate(4).Contains(point))
        {
            Request(color with { Brightness = color.Brightness + (sign * 0.01 * factor) });
        }
        else if (layout.Square.Contains(point))
        {
            Request(color with { Hue = color.Hue + (sign * factor) });
        }

        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        _squareBitmap?.Dispose();
        _squareBitmap = null;
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _squareBitmap?.Dispose();
        _squareBitmap = null;
    }

    private void RequestAt(ColorPickerLayout layout, Point point)
    {
        var color = SelectedColor;
        if (_drag == DragTarget.Square)
        {
            var (hue, saturation) = layout.HueSaturationAt(point);
            Request(color with { Hue = hue, Saturation = saturation });
        }
        else if (_drag == DragTarget.Bar)
        {
            Request(color with { Brightness = layout.BrightnessAt(point) });
        }
    }

    private void Request(LightColor color) => ColorRequested?.Invoke(this, color.Normalized());

    private void OnSwatchPressed(ColorPickerLayout layout, Point point, bool rightButton)
    {
        var favorites = Favorites ?? [];
        var count = Math.Min(favorites.Count, Math.Max(layout.SwatchCapacity - 1, 0));
        for (var i = 0; i < count; i++)
        {
            if (layout.Swatch(i).Contains(point))
            {
                if (rightButton)
                {
                    FavoriteRemoveRequested?.Invoke(this, i);
                }
                else
                {
                    Request(favorites[i]);
                }

                return;
            }
        }

        if (!rightButton && layout.Swatch(count).Contains(point))
        {
            FavoriteAddRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void RenderSwatches(DrawingContext context, ColorPickerLayout layout, LightColor color)
    {
        var favorites = Favorites ?? [];
        var count = Math.Min(favorites.Count, Math.Max(layout.SwatchCapacity - 1, 0));
        var current = color.ToRgb();
        for (var i = 0; i < count; i++)
        {
            var rect = layout.Swatch(i);
            context.FillRectangle(new SolidColorBrush(favorites[i].ToColor()), rect, 3);
            context.DrawRectangle(favorites[i].ToRgb() == current ? SelectedSwatchPen : BorderPen, rect, 3);
        }

        if (layout.SwatchCapacity > 0)
        {
            var plus = layout.Swatch(count);
            context.FillRectangle(TrackBrush, plus, 3);
            context.DrawRectangle(BorderPen, plus, 3);
            DrawCentered(context, "+", plus, 14, Brushes.White);
        }

        // Valeurs lisibles à droite des favoris : en % (F4), la teinte en degrés.
        var readout = string.Create(
            CultureInfo.CurrentCulture,
            $"{color.Hue:0}° · {color.Saturation * 100:0} % · {color.Brightness * 100:0} %");
        var text = new FormattedText(readout, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 11, SecondaryText);
        var x = layout.SwatchRow.Right - text.Width;
        if (x > layout.Swatch(count).Right + 8)
        {
            context.DrawText(text, new Point(x, layout.SwatchRow.Y + ((layout.SwatchRow.Height - text.Height) / 2)));
        }
    }

    private static void DrawCentered(DrawingContext context, string value, Rect rect, double size, IBrush brush)
    {
        var text = new FormattedText(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, size, brush);
        context.DrawText(text, new Point(rect.X + ((rect.Width - text.Width) / 2), rect.Y + ((rect.Height - text.Height) / 2)));
    }

    /// <summary>Image du carré, à intensité pleine, recalculée seulement quand la taille change.</summary>
    private WriteableBitmap SquareBitmap(Size size)
    {
        var width = Math.Max((int)size.Width, 1);
        var height = Math.Max((int)size.Height, 1);
        if (_squareBitmap is { } cached && cached.PixelSize.Width == width && cached.PixelSize.Height == height)
        {
            return cached;
        }

        _squareBitmap?.Dispose();
        var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        var row = new int[width];
        using (var buffer = bitmap.Lock())
        {
            for (var y = 0; y < height; y++)
            {
                var saturation = 1 - ((double)y / Math.Max(height - 1, 1));
                for (var x = 0; x < width; x++)
                {
                    var (r, g, b) = new LightColor((double)x / width * 360, saturation, 1).ToRgb();
                    row[x] = unchecked((int)(0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b));
                }

                Marshal.Copy(row, 0, buffer.Address + (y * buffer.RowBytes), width);
            }
        }

        _squareBitmap = bitmap;
        return bitmap;
    }
}
