namespace Dmx.Output.Arduino;

/// <summary>Message reçu.</summary>
/// <param name="Label">Label Enttec.</param>
/// <param name="Data">Données.</param>
public sealed record EnttecMessage(byte Label, byte[] Data);

/// <summary>
/// Décodeur de flux Enttec octet par octet, avec les mêmes règles que le firmware (doc 10 §5.4) :
/// resynchronisation sur 0x7E, rejet si longueur &gt; 513 ou octet final différent de 0xE7.
/// </summary>
public sealed class EnttecMessageParser
{
    private enum State
    {
        WaitStart,
        Label,
        LengthLow,
        LengthHigh,
        Data,
        WaitEnd,
    }

    private readonly byte[] _buffer = new byte[EnttecProtocol.MaxDataLength];
    private State _state = State.WaitStart;
    private byte _label;
    private int _length;
    private int _received;

    /// <summary>Nombre de messages rejetés (longueur ou fin invalide).</summary>
    public int RejectedCount { get; private set; }

    /// <summary>Traite un octet ; renvoie un message quand il est complet et valide.</summary>
    public EnttecMessage? Feed(byte b)
    {
        switch (_state)
        {
            case State.WaitStart:
                if (b == EnttecProtocol.StartOfMessage)
                {
                    _state = State.Label;
                }

                return null;

            case State.Label:
                _label = b;
                _state = State.LengthLow;
                return null;

            case State.LengthLow:
                _length = b;
                _state = State.LengthHigh;
                return null;

            case State.LengthHigh:
                _length |= b << 8;
                _received = 0;
                if (_length > EnttecProtocol.MaxDataLength)
                {
                    RejectedCount++;
                    _state = State.WaitStart;
                }
                else
                {
                    _state = _length == 0 ? State.WaitEnd : State.Data;
                }

                return null;

            case State.Data:
                _buffer[_received++] = b;
                if (_received == _length)
                {
                    _state = State.WaitEnd;
                }

                return null;

            case State.WaitEnd:
            default:
                _state = State.WaitStart;
                if (b != EnttecProtocol.EndOfMessage)
                {
                    RejectedCount++;
                    return null;
                }

                return new EnttecMessage(_label, _buffer.AsSpan(0, _length).ToArray());
        }
    }

    /// <summary>Traite une suite d'octets et renvoie les messages complets.</summary>
    public IReadOnlyList<EnttecMessage> FeedAll(ReadOnlySpan<byte> bytes)
    {
        var messages = new List<EnttecMessage>();
        foreach (var b in bytes)
        {
            if (Feed(b) is { } message)
            {
                messages.Add(message);
            }
        }

        return messages;
    }
}
