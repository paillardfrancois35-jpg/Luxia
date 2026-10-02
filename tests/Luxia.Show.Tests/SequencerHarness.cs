using Luxia.Core.Dmx;
using Luxia.Core.Time;
using Luxia.Engine;
using Luxia.Engine.Model;
using Luxia.Messaging.Commands;
using Luxia.Messaging.Events;
using Luxia.Show.Model;
using Luxia.Show.Runtime;

namespace Luxia.Show.Tests;

/// <summary>
/// Moteur réel en temps virtuel (40 Hz) avec le séquenceur branché : quatre couches (Couleurs exclusive, Mouvements, Effets,
/// Ambiance non exclusive) et des scènes à une étape, sans appareil (on observe les lectures et les événements).
/// </summary>
internal sealed class SequencerHarness
{
    public static readonly TimeSpan Period = TimeSpan.FromMilliseconds(25);

    public SequencerHarness(int seed = 7)
    {
        Colors = Layer("Couleurs", 1);
        Movements = Layer("Mouvements", 2);
        Effects = Layer("Effets", 3);
        Atmosphere = Layer("Ambiance", 4, exclusive: false);
        Sequencer = new Sequencer(seed);
        Engine = new RenderEngine(new NullSink(), Clock, 1, null, Bus, seed);
        Engine.SetSequencer(Sequencer);
    }

    public VirtualClock Clock { get; } = new();

    public RecordingBus Bus { get; } = new();

    public RenderEngine Engine { get; }

    public Sequencer Sequencer { get; }

    public EngineLayer Colors { get; }

    public EngineLayer Movements { get; }

    public EngineLayer Effects { get; }

    public EngineLayer Atmosphere { get; }

    public List<EngineLayer> Layers { get; } = [];

    public List<EngineScene> Scenes { get; } = [];

    public List<Sequence> Sequences { get; } = [];

    public List<ShowDefinition> Shows { get; } = [];

    public double Seconds => Clock.Now.TotalSeconds;

    public EngineScene Scene(string name, EngineLayer layer, double fadeOut = 0)
    {
        var scene = new EngineScene
        {
            Id = Guid.NewGuid(),
            Name = name,
            LayerId = layer.Id,
            FadeOut = Duration.FromSeconds(fadeOut),
            Steps = [new EngineStep { Fade = Duration.Zero, Hold = Duration.FromSeconds(1) }],
        };
        Scenes.Add(scene);
        return scene;
    }

    /// <summary>Charge scènes, séquences et shows, règle le tempo et fait un tick.</summary>
    public void Load(double bpm = 120)
    {
        Engine.LoadShow(new ShowModel([], Layers, Scenes));
        Sequencer.Load(new SequenceSet { Sequences = [.. Sequences] }, new ShowSet { Shows = [.. Shows] });
        Engine.Send(new SetTempoSourceCommand(CommandOrigin.Tool, TempoSourceKind.Fixed, bpm));
        Engine.Tick();
    }

    public void Send(Command command) => Engine.Send(command);

    public void Tick()
    {
        Clock.Advance(Period);
        Engine.Tick();
    }

    public void Run(double seconds)
    {
        var ticks = (int)Math.Round(seconds / Period.TotalSeconds);
        for (var i = 0; i < ticks; i++)
        {
            Tick();
        }
    }

    /// <summary>Avance jusqu'à l'instant donné (secondes depuis le début).</summary>
    public void RunTo(double seconds)
    {
        while (Seconds < seconds - 1e-9)
        {
            Tick();
        }
    }

    public bool Playing(EngineScene scene) =>
        Engine.Snapshot.Playbacks.Any(p => p.SceneId == scene.Id && p.State is not (PlaybackState.FadingOut or PlaybackState.Done));

    public double LayerLevel(EngineLayer layer) => Engine.Snapshot.LayerMasters[Layers.IndexOf(layer)];

    /// <summary>Instants (secondes) où la scène a démarré.</summary>
    public IReadOnlyList<double> Starts(EngineScene scene) => [.. Bus.Of<SceneStarted>().Where(e => e.SceneId == scene.Id).Select(e => e.At.TotalSeconds)];

    /// <summary>Étapes de show activées, dans l'ordre.</summary>
    public IReadOnlyList<string> Steps => [.. Bus.Of<ShowStepActivated>().Select(e => e.StepId)];

    public IReadOnlyList<string> ActiveSteps(ShowDefinition show) =>
        [.. Sequencer.State.Shows.FirstOrDefault(s => s.ShowId == show.Id)?.ActiveSteps.Select(s => s.Id) ?? []];

    private EngineLayer Layer(string name, int priority, bool exclusive = true)
    {
        var layer = new EngineLayer { Id = Guid.NewGuid(), Name = name, Priority = priority, Exclusive = exclusive, CrossFade = Duration.Zero };
        Layers.Add(layer);
        return layer;
    }

    private sealed class NullSink : IFrameSink
    {
        public void Submit(int universe, DmxFrame frame, TimeSpan timestamp)
        {
        }
    }
}

/// <summary>Bus synchrone qui garde tout ce qui est publié.</summary>
internal sealed class RecordingBus : IEventBus
{
    private readonly List<object> _events = [];

    public IReadOnlyList<T> Of<T>() => [.. _events.OfType<T>()];

    public void Publish<TEvent>(TEvent evt)
        where TEvent : class => _events.Add(evt);

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : class => throw new NotSupportedException();
}
