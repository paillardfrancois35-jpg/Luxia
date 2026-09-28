namespace Luxia.UI.Modules.Control;

/// <summary>Les trois modes d'édition de la charte (doc 60 §4.1, E1) : où va un réglage fait sur un appareil.</summary>
public enum EditMode
{
    /// <summary>Surcharge temporaire par-dessus les scènes ; rien n'est enregistré ; « Libérer » la retire.</summary>
    Live,

    /// <summary>Le réglage s'écrit tout de suite dans l'étape choisie, et sort.</summary>
    Edit,

    /// <summary>Le réglage s'écrit dans l'étape choisie ; la sortie ne change pas (aperçu au plan).</summary>
    Blind,
}
