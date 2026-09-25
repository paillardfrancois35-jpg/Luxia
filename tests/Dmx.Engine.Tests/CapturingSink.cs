using Dmx.Core.Dmx;

namespace Dmx.Engine.Tests;

/// <summary>Destination de test : conserve une copie de chaque trame reçue.</summary>
internal sealed class CapturingSink : IFrameSink
{
    public List<(int Universe, byte[] Values, TimeSpan Timestamp)> Frames { get; } = [];

    public byte[] Last(int universe = 1) => Frames.Last(f => f.Universe == universe).Values;

    public void Submit(int universe, DmxFrame frame, TimeSpan timestamp) =>
        Frames.Add((universe, frame.ToArray(), timestamp));
}
