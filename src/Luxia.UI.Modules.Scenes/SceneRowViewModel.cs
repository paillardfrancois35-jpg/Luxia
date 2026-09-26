using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.Scenes.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Scenes;

/// <summary>Une scène dans la liste (SCN-001, SCN-012) : nom, couleur, icône, couche, état de lecture.</summary>
public sealed partial class SceneRowViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private string _playState = string.Empty;

    /// <summary>Crée la ligne.</summary>
    public SceneRowViewModel(Scene scene, string layerName)
    {
        ArgumentNullException.ThrowIfNull(scene);
        Scene = scene;
        LayerName = layerName;
    }

    /// <summary>Scène enregistrée.</summary>
    public Scene Scene { get; }

    /// <summary>Identifiant.</summary>
    public Guid Id => Scene.Id;

    /// <summary>Nom affiché, avec l'icône éventuelle.</summary>
    public string Title => string.IsNullOrWhiteSpace(Scene.Icon) ? Scene.Name : $"{Scene.Icon} {Scene.Name}";

    /// <summary>Couleur de la scène (GEN-106).</summary>
    public string Color => Scene.Color;

    /// <summary>Couche.</summary>
    public string LayerName { get; }

    /// <summary>Ligne de détail : couche, catégorie, étapes, visibilité en Live.</summary>
    public string Details => string.Create(
        CultureInfo.CurrentCulture,
        $"{LayerName}{(string.IsNullOrWhiteSpace(Scene.Category) ? string.Empty : " · " + Scene.Category)} · {Scene.Steps.Count} étape(s){(Scene.VisibleInLive ? string.Empty : " · masquée en Live")}");
}
