namespace Luxia.UI.Modules.Live;

/// <summary>Raccourcis clavier du Live (doc 18 §5, LIVE-040, GEN-071).</summary>
public enum LiveKey
{
    /// <summary>F : flash général (maintien).</summary>
    Flash,

    /// <summary>S : strobe général (maintien).</summary>
    Strobe,

    /// <summary>Z : fumée (maintien).</summary>
    Smoke,

    /// <summary>G : figer (bascule).</summary>
    Freeze,

    /// <summary>Échap : libérer les palettes rapides.</summary>
    Release,

    /// <summary>← : couche précédente.</summary>
    PreviousLayer,

    /// <summary>→ : couche suivante.</summary>
    NextLayer,

    /// <summary>Page ↑ : Grand Master + 10 %.</summary>
    MasterUp,

    /// <summary>Page ↓ : Grand Master − 10 %.</summary>
    MasterDown,

    /// <summary>↑ : master de la couche encadrée + 10 % (essai P5).</summary>
    LayerMasterUp,

    /// <summary>↓ : master de la couche encadrée − 10 %.</summary>
    LayerMasterDown,

    /// <summary>1 à 9 : scène N de la couche sélectionnée.</summary>
    Scene1,

    /// <summary>Scène 2.</summary>
    Scene2,

    /// <summary>Scène 3.</summary>
    Scene3,

    /// <summary>Scène 4.</summary>
    Scene4,

    /// <summary>Scène 5.</summary>
    Scene5,

    /// <summary>Scène 6.</summary>
    Scene6,

    /// <summary>Scène 7.</summary>
    Scene7,

    /// <summary>Scène 8.</summary>
    Scene8,

    /// <summary>Scène 9.</summary>
    Scene9,
}
