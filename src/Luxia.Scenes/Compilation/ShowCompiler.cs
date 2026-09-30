using System.Globalization;
using Luxia.Engine.Model;
using Luxia.Engine.Timing;
using Luxia.Fixtures.Model;
using Luxia.Fixtures.Rules;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;

namespace Luxia.Scenes.Compilation;

/// <summary>
/// Compile un projet vers le modèle du moteur (D26) : un paramètre par attribut d'appareil patché, puis les scènes
/// avec leurs valeurs résolues (sélections développées, palettes traduites, couleurs converties).
/// Hors du fil du moteur ; le résultat est chargé par <see cref="Engine.RenderEngine.LoadShow"/>.
/// </summary>
public static class ShowCompiler
{
    /// <summary>Fichier des scènes (pour les messages).</summary>
    public const string ScenesFile = "scènes.json";

    /// <summary>Attributs toujours discrets : emplacements, programmes, sélecteurs (MOT-012).</summary>
    private static readonly HashSet<AttributeKind> DiscreteAttributes =
    [
        AttributeKind.ColorWheel,
        AttributeKind.ColorMacro,
        AttributeKind.Gobo,
        AttributeKind.Prism,
        AttributeKind.Program,
        AttributeKind.Mode,
        AttributeKind.Reset,
        AttributeKind.Maintenance,
        AttributeKind.LampControl,
    ];

    /// <summary>Compile le projet.</summary>
    public static CompileResult Compile(ProjectContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var issues = new List<CompileIssue>();
        var patch = new PatchContext(content.Installation, content.Venues, content.TypeOf);
        foreach (var problem in patch.Problems)
        {
            issues.Add(new CompileIssue(IssueSeverity.Warning, "installation.json", problem, "fixtureTypeId", "appareil ignoré"));
        }

        var parameters = BuildParameters(patch, out var aliases);
        var layers = content.Layers.Layers.Select(ToEngine).ToList();
        var provisional = new ShowModel(parameters, layers, [], aliases);
        var resolver = new ValueResolver(patch, content.Palettes);
        var effects = new EffectCompiler(patch, resolver);
        var layerIds = layers.Select(l => l.Id).ToHashSet();
        var scenes = new List<EngineScene>();
        foreach (var scene in content.Scenes.Scenes)
        {
            scenes.Add(CompileScene(scene, provisional, resolver, effects, layerIds, layers, issues));
        }

        // PAL-008 : positions jamais calibrées dans le lieu actif → valeur du lieu « Générique », signalée.
        foreach (var palette in content.Palettes.Palettes.Where(p => p.Kind == PaletteKind.Position))
        {
            var missing = VenuePalettes.Uncalibrated(palette, patch.VenueKey)
                .Select(id => patch.Find(id))
                .Where(f => f is { Absent: false })
                .Select(f => f!.Fixture.Name)
                .ToList();
            if (missing.Count > 0)
            {
                issues.Add(new CompileIssue(
                    IssueSeverity.Warning,
                    "palettes.json",
                    $"palette « {palette.Name} »",
                    "values",
                    $"{string.Join(", ", missing)} : position non calibrée dans le lieu « {patch.Venue.Name} », valeur du lieu Générique utilisée (PAL-008)"));
            }
        }

        var safety = SafetyCompiler.Build(patch, provisional, content.Safety ?? new SafetySettings(), content.Venues.Active, issues);
        var (dimmerGroups, parameterGroups) = DimmerCompiler.Build(patch, content.Installation, content.Groups, parameters, issues);
        return new CompileResult(new ShowModel(parameters, layers, scenes, aliases, safety, ColorGroups(patch, provisional), dimmerGroups, parameterGroups), issues);
    }

    /// <summary>Paramètres des appareils patchés (jumeaux regroupés sur l'appareil de référence).</summary>
    public static IReadOnlyList<RigParameter> BuildParameters(PatchContext patch, out Dictionary<Guid, Guid> aliases)
    {
        ArgumentNullException.ThrowIfNull(patch);
        aliases = [];
        var parameters = new List<RigParameter>();
        var byReference = patch.Fixtures.GroupBy(f => f.ReferenceId);
        foreach (var group in byReference)
        {
            var reference = group.First(f => f.Fixture.Id == group.Key);
            foreach (var twin in group.Where(f => f.Fixture.Id != group.Key))
            {
                aliases[twin.Fixture.Id] = group.Key;
            }

            AddFixture(parameters, reference, group.ToList());
        }

        return parameters;
    }

