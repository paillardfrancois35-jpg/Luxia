using System.Globalization;
using System.Text.RegularExpressions;
using Luxia.Core.Dmx;
using Luxia.Core.Settings;
using Luxia.Core.Time;
using Luxia.Engine;
using Luxia.Engine.Model;
using Luxia.Engine.Timing;
using Luxia.Messaging.Commands;
using Luxia.Messaging.Events;
using Luxia.Midi;
using Luxia.Output;
using Luxia.Output.Arduino;
using Luxia.Output.Drivers;
using Luxia.Output.Recording;
using Luxia.Persistence;
using Microsoft.Extensions.Logging;

namespace Luxia.Hosting;

/// <summary>
/// Assemblage des modules de P0 : préférences, bus, moteur, routeur, pilotes, boucle cadencée.
/// Utilisé tel quel par l'application et par l'outil sans interface.
/// </summary>
public sealed partial class LuxiaRuntime : IAsyncDisposable
{
    private readonly ILoggerFactory _loggers;
    private readonly ILogger _logger;
    private readonly ISerialPortProvider _serialPorts;
    private readonly Lock _lock = new();
    private ArduinoOutputDriver? _arduino;
    private volatile RecorderOutputDriver? _recorder;
    private Dictionary<Guid, string> _names = [];
    private readonly ILogger _uiLogger;
    private readonly SleepInhibitor _sleepInhibitor;
    private bool _started;
    private bool _stopped;
    private bool _previewActive;
    private MidiLayout _midiLayout = MidiLayout.Empty;
    private Timer? _background;
    private int _backgroundTicks;
    private string? _lastResume;
    private TimeSpan _lastCpu;
    private DateTime _lastCpuAt;
    private double _cpuPercent;

    /// <summary>Instantané de reprise (MOT-102) : dans les préférences du poste, pas dans le projet.</summary>
    private static readonly Persistence.Json.DocumentType<ResumeState> ResumeType = new("reprise", ResumeState.CurrentFormatVersion, []);

    /// <summary>Assemble les modules (sans rien démarrer).</summary>
    /// <param name="paths">Emplacements des données.</param>
    /// <param name="loggers">Journaux.</param>
    /// <param name="serialPorts">Ports série (Arduino) ; ceux du système par défaut.</param>
    /// <param name="clock">Horloge ; réelle par défaut.</param>
    /// <param name="midiPorts">Ports MIDI (APC mini, doc 18b) ; <c>null</c> = pas de contrôleur (outils, tests).</param>
    /// <param name="audioSources">Sources audio (son joué par le PC, doc 19) ; <c>null</c> = pas d'écoute (outils, tests).</param>
    public LuxiaRuntime(DataPaths paths, ILoggerFactory loggers, ISerialPortProvider? serialPorts = null, IClock? clock = null, IMidiPorts? midiPorts = null, Audio.IAudioSourceFactory? audioSources = null)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(loggers);
        Paths = paths;
        _loggers = loggers;
        _logger = loggers.CreateLogger<LuxiaRuntime>();
        _serialPorts = serialPorts ?? new SystemSerialPortProvider();
        Clock = clock ?? new SystemClock();

        Preferences = new PreferencesStore(paths.PreferencesFile, loggers.CreateLogger<PreferencesStore>());
        var loaded = Preferences.Load();
        PreferencesLoadMessage = loaded.Status == Persistence.Json.LoadStatus.Missing ? null : loaded.Message;

        Bus = new EventBus(loggers.CreateLogger<EventBus>());
        Router = new OutputRouter(Bus, loggers.CreateLogger<OutputRouter>());
        var universes = Math.Max(1, Preferences.Current.Outputs.Assignments.Select(a => a.Universe).DefaultIfEmpty(1).Max());
        Engine = new RenderEngine(Router, Clock, universes, loggers.CreateLogger<RenderEngine>(), Bus);

        // SORT-066 : commandes traitées, versées au journal de l'enregistrement des trames.
        Engine.CommandApplied += OnCommandApplied;
        _uiLogger = loggers.CreateLogger("IHM");

