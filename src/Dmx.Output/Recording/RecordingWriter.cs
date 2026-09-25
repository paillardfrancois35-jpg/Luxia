using System.Buffers.Binary;
using Dmx.Core.Dmx;

namespace Dmx.Output.Recording;

/// <summary>Écrit un fichier <c>.dmxrec</c> (SORT-060).</summary>
public sealed class RecordingWriter : IDisposable
{
    private readonly Stream _stream;
    private readonly byte[] _previous = new byte[DmxConstants.ChannelCount];
    private readonly byte[] _recordHeader = new byte[6];
    private TimeSpan? _first;
    private bool _hasPrevious;

    /// <summary>Crée le fichier et écrit l'en-tête.</summary>
    public RecordingWriter(Stream stream, int universe, double rateHz, DateTime startedUtc)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _stream = stream;

        Span<byte> header = stackalloc byte[6 + 2 + 2 + 4 + 8 + 2];
        RecordingFormat.Magic.CopyTo(header);
        BinaryPrimitives.WriteUInt16LittleEndian(header[6..], RecordingFormat.Version);
        BinaryPrimitives.WriteUInt16LittleEndian(header[8..], checked((ushort)universe));
        BinaryPrimitives.WriteSingleLittleEndian(header[10..], (float)rateHz);
        BinaryPrimitives.WriteInt64LittleEndian(header[14..], startedUtc.ToUniversalTime().Ticks);
        BinaryPrimitives.WriteUInt16LittleEndian(header[22..], DmxConstants.ChannelCount);
        _stream.Write(header);
    }

    /// <summary>Nombre de trames écrites.</summary>
    public long FrameCount { get; private set; }

    /// <summary>Ajoute une trame horodatée (horloge du moteur).</summary>
    public void Append(ReadOnlySpan<byte> frame, TimeSpan timestamp)
    {
        _first ??= timestamp;
        var elapsedMs = (uint)Math.Max(0, Math.Round((timestamp - _first.Value).TotalMilliseconds));
        var same = _hasPrevious && frame.SequenceEqual(_previous);

        BinaryPrimitives.WriteUInt32LittleEndian(_recordHeader, elapsedMs);
        BinaryPrimitives.WriteUInt16LittleEndian(_recordHeader.AsSpan(4), same ? (ushort)0 : (ushort)frame.Length);
        _stream.Write(_recordHeader);
        if (!same)
        {
            _stream.Write(frame);
            frame.CopyTo(_previous);
            _hasPrevious = true;
        }

        FrameCount++;
    }

    /// <summary>Force l'écriture sur disque.</summary>
    public void Flush() => _stream.Flush();

    /// <inheritdoc />
    public void Dispose() => _stream.Dispose();
}
