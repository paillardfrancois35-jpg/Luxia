using System.Collections.Concurrent;
using System.Diagnostics;
using Luxia.Engine.Timing;
using Microsoft.Extensions.Logging;

namespace Luxia.Audio;

/// <summary>
/// L'écoute de la musique (doc 19) : branche une source (le son joué par le PC), la fait analyser dans le fil de la source
/// (AUD-006) et fournit au moteur, à chaque tick, le tempo et la position dans le temps (<see cref="IAudioFeed"/>).
/// Une erreur de capture ou un changement de périphérique ne touche ni le moteur ni l'interface : l'écoute se reconnecte
/// toute seule (AUD-002, AUD-006) et le moteur garde son dernier tempo (GEN-034).
/// </summary>
/// <remarks>
/// Règle de verrouillage (essai P7) : le verrou ne protège que l'état de l'écoute. **Une source n'est jamais arrêtée ni
/// libérée en le tenant** : le fil de capture peut attendre ce verrou dans <c>OnBlock</c> pendant que <c>Dispose</c> attend
/// la fin de ce fil, ce qui bloquerait tout. On la détache sous le verrou (<c>Detach</c>), puis on la libère dehors.
/// </remarks>
public sealed class AudioListener : IAudioFeed, IDisposable
{
    private const double StaleSeconds = 0.6;

    private readonly IAudioSourceFactory _factory;
    private readonly ILogger _logger;
    private readonly object _gate = new();
    private readonly Timer _watchdog;
    private readonly ConcurrentQueue<AudioEvent> _recent = new();
    private IAudioSource? _source;
    private AudioAnalyzer? _analyzer;
    private Timer? _retry;
    private Snapshot _snapshot = new(AnalysisState.None, 0);
    private long _lastData;
    private long _noticeStamp;
    private int _failures;
    private bool _wanted;
    private bool _disposed;

    /// <summary>Crée l'écoute (rien ne démarre avant <see cref="Start"/>).</summary>
    public AudioListener(IAudioSourceFactory factory, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(logger);
        _factory = factory;
        _logger = logger;
        _watchdog = new Timer(_ => Watch(), null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>L'écoute est demandée (même si le périphérique est en cours de reconnexion).</summary>
    public bool IsListening => Volatile.Read(ref _wanted);

    /// <summary>Périphérique choisi (AUD-003) ; <c>null</c> = le son joué par le PC, sur la sortie par défaut.</summary>
    public string? DeviceId { get; private set; }

    /// <summary>Texte d'état pour l'écran : périphérique écouté, reconnexion ou erreur.</summary>
    public string Status { get; private set; } = "Écoute arrêtée";

    /// <summary>
    /// Dernier événement à signaler à l'utilisateur (changement de périphérique, reprise, erreur) ; il reste affichable
    /// <see cref="NoticeAgeSeconds"/> secondes, car la reconnexion est trop rapide pour que l'état seul se remarque.
    /// </summary>
    public string? Notice { get; private set; }

    /// <summary>Âge de <see cref="Notice"/> en secondes (<see cref="double.MaxValue"/> s'il n'y en a pas).</summary>
    public double NoticeAgeSeconds => _noticeStamp == 0 ? double.MaxValue : Stopwatch.GetElapsedTime(_noticeStamp).TotalSeconds;

    /// <summary>Dernier état de l'analyse.</summary>
    public AnalysisState State => Volatile.Read(ref _snapshot).State;

    /// <summary>Réglages de l'analyse (AUD-081) ; appliqués à la source en cours et aux suivantes.</summary>
    public AudioTuning Tuning { get; private set; } = new();

    /// <summary>Derniers événements musicaux (break, drop, silence…), du plus ancien au plus récent (AUD-080).</summary>
    public IReadOnlyCollection<AudioEvent> RecentEvents => _recent.ToArray();

    /// <summary>Levé (sur le fil de capture) à chaque événement musical (EVT-022, EVT-023).</summary>
    public event EventHandler<AudioEvent>? EventRaised;

    /// <summary>Levé quand <see cref="Status"/> ou l'état d'écoute change (sur un fil quelconque).</summary>
    public event EventHandler? StatusChanged;

    /// <summary>Périphériques proposés (sorties à écouter et entrées).</summary>
    public IReadOnlyList<AudioDeviceInfo> Devices() => _factory.Devices();

    /// <summary>Choisit le périphérique à écouter ; l'écoute en cours se reconnecte dessus.</summary>
    public void SetDevice(string? deviceId)
    {
        IAudioSource? old = null;
        var reconnect = false;
        lock (_gate)
        {
            if (DeviceId == deviceId)
            {
                return;
            }

            DeviceId = deviceId;
            if (_wanted && !_disposed)
            {
                old = Detach();
                reconnect = true;
            }
        }

        Release(old);
        if (reconnect)
        {
            Connect();
        }
    }

    /// <summary>Change les réglages de l'analyse (sensibilité, temps morts, lissage, octave), effectifs tout de suite.</summary>
    public void Tune(AudioTuning tuning)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        IAudioSource? old = null;
        var reconnect = false;
        lock (_gate)
        {
            var rangeChanged = tuning.MinBpm != Tuning.MinBpm || tuning.MaxBpm != Tuning.MaxBpm;
            Tuning = tuning;
            if (_analyzer is { } analyzer)
            {
                Apply(analyzer);
            }

            if (rangeChanged && _source is not null && _wanted && !_disposed)
            {
                // La plage de tempo se règle à la création de l'analyse : on se reconnecte pour l'appliquer.
                old = Detach();
                reconnect = true;
            }
        }

        Release(old);
        if (reconnect)
        {
            Connect();
        }
    }

    /// <summary>Démarre l'écoute du son joué par le PC.</summary>
    public void Start()
    {
        lock (_gate)
        {
            if (_disposed || _wanted)
            {
                return;
            }

            _wanted = true;
            _failures = 0;
        }

        Connect();
        lock (_gate)
        {
            if (!_disposed && _wanted)
            {
                _watchdog.Change(250, 250);
            }
        }
    }

    /// <summary>Arrête l'écoute.</summary>
    public void Stop()
    {
        IAudioSource? old;
        lock (_gate)
        {
            _wanted = false;
            _watchdog.Change(Timeout.Infinite, Timeout.Infinite);
            _retry?.Dispose();
            _retry = null;
            old = Detach();
            Volatile.Write(ref _snapshot, new Snapshot(AnalysisState.None, 0));
        }

        Release(old);
        SetStatus("Écoute arrêtée");
    }

    /// <inheritdoc />
    public AudioReading Read()
    {
        var snapshot = Volatile.Read(ref _snapshot);
        var state = snapshot.State;
        var age = snapshot.Stamp == 0 ? double.MaxValue : Stopwatch.GetElapsedTime(snapshot.Stamp).TotalSeconds;
        var live = Volatile.Read(ref _wanted) && !state.Silent && age < StaleSeconds;
        if (!live || state.Bpm <= 0)
        {
            return new AudioReading(false, state.Bpm, 0, false, 0, 0, 0, 0);
        }

        // La phase publiée date de la fin du dernier bloc : on l'avance du temps écoulé depuis (horloge prédictive).
        var beats = state.BeatPhase + (age * state.Bpm / 60.0);
        var phase = beats - Math.Floor(beats);
        var bar = state.BarBeat == 0 ? 0 : (((state.BarBeat - 1) + (int)Math.Floor(beats)) % 4) + 1;
        var (bass, treble) = Volatile.Read(ref _analyzer)?.TakePulses() ?? (0, 0);
        return new AudioReading(true, state.Bpm, state.Confidence, state.HasGrid, phase, bar, bass, treble, state.Energy);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        IAudioSource? old;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _wanted = false;
            _watchdog.Dispose();
            _retry?.Dispose();
            old = Detach();
        }

        Release(old);
    }

