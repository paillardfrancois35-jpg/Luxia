using Avalonia;

namespace Luxia.UI.Controls;

/// <summary>
/// Découpage du sélecteur de couleur (ERG-004) : carré teinte × saturation à gauche, barre d'intensité à droite,
/// rangée des favoris en bas. Séparé du contrôle pour être testé sans affichage.
/// </summary>
public sealed record ColorPickerLayout
{
    /// <summary>Largeur de la barre d'intensité.</summary>
    public const double BarWidth = 24;

    /// <summary>Côté d'une pastille de favori.</summary>
    public const double SwatchSize = 22;

    /// <summary>Espace entre les zones.</summary>
    public const double Gap = 8;

    private ColorPickerLayout(Rect square, Rect bar, Rect swatchRow)
    {
        Square = square;
        Bar = bar;
        SwatchRow = swatchRow;
    }

    /// <summary>Carré teinte (horizontale) × saturation (verticale, pleine en haut).</summary>
    public Rect Square { get; }

    /// <summary>Barre d'intensité (pleine en haut).</summary>
    public Rect Bar { get; }

    /// <summary>Rangée des favoris, puis la pastille « + ».</summary>
    public Rect SwatchRow { get; }

    /// <summary>Découpe une surface donnée.</summary>
    public static ColorPickerLayout For(Size size)
    {
        var height = Math.Max(size.Height - SwatchSize - Gap, 1);
        var squareWidth = Math.Max(size.Width - BarWidth - Gap, 1);
        return new ColorPickerLayout(
            new Rect(0, 0, squareWidth, height),
            new Rect(squareWidth + Gap, 0, BarWidth, height),
            new Rect(0, height + Gap, size.Width, SwatchSize));
    }

    /// <summary>Emplacement de la pastille d'indice donné (le dernier indice utile est la pastille « + »).</summary>
    public Rect Swatch(int index) =>
        new(SwatchRow.X + (index * (SwatchSize + 4)), SwatchRow.Y, SwatchSize, SwatchSize);

    /// <summary>Nombre de pastilles qui tiennent dans la largeur.</summary>
    public int SwatchCapacity => Math.Max((int)((SwatchRow.Width + 4) / (SwatchSize + 4)), 0);

    /// <summary>Teinte et saturation du point donné du carré (bornées).</summary>
    public (double Hue, double Saturation) HueSaturationAt(Point point)
    {
        var hue = Math.Clamp((point.X - Square.X) / Square.Width, 0, 1) * 360;
        var saturation = 1 - Math.Clamp((point.Y - Square.Y) / Square.Height, 0, 1);
        return (hue, saturation);
    }

    /// <summary>Intensité du point donné de la barre (bornée).</summary>
    public double BrightnessAt(Point point) => 1 - Math.Clamp((point.Y - Bar.Y) / Bar.Height, 0, 1);

    /// <summary>Position du repère d'une couleur dans le carré.</summary>
    public Point SquarePoint(LightColor color) =>
        new(Square.X + (color.Hue / 360 * Square.Width), Square.Y + ((1 - color.Saturation) * Square.Height));

    /// <summary>Ordonnée du repère d'intensité dans la barre.</summary>
    public double BarY(double brightness) => Bar.Y + ((1 - brightness) * Bar.Height);
}
