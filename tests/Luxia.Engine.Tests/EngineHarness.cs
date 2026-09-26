using Luxia.Core.Time;
using Luxia.Engine.Model;
using Luxia.Messaging.Commands;
using Luxia.Messaging.Events;

namespace Luxia.Engine.Tests;

/// <summary>Moteur en temps virtuel (GEN-033) à 40 Hz, avec un modèle chargé et les trames capturées.</summary>
internal sealed class EngineHarness
{
    public static readonly TimeSpan Period = TimeSpan.FromMilliseconds(25);

    public EngineHarness(ShowModel show, int seed = 1, IEventBus? bus = null)
    {
        Engine = new RenderEngine(Sink, Clock, 1, null, bus, seed);
        Engine.LoadShow(show);
        Engine.Tick();
    }

    public VirtualClock Clock { get; } = new();

    public CapturingSink Sink { get; } = new();

    public RenderEngine Engine { get; }

    /// <summary>Octet émis sur un canal (1 à 512) au dernier tick.</summary>
    public byte this[int channel] => Sink.Last()[channel - 1];

    public void Send(Command command) => Engine.Send(command);

    public void Launch(EngineScene scene, double? fade = null, bool solo = false) =>
        Engine.Send(new LaunchSceneCommand(CommandOrigin.Tool, scene.Id, null, fade is { } f ? TimeSpan.FromSeconds(f) : null, solo));

    public void Stop(EngineScene scene, double? fade = null) =>
        Engine.Send(new StopSceneCommand(CommandOrigin.Tool, scene.Id, fade is { } f ? TimeSpan.FromSeconds(f) : null));

    /// <summary>Un tick, 25 ms plus tard.</summary>
    public void Tick()
    {
        Clock.Advance(Period);
        Engine.Tick();
    }

    /// <summary>Ticks pendant la durée donnée.</summary>
    public void Run(double seconds)
    {
        var ticks = (int)Math.Round(seconds / Period.TotalSeconds);
        for (var i = 0; i < ticks; i++)
        {
            Tick();
        }
    }

    /// <summary>Valeur logique finale d'un paramètre au dernier tick.</summary>
    public double Value(int parameter) => Engine.Snapshot.Values[parameter];

    public PlaybackInfo? Playback(EngineScene scene) =>
        Engine.Snapshot.Playbacks.Where(p => p.SceneId == scene.Id).Select(p => (PlaybackInfo?)p).LastOrDefault();
}