    /// <summary>Détache la source courante (verrou tenu par l'appelant) ; à libérer ensuite avec <see cref="Release"/>, hors verrou.</summary>
    private IAudioSource? Detach()
    {
        var source = _source;
        _source = null;
        _analyzer = null;
        if (source is not null)
        {
            source.BlockAvailable -= OnBlock;
            source.Stopped -= OnStopped;
        }

        return source;
    }

    /// <summary>Arrête et libère une source détachée. À appeler **sans** tenir le verrou (voir les remarques de la classe).</summary>
    private void Release(IAudioSource? source)
    {
        if (source is null)
        {
            return;
        }

        try
        {
            source.StopCapture();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Arrêt de la capture");
        }

        try
        {
            source.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Libération de la source audio");
        }
    }

    private void Connect()
    {
        IAudioSource? source = null;
        try
        {
            source = _factory.Create(DeviceId);
            bool unused;
            lock (_gate)
            {
                // Arrêtée, libérée ou déjà reconnectée entre-temps : cette source n'a plus lieu d'être.
                unused = !_wanted || _disposed || _source is not null;
                if (!unused)
                {
                    var analyzer = new AudioAnalyzer(source.SampleRate, Tuning.MinBpm, Tuning.MaxBpm);
                    Apply(analyzer);
                    analyzer.EventRaised += OnAudioEvent;
                    _analyzer = analyzer;
                    _source = source;
                    source.BlockAvailable += OnBlock;
                    source.Stopped += OnStopped;
                    _lastData = Stopwatch.GetTimestamp();
                }
            }

            if (unused)
            {
                Release(source);
                return;
            }

            source.StartCapture();
            var recovered = _failures > 0;
            _failures = 0;
            SetStatus($"Écoute : {source.Name}");
            if (recovered)
            {
                SetNotice($"Écoute reprise sur « {source.Name} »");
            }

            _logger.LogInformation("Écoute du son du PC : {Peripherique} à {Frequence} Hz", source.Name, source.SampleRate);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Écoute du son impossible");
            IAudioSource? failed;
            lock (_gate)
            {
                failed = ReferenceEquals(_source, source) ? Detach() : null;
            }

            Release(failed ?? source);
            _failures++;
            SetStatus("Écoute impossible : " + ex.Message);
            SetNotice("Écoute impossible : " + ex.Message + (DeviceId is not null && ex is UnauthorizedAccessException ? " (autorisez l'accès au micro : Paramètres Windows → Confidentialité → Microphone → applications de bureau)" : string.Empty));
            ScheduleRetry(RetryDelay());
        }
    }

