using Luxia.Core.Dmx;
using Luxia.Output.Drivers;
using Luxia.Output.Recording;

namespace Luxia.Output.Tests;

/// <summary>T-SORT-03 : aller-retour Enregistreur / relecture à l'octet et à la milliseconde près.</summary>
public sealed class RecordingTests
{
    [Fact]
    [Trait("Exigence", "SORT-060")]
    public void WriteThenRead_RoundTripsFramesAndTimestamps()
    {
        using var stream = new MemoryStream();
        var started = new DateTime(2026, 9, 24, 20, 0, 0, DateTimeKind.Utc);
        var written = new List<(TimeSpan, byte[])>();
        var random = new Random(42);

        using (var writer = new RecordingWriter(new NonClosingStream(stream), 1, 40, started))
        {
            for (var i = 0; i < 400; i++)
            {
                var frame = new byte[512];
                if (i % 3 != 0)
                {
                    random.NextBytes(frame); // une trame sur trois identique à la précédente
                }
                else if (written.Count > 0)
                {
                    written[^1].Item2.CopyTo(frame, 0);
                }

                var timestamp = TimeSpan.FromMilliseconds(1000 + (i * 25));
                writer.Append(frame, timestamp);
                written.Add((timestamp, frame));
            }
        }

        stream.Position = 0;
        var (header, frames) = RecordingReader.ReadAll(stream);

        header.Universe.ShouldBe((ushort)1);
        header.RateHz.ShouldBe(40f);
        header.StartedUtc.ShouldBe(started);
        frames.Count.ShouldBe(400);
        for (var i = 0; i < 400; i++)
        {
            frames[i].Elapsed.ShouldBe(written[i].Item1 - written[0].Item1);
            frames[i].Values.ShouldBe(written[i].Item2);
        }
    }

    [Fact]
    [Trait("Exigence", "SORT-060")]
    public void IdenticalFrames_AreStoredCompactly()
    {
        using var stream = new MemoryStream();
        using (var writer = new RecordingWriter(new NonClosingStream(stream), 1, 40, DateTime.UtcNow))
        {
            for (var i = 0; i < 100; i++)
            {
                writer.Append(new byte[512], TimeSpan.FromMilliseconds(i * 25));
            }
        }

        // En-tête 24 + 1 trame complète (6 + 512) + 99 trames répétées (6).
        stream.Length.ShouldBe(24 + 518 + (99 * 6));
    }

    [Fact]
    public void TruncatedFile_ReadsCompleteFramesOnly()
    {
        using var stream = new MemoryStream();
        using (var writer = new RecordingWriter(new NonClosingStream(stream), 1, 40, DateTime.UtcNow))
        {
            writer.Append(new byte[512], TimeSpan.Zero);
            var frame = new byte[512];
            frame[0] = 1;
            writer.Append(frame, TimeSpan.FromMilliseconds(25));
        }

        var truncated = new MemoryStream(stream.ToArray()[..^100]);

        RecordingReader.ReadAll(truncated).Frames.Count.ShouldBe(1);
    }

    [Fact]
    public void NotARecording_Throws() =>
        Should.Throw<InvalidDataException>(() => RecordingReader.ReadAll(new MemoryStream([1, 2, 3])));

    [Fact]
    [Trait("Exigence", "SORT-060")]
    [Trait("Exigence", "SORT-061")]
    public async Task RecorderDriver_AttachedHot_WritesFramesToFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dmx-test-{Guid.NewGuid():N}.dmxrec");
        try
        {
            using (var router = new OutputRouter())
            {
                var recorder = new RecorderOutputDriver(path, 1, 40);
                router.Attach(1, recorder);
                await OutputRouterTests.Eventually(() => recorder.Status.State == Messaging.Events.OutputConnectionState.Connected);

                var frame = new DmxFrame();
                for (var i = 0; i < 10; i++)
                {
                    frame[1] = (byte)i;
                    router.Submit(1, frame, TimeSpan.FromMilliseconds(i * 25));
                    await Task.Delay(30);
                }

                router.Detach(recorder);
            }

            var (_, frames) = RecordingReader.ReadAll(path);
            frames.Count.ShouldBeGreaterThanOrEqualTo(9);
            frames[^1].Values[0].ShouldBe((byte)9);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class NonClosingStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => inner.CanSeek;

        public override bool CanWrite => inner.CanWrite;

        public override long Length => inner.Length;

        public override long Position { get => inner.Position; set => inner.Position = value; }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

        public override void SetLength(long value) => inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);

        public override void Write(ReadOnlySpan<byte> buffer) => inner.Write(buffer);
    }
}
