namespace Luxia.Engine.Model;

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
