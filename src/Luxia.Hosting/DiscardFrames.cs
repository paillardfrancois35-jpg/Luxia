using Luxia.Core.Dmx;

namespace Luxia.Hosting;

/// <summary>Destination qui ignore les trames : celles du moteur d'aperçu ne doivent jamais atteindre une sortie (GEN-063).</summary>
internal sealed class DiscardFrames : IFrameSink
{
    public static readonly DiscardFrames Instance = new();

    public void Submit(int universe, DmxFrame frame, TimeSpan timestamp)
    {
    }
}
