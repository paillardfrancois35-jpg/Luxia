using Luxia.Engine.Model;
using Luxia.Fixtures.Model;
using Luxia.Fixtures.Rules;
using Luxia.Patch.Model;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Compilation;

/// <summary>
/// Compile les limites de sûreté pour le moteur (doc 15 §9) : canaux étiquetés « strobe » ou « fumée » dans la
/// bibliothèque (BIB-007), avec leurs plages actives et leur valeur de repos, réglages du projet et zones interdites du
/// lieu actif.
/// </summary>
public static class SafetyCompiler
{
    /// <summary>Fichier des réglages (pour les messages).</summary>
    public const string SafetyFile = "sûreté.json";

    /// <summary>Construit le modèle de sûreté.</summary>
    /// <param name="patch">Patch résolu.</param>
    /// <param name="model">Modèle en cours de compilation (indices des paramètres Pan / Tilt).</param>
    /// <param name="settings">Réglages du projet.</param>
    /// <param name="venue">Lieu actif (zones interdites).</param>
    /// <param name="issues">Problèmes relevés (zones mal formées, appareil sans Pan/Tilt).</param>
    public static SafetyModel Build(PatchContext patch, ShowModel model, SafetySettings settings, Venue venue, List<CompileIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(patch);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(venue);
        ArgumentNullException.ThrowIfNull(issues);

        var strobe = new List<GuardedChannel>();
        var smoke = new List<GuardedChannel>();
        foreach (var fixture in patch.Fixtures.Where(f => !f.Absent))
        {
            foreach (var channel in fixture.Channels)
            {
                var tags = FixtureRules.Safety(channel);
                if (!tags.HasFlag(SafetyTags.Strobe) && !tags.HasFlag(SafetyTags.Smoke))
                {
                    continue;
                }

                var address = AddressOf(fixture, channel);
                if (address < 0)
                {
                    continue;
                }

                var label = $"{fixture.Fixture.Name} – {channel.Name}";
                if (tags.HasFlag(SafetyTags.Smoke))
                {
                    var rest = (byte)Math.Clamp(channel.Rest ?? 0, 0, 255);
                    smoke.Add(new GuardedChannel
                    {
                        FixtureId = fixture.Fixture.Id,
                        Label = label,
                        Universe = fixture.Fixture.Universe,
                        Channel = address,
                        Rest = rest,
                        Active = rest < 255 ? [new ByteRange(rest + 1, 255)] : [],
                    });
                }
                else
                {
                    strobe.Add(StrobeChannel(fixture, channel, label, address));
                }
            }
        }

        return new SafetyModel
        {
            Strobe = new StrobeLimits
            {
                MaxContinuousSeconds = Math.Max(0.5, settings.Strobe.MaxContinuousSeconds),
                PauseSeconds = Math.Max(0, settings.Strobe.PauseSeconds),
                Forbidden = settings.Strobe.Forbidden,
                MaxSpeed = Math.Clamp(settings.Strobe.MaxSpeedPercent / 100, 0, 1),
            },
            Smoke = new SmokeLimits
            {
                MaxEmissionSeconds = Math.Max(0.5, settings.Smoke.MaxEmissionSeconds),
                MinRestSeconds = Math.Max(0, settings.Smoke.MinRestSeconds),
                RestFactor = Math.Max(0, settings.Smoke.RestFactor),
            },
            StrobeChannels = strobe,
            SmokeChannels = smoke,
            Zones = Zones(patch, model, venue, issues),
        };
    }

