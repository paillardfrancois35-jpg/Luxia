using Luxia.Core.Dmx;
using Luxia.Messaging.Commands;

namespace Luxia.Engine;

/// <summary>
/// Surcharges brutes de la console pour un univers (étape 11 de la chaîne de rendu, CONS-003).
/// Modifiées uniquement par le fil du moteur ; l'interface en lit une copie.
/// </summary>
internal sealed class ChannelOverrides
{
    private readonly bool[] _active = new bool[DmxConstants.ChannelCount];
    private readonly byte[] _values = new byte[DmxConstants.ChannelCount];
    private readonly Lock _lock = new();
    private int _count;

    public int Count => Volatile.Read(ref _count);

    public void Set(IReadOnlyList<ChannelValue> values)
    {
        lock (_lock)
        {
            foreach (var (channel, value) in values)
            {
                if (channel is < 1 or > DmxConstants.ChannelCount)
                {
                    continue;
                }

                if (!_active[channel - 1])
                {
                    _active[channel - 1] = true;
                    _count++;
                }

                _values[channel - 1] = value;
            }
        }
    }

    public void Release(IReadOnlyList<int>? channels)
    {
        lock (_lock)
        {
            if (channels is null)
            {
                Array.Clear(_active);
                _count = 0;
                return;
            }

            foreach (var channel in channels)
            {
                if (channel is >= 1 and <= DmxConstants.ChannelCount && _active[channel - 1])
                {
                    _active[channel - 1] = false;
                    _count--;
                }
            }
        }
    }

    /// <summary>Étape 11 : les canaux surchargés remplacent la valeur calculée.</summary>
    public void ApplyTo(DmxFrame frame)
    {
        if (Volatile.Read(ref _count) == 0)
        {
            return;
        }

        var values = frame.Values;
        lock (_lock)
        {
            for (var i = 0; i < DmxConstants.ChannelCount; i++)
            {
                if (_active[i])
                {
                    values[i] = _values[i];
                }
            }
        }
    }

    /// <summary>Copie l'état : -1 = canal libre, sinon valeur imposée.</summary>
    public void CopyTo(Span<short> destination)
    {
        lock (_lock)
        {
            for (var i = 0; i < DmxConstants.ChannelCount; i++)
            {
                destination[i] = _active[i] ? _values[i] : (short)-1;
            }
        }
    }
}
