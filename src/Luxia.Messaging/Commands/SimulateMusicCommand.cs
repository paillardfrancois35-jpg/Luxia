namespace Luxia.Messaging.Commands;

/// <summary>Événement musical provoqué en mode simulation (CMD-053).</summary>
public enum SimulatedCue
{
    /// <summary>Aucun événement.</summary>
    None,

    /// <summary>Drop.</summary>
    Drop,

    /// <summary>Break.</summary>
    Break,

    /// <summary>Montée.</summary>
    BuildUp,

    /// <summary>Silence.</summary>
    Silence,

    /// <summary>Le son reprend.</summary>
    Resumed,

    /// <summary>Morceau changé.</summary>
    SongChanged,
}

/// <summary>
/// CMD-053 <c>SimulerMusique</c> (mode simulation, SHOW-027, D38) : provoque un événement musical au tick suivant, impose
/// une énergie (0 à 1) ou la rend à l'écoute (<see cref="double.NaN"/>), et règle le style simulé (seule source de style avant
/// P9 ; <c>null</c> = inchangé, vide = aucun). Un champ laissé à sa valeur par défaut ne change rien.
/// </summary>
/// <param name="Origin">Origine.</param>
/// <param name="Cue">Événement à provoquer.</param>
/// <param name="Energy">Énergie imposée ; <c>null</c> = inchangée ; <see cref="double.NaN"/> = rendue à l'écoute.</param>
/// <param name="Style">Style simulé ; <c>null</c> = inchangé.</param>
public sealed record SimulateMusicCommand(CommandOrigin Origin, SimulatedCue Cue = SimulatedCue.None, double? Energy = null, string? Style = null) : Command(Origin);
