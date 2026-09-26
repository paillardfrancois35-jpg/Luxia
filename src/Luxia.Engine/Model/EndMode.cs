namespace Luxia.Engine.Model;

/// <summary>Fin d'une scène jouée une fois ou N fois (MOT-014).</summary>
public enum EndMode
{
    /// <summary>S'arrête (fondu de sortie).</summary>
    Stop,

    /// <summary>Reste sur la dernière étape.</summary>
    Hold,

    /// <summary>Enchaîne sur une autre scène, dans la même couche.</summary>
    Chain,
}
