using Microsoft.Extensions.Logging;
using Windows.Media.Control;

namespace Luxia.Media.Windows;

/// <summary>
/// Les sessions média de Windows (MUS-001, Q43) : Deezer, YouTube Music dans un navigateur, VLC… tout lecteur qui s'annonce au
/// système (touches multimédia, volume). Le système répond de façon asynchrone ; tant qu'il n'a pas répondu, la liste est vide.
/// Un événement du système ne porte jamais la valeur : on relit alors toutes les sessions (titre, état, position), en regroupant
/// les rafales d'événements.
/// </summary>
public sealed class WindowsMediaSessionSource : IMediaSessionSource
{
    private static readonly TimeSpan SafetyRefresh = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(120);

    private readonly ILogger _logger;
    private readonly object _gate = new();
    private readonly Dictionary<string, GlobalSystemMediaTransportControlsSession> _subscribed = [];
    private readonly SemaphoreSlim _refreshing = new(1, 1);
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private IReadOnlyList<MediaSessionInfo> _sessions = [];
    private Timer? _safety;
    private Timer? _debounce;
    private bool _disposed;

    /// <summary>Crée la source et interroge le système en arrière-plan (rien ne bloque l'appelant).</summary>
    /// <param name="logger">Journal.</param>
    public WindowsMediaSessionSource(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _ = Task.Run(InitializeAsync);
    }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public IReadOnlyList<MediaSessionInfo> Sessions()
    {
        lock (_gate)
        {
            return _sessions;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GlobalSystemMediaTransportControlsSessionManager? manager;
        List<GlobalSystemMediaTransportControlsSession> sessions;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _safety?.Dispose();
            _debounce?.Dispose();
            manager = _manager;
            _manager = null;
            sessions = [.. _subscribed.Values];
            _subscribed.Clear();
        }

        if (manager is not null)
        {
            manager.SessionsChanged -= OnSessionsChanged;
        }

        foreach (var session in sessions)
        {
            Unsubscribe(session);
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask().ConfigureAwait(false);
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _manager = manager;
                _safety = new Timer(_ => Request(), null, SafetyRefresh, SafetyRefresh);
            }

            manager.SessionsChanged += OnSessionsChanged;
            await RefreshAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Windows sans service média (très ancien, bac à sable) : aucune session, le reste de l'application fonctionne (MUS-006).
            _logger.LogWarning(ex, "Lecture en cours de Windows indisponible");
        }
    }

    private void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args) => Request();

    private void OnSessionPropertyChanged(GlobalSystemMediaTransportControlsSession sender, object args) => Request();

    private void Unsubscribe(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            session.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            session.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Désabonnement d'une session média");
        }
    }

    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args) => Request();

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args) => Request();

    private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args) => Request();

    /// <summary>Demande une relecture, regroupée : une rafale d'événements (changement de titre) n'en fait qu'une.</summary>
    private void Request()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _debounce ??= new Timer(_ => _ = RefreshAsync(), null, Timeout.Infinite, Timeout.Infinite);
            _debounce.Change(Debounce, Timeout.InfiniteTimeSpan);
        }
    }

    private async Task RefreshAsync()
    {
        if (!await _refreshing.WaitAsync(0).ConfigureAwait(false))
        {
            // Une relecture est déjà en cours : on en reprogramme une pour ne rien rater.
            Request();
            return;
        }

        try
        {
            GlobalSystemMediaTransportControlsSessionManager? manager;
            lock (_gate)
            {
                manager = _manager;
            }

            if (manager is null)
            {
                return;
            }

            var infos = new List<MediaSessionInfo>();
            var current = manager.GetSessions().ToList();
            SynchronizeSubscriptions(current);
            foreach (var session in current)
            {
                if (await ReadAsync(session).ConfigureAwait(false) is { } info)
                {
                    infos.Add(info);
                }
            }

            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _sessions = infos;
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Relecture des sessions média");
        }
        finally
        {
            _refreshing.Release();
        }
    }

    private void SynchronizeSubscriptions(List<GlobalSystemMediaTransportControlsSession> current)
    {
        lock (_gate)
        {
            var ids = current.Select(s => s.SourceAppUserModelId).ToHashSet();
            foreach (var gone in _subscribed.Keys.Where(k => !ids.Contains(k)).ToList())
            {
                Unsubscribe(_subscribed[gone]);
                _subscribed.Remove(gone);
            }

            foreach (var session in current)
            {
                if (_subscribed.TryAdd(session.SourceAppUserModelId, session))
                {
                    session.MediaPropertiesChanged += OnMediaPropertiesChanged;
                    session.PlaybackInfoChanged += OnPlaybackInfoChanged;
                    session.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
                }
            }
        }
    }

    private async Task<MediaSessionInfo?> ReadAsync(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            var id = session.SourceAppUserModelId;
            var playbackInfo = session.GetPlaybackInfo();
            var playback = playbackInfo.PlaybackStatus switch
            {
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => MediaPlayback.Playing,
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => MediaPlayback.Paused,
                _ => MediaPlayback.Stopped,
            };
            var properties = await session.TryGetMediaPropertiesAsync().AsTask().ConfigureAwait(false);
            var timeline = session.GetTimelineProperties();

            // Les lecteurs qui ne donnent pas de position la laissent à zéro, sans date de mise à jour : on la traite comme inconnue.
            var hasTimeline = timeline is not null && timeline.LastUpdatedTime.Year > 1601 && (timeline.EndTime > TimeSpan.Zero || timeline.Position > TimeSpan.Zero);
            var duration = timeline is null ? TimeSpan.Zero : timeline.EndTime - timeline.StartTime;
            return new MediaSessionInfo(
                id,
                MediaApps.FriendlyName(id),
                properties?.Title ?? string.Empty,
                properties?.Artist ?? string.Empty,
                properties?.AlbumTitle ?? string.Empty,
                playback,
                hasTimeline ? timeline!.Position : null,
                hasTimeline ? timeline!.LastUpdatedTime : null,
                duration < TimeSpan.Zero ? TimeSpan.Zero : duration,
                playbackInfo.PlaybackRate ?? 1.0);
        }
        catch (Exception ex)
        {
            // Une session qui disparaît pendant la lecture de ses propriétés : on l'ignore, la prochaine relecture la reverra.
            _logger.LogDebug(ex, "Lecture d'une session média");
            return null;
        }
    }
}
