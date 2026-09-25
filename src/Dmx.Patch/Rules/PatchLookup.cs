using Dmx.Fixtures.Model;
using Dmx.Patch.Model;

namespace Dmx.Patch.Rules;

/// <summary>Un canal résolu jusqu'à l'appareil patché qui l'occupe (CONS-007, CONS-041).</summary>
/// <param name="Fixture">Appareil patché.</param>
/// <param name="Type">Modèle (copie du projet).</param>
/// <param name="Channel">Définition du canal.</param>
/// <param name="Part">Octet grossier ou fin (canal 16 bits).</param>
public sealed record PatchChannelInfo(PatchedFixture Fixture, FixtureType Type, ChannelDefinition Channel, ChannelPart Part);

/// <summary>Résolution d'un canal DMX vers l'appareil patché qui l'occupe (doc 11 §3, CONS-007).</summary>
public static class PatchLookup
{
    /// <summary>Appareil et canal à cette adresse, si occupée (le premier appareil trouvé en cas de jumeaux).</summary>
    public static PatchChannelInfo? FindChannel(
        IReadOnlyList<PatchedFixture> fixtures, Func<PatchedFixture, FixtureType?> typeOf, int universe, int channel)
    {
        ArgumentNullException.ThrowIfNull(fixtures);
        ArgumentNullException.ThrowIfNull(typeOf);
        foreach (var fixture in fixtures)
        {
            if (fixture.Universe != universe || typeOf(fixture) is not { } type)
            {
                continue;
            }

            var mode = type.Modes.FirstOrDefault(m => m.Name == fixture.ModeName);
            if (mode is null)
            {
                continue;
            }

            var offset = channel - fixture.Address;
            if (offset < 0 || offset >= mode.Channels.Count)
            {
                continue;
            }

            var slot = mode.Channels[offset];
            if (type.Channel(slot.Channel) is { } definition)
            {
                return new PatchChannelInfo(fixture, type, definition, slot.Part);
            }
        }

        return null;
    }

    /// <summary>Plages de canaux occupées par chaque appareil d'un univers (délimitation du moniteur, CONS-043).</summary>
    public static IReadOnlyList<(int First, int Last)> FixtureRanges(
        IReadOnlyList<PatchedFixture> fixtures, Func<PatchedFixture, FixtureType?> typeOf, int universe)
    {
        ArgumentNullException.ThrowIfNull(fixtures);
        ArgumentNullException.ThrowIfNull(typeOf);
        var ranges = new List<(int, int)>();
        foreach (var fixture in fixtures.Where(f => f.Universe == universe))
        {
            var mode = typeOf(fixture)?.Modes.FirstOrDefault(m => m.Name == fixture.ModeName);
            if (mode is { ChannelCount: > 0 })
            {
                ranges.Add((fixture.Address, fixture.Address + mode.ChannelCount - 1));
            }
        }

        return ranges;
    }
}
