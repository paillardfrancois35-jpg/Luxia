using System.Globalization;
using Dmx.Fixtures.Model;

namespace Dmx.Fixtures.Rules;

/// <summary>Validation d'un modèle d'appareil (BIB-004, BIB-002).</summary>
public static class FixtureValidator
{
    /// <summary>Contrôle le modèle et renvoie la liste des problèmes (vide si tout va bien).</summary>
    public static IReadOnlyList<ValidationIssue> Validate(FixtureType fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        var issues = new List<ValidationIssue>();

        if (string.IsNullOrWhiteSpace(fixture.Manufacturer) || string.IsNullOrWhiteSpace(fixture.Model))
        {
            issues.Add(Error("Identité", "le fabricant et le modèle sont obligatoires."));
        }

        foreach (var duplicate in fixture.Channels.GroupBy(c => c.Key, StringComparer.Ordinal).Where(g => g.Count() > 1))
        {
            issues.Add(Error($"Canal {duplicate.Key}", "deux définitions de canal ont la même clé."));
        }

        foreach (var channel in fixture.Channels)
        {
            ValidateChannel(fixture, channel, issues);
        }

        if (fixture.Modes.Count == 0)
        {
            issues.Add(Error("Modes", "le modèle doit avoir au moins un mode."));
        }

        foreach (var duplicate in fixture.Modes.GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        {
            issues.Add(Error($"Mode {duplicate.Key}", "deux modes ont le même nom."));
        }

        foreach (var mode in fixture.Modes)
        {
            ValidateMode(fixture, mode, issues);
        }

        return issues;
    }

    /// <summary>Le modèle ne contient aucune erreur (les avertissements sont admis).</summary>
    public static bool IsValid(FixtureType fixture) => Validate(fixture).All(i => i.Severity != IssueSeverity.Error);

    private static void ValidateChannel(FixtureType fixture, ChannelDefinition channel, List<ValidationIssue> issues)
    {
        var where = $"Canal « {channel.Name} »";
        var max = 255;
        if (channel.Default < 0 || channel.Default > max)
        {
            issues.Add(Error(where, "valeur par défaut hors de 0-255."));
        }

        if (channel.Wheel is { } wheel && fixture.Wheels.All(w => w.Key != wheel))
        {
            issues.Add(Error(where, $"roue « {wheel} » inconnue."));
        }

        var ranges = channel.Capabilities.OrderBy(c => c.Min).ToList();
        foreach (var range in ranges)
        {
            if (range.Min < 0 || range.Max > max || range.Min > range.Max)
            {
                issues.Add(Error(where, $"plage « {range.Label} » invalide ({range.Min}-{range.Max})."));
            }
        }

        for (var i = 1; i < ranges.Count; i++)
        {
            var previous = ranges[i - 1];
            var current = ranges[i];
            if (current.Min <= previous.Max)
            {
                issues.Add(Error(where, $"les plages « {previous.Label} » ({previous.Min}-{previous.Max}) et « {current.Label} » ({current.Min}-{current.Max}) se chevauchent."));
            }
            else if (current.Min > previous.Max + 1)
            {
                issues.Add(Warning(where, string.Create(CultureInfo.InvariantCulture, $"trou entre {previous.Max} et {current.Min} (valeurs {previous.Max + 1}-{current.Min - 1} sans plage).")));
            }
        }

        if (ranges.Count > 0)
        {
            if (ranges[0].Min > 0)
            {
                issues.Add(Warning(where, $"valeurs 0-{ranges[0].Min - 1} sans plage."));
            }

            if (ranges[^1].Max < max)
            {
                issues.Add(Warning(where, $"valeurs {ranges[^1].Max + 1}-{max} sans plage."));
            }
        }
    }

    private static void ValidateMode(FixtureType fixture, FixtureMode mode, List<ValidationIssue> issues)
    {
        var where = $"Mode « {mode.Name} »";
        if (mode.Channels.Count == 0)
        {
            issues.Add(Error(where, "le mode n'a aucun canal."));
            return;
        }

        if (mode.Channels.Count > 512)
        {
            issues.Add(Error(where, "plus de 512 canaux."));
        }

        // Deux positions du mode ne peuvent pas porter le même canal (même octet).
        foreach (var duplicate in mode.Channels.GroupBy(m => m).Where(g => g.Count() > 1))
        {
            issues.Add(Error(where, $"le canal « {Name(fixture, duplicate.Key.Channel)} »{(duplicate.Key.Part == ChannelPart.Fine ? " (fin)" : string.Empty)} occupe plusieurs positions."));
        }

        for (var i = 0; i < mode.Channels.Count; i++)
        {
            var slot = mode.Channels[i];
            var position = $"{where}, position {i + 1}";
            var definition = fixture.Channel(slot.Channel);
            if (definition is null)
            {
                issues.Add(Error(position, $"canal « {slot.Channel} » inconnu."));
                continue;
            }

            if (slot.Part == ChannelPart.Fine)
            {
                if (definition.Resolution != ChannelResolution.Bit16)
                {
                    issues.Add(Error(position, $"octet fin d'un canal 8 bits (« {definition.Name} »)."));
                }
                else if (!mode.Channels.Contains(slot with { Part = ChannelPart.Coarse }))
                {
                    issues.Add(Error(position, $"canal fin orphelin : l'octet grossier de « {definition.Name} » n'est pas dans le mode."));
                }
            }
            else if (definition.Resolution == ChannelResolution.Bit16 && !mode.Channels.Contains(slot with { Part = ChannelPart.Fine }))
            {
                issues.Add(Warning(position, $"« {definition.Name} » est en 16 bits mais seul l'octet grossier est dans le mode."));
            }
        }
    }

    private static string Name(FixtureType fixture, string key) => fixture.Channel(key)?.Name ?? key;

    private static ValidationIssue Error(string location, string message) => new(IssueSeverity.Error, location, message);

    private static ValidationIssue Warning(string location, string message) => new(IssueSeverity.Warning, location, message);
}
