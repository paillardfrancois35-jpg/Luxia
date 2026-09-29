namespace Luxia.Scenes.Model;

/// <summary>Répartition du décalage de phase entre les membres d'un effet, dans l'ordre de la sélection (EFF-005).</summary>
public enum EffectPhaseMode
{
    /// <summary>Linéaire : du premier au dernier membre (vague de gauche à droite).</summary>
    Linear,

    /// <summary>Miroir : du centre de la sélection vers les bords, les deux moitiés symétriques.</summary>
    Mirror,

    /// <summary>Par groupes : un membre sur N ensemble (N = 2 : pairs et impairs en alternance).</summary>
    Groups,

    /// <summary>Aléatoire : ordre tiré une fois pour toutes (même effet = même ordre).</summary>
    Random,
}
