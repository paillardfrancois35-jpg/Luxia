using Luxia.Fixtures.Model;
using Luxia.Patch.Model;
using Luxia.Patch.Rules;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Compilation;

/// <summary>
/// Patch résolu pour la compilation et le programmeur : appareils avec leur modèle et leur mode, jumeaux, absents,
/// et développement d'une cible de valeur en membres ordonnés (SCN-007).
/// </summary>
public sealed class PatchContext
{
    private readonly Dictionary<Guid, FixtureInfo> _byId = [];
    private readonly Installation _installation;
    private readonly Func<Guid, FixtureType?> _typeOf;

    /// <summary>Résout le patch.</summary>
    public PatchContext(Installation installation, VenueSet venues, Func<Guid, FixtureType?> typeOf)
    {
        ArgumentNullException.ThrowIfNull(installation);
        ArgumentNullException.ThrowIfNull(venues);
        ArgumentNullException.ThrowIfNull(typeOf);
        _installation = installation;
        _typeOf = typeOf;
        var venue = venues.Active;
        var problems = new List<string>();
        var twinReference = new Dictionary<Guid, Guid>();
        var fixtures = new List<FixtureInfo>();
        foreach (var fixture in installation.Fixtures.OrderBy(f => f.Universe).ThenBy(f => f.Address))
        {
            var type = typeOf(fixture.FixtureTypeId);
            var mode = type?.Modes.FirstOrDefault(m => m.Name == fixture.ModeName);
            if (type is null || mode is null)
            {
                problems.Add(type is null
                    ? $"« {fixture.Name} » : modèle introuvable dans la copie du projet"
                    : $"« {fixture.Name} » : mode « {fixture.ModeName} » introuvable dans le modèle {type.DisplayName}");
                continue;
            }

            var reference = fixture.Id;
            if (fixture.TwinGroupId is { } group)
            {
                // Le premier jumeau (ordre du patch) porte les paramètres ; les suivants les partagent (MOT-092).
                if (!twinReference.TryGetValue(group, out reference))
                {
                    reference = fixture.Id;
                    twinReference[group] = reference;
                }
            }

            var absent = venue.PlacementOf(fixture.Id)?.Absent ?? false;
            var info = new FixtureInfo(fixture, type, mode, reference, absent);
            fixtures.Add(info);
            _byId[fixture.Id] = info;
        }

        Fixtures = fixtures;
        Problems = problems;
        Venue = venue;
        VenueKey = Rules.VenuePalettes.Key(venue);
    }

    /// <summary>Lieu actif.</summary>
    public Venue Venue { get; }

    /// <summary>Clé du lieu actif pour les palettes de position (<c>null</c> = « Générique », PAL-004).</summary>
    public Guid? VenueKey { get; }

    /// <summary>Appareils résolus, dans l'ordre du patch (univers, adresse).</summary>
    public IReadOnlyList<FixtureInfo> Fixtures { get; }

    /// <summary>Appareils non résolus (modèle ou mode introuvable) : ignorés.</summary>
    public IReadOnlyList<string> Problems { get; }

    /// <summary>Appareil résolu par identifiant.</summary>
    public FixtureInfo? Find(Guid fixtureId) => _byId.GetValueOrDefault(fixtureId);

    /// <summary>
    /// Membres d'une cible, dans l'ordre (sélection manuelle : son ordre ; automatique : ordre du patch).
    /// Seuls les appareils <b>présents</b> d'une sélection en font partie (SCN-007) ; un appareil visé directement
    /// est gardé même absent (il sera de toute façon émis à 0, MOT-091).
    /// </summary>
    /// <param name="target">Cible.</param>
    /// <param name="problem">Pourquoi la cible ne donne aucun membre, le cas échéant.</param>
    public IReadOnlyList<(FixtureInfo Fixture, int Cell)> Members(ValueTarget target, out string? problem)
    {
        ArgumentNullException.ThrowIfNull(target);
        problem = null;
        if (target.FixtureId is { } fixtureId)
        {
            if (Find(fixtureId) is { } info)
            {
                return [(info, target.Cell)];
            }

            problem = "appareil introuvable dans le patch";
            return [];
        }

        if (target.SelectionId is { } selectionId)
        {
            var selection = _installation.Selections.FirstOrDefault(s => s.Id == selectionId);
            if (selection is null)
            {
                problem = "sélection introuvable";
                return [];
            }

            return [.. selection.Items
                .Select(item => (Info: Find(item.FixtureId), item.Cell))
                .Where(m => m.Info is { Absent: false })
                .Select(m => (m.Info!, m.Cell))];
        }

        if (target.Auto is { } auto)
        {
            var present = Fixtures.Where(f => !f.Absent).Select(f => f.Fixture).ToList();
            var selection = AutoSelections.Build(present, f => _typeOf(f.FixtureTypeId))
                .FirstOrDefault(s => s.Kind == auto.Kind
                    && (auto.Kind != AutoSelectionKind.ByCategory || s.Category == auto.Category)
                    && (auto.Kind != AutoSelectionKind.ByModel || s.ModelDisplayName == auto.Model));
            // SCN-007 : une cellule précisée vaut pour chaque membre (rangée 2 de tous les UV) ; 0 = l'appareil entier.
            return selection is null ? [] : [.. selection.Items.Select(f => (Find(f.Id)!, target.Cell))];
        }

        problem = "cible vide (ni appareil, ni sélection)";
        return [];
    }
}
