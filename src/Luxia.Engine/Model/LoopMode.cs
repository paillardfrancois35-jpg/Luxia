namespace Luxia.Engine.Model;

/// <summary>Mode de boucle d'une scène (MOT-013).</summary>
public enum LoopMode
{
    /// <summary>Une seule fois, puis la fin de scène s'applique.</summary>
    Once,

    /// <summary>N passages, puis la fin de scène s'applique.</summary>
    Count,

    /// <summary>Sans fin.</summary>
    Infinite,

    /// <summary>Aller-retour sans fin (1, 2, 3, 2, 1, 2…).</summary>
    PingPong,

    /// <summary>Étape suivante tirée au hasard, sans répéter l'étape courante, sans fin.</summary>
    Random,
}
