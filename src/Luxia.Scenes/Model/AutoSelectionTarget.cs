using Luxia.Fixtures.Model;
using Luxia.Patch.Rules;

namespace Luxia.Scenes.Model;

/// <summary>
/// Référence à une sélection automatique (INST-031), recalculée à chaque compilation depuis le patch :
/// un appareil ajouté plus tard en fait partie sans rien modifier (SCN-007).
/// </summary>
/// <param name="Kind">Genre : tous, par catégorie, par modèle.</param>
/// <param name="Category">Catégorie visée (genre « par catégorie »).</param>
/// <param name="Model">Modèle visé, « Fabricant Modèle » (genre « par modèle »).</param>
public sealed record AutoSelectionTarget(AutoSelectionKind Kind, FixtureCategory? Category = null, string? Model = null);