    /// <summary>Temporisation croissante (2, 5 puis 10 s) : un périphérique absent ne remplit pas le journal.</summary>
    private int RetryDelay() => _failures <= 1 ? 2000 : _failures == 2 ? 5000 : 10000;

    private void ScheduleRetry(int milliseconds)
    {
        lock (_gate)
        {
            if (!_wanted || _disposed)
            {
                return;
            }

            _retry?.Dispose();
            _retry = new Timer(
                _ =>
                {
                    bool again;
                    lock (_gate)
                    {
                        again = _wanted && !_disposed && _source is null;
                    }

                    if (again)
                    {
                        Connect();
                    }
                },
                null,
                milliseconds,
                Timeout.Infinite);
        }
    }

    private void Apply(AudioAnalyzer analyzer)
    {
        analyzer.PulseSensitivity = Tuning.PulseSensitivity;
        analyzer.BassDeadSeconds = Tuning.BassDeadSeconds;
        analyzer.TrebleDeadSeconds = Tuning.TrebleDeadSeconds;
        analyzer.EnergySmoothingSeconds = Tuning.EnergySmoothingSeconds;
        analyzer.PreferredBpm = Tuning.PreferredBpm;
    }

    private void OnAudioEvent(AudioEvent audioEvent)
    {
        _recent.Enqueue(audioEvent);
        while (_recent.Count > 30)
        {
            _recent.TryDequeue(out _);
        }

        EventRaised?.Invoke(this, audioEvent);
    }

    private void OnBlock(object? sender, AudioBlock block)
    {
        lock (_gate)
        {
            if (_analyzer is not { } analyzer || !ReferenceEquals(sender, _source))
            {
                return;
            }

            try
            {
                analyzer.Push(block.Samples.Span);
                _lastData = Stopwatch.GetTimestamp();
                Publish(analyzer);
                return;
            }
            catch (Exception ex)
            {
                // AUD-006 : une erreur d'analyse ne doit jamais remonter au fil de capture (ni au moteur, ni à l'interface).
                _logger.LogError(ex, "Erreur d'analyse audio : l'écoute redémarre");
            }
        }

        // Erreur d'analyse : on repart sur une source neuve (hors verrou pour la libérer).
        HandleStop(sender, new InvalidOperationException("erreur d'analyse"));
    }

    private void OnStopped(object? sender, Exception? error) => HandleStop(sender, error);

    private void HandleStop(object? sender, Exception? error)
    {
        IAudioSource? old;
        lock (_gate)
        {
            if (!ReferenceEquals(sender, _source))
            {
                return;
            }

            old = Detach();
        }

        Release(old);
        bool wanted;
        lock (_gate)
        {
            wanted = _wanted && !_disposed;
        }

        if (!wanted)
        {
            return;
        }

        // AUD-002 : changement de périphérique (pas d'erreur) : on se rebranche vite ; AUD-006 : erreur : on réessaie.
        var message = error is null ? "Changement de périphérique audio : reconnexion…" : "Écoute interrompue : " + error.Message + " (reconnexion…)";
        SetStatus(message);
        SetNotice(message);
        _logger.LogWarning(error, "Capture audio arrêtée ({Motif})", error is null ? "changement de périphérique" : "erreur");
        ScheduleRetry(error is null ? 300 : 2000);
    }

    /// <summary>Sans bloc depuis un moment (rien ne joue : la boucle WASAPI ne livre rien), on fait entendre du silence à l'analyse.</summary>
    private void Watch()
    {
        lock (_gate)
        {
            if (_analyzer is not { } analyzer || !_wanted)
            {
                return;
            }

            if (Stopwatch.GetElapsedTime(_lastData).TotalSeconds < 0.4)
            {
                return;
            }

            try
            {
                analyzer.Push(new float[(int)(analyzer.SampleRate * 0.25)]);
                Publish(analyzer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur d'analyse audio pendant le silence");
            }
        }
    }

    private void Publish(AudioAnalyzer analyzer)
    {
        var state = analyzer.State;

        // Les trames se publient par quatre : on ramène la phase à l'instant de la dernière trame reçue.
        var behind = (analyzer.FrameCount - state.FrameIndex) / analyzer.FrameRate;
        var beats = state.BeatPhase + (behind * state.Bpm / 60.0);
        var corrected = state with { BeatPhase = beats - Math.Floor(beats) };
        Volatile.Write(ref _snapshot, new Snapshot(corrected, Stopwatch.GetTimestamp()));
    }

    private void SetStatus(string status)
    {
        Status = status;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SetNotice(string notice)
    {
        Notice = notice;
        _noticeStamp = Stopwatch.GetTimestamp();
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed record Snapshot(AnalysisState State, long Stamp);
}
