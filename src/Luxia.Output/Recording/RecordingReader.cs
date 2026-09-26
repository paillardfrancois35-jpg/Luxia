using System.Buffers.Binary;

namespace Luxia.Output.Recording;

/// <summary>Relit un fichier <c>.dmxrec</c> (SORT-060, T-SORT-03 ; base du futur pilote Lecteur SORT-063).</summary>
public static class RecordingReader
{
    /// <summary>Lit l'en-tête puis toutes les trames. Une fin de fichier tronquée (arrêt brutal) est tolérée.</summary>
    public static (RecordingHeader Header, IReadOnlyList<RecordedFrame> Frames) ReadAll(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var header = ReadHeader(stream);
        var frames = new List<RecordedFrame>();
        var previous = new byte[header.ChannelCount];
        Span<byte> recordHeader = stackalloc byte[6];

        while (TryReadExactly(stream, recordHeader))
        {
            var elapsed = TimeSpan.FromMilliseconds(BinaryPrimitives.ReadUInt32LittleEndian(recordHeader));
            var length = BinaryPrimitives.ReadUInt16LittleEndian(recordHeader[4..]);
            if (length > 0)
            {
                var values = new byte[header.ChannelCount];
                if (length > values.Length || !TryReadExactly(stream, values.AsSpan(0, length)))
                {
                    break;
                }

                previous = values;
            }

            frames.Add(new RecordedFrame(elapsed, (byte[])previous.Clone()));
        }

        return (header, frames);
    }

    /// <summary>Lit un fichier.</summary>
    public static (RecordingHeader Header, IReadOnlyList<RecordedFrame> Frames) ReadAll(string path)
    {
        using var stream = File.OpenRead(path);
        return ReadAll(stream);
    }

    private static RecordingHeader ReadHeader(Stream stream)
    {
        Span<byte> header = stackalloc byte[24];
        if (!TryReadExactly(stream, header) || !header[..6].SequenceEqual(RecordingFormat.Magic))
        {
            throw new InvalidDataException("Ce fichier n'est pas un enregistrement de trames DMX.");
        }

        var version = BinaryPrimitives.ReadUInt16LittleEndian(header[6..]);
        if (version > RecordingFormat.Version)
        {
            throw new InvalidDataException($"Version d'enregistrement {version} non prise en charge.");
        }

        return new RecordingHeader(
            version,
            BinaryPrimitives.ReadUInt16LittleEndian(header[8..]),
            BinaryPrimitives.ReadSingleLittleEndian(header[10..]),
            new DateTime(BinaryPrimitives.ReadInt64LittleEndian(header[14..]), DateTimeKind.Utc),
            BinaryPrimitives.ReadUInt16LittleEndian(header[22..]));
    }

    private static bool TryReadExactly(Stream stream, Span<byte> buffer)
    {
        var read = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
        return read == buffer.Length;
    }
}
