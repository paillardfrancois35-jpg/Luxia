using System.Collections.Concurrent;
using System.Globalization;
using Luxia.Core.Dmx;
using Luxia.Core.Time;
using Luxia.Engine.Model;
using Luxia.Engine.Timing;
using Luxia.Messaging.Commands;
using Luxia.Messaging.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Luxia.Engine;

/// <summary>
/// Moteur de rendu (doc 15). À chaque <see cref="Tick"/> : applique les commandes reçues, fait avancer les scènes,
/// fusionne les couches, applique surcharges, Grand Master et blackout, convertit en octets et remet une trame par univers
/// au <see cref="IFrameSink"/>. Ne dépend d'aucune interface ni d'aucun pilote (GEN-001) : le cadencement est fourni
/// de l'extérieur (<see cref="Timing.TickLoop"/> en temps réel, appel direct en temps virtuel).
/// </summary>
/// <remarks>
/// Le moteur calcule sur des <b>paramètres</b> (un par attribut d'appareil patché) compilés hors de son fil
/// (<see cref="ShowModel"/>, D26) : il ne connaît ni la bibliothèque, ni le patch, ni les palettes.
/// Chaîne de rendu (doc 02 §9) : 1 valeurs par défaut · 2-3 couches et masters · 5 surcharges d'attributs ·
/// 6 intensité virtuelle et Grand Master · 7 blackout · 10 conversion en octets · 11 surcharges brutes de la console
/// (soumises au blackout, GEN-042) · puis test de sortie (D21) · 9 sûreté : zones interdites sur les paramètres, strobe et fumée
/// sur les octets finaux, pour qu'aucune surcharge ni aucun test ne les contourne (GEN-042, MOT-080 à 083).
/// </remarks>
public sealed partial class RenderEngine : ICommandSink
{
    private const int CommandLogCapacity = 500;

    private readonly IFrameSink _sink;
    private readonly IClock _clock;
    private readonly ILogger _logger;
    private readonly IEventBus? _bus;
    private readonly ConcurrentQueue<(Command Command, TimeSpan ReceivedAt)> _pending = new();
    private readonly DmxFrame[] _frames;
    private readonly DmxFrame[] _published;
    private readonly Lock _publishedLock = new();
    private readonly TestPattern _testPattern = new();
    private readonly SafetyLimiter _safety = new();
    private readonly Action<SafetyLimitReached> _publishSafety;
    private readonly ChannelOverrides[] _channelOverrides;
    private readonly Random _random;
    private readonly List<Playback> _playbacks = new(64);
    private readonly List<Playback> _scratch = new(16);
    private readonly List<Playback> _chains = new(8);
    private readonly CommandLogEntry[] _log = new CommandLogEntry[CommandLogCapacity];
    private readonly Lock _logLock = new();
    private int _logNext;
    private int _logCount;
    private long _tickCount;
    private long _sequence;
    private long _sequenceAtTickStart;
    private TimeSpan? _lastTick;
    private readonly MusicalClock _tempo = new();
    private readonly TickEvents _events = new();
    private readonly List<PendingLaunch> _quantized = [];
    private PendingSceneLaunch[] _publishedPending = [];
    private IAudioFeed? _audioFeed;
    private TempoInfo _publishedTempo = TempoInfo.Default;

    // Modèle courant et tableaux alignés sur ses paramètres (réalloués seulement au chargement d'un modèle).
    private ShowModel _show = ShowModel.Empty;
    private double[] _defaults = [];
    private double[] _result = [];
    private double[] _output = [];
    private double[] _overrides = [];
    private bool[] _touched = [];
    private ParameterSource[] _sources = [];
    private ParameterRole[] _roles = [];
    private int[] _intensitySource = [];
    private double[] _layerMasters = [];
    private double[] _dimmerLevels = [];
    private double[] _dimmerEffective = [];
    private int[] _parameterGroup = [];
    private (int Parameter, ChannelAddress Address)[][] _outputSlots;
    private bool[][] _blackoutMasks;
    private bool _blackout;
    private double _grandMaster = 1;
    private double[] _frozen = [];
    private bool _frozenActive;
    private bool _freezeCapture;
    private bool _freezeSuspend;
    private Playback? _shownStep;

    // État publié pour l'interface (MOT-100), recopié à la fin de chaque tick.
    private ShowModel _publishedShow = ShowModel.Empty;
    private bool _publishedBlackout;
    private bool _publishedFrozen;
    private bool _publishedSmoking;
    private double _publishedSmokeRest;
    private double _publishedGrandMaster = 1;
    private double[] _publishedValues = [];
    private ParameterSource[] _publishedSources = [];
    private double[] _publishedOverrides = [];
    private PlaybackInfo[] _publishedPlaybacks = new PlaybackInfo[64];
    private int _publishedPlaybackCount;
    private double[] _publishedLayerMasters = [];
    private double[] _publishedDimmerLevels = [];
    private double[] _publishedDimmerEffective = [];
    private ActiveLimit[] _publishedLimits = [];

    /// <summary>Crée un moteur.</summary>
    /// <param name="sink">Destination des trames.</param>
    /// <param name="clock">Horloge (réelle ou virtuelle).</param>
    /// <param name="universeCount">Nombre d'univers calculés.</param>
    /// <param name="logger">Journal.</param>
    /// <param name="bus">Bus d'événements (EVT-010, EVT-011) ; facultatif.</param>
    /// <param name="seed">Graine du générateur aléatoire (MOT-004) ; <c>null</c> = tirée au hasard, et journalisée.</param>
    public RenderEngine(IFrameSink sink, IClock clock, int universeCount = 1, ILogger<RenderEngine>? logger = null, IEventBus? bus = null, int? seed = null)
    {
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentOutOfRangeException.ThrowIfLessThan(universeCount, 1);
        _sink = sink;
        _clock = clock;
        _bus = bus;
        _logger = logger ?? NullLogger<RenderEngine>.Instance;
        _frames = [.. Enumerable.Range(0, universeCount).Select(_ => new DmxFrame())];
        _published = [.. Enumerable.Range(0, universeCount).Select(_ => new DmxFrame())];
        _channelOverrides = [.. Enumerable.Range(0, universeCount).Select(_ => new ChannelOverrides())];
        _outputSlots = [.. Enumerable.Range(0, universeCount).Select(_ => Array.Empty<(int, ChannelAddress)>())];
        _blackoutMasks = [.. Enumerable.Range(0, universeCount).Select(_ => new bool[DmxConstants.ChannelCount])];
        _publishSafety = PublishSafety;

        // MOT-004 : tout tirage aléatoire passe par ce générateur ; la graine est journalisée pour rejouer une session.
        Seed = seed ?? Environment.TickCount;
        _random = new Random(Seed);
        _logger.LogInformation("Moteur : graine aléatoire de la session {Graine}", Seed);
    }

