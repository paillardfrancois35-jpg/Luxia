namespace Luxia.Messaging.Events;

/// <summary>Nature d'un événement musical (EVT-022, EVT-023).</summary>
public enum MusicEventKind
{
    /// <summary>Plus aucun son (AUD-005).</summary>
    Silence,

    /// <summary>Le son reprend (AUD-005).</summary>
    Resumed,

    /// <summary>Chute durable de l'énergie (AUD-062).</summary>
    Break,

    /// <summary>Retour brutal de l'énergie après un break ou une montée (AUD-062).</summary>
    Drop,

    /// <summary>Énergie croissante sur plusieurs mesures (AUD-063).</summary>
    BuildUp,

    /// <summary>Le niveau d'énergie (0 Calme à 3 Explosif) a changé (AUD-061).</summary>
    EnergyChanged,
}

/// <summary>Événement musical issu de l'écoute : silence, break, drop, montée, niveau d'énergie (EVT-022, EVT-023).</summary>
/// <param name="Kind">Nature.</param>
/// <param name="EnergyLevel">Niveau d'énergie : 0 Calme, 1 Groove, 2 Énergique, 3 Explosif.</param>
/// <param name="Energy">Énergie (0 à 1).</param>
/// <param name="At">Instant (horloge du moteur).</param>
public sealed record MusicEvent(MusicEventKind Kind, int EnergyLevel, double Energy, TimeSpan At);
