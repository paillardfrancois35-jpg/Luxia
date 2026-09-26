using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Rules;

/// <summary>Palettes automatiques issues de la bibliothèque (PAL-003) : disponibles sans aucune action de l'utilisateur.</summary>
public static class AutoPalettes
{
    /// <summary>Palettes automatiques des appareils donnés, dans l'ordre des canaux puis des plages.</summary>
    public static IReadOnlyList<AutoPalette> For(IEnumerable<FixtureInfo> fixtures)
    {
        ArgumentNullException.ThrowIfNull(fixtures);
        var result = new List<AutoPalette>();
        var index = new Dictionary<(Fixtures.Model.AttributeKind, string), int>();
        foreach (var fixture in fixtures.DistinctBy(f => (f.Type.Id, f.Mode.Name)))
        {
            foreach (var channel in fixture.Channels)
            {
                foreach (var capability in channel.Capabilities.Where(c => c.AutoPalette))
                {
                    var key = (channel.Attribute, capability.Label);
                    var range = (fixture.Type.Id, channel.Key, capability.Min, capability.Max);
                    if (index.TryGetValue(key, out var existing))
                    {
                        var palette = result[existing];
                        if (!palette.Ranges.Any(r => r.FixtureTypeId == fixture.Type.Id))
                        {
                            result[existing] = palette with { Ranges = [.. palette.Ranges, range] };
                        }

                        continue;
                    }

                    index[key] = result.Count;
                    result.Add(new AutoPalette(channel.Attribute, capability.Label, capability.Colors.Count == 1 ? capability.Colors[0] : null, [range]));
                }
            }
        }

        return result;
    }

    /// <summary>Valeurs de scène qui appliquent une palette automatique aux appareils donnés (ceux dont le modèle la connaît).</summary>
    public static IReadOnlyList<SceneValue> Apply(AutoPalette palette, IEnumerable<FixtureInfo> fixtures)
    {
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(fixtures);
        var values = new List<SceneValue>();
        foreach (var fixture in fixtures)
        {
            foreach (var range in palette.Ranges.Where(r => r.FixtureTypeId == fixture.Type.Id))
            {
                values.Add(new SceneValue
                {
                    Target = ValueTarget.Fixture(fixture.Fixture.Id),
                    Channel = range.ChannelKey,
                    Range = new RangeValue(range.Min, range.Max),
                });
            }
        }

        return values;
    }
}
