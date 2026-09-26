using System.Globalization;
using Luxia.Core.Dmx;
using Luxia.Core.Settings;
using Luxia.Core.Time;
using Luxia.Engine;
using Luxia.Engine.Timing;
using Luxia.Messaging.Commands;
using Luxia.Messaging.Events;
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
public sealed class LuxiaRuntime : IAsyncDisposable
{
    private readonly ILoggerFactory _loggers;
    private readonly ILogger _logger;
    private readonly ISerialPortProvider _serialPorts;
    private readonly Lock _lock = new();
    private ArduinoOutputDriver? _arduino;
    private RecorderOutputDriver? _recorder;
    private readonly SleepInhibitor _sleepInhibitor;
    private bool _started;
    private bool _stopped;

    /// <summary>Assemble les modules (sans rien démarrer).</summary>
    public LuxiaRuntime(DataPaths paths, ILoggerFactory loggers, ISerialPortProvider? serialPorts = null, IClock? clock = null)
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
        Engine = new RenderEngine(Router, Clock, universes, loggers.CreateLogger<RenderEngine>());
        Loop = new TickLoop(Engine.Tick, Clock, Preferences.Current.TickRateHz, loggers.CreateLogger<TickLoop>());
        Project = new ProjectSession(Preferences, loggers.CreateLogger<ProjectSession>());
        Project.OpenLast();
        _sleepInhibitor = new SleepInhibitor(loggers.CreateLogger<SleepInhibitor>());
        Library = new Fixtures.FixtureLibrary(paths.Library, loggers.CreateLogger<Fixtures.FixtureLibrary>());
        Library.Load();
    }

    /// <summary>Bibliothèque d'appareils (<c>Documents\LuXia\Bibliothèque</c>).</summary>
    public Fixtures.FixtureLibrary Library { get; }

    /// <summary>
    /// Fabrique de journaux (GEN-110) : permet à un modèle de vue de créer son propre journal, dans le même
    /// fichier technique (<c>Documents\LuXia\Journaux\technique-AAAAMMJJ.log</c>) que le reste de l'application.
    /// </summary>
    public ILoggerFactory Loggers => _loggers;

    /// <summary>Projet ouvert.</summary>
    public ProjectSession Project { get; }

    /// <summary>Emplacements des données.</summary>
    public DataPaths Paths { get; }

    /// <summary>Horloge du moteur.</summary>
    public IClock Clock { get; }

    /// <summary>Préférences du poste.</summary>
    public PreferencesStore Preferences { get; }

    /// <summary>Message éventuel issu du chargement des préférences (migration, fichier mis de côté).</summary>
    public string? PreferencesLoadMessage { get; }

    /// <summary>Bus d'événements.</summary>
    public EventBus Bus { get; }

    /// <summary>Moteur de rendu (porte d'entrée des commandes).</summary>
    public RenderEngine Engine { get; }

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
    /// Arrêt propre : arrêt de la boucle, trame de blackout envoyée à toutes les sorties
    /// (les appareils s'éteignent tout de suite, sans attendre le chien de garde), fermeture des pilotes.
    /// Le fondu au noir de GEN-061 viendra en P5.
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
