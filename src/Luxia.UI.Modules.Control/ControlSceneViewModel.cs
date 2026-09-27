using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.Scenes.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>
/// Bouton de scène à deux zones (doc 60 §2, §5) : la grande zone joue / arrête (flash maintenu dans une couche Flash),
/// la bande ✎ choisit la scène à éditer. Scène qui joue : fond à sa couleur et progression ; scène éditée : contour
/// à la couleur du mode.
/// </summary>
public sealed partial class ControlSceneViewModel : ViewModelBase
{
    private long _expectedAfterTick = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Fill), nameof(TextColor))]
    private bool _isActive;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _state = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditBandFill), nameof(OutlineThickness))]
    private bool _isEditTarget;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditBandFill))]
    private string _editColor = "#58A6FF";

    /// <summary>Crée le bouton.</summary>
    public ControlSceneViewModel(Scene scene, ControlColumnViewModel column)
    {
        ArgumentNullException.ThrowIfNull(scene);
        Scene = scene;
        Column = column;
    }

    /// <summary>Scène.</summary>
    public Scene Scene { get; }

    /// <summary>Colonne (couche).</summary>
    public ControlColumnViewModel Column { get; }

    /// <summary>Nom.</summary>
    public string Name => Scene.Name;

    /// <summary>Couleur de la scène (GEN-106).</summary>
    public string Color => Scene.Color.Length == 7 ? Scene.Color : "#58A6FF";

    /// <summary>Scène masquée du Live (elle reste jouable et éditable ici).</summary>
    public bool HiddenInLive => !Scene.VisibleInLive;

    /// <summary>Fond de la grande zone : la couleur de la scène quand elle joue, neutre sinon.</summary>
    public string Fill => IsActive ? "#D9" + Color[1..] : "#21262D";

    /// <summary>Texte lisible sur le fond (sombre sur une couleur claire).</summary>
    public string TextColor => IsActive && Luminance(Color) > 0.6 ? "#0D1117" : "#E6EDF3";

    /// <summary>Fond de la bande ✎ : couleur du mode quand la scène est éditée.</summary>
    public string EditBandFill => IsEditTarget ? EditColor : "#161B22";

    /// <summary>Épaisseur du contour de la scène éditée.</summary>
    public double OutlineThickness => IsEditTarget ? 2 : 0;

    /// <summary>
    /// Affiche tout de suite l'état attendu après un clic, sans le laisser écraser par un état du moteur antérieur à la
    /// commande (course écran / moteur, doc 03 §11).
    /// </summary>
    public void ExpectActive(bool active, long tick)
    {
        IsActive = active;
        _expectedAfterTick = tick + 2;
    }

    /// <summary>Un clic attend encore d'être traité par le moteur.</summary>
    public bool WaitingForEngine(long tick) => tick < _expectedAfterTick;

    private static double Luminance(string hex)
    {
        var c = Avalonia.Media.Color.Parse(hex);
        return ((0.299 * c.R) + (0.587 * c.G) + (0.114 * c.B)) / 255;
    }
}