        // GEN-063 : moteur d'aperçu pour l'édition en aveugle ; ses trames ne vont à aucune sortie, seulement au simulateur.
        Preview = new RenderEngine(DiscardFrames.Instance, Clock, universes, loggers.CreateLogger<RenderEngine>());
        Loop = new TickLoop(TickEngines, Clock, Preferences.Current.TickRateHz, loggers.CreateLogger<TickLoop>());
        Project = new ProjectSession(Preferences, loggers.CreateLogger<ProjectSession>());
        Project.OpenLast();
        Show = new ShowService(Project, [Engine, Preview], loggers.CreateLogger<ShowService>());

        // Contrôleurs MIDI : mêmes colonnes et mêmes boutons que l'écran Live, relus à chaque recompilation.
        Show.Compiled += (_, _) => _midiLayout = BuildMidiLayout();
        _midiLayout = BuildMidiLayout();
        Midi = midiPorts is null
            ? null
            : new MidiService(midiPorts, Engine, () => Engine.Snapshot, () => _midiLayout, loggers.CreateLogger<MidiService>());

        // Écoute de la musique (doc 19) : le moteur lit tempo et impulsions à chaque tick ; le démarrage suit les préférences.
        if (audioSources is not null)
        {
            Audio = new Audio.AudioListener(audioSources, loggers.CreateLogger<Audio.AudioListener>());
            Audio.SetDevice(Preferences.Current.Audio.DeviceId);
            Audio.Tune(AudioTuningFrom(Preferences.Current.Audio));
            Audio.EventRaised += (_, e) => Bus.Publish(new Messaging.Events.MusicEvent((Messaging.Events.MusicEventKind)(int)e.Kind, (int)e.Level, e.Energy, Clock.Now));
            Engine.SetAudioFeed(Audio);
        }

        Engine.Send(new Messaging.Commands.SetTempoLatencyCommand(Messaging.Commands.CommandOrigin.Tool, Preferences.Current.Audio.LatencyFor(Preferences.Current.Audio.DeviceId)));

