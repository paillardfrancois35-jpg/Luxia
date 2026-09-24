namespace Dmx.Core.Dmx;

/// <summary>
/// Trame d'un univers : 512 octets, canal 1 à l'indice 0.
/// Classe mutable réutilisée d'un tick à l'autre pour ne pas allouer (doc 03 §4.1).
/// </summary>
public sealed class DmxFrame
{
    private readonly byte[] _values = new byte[DmxConstants.ChannelCount];

    /// <summary>Valeurs brutes (indice 0 = canal 1).</summary>
    public Span<byte> Values => _values;

    /// <summary>Valeurs en lecture seule.</summary>
    public ReadOnlySpan<byte> ReadOnlyValues => _values;

    /// <summary>Valeur d'un canal numéroté de 1 à 512.</summary>
    public byte this[int channel]
    {
        get => _values[ToIndex(channel)];
        set => _values[ToIndex(channel)] = value;
    }

    /// <summary>Met tous les canaux à 0.</summary>
    public void Clear() => Array.Clear(_values);

    /// <summary>Copie cette trame dans <paramref name="destination"/>.</summary>
    public void CopyTo(DmxFrame destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        _values.CopyTo(destination._values, 0);
    }

    /// <summary>Copie des valeurs dans un nouveau tableau (hors code temps réel).</summary>
    public byte[] ToArray() => (byte[])_values.Clone();

    private static int ToIndex(int channel)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(channel, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(channel, DmxConstants.ChannelCount);
        return channel - 1;
    }
}
