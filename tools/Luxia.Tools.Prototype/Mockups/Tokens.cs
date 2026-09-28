using Avalonia.Media;

namespace Luxia.Tools.Prototype.Mockups;

/// <summary>
/// Jetons de couleur de la charte (doc 60 §4.5) : un sens = une couleur, jamais en dur ailleurs. Dans LuXia, ils
/// vivront dans <c>App.axaml</c> ; ici, dans les maquettes.
/// </summary>
internal static class Tokens
{
    public static readonly Color Background = Color.Parse("#0D1117");
    public static readonly Color Surface = Color.Parse("#161B22");
    public static readonly Color Raised = Color.Parse("#21262D");
    public static readonly Color Border = Color.Parse("#30363D");
    public static readonly Color Text = Color.Parse("#E6EDF3");
    public static readonly Color Secondary = Color.Parse("#8B949E");

    /// <summary>Sélection, focus.</summary>
    public static readonly Color Accent = Color.Parse("#58A6FF");

    /// <summary>Surcharge LIVE (temporaire).</summary>
    public static readonly Color Live = Color.Parse("#D29922");

    /// <summary>ÉDITION : enregistré dans la scène.</summary>
    public static readonly Color Edit = Color.Parse("#3FB950");

    /// <summary>AVEUGLE : enregistré sans sortir.</summary>
    public static readonly Color Blind = Color.Parse("#79C0FF");

    /// <summary>La sûreté agit.</summary>
    public static readonly Color Safety = Color.Parse("#F0883E");

    /// <summary>Danger, blackout, enregistrement.</summary>
    public static readonly Color Danger = Color.Parse("#F85149");

    public static SolidColorBrush Brush(Color color, double opacity = 1) => new(color, opacity);

    /// <summary>Couleur du mode d'édition.</summary>
    public static Color ModeColor(MockMode mode) => mode switch
    {
        MockMode.Edit => Edit,
        MockMode.Blind => Blind,
        _ => Live,
    };
}
