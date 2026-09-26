using System.Globalization;
using Luxia.Fixtures.Model;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Rules;

/// <summary>
/// Rapports d'utilisation (SCN-013, PAL-006, SC-03) et figeage d'une palette dans les scènes avant sa suppression.
/// </summary>
public static class SceneUsage
{
    /// <summary>Où une palette est utilisée : une ligne par étape qui la référence.</summary>
    public static IReadOnlyList<string> PaletteUsages(SceneSet scenes, Guid paletteId)
    {
        ArgumentNullException.ThrowIfNull(scenes);
        var result = new List<string>();
        foreach (var scene in scenes.Scenes)
        {
            for (var s = 0; s < scene.Steps.Count; s++)
            {
                if (scene.Steps[s].Values.Any(v => v.PaletteId == paletteId))
                {
                    result.Add(string.Create(CultureInfo.CurrentCulture, $"Scène « {scene.Name} », étape {s + 1}"));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Ce qui utilise une scène (SCN-013) : scènes qui s'enchaînent sur elle. Les séquences et shows (P8)
    /// s'ajouteront ici.
    /// </summary>
    public static IReadOnlyList<string> SceneUsages(SceneSet scenes, Guid sceneId)
    {
        ArgumentNullException.ThrowIfNull(scenes);
        return [.. scenes.Scenes
            .Where(s => s.Id != sceneId && s.End == Engine.Model.EndMode.Chain && s.ChainSceneId == sceneId)
            .Select(s => $"Scène « {s.Name} » (enchaînement en fin de scène)")];
    }

    /// <summary>
    /// Remplace, dans toutes les scènes, les références à une palette par sa valeur actuelle (PAL-006,
    /// « figer les valeurs ») : couleur logique ou niveau quand c'est possible, sinon valeurs par appareil.
    /// </summary>
    public static SceneSet FreezePalette(SceneSet scenes, Palette palette, PatchContext patch)
    {
        ArgumentNullException.ThrowIfNull(scenes);
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(patch);
        return scenes with
        {
            Scenes = [.. scenes.Scenes.Select(scene => scene with
            {
                Steps = [.. scene.Steps.Select(step => step with
                {
                    Values = [.. step.Values.SelectMany(v => v.PaletteId == palette.Id ? Freeze(v, palette, patch) : [v])],
                })],
            })],
        };
    }

    /// <summary>
    /// Changement de mode d'un appareil (SC-03) : valeurs de scènes qui visent cet appareil et un attribut ou un canal
    /// absent du nouveau mode (elles seront ignorées ; tout le reste est conservé tel quel).
    /// </summary>
    /// <param name="scenes">Scènes du projet.</param>
    /// <param name="fixture">Appareil dont le mode change.</param>
    /// <param name="newMode">Nouveau mode.</param>
    /// <param name="patch">Patch résolu : s'il est fourni, les valeurs sur une sélection qui contient l'appareil comptent aussi.</param>
    public static IReadOnlyList<string> ModeChangeImpact(SceneSet scenes, FixtureInfo fixture, FixtureMode newMode, PatchContext? patch = null)
    {
        ArgumentNullException.ThrowIfNull(scenes);
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(newMode);
        var after = fixture.WithMode(newMode);
        var result = new List<string>();
        foreach (var scene in scenes.Scenes)
        {
            for (var s = 0; s < scene.Steps.Count; s++)
            {
                foreach (var value in scene.Steps[s].Values.Where(v => Targets(v.Target, fixture, patch)))
                {
                    var lost = value.Channel is { } key
                        ? after.Channels.All(c => c.Key != key)
                        : value.Attribute is { } attribute && !ValueResolver.Keys(after, value.Target.Cell, attribute).Any();
                    if (lost)
                    {
                        var what = value.Channel ?? AttributeCatalog.Label(value.Attribute!.Value);
                        result.Add(string.Create(CultureInfo.CurrentCulture, $"Scène « {scene.Name} », étape {s + 1} : « {what} » n'existe pas dans le mode « {newMode.Name} »"));
                    }
                }
            }
        }

        return result;
    }

    private static bool Targets(ValueTarget target, FixtureInfo fixture, PatchContext? patch) =>
        target.FixtureId == fixture.Fixture.Id
        || (target.FixtureId is null && patch is not null && patch.Members(target, out _).Any(m => m.Fixture.Fixture.Id == fixture.Fixture.Id));

    private static List<SceneValue> Freeze(SceneValue value, Palette palette, PatchContext patch)
    {
        var bare = value with { PaletteId = null };
        if (palette.Values.Count == 0)
        {
            switch (palette.Kind)
            {
                case PaletteKind.Color when palette.Light is { } light:
                    return [bare with { Color = light }];
                case PaletteKind.Intensity when palette.Level is { } level:
                    return [bare with { Attribute = AttributeKind.Intensity, Level = level }];
                default:
                    break;
            }
        }

        // Valeurs propres à des appareils ou des modèles : figées appareil par appareil, canal par canal.
        var frozen = new List<SceneValue>();
        foreach (var (fixture, cell) in patch.Members(value.Target, out _))
        {
            foreach (var (key, level) in ValueResolver.ResolveMember(fixture, cell, value, palette))
            {
                frozen.Add(bare with { Target = ValueTarget.Fixture(fixture.Fixture.Id), Channel = key, Level = level, Spread = null });
            }
        }

        return frozen;
    }
}
