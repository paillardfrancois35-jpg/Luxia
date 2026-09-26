using System.Globalization;
using Luxia.Engine.Model;
using Luxia.Fixtures.Rules;
using Luxia.Patch;
using Luxia.Persistence;
using Luxia.Scenes;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;

namespace Luxia.Hosting.Tools;

/// <summary>
/// Validation d'un projet sans ouvrir l'application (GEN-131) : lecture de chaque fichier (format, version),
/// références (appareils, sélections, palettes, couches, scènes enchaînées) et règles des modules. Chaque problème dit
/// où il se trouve (fichier, objet, champ) : une IA de conception peut corriger ce qu'elle a écrit (doc 02 §17b).
/// </summary>
public static class ProjectValidator
{
    /// <summary>Valide le projet du dossier donné.</summary>
    public static IReadOnlyList<CompileIssue> Validate(string folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        var issues = new List<CompileIssue>();
        var report = ProjectStore.Open(folder);
        if (!report.Succeeded)
        {
            issues.Add(Error(ProjectStore.ProjectFileName, "projet", "—", string.Join(" ", report.Messages.DefaultIfEmpty("projet illisible"))));
            return issues;
        }

        var (installation, installationMessage) = InstallationStore.Load(folder);
        var (venues, venuesMessage) = VenueStore.Load(folder);
        var (scenes, scenesMessage) = SceneStore.Load(folder);
        var (palettes, palettesMessage) = PaletteStore.Load(folder);
        var (layers, layersMessage) = LayerStore.Load(folder);
        var (safety, safetyMessage) = SafetyStore.Load(folder);
        var (live, liveMessage) = LiveStore.Load(folder);
        AddLoadMessage(issues, InstallationStore.FileName, installationMessage);
        AddLoadMessage(issues, VenueStore.FileName, venuesMessage);
        AddLoadMessage(issues, SceneStore.FileName, scenesMessage);
        AddLoadMessage(issues, PaletteStore.FileName, palettesMessage);
        AddLoadMessage(issues, LayerStore.FileName, layersMessage);
        AddLoadMessage(issues, SafetyStore.FileName, safetyMessage);
        AddLoadMessage(issues, LiveStore.FileName, liveMessage);

        var library = new ProjectFixtureLibrary(folder);
        var content = new ProjectContent(installation, venues, library.Find, layers, scenes, palettes, safety);
        issues.AddRange(ShowCompiler.Compile(content).Issues);
        issues.AddRange(CheckScenes(scenes, layers, palettes));
        issues.AddRange(CheckLayers(scenes, layers, palettes));
        issues.AddRange(CheckPalettes(palettes));
        issues.AddRange(CheckSafety(safety));
        issues.AddRange(CheckLive(live, scenes, layers));
        return issues;
    }

    private static IEnumerable<CompileIssue> CheckLayers(SceneSet scenes, LayerSet layers, PaletteSet palettes)
    {
        var file = LayerStore.FileName;
        foreach (var duplicate in layers.Layers.GroupBy(l => l.Id).Where(g => g.Count() > 1))
        {
            yield return Error(file, $"couche « {duplicate.First().Name} »", "id", "identifiant utilisé par plusieurs couches (GEN-052)");
        }

        var sceneIds = scenes.Scenes.Select(s => s.Id).ToHashSet();
        foreach (var layer in layers.Layers)
        {
            if (layer.RestSceneId is { } rest && !sceneIds.Contains(rest))
            {
                yield return Warning(file, $"couche « {layer.Name} »", "restSceneId", "scène de repos introuvable (COU-009)");
            }
        }

        // COU-008 : scène qui touche des familles d'attributs hors de celles de sa couche (non bloquant).
        var byId = layers.Layers.GroupBy(l => l.Id).ToDictionary(g => g.Key, g => g.First());
        foreach (var scene in scenes.Scenes)
        {
            if (!byId.TryGetValue(scene.LayerId, out var layer))
            {
                continue;
            }

            var outside = LayerRules.OutOfFamily(scene, layer, palettes);
            if (outside.Count > 0)
            {
                yield return Warning(
                    SceneStore.FileName,
                    $"scène « {scene.Name} »",
                    "layerId",
                    $"touche {string.Join(", ", outside.Select(LayerRules.Label))}, hors des familles de la couche « {layer.Name} » (COU-008)");
            }
        }
    }

