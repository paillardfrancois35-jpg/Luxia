using Avalonia.Input;

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

/// <summary>Traduction des touches Avalonia en raccourcis du Live.</summary>
public static class LiveKeys
{
    /// <summary>Raccourci d'une touche (sans modificateur), ou <c>null</c>. B (blackout) reste traité par la fenêtre.</summary>
    public static LiveKey? From(Key key) => key switch
    {
        Key.F => LiveKey.Flash,
        Key.S => LiveKey.Strobe,
        Key.Z => LiveKey.Smoke,
        Key.G => LiveKey.Freeze,
        Key.Escape => LiveKey.Release,
        Key.Left => LiveKey.PreviousLayer,
        Key.Right => LiveKey.NextLayer,
        Key.PageUp => LiveKey.MasterUp,
        Key.PageDown => LiveKey.MasterDown,
        >= Key.D1 and <= Key.D9 => LiveKey.Scene1 + (key - Key.D1),
        >= Key.NumPad1 and <= Key.NumPad9 => LiveKey.Scene1 + (key - Key.NumPad1),
        _ => null,
    };
}
