using Dmx.Core.Dmx;
using Dmx.Patch.Model;

namespace Dmx.Patch.Rules;

/// <summary>Un conflit d'adresse entre deux appareils patchés (INST-013).</summary>
/// <param name="First">Premier appareil.</param>
/// <param name="Second">Second appareil.</param>
public sealed record PatchOverlap(PatchedFixture First, PatchedFixture Second);

/// <summary>
/// Règles de patch (doc 13 §2-3) : adresse libre (INST-012), chevauchements (INST-013), jumeaux (INST-014),
/// ajout multiple (INST-011). Ne connaît que les adresses et tailles ; la résolution du mode (nombre de canaux)
/// est fournie par l'appelant (qui a accès à la bibliothèque du projet).
/// </summary>
public static class PatchRules
{
    /// <summary>Plage de canaux occupée par un appareil, dans son univers.</summary>
    public static ChannelRange RangeOf(PatchedFixture fixture, int channelCount)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        return new ChannelRange(fixture.Address, fixture.Address + channelCount - 1);
    }

    /// <summary>
    /// Premier appareil déjà présent à la même adresse (même univers), utilisable pour vérifier des jumeaux
    /// (INST-014) avant d'ajouter un nouvel appareil.
    /// </summary>
    public static PatchedFixture? FixtureAt(IEnumerable<PatchedFixture> fixtures, int universe, int address) =>
        fixtures.FirstOrDefault(f => f.Universe == universe && f.Address == address);

    /// <summary>
    /// Détecte les chevauchements d'adresses dans un univers (INST-013). Deux appareils du même
    /// <see cref="PatchedFixture.TwinGroupId"/> (non nul), même modèle et même mode, ne sont jamais en conflit
    /// (INST-014 : jumeaux, reçoivent les mêmes valeurs).
    /// </summary>
    /// <param name="fixtures">Appareils à vérifier (un seul univers).</param>
    /// <param name="channelCountOf">Nombre de canaux occupés par un appareil (dépend de son mode).</param>
    public static IReadOnlyList<PatchOverlap> DetectOverlaps(
        IReadOnlyList<PatchedFixture> fixtures, Func<PatchedFixture, int> channelCountOf)
    {
        ArgumentNullException.ThrowIfNull(fixtures);
        ArgumentNullException.ThrowIfNull(channelCountOf);
        var overlaps = new List<PatchOverlap>();
        for (var i = 0; i < fixtures.Count; i++)
        {
            for (var j = i + 1; j < fixtures.Count; j++)
            {
                var a = fixtures[i];
                var b = fixtures[j];
                if (a.Universe != b.Universe || AreTwins(a, b))
                {
                    continue;
                }

                var rangeA = RangeOf(a, channelCountOf(a));
                var rangeB = RangeOf(b, channelCountOf(b));
                if (rangeA.First <= rangeB.Last && rangeB.First <= rangeA.Last)
                {
                    overlaps.Add(new PatchOverlap(a, b));
                }
            }
        }

        return overlaps;
    }

    /// <summary>Deux appareils jumeaux (INST-014) : même groupe non nul, même modèle, même mode.</summary>
    public static bool AreTwins(PatchedFixture a, PatchedFixture b) =>
        a.TwinGroupId is { } group && group == b.TwinGroupId
        && a.FixtureTypeId == b.FixtureTypeId
        && a.ModeName == b.ModeName;

    /// <summary>
    /// Première adresse libre (INST-012) capable d'accueillir <paramref name="channelCount"/> canaux sans
    /// chevaucher un appareil existant de l'univers, à partir de <paramref name="startFrom"/>.
    /// </summary>
    public static int? FindFreeAddress(
        IReadOnlyList<PatchedFixture> fixtures, int universe, int channelCount, Func<PatchedFixture, int> channelCountOf, int startFrom = 1)
    {
        ArgumentNullException.ThrowIfNull(fixtures);
        ArgumentNullException.ThrowIfNull(channelCountOf);
        ArgumentOutOfRangeException.ThrowIfLessThan(channelCount, 1);
        var occupied = fixtures.Where(f => f.Universe == universe)
            .Select(f => RangeOf(f, channelCountOf(f)))
            .OrderBy(r => r.First)
            .ToList();

        for (var candidate = Math.Max(startFrom, 1); candidate + channelCount - 1 <= DmxConstants.ChannelCount; candidate++)
        {
            var candidateRange = new ChannelRange(candidate, candidate + channelCount - 1);
            if (occupied.All(r => r.First > candidateRange.Last || r.Last < candidateRange.First))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Prépare l'ajout de <paramref name="count"/> appareils identiques (INST-011) : adresses consécutives
    /// (espacées de <paramref name="channelCount"/> + <paramref name="gap"/> canaux libres), noms numérotés
    /// automatiquement à partir de <paramref name="startNumber"/> (« PAR 1 », « PAR 2 »…).
    /// </summary>
    public static IReadOnlyList<(int Address, string Name, int Number)> PlanMultiple(
        int firstAddress, int count, int channelCount, string baseName, int gap = 0, int startNumber = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(channelCount, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);
        var step = channelCount + Math.Max(gap, 0);
        var plan = new List<(int, string, int)>();
        for (var i = 0; i < count; i++)
        {
            var number = startNumber + i;
            plan.Add((firstAddress + (i * step), $"{baseName} {number}", number));
        }

        return plan;
    }
}
