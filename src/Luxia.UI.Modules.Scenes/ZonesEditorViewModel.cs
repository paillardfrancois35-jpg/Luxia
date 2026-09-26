using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Fixtures.Model;
using Luxia.Hosting;
using Luxia.Patch.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Scenes;

/// <summary>
/// Zones interdites des lyres dans le lieu actif (INST-053, GEN-085) : on vise avec le programmeur (faders Pan/Tilt) puis
/// on prend chaque coin du rectangle. Valeurs en % de Pan et de Tilt, telles que lues au programmeur.
/// </summary>
public sealed partial class ZonesEditorViewModel : ViewModelBase
{
    private readonly LuxiaRuntime _runtime;
    private readonly ProgrammerViewModel _programmer;

    [ObservableProperty]
    private ZoneRowViewModel? _selected;

    [ObservableProperty]
    private string? _message;

    /// <summary>Charge les zones du lieu actif.</summary>
    public ZonesEditorViewModel(LuxiaRuntime runtime, ProgrammerViewModel programmer)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(programmer);
        _runtime = runtime;
        _programmer = programmer;
        Venue = runtime.Project.Venues.Active;
        foreach (var fixture in runtime.Show.Patch.Fixtures.Where(f => HasPanTilt(f.Channels)))
        {
            Movers.Add(new Choice<Guid>(fixture.Fixture.Id, fixture.Fixture.Name));
        }

        foreach (var zone in Venue.ForbiddenZones)
        {
            var mover = Movers.FirstOrDefault(m => m.Value == zone.FixtureId);
            if (mover is not null)
            {
                Zones.Add(new ZoneRowViewModel(zone, mover));
            }
        }

        Selected = Zones.FirstOrDefault();
    }

    /// <summary>Lieu actif (les zones lui sont propres).</summary>
    public Venue Venue { get; }

    /// <summary>Titre de la fenêtre.</summary>
    public string Title => $"Zones interdites – lieu « {Venue.Name} »";

    /// <summary>Appareils à Pan et Tilt du patch.</summary>
    public ObservableCollection<Choice<Guid>> Movers { get; } = [];

    /// <summary>Zones du lieu.</summary>
    public ObservableCollection<ZoneRowViewModel> Zones { get; } = [];

    /// <summary>Levé à l'enregistrement ou à l'abandon.</summary>
    public event EventHandler? Closed;

    /// <summary>Nouvelle zone autour de la position visée par la lyre sélectionnée au programmeur (± 5 %).</summary>
    [RelayCommand]
    private void AddFromProgrammer()
    {
        var mover = _programmer.SelectedFixtures.FirstOrDefault(f => HasPanTilt(f.Channels));
        var choice = mover is null ? null : Movers.FirstOrDefault(m => m.Value == mover.Fixture.Id);
        if (choice is null || Aim(choice.Value) is not { } aim)
        {
            Message = "Sélectionnez une lyre au programmeur et visez la zone à interdire.";
            return;
        }

        var row = new ZoneRowViewModel(
            new ForbiddenZone
            {
                FixtureId = choice.Value,
                Name = "Zone",
                PanMin = Math.Max(0, aim.Pan - 0.05),
                PanMax = Math.Min(1, aim.Pan + 0.05),
                TiltMin = Math.Max(0, aim.Tilt - 0.05),
                TiltMax = Math.Min(1, aim.Tilt + 0.05),
            },
            choice);
        Zones.Add(row);
        Selected = row;
        Message = "Zone créée autour de la position visée : visez un coin puis « Coin 1 », l'autre coin puis « Coin 2 ».";
    }

    /// <summary>Premier coin de la zone sélectionnée = position visée actuellement par sa lyre.</summary>
    [RelayCommand]
    private void TakeFirstCorner() => TakeCorner(first: true);

    /// <summary>Coin opposé de la zone sélectionnée = position visée actuellement par sa lyre.</summary>
    [RelayCommand]
    private void TakeSecondCorner() => TakeCorner(first: false);

    [RelayCommand]
    private void Delete()
    {
        if (Selected is { } row)
        {
            Zones.Remove(row);
            Selected = Zones.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void Save()
    {
        var venues = _runtime.Project.Venues;
        var zones = Zones.Select(z => z.ToZone()).ToList();
        _runtime.Project.SaveVenues(venues with
        {
            Venues = [.. venues.Venues.Select(v => v.Id == Venue.Id ? v with { ForbiddenZones = zones } : v)],
        });
        Closed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => Closed?.Invoke(this, EventArgs.Empty);

    private void TakeCorner(bool first)
    {
        if (Selected is not { } row || Aim(row.Mover.Value) is not { } aim)
        {
            Message = "Choisissez une zone dans la liste.";
            return;
        }

        // Les deux coins pris forment le rectangle, dans n'importe quel ordre.
        var (pan, tilt) = ((decimal)Math.Round(aim.Pan * 100, 1), (decimal)Math.Round(aim.Tilt * 100, 1));
        if (first)
        {
            row.SetFirstCorner(pan, tilt);
        }
        else
        {
            row.SetSecondCorner(pan, tilt);
        }

        Message = null;
    }

    /// <summary>Position visée par un appareil : surcharge du programmeur si posée, sinon valeur jouée.</summary>
    private (double Pan, double Tilt)? Aim(Guid fixtureId)
    {
        var info = _runtime.Show.Patch.Find(fixtureId);
        if (info is null)
        {
            return null;
        }

        var snapshot = _programmer.TargetEngine.Snapshot;
        double? Read(AttributeKind attribute)
        {
            var key = info.Channels.FirstOrDefault(c => c.Attribute == attribute)?.Key;
            var index = key is null ? -1 : snapshot.Show.IndexOf(info.ReferenceId, key);
            if (index < 0 || index >= snapshot.Values.Length)
            {
                return null;
            }

            return double.IsNaN(snapshot.Overrides[index]) ? snapshot.Values[index] : snapshot.Overrides[index];
        }

        return Read(AttributeKind.Pan) is { } pan && Read(AttributeKind.Tilt) is { } tilt ? (pan, tilt) : null;
    }

    private static bool HasPanTilt(IReadOnlyList<ChannelDefinition> channels) =>
        channels.Any(c => c.Attribute == AttributeKind.Pan) && channels.Any(c => c.Attribute == AttributeKind.Tilt);
}
