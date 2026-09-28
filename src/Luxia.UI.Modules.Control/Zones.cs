using Luxia.Patch.Model;

namespace Luxia.UI.Modules.Control;

/// <summary>Modifications des zones d'un appareil dans le lieu actif (INST-053, F7).</summary>
public static class Zones
{
    /// <summary>Zones de l'appareil dans le lieu actif.</summary>
    public static IReadOnlyList<ForbiddenZone> Of(VenueSet venues, Guid fixtureId)
    {
        ArgumentNullException.ThrowIfNull(venues);
        return [.. venues.Active.ForbiddenZones.Where(z => z.FixtureId == fixtureId)];
    }

    /// <summary>Remplace toutes les zones de l'appareil dans le lieu actif (les autres appareils ne bougent pas).</summary>
    public static VenueSet Replace(VenueSet venues, Guid fixtureId, IReadOnlyList<ForbiddenZone> zones)
    {
        ArgumentNullException.ThrowIfNull(venues);
        ArgumentNullException.ThrowIfNull(zones);
        var active = venues.Active;
        var kept = active.ForbiddenZones.Where(z => z.FixtureId != fixtureId);
        var updated = active with { ForbiddenZones = [.. kept, .. zones] };
        return venues with { Venues = [.. venues.Venues.Select(v => v.Id == active.Id ? updated : v)] };
    }
}