    /// <summary>Nombre d'univers calculés.</summary>
    public int UniverseCount => _frames.Length;

    /// <summary>Nombre de ticks exécutés.</summary>
    public long TickCount => Interlocked.Read(ref _tickCount);

    /// <summary>Graine du générateur aléatoire de la session (MOT-004).</summary>
    public int Seed { get; }

    /// <summary>État du test de sortie (pour l'affichage).</summary>
    public TestPatternState TestState => _testPattern.State;

    /// <summary>
    /// Tempo utilisé pour convertir les durées musicales (GEN-023, MOT-016) : celui de l'horloge musicale (120 BPM fixes
    /// par défaut). L'écrire revient à choisir la source Fixe ; les commandes CMD-040 à 042 règlent l'horloge en service.
    /// </summary>
    public double Bpm
    {
        get => _tempo.Bpm;
        set => _tempo.SetFixed(value);
    }

    /// <summary>Horloge musicale (lecture seule pour les autres composants ; réglée par commandes).</summary>
    internal MusicalClock Tempo => _tempo;

    /// <summary>Dernier état publié (mis à jour à la fin de chaque tick).</summary>
    public EngineSnapshot Snapshot
    {
        get
        {
            lock (_publishedLock)
            {
                return BuildSnapshot();
            }
        }
    }

    /// <inheritdoc />
    public void Send(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _pending.Enqueue((command, _clock.Now));
    }

    /// <summary>
    /// Charge un nouveau modèle (ouverture de projet, modification d'une scène ou d'une palette, changement de lieu) :
    /// appliqué au tick suivant, dans l'ordre des commandes. Les scènes en cours continuent avec leur nouvelle version
    /// (PAL-005) ; les surcharges d'attributs sont conservées pour les attributs qui existent encore.
    /// </summary>
    public void LoadShow(ShowModel show)
    {
        ArgumentNullException.ThrowIfNull(show);
        _pending.Enqueue((new LoadShowRequest(show), _clock.Now));
    }

    /// <summary>Exécute un tick à l'instant donné par l'horloge.</summary>
    public void Tick()
    {
        var now = _clock.Now;
        var elapsed = _lastTick is { } last ? Math.Max(0, (now - last).TotalSeconds) : 0;
        _lastTick = now;
        _sequenceAtTickStart = _sequence;
        _tempo.Advance(elapsed);
        ReadAudio();

        // Le séquenceur peut agir dès les commandes de ce tick (lancer un show, une séquence) : il voit déjà l'instant et la musique.
        Host.Begin(now, Signals());

        // GEN-010 / GEN-011 : commandes appliquées au tick suivant leur réception, dans l'ordre d'arrivée.
        while (_pending.TryDequeue(out var item))
        {
            Apply(item.Command, item.ReceivedAt, now);
        }

        LaunchDueQuantized(now);

        // D37 : shows et séquences évoluent ici, après les commandes et avant l'avancement des scènes.
        TickSequencer(now);

        // GEN-032 : les scènes avancent du temps réellement écoulé, pas d'un nombre de ticks.
        // MOT-073 : figé avec lectures suspendues → elles n'avancent plus.
        AdvancePlaybacks(_frozenActive && _freezeSuspend ? 0 : elapsed, now);
        LaunchRestScenes(now);
        MergeLayers();
        ApplyMasters();

        // Étape 9 (paramètres) : zones interdites Pan/Tilt (MOT-082).
        _safety.BeginTick();
        _safety.ApplyZones(_output, _result, now, _publishSafety);

        for (var u = 0; u < _frames.Length; u++)
        {
            var frame = _frames[u];
            frame.Clear();

            // Étape 10 : conversion des paramètres en octets (MOT-090), appareils absents à 0 (MOT-091).
            WriteParameters(u, frame);

            // Étape 11 : surcharges brutes de la console (CONS-003), soumises au blackout (GEN-042, CONS-008).
            _channelOverrides[u].ApplyTo(frame, _blackout ? _blackoutMasks[u] : default);

            // Test de sortie : remplace toute la restitution de l'univers testé (D21).
            _testPattern.Render(u + 1, frame, now);
        }

        // Étape 9 (octets) : strobe et fumée, jamais contournables (GEN-042, MOT-080, MOT-081).
        _safety.ApplyToFrames(_frames, elapsed, now, _publishSafety);

        for (var u = 0; u < _frames.Length; u++)
        {
            lock (_publishedLock)
            {
                _frames[u].CopyTo(_published[u]);
            }

            _sink.Submit(u + 1, _frames[u], now);
        }

        Publish();
        PublishTempoChange(now);
        Interlocked.Increment(ref _tickCount);
    }