    private static void AddFixture(List<RigParameter> parameters, FixtureInfo reference, IReadOnlyList<FixtureInfo> members)
    {
        var type = reference.Type;
        var mode = reference.Mode;
        var name = reference.Fixture.Name;
        var channels = reference.Channels;

        var virtualIndex = -1;
        if (reference.NeedsVirtualIntensity)
        {
            virtualIndex = parameters.Count;
            parameters.Add(new RigParameter
            {
                FixtureId = reference.Fixture.Id,
                ChannelKey = RigParameter.VirtualIntensityKey,
                Label = $"{name} – Intensité (virtuelle)",
                Role = ParameterRole.Intensity,
                Absent = members.All(m => m.Absent),
            });
        }

        // Les gradateurs d'abord : les canaux qui les suivent y font référence par indice (MOT-040).
        var first = parameters.Count;
        var order = channels.OrderBy(c => AttributeCatalog.IsIntensity(c.Attribute) ? 0 : 1).ToList();
        var indexOf = new Dictionary<string, int>();
        for (var i = 0; i < order.Count; i++)
        {
            indexOf[order[i].Key] = first + i;
        }

        foreach (var channel in order)
        {
            var attribute = channel.Attribute;
            var info = AttributeCatalog.Get(attribute);
            var sameAttribute = channels.Count(c => c.Attribute == attribute && c.Cell == channel.Cell);
            var label = sameAttribute > 1
                ? $"{name} – {channel.Name}"
                : $"{name} – {info.Label}{(channel.Cell > 0 ? string.Create(CultureInfo.CurrentCulture, $" {channel.Cell}") : string.Empty)}";

            var source = -1;
            if (FixtureRules.FollowsIntensity(type, mode, channel) && !AttributeCatalog.IsIntensity(attribute))
            {
                source = reference.DimmerFor(channel) is { } dimmer ? indexOf[dimmer.Key] : virtualIndex;
            }

            var options = reference.Fixture.Options;
            var inverted = channel.Inverted
                ^ (attribute == AttributeKind.Pan && options.InvertPan)
                ^ (attribute == AttributeKind.Tilt && options.InvertTilt);

            parameters.Add(new RigParameter
            {
                FixtureId = reference.Fixture.Id,
                ChannelKey = channel.Key,
                Cell = channel.Cell,
                Label = label,
                Role = AttributeCatalog.IsIntensity(attribute) ? ParameterRole.Intensity : info.IsEmitter ? ParameterRole.Emitter : ParameterRole.Other,
                Default = Math.Clamp(channel.Default / 255.0, 0, 1),
                Discrete = IsDiscrete(channel),
                Inverted = inverted,
                IntensitySource = source,
                Absent = members.All(m => m.Absent),
                Outputs = Outputs(channel, members),
            });
        }
    }

    /// <summary>Canal discret (MOT-012) : attribut à emplacements, ou plages dont au moins une n'est pas progressive.</summary>
    public static bool IsDiscrete(ChannelDefinition channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        return DiscreteAttributes.Contains(channel.Attribute)
            || (channel.Capabilities.Count >= 2 && channel.Capabilities.Any(c => c.Kind != CapabilityKind.Progressive));
    }

    private static List<ChannelAddress> Outputs(ChannelDefinition channel, IReadOnlyList<FixtureInfo> members)
    {
        var outputs = new List<ChannelAddress>();
        foreach (var member in members)
        {
            var slots = member.Mode.Channels;
            var coarse = -1;
            var fine = -1;
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i].Channel != channel.Key)
                {
                    continue;
                }

