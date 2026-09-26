namespace Luxia.Engine.Model;

/// <summary>État d'une lecture de scène (doc 15 §2).</summary>
public enum PlaybackState
{
    /// <summary>Fondu d'entrée en cours.</summary>
    FadingIn,

    /// <summary>En cours (y compris maintenue sur sa dernière étape).</summary>
    Running,

    /// <summary>Fondu de sortie en cours.</summary>
    FadingOut,

    /// <summary>Terminée (retirée au tick suivant).</summary>
    Done,
}
