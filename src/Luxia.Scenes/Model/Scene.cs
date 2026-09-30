using Luxia.Engine.Model;

namespace Luxia.Scenes.Model;

/// <summary>
/// Scène (doc 16 §2) : suite d'une ou plusieurs étapes. Une étape en boucle = un état fixe (« cue »),
/// plusieurs = un « chase » : un seul concept.
/// </summary>
public sealed record Scene
{
    /// <summary>Identifiant stable (GEN-052).</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nom.</summary>
    public required string Name { get; init; }

    /// <summary>Couleur d'affichage « #RRGGBB » (GEN-106).</summary>
    public string Color { get; init; } = "#58A6FF";

    /// <summary>Icône facultative (caractère ou nom court, GEN-106).</summary>
    public string? Icon { get; init; }

    /// <summary>Catégorie (SCN-012 ; « Phase P4 », « Proposé par IA »…).</summary>
    public string? Category { get; init; }

    /// <summary>Notes.</summary>
    public string? Notes { get; init; }

    /// <summary>Visible en Live (l'« œil », SCN-009).</summary>
    public bool VisibleInLive { get; init; } = true;

    /// <summary>Couche d'appartenance (doc 17).</summary>
    public Guid LayerId { get; init; }

    /// <summary>Boucle (MOT-013).</summary>
    public LoopMode Loop { get; init; } = LoopMode.Infinite;

    /// <summary>Nombre de passages pour une boucle « N fois ».</summary>
    public int LoopCount { get; init; } = 1;

    /// <summary>Fin de scène (MOT-014).</summary>
    public EndMode End { get; init; } = EndMode.Stop;

    /// <summary>Scène enchaînée (fin « enchaîner »).</summary>
    public Guid? ChainSceneId { get; init; }

    /// <summary>Fondu d'entrée par défaut ; <c>null</c> = fondu de la première étape.</summary>
    public Duration? FadeIn { get; init; }

    /// <summary>Fondu de sortie par défaut ; <c>null</c> = arrêt immédiat.</summary>
    public Duration? FadeOut { get; init; }

    /// <summary>Vitesse (multiplicateur 0,1 à 10, MOT-015).</summary>
    public double Speed { get; init; } = 1;

    /// <summary>Événement qui fait avancer d'étape (MOT-017, SCN-050) ; par défaut, la durée de l'étape.</summary>
    public StepAdvanceMode Advance { get; init; } = StepAdvanceMode.Duration;

    /// <summary>Nombre d'événements entre deux étapes (1 ou plus).</summary>
    public int AdvanceEvery { get; init; } = 1;

    /// <summary>Instant musical attendu avant de démarrer (MOT-018).</summary>
    public LaunchQuantize Quantize { get; init; } = LaunchQuantize.None;

    /// <summary>La vitesse de la scène suit l'énergie de la musique (SCN-051) : de 0,6× (calme) à 1,4× (explosif).</summary>
    public bool EnergySpeed { get; init; }

    /// <summary>Tempo propre de la scène (MOT-020) ; <c>null</c> = elle suit l'horloge principale.</summary>
    public double? OwnBpm { get; init; }

    /// <summary>Étapes (au moins une, SCN-002).</summary>
    public IReadOnlyList<SceneStep> Steps { get; init; } = [new SceneStep()];
}
