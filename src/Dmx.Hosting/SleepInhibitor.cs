using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dmx.Hosting;

/// <summary>
/// Empêche la mise en veille du PC tant que l'application émet (GEN-096, Q26) : une veille coupe la lumière
/// (plus de trames → chien de garde → noir). Demande temporaire au système, propre au processus
/// (<c>SetThreadExecutionState</c>), sans modifier les réglages du poste ; elle cesse à l'arrêt ou si le processus meurt.
/// L'écran, lui, peut toujours s'éteindre.
/// </summary>
public sealed partial class SleepInhibitor : IDisposable
{
    private const uint EsContinuous = 0x80000000;
    private const uint EsSystemRequired = 0x00000001;

    private readonly ILogger _logger;
    private readonly ManualResetEventSlim _stop = new(false);
    private Thread? _thread;

    /// <summary>Crée l'inhibiteur (inactif).</summary>
    public SleepInhibitor(ILogger<SleepInhibitor>? logger = null) => _logger = logger ?? NullLogger<SleepInhibitor>.Instance;

    /// <summary>La demande est en place.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Pose la demande. L'état d'exécution est lié à un fil : on dédie un fil qui vit jusqu'à <see cref="Stop"/>.</summary>
    public void Start()
    {
        if (_thread is not null || !OperatingSystem.IsWindows())
        {
            return;
        }

        using var ready = new ManualResetEventSlim(false);
        _stop.Reset();
        _thread = new Thread(() => Hold(ready)) { Name = "Anti-veille", IsBackground = true };
        _thread.Start();
        ready.Wait(TimeSpan.FromSeconds(2));
    }

    /// <summary>Lève la demande : le PC peut de nouveau se mettre en veille.</summary>
    public void Stop()
    {
        if (_thread is null)
        {
            return;
        }

        _stop.Set();
        _thread.Join();
        _thread = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Stop();
        _stop.Dispose();
    }

    private void Hold(ManualResetEventSlim ready)
    {
        if (OperatingSystem.IsWindows())
        {
            IsActive = SetState(EsContinuous | EsSystemRequired);
            if (IsActive)
            {
                _logger.LogInformation("Mise en veille du PC bloquée pendant l'émission");
            }
            else
            {
                _logger.LogWarning("Impossible d'empêcher la mise en veille du PC");
            }
        }

        ready.Set();
        _stop.Wait();

        if (OperatingSystem.IsWindows() && IsActive)
        {
            SetState(EsContinuous);
            IsActive = false;
            _logger.LogInformation("Mise en veille du PC de nouveau autorisée");
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool SetState(uint flags) => SetThreadExecutionState(flags) != 0;

    [LibraryImport("kernel32.dll")]
    [SupportedOSPlatform("windows")]
    private static partial uint SetThreadExecutionState(uint flags);
}