        // GEN-095 : arrêt brutal lors de la dernière session, avec le même projet ouvert → reprise proposée.
        PendingResume = ReadPendingResume();
        _sleepInhibitor = new SleepInhibitor(loggers.CreateLogger<SleepInhibitor>());
        Library = new Fixtures.FixtureLibrary(paths.Library, loggers.CreateLogger<Fixtures.FixtureLibrary>());
        Library.Load();
    }

    /// <summary>Écoute de la musique (doc 19) ; <c>null</c> si l'application n'en a pas (outils, tests).</summary>
    public Audio.AudioListener? Audio { get; }

    /// <summary>
    /// Démarre ou arrête l'écoute du son joué par le PC et le mémorise dans les préférences du poste. Sans écoute, la source
    /// de tempo Audio garde le dernier tempo (GEN-034).
    /// </summary>
    public void SetListening(bool listen)
    {
        if (Audio is null)
        {
            return;
        }

        if (listen)
        {
            Audio.Start();
        }
        else
        {
            Audio.Stop();

            // Sans écoute, la source Audio n'a plus de sens : le tempo reste celui d'avant, devenu fixe (l'état affiché doit être l'état réel).
            if (Engine.Snapshot.Tempo.Source == Messaging.Commands.TempoSourceKind.Audio)
            {
                Engine.Send(new Messaging.Commands.SetTempoSourceCommand(Messaging.Commands.CommandOrigin.User, Messaging.Commands.TempoSourceKind.Fixed));
            }
        }

        Preferences.Update(p => p with { Audio = p.Audio with { Listen = listen } });
    }

    /// <summary>Choisit le périphérique écouté (AUD-003) et le mémorise ; <c>null</c> = le son joué par le PC.</summary>
    public void SetAudioDevice(string? deviceId)
    {
        Audio?.SetDevice(deviceId);
        Preferences.Update(p => p with { Audio = p.Audio with { DeviceId = deviceId } });

        // La latence suit le périphérique : celle de son dernier réglage (0 pour un périphérique encore jamais calibré).
        Engine.Send(new Messaging.Commands.SetTempoLatencyCommand(Messaging.Commands.CommandOrigin.User, Preferences.Current.Audio.LatencyFor(deviceId)));
    }

    /// <summary>Applique et mémorise les réglages de l'analyse (AUD-081).</summary>
    public void SetAudioTuning(Audio.AudioTuning tuning, double latencySeconds)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        Audio?.Tune(tuning);
        Engine.Send(new Messaging.Commands.SetTempoLatencyCommand(Messaging.Commands.CommandOrigin.User, latencySeconds));
        Preferences.Update(p => p with
        {
            Audio = p.Audio with
            {
                PulseSensitivity = tuning.PulseSensitivity,
                EnergySmoothingSeconds = tuning.EnergySmoothingSeconds,
                MinBpm = tuning.MinBpm,
                MaxBpm = tuning.MaxBpm,
                PreferredBpm = tuning.PreferredBpm,
                LatencySeconds = p.Audio.DeviceId is null ? Math.Clamp(latencySeconds, -0.5, 0.5) : p.Audio.LatencySeconds,
                LatencyByDevice = p.Audio.DeviceId is null
                    ? p.Audio.LatencyByDevice
                    : new Dictionary<string, double>(p.Audio.LatencyByDevice) { [p.Audio.DeviceId] = Math.Clamp(latencySeconds, -0.5, 0.5) },
            },
        });
    }

    private static Audio.AudioTuning AudioTuningFrom(Core.Settings.AudioPreferences prefs) => new()
    {
        PulseSensitivity = prefs.PulseSensitivity,
        EnergySmoothingSeconds = prefs.EnergySmoothingSeconds,
        MinBpm = prefs.MinBpm,
        MaxBpm = prefs.MaxBpm,
        PreferredBpm = prefs.PreferredBpm,
    };

    /// <summary>Bibliothèque d'appareils (<c>Documents\LuXia\Bibliothèque</c>).</summary>
    public Fixtures.FixtureLibrary Library { get; }

    /// <summary>
    /// Fabrique de journaux (GEN-110) : permet à un modèle de vue de créer son propre journal, dans le même
    /// fichier technique (<c>Documents\LuXia\Journaux\technique-AAAAMMJJ.log</c>) que le reste de l'application.
    /// </summary>
    public ILoggerFactory Loggers => _loggers;

    /// <summary>Projet ouvert.</summary>
    public ProjectSession Project { get; }

    /// <summary>Compilation du projet vers le moteur (D26), tenue à jour à chaque modification.</summary>
    public ShowService Show { get; }

    /// <summary>Emplacements des données.</summary>
    public DataPaths Paths { get; }

    /// <summary>Horloge du moteur.</summary>
    public IClock Clock { get; }

    /// <summary>Préférences du poste.</summary>
    public PreferencesStore Preferences { get; }

    /// <summary>Message éventuel issu du chargement des préférences (migration, fichier mis de côté).</summary>
    public string? PreferencesLoadMessage { get; }

    /// <summary>
    /// Reprise proposée au démarrage (GEN-095) : l'application s'est arrêtée brutalement alors que des scènes jouaient dans
    /// le projet qui vient d'être rouvert ; <c>null</c> sinon.
    /// </summary>
    public ResumeState? PendingResume { get; private set; }

    /// <summary>Utilisation moyenne du processeur par l'application, en % de la machine (GEN-094), mesurée toutes les 5 s.</summary>
    public double CpuPercent => Volatile.Read(ref _cpuPercent);

    /// <summary>Fichier de l'instantané de reprise.</summary>
    public string ResumeFile => Path.Combine(Paths.AppDataRoot, "reprise.json");

    /// <summary>Contrôleurs MIDI (APC mini, doc 18b) ; <c>null</c> sans ports MIDI.</summary>
    public MidiService? Midi { get; }

    /// <summary>Bus d'événements.</summary>
    public EventBus Bus { get; }

    /// <summary>Moteur de rendu (porte d'entrée des commandes).</summary>
    public RenderEngine Engine { get; }

    /// <summary>
    /// Moteur d'aperçu (GEN-063, SCN-035) : mêmes scènes et palettes, mais ses trames ne sont émises nulle part ;
    /// le programmeur en aveugle y envoie ses commandes et le simulateur l'affiche tant que <see cref="PreviewActive"/>.
    /// </summary>
    public RenderEngine Preview { get; }

    /// <summary>Aperçu en cours (édition en aveugle) : le moteur d'aperçu est cadencé et montré au simulateur.</summary>
    public bool PreviewActive
    {
        get => Volatile.Read(ref _previewActive);
        set => Volatile.Write(ref _previewActive, value);
    }

    /// <summary>Routeur de sorties.</summary>
    public OutputRouter Router { get; }

    /// <summary>Boucle cadencée.</summary>
    public TickLoop Loop { get; }

    /// <summary>Pilote Arduino actif (s'il est configuré).</summary>
    public ArduinoOutputDriver? Arduino => _arduino;

    /// <summary>Enregistreur actif.</summary>
    public RecorderOutputDriver? Recorder => _recorder;

    /// <summary>La mise en veille du PC est bloquée (GEN-096).</summary>
    public bool IsSleepBlocked => _sleepInhibitor.IsActive;

    /// <summary>Ports série présents (diagnostic).</summary>
    public IReadOnlyList<SerialPortInfo> SerialPorts => _serialPorts.GetPorts();

    /// <summary>Démarre les sorties puis la boucle. La sortie émet un blackout tant que rien n'est lancé (GEN-060).</summary>
    public void Start(bool forceNullOutput = false)
    {
        lock (_lock)
        {
            if (_started)
            {
                return;
            }

            _started = true;
            _logger.LogInformation("Démarrage de LuXia {Version}", typeof(LuxiaRuntime).Assembly.GetName().Version);

            var assignments = forceNullOutput
                ? [new OutputAssignment(1, OutputDriverKind.Null)]
                : Preferences.Current.Outputs.Assignments;
            foreach (var assignment in assignments)
            {
                Router.Attach(assignment.Universe, CreateDriver(assignment.Driver));
            }

            Loop.Start();
            Midi?.Start();
            if (Preferences.Current.Audio.Listen && Audio is { } listener)
            {
                // L'ouverture de la capture (0,4 à 0,5 s) ne doit pas retarder le démarrage de l'application.
                _ = Task.Run(listener.Start);
            }

            // Toutes les 5 s : instantané de reprise (MOT-102) et mesure du processeur (GEN-094) ; toutes les 2 min :
            // version du projet si quelque chose a changé (GEN-054, GEN-055).
            _lastCpu = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
            _lastCpuAt = DateTime.UtcNow;
            _background = new Timer(_ => Background(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

            // GEN-096 : une mise en veille couperait la lumière (plus de trames → chien de garde → noir).
            _sleepInhibitor.Start();
        }
    }

    /// <summary>Recrée le pilote Arduino (bouton « Reconnecter », changement de port ou de protocole).</summary>
    public void ReconnectArduino()
    {
        lock (_lock)
        {
            var current = _arduino;
            if (current is null)
            {
                return;
            }

            var universe = Router.Routes.First(r => r.Driver == current).Universe;
            Router.Detach(current);
            Router.Attach(universe, CreateDriver(OutputDriverKind.Arduino));
            _logger.LogInformation("Reconnexion de l'Arduino demandée");
        }
    }

    /// <summary>Modifie les réglages de l'Arduino, les enregistre et les applique.</summary>
    public void UpdateArduinoSettings(Func<ArduinoPreferences, ArduinoPreferences> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        var before = Preferences.Current.Outputs.Arduino;
        Preferences.Update(p => p with { Outputs = p.Outputs with { Arduino = change(p.Outputs.Arduino) } });
        var after = Preferences.Current.Outputs.Arduino;

        if (_arduino is { } arduino)
        {
            arduino.Settings = after;
            if (!string.Equals(before.ForcedPort, after.ForcedPort, StringComparison.OrdinalIgnoreCase)
                || before.Protocol != after.Protocol
                || before.ProbeAllPorts != after.ProbeAllPorts)
            {
                ReconnectArduino();
            }
        }
    }

    /// <summary>Change la fréquence du moteur (25-44 Hz) et l'enregistre.</summary>
    public void SetTickRate(double rateHz)
    {
        Loop.RateHz = rateHz;
        Preferences.Update(p => p with { TickRateHz = Loop.RateHz });
    }

    /// <summary>Lance le chenillard de test (CMD-024) avec ces réglages.</summary>
    /// <param name="settings">Réglages du test.</param>
    /// <param name="origin">Origine de la commande.</param>
    /// <param name="loop">Recommencer au début après le dernier canal.</param>
    /// <param name="universe">Univers testé.</param>
    /// <param name="remember">Enregistrer les réglages dans les préférences (écran Sorties).</param>
    /// <param name="mode">Chenillard ou rampe (endurance).</param>
    /// <returns>Message d'erreur si les réglages sont invalides, sinon null.</returns>
    public string? StartTest(
        TestOutputPreferences settings,
        CommandOrigin origin = CommandOrigin.User,
        bool loop = true,
        int universe = 1,
        bool remember = true,
        TestPatternMode mode = TestPatternMode.Chase)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!ChannelRange.TryParse(settings.Range, out var range))
        {
            return $"Plage de canaux invalide : « {settings.Range} » (ex. 1-16).";
        }

        if (!ChannelList.TryParse(settings.ExcludedChannels, out var excluded))
        {
            return $"Canaux exclus invalides : « {settings.ExcludedChannels} » (ex. 180 ou 1, 5-8).";
        }

        if (!ChannelList.TryParse(settings.HeldChannels, out var held))
        {
            return $"Canaux maintenus invalides : « {settings.HeldChannels} » (ex. 1, 8, 15, 22).";
        }

        if (settings.StepMilliseconds is < 50 or > 60_000)
        {
            return "La durée par canal doit être comprise entre 50 ms et 60 s.";
        }

        if (remember)
        {
            Preferences.Update(p => p with { TestOutput = settings });
        }

        Engine.Send(new TestOutputCommand(
            origin,
            true,
            universe,
            range,
            excluded,
            settings.ValueByte,
            TimeSpan.FromMilliseconds(settings.StepMilliseconds),
            loop,
            mode,
            held));
        return null;
    }

    /// <summary>Arrête le chenillard de test.</summary>
    public void StopTest(CommandOrigin origin = CommandOrigin.User) => Engine.Send(TestOutputCommand.Stop(origin));

    /// <summary>Impose des valeurs brutes (console, CMD-020).</summary>
    public void SetChannels(int universe, IReadOnlyList<ChannelValue> values, CommandOrigin origin = CommandOrigin.User) =>
        Engine.Send(new OverrideChannelsCommand(origin, universe, values));

    /// <summary>Libère des canaux (null = tous les canaux de l'univers ; univers null = tout, CMD-022).</summary>
    public void ReleaseChannels(int? universe, IReadOnlyList<int>? channels, CommandOrigin origin = CommandOrigin.User) =>
        Engine.Send(new ReleaseOverridesCommand(origin, universe, channels));

    /// <summary>Rappelle un instantané : les surcharges de son univers sont remplacées par les siennes (CONS-010).</summary>
    public void RecallSnapshot(Core.Snapshots.ConsoleSnapshot snapshot, CommandOrigin origin = CommandOrigin.User)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Engine.Send(new ReleaseOverridesCommand(origin, snapshot.Universe));
        Engine.Send(new OverrideChannelsCommand(origin, snapshot.Universe, [.. snapshot.Channels.Select(c => new ChannelValue(c.Channel, c.Value))]));
        _logger.LogInformation("Instantané rappelé : {Nom}", snapshot.Name);
    }

    /// <summary>Blackout (CMD-001, GEN-082).</summary>
    public void SetBlackout(bool active, CommandOrigin origin = CommandOrigin.User) => Engine.Send(new BlackoutCommand(origin, active));

    /// <summary>Grand Master 0-1 (CMD-002).</summary>
    public void SetGrandMaster(double level, CommandOrigin origin = CommandOrigin.User) => Engine.Send(new SetGrandMasterCommand(origin, level));

    /// <summary>Lance une scène (CMD-010).</summary>
    public void LaunchScene(Guid sceneId, bool solo = false, CommandOrigin origin = CommandOrigin.User) =>
        Engine.Send(new LaunchSceneCommand(origin, sceneId, Solo: solo));

    /// <summary>Arrête une scène avec son fondu de sortie (CMD-011).</summary>
    public void StopScene(Guid sceneId, CommandOrigin origin = CommandOrigin.User) => Engine.Send(new StopSceneCommand(origin, sceneId));

    /// <summary>Arrête toutes les scènes (CMD-012 sur toutes les couches).</summary>
    public void StopAllScenes(CommandOrigin origin = CommandOrigin.User) => Engine.Send(new StopLayerCommand(origin));

    /// <summary>Démarre l'enregistrement des trames de l'univers 1 (SORT-061).</summary>
    /// <param name="path">Fichier ; par défaut <c>Documents\LuXia\Enregistrements\trames-horodatage.dmxrec</c>.</param>
    /// <returns>Chemin du fichier.</returns>
    public string StartRecording(string? path = null)
    {
        lock (_lock)
        {
            StopRecordingCore();
            var folder = Preferences.Current.Outputs.RecordingsFolder ?? Paths.Recordings;
            path ??= Path.Combine(folder, $"trames-{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}{RecordingFormat.Extension}");
            _recorder = new RecorderOutputDriver(path, 1, Loop.RateHz, _loggers.CreateLogger<RecorderOutputDriver>());
            _names = BuildNames();
            Router.Attach(1, _recorder);
            return path;
        }
    }

    /// <summary>Arrête l'enregistrement en cours ; renvoie le chemin du fichier fermé.</summary>
    public string? StopRecording()
    {
        lock (_lock)
        {
            return StopRecordingCore();
        }
    }

    /// <summary>
    /// Arrêt propre : fondu au noir (GEN-061), arrêt de la boucle, trame de blackout envoyée à toutes les sorties
    /// (les appareils s'éteignent tout de suite, sans attendre le chien de garde), fermeture des pilotes.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        lock (_lock)
        {
            if (_stopped)
            {
                return;
            }

            _stopped = true;
        }

        _background?.Dispose();

        // GEN-061 : fondu au noir avant d'arrêter l'émission (le blackout final suit).
        if (_started)
        {
            await FadeToBlackAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }

        WriteResume(clean: true);
        Midi?.Dispose();
        Audio?.Dispose();
        Loop.Stop();
        _sleepInhibitor.Dispose();
        var blackout = new DmxFrame();
        for (var u = 1; u <= Engine.UniverseCount; u++)
        {
            Router.Submit(u, blackout, Clock.Now);
        }

        await Task.Delay(150).ConfigureAwait(false);
        Router.Dispose();
        await Bus.DisposeAsync().ConfigureAwait(false);
        _logger.LogInformation("Arrêt de LuXia");
    }

    /// <summary>
    /// Enregistre une version du projet si quelque chose a changé (GEN-054, GEN-055), sans bloquer l'appelant.
    /// </summary>
    /// <param name="reason">Motif (« passage en Live »…).</param>
    public void SaveProjectVersion(string reason)
    {
        if (Project.Folder is not { } folder)
        {
            return;
        }

        _ = Task.Run(() =>
        {
            try
            {
                if (ProjectVersions.Save(folder, reason, DateTime.Now) is { } name)
                {
                    _logger.LogInformation("Version du projet enregistrée : {Version} ({Motif})", name, reason);
                }
            }
            catch (IOException exception)
            {
                _logger.LogWarning(exception, "Version du projet non enregistrée ({Motif})", reason);
            }
            catch (UnauthorizedAccessException exception)
            {
                _logger.LogWarning(exception, "Version du projet non enregistrée ({Motif})", reason);
            }
        });
    }

    /// <summary>Reprend les scènes, masters et modes de l'instantané (GEN-095).</summary>
    public void Resume(ResumeState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        PendingResume = null;
        foreach (var (layer, level) in state.LayerMasters)
        {
            Engine.Send(new SetLayerMasterCommand(CommandOrigin.Tool, layer, level));
        }

        foreach (var scene in state.Scenes)
        {
            Engine.Send(new LaunchSceneCommand(CommandOrigin.Tool, scene));
        }

        Engine.Send(new SetGrandMasterCommand(CommandOrigin.Tool, state.GrandMaster));
        Engine.Send(new BlackoutCommand(CommandOrigin.Tool, state.Blackout));
        if (state.Frozen)
        {
            Engine.Send(new FreezeCommand(CommandOrigin.Tool, true));
        }

        _logger.LogInformation("Reprise après arrêt brutal : {Scenes} scène(s) relancée(s)", state.Scenes.Count);
    }

    /// <summary>Renonce à la reprise proposée.</summary>
    public void DismissResume() => PendingResume = null;

    /// <summary>Fondu du Grand Master jusqu'à 0 (GEN-061), la boucle du moteur tournant encore.</summary>
    private async Task FadeToBlackAsync(TimeSpan duration)
    {
        var start = Engine.Snapshot.GrandMaster;
        if (start <= 0 || Engine.Snapshot.Blackout)
        {
            return;
        }

        const int Steps = 20;
        for (var i = 1; i <= Steps; i++)
        {
            Engine.Send(new SetGrandMasterCommand(CommandOrigin.Tool, start * (1 - ((double)i / Steps))));
            await Task.Delay(duration / Steps).ConfigureAwait(false);
        }
    }

    private void Background()
    {
        try
        {
            SaveResumePoint();
            MeasureCpu();
            if (++_backgroundTicks % 24 == 0)
            {
                SaveProjectVersion("automatique");
            }
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "Tâche de fond (reprise, versions) : écriture impossible");
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Tâche de fond (reprise, versions) : écriture impossible");
        }
    }

    private void MeasureCpu()
    {
        var now = DateTime.UtcNow;
        var cpu = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
        var wall = (now - _lastCpuAt).TotalMilliseconds * Environment.ProcessorCount;
        if (wall > 0)
        {
            Volatile.Write(ref _cpuPercent, Math.Round(100 * (cpu - _lastCpu).TotalMilliseconds / wall, 1));
        }

        _lastCpu = cpu;
        _lastCpuAt = now;
        if (_backgroundTicks % 12 == 0)
        {
            _logger.LogInformation("Processeur : {Cpu} % en moyenne sur 5 s (GEN-094)", CpuPercent);
        }
    }

    /// <summary>Écrit l'instantané de reprise tout de suite (appelé toutes les 5 s, MOT-102).</summary>
    public void SaveResumePoint() => WriteResume(clean: false);

    private void WriteResume(bool clean)
    {
        var snapshot = Engine.Snapshot;
        var state = new ResumeState
        {
            ProjectFolder = Project.Folder,
            SavedAt = DateTime.Now,
            CleanExit = clean,
            Scenes = [.. snapshot.Playbacks
                .Where(p => !p.Flash && p.State is not (PlaybackState.FadingOut or PlaybackState.Done))
                .Select(p => p.SceneId)
                .Distinct()],
            LayerMasters = snapshot.Show.Layers.Select((l, i) => (l.Id, i)).Where(x => x.i < snapshot.LayerMasters.Length)
                .ToDictionary(x => x.Id, x => snapshot.LayerMasters[x.i]),
            GrandMaster = snapshot.GrandMaster,
            Blackout = snapshot.Blackout,
            Frozen = snapshot.Frozen,
        };

        // Écrit seulement si quelque chose a changé (hors horodatage) : pas d'écriture inutile toutes les 5 s.
        var key = string.Join(
            '|',
            state.ProjectFolder,
            state.CleanExit,
            string.Join(',', state.Scenes),
            string.Join(',', state.LayerMasters.OrderBy(m => m.Key).Select(m => string.Create(CultureInfo.InvariantCulture, $"{m.Key}={m.Value:0.###}"))),
            state.GrandMaster.ToString("0.###", CultureInfo.InvariantCulture),
            state.Blackout,
            state.Frozen);
        if (key == _lastResume)
        {
            return;
        }

        Directory.CreateDirectory(Paths.AppDataRoot);
        Persistence.Json.VersionedJsonFile.Save(ResumeFile, state, ResumeType);
        _lastResume = key;
    }

    private ResumeState? ReadPendingResume()
    {
        var loaded = Persistence.Json.VersionedJsonFile.Load(ResumeFile, ResumeType);
        if (!loaded.Succeeded || loaded.Value is not { } state)
        {
            return null;
        }

        var sameProject = state.ProjectFolder is { } folder && Project.Folder is { } open
            && string.Equals(Path.GetFullPath(folder), Path.GetFullPath(open), StringComparison.OrdinalIgnoreCase);
        var recent = DateTime.Now - state.SavedAt < TimeSpan.FromHours(12);
        return !state.CleanExit && sameProject && recent && state.Scenes.Count > 0 ? state : null;
    }

    /// <summary>Disposition des contrôleurs MIDI d'après le projet : colonnes du Live, boutons, affectations (MIDI-002, MIDI-007).</summary>
    private MidiLayout BuildMidiLayout()
    {
        var project = Project;
        var live = project.Live;
        var (flash, strobe) = Scenes.Rules.LiveRules.PermanentScenes(live, project.Layers, project.Scenes);
        return new MidiLayout
        {
            Columns = [.. Scenes.Rules.LiveRules.Columns(live, project.Layers, project.Scenes)
                .Select(c => new MidiColumn(
                    c.Layer.Id,
                    c.Layer.Kind == LayerKind.Flash,
                    [.. c.Scenes.Select(s => new MidiSceneSlot(s.Id, s.Color))]))],
            FlashSceneId = flash,
            StrobeSceneId = strobe,
            SmokeBurstSeconds = live.SmokeBurstSeconds,
            ActiveClickRestarts = live.ActiveSceneClick == Scenes.Model.ActiveSceneClick.Restart,
            Bindings = project.Midi.Bindings,
            Dimmers = [.. Patch.Rules.GroupRules.Layout(project.Groups).Where(n => n.Group.HasDimmer).Select(n => new MidiDimmerSlot(n.Group.Id, n.Group.Name))],
            DimmerController = project.Midi.DimmerController,
        };
    }

    private void TickEngines()
    {
        Engine.Tick();
        if (PreviewActive)
        {
            // L'aperçu (aveugle, édition) suit le tempo du moteur : les durées musicales y ont la même valeur.
            Preview.Bpm = Engine.Bpm;
            Preview.Tick();
        }
    }

    private OutputDriver CreateDriver(OutputDriverKind kind)
    {
        switch (kind)
        {
            case OutputDriverKind.Arduino:
                var arduino = new ArduinoOutputDriver(Preferences.Current.Outputs.Arduino, _serialPorts, _loggers.CreateLogger<ArduinoOutputDriver>());
                arduino.PortSelected += OnArduinoPortSelected;
                _arduino = arduino;
                return arduino;
            case OutputDriverKind.Null:
            default:
                return new NullOutputDriver(_loggers.CreateLogger<NullOutputDriver>());
        }
    }

    private void OnArduinoPortSelected(object? sender, string port)
    {
        if (!string.Equals(Preferences.Current.Outputs.Arduino.LastPort, port, StringComparison.OrdinalIgnoreCase))
        {
            Preferences.Update(p => p with { Outputs = p.Outputs with { Arduino = p.Outputs.Arduino with { LastPort = port } } });
        }
    }

    /// <summary>
    /// Problèmes du projet ouvert (menu Projet → Problèmes du projet) : ceux de la compilation, plus les règles vérifiées
    /// par <c>luxia-headless valider</c> (COU-008 hors famille, COU-009 scène de repos, Live, MIDI…), sur les fichiers
    /// enregistrés (chaque modification l'est tout de suite). Mêmes messages à l'écran et sans interface.
    /// </summary>
    public IReadOnlyList<Scenes.Compilation.CompileIssue> ProjectProblems() =>
        Project.Folder is { } folder ? Tools.ProjectValidator.Validate(folder) : Show.Last?.Issues ?? [];

    /// <summary>
    /// Trace une action de l'utilisateur (SORT-066) : « IHM – onglet – action » dans le journal technique et, pendant un
    /// enregistrement des trames, dans son journal, entre les lignes DMX.
    /// </summary>
    /// <param name="tab">Onglet (écran) d'où vient l'action.</param>
    /// <param name="action">Action réalisée.</param>
    public void TraceUi(string tab, string action)
    {
        _uiLogger.LogInformation("IHM – {Onglet} – {Action}", tab, action);
        _recorder?.Note("IHM", $"{tab} – {action}");
    }

    private void OnCommandApplied(CommandLogEntry entry)
    {
        // Fil du moteur : rien à faire hors enregistrement.
        if (_recorder is not { } recorder)
        {
            return;
        }

        var text = Guids().Replace(entry.Command.ToString() ?? string.Empty, m => Guid.TryParse(m.Value, out var id) && _names.TryGetValue(id, out var name) ? $"« {name} »" : m.Value);
        recorder.Note("MOTEUR", entry.Rejection is null ? text : $"{text} REFUSÉE : {entry.Rejection}");
    }

    private Dictionary<Guid, string> BuildNames()
    {
        var names = new Dictionary<Guid, string>();
        foreach (var scene in Project.Scenes.Scenes)
        {
            names.TryAdd(scene.Id, scene.Name);
        }

        foreach (var layer in Project.Layers.Layers)
        {
            names.TryAdd(layer.Id, "couche " + layer.Name);
        }

        foreach (var fixture in Project.Installation.Fixtures)
        {
            names.TryAdd(fixture.Id, fixture.Name);
        }

        foreach (var palette in Project.Palettes.Palettes)
        {
            names.TryAdd(palette.Id, "palette " + palette.Name);
        }

        return names;
    }

    [GeneratedRegex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex Guids();

    private string? StopRecordingCore()
    {
        var recorder = _recorder;
        if (recorder is null)
        {
            return null;
        }

        _recorder = null;
        Router.Detach(recorder);
        return recorder.FilePath;
    }
}
