using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Dmx.Patch.Model;

namespace Dmx.UI.Modules.Installation;

/// <summary>Un lieu du projet affiché (doc 13 §5).</summary>
public sealed partial class VenueRowViewModel : ObservableObject
{
    /// <summary>Crée la ligne : une position par appareil patché (placée si déjà présente dans le lieu).</summary>
    public VenueRowViewModel(Venue venue, IReadOnlyList<PatchedFixture> fixtures)
    {
        Venue = venue;
        _name = venue.Name;
        _widthM = venue.WidthM;
        _depthM = venue.DepthM;
        Placements = [.. fixtures.OrderBy(f => f.Universe).ThenBy(f => f.Address)
            .Select(f => new PlacementRowViewModel(f, venue.PlacementOf(f.Id)))];
    }

    /// <summary>Lieu.</summary>
    public Venue Venue { get; }

    /// <summary>Identifiant stable.</summary>
    public Guid Id => Venue.Id;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private double _widthM;

    [ObservableProperty]
    private double _depthM;

    /// <summary>Position de chaque appareil du patch (INST-051, INST-052).</summary>
    public ObservableCollection<PlacementRowViewModel> Placements { get; }

    /// <summary>Reconstruit le lieu à partir des champs en cours d'édition.</summary>
    public Venue ToVenue() => Venue with
    {
        Name = string.IsNullOrWhiteSpace(Name) ? Venue.Name : Name.Trim(),
        WidthM = WidthM,
        DepthM = DepthM,
        Placements = [.. Placements.Select(p => p.ToPlacement())],
    };
}
