using Luxia.Core.Dmx;
using Luxia.Core.Time;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Luxia.Engine.Timing;

/// <summary>
/// Cadence le moteur en temps réel (GEN-030) sur un fil dédié de haute priorité.
/// Les échéances sont absolues (origine + n × période) : pas de dérive cumulée.
/// Si un tick a plus d'une période de retard, on se recale au lieu de rattraper en rafale.
/// </summary>
public sealed class TickLoop : IDisposable
{
    private readonly Action _tick;
    private readonly IClock _clock;
    private readonly ILogger _logger;
    private readonly LatenessHistogram _histogram = new();
    private Thread? _thread;
    private volatile bool _running;
    private long _periodTicks;
    private TimeSpan _measureStart;

    /// <summary>Crée la boucle.</summary>
    /// <param name="tick">Action exécutée à chaque tick (en pratique <see cref="RenderEngine.Tick"/>).</param>
    /// <param name="clock">Horloge système.</param>
    /// <param name="rateHz">Fréquence (25 à 44 Hz, GEN-030).</param>
    /// <param name="logger">Journal.</param>
    public TickLoop(Action tick, IClock clock, double rateHz = DmxConstants.DefaultTickRateHz, ILogger<TickLoop>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(tick);
        ArgumentNullException.ThrowIfNull(clock);
        _tick = tick;
        _clock = clock;
        _logger = logger ?? NullLogger<TickLoop>.Instance;
        RateHz = rateHz;
    }

    /// <summary>Fréquence visée, bornée à 25-44 Hz ; modifiable à chaud.</summary>
    public double RateHz
    {
        get => TimeSpan.TicksPerSecond / (double)Interlocked.Read(ref _periodTicks);
        set
        {
            var clamped = Math.Clamp(value, DmxConstants.MinTickRateHz, DmxConstants.MaxTickRateHz);
            Interlocked.Exchange(ref _periodTicks, (long)Math.Round(TimeSpan.TicksPerSecond / clamped));
        }
    }

    /// <summary>La boucle tourne.</summary>
    public bool IsRunning => _running;

    /// <summary>Démarre la boucle.</summary>
    public void Start()
    {
        if (_running)
        {
            return;
        }

        _running = true;
        ResetStatistics();
        _thread = new Thread(Run)
        {
            Name = "Moteur – tick",
            IsBackground = true,
            Priority = ThreadPriority.Highest,
        };
        _thread.Start();
        _logger.LogInformation("Boucle du moteur démarrée à {Frequence:F1} Hz", RateHz);
    }

    /// <summary>Arrête la boucle et attend la fin du tick en cours.</summary>
    public void Stop()
    {
        if (!_running)
        {
            return;
        }

        _running = false;
        _thread?.Join();
        _thread = null;
        _logger.LogInformation("Boucle du moteur arrêtée");
    }

    /// <summary>Mesures depuis le démarrage ou la dernière remise à zéro.</summary>
    public TickStatistics Statistics => _histogram.Snapshot(_clock.Now - _measureStart, RateHz);

    /// <summary>Remet les mesures à zéro.</summary>
    public void ResetStatistics()
    {
        _histogram.Reset();
        _measureStart = _clock.Now;
    }

    /// <inheritdoc />
    public void Dispose() => Stop();

    private void Run()
    {
        using var waiter = OperatingSystem.IsWindows() ? new HighResolutionWaiter() : null;
        var next = _clock.Now;

        while (_running)
        {
            var period = new TimeSpan(Interlocked.Read(ref _periodTicks));
            var remaining = next - _clock.Now;
            if (remaining > TimeSpan.Zero)
            {
                if (OperatingSystem.IsWindows() && waiter is not null)
                {
                    waiter.Wait(remaining);
                }
                else
                {
                    Thread.Sleep(remaining);
                }
            }

            var now = _clock.Now;
            _histogram.Add(now - next);

            try
            {
                _tick();
            }
#pragma warning disable CA1031 // Une erreur de tick est journalisée ; la boucle continue (GEN-093).
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(ex, "Erreur pendant un tick du moteur");
            }

            next += period;
            var late = _clock.Now - next;
            if (late > period)
            {
                var skipped = late.Ticks / period.Ticks;
                _histogram.AddSkipped(skipped);
                next += new TimeSpan(skipped * period.Ticks);
            }
        }
    }
}
