using Luxia.Fixtures.Model;
using Luxia.Fixtures.Rules;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Rules;

/// <summary>
/// Traduction d'une couleur logique selon les émetteurs d'un appareil (GEN-022, doc 17 §2.2) :
/// RVB direct (MOT-050), extraction du blanc en RVBW (MOT-051), emplacement le plus proche d'une roue ou d'une
/// macro (MOT-052), UV et ambre en émetteurs indépendants (MOT-053).
/// </summary>
public static class ColorConversion
{
    /// <summary>En dessous de ce niveau, une couleur est considérée noire : aucune roue ne la rend.</summary>
    private const double BlackThreshold = 0.02;

    /// <summary>
    /// Valeurs des canaux de couleur d'une cellule (0 = cellule de l'appareil entier) pour rendre <paramref name="color"/>.
    /// Les canaux absents du résultat ne sont pas touchés par la couleur (ils gardent leur valeur sous-jacente).
    /// </summary>
    public static IReadOnlyList<(ChannelDefinition Channel, double Level)> Translate(
        FixtureType type, FixtureMode mode, int cell, LogicalColor color)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(mode);
        ArgumentNullException.ThrowIfNull(color);
        var channels = FixtureRules.ChannelsOf(type, mode).Where(c => c.Cell == cell).ToList();
        var result = new List<(ChannelDefinition, double)>();

        var red = channels.Where(c => c.Attribute == AttributeKind.Red).ToList();
        var green = channels.Where(c => c.Attribute == AttributeKind.Green).ToList();
        var blue = channels.Where(c => c.Attribute == AttributeKind.Blue).ToList();
        var white = channels.Where(c => c.Attribute == AttributeKind.White).ToList();
        if (white.Count == 0)
        {
            white = [.. channels.Where(c => c.Attribute == AttributeKind.WarmWhite)];
        }

        double r = Clamp(color.R), g = Clamp(color.G), b = Clamp(color.B);
        var hasRgb = red.Count + green.Count + blue.Count > 0;
        double? w = color.White is { } imposed ? Clamp(imposed) : null;
        if (hasRgb && white.Count > 0 && w is null)
        {
            // MOT-051 : extraction de la composante blanche selon le réglage du modèle.
            var common = Math.Min(r, Math.Min(g, b));
            switch (type.WhiteMode ?? WhiteMode.Extract)
            {
                case WhiteMode.Off:
                    w = 0;
                    break;
                case WhiteMode.Boost:
                    w = common;
                    break;
                default:
                    w = common;
                    r -= common;
                    g -= common;
                    b -= common;
                    break;
            }
        }
        else if (!hasRgb && white.Count > 0 && w is null)
        {
            // Appareil blanc seul : il rend la part blanche de la couleur.
            w = Math.Min(r, Math.Min(g, b));
        }

        Add(result, red, r);
        Add(result, green, g);
        Add(result, blue, b);
        if (w is { } whiteLevel)
        {
            Add(result, white, whiteLevel);
        }

        if (!hasRgb)
        {
            // Synthèse soustractive (CMY) : seulement pour un appareil sans émetteurs RVB.
            Add(result, channels.Where(c => c.Attribute == AttributeKind.Cyan), 1 - Clamp(color.R));
            Add(result, channels.Where(c => c.Attribute == AttributeKind.Magenta), 1 - Clamp(color.G));
            Add(result, channels.Where(c => c.Attribute == AttributeKind.Yellow), 1 - Clamp(color.B));
        }

        // MOT-053 : ambre et UV ne sont pilotés que si la couleur logique en précise l'usage.
        if (color.Amber is { } amber)
        {
            Add(result, channels.Where(c => c.Attribute == AttributeKind.Amber), Clamp(amber));
        }

        if (color.Uv is { } uv)
        {
            Add(result, channels.Where(c => c.Attribute == AttributeKind.Uv), Clamp(uv));
        }

