using Luxia.Engine;
using Luxia.Engine.Model;
using Luxia.Fixtures.Model;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;

namespace Luxia.Scenes.Compilation;

/// <summary>
/// Compile un effet de scène vers le moteur (doc 16 §6, D26) : cible développée en membres ordonnés (cellules
/// comprises, EFF-008), retard de phase de chaque membre (EFF-005), taille en degrés convertie selon la course de
/// chaque modèle, couleurs traduites selon les émetteurs de chaque appareil (EFF-004).
/// </summary>
public sealed class EffectCompiler
{
    /// <summary>Course Pan par défaut d'un modèle qui ne la précise pas (lyre courante).</summary>
    public const double DefaultPanRange = 540;

    /// <summary>Course Tilt par défaut.</summary>
    public const double DefaultTiltRange = 270;

    /// <summary>Nombre de teintes calculées pour un tour d'arc-en-ciel (interpolées entre elles par le moteur).</summary>
    private const int RainbowSamples = 36;

    private readonly PatchContext _patch;
    private readonly ValueResolver _resolver;

    /// <summary>Crée le compilateur d'effets.</summary>
    public EffectCompiler(PatchContext patch, ValueResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(patch);
        ArgumentNullException.ThrowIfNull(resolver);
        _patch = patch;
        _resolver = resolver;
    }

    /// <summary>Compile un effet ; <paramref name="problem"/> explique un effet sans effet possible.</summary>
    public EngineEffect Compile(SceneEffect effect, ShowModel model, out string? problem)
    {
        ArgumentNullException.ThrowIfNull(effect);
        ArgumentNullException.ThrowIfNull(model);
        var members = Members(effect, out problem);
        var channels = new List<EffectChannel>();
        var colors = effect.IsColor ? Colors(effect, ref problem) : [];
        for (var m = 0; m < members.Count; m++)
        {
            var (fixture, cell) = members[m];
            var lag = Lag(effect, m, members.Count);
            if (effect.IsColor)
            {
                AddColor(channels, model, fixture, cell, m, lag, colors);
            }
            else if (effect.IsPosition)
            {
                AddPosition(channels, model, fixture, cell, m, lag, effect);
            }
            else
            {
                foreach (var key in ValueResolver.Keys(fixture, cell, effect.Attribute))
                {
                    Add(channels, model, fixture, key, new EffectChannel(-1, m, lag, Math.Clamp(effect.Center, 0, 1), Math.Clamp(effect.Size, 0, 1)));
                }
            }
        }

        if (problem is null && members.Count > 0 && channels.Count == 0)
        {
            problem = effect.IsPosition
                ? "aucun membre de la cible n'a de Pan ou de Tilt"
                : effect.IsColor ? "aucun membre de la cible n'a de couleur" : "aucun membre de la cible n'a cet attribut";
        }

        return new EngineEffect
        {
            Id = effect.Id,
            Name = effect.Name ?? string.Empty,
            Shape = ToEngine(effect.Shape),
            Period = effect.Period.Value > 0 ? effect.Period : Duration.FromSeconds(1),
            DutyCycle = Math.Clamp(effect.DutyCycle, 0.01, 1),
            Direction = effect.Direction,
            Relative = effect.Relative && !effect.IsColor && effect.PositionPaletteId is null,
            Stepped = effect.Shape == SceneEffectShape.Alternate,
            Seed = SeedOf(effect.Id),
            Channels = channels,
        };
    }

