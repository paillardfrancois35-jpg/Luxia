namespace Luxia.Scenes.Model;

/// <summary>Forme d'un effet de scène (doc 16 §6.1) : intensité (EFF-002), position (EFF-003) ou couleur (EFF-004).</summary>
public enum SceneEffectShape
{
    /// <summary>Sinus.</summary>
    Sine,

    /// <summary>Triangle.</summary>
    Triangle,

    /// <summary>Carré (on/off).</summary>
    Square,

    /// <summary>Dent de scie montante.</summary>
    SawUp,

    /// <summary>Dent de scie descendante.</summary>
    SawDown,

    /// <summary>Impulsion.</summary>
    Pulse,

    /// <summary>Aléatoire (scintillement).</summary>
    Random,

    /// <summary>Cercle (Pan/Tilt).</summary>
    Circle,

    /// <summary>Huit (Pan/Tilt).</summary>
    Eight,

    /// <summary>Balayage horizontal (Pan).</summary>
    SweepPan,

    /// <summary>Balayage vertical (Tilt).</summary>
    SweepTilt,

    /// <summary>Aléatoire lent (Pan/Tilt).</summary>
    RandomSlow,

    /// <summary>Arc-en-ciel : tour complet de la roue des teintes.</summary>
    Rainbow,

    /// <summary>Alternance franche entre les couleurs de l'effet (ou d'un thème).</summary>
    Alternate,

    /// <summary>Dégradé : passage progressif d'une couleur à la suivante, puis retour à la première.</summary>
    Gradient,
}
