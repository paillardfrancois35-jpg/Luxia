namespace Luxia.Show.Model;

/// <summary>
/// Frontière musicale attendue avant un démarrage de séquence ou un franchissement de transition (doc 20 §3.2,
/// « quantification »). Les phrases se comptent depuis l'origine de l'horloge (« 1 ici » la recale).
/// </summary>
public enum ShowQuantize
{
    /// <summary>Tout de suite.</summary>
    None,

    /// <summary>Prochain temps.</summary>
    Beat,

    /// <summary>Prochaine mesure.</summary>
    Bar,

    /// <summary>Prochaine phrase de 4 mesures.</summary>
    Phrase4,

    /// <summary>Prochaine phrase de 8 mesures.</summary>
    Phrase8,

    /// <summary>Prochaine phrase de 16 mesures.</summary>
    Phrase16,
}