    /// <summary>Membres de l'effet dans l'ordre : ceux de la cible, chacun développé en ses cellules si demandé (EFF-008).</summary>
    public IReadOnlyList<(FixtureInfo Fixture, int Cell)> Members(SceneEffect effect, out string? problem)
    {
        ArgumentNullException.ThrowIfNull(effect);
        problem = effect.Targets.Count == 0 ? "aucune cible (choisir des appareils)" : null;
        var members = new List<(FixtureInfo Fixture, int Cell)>();
        foreach (var target in effect.Targets)
        {
            var targetMembers = _patch.Members(target, out var targetProblem);
            foreach (var member in targetMembers)
            {
                if (!members.Exists(m => m.Fixture.Fixture.Id == member.Fixture.Fixture.Id && m.Cell == member.Cell))
                {
                    members.Add(member);
                }
            }

            problem ??= targetProblem;
        }

        if (!effect.PerCell)
        {
            return members;
        }

        var expanded = new List<(FixtureInfo, int)>();
        foreach (var (fixture, cell) in members)
        {
            var cells = cell > 0 ? [cell] : fixture.Cells.Where(c => c > 0).ToList();
            if (cells.Count == 0)
            {
                expanded.Add((fixture, 0));
                continue;
            }

            expanded.AddRange(cells.Select(c => (fixture, c)));
        }

        return expanded;
    }

    /// <summary>
    /// Retard de phase d'un membre, en fraction de cycle (EFF-005) : 0 = en tête. Linéaire : du premier au dernier ;
    /// miroir : du centre vers les bords ; groupes : un sur N ensemble ; aléatoire : ordre tiré une fois pour toutes.
    /// </summary>
    public static double Lag(SceneEffect effect, int index, int count)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if (count <= 1)
        {
            return 0;
        }

