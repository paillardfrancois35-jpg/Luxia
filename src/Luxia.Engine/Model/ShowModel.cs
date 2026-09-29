using System.Collections.Frozen;

namespace Luxia.Engine.Model;

/// <summary>
/// Tout ce que le moteur doit connaître du projet, compilé hors du fil du moteur (D26) : paramètres des appareils
/// patchés, couches et scènes. Chargé par <see cref="RenderEngine.LoadShow"/> et appliqué au tick suivant ;
/// les lectures en cours continuent avec la nouvelle version (modification d'une palette, PAL-005).
/// </summary>
public sealed class ShowModel
{
    private readonly FrozenDictionary<(Guid, string), int> _parameterIndex;
    private readonly FrozenDictionary<Guid, int> _sceneIndex;
    private readonly FrozenDictionary<Guid, int> _layerIndex;
    private readonly FrozenDictionary<Guid, int> _groupIndex;

    /// <summary>Crée un modèle.</summary>
    /// <param name="parameters">Paramètres (un par attribut d'appareil).</param>
    /// <param name="layers">Couches (au moins une si des scènes doivent jouer).</param>
    /// <param name="scenes">Scènes.</param>
    /// <param name="aliases">Autres appareils qui partagent les paramètres d'un appareil (jumeaux, MOT-092) : alias → appareil de référence.</param>
    /// <param name="safety">Limites de sûreté (doc 15 §9) ; aucune par défaut.</param>
    /// <param name="colorGroups">Triplets rouge / vert / bleu d'une même cellule, pour le fondu par la teinte (MOT-054).</param>
    /// <param name="dimmerGroups">Arbre des groupes de dimmers (ERG-037) ; un parent précède toujours ses enfants.</param>
    /// <param name="parameterGroups">Groupe de chaque paramètre (rang dans <paramref name="dimmerGroups"/>, -1 = aucun) ; vide = aucun.</param>
    public ShowModel(
        IReadOnlyList<RigParameter> parameters,
        IReadOnlyList<EngineLayer>? layers = null,
        IReadOnlyList<EngineScene>? scenes = null,
        IReadOnlyDictionary<Guid, Guid>? aliases = null,
        SafetyModel? safety = null,
        IReadOnlyList<ColorGroup>? colorGroups = null,
        IReadOnlyList<DimmerGroup>? dimmerGroups = null,
        IReadOnlyList<int>? parameterGroups = null)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        Parameters = parameters;
        Layers = layers ?? [];
        Scenes = scenes ?? [];
        Aliases = aliases ?? new Dictionary<Guid, Guid>();
        Safety = safety ?? SafetyModel.None;
        ColorGroups = colorGroups ?? [];
        DimmerGroups = dimmerGroups ?? [];
        ParameterGroups = parameterGroups is { Count: > 0 } ? parameterGroups : [.. Enumerable.Repeat(-1, parameters.Count)];
        if (ParameterGroups.Count != parameters.Count)
        {
            throw new ArgumentException("Un groupe (ou -1) est attendu pour chaque paramètre.", nameof(parameterGroups));
        }

        for (var i = 0; i < DimmerGroups.Count; i++)
        {
            if (DimmerGroups[i].Parent >= i || DimmerGroups[i].Parent < -1)
            {
                throw new ArgumentException($"Le parent du groupe « {DimmerGroups[i].Name} » doit le précéder dans l'arbre.", nameof(dimmerGroups));
            }
        }

        if (ParameterGroups.Any(g => g < -1 || g >= DimmerGroups.Count))
        {
            throw new ArgumentException("Groupe de paramètre inconnu.", nameof(parameterGroups));
        }

        var index = new Dictionary<(Guid, string), int>();
        for (var i = 0; i < parameters.Count; i++)
        {
            index[(parameters[i].FixtureId, parameters[i].ChannelKey)] = i;
        }

        foreach (var (alias, target) in Aliases)
        {
            for (var i = 0; i < parameters.Count; i++)
            {
                if (parameters[i].FixtureId == target)
                {
                    index[(alias, parameters[i].ChannelKey)] = i;
                }
            }
        }

        _parameterIndex = index.ToFrozenDictionary();
        _sceneIndex = Scenes.Select((s, i) => (s.Id, i)).ToFrozenDictionary(x => x.Id, x => x.i);
        _layerIndex = Layers.Select((l, i) => (l.Id, i)).ToFrozenDictionary(x => x.Id, x => x.i);
        _groupIndex = DimmerGroups.Select((g, i) => (g.Id, i)).ToFrozenDictionary(x => x.Id, x => x.i);
    }

    /// <summary>Modèle vide : aucun appareil, aucune scène (projet non ouvert, P0-P3).</summary>
    public static ShowModel Empty { get; } = new([]);

    /// <summary>Paramètres, dans l'ordre de calcul.</summary>
    public IReadOnlyList<RigParameter> Parameters { get; }

    /// <summary>Couches.</summary>
    public IReadOnlyList<EngineLayer> Layers { get; }

    /// <summary>Scènes.</summary>
    public IReadOnlyList<EngineScene> Scenes { get; }

    /// <summary>Jumeaux : appareil alias → appareil dont il partage les paramètres.</summary>
    public IReadOnlyDictionary<Guid, Guid> Aliases { get; }

    /// <summary>Limites de sûreté : réglages, canaux de strobe et de fumée, zones interdites du lieu actif.</summary>
    public SafetyModel Safety { get; }

    /// <summary>Triplets rouge / vert / bleu d'une même cellule (MOT-054).</summary>
    public IReadOnlyList<ColorGroup> ColorGroups { get; }

    /// <summary>Arbre des groupes de dimmers (ERG-037) : un parent précède toujours ses enfants.</summary>
    public IReadOnlyList<DimmerGroup> DimmerGroups { get; }

    /// <summary>Groupe de chaque paramètre (rang dans <see cref="DimmerGroups"/>, -1 = aucun), aligné sur <see cref="Parameters"/>.</summary>
    public IReadOnlyList<int> ParameterGroups { get; }

    /// <summary>Rang d'un groupe de dimmers dans <see cref="DimmerGroups"/> (et dans les niveaux d'un instantané), ou -1.</summary>
    public int IndexOfDimmerGroup(Guid id) => _groupIndex.TryGetValue(id, out var index) ? index : -1;

    /// <summary>Indice d'un paramètre, ou -1.</summary>
    public int IndexOf(Guid fixtureId, string channelKey) =>
        _parameterIndex.TryGetValue((fixtureId, channelKey), out var index) ? index : -1;

    /// <summary>Scène par identifiant.</summary>
    public EngineScene? Scene(Guid id) => _sceneIndex.TryGetValue(id, out var index) ? Scenes[index] : null;

    /// <summary>Couche par identifiant.</summary>
    public EngineLayer? Layer(Guid id) => _layerIndex.TryGetValue(id, out var index) ? Layers[index] : null;

    /// <summary>Rang d'une couche dans <see cref="Layers"/> (et dans les masters d'un instantané), ou -1.</summary>
    public int IndexOfLayer(Guid id) => _layerIndex.TryGetValue(id, out var index) ? index : -1;
}
