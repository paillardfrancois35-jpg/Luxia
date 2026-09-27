using Luxia.Fixtures.Model;
using Luxia.Patch.Model;

namespace Luxia.Patch.Rules;

/// <summary>Genre d'une sélection automatique (INST-031).</summary>
public enum AutoSelectionKind
{
    /// <summary>Tout le parc.</summary>
    AllFixtures,

    /// <summary>Un modèle (fabricant + modèle).</summary>
    ByModel,

    /// <summary>Une catégorie (tous les PAR, toutes les lyres…).</summary>
    ByCategory,
}

/// <summary>
/// Sélection automatique (INST-031) : « Tous », par modèle, par catégorie. Recalculée à chaque appel de
/// <see cref="AutoSelections.Build"/> à partir de l'installation courante : toujours à jour (aucune persistance nécessaire).
/// L'ordre des éléments suit l'ordre du patch (adresse). Le libellé affiché (français) est du ressort de
/// l'interface, qui connaît déjà les intitulés de <see cref="FixtureCategory"/>.
/// </summary>
/// <param name="Kind">Genre.</param>
/// <param name="Category">Catégorie visée si <see cref="Kind"/> = <see cref="AutoSelectionKind.ByCategory"/>.</param>
/// <param name="ModelDisplayName">Modèle visé si <see cref="Kind"/> = <see cref="AutoSelectionKind.ByModel"/>.</param>
/// <param name="Items">Appareils, dans l'ordre du patch.</param>
public sealed record AutoSelection(AutoSelectionKind Kind, FixtureCategory? Category, string? ModelDisplayName, IReadOnlyList<PatchedFixture> Items)
{
    /// <summary>Genre + intitulé de la catégorie ou du modèle, prêt à afficher.</summary>
    public string Title(Func<FixtureCategory, string> categoryLabel)
    {
        ArgumentNullException.ThrowIfNull(categoryLabel);
        return Kind switch
        {
            AutoSelectionKind.AllFixtures => "Tous",
            AutoSelectionKind.ByCategory => $"{(Category is FixtureCategory.LedBar or FixtureCategory.MovingHead or FixtureCategory.Smoke ? "Toutes" : "Tous")} les {categoryLabel(Category!.Value)}",
            AutoSelectionKind.ByModel => $"Tous les {ModelDisplayName}",
            _ => throw new NotSupportedException(),
        };
    }
}

/// <summary>Calcule les sélections automatiques (INST-031).</summary>
public static class AutoSelections
{
    /// <summary>
    /// Construit les sélections automatiques à partir des appareils présents (patch, adresse croissante).
    /// </summary>
    /// <param name="fixtures">Appareils patchés (sans les absents du lieu actif, si applicable — INST-052).</param>
    /// <param name="typeOf">Modèle utilisé par un appareil (copie du projet).</param>
    public static IReadOnlyList<AutoSelection> Build(IReadOnlyList<PatchedFixture> fixtures, Func<PatchedFixture, FixtureType?> typeOf)
    {
        ArgumentNullException.ThrowIfNull(fixtures);
        ArgumentNullException.ThrowIfNull(typeOf);
        var ordered = fixtures.OrderBy(f => f.Universe).ThenBy(f => f.Address).ToList();
        var result = new List<AutoSelection> { new(AutoSelectionKind.AllFixtures, null, null, ordered) };

        foreach (var category in ordered.Select(f => typeOf(f)?.Category).Where(c => c.HasValue).Select(c => c!.Value).Distinct().Order())
        {
            var items = ordered.Where(f => typeOf(f)?.Category == category).ToList();
            result.Add(new AutoSelection(AutoSelectionKind.ByCategory, category, null, items));
        }

        foreach (var model in ordered.Select(f => typeOf(f)?.DisplayName).Where(n => n is not null).Distinct().Order())
        {
            var items = ordered.Where(f => typeOf(f)?.DisplayName == model).ToList();
            result.Add(new AutoSelection(AutoSelectionKind.ByModel, null, model, items));
        }

        return result;
    }
}