    /// <summary>Copie la dernière trame calculée d'un univers (lecture par l'interface, à son rythme).</summary>
    public void CopyLastFrame(int universe, Span<byte> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(universe, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(universe, _frames.Length);
        lock (_publishedLock)
        {
            _published[universe - 1].ReadOnlyValues.CopyTo(destination);
        }
    }

    /// <summary>Nombre de canaux surchargés dans un univers.</summary>
    public int OverrideCount(int universe) => _channelOverrides[CheckUniverse(universe) - 1].Count;

    /// <summary>Copie les surcharges d'un univers : -1 = canal libre, sinon valeur imposée.</summary>
    public void CopyOverrides(int universe, Span<short> destination) => _channelOverrides[CheckUniverse(universe) - 1].CopyTo(destination);

    /// <summary>
    /// Commande traitée (acceptée ou refusée), levé sur le fil du moteur : le gestionnaire doit être immédiat
    /// (journal de l'enregistrement, SORT-066).
    /// </summary>
    public event Action<CommandLogEntry>? CommandApplied;

    /// <summary>Dernières commandes reçues, de la plus ancienne à la plus récente (GEN-112).</summary>
    public IReadOnlyList<CommandLogEntry> CommandLog()
    {
        lock (_logLock)
        {
            var result = new CommandLogEntry[_logCount];
            var start = (_logNext - _logCount + CommandLogCapacity) % CommandLogCapacity;
            for (var i = 0; i < _logCount; i++)
            {
                result[i] = _log[(start + i) % CommandLogCapacity];
            }

            return result;
        }
    }

    private int CheckUniverse(int universe)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(universe, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(universe, _frames.Length);
        return universe;
    }

    private void Apply(Command command, TimeSpan receivedAt, TimeSpan now)
    {
        var rejection = ApplyCore(command, now, receivedAt);
        var entry = new CommandLogEntry(receivedAt, now, command, rejection);
        Log(entry);
        CommandApplied?.Invoke(entry);
        if (rejection is not null)
        {
            // GEN-012 : une commande refusée produit un événement avec le motif.
            _logger.LogWarning("{Commande} refusée (origine {Origine}) : {Motif}", command.GetType().Name, command.Origin, rejection);
            _bus?.Publish(new CommandRejected(command, rejection, now));
        }
    }

    private string? ApplyCore(Command command, TimeSpan now, TimeSpan receivedAt)
    {
        switch (command)
        {
            case LoadShowRequest load:
                ApplyShow(load.Show);
                return null;

            case OverrideChannelsCommand overrideCommand:
                if (overrideCommand.Universe < 1 || overrideCommand.Universe > _frames.Length)
                {
                    return string.Create(CultureInfo.CurrentCulture, $"univers {overrideCommand.Universe} inexistant");
                }

                _channelOverrides[overrideCommand.Universe - 1].Set(overrideCommand.Values);
                return null;

            case ReleaseOverridesCommand release:
                foreach (var (index, overrides) in _channelOverrides.Index())
                {
                    if (release.Universe is null || release.Universe == index + 1)
                    {
                        overrides.Release(release.Channels);
                    }
                }

                return null;

            case OverrideAttributesCommand attributes:
                return OverrideAttributes(attributes);

            case ReleaseAttributesCommand release:
                ReleaseAttributes(release);
                return null;

            case BlackoutCommand blackout:
                _blackout = blackout.Active;
                _logger.LogInformation("Blackout {Etat} (origine {Origine})", blackout.Active ? "activé" : "désactivé", blackout.Origin);
                return null;

            case SetGrandMasterCommand master:
                _grandMaster = Math.Clamp(master.Level, 0, 1);
                return null;

            case LaunchSceneCommand launch:
                return LaunchScene(launch, now);

            case StopSceneCommand stop:
                return StopScene(stop);

            case StopLayerCommand stopLayer:
                // « Tout arrêter » (écran, MIDI) arrête aussi shows et séquences : sinon un show relancerait ses scènes (D37).
                if (stopLayer.LayerId is null && stopLayer.Origin != CommandOrigin.Show && Volatile.Read(ref _sequencer) is not null)
                {
                    ApplySequencer(new StopShowCommand(stopLayer.Origin, KeepSecondary: !stopLayer.Everything));
                }

                return StopLayer(stopLayer);

            case SetLayerMasterCommand layerMaster:
                return SetLayerMaster(layerMaster);

            case SetGroupDimmerCommand groupDimmer:
                return SetGroupDimmer(groupDimmer);

            case FlashSceneCommand flash:
                return Flash(flash, now);

            case FreezeCommand freeze:
                _frozenActive = freeze.Active;
                _freezeCapture = freeze.Active;
                _freezeSuspend = freeze.Active && freeze.SuspendPlaybacks;
                _logger.LogInformation("Figer {Etat} (origine {Origine})", freeze.Active ? "activé" : "désactivé", freeze.Origin);
                return null;

            case SmokeCommand smoke:
                if (_show.Safety.SmokeChannels.Count == 0)
                {
                    return "aucune machine à fumée dans le patch";
                }

                _safety.SetManualSmoke(smoke.Pressed, smoke.Burst?.TotalSeconds, smoke.Level);
                return null;

            case StepSceneCommand step:
                return StepScene(step, now);

            case SetSceneSpeedCommand speed:
                return SetSceneSpeed(speed);

            case TapTempoCommand:
                _tempo.Tap(receivedAt.TotalSeconds, Math.Max(0, (now - receivedAt).TotalSeconds));
                return null;

            case SetTempoSourceCommand source:
                if (source.Bpm is { } fixedBpm)
                {
                    if (fixedBpm < MusicalClock.MinBpm || fixedBpm > MusicalClock.MaxBpm)
                    {
                        return $"BPM hors de {MusicalClock.MinBpm:0} à {MusicalClock.MaxBpm:0}";
                    }

                    _tempo.SetFixed(fixedBpm);
                }
                else
                {
                    _tempo.UseSource(source.Source);
                }

                return null;

            case AdjustTempoCommand adjust:
                switch (adjust.Adjustment)
                {
                    case TempoAdjustment.TimesTwo:
                        _tempo.Scale(2);
                        break;
                    case TempoAdjustment.DivideByTwo:
                        _tempo.Scale(0.5);
                        break;
                    case TempoAdjustment.AddBpm:
                        _tempo.Nudge(adjust.Value);
                        break;
                    case TempoAdjustment.FollowHeard:
                        _tempo.FollowHeard();
                        break;
                    default:
                        _tempo.ResyncBar();
                        break;
                }

                return null;

            case SetTempoLatencyCommand latency:
                _tempo.SetLatency(latency.Seconds);
                return null;

            case ShowStepCommand show:
                return ShowStep(show);

            case SequencerCommand sequencer:
                return ApplySequencer(sequencer);

            case SimulateMusicCommand simulate:
                Simulate(simulate);
                return null;

            case ForceStyleCommand forced:
                _forcedStyle = string.IsNullOrWhiteSpace(forced.Style) ? null : forced.Style.Trim();
                return null;

            case SetMusicContextCommand context:
                SetMusicContext(context);
                return null;

            case TestOutputCommand test:
                if (test.Universe < 1 || test.Universe > _frames.Length)
                {
                    return string.Create(CultureInfo.CurrentCulture, $"univers {test.Universe} inexistant");
                }

                _testPattern.Apply(test, now);
                _logger.LogInformation(
                    "TesterSortie {Etat} (origine {Origine}) : univers {Univers}, canaux {Plage}, exclus [{Exclus}], valeur {Valeur}",
                    test.Active ? "démarré" : "arrêté",
                    test.Origin,
                    test.Universe,
                    test.Range,
                    ChannelList.Format(test.ExcludedChannels),
                    test.Value);
                return null;

            default:
                return "commande non prise en charge par le moteur";
        }
    }

    private string? OverrideAttributes(OverrideAttributesCommand command)
    {
        var applied = 0;
        foreach (var value in command.Values)
        {
            var index = _show.IndexOf(value.FixtureId, value.ChannelKey);
            if (index < 0)
            {
                continue;
            }

            _overrides[index] = Math.Clamp(value.Value, 0, 1);
            applied++;
        }

        return applied == 0 && command.Values.Count > 0 ? "attribut inconnu (appareil non patché ou canal absent du mode)" : null;
    }

    private void ReleaseAttributes(ReleaseAttributesCommand command)
    {
        if (command.FixtureId is not { } fixture)
        {
            Array.Fill(_overrides, double.NaN);
            return;
        }

        var target = _show.Aliases.TryGetValue(fixture, out var alias) ? alias : fixture;
        var parameters = _show.Parameters;
        for (var i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].FixtureId == target && (command.ChannelKeys is null || command.ChannelKeys.Contains(parameters[i].ChannelKey)))
            {
                _overrides[i] = double.NaN;
            }
        }
    }

    private string? LaunchScene(LaunchSceneCommand command, TimeSpan now)
    {
        var scene = _show.Scene(command.SceneId);
        if (scene is null)
        {
            return "scène inconnue";
        }

        if (scene.Steps.Count == 0)
        {
            return $"la scène « {scene.Name} » n'a aucune étape";
        }

        var layerIndex = LayerIndexFor(command.LayerId ?? scene.LayerId);
        if (layerIndex < 0)
        {
            return "aucune couche pour jouer la scène";
        }

        if (command.StopIfPlaying && _quantized.RemoveAll(q => q.SceneId == scene.Id) > 0)
        {
            // Une seconde pression sur une scène qui attend son instant musical annule l'attente (MOT-018).
            return null;
        }

        if (command.StopIfPlaying)
        {
            // Bascule (LIVE-003) : décidée ici, sur l'état exact du moteur, pas sur celui qu'affiche l'écran.
            var stopped = false;
            foreach (var playback in _playbacks)
            {
                if (playback.Scene.Id == scene.Id && !playback.Flash && playback.State is not (PlaybackState.FadingOut or PlaybackState.Done))
                {
                    playback.BeginExit(playback.Scene.FadeOut?.ToSeconds(Bpm) ?? 0);
                    stopped = true;
                }
            }

            if (stopped)
            {
                _logger.LogInformation("Scène « {Scene} » arrêtée (origine {Origine})", scene.Name, command.Origin);
                return null;
            }
        }

        if (!command.Immediate && QuantizeTarget(scene.Quantize) is { } target)
        {
            // MOT-018 : la scène attend le prochain temps, la prochaine mesure ou la prochaine phrase.
            _quantized.RemoveAll(q => q.SceneId == scene.Id);
            _quantized.Add(new PendingLaunch(command, scene.Id, target));
            return null;
        }

        Launch(scene, layerIndex, command.Fade?.TotalSeconds, command.Origin, command.Solo, null, now);
        return null;
    }

    private string? Flash(FlashSceneCommand command, TimeSpan now)
    {
        var scene = _show.Scene(command.SceneId);
        if (scene is null)
        {
            return "scène inconnue";
        }

        if (!command.Pressed)
        {
            // Relâche : retour instantané (ou fondu court) à ce que jouent les couches (MOT-072).
            var fade = command.ReleaseFade?.TotalSeconds ?? 0;
            foreach (var playback in _playbacks)
            {
                if (playback.Flash && playback.Scene.Id == scene.Id)
                {
                    playback.BeginExit(fade);
                }
            }

            return null;
        }

        if (scene.Steps.Count == 0)
        {
            return $"la scène « {scene.Name} » n'a aucune étape";
        }

        var layerIndex = LayerIndexFor(scene.LayerId);
        if (layerIndex < 0)
        {
            return "aucune couche pour jouer la scène";
        }

        Launch(scene, layerIndex, 0, command.Origin, false, null, now, flash: true);
        return null;
    }

    private void Launch(EngineScene scene, int layerIndex, double? fade, CommandOrigin origin, bool solo, Playback? replaced, TimeSpan now, bool flash = false)
    {
        var layer = _show.Layers[layerIndex];
        var playback = new Playback(scene, layerIndex, ++_sequence, origin, solo) { Flash = flash, SessionSeed = unchecked((ulong)Seed), Clock = _tempo, Events = _events };
        playback.Bind(scene, _show);

        // Lectures remplacées : toute la couche si elle est exclusive (MOT-030), sinon une lecture de la même scène.
        // Un flash ne remplace qu'un flash de la même scène, et une scène lancée ne coupe pas un flash maintenu.
        _scratch.Clear();
        foreach (var other in _playbacks)
        {
            if (other.LayerIndex == layerIndex && other.State != PlaybackState.Done && other.Flash == flash
                && (other == replaced || other.Scene.Id == scene.Id || (layer.Exclusive && !flash)))
            {
                _scratch.Add(other);
            }
        }

        if (replaced is not null && !_scratch.Contains(replaced))
        {
            _scratch.Add(replaced);
        }

        var bpm = Bpm;
        double? entry;
        if (_scratch.Count > 0)
        {
            // Fondu croisé : la nouvelle scène reprend, attribut par attribut, la valeur de l'ancienne (doc 15 §5.1).
            entry = fade ?? layer.CrossFade.ToSeconds(bpm);
            for (var i = _scratch.Count - 1; i >= 0; i--)
            {
                playback.TakeOverFrom(_scratch[i]);
            }

            foreach (var old in _scratch)
            {
                old.BeginExit(entry.Value);
            }
        }
        else
        {
            entry = fade ?? scene.FadeIn?.ToSeconds(bpm);
        }

        playback.Start(flash ? 0 : entry, bpm);
        Insert(playback);
        _logger.LogInformation(
            "Scène « {Scene} » {Mode} dans la couche « {Couche} » (origine {Origine})",
            scene.Name,
            flash ? "en flash" : "lancée",
            layer.Name,
            origin);
        _bus?.Publish(new SceneStarted(scene.Id, scene.Name, layer.Id, origin, now));
    }

    /// <summary>Rang de fusion : les flashs au-dessus de toutes les couches (étape 4), sinon la priorité de la couche.</summary>
    private static long MergeRank(ShowModel show, Playback playback) =>
        playback.Flash ? long.MaxValue : show.Layers[playback.LayerIndex].Priority;

    private void Insert(Playback playback)
    {
        // Ordre de fusion : priorité de couche croissante, puis ordre d'activation (la plus récente en dernier).
        var priority = MergeRank(_show, playback);
        var index = _playbacks.Count;
        for (var i = 0; i < _playbacks.Count; i++)
        {
            if (MergeRank(_show, _playbacks[i]) > priority)
            {
                index = i;
                break;
            }
        }

        _playbacks.Insert(index, playback);
    }

    private string? StopScene(StopSceneCommand command)
    {
        var found = false;
        foreach (var playback in _playbacks)
        {
            if (playback.Scene.Id == command.SceneId && playback.State is not (PlaybackState.FadingOut or PlaybackState.Done))
            {
                playback.BeginExit(command.Fade?.TotalSeconds ?? playback.Scene.FadeOut?.ToSeconds(Bpm) ?? 0);
                found = true;
            }
        }

        var cancelled = CancelQuantized(s => s?.Id == command.SceneId) > 0;
        if (found)
        {
            _logger.LogInformation("Scène « {Scene} » arrêtée (origine {Origine})", _show.Scene(command.SceneId)?.Name, command.Origin);
        }

        return found || cancelled ? null : "la scène ne joue pas";
    }

    private string? StopLayer(StopLayerCommand command)
    {
        var layerIndex = -1;
        if (command.LayerId is { } layerId)
        {
            layerIndex = LayerIndexFor(layerId, fallback: false);
            if (layerIndex < 0)
            {
                return "couche inconnue";
            }
        }

        CancelQuantized(s =>
        {
            var index = s is null ? -1 : LayerIndexFor(s.LayerId, fallback: false);
            return index >= 0 && (layerIndex >= 0 ? index == layerIndex : command.Everything || !_show.Layers[index].KeepOnStopAll);
        });
        foreach (var playback in _playbacks)
        {
            // « Tout arrêter » épargne les couches protégées (Ambiance par défaut), sauf demande expresse (COU-007).
            var stop = layerIndex >= 0
                ? playback.LayerIndex == layerIndex
                : command.Everything || !_show.Layers[playback.LayerIndex].KeepOnStopAll;
            if (stop)
            {
                playback.BeginExit(command.Fade?.TotalSeconds ?? playback.Scene.FadeOut?.ToSeconds(Bpm) ?? 0);
            }
        }

        return null;
    }

    private string? SetLayerMaster(SetLayerMasterCommand command)
    {
        var index = LayerIndexFor(command.LayerId, fallback: false);
        if (index < 0)
        {
            return "couche inconnue";
        }

        _layerMasters[index] = Math.Clamp(command.Level, 0, 1);
        return null;
    }

    // ERG-037 : niveau du dimmer d'un groupe (CMD-031).
    private string? SetGroupDimmer(SetGroupDimmerCommand command)
    {
        var index = _show.IndexOfDimmerGroup(command.GroupId);
        if (index < 0)
        {
            return "groupe inconnu";
        }

        if (!_show.DimmerGroups[index].HasDimmer)
        {
            return "ce groupe n'a pas de dimmer";
        }

        _dimmerLevels[index] = Math.Clamp(command.Level, 0, 1);
        return null;
    }

    private string? StepScene(StepSceneCommand command, TimeSpan now)
    {
        var found = false;
        foreach (var playback in _playbacks)
        {
            if (playback.Scene.Id == command.SceneId && playback.State != PlaybackState.Done)
            {
                playback.Step(command.Direction, Bpm);
                _bus?.Publish(new StepChanged(playback.Scene.Id, playback.StepIndex, now));
                found = true;
            }
        }

        return found ? null : "la scène ne joue pas";
    }

    private string? ShowStep(ShowStepCommand command)
    {
        if (command.SceneId is not { } sceneId)
        {
            _shownStep = null;
            return null;
        }

        var scene = _show.Scene(sceneId);
        if (scene is null)
        {
            return "scène inconnue";
        }

        if (scene.Steps.Count == 0)
        {
            return $"la scène « {scene.Name} » n'a aucune étape";
        }

        var step = Math.Clamp(command.StepIndex, 0, scene.Steps.Count - 1);
        if (_shownStep is { } shown && shown.Scene.Id == sceneId && shown.StepIndex == step)
        {
            // Même étape redemandée : les effets continuent sans repartir au début.
            return null;
        }

        var layerIndex = LayerIndexFor(scene.LayerId);
        if (layerIndex < 0)
        {
            return "aucune couche pour jouer la scène";
        }

        var playback = new Playback(scene, layerIndex, ++_sequence, command.Origin, false) { Flash = true, SessionSeed = unchecked((ulong)Seed), Clock = _tempo, Events = _events };
        playback.Bind(scene, _show);
        playback.Pin(step, Bpm);
        _shownStep = playback;
        return null;
    }

    private string? SetSceneSpeed(SetSceneSpeedCommand command)
    {
        var found = false;
        foreach (var playback in _playbacks)
        {
            if (playback.Scene.Id == command.SceneId)
            {
                playback.Speed = Math.Clamp(command.Speed, 0.1, 10);
                found = true;
            }
        }

        return found ? null : "la scène ne joue pas";
    }

    private int LayerIndexFor(Guid layerId, bool fallback = true)
    {
        var layers = _show.Layers;
        for (var i = 0; i < layers.Count; i++)
        {
            if (layers[i].Id == layerId)
            {
                return i;
            }
        }

        return fallback && layers.Count > 0 ? 0 : -1;
    }

    private void AdvancePlaybacks(double elapsed, TimeSpan now)
    {
        if (_playbacks.Count == 0 && _shownStep is null)
        {
            return;
        }

        var bpm = Bpm;
        _chains.Clear();
        foreach (var playback in _playbacks)
        {
            // Une scène lancée pendant ce tick part de son instant de lancement : elle n'avance pas encore.
            var own = playback.Sequence > _sequenceAtTickStart ? 0 : elapsed;
            var action = playback.Advance(own, bpm, _random, out var stepChanged);
            if (stepChanged)
            {
                _bus?.Publish(new StepChanged(playback.Scene.Id, playback.StepIndex, now));
            }

            switch (action)
            {
                case PlaybackAdvance.Stop:
                    playback.BeginExit(playback.Scene.FadeOut?.ToSeconds(bpm) ?? 0, startsNow: false);
                    break;
                case PlaybackAdvance.Chain:
                    _chains.Add(playback);
                    break;
            }
        }

        // L'étape montrée (CMD-017) ne change jamais d'étape ; ses effets tournent.
        _shownStep?.Advance(_shownStep.Sequence > _sequenceAtTickStart ? 0 : elapsed, bpm, _random, out _);

        foreach (var playback in _chains)
        {
            // MOT-014 : enchaîner sur une scène donnée, dans la même couche (elle remplace celle qui se termine).
            var next = playback.Scene.ChainSceneId is { } id ? _show.Scene(id) : null;
            if (next is null || next.Steps.Count == 0)
            {
                playback.BeginExit(playback.Scene.FadeOut?.ToSeconds(bpm) ?? 0);
                continue;
            }

            Launch(next, playback.LayerIndex, null, playback.Origin, playback.Solo, playback, now);
        }

        for (var i = _playbacks.Count - 1; i >= 0; i--)
        {
            var playback = _playbacks[i];
            if (playback.State == PlaybackState.Done)
            {
                _playbacks.RemoveAt(i);
                _bus?.Publish(new SceneStopped(playback.Scene.Id, playback.Scene.Name, now));
            }
        }
    }

    /// <summary>COU-009 : une couche sans scène (hors fondu de sortie) rejoue sa scène de repos, avec son fondu croisé.</summary>
    private void LaunchRestScenes(TimeSpan now)
    {
        var layers = _show.Layers;
        for (var l = 0; l < layers.Count; l++)
        {
            if (layers[l].RestSceneId is not { } restId)
            {
                continue;
            }

            var busy = false;
            foreach (var playback in _playbacks)
            {
                busy |= playback.LayerIndex == l && !playback.Flash && playback.State is not (PlaybackState.FadingOut or PlaybackState.Done);
            }

            if (busy || _show.Scene(restId) is not { Steps.Count: > 0 } rest)
            {
                continue;
            }

            Launch(rest, l, layers[l].CrossFade.ToSeconds(Bpm), CommandOrigin.Tool, false, null, now);
        }
    }

    /// <summary>Étapes 1 à 3 : valeurs par défaut, puis fusion des lectures par priorité de couche (doc 15 §5).</summary>
    private void MergeLayers()
    {
        Array.Copy(_defaults, _result, _defaults.Length);
        Array.Clear(_touched);
        Array.Fill(_sources, default);

        var solo = false;
        foreach (var playback in _playbacks)
        {
            solo |= playback.Solo && playback.State != PlaybackState.Done;
        }

        foreach (var playback in _playbacks)
        {
            if (solo && !playback.Solo)
            {
                continue;
            }

            var layer = _show.Layers[playback.LayerIndex];

            // Étape 4 : un flash passe au-dessus de toutes les couches, intensité comprise (MOT-072).
            MergePlayback(
                playback,
                _layerMasters[playback.LayerIndex],
                layer.MasterOnAllAttributes,
                playback.Flash ? IntensityMode.Priority : layer.IntensityMode,
                new ParameterSource(SourceKind.Scene, playback.Scene.Id, layer.Id));
        }

        // CMD-017 : l'étape éditée, effets compris, au-dessus de toutes les couches (aperçu de l'édition, EFF-006).
        if (_shownStep is { } shown)
        {
            MergePlayback(shown, 1, false, IntensityMode.Priority, new ParameterSource(SourceKind.Scene, shown.Scene.Id, _show.Layers[shown.LayerIndex].Id));
        }
    }

    private void MergePlayback(Playback playback, double master, bool masterOnAll, IntensityMode intensityMode, ParameterSource source)
    {
        var parameters = playback.Parameters;
        var fadingOut = playback.State == PlaybackState.FadingOut;
        var effects = playback.HasEffects;
        for (var i = 0; i < parameters.Length; i++)
        {
            if (playback.Masked[i])
            {
                continue;
            }

            var p = parameters[i];
            var discrete = playback.Discrete[i];
            var value = playback.Value[i];
            var own = playback.Weight[i];
            if (effects)
            {
                // MOT-061 : effets de l'étape, sur la valeur de la scène ou, en relatif, sur la valeur sous-jacente.
                playback.ApplyEffects(i, _result[p], ref value, ref own);
            }

            // Un attribut discret revient à la valeur sous-jacente dès le début du fondu de sortie (MOT-012).
            var weight = discrete && fadingOut ? 0 : own * playback.ExitWeight;
            if (weight <= 0)
            {
                continue;
            }

            if (_roles[p] == ParameterRole.Intensity)
            {
                MergeIntensity(p, value, weight, master, intensityMode, source);
                continue;
            }

            // LTP par priorité : interpolation du résultat inférieur vers la valeur de la couche, selon le poids.
            var ltpWeight = masterOnAll ? weight * master : weight;
            if (discrete)
            {
                if (ltpWeight >= 0.5)
                {
                    _result[p] = value;
                    _sources[p] = source;
                }
            }
            else
            {
                _result[p] += (value - _result[p]) * ltpWeight;
                _sources[p] = source;
            }

            _touched[p] = true;
        }
    }

    private void MergeIntensity(int p, double value, double weight, double master, IntensityMode mode, ParameterSource source)
    {
        switch (mode)
        {
            case IntensityMode.Priority:
                _result[p] += ((value * master) - _result[p]) * weight;
                _sources[p] = source;
                break;

            case IntensityMode.Additive:
                _result[p] = Math.Min(1, (_touched[p] ? _result[p] : 0) + (value * weight * master));
                _sources[p] = source;
                break;

            case IntensityMode.Multiplicative:
                _result[p] *= 1 + ((value - 1) * weight * master);
                break;

            default:
                // HTP : la plus haute contribution (valeur × poids × master) l'emporte (D11, D16).
                var contribution = value * weight * master;
                if (!_touched[p] || contribution >= _result[p])
                {
                    _result[p] = contribution;
                    _sources[p] = source;
                }

                break;
        }

        _touched[p] = true;
    }

    /// <summary>Étapes 5 à 8, puis « Suit l'intensité » (MOT-040).</summary>
    private void ApplyMasters()
    {
        var count = _result.Length;

        // ERG-037 : niveau effectif de chaque groupe = son niveau × celui de tous ses parents (règle proportionnelle) ;
        // un parent précède toujours ses enfants (ShowModel), un seul passage suffit.
        var groups = _show.DimmerGroups;
        for (var g = 0; g < groups.Count; g++)
        {
            var parent = groups[g].Parent;
            _dimmerEffective[g] = _dimmerLevels[g] * (parent >= 0 ? _dimmerEffective[parent] : 1);
        }

        for (var p = 0; p < count; p++)
        {
            // Étape 5 : surcharges d'attributs (console en mode appareils, programmeur).
            var overrideValue = _overrides[p];
            if (!double.IsNaN(overrideValue))
            {
                _result[p] = overrideValue;
                _sources[p] = new ParameterSource(SourceKind.Override);
            }

            // Étape 6 : Grand Master, sur les seules intensités (GEN-041).
            if (_roles[p] == ParameterRole.Intensity)
            {
                _result[p] *= _grandMaster;

                // Étape 6 bis : dimmers de groupe, après la fusion des couches et le Grand Master (ERG-037).
                var group = _parameterGroup[p];
                if (group >= 0)
                {
                    _result[p] *= _dimmerEffective[group];
                }
            }
        }

        // Étape 8 (MOT-073) : figer garde les valeurs du moment du gel ; le blackout (étape 7) et la sûreté (étape 9)
        // restent actifs, d'où le blackout appliqué après.
        if (_freezeCapture)
        {
            _freezeCapture = false;
            Array.Copy(_result, _frozen, count);
        }

        if (_frozenActive)
        {
            Array.Copy(_frozen, _result, count);
            Array.Fill(_sources, new ParameterSource(SourceKind.Frozen));
        }

        if (_blackout)
        {
            // Étape 7 : blackout, sur les seules intensités (GEN-041).
            for (var p = 0; p < count; p++)
            {
                if (_roles[p] == ParameterRole.Intensity)
                {
                    _result[p] = 0;
                    _sources[p] = new ParameterSource(SourceKind.Blackout);
                }
            }
        }

        for (var p = 0; p < count; p++)
        {
            // MOT-040 : un canal qui suit l'intensité est multiplié par l'intensité logique finale de l'appareil.
            var source = _intensitySource[p];
            _output[p] = source >= 0 ? _result[p] * _result[source] : _result[p];
        }
    }

    private void WriteParameters(int universe, DmxFrame frame)
    {
        var values = frame.Values;
        var parameters = _show.Parameters;
        foreach (var (p, address) in _outputSlots[universe])
        {
            var parameter = parameters[p];
            var value = parameter.Absent ? 0 : _output[p];
            if (parameter.Inverted && !parameter.Absent)
            {
                value = 1 - value;
            }

            if (address.Is16Bit)
            {
                var (coarse, fine) = DmxValues.To16Bit(value);
                values[address.Coarse - 1] = coarse;
                values[address.Fine - 1] = fine;
            }
            else
            {
                values[address.Coarse - 1] = DmxValues.To8Bit(value);
            }
        }
    }

    private void ApplyShow(ShowModel show)
    {
        var previous = _show;
        var count = show.Parameters.Count;

        var overrides = new double[count];
        Array.Fill(overrides, double.NaN);
        for (var i = 0; i < _overrides.Length; i++)
        {
            if (double.IsNaN(_overrides[i]))
            {
                continue;
            }

            var p = previous.Parameters[i];
            var index = show.IndexOf(p.FixtureId, p.ChannelKey);
            if (index >= 0)
            {
                overrides[index] = _overrides[i];
            }
        }

        var masters = new double[show.Layers.Count];
        for (var i = 0; i < show.Layers.Count; i++)
        {
            var old = previous.Layer(show.Layers[i].Id);
            masters[i] = old is not null ? _layerMasters[previous.IndexOfLayer(old.Id)] : Math.Clamp(show.Layers[i].Master, 0, 1);
        }

        var levels = new double[show.DimmerGroups.Count];
        for (var i = 0; i < levels.Length; i++)
        {
            // Un dimmer garde son niveau quand le projet est recompilé (comme le master d'une couche) ; un nouveau groupe démarre à 1.
            var oldIndex = previous.IndexOfDimmerGroup(show.DimmerGroups[i].Id);
            levels[i] = oldIndex >= 0 && show.DimmerGroups[i].HasDimmer ? _dimmerLevels[oldIndex] : 1;
        }

        var frozen = new double[count];
        for (var i = 0; i < _frozen.Length && i < previous.Parameters.Count; i++)
        {
            var p = previous.Parameters[i];
            var index = show.IndexOf(p.FixtureId, p.ChannelKey);
            if (index >= 0)
            {
                frozen[index] = _frozen[i];
            }
        }

        _show = show;
        _safety.Load(show.Safety);
        _frozen = frozen;
        _overrides = overrides;
        _layerMasters = masters;
        _dimmerLevels = levels;
        _dimmerEffective = new double[levels.Length];
        _parameterGroup = [.. show.ParameterGroups];
        _defaults = [.. show.Parameters.Select(p => Math.Clamp(p.Default, 0, 1))];
        _result = new double[count];
        _output = new double[count];
        _touched = new bool[count];
        _sources = new ParameterSource[count];
        _roles = [.. show.Parameters.Select(p => p.Role)];
        _intensitySource = [.. show.Parameters.Select(p => p.IntensitySource >= 0 && p.IntensitySource < count ? p.IntensitySource : -1)];
        BuildOutputs(show);

        // Les lectures en cours continuent avec la nouvelle version de leur scène (PAL-005), ou s'arrêtent si elle a disparu.
        for (var i = _playbacks.Count - 1; i >= 0; i--)
        {
            var playback = _playbacks[i];
            var scene = show.Scene(playback.Scene.Id);
            var oldLayerId = previous.Layers[playback.LayerIndex].Id;
            var layerIndex = LayerIndexFor(oldLayerId, fallback: false);
            if (layerIndex < 0 && scene is not null)
            {
                layerIndex = LayerIndexFor(scene.LayerId);
            }

            if (scene is null || scene.Steps.Count == 0 || layerIndex < 0)
            {
                _playbacks.RemoveAt(i);
                _bus?.Publish(new SceneStopped(playback.Scene.Id, playback.Scene.Name, _lastTick ?? TimeSpan.Zero));
                continue;
            }

            playback.LayerIndex = layerIndex;
            playback.Bind(scene, show, previous);
        }

        if (_shownStep is { } shownStep)
        {
            var shownScene = show.Scene(shownStep.Scene.Id);
            var shownLayer = shownScene is null ? -1 : LayerIndexFor(shownScene.LayerId);
            if (shownScene is null || shownScene.Steps.Count == 0 || shownLayer < 0)
            {
                _shownStep = null;
            }
            else
            {
                shownStep.LayerIndex = shownLayer;
                shownStep.Bind(shownScene, show, previous);
            }
        }

        _playbacks.Sort((a, b) =>
        {
            var byPriority = MergeRank(show, a).CompareTo(MergeRank(show, b));
            return byPriority != 0 ? byPriority : a.Sequence.CompareTo(b.Sequence);
        });

        _logger.LogInformation(
            "Moteur : modèle chargé ({Parametres} paramètres, {Couches} couches, {Scenes} scènes)",
            count,
            show.Layers.Count,
            show.Scenes.Count);
    }

    private void BuildOutputs(ShowModel show)
    {
        var slots = Enumerable.Range(0, _frames.Length).Select(_ => new List<(int, ChannelAddress)>()).ToArray();
        var masks = Enumerable.Range(0, _frames.Length).Select(_ => new bool[DmxConstants.ChannelCount]).ToArray();
        for (var p = 0; p < show.Parameters.Count; p++)
        {
            var parameter = show.Parameters[p];
            var dimmed = parameter.Role == ParameterRole.Intensity || parameter.IntensitySource >= 0;
            foreach (var address in parameter.Outputs)
            {
                if (address.Universe < 1 || address.Universe > _frames.Length
                    || address.Coarse is < 1 or > DmxConstants.ChannelCount
                    || address.Fine is < 0 or > DmxConstants.ChannelCount)
                {
                    continue;
                }

                slots[address.Universe - 1].Add((p, address));
                if (dimmed)
                {
                    masks[address.Universe - 1][address.Coarse - 1] = true;
                    if (address.Is16Bit)
                    {
                        masks[address.Universe - 1][address.Fine - 1] = true;
                    }
                }
            }
        }

        _outputSlots = [.. slots.Select(s => s.ToArray())];
        _blackoutMasks = masks;
    }

    private void Publish()
    {
        lock (_publishedLock)
        {
            if (!ReferenceEquals(_publishedShow, _show))
            {
                // Nouveau modèle : tableaux publiés réalloués (rare, au chargement d'un projet ou d'une modification).
                _publishedShow = _show;
                _publishedValues = new double[_result.Length];
                _publishedSources = new ParameterSource[_sources.Length];
                _publishedOverrides = new double[_overrides.Length];
                _publishedLayerMasters = new double[_layerMasters.Length];
                _publishedDimmerLevels = new double[_dimmerLevels.Length];
                _publishedDimmerEffective = new double[_dimmerEffective.Length];
            }

            _publishedBlackout = _blackout;
            _publishedFrozen = _frozenActive;
            _publishedSmoking = _safety.ManualSmoke;
            _publishedSmokeRest = _safety.SmokeRestRemaining;
            _publishedGrandMaster = _grandMaster;
            _publishedPending = _quantized.Count == 0 ? [] : [.. _quantized.Select(q => new PendingSceneLaunch(q.SceneId, Math.Max(0, q.TargetBeat - _tempo.EffectivePosition)))];
            _publishedTempo = new TempoInfo(_tempo.Bpm, _tempo.Source, _tempo.Confidence, _tempo.BeatInBar, _tempo.Bar, _tempo.Phase, _tempo.LatencySeconds, _tempo.HeardBpm);
            Array.Copy(_result, _publishedValues, _result.Length);
            Array.Copy(_sources, _publishedSources, _sources.Length);
            Array.Copy(_overrides, _publishedOverrides, _overrides.Length);
            Array.Copy(_layerMasters, _publishedLayerMasters, _layerMasters.Length);
            Array.Copy(_dimmerLevels, _publishedDimmerLevels, _dimmerLevels.Length);
            Array.Copy(_dimmerEffective, _publishedDimmerEffective, _dimmerEffective.Length);
            if (_publishedPlaybacks.Length < _playbacks.Count)
            {
                _publishedPlaybacks = new PlaybackInfo[_playbacks.Count * 2];
            }

            for (var i = 0; i < _playbacks.Count; i++)
            {
                var playback = _playbacks[i];
                _publishedPlaybacks[i] = new PlaybackInfo(
                    playback.Scene.Id,
                    _show.Layers[playback.LayerIndex].Id,
                    playback.State,
                    playback.StepIndex,
                    playback.Scene.Steps.Count,
                    playback.StepProgress,
                    playback.Speed,
                    playback.Solo,
                    playback.Flash);
            }

            _publishedPlaybackCount = _playbacks.Count;
            _publishedLimits = _safety.Active.Count == 0 ? [] : [.. _safety.Active];
        }
    }

    /// <summary>Construit l'instantané à la demande (copie pour l'interface, hors du fil du moteur), sous le verrou de publication.</summary>
    private EngineSnapshot BuildSnapshot()
    {
        return new EngineSnapshot
        {
            Show = _publishedShow,
            Values = (double[])_publishedValues.Clone(),
            Sources = (ParameterSource[])_publishedSources.Clone(),
            Overrides = (double[])_publishedOverrides.Clone(),
            Playbacks = _publishedPlaybacks.AsSpan(0, _publishedPlaybackCount).ToArray(),
            LayerMasters = (double[])_publishedLayerMasters.Clone(),
            DimmerLevels = (double[])_publishedDimmerLevels.Clone(),
            DimmerEffective = (double[])_publishedDimmerEffective.Clone(),
            Blackout = _publishedBlackout,
            Frozen = _publishedFrozen,
            Smoking = _publishedSmoking,
            SmokeRestSeconds = _publishedSmokeRest,
            GrandMaster = _publishedGrandMaster,
            Tempo = _publishedTempo,
            PendingLaunches = _publishedPending,
            ActiveLimits = _publishedLimits,
        };
    }

    private void PublishSafety(SafetyLimitReached limit)
    {
        // MOT-083 : une seule fois par épisode (le limiteur s'en charge), journalisé et publié.
        _logger.LogWarning("Sûreté ({Limite}) : {Appareil} — {Detail}", limit.Kind, limit.Label, limit.Detail);
        _bus?.Publish(limit);
    }

    private void Log(CommandLogEntry entry)
    {
        lock (_logLock)
        {
            if (_logCount > 0)
            {
                var lastIndex = (_logNext - 1 + CommandLogCapacity) % CommandLogCapacity;
                var last = _log[lastIndex];
                if (last.Rejection is null && entry.Rejection is null && last.Command.GetType() == entry.Command.GetType()
                    && last.Command.Origin == entry.Command.Origin
                    && entry.Command is OverrideChannelsCommand or OverrideAttributesCommand or SetGrandMasterCommand or SetLayerMasterCommand or SetGroupDimmerCommand)
                {
                    _log[lastIndex] = entry with { ReceivedAt = last.ReceivedAt, Repeat = last.Repeat + 1 };
                    return;
                }
            }

            _log[_logNext] = entry;
            _logNext = (_logNext + 1) % CommandLogCapacity;
            _logCount = Math.Min(_logCount + 1, CommandLogCapacity);
        }
    }

    /// <summary>Lancement quantifié en attente de son instant musical (MOT-018).</summary>
    private sealed record PendingLaunch(LaunchSceneCommand Command, Guid SceneId, double TargetBeat);

    /// <summary>Chargement d'un modèle, transporté par la file des commandes pour être appliqué entre deux ticks.</summary>
    private sealed record LoadShowRequest(ShowModel Show) : Command(CommandOrigin.Tool);
}
