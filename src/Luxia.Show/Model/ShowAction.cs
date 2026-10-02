namespace Luxia.Show.Model;

/// <summary>
/// Action d'une étape de show (doc 20 §3.1, SHOW-021). Sa nature dit quand elle agit : <b>continue</b> (tant que l'étape est
/// active : <see cref="ShowActionKind.Play"/>, <see cref="ShowActionKind.PlaySequence"/>), <b>mémorisée</b> (à l'activation, et
/// elle reste : lancer, arrêter, niveau, vitesse) ou <b>impulsionnelle</b> (une fois à l'activation : flash, fumée, noir court,
/// variable).
/// </summary>
public sealed record ShowAction
{
    /// <summary>Nature.</summary>
    public ShowActionKind Kind { get; init; }

    /// <summary>Scène visée.</summary>
    public Guid? SceneId { get; init; }

    /// <summary>Séquence visée.</summary>
    public Guid? SequenceId { get; init; }

    /// <summary>Couche visée.</summary>
    public Guid? LayerId { get; init; }

    /// <summary>Niveau (0 à 1), facteur de vitesse, ou valeur d'une variable.</summary>
    public double Value { get; init; } = 1;

    /// <summary>Durée en secondes d'un flash, d'une rafale de fumée ou d'un noir court.</summary>
    public double Seconds { get; init; } = 1;

    /// <summary>Variable visée.</summary>
    public string? Variable { get; init; }

    /// <summary>Opération sur la variable.</summary>
    public VariableOperation Operation { get; init; } = VariableOperation.Add;
}

/// <summary>Nature d'une action de show.</summary>
public enum ShowActionKind
{
    /// <summary>Continue : joue une scène tant que l'étape est active (pas de coupure si l'étape suivante la rejoue, R5).</summary>
    Play,

    /// <summary>Continue : joue une séquence tant que l'étape est active.</summary>
    PlaySequence,

    /// <summary>Mémorisée : lance une scène et la laisse jouer.</summary>
    Launch,

    /// <summary>Mémorisée : lance une séquence et la laisse jouer.</summary>
    LaunchSequence,

    /// <summary>Mémorisée : arrête une scène.</summary>
    Stop,

    /// <summary>Mémorisée : arrête une séquence.</summary>
    StopSequence,

    /// <summary>Mémorisée : arrête toutes les scènes d'une couche.</summary>
    StopLayer,

    /// <summary>Mémorisée : règle le niveau d'une couche.</summary>
    LayerLevel,

    /// <summary>Mémorisée : règle la vitesse d'une scène (multiplicateur).</summary>
    Speed,

    /// <summary>Impulsionnelle : flash d'une scène pendant <see cref="ShowAction.Seconds"/>.</summary>
    Flash,

    /// <summary>Impulsionnelle : rafale de fumée (limiteur toujours actif).</summary>
    Smoke,

    /// <summary>Impulsionnelle : noir pendant <see cref="ShowAction.Seconds"/>.</summary>
    Blackout,

    /// <summary>Impulsionnelle : modifie une variable du show (SHOW-029).</summary>
    Variable,
}

/// <summary>Opération sur une variable.</summary>
public enum VariableOperation
{
    /// <summary>Ajoute la valeur.</summary>
    Add,

    /// <summary>Remplace par la valeur.</summary>
    Set,
}