    private static IEnumerable<CompileIssue> CheckLive(LiveSettings live, SceneSet scenes, LayerSet layers)
    {
        var file = LiveStore.FileName;
        var sceneIds = scenes.Scenes.Select(s => s.Id).ToHashSet();
        if (live.FlashSceneId is { } flash && !sceneIds.Contains(flash))
        {
            yield return Warning(file, "Live", "flashSceneId", "scène du bouton FLASH introuvable");
        }

        if (live.StrobeSceneId is { } strobe && !sceneIds.Contains(strobe))
        {
            yield return Warning(file, "Live", "strobeSceneId", "scène du bouton STROBE introuvable");
        }

        var layerIds = layers.Layers.Select(l => l.Id).ToHashSet();
        foreach (var hidden in live.HiddenLayerIds.Where(id => !layerIds.Contains(id)))
        {
            yield return Warning(file, "Live", "hiddenLayerIds", $"couche masquée introuvable ({hidden})");
        }

        if (live.SmokeBurstSeconds is <= 0 or > 60)
        {
            yield return Warning(file, "Live", "smokeBurstSeconds", "rafale de fumée hors de 0-60 s");
        }
    }

    private static IEnumerable<CompileIssue> CheckSafety(SafetySettings safety)
    {
        var file = SafetyStore.FileName;
        if (safety.Strobe.MaxContinuousSeconds is < 0.5 or > 600)
        {
            yield return Warning(file, "strobe", "maxContinuousSeconds", "durée de strobe hors de 0,5-600 s : ramenée dans ces bornes");
        }

        if (safety.Strobe.MaxSpeedPercent is < 0 or > 100)
        {
            yield return Warning(file, "strobe", "maxSpeedPercent", "vitesse maximale hors de 0-100 %");
        }

        if (safety.Smoke.MaxEmissionSeconds is < 0.5 or > 600)
        {
            yield return Warning(file, "fumée", "maxEmissionSeconds", "durée d'émission hors de 0,5-600 s : ramenée dans ces bornes");
        }

        if (safety.Smoke.MinRestSeconds < 0)
        {
            yield return Warning(file, "fumée", "minRestSeconds", "repos minimal négatif : compté comme 0");
        }
    }

    private static void AddLoadMessage(List<CompileIssue> issues, string file, string? message)
    {
        if (message is not null)
        {
            issues.Add(Error(file, "fichier", "—", message));
        }
    }