    /// <summary>
    /// Canal de strobe : plages « strobe / pulsation / aléatoire » actives ; repos = plage « ouvert » (sinon valeur de
    /// repos du canal, sinon 0). Sans plage décrite, toute valeur au-dessus du repos compte comme un strobe.
    /// </summary>
    private static GuardedChannel StrobeChannel(FixtureInfo fixture, ChannelDefinition channel, string label, int address)
    {
        var strobing = channel.Capabilities.Where(c => c.Strobe is StrobeEffect.Strobe or StrobeEffect.Pulse or StrobeEffect.Random).ToList();
        var open = channel.Capabilities.FirstOrDefault(c => c.Strobe == StrobeEffect.Open);
        var rest = channel.Rest ?? open?.Min ?? 0;
        IReadOnlyList<ByteRange> active = strobing.Count > 0
            ? [.. strobing.Select(c => new ByteRange(c.Min, c.Max))]
            : rest < 255 ? [new ByteRange(rest + 1, 255)] : [];
        IReadOnlyList<ByteRange> progressive = strobing.Count > 0
            ? [.. strobing
                .Where(c => c.Kind == CapabilityKind.Progressive)
                .Select(c => new ByteRange(c.Min, c.Max, c.Parameter is not { } p || p.Start <= p.End))]
            : active;
        return new GuardedChannel
        {
            FixtureId = fixture.Fixture.Id,
            Label = label,
            Universe = fixture.Fixture.Universe,
            Channel = address,
            Rest = (byte)Math.Clamp(rest, 0, 255),
            Active = active,
            Progressive = progressive,
        };
    }

    private static List<MovementGuard> Zones(PatchContext patch, ShowModel model, Venue venue, List<CompileIssue> issues)
    {
        var guards = new List<MovementGuard>();
        foreach (var group in venue.ForbiddenZones.GroupBy(z => z.FixtureId))
        {
            var fixture = patch.Find(group.Key);
            var where = $"lieu « {venue.Name} », zone interdite";
            if (fixture is null)
            {
                issues.Add(new CompileIssue(IssueSeverity.Warning, "lieux.json", where, "fixtureId", "appareil introuvable dans le patch : zone ignorée"));
                continue;
            }

            var pan = fixture.Channels.FirstOrDefault(c => c.Attribute == AttributeKind.Pan);
            var tilt = fixture.Channels.FirstOrDefault(c => c.Attribute == AttributeKind.Tilt);
            var panIndex = pan is null ? -1 : model.IndexOf(fixture.ReferenceId, pan.Key);
            var tiltIndex = tilt is null ? -1 : model.IndexOf(fixture.ReferenceId, tilt.Key);
            if (panIndex < 0 || tiltIndex < 0)
            {
                issues.Add(new CompileIssue(IssueSeverity.Warning, "lieux.json", $"{where} de « {fixture.Fixture.Name} »", "fixtureId", "appareil sans Pan et Tilt : zone ignorée"));
                continue;
            }

            var zones = new List<PanTiltZone>();
            foreach (var zone in group)
            {
                if (zone.PanMin >= zone.PanMax || zone.TiltMin >= zone.TiltMax)
                {
                    issues.Add(new CompileIssue(
                        IssueSeverity.Warning,
                        "lieux.json",
                        $"{where} « {zone.Name ?? "sans nom"} » de « {fixture.Fixture.Name} »",
                        "panMin/panMax/tiltMin/tiltMax",
                        "rectangle vide (minimum ≥ maximum) : zone ignorée"));
                    continue;
                }

                zones.Add(new PanTiltZone(
                    Math.Clamp(zone.PanMin, 0, 1),
                    Math.Clamp(zone.PanMax, 0, 1),
                    Math.Clamp(zone.TiltMin, 0, 1),
                    Math.Clamp(zone.TiltMax, 0, 1)));
            }

            // Des jumeaux partagent Pan/Tilt : une seule garde par paramètre, avec toutes les zones du groupe.
            var existing = guards.FindIndex(g => g.Pan == panIndex && g.Tilt == tiltIndex);
            if (existing >= 0)
            {
                guards[existing] = guards[existing] with { Zones = [.. guards[existing].Zones, .. zones] };
            }
            else if (zones.Count > 0)
            {
                guards.Add(new MovementGuard { FixtureId = fixture.ReferenceId, Label = fixture.Fixture.Name, Pan = panIndex, Tilt = tiltIndex, Zones = zones });
            }
        }

        return guards;
    }

    private static int AddressOf(FixtureInfo fixture, ChannelDefinition channel)
    {
        var slots = fixture.Mode.Channels;
        for (var i = 0; i < slots.Count; i++)
        {
            if (slots[i].Channel == channel.Key && slots[i].Part != ChannelPart.Fine)
            {
                return fixture.Fixture.Address + i;
            }
        }

        return -1;
    }
}
