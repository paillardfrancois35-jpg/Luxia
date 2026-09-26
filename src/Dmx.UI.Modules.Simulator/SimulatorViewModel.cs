using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Dmx.Core.Dmx;
using Dmx.Fixtures.Model;
using Dmx.Hosting;
using Dmx.Patch.Model;
using Dmx.Patch.Rules;
using Dmx.UI.Controls;

namespace Dmx.UI.Modules.Simulator;

/// <summary>
/// Écran « Simulateur » (doc 14) : plan du lieu actif, appareils présents à leur position, décodés depuis la
/// trame réellement émise (SIM-003). Lit l'état du moteur à son propre rythme, comme le moniteur de la console
/// (GEN-013 : ne ralentit jamais le moteur).
/// </summary>
public sealed partial class SimulatorViewModel : ViewModelBase, IRefreshable
{
    private readonly DmxRuntime _runtime;
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
    public SimulatorViewModel(DmxRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
        _runtime.Project.Changed += (_, _) => LoadVenue();
        LoadVenue();
    }

    /// <summary>Source affichée (SIM-006). Seule « Sortie » est disponible avant le mode aveugle (P4) et le lecteur (P3, S).</summary>
    public string Source => "Sortie (trames réellement émises)";

    /// <summary>Levé après chaque rafraîchissement (la vue met alors le simulateur à jour).</summary>
    public event EventHandler? Refreshed;

    /// <summary>Appareils à dessiner.</summary>
    public ObservableCollection<SimulatorFixtureVisual> Fixtures { get; } = [];

    /// <inheritdoc />
    public void Refresh()
    {
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

        if (universe >= 1 && universe <= _runtime.Engine.UniverseCount)
        {
            _runtime.Engine.CopyLastFrame(universe, frame);
        }

        return frame;
    }

    private void LoadVenue()
    {
        HasProject = _runtime.Project.Folder is not null;
        Refresh();
    }
}
