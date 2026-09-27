namespace Luxia.UI.Modules.Control;

/// <summary>
/// Jetons de couleur de la charte (doc 60 §4.5) : un sens = une couleur. Ils sont aussi déclarés en ressources dans
/// <c>ControlView.axaml</c> ; ceux-ci servent aux modèles de vue (couleur d'un contour selon le mode…).
/// </summary>
public static class ControlColors
{
    /// <summary>Sélection, focus.</summary>
    public const string Accent = "#58A6FF";

    /// <summary>Surcharge LIVE (temporaire).</summary>
    public const string Live = "#D29922";

    /// <summary>ÉDITION : enregistré dans la scène.</summary>
    public const string Edit = "#3FB950";

    /// <summary>AVEUGLE : enregistré sans sortir.</summary>
    public const string Blind = "#79C0FF";

    /// <summary>Zones du lieu.</summary>
    public const string Zones = "#F0883E";

    /// <summary>Texte secondaire.</summary>
    public const string Secondary = "#8B949E";

    /// <summary>Couleur de pastille d'un paramètre (doc 60 §4.3).</summary>
    public static string Of(ParameterState state) => state switch
    {
        ParameterState.LiveOverride => Live,
        ParameterState.InScene => Edit,
        _ => "#00000000",
    };

    /// <summary>Couleur du mode d'édition.</summary>
    public static string Of(EditMode mode) => mode switch
    {
        EditMode.Edit => Edit,
        EditMode.Blind => Blind,
        _ => Live,
    };
}
