namespace Luxia.Messaging.Commands;

/// <summary>Source du tempo de l'horloge musicale (CMD-041, doc 19 §3.1).</summary>
public enum TempoSourceKind
{
    /// <summary>BPM saisi ; phase libre, recalable.</summary>
    Fixed,

    /// <summary>Tempo tapé par l'utilisateur (AUD-025).</summary>
    Tap,

    /// <summary>Tempo estimé sur le son (AUD-020).</summary>
    Audio,
}
