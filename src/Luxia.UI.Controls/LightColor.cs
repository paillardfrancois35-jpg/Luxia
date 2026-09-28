using Avalonia.Media;

namespace Luxia.UI.Controls;

/// <summary>
/// Couleur en teinte, saturation et intensité (ERG-004) : le modèle du sélecteur de couleur, plus proche de la façon
/// dont on choisit une couleur de lumière (« un bleu un peu pâle, à moitié ») que le rouge / vert / bleu.
/// </summary>
/// <param name="Hue">Teinte en degrés (0-360, 0 = rouge, 120 = vert, 240 = bleu).</param>
/// <param name="Saturation">Saturation (0 = blanc, 1 = couleur pure).</param>
/// <param name="Brightness">Intensité (0 = noir, 1 = pleine).</param>
public readonly record struct LightColor(double Hue, double Saturation, double Brightness)
{
    /// <summary>Blanc plein.</summary>
    public static LightColor White => new(0, 0, 1);

    /// <summary>Copie ramenée dans les bornes (teinte modulo 360, le reste entre 0 et 1).</summary>
    public LightColor Normalized()
    {
        var hue = Hue % 360;
        if (hue < 0)
        {
            hue += 360;
        }

        return new LightColor(hue, Math.Clamp(Saturation, 0, 1), Math.Clamp(Brightness, 0, 1));
    }

    /// <summary>Composantes rouge, vert, bleu (0-255).</summary>
    public (byte R, byte G, byte B) ToRgb()
    {
        var c = Normalized();
        var chroma = c.Brightness * c.Saturation;
        var sector = c.Hue / 60;
        var x = chroma * (1 - Math.Abs((sector % 2) - 1));
        var (r, g, b) = (int)sector switch
        {
            0 => (chroma, x, 0.0),
            1 => (x, chroma, 0.0),
            2 => (0.0, chroma, x),
            3 => (0.0, x, chroma),
            4 => (x, 0.0, chroma),
            _ => (chroma, 0.0, x),
        };
        var m = c.Brightness - chroma;
        return (ToByte(r + m), ToByte(g + m), ToByte(b + m));
    }

    /// <summary>Couleur Avalonia équivalente (opaque).</summary>
    public Color ToColor()
    {
        var (r, g, b) = ToRgb();
        return Color.FromRgb(r, g, b);
    }

    /// <summary>Couleur à partir du rouge, vert, bleu (0-255). Un gris garde une teinte de 0.</summary>
    public static LightColor FromRgb(byte r, byte g, byte b)
    {
        double rf = r / 255.0, gf = g / 255.0, bf = b / 255.0;
        var max = Math.Max(rf, Math.Max(gf, bf));
        var min = Math.Min(rf, Math.Min(gf, bf));
        var delta = max - min;
        double hue;
        if (delta == 0)
        {
            hue = 0;
        }
        else if (max == rf)
        {
            hue = 60 * (((gf - bf) / delta) % 6);
        }
        else if (max == gf)
        {
            hue = 60 * (((bf - rf) / delta) + 2);
        }
        else
        {
            hue = 60 * (((rf - gf) / delta) + 4);
        }

        return new LightColor(hue, max == 0 ? 0 : delta / max, max).Normalized();
    }

    private static byte ToByte(double value) => (byte)Math.Clamp((int)Math.Round(value * 255), 0, 255);
}
