using Luxia.Fixtures.Model;
using Luxia.Patch.Rules;

namespace Luxia.Scenes.Model;

/// <summary>Scènes d'un projet, enregistrées dans <c>scènes.json</c> (doc 50).</summary>
public sealed record SceneSet
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Catégorie réservée au contenu généré par une IA de conception (GEN-133).</summary>
    public const string AiCategory = "Proposé par IA";

    /// <summary>Scène « Plein feu » du contenu par défaut (MOT-042), identifiant fixe.</summary>
    public static readonly Guid FullOnSceneId = new("7c1a0002-0000-4000-8000-000000000001");

    /// <summary>Scènes, dans l'ordre d'affichage.</summary>
    public IReadOnlyList<Scene> Scenes { get; init; } = [];

    /// <summary>
    /// Contenu d'un nouveau projet (MOT-042) : la scène « Plein feu » (tous les appareils à 100 %) dans la couche
    /// Intensité, pour séparer intensité et couleurs dès le départ.
    /// </summary>
    public static SceneSet Default() => new()
    {
        Scenes =
        [
            new Scene
            {
                Id = FullOnSceneId,
                Name = "Plein feu",
                Color = "#E3B341",
                LayerId = LayerSet.IntensityLayerId,
                Steps =
                [
                    new SceneStep
                    {
                        Values =
                        [
                            new SceneValue
                            {
                                Target = new ValueTarget { Auto = new AutoSelectionTarget(AutoSelectionKind.AllFixtures) },
                                Attribute = AttributeKind.Intensity,
                                Level = 1,
                            },
                        ],
                    },
                ],
            },
        ],
    };
}