        var spread = Math.Clamp(effect.Spread, 0, 3600) / 360.0;
        switch (effect.PhaseMode)
        {
            case EffectPhaseMode.Mirror:
                {
                    var half = (count + 1) / 2;
                    var fromEdge = Math.Min(index, count - 1 - index);
                    return half <= 1 ? 0 : spread * (half - 1 - fromEdge) / half;
                }

            case EffectPhaseMode.Groups:
                {
                    var size = Math.Max(1, effect.GroupSize);
                    return spread * (index % size) / size;
                }

            case EffectPhaseMode.Random:
                {
                    var order = Enumerable.Range(0, count).ToArray();
                    new Random((int)(SeedOf(effect.Id) & 0x7FFFFFFF)).Shuffle(order);
                    return spread * order[index] / count;
                }

            default:
                return spread * index / count;
        }
    }

    /// <summary>Graine stable d'un effet (dérivée de son identifiant).</summary>
    public static ulong SeedOf(Guid id) => BitConverter.ToUInt64(id.ToByteArray(), 0);

    /// <summary>Forme du moteur qui joue une forme de scène (les formes de couleur deviennent des tables).</summary>
    public static EffectShape ToEngine(SceneEffectShape shape) => shape switch
    {
        SceneEffectShape.Sine => EffectShape.Sine,
        SceneEffectShape.Triangle => EffectShape.Triangle,
        SceneEffectShape.Square => EffectShape.Square,
        SceneEffectShape.SawUp => EffectShape.SawUp,
        SceneEffectShape.SawDown => EffectShape.SawDown,
        SceneEffectShape.Pulse => EffectShape.Pulse,
        SceneEffectShape.Random => EffectShape.Random,
        SceneEffectShape.Circle => EffectShape.Circle,
        SceneEffectShape.Eight => EffectShape.Eight,
        SceneEffectShape.SweepPan => EffectShape.SweepPan,
        SceneEffectShape.SweepTilt => EffectShape.SweepTilt,
        SceneEffectShape.RandomSlow => EffectShape.RandomSlow,
        _ => EffectShape.Table,
    };

    private static void Add(List<EffectChannel> channels, ShowModel model, FixtureInfo fixture, string key, EffectChannel channel)
    {
        var parameter = model.IndexOf(fixture.ReferenceId, key);
        if (parameter >= 0 && !channels.Exists(c => c.Parameter == parameter))
        {
            channels.Add(channel with { Parameter = parameter });
        }
    }

    private void AddPosition(List<EffectChannel> channels, ShowModel model, FixtureInfo fixture, int cell, int member, double lag, SceneEffect effect)
    {
        // Centre : la palette de position (lieu actif, PAL-004), sinon le centre réglé.
        var centers = new Dictionary<string, double>();
        if (effect.PositionPaletteId is { } paletteId && _resolver.Palette(paletteId) is { } palette)
        {
            foreach (var (key, level) in ValueResolver.ResolveMember(fixture, cell, new SceneValue { Target = new() }, palette, _patch.VenueKey))
            {
                centers[key] = level;
            }
        }

        var physical = fixture.Type.Physical;
        var panRange = physical.PanRange is > 0 and var pan ? pan : DefaultPanRange;
        var tiltRange = physical.TiltRange is > 0 and var tilt ? tilt : DefaultTiltRange;
        foreach (var (attribute, axis, range) in new[] { (AttributeKind.Pan, EffectAxis.X, panRange), (AttributeKind.Tilt, EffectAxis.Y, tiltRange) })
        {
            foreach (var key in ValueResolver.Keys(fixture, cell, attribute))
            {
                var center = centers.TryGetValue(key, out var c) ? c : Math.Clamp(effect.Center, 0, 1);
                var size = Math.Clamp(effect.Size / range, 0, 1);
                Add(channels, model, fixture, key, new EffectChannel(-1, member, lag, center, size, axis));
            }
        }
    }

    private static void AddColor(
        List<EffectChannel> channels,
        ShowModel model,
        FixtureInfo fixture,
        int cell,
        int member,
        double lag,
        List<LogicalColor> colors)
    {
        if (colors.Count == 0)
        {
            return;
        }

        var cells = cell == 0 ? fixture.Cells : [cell];
        foreach (var c in cells)
        {
            // Une table par canal de couleur : la valeur de chaque couleur du cycle, traduite pour ce modèle (MOT-050 à 053).
            var tables = new Dictionary<string, double[]>();
            for (var i = 0; i < colors.Count; i++)
            {
                foreach (var (channel, level) in ColorConversion.Translate(fixture.Type, fixture.Mode, c, colors[i]))
                {
                    if (!tables.TryGetValue(channel.Key, out var table))
                    {
                        table = new double[colors.Count];
                        tables[channel.Key] = table;
                    }

                    table[i] = Math.Clamp(level, 0, 1);
                }
            }

            foreach (var (key, table) in tables)
            {
                Add(channels, model, fixture, key, new EffectChannel(-1, member, lag, table[0], 0, EffectAxis.X, table));
            }
        }
    }

    private List<LogicalColor> Colors(SceneEffect effect, ref string? problem)
    {
        if (effect.Shape == SceneEffectShape.Rainbow)
        {
            return [.. Enumerable.Range(0, RainbowSamples).Select(i => Hue((double)i / RainbowSamples))];
        }

        var colors = new List<LogicalColor>();
        if (effect.ThemeId is { } themeId)
        {
            if (_resolver.Palette(themeId) is { Kind: PaletteKind.Theme } theme)
            {
                colors.AddRange(theme.Colors);
            }
            else
            {
                problem ??= "thème introuvable";
            }
        }
        else
        {
            foreach (var color in effect.Colors)
            {
                if (color.PaletteId is { } paletteId)
                {
                    if (_resolver.Palette(paletteId)?.Light is { } light)
                    {
                        colors.Add(light);
                    }
                    else
                    {
                        problem ??= "palette couleur introuvable";
                    }
                }
                else if (color.Color is { } logical)
                {
                    colors.Add(logical);
                }
            }
        }

        if (colors.Count == 0)
        {
            problem ??= "aucune couleur (choisir des couleurs ou un thème)";
        }

        return colors;
    }

    /// <summary>Couleur pure d'une teinte (0-1 = tour complet de la roue).</summary>
    public static LogicalColor Hue(double hue)
    {
        var h = (hue - Math.Floor(hue)) * 6;
        var x = 1 - Math.Abs((h % 2) - 1);
        var (r, g, b) = (int)h switch
        {
            0 => (1.0, x, 0.0),
            1 => (x, 1.0, 0.0),
            2 => (0.0, 1.0, x),
            3 => (0.0, x, 1.0),
            4 => (x, 0.0, 1.0),
            _ => (1.0, 0.0, x),
        };
        return new LogicalColor { R = r, G = g, B = b };
    }
}
