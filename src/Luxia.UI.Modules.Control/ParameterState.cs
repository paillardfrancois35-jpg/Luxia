namespace Luxia.UI.Modules.Control;

/// <summary>Pastille d'un paramètre (doc 60 §4.3) : d'où vient ce qu'on voit.</summary>
public enum ParameterState
{
    /// <summary>◯ Rien ne le règle ici : l'appareil garde ce que donnent les autres couches (ou sa valeur par défaut).</summary>
    Unused,

    /// <summary>🟢 Réglé par une scène (en LIVE : une scène qui joue ; en ÉDITION / AVEUGLE : l'étape éditée).</summary>
    InScene,

    /// <summary>🟡 Surcharge LIVE, temporaire.</summary>
    LiveOverride,
}
