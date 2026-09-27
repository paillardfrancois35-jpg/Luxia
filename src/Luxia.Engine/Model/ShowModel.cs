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

    /// <summary>Crée un modèle.</summary>
    /// <param name="parameters">Paramètres (un par attribut d'appareil).</param>
    /// <param name="layers">Couches (au moins une si des scènes doivent jouer).</param>
    /// <param name="scenes">Scènes.</param>
    /// <param name="aliases">Autres appareils qui partagent les paramètres d'un appareil (jumeaux, MOT-092) : alias → appareil de référence.</param>
    /// <param name="safety">Limites de sûreté (doc 15 §9) ; aucune par défaut.</param>
    public ShowModel(
        IReadOnlyList<RigParameter> parameters,
        IReadOnlyList<EngineLayer>? layers = null,
        IReadOnlyList<EngineScene>? scenes = null,
        IReadOnlyDictionary<Guid, Guid>? aliases = null,
        SafetyModel? safety = null)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        Parameters = parameters;
        Layers = layers ?? [];
        Scenes = scenes ?? [];
        Aliases = aliases ?? new Dictionary<Guid, Guid>();
        Safety = safety ?? SafetyModel.None;

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

    /// <summary>Indice d'un paramètre, ou -1.</summary>
    public int IndexOf(Guid fixtureId, string channelKey) =>
        _parameterIndex.TryGetValue((fixtureId, channelKey), out var index) ? index : -1;

    /// <summary>Scène par identifiant.</summary>
    public EngineScene? Scene(Guid id) => _sceneIndex.TryGetValue(id, out var index) ? Scenes[index] : null;

    /// <summary>Couche par identifiant.</summary>
    public EngineLayer? Layer(Guid id) => _layerIndex.TryGetValue(id, out var index) ? Layers[index] : null;
}
