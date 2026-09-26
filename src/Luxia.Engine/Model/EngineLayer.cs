namespace Luxia.Engine.Model;

/// <summary>Couche compilée (doc 17 §1.2) : priorité, exclusivité, master, mode d'intensité.</summary>
public sealed record EngineLayer
{
    /// <summary>Identifiant stable.</summary>
    public required Guid Id { get; init; }

    /// <summary>Nom (journal, explication de la valeur).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Priorité : ordre d'empilement pour la fusion LTP (la plus haute l'emporte).</summary>
    public int Priority { get; init; }

    /// <summary>Couche exclusive : lancer une scène remplace celle qui jouait (fondu croisé, MOT-030).</summary>
    public bool Exclusive { get; init; } = true;

    /// <summary>Master initial (0 à 1, MOT-033) ; modifiable en direct par CMD-013.</summary>
    public double Master { get; init; } = 1;

    /// <summary>Mode d'intensité (MOT-031).</summary>
    public IntensityMode IntensityMode { get; init; } = IntensityMode.Htp;

    /// <summary>Le master agit aussi sur les autres attributs (poids LTP multiplié, MOT-033).</summary>
    public bool MasterOnAllAttributes { get; init; }

    /// <summary>Fondu croisé par défaut entre deux scènes de la couche.</summary>
    public Duration CrossFade { get; init; } = Duration.FromSeconds(0.5);

    /// <summary>Type : normale, ou Flash (scènes actives tant que la commande est maintenue, COU-005).</summary>
    public LayerKind Kind { get; init; } = LayerKind.Normal;

    /// <summary>Couche protégée de « Tout arrêter » (Ambiance par défaut, COU-007).</summary>
    public bool KeepOnStopAll { get; init; }

    /// <summary>Scène jouée quand aucune autre ne joue dans la couche (COU-009).</summary>
    public Guid? RestSceneId { get; init; }
}
