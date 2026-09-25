namespace Dmx.Patch.Model;

/// <summary>
/// Lieu (doc 13 §5) : plan de la salle, position de chaque appareil, présence ce soir.
/// Un projet contient un ou plusieurs lieux, dont un lieu actif (<see cref="VenueSet.ActiveVenueId"/>).
/// </summary>
public sealed record Venue
{
    /// <summary>Identifiant stable (GEN-052).</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nom affiché.</summary>
    public required string Name { get; init; }

    /// <summary>Largeur de la salle, en mètres.</summary>
    public double WidthM { get; init; } = 12;

    /// <summary>Profondeur de la salle, en mètres.</summary>
    public double DepthM { get; init; } = 8;

    /// <summary>Positions des appareils placés (un appareil non placé n'apparaît pas au plan).</summary>
    public IReadOnlyList<FixturePlacement> Placements { get; init; } = [];

    /// <summary>Position d'un appareil, si placé dans ce lieu.</summary>
    public FixturePlacement? PlacementOf(Guid fixtureId) => Placements.FirstOrDefault(p => p.FixtureId == fixtureId);
}

/// <summary>Ensemble des lieux d'un projet, enregistré dans <c>lieux.json</c> (doc 50).</summary>
public sealed record VenueSet
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Nom du lieu par défaut, créé avec tout nouveau projet (doc 13 §5.2, INST-050).</summary>
    public const string DefaultVenueName = "Générique";

    /// <summary>Lieux (au moins un).</summary>
    public IReadOnlyList<Venue> Venues { get; init; } = [new Venue { Name = DefaultVenueName }];

    /// <summary>Lieu actif ; <c>null</c> = le premier de la liste.</summary>
    public Guid? ActiveVenueId { get; init; }

    /// <summary>Lieu actif effectif.</summary>
    public Venue Active => (ActiveVenueId is { } id ? Venues.FirstOrDefault(v => v.Id == id) : null) ?? Venues[0];
}
