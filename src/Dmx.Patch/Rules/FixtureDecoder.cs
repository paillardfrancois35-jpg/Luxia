using Dmx.Fixtures.Model;
using Dmx.Fixtures.Rules;
using Dmx.Patch.Model;

namespace Dmx.Patch.Rules;

/// <summary>Couleur logique 0-1 (avant tout mélange d'écran).</summary>
/// <param name="R">Rouge.</param>
/// <param name="G">Vert.</param>
/// <param name="B">Bleu.</param>
public readonly record struct DisplayColor(double R, double G, double B)
{
    /// <summary>Noir.</summary>
    public static readonly DisplayColor Black = new(0, 0, 0);

    /// <summary>Mélange additif, chaque composante bornée à 1.</summary>
    public static DisplayColor Add(DisplayColor a, DisplayColor b) =>
        new(Math.Min(1, a.R + b.R), Math.Min(1, a.G + b.G), Math.Min(1, a.B + b.B));

    /// <summary>Multiplie chaque composante par un facteur (intensité).</summary>
    public DisplayColor Scale(double factor) => new(R * factor, G * factor, B * factor);

    /// <summary>Couleur au format « #RRGGBB ».</summary>
    public override string ToString() =>
        $"#{To255(R):X2}{To255(G):X2}{To255(B):X2}";

    private static int To255(double v) => (int)Math.Round(Math.Clamp(v, 0, 1) * 255);

    /// <summary>Lit une couleur « #RRGGBB ».</summary>
    public static DisplayColor Parse(string hex)
    {
        return new DisplayColor(
            Convert.ToInt32(hex[1..3], 16) / 255.0,
            Convert.ToInt32(hex[3..5], 16) / 255.0,
            Convert.ToInt32(hex[5..7], 16) / 255.0);
    }
}

/// <summary>État décodé d'une cellule d'appareil (0 = appareil entier) pour le simulateur (SIM-003).</summary>
/// <param name="Cell">Numéro de cellule (doc 12 §2.6).</param>
/// <param name="Color">Couleur résultante.</param>
/// <param name="Intensity">Intensité 0-1 (après intensité virtuelle, BIB-006).</param>
/// <param name="Strobing">Un canal de cette cellule est en strobe / pulsation / aléatoire (SIM-012).</param>
public sealed record DecodedCell(int Cell, DisplayColor Color, double Intensity, bool Strobing);

/// <summary>État décodé d'un appareil patché, prêt pour l'affichage (doc 14 §3, SIM-003, SIM-004).</summary>
/// <param name="PanDegrees">Angle Pan, si l'appareil en a un et que l'amplitude est connue (SIM-004).</param>
/// <param name="TiltDegrees">Angle Tilt, si l'appareil en a un et que l'amplitude est connue (SIM-004).</param>
/// <param name="Cells">Cellules (une seule, numéro 0, pour un appareil sans subdivision).</param>
public sealed record DecodedFixture(double? PanDegrees, double? TiltDegrees, IReadOnlyList<DecodedCell> Cells)
{
    /// <summary>Couleur et intensité moyennes toutes cellules confondues (affichage simple, symbole de l'appareil).</summary>
    public (DisplayColor Color, double Intensity) Overall()
    {
        if (Cells.Count == 0)
        {
            return (DisplayColor.Black, 0);
        }

        var color = Cells.Aggregate(DisplayColor.Black, (acc, c) => DisplayColor.Add(acc, c.Color.Scale(c.Intensity)));
        var intensity = Cells.Max(c => c.Intensity);
        return (color, intensity);
    }
}

/// <summary>
/// Décode la trame DMX d'un appareil patché en couleurs, intensités et angles logiques (doc 14 §3, SIM-003).
/// Ne connaît que le modèle et les options de montage (INST-021) ; la position dans le lieu est du ressort de l'appelant.
/// </summary>
public static class FixtureDecoder
{
    // Couleurs approximatives des émetteurs non-RVB, pour un rendu plausible à l'écran (pas une colorimétrie exacte).
    private static readonly DisplayColor White = new(1, 1, 1);
    private static readonly DisplayColor WarmWhite = new(1, 0.85, 0.6);
    private static readonly DisplayColor Amber = new(1, 0.65, 0);
    private static readonly DisplayColor Uv = new(0.35, 0, 0.55);
    private static readonly DisplayColor Cyan = new(0, 1, 1);
    private static readonly DisplayColor Magenta = new(1, 0, 1);
    private static readonly DisplayColor Yellow = new(1, 1, 0);
    private static readonly DisplayColor Lime = new(0.6, 1, 0.2);

    /// <summary>Décode un appareil à partir de la trame de son univers.</summary>
    public static DecodedFixture Decode(FixtureType type, FixtureMode mode, PatchedFixture fixture, ReadOnlySpan<byte> frame)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(mode);
        ArgumentNullException.ThrowIfNull(fixture);
        var raw = ResolveRawValues(mode, fixture.Address, frame);
        var cells = new List<DecodedCell>();
        foreach (var cellNumber in raw.Keys.Select(k => type.Channel(k)?.Cell ?? 0).Distinct().Order())
        {
            cells.Add(DecodeCell(type, cellNumber, raw));
        }

