namespace Luxia.Engine.Model;

/// <summary>Scène compilée, prête à être jouée par le moteur (doc 16 §2, doc 15 §4).</summary>
public sealed record EngineScene
{
    /// <summary>Identifiant stable (GEN-052), référencé par les commandes.</summary>
    public required Guid Id { get; init; }

    /// <summary>Nom (journal, explication de la valeur).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Couche d'appartenance.</summary>
    public Guid LayerId { get; init; }

    /// <summary>Mode de boucle (MOT-013).</summary>
    public LoopMode Loop { get; init; } = LoopMode.Infinite;

    /// <summary>Nombre de passages pour <see cref="LoopMode.Count"/>.</summary>
    public int LoopCount { get; init; } = 1;

    /// <summary>Fin de scène (MOT-014).</summary>
    public EndMode End { get; init; } = EndMode.Stop;

    /// <summary>Scène enchaînée pour <see cref="EndMode.Chain"/>.</summary>
    public Guid? ChainSceneId { get; init; }

    /// <summary>Fondu d'entrée par défaut ; <c>null</c> = fondu de la première étape.</summary>
    public Duration? FadeIn { get; init; }

    /// <summary>Fondu de sortie par défaut ; <c>null</c> = arrêt immédiat.</summary>
    public Duration? FadeOut { get; init; }

    /// <summary>Vitesse de lecture (multiplicateur 0,1 à 10, MOT-015).</summary>
    public double Speed { get; init; } = 1;

    /// <summary>Étapes (au moins une pour être jouable).</summary>
    public IReadOnlyList<EngineStep> Steps { get; init; } = [];
}
