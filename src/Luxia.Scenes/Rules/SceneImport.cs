using System.Globalization;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Rules;

/// <summary>Résultat d'un import de scènes (GEN-133).</summary>
/// <param name="Scenes">Scènes du projet après import.</param>
/// <param name="Report">Une ligne par scène importée ou écartée, avec la raison.</param>
/// <param name="Imported">Nombre de scènes ajoutées.</param>
public sealed record SceneImportResult(SceneSet Scenes, IReadOnlyList<string> Report, int Imported);

/// <summary>
/// Import d'un lot de scènes produites à côté de l'application, par exemple par une IA de conception (GEN-133, D18) :
/// on <b>ajoute</b>, on n'écrase jamais. Une scène dont l'identifiant existe déjà est écartée (et signalée) ; une scène
/// sans catégorie est rangée dans « Proposé par IA » ; un nom déjà pris reçoit un suffixe.
/// </summary>
public static class SceneImport
{
    /// <summary>Ajoute les scènes de <paramref name="incoming"/> à <paramref name="existing"/>.</summary>
    public static SceneImportResult Merge(SceneSet existing, SceneSet incoming)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(incoming);
        var ids = existing.Scenes.Select(s => s.Id).ToHashSet();
        var names = existing.Scenes.Select(s => s.Name).ToHashSet(StringComparer.CurrentCultureIgnoreCase);
        var added = new List<Scene>();
        var report = new List<string>();
        foreach (var scene in incoming.Scenes)
        {
            if (!ids.Add(scene.Id))
            {
                report.Add($"« {scene.Name} » écartée : son identifiant existe déjà dans le projet (rien n'est écrasé).");
                continue;
            }

            var name = scene.Name;
            for (var n = 2; !names.Add(name); n++)
            {
                name = string.Create(CultureInfo.CurrentCulture, $"{scene.Name} ({n})");
            }

            var imported = scene with
            {
                Name = name,
                Category = string.IsNullOrWhiteSpace(scene.Category) ? SceneSet.AiCategory : scene.Category,
            };
            added.Add(imported);
            report.Add(name == scene.Name
                ? $"« {name} » importée ({imported.Category})."
                : $"« {scene.Name} » importée sous le nom « {name} » (nom déjà pris).");
        }

        return new SceneImportResult(existing with { Scenes = [.. existing.Scenes, .. added] }, report, added.Count);
    }
}
