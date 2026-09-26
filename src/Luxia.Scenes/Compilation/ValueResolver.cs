using Luxia.Engine.Model;
using Luxia.Fixtures.Model;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;

namespace Luxia.Scenes.Compilation;

/// <summary>
/// Résout une valeur de scène (cible × attribut → valeur, plage, couleur ou palette) en valeurs de paramètres du
/// moteur, appareil par appareil (P4 du doc 02 : on décrit des intentions, la traduction se fait ici).
/// Utilisé par la compilation et par le programmeur (surcharges d'attributs en direct).
/// </summary>
public sealed class ValueResolver
{
    private readonly PatchContext _patch;
    private readonly Dictionary<Guid, Palette> _palettes;

    /// <summary>Crée le résolveur.</summary>
    public ValueResolver(PatchContext patch, PaletteSet palettes)
    {
        ArgumentNullException.ThrowIfNull(patch);
        ArgumentNullException.ThrowIfNull(palettes);
        _patch = patch;
        _palettes = palettes.Palettes.ToDictionary(p => p.Id);
    }

    /// <summary>Palette par identifiant.</summary>
    public Palette? Palette(Guid id) => _palettes.GetValueOrDefault(id);

    /// <summary>Résout une valeur ; <paramref name="problem"/> explique une valeur sans effet possible (référence introuvable).</summary>
    public IReadOnlyList<ResolvedValue> Resolve(SceneValue value, out string? problem)
    {
        ArgumentNullException.ThrowIfNull(value);
        var members = _patch.Members(value.Target, out problem);
        var result = new List<ResolvedValue>();
        Palette? palette = null;
        if (value.PaletteId is { } paletteId && !_palettes.TryGetValue(paletteId, out palette))
        {
            problem = "palette introuvable";
            return result;
        }

        for (var index = 0; index < members.Count; index++)
        {
            var (fixture, cell) = members[index];
            foreach (var (key, level) in ResolveMember(fixture, cell, value, palette))
            {
                result.Add(new ResolvedValue(fixture.ReferenceId, key, Math.Clamp(level, 0, 1), index, members.Count));
            }
        }

        return result;
    }

    /// <summary>Valeurs (clé de canal → niveau) d'une valeur de scène pour un membre.</summary>
    public static IEnumerable<(string Key, double Level)> ResolveMember(FixtureInfo fixture, int cell, SceneValue value, Palette? palette)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(value);
        if (palette is not null)
        {
            return ResolvePalette(fixture, cell, palette);
        }

        if (value.Color is { } color)
        {
            return ResolveColor(fixture, cell, color);
        }

        var level = value.Range is { } range ? range.Dmx / 255.0 : value.Level;
        if (level is not { } l)
        {
            return [];
        }

        if (value.Channel is { } key)
        {
            var known = fixture.Channels.Any(c => c.Key == key)
                || (key == RigParameter.VirtualIntensityKey && fixture.NeedsVirtualIntensity);
            return known ? [(key, l)] : [];
        }

        return value.Attribute is { } attribute ? [.. Keys(fixture, cell, attribute).Select(k => (k, l))] : [];
    }

    /// <summary>
    /// Clés des canaux d'un appareil qui portent un attribut dans une cellule (0 = toutes les cellules).
    /// L'intensité de l'appareil entier va au gradateur maître, sinon aux gradateurs de cellule, sinon à l'intensité
    /// virtuelle (BIB-006). Pan et Tilt sont échangés pour un appareil monté sur le côté (INST-021).
    /// </summary>
    public static IEnumerable<string> Keys(FixtureInfo fixture, int cell, AttributeKind attribute)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        if (fixture.Fixture.Options.SwapPanTilt)
        {
            attribute = attribute switch
            {
                AttributeKind.Pan => AttributeKind.Tilt,
                AttributeKind.Tilt => AttributeKind.Pan,
                _ => attribute,
            };
        }

        if (attribute is AttributeKind.Intensity or AttributeKind.CellIntensity)
        {
            return IntensityKeys(fixture, cell);
        }

        return fixture.Channels.Where(c => c.Attribute == attribute && (cell == 0 || c.Cell == cell)).Select(c => c.Key);
    }

    private static IEnumerable<string> IntensityKeys(FixtureInfo fixture, int cell)
    {
        if (cell > 0)
        {
            var own = fixture.Channels.Where(c => c.Attribute == AttributeKind.CellIntensity && c.Cell == cell).Select(c => c.Key).ToList();
            return own.Count > 0 ? own : IntensityKeys(fixture, 0);
        }

        var master = fixture.Channels.Where(c => c.Attribute == AttributeKind.Intensity).Select(c => c.Key).ToList();
        if (master.Count > 0)
        {
            return master;
        }

        var cells = fixture.Channels.Where(c => c.Attribute == AttributeKind.CellIntensity).Select(c => c.Key).ToList();
        if (cells.Count > 0)
        {
            return cells;
        }

        return fixture.NeedsVirtualIntensity ? [RigParameter.VirtualIntensityKey] : [];
    }

    private static IEnumerable<(string, double)> ResolveColor(FixtureInfo fixture, int cell, LogicalColor color)
    {
        var cells = cell == 0 ? fixture.Cells : [cell];
        foreach (var c in cells)
        {
            foreach (var (channel, level) in ColorConversion.Translate(fixture.Type, fixture.Mode, c, color))
            {
                yield return (channel.Key, level);
            }
        }
    }

    private static IEnumerable<(string, double)> ResolvePalette(FixtureInfo fixture, int cell, Palette palette)
    {
        // Valeurs propres à cet appareil, sinon à son modèle : elles priment sur toute traduction automatique (PAL-002).
        var specific = palette.Values.Where(v => v.FixtureId == fixture.Fixture.Id || v.FixtureId == fixture.ReferenceId).ToList();
        if (specific.Count == 0)
        {
            specific = [.. palette.Values.Where(v => v.FixtureId is null && v.FixtureTypeId == fixture.Type.Id)];
        }

        if (specific.Count > 0)
        {
            foreach (var value in specific)
            {
                var keys = value.Channel is { } key
                    ? fixture.Channels.Where(c => c.Key == key).Select(c => c.Key)
                    : value.Attribute is { } attribute ? Keys(fixture, cell, attribute) : [];
                foreach (var k in keys)
                {
                    yield return (k, value.Level);
                }
            }

            yield break;
        }

        switch (palette.Kind)
        {
            case PaletteKind.Color when palette.Light is { } light:
                foreach (var item in ResolveColor(fixture, cell, light))
                {
                    yield return item;
                }

                break;

            case PaletteKind.Intensity when palette.Level is { } level:
                foreach (var key in IntensityKeys(fixture, cell))
                {
                    yield return (key, level);
                }

                break;
        }
    }
}
