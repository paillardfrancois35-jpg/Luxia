using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.Hosting;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Simulator;

/// <summary>
/// Écran « Simulateur » (doc 14) : plan du lieu actif, appareils présents à leur position, décodés depuis la
/// trame réellement émise (SIM-003). Lit l'état du moteur à son propre rythme, comme le moniteur de la console
/// (GEN-013 : ne ralentit jamais le moteur).
/// </summary>
public sealed partial class SimulatorViewModel : ViewModelBase, IRefreshable
{
    private readonly LuxiaRuntime _runtime;
    private readonly Dictionary<int, byte[]> _frames = [];

    [ObservableProperty]
    private bool _hasProject;

    [ObservableProperty]
    private string _venueName = string.Empty;

    [ObservableProperty]
    private double _roomWidthM = 12;

    [ObservableProperty]
    private double _roomDepthM = 8;

    [ObservableProperty]
    private string _hoverText = "Survolez un appareil.";

    /// <summary>Crée l'écran.</summary>
    public SimulatorViewModel(LuxiaRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
        _runtime.Project.Changed += (_, _) => LoadVenue();
        LoadVenue();
    }

    /// <summary>
    /// Source affichée en permanence (SIM-006) : la sortie, ou l'aperçu pendant l'édition en aveugle (GEN-063, SCN-035).
    /// Le lecteur d'enregistrements (SORT-063, S) n'est pas réalisé.
    /// </summary>
    public string Source => _runtime.PreviewActive
        ? "APERÇU (édition en aveugle : rien n'est émis)"
        : "Sortie (trames réellement émises)";

    /// <summary>L'aperçu est affiché (bandeau d'avertissement).</summary>
    public bool IsPreview => _runtime.PreviewActive;

    /// <summary>Levé après chaque rafraîchissement (la vue met alors le simulateur à jour).</summary>
    public event EventHandler? Refreshed;

    /// <summary>Appareils à dessiner.</summary>
    public ObservableCollection<SimulatorFixtureVisual> Fixtures { get; } = [];

    /// <inheritdoc />
    public void Refresh()
    {
        OnPropertyChanged(nameof(Source));
        OnPropertyChanged(nameof(IsPreview));
        if (!HasProject)
        {
            return;
        }

        var venue = _runtime.Project.Venues.Active;

        // Le lieu actif ou ses dimensions peuvent avoir changé dans l'écran Installation (doc 13 §5) : pas
        // d'événement dédié (comme les instantanés de console), donc relu à chaque rafraîchissement.
        VenueName = venue.Name;
        RoomWidthM = venue.WidthM;
        RoomDepthM = venue.DepthM;

        // GEN-063 : en aveugle, le simulateur montre le moteur d'aperçu (mêmes scènes, réglages non émis).
        Fixtures.Clear();
        foreach (var visual in SimulatorVisuals.Build(_runtime, _runtime.PreviewActive ? _runtime.Preview : _runtime.Engine, _frames))
        {
            Fixtures.Add(visual);
        }

        Refreshed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Survol d'un appareil (SIM-005).</summary>
    public void OnFixtureHovered(Guid id)
    {
        if (id == Guid.Empty)
        {
            HoverText = "Survolez un appareil.";
            return;
        }

        var fixture = _runtime.Project.Installation.Fixtures.FirstOrDefault(f => f.Id == id);
        if (fixture is null)
        {
            return;
        }

        var type = _runtime.Project.FixtureLibrary?.Find(fixture.FixtureTypeId);
        HoverText = string.Create(
            CultureInfo.CurrentCulture,
            $"{fixture.Name} — {type?.DisplayName ?? "modèle introuvable"} — univers {fixture.Universe}, adresse {fixture.Address}");
    }

    private void LoadVenue()
    {
        HasProject = _runtime.Project.Folder is not null;
        Refresh();
    }
}
