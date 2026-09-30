namespace Luxia.Audio;

/// <summary>Niveau d'énergie discret, avec hystérésis (AUD-061).</summary>
public enum EnergyLevel
{
    /// <summary>Calme : pause, ballade, introduction.</summary>
    Calm,

    /// <summary>Groove : rythme établi, énergie moyenne.</summary>
    Groove,

    /// <summary>Énergique.</summary>
    Energetic,

    /// <summary>Explosif : pic d'énergie (refrain, drop).</summary>
    Explosive,
}

/// <summary>Tendance de l'énergie (AUD-064).</summary>
public enum EnergyTrend
{
    /// <summary>Stable.</summary>
    Steady,

    /// <summary>Monte.</summary>
    Rising,

    /// <summary>Descend.</summary>
    Falling,
}

/// <summary>Nature d'un événement musical (doc 19 §5).</summary>
public enum AudioEventKind
{
    /// <summary>Plus aucun son (AUD-005).</summary>
    Silence,

    /// <summary>Le son reprend après un silence (AUD-005).</summary>
    Resumed,

    /// <summary>Chute durable de l'énergie et des basses (AUD-062).</summary>
    Break,

    /// <summary>Retour brutal de l'énergie et des basses après un break ou une montée (AUD-062).</summary>
    Drop,

    /// <summary>Énergie et densité rythmique croissantes sur plusieurs mesures (AUD-063).</summary>
    BuildUp,

    /// <summary>Le niveau d'énergie a changé (AUD-061).</summary>
    EnergyChanged,
}

/// <summary>Événement musical horodaté (EVT-022, EVT-023).</summary>
/// <param name="Kind">Nature.</param>
/// <param name="Seconds">Instant depuis le début de l'écoute, en secondes de son analysé.</param>
/// <param name="Level">Niveau d'énergie à cet instant.</param>
/// <param name="Energy">Énergie (0 à 1) à cet instant.</param>
public sealed record AudioEvent(AudioEventKind Kind, double Seconds, EnergyLevel Level, double Energy);
