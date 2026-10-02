namespace Luxia.Engine.Timing;

/// <summary>Événements musicaux arrivés depuis la lecture précédente (D38) : un drapeau par nature.</summary>
[Flags]
public enum MusicCues
{
    /// <summary>Aucun.</summary>
    None = 0,

    /// <summary>Drop.</summary>
    Drop = 1,

    /// <summary>Break.</summary>
    Break = 2,

    /// <summary>Montée.</summary>
    BuildUp = 4,

    /// <summary>Plus aucun son.</summary>
    Silence = 8,

    /// <summary>Le son reprend après un silence.</summary>
    Resumed = 16,

    /// <summary>Morceau changé (avant P9 : reprise après un silence ou saut de tempo, D38).</summary>
    SongChanged = 32,
}
