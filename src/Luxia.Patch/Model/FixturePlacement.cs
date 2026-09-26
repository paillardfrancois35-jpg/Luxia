namespace Luxia.Patch.Model;

/// <summary>Position d'un appareil dans un lieu (doc 13 §5.1) : un appareil non placé n'apparaît pas au plan.</summary>
public sealed record FixturePlacement
{
    /// <summary>Appareil positionné.</summary>
    public required Guid FixtureId { get; init; }

    /// <summary>Position au sol, en mètres depuis le coin (0, 0) de la salle.</summary>
    public double X { get; init; }

    /// <summary>Position au sol, en mètres depuis le coin (0, 0) de la salle.</summary>
    public double Y { get; init; }

    /// <summary>Hauteur, en mètres.</summary>
    public double HeightM { get; init; } = 2;

    /// <summary>Orientation, en degrés (0 = vers le fond de la salle, sens horaire).</summary>
    public double OrientationDeg { get; init; }

    /// <summary>Montage.</summary>
    public MountingKind Mounting { get; init; } = MountingKind.Standing;

    /// <summary>
    /// Absent ce soir (INST-052) : n'est pas émis (canaux à 0), n'apparaît plus au simulateur ni dans les
    /// sélections actives ; les scènes l'ignorent sans erreur (à partir de P4).
    /// </summary>
    public bool Absent { get; init; }
}
