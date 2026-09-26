using System.Globalization;
using System.Text.Json.Serialization;

namespace Luxia.Scenes.Model;

/// <summary>
/// Couleur « intention » (GEN-022) : rouge, vert, bleu normalisés 0-1, plus l'usage éventuel des émetteurs spéciaux.
/// Elle est convertie à la compilation selon les émetteurs de chaque appareil : RVB direct, extraction du blanc
/// en RVBW, emplacement le plus proche sur une roue (MOT-050 à MOT-053).
/// </summary>
public sealed record LogicalColor
{
    /// <summary>Rouge (0-1).</summary>
    public double R { get; init; }

    /// <summary>Vert (0-1).</summary>
    public double G { get; init; }

    /// <summary>Bleu (0-1).</summary>
    public double B { get; init; }

    /// <summary>Blanc imposé (0-1) ; <c>null</c> = déduit du mélange selon le réglage du modèle (MOT-051).</summary>
    public double? White { get; init; }

    /// <summary>Ambre (0-1) ; <c>null</c> = l'émetteur ambre n'est pas touché (MOT-053).</summary>
    public double? Amber { get; init; }

    /// <summary>UV (0-1) ; <c>null</c> = l'émetteur UV n'est pas touché (MOT-053).</summary>
    public double? Uv { get; init; }

    /// <summary>Couleur « #RRGGBB » (sans blanc, ambre ni UV).</summary>
    public static LogicalColor FromHex(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);
        var text = hex.TrimStart('#');
        if (text.Length != 6 || !int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
        {
            throw new FormatException($"Couleur invalide : « {hex} » (attendu #RRGGBB).");
        }

        return new LogicalColor
        {
            R = ((value >> 16) & 0xFF) / 255.0,
            G = ((value >> 8) & 0xFF) / 255.0,
            B = (value & 0xFF) / 255.0,
        };
    }

    /// <summary>Couleur d'affichage « #RRGGBB » (approximative pour l'UV et l'ambre), recalculée : jamais enregistrée.</summary>
    [JsonIgnore]
    public string Hex
    {
        get
        {
            var r = R + (Amber ?? 0) + ((Uv ?? 0) * 0.35) + (White ?? 0);
            var g = G + ((Amber ?? 0) * 0.65) + (White ?? 0);
            var b = B + ((Uv ?? 0) * 0.55) + (White ?? 0);
            return string.Create(CultureInfo.InvariantCulture, $"#{To255(r):X2}{To255(g):X2}{To255(b):X2}");
        }
    }

    private static int To255(double value) => (int)Math.Round(Math.Clamp(value, 0, 1) * 255);
}