        double? pan = null;
        double? tilt = null;
        foreach (var (key, value) in raw)
        {
            var definition = type.Channel(key);
            if (definition is null)
            {
                continue;
            }

            if (definition.Attribute == AttributeKind.Pan && type.Physical.PanRange is { } panRange)
            {
                pan = Angle(value, panRange, fixture.Options.InvertPan, fixture.Options.PanOffsetDegrees);
            }
            else if (definition.Attribute == AttributeKind.Tilt && type.Physical.TiltRange is { } tiltRange)
            {
                tilt = Angle(value, tiltRange, fixture.Options.InvertTilt, 0);
            }
        }

        if (fixture.Options.SwapPanTilt)
        {
            (pan, tilt) = (tilt, pan);
        }

        return new DecodedFixture(pan, tilt, cells);
    }

    private static double Angle(double normalized, double range, bool inverted, double offsetDegrees) =>
        ((inverted ? 1 - normalized : normalized) * range) + offsetDegrees;

    private static DecodedCell DecodeCell(FixtureType type, int cell, IReadOnlyDictionary<string, double> raw)
    {
        var color = DisplayColor.Black;
        double? dimmer = null;
        var strobing = false;
        var hasColorSignal = false;

        foreach (var (key, value) in raw)
        {
            var definition = type.Channel(key);
            if (definition is null || definition.Cell != cell)
            {
                continue;
            }

            var rawByte = (int)Math.Round(value * 255);
            if (definition.CapabilityAt(rawByte)?.Strobe is StrobeEffect.Strobe or StrobeEffect.Pulse or StrobeEffect.Random)
            {
                strobing = true;
            }

            switch (definition.Attribute)
            {
                case AttributeKind.Intensity or AttributeKind.CellIntensity:
                    dimmer = value;
                    break;
                case AttributeKind.Red:
                    color = DisplayColor.Add(color, new DisplayColor(value, 0, 0));
                    hasColorSignal |= value > 0;
                    break;
                case AttributeKind.Green:
                    color = DisplayColor.Add(color, new DisplayColor(0, value, 0));
                    hasColorSignal |= value > 0;
                    break;
                case AttributeKind.Blue:
                    color = DisplayColor.Add(color, new DisplayColor(0, 0, value));
                    hasColorSignal |= value > 0;
                    break;
                case AttributeKind.White:
                    color = DisplayColor.Add(color, White.Scale(value));
                    hasColorSignal |= value > 0;
                    break;
                case AttributeKind.WarmWhite:
                    color = DisplayColor.Add(color, WarmWhite.Scale(value));
                    hasColorSignal |= value > 0;
                    break;
                case AttributeKind.Amber:
                    color = DisplayColor.Add(color, Amber.Scale(value));
                    hasColorSignal |= value > 0;
                    break;
                case AttributeKind.Uv:
                    color = DisplayColor.Add(color, Uv.Scale(value));
                    hasColorSignal |= value > 0;
                    break;
                case AttributeKind.Cyan:
                    color = DisplayColor.Add(color, Cyan.Scale(value));
                    hasColorSignal |= value > 0;
                    break;
                case AttributeKind.Magenta:
                    color = DisplayColor.Add(color, Magenta.Scale(value));
                    hasColorSignal |= value > 0;
                    break;
                case AttributeKind.Yellow:
                    color = DisplayColor.Add(color, Yellow.Scale(value));
                    hasColorSignal |= value > 0;
                    break;
                case AttributeKind.Lime:
                    color = DisplayColor.Add(color, Lime.Scale(value));
                    hasColorSignal |= value > 0;
                    break;
                case AttributeKind.ColorWheel or AttributeKind.ColorMacro:
                    if (definition.CapabilityAt(rawByte)?.Colors is { Count: > 0 } colors)
                    {
                        color = DisplayColor.Add(color, DisplayColor.Parse(colors[0]));
                        hasColorSignal = true;
                    }

                    break;
            }
        }

        // Intensité virtuelle (BIB-006) : sans canal dimmer, un appareil qui n'émet qu'une couleur est considéré allumé.
        var intensity = dimmer ?? (hasColorSignal ? 1.0 : 0.0);
        return new DecodedCell(cell, color, intensity, strobing);
    }

    private static Dictionary<string, double> ResolveRawValues(FixtureMode mode, int address, ReadOnlySpan<byte> frame)
    {
        var coarse = new Dictionary<string, byte>();
        var fine = new Dictionary<string, byte>();
        for (var i = 0; i < mode.Channels.Count; i++)
        {
            var slot = mode.Channels[i];
            var channel = address + i;
            var value = channel >= 1 && channel <= frame.Length ? frame[channel - 1] : (byte)0;
            (slot.Part == ChannelPart.Fine ? fine : coarse)[slot.Channel] = value;
        }

        var result = new Dictionary<string, double>();
        foreach (var (key, c) in coarse)
        {
            result[key] = fine.TryGetValue(key, out var f) ? DmxConversion.From16Bit(c, f) : DmxConversion.From8Bit(c);
        }

        return result;
    }
}
