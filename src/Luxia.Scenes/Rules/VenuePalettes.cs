using Luxia.Patch.Model;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Rules;

/// <summary>
/// Palettes de position par lieu (PAL-004, PAL-008, INST-054). Une valeur de palette sans lieu (<c>venueId</c> absent)
/// est celle du lieu « Générique » : c'est aussi la valeur de repli d'un lieu où la position n'a jamais été calibrée.
/// </summary>
public static class VenuePalettes
{
    /// <summary>
    /// Clé du lieu pour les valeurs de palette : <c>null</c> pour le lieu « Générique » (valeurs de repli), sinon son
    /// identifiant.
    /// </summary>
    public static Guid? Key(Venue venue)
    {
        ArgumentNullException.ThrowIfNull(venue);
        return string.Equals(venue.Name, VenueSet.DefaultVenueName, StringComparison.CurrentCultureIgnoreCase) ? null : venue.Id;
    }

    /// <summary>
    /// Valeurs d'une palette pour un lieu : celles du lieu si elles existent pour l'appareil (ou le modèle), sinon celles
    /// du lieu « Générique ». Les valeurs d'autres lieux sont ignorées.
    /// </summary>
    public static IReadOnlyList<PaletteValue> For(Palette palette, Guid? venueKey, Func<PaletteValue, bool> match)
    {
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(match);
        if (venueKey is not null)
        {
            var own = palette.Values.Where(v => v.VenueId == venueKey && match(v)).ToList();
            if (own.Count > 0)
            {
                return own;
            }
        }

        return [.. palette.Values.Where(v => v.VenueId is null && match(v))];
    }

    /// <summary>
    /// Remplace, pour les appareils capturés et le lieu donné, les valeurs d'une palette de position ; les autres
    /// appareils et les autres lieux sont gardés. Un appareil qui n'avait encore aucune valeur de repli reçoit aussi
    /// celle-ci comme valeur « Générique », pour que les autres lieux aient un repli (PAL-008).
    /// </summary>
    public static Palette Merge(Palette palette, IReadOnlyList<PaletteValue> captured, Guid? venueKey)
    {
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(captured);
        var fixtures = captured.Select(v => v.FixtureId).ToHashSet();
        var kept = palette.Values.Where(v => !(fixtures.Contains(v.FixtureId) && v.VenueId == venueKey)).ToList();
        kept.AddRange(captured.Select(v => v with { VenueId = venueKey }));
        if (venueKey is not null)
        {
            var withFallback = palette.Values.Where(v => v.VenueId is null).Select(v => v.FixtureId).ToHashSet();
            kept.AddRange(captured.Where(v => !withFallback.Contains(v.FixtureId)).Select(v => v with { VenueId = null }));
        }

        return palette with { Values = kept };
    }

    /// <summary>
    /// Copie les positions d'un lieu vers un nouveau lieu (duplication d'un lieu, INST-054) : valeurs effectives du lieu
    /// source (propres, sinon « Générique »), enregistrées pour le lieu cible.
    /// </summary>
    public static PaletteSet CopyVenue(PaletteSet palettes, Guid? sourceKey, Guid targetId)
    {
        ArgumentNullException.ThrowIfNull(palettes);
        return palettes with
        {
            Palettes = [.. palettes.Palettes.Select(p =>
            {
                if (p.Kind != PaletteKind.Position)
                {
                    return p;
                }

                var copies = p.Values
                    .Where(v => v.FixtureId is not null)
                    .Select(v => v.FixtureId)
                    .Distinct()
                    .SelectMany(fixture => For(p, sourceKey, v => v.FixtureId == fixture))
                    .Select(v => v with { VenueId = targetId })
                    .ToList();
                return copies.Count == 0 ? p : p with { Values = [.. p.Values, .. copies] };
            })],
        };
    }

    /// <summary>
    /// Appareils dont une palette de position n'a pas de valeur propre au lieu (jamais calibrés, PAL-008) : ils utilisent
    /// la valeur du lieu « Générique ». Vide pour le lieu « Générique » lui-même.
    /// </summary>
    public static IReadOnlyList<Guid> Uncalibrated(Palette palette, Guid? venueKey)
    {
        ArgumentNullException.ThrowIfNull(palette);
        if (venueKey is null || palette.Kind != PaletteKind.Position)
        {
            return [];
        }

        var own = palette.Values.Where(v => v.VenueId == venueKey).Select(v => v.FixtureId).ToHashSet();
        return [.. palette.Values
            .Where(v => v.VenueId is null && v.FixtureId is { } id && !own.Contains(id))
            .Select(v => v.FixtureId!.Value)
            .Distinct()];
    }
}
