using System.Globalization;

namespace Dmx.Core.Dmx;

/// <summary>Plage de canaux inclusive, de <see cref="First"/> à <see cref="Last"/> (1 à 512).</summary>
public readonly record struct ChannelRange
{
    /// <summary>Crée une plage validée.</summary>
    public ChannelRange(int first, int last)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(first, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(last, DmxConstants.ChannelCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(first, last);
        First = first;
        Last = last;
    }

    /// <summary>Premier canal.</summary>
    public int First { get; }

    /// <summary>Dernier canal (inclus).</summary>
    public int Last { get; }

    /// <summary>Nombre de canaux.</summary>
    public int Count => Last - First + 1;

    /// <summary>Indique si le canal appartient à la plage.</summary>
    public bool Contains(int channel) => channel >= First && channel <= Last;

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{First}-{Last}");

    /// <summary>Lit une plage « 1-16 » ou un canal seul « 5 ».</summary>
    public static bool TryParse(string? text, out ChannelRange range)
    {
        range = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts = text.Split('-', StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 2
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var first))
        {
            return false;
        }

        var last = first;
        if (parts.Length == 2 && !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out last))
        {
            return false;
        }

        if (first < 1 || last > DmxConstants.ChannelCount || first > last)
        {
            return false;
        }

        range = new ChannelRange(first, last);
        return true;
    }
}