        foreach (var wheel in channels.Where(c => c.Attribute is AttributeKind.ColorWheel or AttributeKind.ColorMacro))
        {
            if (NearestSlot(type, wheel, color) is { } slot)
            {
                result.Add((wheel, slot.Median / 255.0));
            }
        }

        return result;
    }

    /// <summary>
    /// Plage de roue (ou de macro) dont la couleur est la plus proche de <paramref name="color"/>, en distance
    /// perceptuelle (CIE Lab, ΔE 1976) ; demi-couleurs exclues ; une position « ouverte » sans couleur compte pour du
    /// blanc. <c>null</c> pour une couleur noire ou une roue sans couleur connue.
    /// </summary>
    public static Capability? NearestSlot(FixtureType type, ChannelDefinition channel, LogicalColor color)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(color);
        if (Math.Max(color.R, Math.Max(color.G, color.B)) < BlackThreshold && (color.White ?? 0) < BlackThreshold)
        {
            return null;
        }

        var target = ToLab(Clamp(color.R + (color.White ?? 0)), Clamp(color.G + (color.White ?? 0)), Clamp(color.B + (color.White ?? 0)));
        Capability? best = null;
        var bestDistance = double.MaxValue;
        foreach (var capability in channel.Capabilities)
        {
            if (SlotColor(type, channel, capability) is not { } hex)
            {
                continue;
            }

            var slot = LogicalColor.FromHex(hex);
            var lab = ToLab(slot.R, slot.G, slot.B);
            var distance = Math.Sqrt(Square(lab.L - target.L) + Square(lab.A - target.A) + Square(lab.B - target.B));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = capability;
            }
        }

        return best;
    }

    /// <summary>Couleur d'une plage de roue : une seule couleur définie ; « ouvert » (aucune couleur) = blanc ; demi-couleur exclue.</summary>
    private static string? SlotColor(FixtureType type, ChannelDefinition channel, Capability capability)
    {
        var colors = capability.Colors;
        if (colors.Count == 0 && capability.WheelSlot is { } index && channel.Wheel is { } wheelKey
            && type.Wheels.FirstOrDefault(w => w.Key == wheelKey) is { } wheel && index >= 1 && index <= wheel.Slots.Count)
        {
            colors = wheel.Slots[index - 1].Colors;
        }

        if (colors.Count == 1)
        {
            return colors[0];
        }

        var isSlot = capability.Kind is CapabilityKind.WheelSlot or CapabilityKind.Open;
        return colors.Count == 0 && isSlot && channel.Attribute == AttributeKind.ColorWheel ? "#FFFFFF" : null;
    }

    private static void Add(List<(ChannelDefinition, double)> result, IEnumerable<ChannelDefinition> channels, double level)
    {
        foreach (var channel in channels)
        {
            result.Add((channel, Clamp(level)));
        }
    }

    private static double Clamp(double value) => Math.Clamp(value, 0, 1);

    private static double Square(double value) => value * value;

    /// <summary>sRVB (0-1) → CIE Lab (illuminant D65).</summary>
    private static (double L, double A, double B) ToLab(double r, double g, double b)
    {
        static double Linear(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        static double F(double t) => t > 0.008856 ? Math.Cbrt(t) : (7.787 * t) + (16.0 / 116);

        var lr = Linear(r);
        var lg = Linear(g);
        var lb = Linear(b);
        var x = ((0.4124 * lr) + (0.3576 * lg) + (0.1805 * lb)) / 0.95047;
        var y = (0.2126 * lr) + (0.7152 * lg) + (0.0722 * lb);
        var z = ((0.0193 * lr) + (0.1192 * lg) + (0.9505 * lb)) / 1.08883;
        var fx = F(x);
        var fy = F(y);
        var fz = F(z);
        return ((116 * fy) - 16, 500 * (fx - fy), 200 * (fy - fz));
    }
}
