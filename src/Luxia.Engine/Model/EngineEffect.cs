namespace Luxia.Engine.Model;

/// <summary>
/// Effet généré compilé, attaché à une étape (EFF-001, doc 15 §7) : une forme parcourue en boucle, décalée d'un
/// membre à l'autre, appliquée à chaque paramètre de <see cref="Channels"/>.
/// </summary>
public sealed record EngineEffect
{
    /// <summary>Identifiant stable : un même effet présent dans deux étapes successives continue sans à-coup.</summary>
    public required Guid Id { get; init; }

    /// <summary>Nom (journal).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Forme.</summary>
    public EffectShape Shape { get; init; } = EffectShape.Sine;

    /// <summary>Durée d'un cycle, en secondes ou en temps musicaux (vitesse en Hz = 1 / durée, GEN-023).</summary>
    public Duration Period { get; init; } = Duration.FromSeconds(1);

    /// <summary>Rapport cyclique 0-1 des formes carré et impulsion.</summary>
    public double DutyCycle { get; init; } = 0.5;

    /// <summary>Sens.</summary>
    public EffectDirection Direction { get; init; } = EffectDirection.Forward;

    /// <summary>
    /// Mode relatif (MOT-061) : la forme s'ajoute à la valeur de base (celle de l'étape, sinon la valeur
    /// sous-jacente) ; absolu : elle la remplace, autour de <see cref="EffectChannel.Center"/>.
    /// </summary>
    public bool Relative { get; init; }

    /// <summary>Table : valeurs en escalier (alternance) plutôt qu'interpolées (dégradé, arc-en-ciel).</summary>
    public bool Stepped { get; init; }

    /// <summary>Graine des formes aléatoires propre à l'effet (combinée à celle de la session, MOT-004).</summary>
    public ulong Seed { get; init; }

    /// <summary>Paramètres pilotés, membre par membre.</summary>
    public IReadOnlyList<EffectChannel> Channels { get; init; } = [];
}
