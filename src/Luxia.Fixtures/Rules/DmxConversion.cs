using System.Globalization;
using Luxia.Core.Dmx;
using Luxia.Fixtures.Model;

namespace Luxia.Fixtures.Rules;

/// <summary>
/// Conversions entre valeurs logiques normalisées (0 à 1, GEN-020) et octets DMX (8 ou 16 bits), et affichage
/// dans l'unité la plus parlante (GEN-021). La conversion en octets n'a lieu qu'en fin de chaîne (P4, D13).
/// </summary>
public static class DmxConversion
{
    /// <summary>Valeur normalisée → octet 8 bits (arrondi au plus proche : 0,5 → 128).</summary>
    public static byte To8Bit(double normalized) => DmxValues.To8Bit(normalized);

    /// <summary>Valeur normalisée → octets grossier et fin (0,5 → 32768 = 0x80 / 0x00).</summary>
    public static (byte Coarse, byte Fine) To16Bit(double normalized) => DmxValues.To16Bit(normalized);

    /// <summary>Octet 8 bits → valeur normalisée.</summary>
    public static double From8Bit(byte value) => DmxValues.From8Bit(value);

    /// <summary>Octets grossier et fin → valeur normalisée.</summary>
    public static double From16Bit(byte coarse, byte fine) => DmxValues.From16Bit(coarse, fine);

    /// <summary>
    /// Texte d'une valeur DMX dans l'unité la plus parlante (GEN-021) : nom de plage si le canal a des plages,
    /// degrés pour Pan / Tilt si l'amplitude est connue, % pour les intensités et émetteurs, sinon 0-255.
    /// </summary>
    public static string Describe(ChannelDefinition channel, int value, PhysicalInfo? physical = null)
    {
        ArgumentNullException.ThrowIfNull(channel);
        var raw = value.ToString(CultureInfo.CurrentCulture);
        if (channel.CapabilityAt(value) is { } range && channel.Capabilities.Count > 1)
        {
            return $"{raw} – {range.Label}";
        }

        var amplitude = channel.Attribute switch
        {
            AttributeKind.Pan => physical?.PanRange,
            AttributeKind.Tilt => physical?.TiltRange,
            _ => null,
        };
        if (amplitude is { } degrees)
        {
            return string.Create(CultureInfo.CurrentCulture, $"{raw} – {Math.Round(value / 255.0 * degrees)}°");
        }

        var info = AttributeCatalog.Get(channel.Attribute);
        if (info.Family == AttributeFamily.Intensity || info.IsEmitter)
        {
            return string.Create(CultureInfo.CurrentCulture, $"{raw} – {Math.Round(value * 100 / 255.0)} %");
        }

        return channel.CapabilityAt(value) is { } only ? $"{raw} – {only.Label}" : raw;
    }
}
