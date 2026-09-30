namespace Luxia.Engine.Model;

/// <summary>Quantification du lancement d'une scène (MOT-018) : elle attend l'instant musical choisi pour démarrer.</summary>
public enum LaunchQuantize
{
    /// <summary>Démarrage immédiat.</summary>
    None,

    /// <summary>Au prochain temps.</summary>
    Beat,

    /// <summary>Au début de la prochaine mesure.</summary>
    Bar,

    /// <summary>Au début de la prochaine phrase de 4 mesures.</summary>
    Phrase4,

    /// <summary>Au début de la prochaine phrase de 8 mesures.</summary>
    Phrase8,
}
