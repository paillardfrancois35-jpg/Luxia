using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.Core.Dmx;
using Luxia.Fixtures.Model;
using Luxia.Hosting;
using Luxia.Patch.Model;
using Luxia.Patch.Rules;
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

        var installation = _runtime.Project.Installation;
        var venue = _runtime.Project.Venues.Active;

        // Le lieu actif ou ses dimensions peuvent avoir changé dans l'écran Installation (doc 13 §5) : pas
        // d'événement dédié (comme les instantanés de console), donc relu à chaque rafraîchissement.
        VenueName = venue.Name;
        RoomWidthM = venue.WidthM;
        RoomDepthM = venue.DepthM;

        Fixtures.Clear();
        foreach (var fixture in installation.Fixtures)
        {
            var placement = venue.PlacementOf(fixture.Id);
            if (placement is not { Absent: false })
            {
                continue;
            }

            var type = _runtime.Project.FixtureLibrary?.Find(fixture.FixtureTypeId);
            var mode = type?.Modes.FirstOrDefault(m => m.Name == fixture.ModeName);
            if (type is null || mode is null)
            {
                Fixtures.Add(new SimulatorFixtureVisual(fixture.Id, fixture.Name, placement.X, placement.Y, placement.OrientationDeg, false, null, [], HasError: true, Strobing: false));
                continue;
            }

            var frame = FrameOf(fixture.Universe);
            var decoded = FixtureDecoder.Decode(type, mode, fixture, frame);
            var cells = decoded.Cells.Count > 0
                ? decoded.Cells.Select((c, i) => new SimulatorCellVisual(CellOffset(i, decoded.Cells.Count), c.Color.ToString(), c.Intensity)).ToList()
                : [new SimulatorCellVisual(0, "#58A6FF", 0)];
            Fixtures.Add(new SimulatorFixtureVisual(
                fixture.Id,
                fixture.Name,
                placement.X,
                placement.Y,
                placement.OrientationDeg,
                decoded.PanDegrees.HasValue,
                decoded.PanDegrees,
                cells,
                HasError: false,
                decoded.Cells.Any(c => c.Strobing)));
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

    private static double CellOffset(int index, int count) => (index - ((count - 1) / 2.0)) * 0.15;

    private byte[] FrameOf(int universe)
    {
        if (!_frames.TryGetValue(universe, out var frame))
        {
            frame = new byte[DmxConstants.ChannelCount];
            _frames[universe] = frame;
        }

        // GEN-063 : en aveugle, le simulateur montre le moteur d'aperçu (mêmes scènes, programmeur non émis).
        var engine = _runtime.PreviewActive ? _runtime.Preview : _runtime.Engine;
        if (universe >= 1 && universe <= engine.UniverseCount)
        {
            engine.CopyLastFrame(universe, frame);
        }

        return frame;
    }

    private void LoadVenue()
    {
        HasProject = _runtime.Project.Folder is not null;
        Refresh();
    }
}
