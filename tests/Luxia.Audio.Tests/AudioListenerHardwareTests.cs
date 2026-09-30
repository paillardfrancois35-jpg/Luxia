using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Luxia.Audio.Tests;

/// <summary>Essai de robustesse sur le matériel réel (AUD-006) : arrêts et redémarrages de l'écoute pendant qu'un son joue.</summary>
public sealed class AudioListenerHardwareTests
{
    [Fact]
    [Trait("Categorie", "Materiel")]
    [Trait("Exigence", "AUD-006")]
    public async Task ToggleWhileSoundPlays_DoesNotHang()
    {
        var lines = new List<string>();
        using var output = new WasapiOut();
        var tone = new SignalGenerator(44100, 2) { Gain = 0.05, Frequency = 220, Type = SignalGeneratorType.Square };
        output.Init(tone);
        output.Play();
        using var listener = new AudioListener(new WasapiSourceFactory(), NullLogger.Instance);
        var done = Task.Run(() =>
        {
            for (var i = 0; i < 40; i++)
            {
                var w = Stopwatch.StartNew();
                listener.Start();
                Thread.Sleep(150 + (i * 7 % 50));
                listener.Stop();
                lines.Add($"cycle {i} : démarrage + arrêt {w.ElapsedMilliseconds} ms");
            }
        });
        var finished = await Task.WhenAny(done, Task.Delay(TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken)) == done;
        lines.Add(finished ? "TERMINE" : "BLOQUE (interblocage)");
        File.WriteAllLines(Path.Combine(Path.GetTempPath(), "luxia-stress.txt"), lines);
        output.Stop();
        finished.ShouldBeTrue();
    }
}
