namespace Dmx.Fixtures.Model;

/// <summary>Type d'une plage (doc 12 §2.5).</summary>
public enum CapabilityKind
{
    /// <summary>Une seule signification.</summary>
    Fixed,

    /// <summary>Un paramètre varie du début à la fin.</summary>
    Progressive,

    /// <summary>Emplacement de roue (couleur, gobo).</summary>
    WheelSlot,

    /// <summary>Rotation.</summary>
    Rotation,

    /// <summary>Programme.</summary>
    Program,

    /// <summary>Sans fonction.</summary>
    NoFunction,

    /// <summary>Arrêt / fermé.</summary>
    Closed,

    /// <summary>Ouvert.</summary>
    Open,
}
