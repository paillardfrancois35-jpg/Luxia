namespace Luxia.Show.Model;

/// <summary>
/// Séquence (doc 20 §2, SHOW-001) : des scènes et des actions posées sur des pistes, en mesures. Elle se rejoue sur n'importe
/// quel morceau en suivant l'horloge musicale (« Montée 16 mesures »).
/// </summary>
public sealed record Sequence
{
    /// <summary>Identifiant stable (GEN-052).</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nom.</summary>
    public required string Name { get; init; }

    /// <summary>Couleur d'affichage « #RRGGBB ».</summary>
    public string Color { get; init; } = "#3FB950";

    /// <summary>Catégorie (« Phase P8 », « Proposé par IA »…).</summary>
    public string? Category { get; init; }

    /// <summary>Notes.</summary>
    public string? Notes { get; init; }

    /// <summary>Longueur en mesures (mesure à 4 temps, D39).</summary>
    public double Bars { get; init; } = 8;

    /// <summary>Ce qui se passe à la fin : s'arrêter ou reprendre au début (SHOW-005).</summary>
    public SequenceEnd End { get; init; } = SequenceEnd.Stop;

    /// <summary>Instant musical attendu avant de démarrer (SHOW-005) ; la prochaine mesure par défaut.</summary>
    public ShowQuantize Quantize { get; init; } = ShowQuantize.Bar;

    /// <summary>Vitesse relative (SHOW-008) : 0,5 = demi-temps, 1 = normale, 2 = double temps.</summary>
    public double Speed { get; init; } = 1;

    /// <summary>Pistes, dans l'ordre d'affichage.</summary>
    public IReadOnlyList<SequenceTrack> Tracks { get; init; } = [];
}

/// <summary>Fin d'une séquence.</summary>
public enum SequenceEnd
{
    /// <summary>La séquence s'arrête (une seule fois).</summary>
    Stop,

    /// <summary>La séquence reprend au début (boucle).</summary>
    Loop,
}

/// <summary>
/// Piste d'une séquence : une couche (on n'y pose que des scènes de cette couche, D39) ou, sans couche, une piste d'actions
/// (niveaux, fumée, flash, noir).
/// </summary>
public sealed record SequenceTrack
{
    /// <summary>Couche de la piste ; <c>null</c> = piste d'actions.</summary>
    public Guid? LayerId { get; init; }

    /// <summary>Blocs de la piste.</summary>
    public IReadOnlyList<SequenceBlock> Blocks { get; init; } = [];
}

/// <summary>
/// Bloc d'une piste (SHOW-002 à SHOW-004) : une scène ou une action, de <see cref="Start"/> à <see cref="Start"/> +
/// <see cref="Length"/>, en mesures depuis le début de la séquence (0 = mesure 1, 0,25 = un temps).
/// </summary>
public sealed record SequenceBlock
{
    /// <summary>Début, en mesures depuis le début de la séquence.</summary>
    public double Start { get; init; }

    /// <summary>Durée, en mesures.</summary>
    public double Length { get; init; } = 1;

    /// <summary>Scène lancée au début du bloc (piste de couche).</summary>
    public Guid? SceneId { get; init; }

    /// <summary>Action du bloc (piste d'actions).</summary>
    public BlockAction? Action { get; init; }

    /// <summary>Ce que devient la scène à la fin du bloc.</summary>
    public BlockEnd End { get; init; } = BlockEnd.Stop;
}

/// <summary>Fin d'un bloc de scène.</summary>
public enum BlockEnd
{
    /// <summary>La scène s'arrête (sauf si le bloc suivant de la piste prend le relais au même instant).</summary>
    Stop,

    /// <summary>La scène continue après le bloc (« lancer et laisser »).</summary>
    Keep,
}

/// <summary>Action d'un bloc de la piste d'actions (SHOW-004).</summary>
public sealed record BlockAction
{
    /// <summary>Nature.</summary>
    public BlockActionKind Kind { get; init; }

    /// <summary>Couche visée (<see cref="BlockActionKind.LayerLevel"/>).</summary>
    public Guid? LayerId { get; init; }

    /// <summary>Scène visée (<see cref="BlockActionKind.Flash"/>).</summary>
    public Guid? SceneId { get; init; }

    /// <summary>Niveau au début du bloc (0 à 1) ; <c>null</c> = le niveau courant.</summary>
    public double? From { get; init; }

    /// <summary>Niveau à la fin du bloc (0 à 1) : rampe de <see cref="From"/> à cette valeur, puis le niveau reste.</summary>
    public double To { get; init; } = 1;
}

/// <summary>Nature d'une action de séquence.</summary>
public enum BlockActionKind
{
    /// <summary>Niveau d'une couche, en rampe sur la durée du bloc.</summary>
    LayerLevel,

    /// <summary>Grand Master, en rampe sur la durée du bloc.</summary>
    GrandMaster,

    /// <summary>Rafale de fumée pendant le bloc (limiteur de fumée toujours actif).</summary>
    Smoke,

    /// <summary>Flash d'une scène pendant le bloc.</summary>
    Flash,

    /// <summary>Noir (blackout) pendant le bloc.</summary>
    Blackout,
}
