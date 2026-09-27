namespace Luxia.Midi;

/// <summary>Famille de contrôle physique.</summary>
public enum MidiControlKind
{
    /// <summary>Pad de la grille (colonne, ligne ; ligne 1 = en haut).</summary>
    Pad,

    /// <summary>Bouton rond du bas (1 à 8).</summary>
    Bottom,

    /// <summary>Bouton rond de droite (1 à 8, de haut en bas).</summary>
    Right,

    /// <summary>Fader (1 à 9, 9 = master).</summary>
    Fader,
}
