namespace Luxia.Engine;

/// <summary>Action de fin de scène demandée au moteur par une lecture (MOT-014).</summary>
internal enum PlaybackAdvance
{
    /// <summary>Rien à faire.</summary>
    None,

    /// <summary>Fin atteinte : fondu de sortie.</summary>
    Stop,

    /// <summary>Fin atteinte : enchaîner sur la scène suivante, dans la même couche.</summary>
    Chain,
}