    private static IEnumerable<CompileIssue> CheckScenes(SceneSet scenes, LayerSet layers, PaletteSet palettes)
    {
        var file = SceneStore.FileName;
        foreach (var duplicate in scenes.Scenes.GroupBy(s => s.Id).Where(g => g.Count() > 1))
        {
            yield return Error(file, $"scène « {duplicate.First().Name} »", "id", string.Create(CultureInfo.CurrentCulture, $"identifiant utilisé par {duplicate.Count()} scènes (GEN-052)"));
        }

        var ids = scenes.Scenes.Select(s => s.Id).ToHashSet();
        var paletteKinds = palettes.Palettes.GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First().Kind);
        foreach (var scene in scenes.Scenes)
        {
            var where = $"scène « {scene.Name} »";
            if (string.IsNullOrWhiteSpace(scene.Name))
            {
                yield return Warning(file, where, "name", "nom vide");
            }

            if (scene.Steps.Count == 0)
            {
                yield return Error(file, where, "steps", "au moins une étape est nécessaire (SCN-002)");
            }

            if (scene.End == EndMode.Chain && (scene.ChainSceneId is not { } chain || !ids.Contains(chain)))
            {
                yield return Error(file, where, "chainSceneId", "fin « enchaîner » sans scène enchaînée existante");
            }

            if (scene.Speed is < 0.1 or > 10)
            {
                yield return Warning(file, where, "speed", "vitesse hors de 0,1 à 10 : elle sera bornée (MOT-015)");
            }

            if (scene.Loop == LoopMode.Count && scene.LoopCount < 1)
            {
                yield return Warning(file, where, "loopCount", "nombre de passages inférieur à 1 : 1 sera utilisé");
            }

            for (var s = 0; s < scene.Steps.Count; s++)
            {
                for (var v = 0; v < scene.Steps[s].Values.Count; v++)
                {
                    var value = scene.Steps[s].Values[v];
                    var at = string.Create(CultureInfo.CurrentCulture, $"{where}, étape {s + 1}");
                    var field = string.Create(CultureInfo.CurrentCulture, $"values[{v}]");
                    if (CheckValue(value, paletteKinds) is { } problem)
                    {
                        yield return Error(file, at, field, problem);
                    }
                }
            }
        }
    }

    /// <summary>Une valeur de scène a exactement une forme valide (doc 50 : attribut ou canal + niveau ou plage, couleur, palette).</summary>
    private static string? CheckValue(SceneValue value, Dictionary<Guid, Scenes.Model.PaletteKind> palettes)
    {
        var forms = (value.PaletteId is not null ? 1 : 0) + (value.Color is not null ? 1 : 0) + (value.Level is not null || value.Range is not null ? 1 : 0);
        if (forms != 1)
        {
            return "une valeur doit avoir une seule forme : level (ou range), color, ou paletteId";
        }

        if ((value.Level is not null || value.Range is not null) && value.Attribute is null && value.Channel is null)
        {
            return "level / range sans attribute ni channel";
        }

        if (value.Level is { } level && level is < 0 or > 1)
        {
            return string.Create(CultureInfo.CurrentCulture, $"level {level} hors de 0 à 1 (valeurs normalisées, GEN-020)");
        }

        if (value.Range is { } range && (range.Min < 0 || range.Max > 255 || range.Min > range.Max))
        {
            return "range : bornes attendues 0 ≤ min ≤ max ≤ 255";
        }

        if (value.Color is { } color && new[] { color.R, color.G, color.B }.Any(c => c is < 0 or > 1))
        {
            return "color : r, g, b attendus entre 0 et 1";
        }

        var target = value.Target;
        var targets = (target.FixtureId is not null ? 1 : 0) + (target.SelectionId is not null ? 1 : 0) + (target.Auto is not null ? 1 : 0);
        return targets == 1 ? null : "target : une seule forme attendue (fixtureId, selectionId ou auto)";
    }

    private static IEnumerable<CompileIssue> CheckPalettes(PaletteSet palettes)
    {
        var file = PaletteStore.FileName;
        foreach (var palette in palettes.Palettes)
        {
            var where = $"palette « {palette.Name} »";
            switch (palette.Kind)
            {
                case Scenes.Model.PaletteKind.Color when palette.Light is null && palette.Values.Count == 0:
                    yield return Error(file, where, "light", "palette couleur sans couleur (light) ni valeurs par modèle");
                    break;
                case Scenes.Model.PaletteKind.Intensity when palette.Level is null:
                    yield return Error(file, where, "level", "palette d'intensité sans niveau");
                    break;
                case Scenes.Model.PaletteKind.Position or Scenes.Model.PaletteKind.Beam when palette.Values.Count == 0:
                    yield return Warning(file, where, "values", "palette vide : elle ne règle rien");
                    break;
            }
        }
    }

    private static CompileIssue Error(string file, string item, string field, string message) =>
        new(IssueSeverity.Error, file, item, field, message);

    private static CompileIssue Warning(string file, string item, string field, string message) =>
        new(IssueSeverity.Warning, file, item, field, message);
}
