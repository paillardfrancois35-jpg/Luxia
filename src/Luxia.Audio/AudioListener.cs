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
public sealed class AudioListener : IAudioFeed, IDisposable
{
    private const double StaleSeconds = 0.6;

    private readonly IAudioSourceFactory _factory;
    private readonly ILogger _logger;
    private readonly object _gate = new();
    private readonly Timer _watchdog;
    private IAudioSource? _source;
    private AudioAnalyzer? _analyzer;
    private Timer? _retry;
    private Snapshot _snapshot = new(AnalysisState.None, 0);
    private long _lastData;
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

    /// <summary>Texte d'état pour l'écran : périphérique écouté, reconnexion ou erreur.</summary>
    public string Status { get; private set; } = "Écoute arrêtée";

    /// <summary>Dernier état de l'analyse.</summary>
    public AnalysisState State => Volatile.Read(ref _snapshot).State;

    /// <summary>Levé quand <see cref="Status"/> ou l'état d'écoute change (sur un fil quelconque).</summary>
    public event EventHandler? StatusChanged;

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
            Connect();
            _watchdog.Change(250, 250);
        }
    }

    /// <summary>Arrête l'écoute.</summary>
    public void Stop()
    {
        lock (_gate)
        {
            _wanted = false;
            _watchdog.Change(Timeout.Infinite, Timeout.Infinite);
            _retry?.Dispose();
            _retry = null;
            Disconnect();
            Volatile.Write(ref _snapshot, new Snapshot(AnalysisState.None, 0));
            SetStatus("Écoute arrêtée");
        }
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
        return new AudioReading(true, state.Bpm, state.Confidence, state.HasGrid, phase, bar, 0, 0);
    }

    /// <inheritdoc />
    public void Dispose()
    {
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
            Disconnect();
        }
    }

    private void Connect()
    {
        try
        {
            var source = _factory.CreateLoopback();
            _analyzer = new AudioAnalyzer(source.SampleRate);
            _source = source;
            source.BlockAvailable += OnBlock;
            source.Stopped += OnStopped;
            source.StartCapture();
            _lastData = Stopwatch.GetTimestamp();
            SetStatus($"Écoute : {source.Name}");
            _logger.LogInformation("Écoute du son du PC : {Peripherique} à {Frequence} Hz", source.Name, source.SampleRate);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Écoute du son impossible");
            Disconnect();
            SetStatus("Écoute impossible : " + ex.Message + " (nouvel essai dans 2 s)");
            ScheduleRetry(2000);
        }
    }

    private void Disconnect()
    {
        var source = _source;
        _source = null;
        _analyzer = null;
        if (source is null)
        {
            return;
        }

        source.BlockAvailable -= OnBlock;
        source.Stopped -= OnStopped;
        try
        {
            source.StopCapture();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Arrêt de la capture");
        }

        source.Dispose();
    }

    private void ScheduleRetry(int milliseconds)
    {
        _retry?.Dispose();
        _retry = new Timer(
            _ =>
            {
                lock (_gate)
                {
                    if (_wanted && !_disposed && _source is null)
                    {
                        Connect();
                    }
                }
            },
            null,
            milliseconds,
            Timeout.Infinite);
    }

    private void OnBlock(object? sender, AudioBlock block)
    {
        lock (_gate)
        {
            if (_analyzer is not { } analyzer || !ReferenceEquals(sender, _source))
            {
                return;
            }

            analyzer.Push(block.Samples.Span);
            _lastData = Stopwatch.GetTimestamp();
            Publish(analyzer);
        }
    }

    private void OnStopped(object? sender, Exception? error)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(sender, _source))
            {
                return;
            }

            Disconnect();
            if (!_wanted || _disposed)
            {
                return;
            }

            // AUD-002 : changement de périphérique (pas d'erreur) : on se rebranche vite ; AUD-006 : erreur : on réessaie.
            SetStatus(error is null ? "Changement de périphérique audio : reconnexion…" : "Écoute interrompue : " + error.Message + " (reconnexion…)");
            _logger.LogWarning(error, "Capture audio arrêtée ({Motif})", error is null ? "changement de périphérique" : "erreur");
            ScheduleRetry(error is null ? 300 : 2000);
        }
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

            analyzer.Push(new float[(int)(analyzer.SampleRate * 0.25)]);
            Publish(analyzer);
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

    private sealed record Snapshot(AnalysisState State, long Stamp);
}
