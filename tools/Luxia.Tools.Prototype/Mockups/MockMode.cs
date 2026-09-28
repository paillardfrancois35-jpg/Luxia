namespace Luxia.Tools.Prototype.Mockups;

/// <summary>Les trois modes d'édition de la charte (doc 60 §4.1, E1).</summary>
internal enum MockMode
{
    /// <summary>Surcharges temporaires par-dessus les scènes ; rien n'est enregistré.</summary>
    Live,

    /// <summary>Les réglages s'écrivent dans l'étape sélectionnée, et sortent.</summary>
    Edit,

    /// <summary>Les réglages s'écrivent dans la scène, la sortie ne change pas.</summary>
    Blind,
}
