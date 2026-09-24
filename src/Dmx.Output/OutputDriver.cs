using System.Diagnostics;
using Dmx.Core.Dmx;
using Dmx.Messaging.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dmx.Output;

/// <summary>
/// Base des pilotes de sortie. Chaque pilote tourne sur son propre fil (SORT-002) et ne garde que
/// la trame la plus récente (SORT-003) : <see cref="Post"/> copie la trame dans un emplacement unique
/// et rend la main immédiatement ; le fil du pilote émet la dernière trame disponible.
/// En cas d'échec, le pilote passe en <c>Erreur</c> puis retente la connexion toutes les secondes (SORT-013, SORT-022).
/// </summary>
public abstract class OutputDriver : IDisposable
{
    /// <summary>Délai entre deux tentatives de connexion (SORT-013).</summary>
    public static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(1);

    private readonly byte[] _slot = new byte[DmxConstants.ChannelCount];
    private readonly byte[] _sending = new byte[DmxConstants.ChannelCount];
    private readonly Lock _slotLock = new();
    private readonly AutoResetEvent _frameAvailable = new(false);
    private readonly Lock _statusLock = new();
    private Thread? _thread;
    private volatile bool _running;
    private bool _hasFrame;
    private TimeSpan _slotTimestamp;
    private OutputDriverStatus _status = new(OutputConnectionState.Disconnected, null, 0, 0, TimeSpan.Zero);
    private long _framesInWindow;
    private long _windowStart = Stopwatch.GetTimestamp();

    /// <summary>Crée un pilote.</summary>
    protected OutputDriver(string id, string name, ILogger? logger)
    {
        Id = id;
        Name = name;
        Logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Identifiant stable (préférences, journal).</summary>
    public string Id { get; }

    /// <summary>Nom affiché.</summary>
    public string Name { get; }

    /// <summary>État courant.</summary>
    public OutputDriverStatus Status
    {
        get
        {
            lock (_statusLock)
            {
                return _status;
            }
        }
    }

    /// <summary>Levé (sur le fil du pilote) à chaque changement d'état de connexion.</summary>
    public event EventHandler<OutputDriverStatus>? StateChanged;

    /// <summary>Journal.</summary>
    protected ILogger Logger { get; }

    /// <summary>Démarre le fil du pilote.</summary>
    public void Start()
    {
        if (_running)
        {
            return;
        }

        _running = true;
        _thread = new Thread(Run) { Name = $"Sortie – {Name}", IsBackground = true, Priority = ThreadPriority.AboveNormal };
        _thread.Start();
    }

    /// <summary>Arrête le fil du pilote et ferme la liaison.</summary>
    public void Stop()
    {
        if (!_running)
        {
            return;
        }

        _running = false;
        _frameAvailable.Set();
        _thread?.Join();
        _thread = null;
    }

    /// <summary>
    /// Dépose la trame du tick. Ne bloque pas : si la précédente n'est pas encore partie, elle est remplacée (SORT-003).
    /// </summary>
    public void Post(DmxFrame frame, TimeSpan timestamp)
    {
        ArgumentNullException.ThrowIfNull(frame);
        lock (_slotLock)
        {
            frame.ReadOnlyValues.CopyTo(_slot);
            _slotTimestamp = timestamp;
            _hasFrame = true;
        }

        _frameAvailable.Set();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Établit la liaison ; renvoie <c>false</c> (sans lever) si elle est impossible pour l'instant.</summary>
    protected abstract bool Connect();

    /// <summary>Émet une trame ; lève une exception en cas d'échec d'écriture.</summary>
    protected abstract void Write(ReadOnlySpan<byte> frame, TimeSpan timestamp);

    /// <summary>Ferme la liaison (ne lève pas).</summary>
    protected abstract void Disconnect();

    /// <summary>Libère les ressources.</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Stop();
            _frameAvailable.Dispose();
        }
    }

    /// <summary>Change l'état publié et lève <see cref="StateChanged"/> si l'état de connexion change.</summary>
    protected void SetState(OutputConnectionState state, string? message)
    {
        bool changed;
        OutputDriverStatus status;
        lock (_statusLock)
        {
            changed = _status.State != state || _status.Message != message;
            _status = _status with
            {
                State = state,
                Message = message,
                ErrorCount = _status.ErrorCount + (state == OutputConnectionState.Error ? 1 : 0),
                FramesPerSecond = state == OutputConnectionState.Connected ? _status.FramesPerSecond : 0,
            };
            status = _status;
        }

        if (changed)
        {
            StateChanged?.Invoke(this, status);
        }
    }

    private void Run()
    {
        var nextAttempt = TimeSpan.Zero;
        var clock = Stopwatch.StartNew();

        while (_running)
        {
            if (Status.State != OutputConnectionState.Connected)
            {
                if (clock.Elapsed < nextAttempt)
                {
                    _frameAvailable.WaitOne(nextAttempt - clock.Elapsed);
                    continue;
                }

                SetState(OutputConnectionState.Connecting, Status.Message);
                if (!TryConnect())
                {
                    nextAttempt = clock.Elapsed + ReconnectDelay;
                    continue;
                }
            }

            if (!_frameAvailable.WaitOne(ReconnectDelay) || !_running)
            {
                continue;
            }

            TimeSpan timestamp;
            lock (_slotLock)
            {
                if (!_hasFrame)
                {
                    continue;
                }

                _slot.CopyTo(_sending, 0);
                timestamp = _slotTimestamp;
                _hasFrame = false;
            }

            var started = Stopwatch.GetTimestamp();
            try
            {
                Write(_sending, timestamp);
                RecordWrite(Stopwatch.GetElapsedTime(started));
            }
#pragma warning disable CA1031 // SORT-022 : une erreur d'écriture ne remonte jamais au moteur.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                Logger.LogWarning(ex, "Sortie {Sortie} : erreur d'écriture, reconnexion", Name);
                SafeDisconnect();
                SetState(OutputConnectionState.Error, ex.Message);
                nextAttempt = clock.Elapsed + ReconnectDelay;
            }
        }

        SafeDisconnect();
        SetState(OutputConnectionState.Disconnected, null);
    }

    private bool TryConnect()
    {
        try
        {
            if (!Connect())
            {
                return false;
            }

            if (Status.State != OutputConnectionState.Connected)
            {
                SetState(OutputConnectionState.Connected, null);
            }

            return true;
        }
#pragma warning disable CA1031 // Un échec de connexion est un état, pas une panne (GEN-091).
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Logger.LogWarning(ex, "Sortie {Sortie} : échec de connexion", Name);
            SafeDisconnect();
            SetState(OutputConnectionState.Error, ex.Message);
            return false;
        }
    }

    private void SafeDisconnect()
    {
        try
        {
            Disconnect();
        }
#pragma warning disable CA1031 // La fermeture ne doit jamais empêcher la reconnexion.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Logger.LogDebug(ex, "Sortie {Sortie} : erreur à la fermeture", Name);
        }
    }

    private void RecordWrite(TimeSpan duration)
    {
        _framesInWindow++;
        var windowElapsed = Stopwatch.GetElapsedTime(_windowStart);
        lock (_statusLock)
        {
            _status = _status with { LastWriteDuration = duration };
            if (windowElapsed >= TimeSpan.FromSeconds(1))
            {
                _status = _status with { FramesPerSecond = _framesInWindow / windowElapsed.TotalSeconds };
                _framesInWindow = 0;
                _windowStart = Stopwatch.GetTimestamp();
            }
        }
    }
}
