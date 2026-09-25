namespace Dmx.Fixtures.Model;

/// <summary>Effet d'une plage de canal Strobe / Obturateur (doc 12 §2.5).</summary>
public enum StrobeEffect
{
    /// <summary>Faisceau fermé.</summary>
    Closed,

    /// <summary>Faisceau ouvert.</summary>
    Open,

    /// <summary>Clignotement régulier.</summary>
    Strobe,

    /// <summary>Pulsation.</summary>
    Pulse,

    /// <summary>Clignotement aléatoire.</summary>
    Random,
}
