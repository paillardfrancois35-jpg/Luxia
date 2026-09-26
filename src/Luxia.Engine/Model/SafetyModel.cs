namespace Luxia.Engine.Model;

/// <summary>
/// Limites de sûreté compilées (doc 15 §9, étape 9 de la chaîne de rendu) : réglages du projet, canaux de strobe et de
/// fumée du patch, zones interdites des lyres dans le lieu actif. Le moteur ne connaît ni la bibliothèque ni les lieux (D26).
/// </summary>
public sealed record SafetyModel
{
    /// <summary>Aucune limite (projet non ouvert, tests).</summary>
    public static SafetyModel None { get; } = new();

    /// <summary>Réglages du limiteur de strobe (GEN-083).</summary>
    public StrobeLimits Strobe { get; init; } = new();

    /// <summary>Réglages du limiteur de fumée (GEN-084).</summary>
    public SmokeLimits Smoke { get; init; } = new();

    /// <summary>Canaux de strobe (étiquette de sûreté « strobe », BIB-007), un par canal DMX émis.</summary>
    public IReadOnlyList<GuardedChannel> StrobeChannels { get; init; } = [];

    /// <summary>Canaux de fumée (étiquette « fumée »), un par canal DMX émis.</summary>
    public IReadOnlyList<GuardedChannel> SmokeChannels { get; init; } = [];

    /// <summary>Zones interdites Pan/Tilt du lieu actif, par appareil (GEN-085).</summary>
    public IReadOnlyList<MovementGuard> Zones { get; init; } = [];
}

/// <summary>Réglages du limiteur de strobe (GEN-083, MOT-080).</summary>
public sealed record StrobeLimits
{
    /// <summary>Durée continue maximale de strobe par appareil, en secondes.</summary>
    public double MaxContinuousSeconds { get; init; } = 10;

    /// <summary>Pause forcée après un dépassement, en secondes.</summary>
    public double PauseSeconds { get; init; } = 10;

    /// <summary>Strobe interdit partout.</summary>
    public bool Forbidden { get; init; }

    /// <summary>Vitesse maximale (0-1 de chaque plage progressive de strobe ; 1 = pas de plafond).</summary>
    public double MaxSpeed { get; init; } = 1;
}

/// <summary>Réglages du limiteur de fumée (GEN-084, MOT-081).</summary>
public sealed record SmokeLimits
{
    /// <summary>Durée d'émission continue maximale, en secondes.</summary>
    public double MaxEmissionSeconds { get; init; } = 10;

    /// <summary>Repos minimal entre deux émissions, en secondes.</summary>
    public double MinRestSeconds { get; init; } = 30;
}

/// <summary>Plage de valeurs DMX (bornes incluses).</summary>
/// <param name="Min">Borne basse.</param>
/// <param name="Max">Borne haute.</param>
/// <param name="Increasing">La vitesse croît avec la valeur (faux pour « rapide → lent »).</param>
public readonly record struct ByteRange(int Min, int Max, bool Increasing = true)
{
    /// <summary>La valeur appartient à la plage.</summary>
    public bool Contains(int value) => value >= Min && value <= Max;
}

/// <summary>Canal DMX surveillé par un limiteur (strobe ou fumée), pour un appareil patché.</summary>
public sealed record GuardedChannel
{
    /// <summary>Appareil patché (chaque jumeau a les siens).</summary>
    public required Guid FixtureId { get; init; }

    /// <summary>Libellé lisible (« PAR 1 – Strobe général »).</summary>
    public required string Label { get; init; }

    /// <summary>Univers (1…).</summary>
    public required int Universe { get; init; }

    /// <summary>Canal (1-512).</summary>
    public required int Channel { get; init; }

    /// <summary>Valeur de repos imposée par le limiteur (pas de strobe, pas de fumée).</summary>
    public byte Rest { get; init; }

    /// <summary>Plages « actives » (strobe, fumée) ; les autres valeurs sont sans danger.</summary>
    public IReadOnlyList<ByteRange> Active { get; init; } = [];

    /// <summary>Plages actives progressives, bornées par <see cref="StrobeLimits.MaxSpeed"/>.</summary>
    public IReadOnlyList<ByteRange> Progressive { get; init; } = [];

    /// <summary>La valeur est dans une plage active.</summary>
    public bool IsActive(int value)
    {
        foreach (var range in Active)
        {
            if (range.Contains(value))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>Zones interdites d'un appareil à Pan/Tilt dans le lieu actif (MOT-082).</summary>
public sealed record MovementGuard
{
    /// <summary>Appareil (appareil de référence pour des jumeaux, qui partagent Pan et Tilt).</summary>
    public required Guid FixtureId { get; init; }

    /// <summary>Libellé lisible.</summary>
    public required string Label { get; init; }

    /// <summary>Indice du paramètre Pan.</summary>
    public required int Pan { get; init; }

    /// <summary>Indice du paramètre Tilt.</summary>
    public required int Tilt { get; init; }

    /// <summary>Rectangles interdits.</summary>
    public IReadOnlyList<PanTiltZone> Zones { get; init; } = [];
}

/// <summary>Rectangle interdit en valeurs logiques normalisées de Pan et Tilt (0-1, avant inversion de montage).</summary>
/// <param name="PanMin">Pan minimal.</param>
/// <param name="PanMax">Pan maximal.</param>
/// <param name="TiltMin">Tilt minimal.</param>
/// <param name="TiltMax">Tilt maximal.</param>
public readonly record struct PanTiltZone(double PanMin, double PanMax, double TiltMin, double TiltMax)
{
    /// <summary>Le point est strictement à l'intérieur (le bord est autorisé).</summary>
    public bool Contains(double pan, double tilt) => pan > PanMin && pan < PanMax && tilt > TiltMin && tilt < TiltMax;
}
