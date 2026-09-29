namespace Luxia.Engine.Model;

/// <summary>Forme d'un effet généré (doc 16 §6.1, EFF-002 à EFF-004).</summary>
public enum EffectShape
{
    /// <summary>Sinus : montée et descente douces (EFF-002).</summary>
    Sine,

    /// <summary>Triangle : montée et descente régulières.</summary>
    Triangle,

    /// <summary>Carré : tout ou rien, selon le rapport cyclique (chenillard on/off).</summary>
    Square,

    /// <summary>Dent de scie montante : montée régulière, retombée d'un coup.</summary>
    SawUp,

    /// <summary>Dent de scie descendante : allumage d'un coup, descente régulière.</summary>
    SawDown,

    /// <summary>Impulsion : éclat bref qui décroît, puis éteint le reste du cycle (rapport cyclique = durée de l'éclat).</summary>
    Pulse,

    /// <summary>Aléatoire : une nouvelle valeur tirée à chaque cycle (scintillement), reproductible (MOT-004).</summary>
    Random,

    /// <summary>Cercle autour du centre (EFF-003).</summary>
    Circle,

    /// <summary>Huit couché autour du centre.</summary>
    Eight,

    /// <summary>Balayage horizontal (Pan) de part et d'autre du centre.</summary>
    SweepPan,

    /// <summary>Balayage vertical (Tilt) de part et d'autre du centre.</summary>
    SweepTilt,

    /// <summary>Déplacement aléatoire lent : un point tiré par cycle, rejoint en douceur.</summary>
    RandomSlow,

    /// <summary>
    /// Suite de valeurs précalculées sur un cycle (couleurs : arc-en-ciel, alternance, dégradé, EFF-004) :
    /// chaque canal porte sa table, la compilation a déjà converti les couleurs selon les émetteurs de l'appareil.
    /// </summary>
    Table,
}