                if (slots[i].Part == ChannelPart.Fine)
                {
                    fine = i;
                }
                else if (coarse < 0)
                {
                    coarse = i;
                }
            }

            if (coarse < 0)
            {
                continue;
            }

            var address = new ChannelAddress(
                member.Fixture.Universe,
                member.Fixture.Address + coarse,
                fine >= 0 ? member.Fixture.Address + fine : 0);
            if (!outputs.Contains(address))
            {
                outputs.Add(address);
            }
        }

        return outputs;
    }

    private static EngineLayer ToEngine(Layer layer) => new()
    {
        Id = layer.Id,
        Name = layer.Name,
        Priority = layer.Priority,
        Exclusive = layer.Exclusive,
        Master = layer.Master,
        IntensityMode = layer.IntensityMode,
        MasterOnAllAttributes = layer.MasterOnAllAttributes,
        CrossFade = layer.CrossFade,
        Kind = layer.Kind,
        KeepOnStopAll = layer.KeepOnStopAll,
        RestSceneId = layer.RestSceneId,
    };

    private static EngineScene CompileScene(
        Scene scene,
        ShowModel model,
        ValueResolver resolver,
        EffectCompiler effects,
        HashSet<Guid> layerIds,
        List<EngineLayer> layers,
        List<CompileIssue> issues)
    {
        var layerId = scene.LayerId;
        if (!layerIds.Contains(layerId) && layers.Count > 0)
        {
            issues.Add(new CompileIssue(IssueSeverity.Warning, ScenesFile, $"scène « {scene.Name} »", "layerId", $"couche introuvable : jouée dans « {layers[0].Name} »"));
            layerId = layers[0].Id;
        }

        var steps = new List<EngineStep>();
        for (var s = 0; s < scene.Steps.Count; s++)
        {
            steps.Add(CompileStep(scene, s, model, resolver, effects, issues));
        }

        if (steps.Count == 0)
        {
            issues.Add(new CompileIssue(IssueSeverity.Error, ScenesFile, $"scène « {scene.Name} »", "steps", "aucune étape : la scène ne peut pas être jouée"));
        }

        return new EngineScene
        {
            Id = scene.Id,
            Name = scene.Name,
            LayerId = layerId,
            Loop = scene.Loop,
            LoopCount = Math.Max(1, scene.LoopCount),
            End = scene.End,
            ChainSceneId = scene.ChainSceneId,
            FadeIn = scene.FadeIn,
            FadeOut = scene.FadeOut,
            Speed = Math.Clamp(scene.Speed, 0.1, 10),
            Advance = scene.Advance,
            AdvanceEvery = Math.Clamp(scene.AdvanceEvery, 1, 64),
            Quantize = scene.Quantize,
            EnergySpeed = scene.EnergySpeed,
            OwnBpm = scene.OwnBpm is { } own ? Math.Clamp(own, MusicalClock.MinBpm, MusicalClock.MaxBpm) : null,
            Steps = steps,
        };
    }

    private static EngineStep CompileStep(Scene scene, int stepIndex, ShowModel model, ValueResolver resolver, EffectCompiler effects, List<CompileIssue> issues)
    {
        var step = scene.Steps[stepIndex];
        var where = string.Create(CultureInfo.CurrentCulture, $"scène « {scene.Name} », étape {stepIndex + 1}");

        // Une cible plus précise l'emporte : sélections d'abord, puis appareils, puis cellules ; à égalité, la dernière valeur.
        var ordered = step.Values
            .Select((value, index) => (Value: value, Index: index))
            .OrderBy(v => Specificity(v.Value.Target))
            .ThenBy(v => v.Index);

        var values = new Dictionary<int, StepValue>();
        foreach (var (value, index) in ordered)
        {
            var resolved = resolver.Resolve(value, out var problem);
            if (problem is not null)
            {
                issues.Add(new CompileIssue(
                    IssueSeverity.Warning,
                    ScenesFile,
                    where,
                    string.Create(CultureInfo.CurrentCulture, $"values[{index}]"),
                    problem));
                continue;
            }

            foreach (var item in resolved)
            {
                var parameter = model.IndexOf(item.FixtureId, item.ChannelKey);
                if (parameter < 0)
                {
                    continue;
                }

                var delay = value.Delay ?? Duration.Zero;
                if (value.Spread is { } spread && item.MemberCount > 1)
                {
                    // SCN-010 : retard réparti linéairement dans l'ordre de la sélection, dans l'unité de la répartition.
                    // TODO(P7, MOT-016) : retard et répartition d'unités différentes (secondes / temps) convertis au tempo courant.
                    var share = spread.Value * item.MemberIndex / (item.MemberCount - 1);
                    delay = delay.Unit == spread.Unit || delay.Value == 0
                        ? new Duration(delay.Value + share, spread.Unit)
                        : Duration.FromSeconds(delay.ToSeconds(120) + new Duration(share, spread.Unit).ToSeconds(120));
                }

                values[parameter] = new StepValue(parameter, item.Level, value.Fade, delay);
            }
        }

        var compiled = new List<EngineEffect>();
        for (var e = 0; e < step.Effects.Count; e++)
        {
            var effect = effects.Compile(step.Effects[e], model, out var problem);
            if (problem is not null)
            {
                issues.Add(new CompileIssue(
                    IssueSeverity.Warning,
                    ScenesFile,
                    $"{where}, effet « {step.Effects[e].Name ?? (e + 1).ToString(CultureInfo.CurrentCulture)} »",
                    string.Create(CultureInfo.CurrentCulture, $"effects[{e}]"),
                    problem));
            }

            if (effect.Channels.Count > 0)
            {
                compiled.Add(effect);
            }
        }

        return new EngineStep
        {
            Fade = step.Fade,
            Hold = step.Hold,
            Curve = step.Curve,
            Switch = step.Switch,
            Values = [.. values.Values.OrderBy(v => v.Parameter)],
            Effects = compiled,
            HueFade = step.HueFade,
        };
    }

    /// <summary>Triplets rouge / vert / bleu de chaque cellule (fondu par la teinte, MOT-054).</summary>
    private static List<ColorGroup> ColorGroups(PatchContext patch, ShowModel model)
    {
        var groups = new List<ColorGroup>();
        foreach (var fixture in patch.Fixtures.Where(f => f.ReferenceId == f.Fixture.Id))
        {
            foreach (var cell in fixture.Cells)
            {
                int Find(AttributeKind attribute)
                {
                    var channels = fixture.Channels.Where(c => c.Attribute == attribute && c.Cell == cell).ToList();
                    return channels.Count == 1 ? model.IndexOf(fixture.ReferenceId, channels[0].Key) : -1;
                }

                var (r, g, b) = (Find(AttributeKind.Red), Find(AttributeKind.Green), Find(AttributeKind.Blue));
                if (r >= 0 && g >= 0 && b >= 0)
                {
                    groups.Add(new ColorGroup(r, g, b));
                }
            }
        }

        return groups;
    }

    private static int Specificity(ValueTarget target) => target.FixtureId is null ? 0 : target.Cell == 0 ? 1 : 2;
}
