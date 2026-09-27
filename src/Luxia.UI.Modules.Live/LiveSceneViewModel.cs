using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.Scenes.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Live;

/// <summary>Un bouton de scène en Live : couleur de la scène, active ou non, progression (LIVE-002).</summary>
public sealed partial class LiveSceneViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _state = string.Empty;

    /// <summary>Crée le bouton.</summary>
    public LiveSceneViewModel(Scene scene, LayerColumnViewModel column, int index)
    {
        ArgumentNullException.ThrowIfNull(scene);
        Scene = scene;
        Column = column;
        Index = index;
    }

    /// <summary>Scène.</summary>
    public Scene Scene { get; }

    /// <summary>Colonne (couche).</summary>
    public LayerColumnViewModel Column { get; }

    /// <summary>Rang dans la colonne (touches 1 à 9, LIVE-040).</summary>
    public int Index { get; }

    /// <summary>Nom.</summary>
    public string Name => Scene.Name;

    /// <summary>Couleur de la scène (GEN-106).</summary>
    public string Color => Scene.Color;

    /// <summary>Fond du bouton : la couleur de la scène (atténuée) quand elle joue, sinon neutre.</summary>
    public string Fill => IsActive && Color.Length == 7 ? "#66" + Color[1..] : "#161B22";

    /// <summary>Raccourci clavier affiché (1 à 9), ou ▶ quand la scène joue.</summary>
    public string Marker => IsActive ? "▶" : Key;

    partial void OnIsActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(Fill));
        OnPropertyChanged(nameof(Marker));
    }

    /// <summary>Raccourci clavier affiché (1 à 9).</summary>
    public string Key => Index <= 9 ? Index.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
}
