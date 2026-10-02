using Microsoft.Extensions.Logging;

namespace Luxia.Media;

/// <summary>
/// La lecture en cours (doc 21 §2) : choisit la session média à suivre (MUS-001), publie les changements de morceau une fois
/// stabilisés (MUS-002), estime la position entre deux mises à jour du lecteur (MUS-003). Sans session, l'état est « aucune
/// lecture » et le reste du système fonctionne (MUS-006).
/// </summary>
/// <remarks>
/// Règle de choix : parmi les sessions en lecture, la dernière démarrée ; sinon la session suivie jusque-là si elle existe
/// encore ; sinon la dernière qui a joué depuis le démarrage (une session restée en pause n'est jamais choisie). Un titre vide (transition d'un lecteur) ne compte pas comme un changement.
/// Les événements sont levés hors du verrou, sur le fil qui a appelé <see cref="Poll"/>.
/// </remarks>
public sealed class NowPlayingTracker : IDisposable
{
    /// <summary>Durée pendant laquelle un nouveau titre doit rester présent avant d'être publié (MUS-002).</summary>
    public static readonly TimeSpan Stabilization = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    private readonly IMediaSessionSource _source;
    private readonly TimeProvider _time;
    private readonly ILogger _logger;
    private readonly object _gate = new();
    private readonly Dictionary<string, DateTimeOffset> _playingSince = [];
    private readonly Dictionary<string, DateTimeOffset> _lastActive = [];
    private Timer? _timer;
    private IReadOnlyList<MediaSessionInfo> _sessions = [];
    private string? _selectedId;
    private NowPlayingTrack? _published;
    private string? _publishedKey;
    private string? _candidateKey;
    private DateTimeOffset _candidateSince;
    private bool _playing;
    private bool _disposed;

    /// <summary>Crée le suivi (rien ne tourne avant <see cref="Start"/> ; la source appartient au suivi et est libérée avec lui).</summary>
    /// <param name="source">Source des sessions.</param>
    /// <param name="logger">Journal.</param>
    /// <param name="time">Temps (réel par défaut ; simulé en test).</param>
    public NowPlayingTracker(IMediaSessionSource source, ILogger logger, TimeProvider? time = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(logger);
        _source = source;
        _logger = logger;
        _time = time ?? TimeProvider.System;
    }

    /// <summary>Levé quand le morceau change, une fois stabilisé (EVT-040), ou quand il n'y en a plus.</summary>
    public event EventHandler<TrackChange>? TrackChanged;

    /// <summary>Levé quand la lecture démarre (<c>true</c>) ou s'arrête (<c>false</c>) (EVT-041).</summary>
    public event EventHandler<bool>? PlaybackChanged;

    /// <summary>Levé quand quelque chose d'affichable change (morceau, lecture, session suivie, liste des sessions).</summary>
    public event EventHandler? StateChanged;

    /// <summary>Sessions vues au dernier relevé.</summary>
    public IReadOnlyList<MediaSessionInfo> Sessions
    {
        get
        {
            lock (_gate)
            {
                return _sessions;
            }
        }
    }

    /// <summary>Identifiant de la session suivie ; <c>null</c> s'il n'y en a pas.</summary>
    public string? SelectedSessionId
    {
        get
        {
            lock (_gate)
            {
                return _selectedId;
            }
        }
    }

    /// <summary>État courant, position estimée à l'instant de l'appel (MUS-003).</summary>
    public NowPlayingState Current
    {
        get
        {
            lock (_gate)
            {
                var selected = Selected();
                if (selected is null || _published is null)
                {
                    return NowPlayingState.None;
                }

                return new NowPlayingState(_published, _playing, EstimatePosition(selected, _time.GetUtcNow()));
            }
        }
    }

    /// <summary>Démarre le suivi : relevé immédiat, puis à chaque changement de la source et toutes les 250 ms (stabilisation).</summary>
    public void Start()
    {
        lock (_gate)
        {
            if (_disposed || _timer is not null)
            {
                return;
            }

            _timer = new Timer(_ => Guard(), null, PollInterval, PollInterval);
        }

        _source.Changed += OnSourceChanged;
        Guard();
    }

    /// <summary>Relève les sessions, choisit celle à suivre et lève les événements dus. Appelé par le suivi lui-même ; public pour les tests.</summary>
    public void Poll()
    {
        var raised = new List<Action>();
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            var now = _time.GetUtcNow();
            var sessions = _source.Sessions();
            var changed = !sessions.SequenceEqual(_sessions);
            _sessions = sessions;
            TrackActivity(sessions, now);
            var previousSelected = _selectedId;
            _selectedId = Choose(sessions);
            changed |= previousSelected != _selectedId;
            var selected = Selected();

            var playing = selected?.Playback == MediaPlayback.Playing;
            if (playing != _playing)
            {
                _playing = playing;
                changed = true;
                raised.Add(() => PlaybackChanged?.Invoke(this, playing));
            }

            changed |= UpdateTrack(selected, now, raised);
            if (changed)
            {
                raised.Add(() => StateChanged?.Invoke(this, EventArgs.Empty));
            }
        }

        foreach (var action in raised)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                // Un abonné défaillant ne doit jamais arrêter le suivi.
                _logger.LogError(ex, "Erreur dans un abonné à la lecture en cours");
            }
        }
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
            _timer?.Dispose();
            _timer = null;
        }

        _source.Changed -= OnSourceChanged;
        _source.Dispose();
    }

    private static string Key(MediaSessionInfo session) => string.Join('\u001f', session.Id, session.Title.Trim(), session.Artist.Trim(), session.Album.Trim());

    private static TimeSpan? EstimatePosition(MediaSessionInfo session, DateTimeOffset now)
    {
        if (session.Position is not { } position)
        {
            return null;
        }

        if (session.Playback == MediaPlayback.Playing && session.PositionUpdatedUtc is { } updated)
        {
            var elapsed = now - updated;
            if (elapsed > TimeSpan.Zero)
            {
                position += elapsed * session.Rate;
            }
        }

        if (session.Duration > TimeSpan.Zero && position > session.Duration)
        {
            position = session.Duration;
        }

        return position < TimeSpan.Zero ? TimeSpan.Zero : position;
    }

    private MediaSessionInfo? Selected() => _selectedId is null ? null : _sessions.FirstOrDefault(s => s.Id == _selectedId);

    private void TrackActivity(IReadOnlyList<MediaSessionInfo> sessions, DateTimeOffset now)
    {
        var ids = sessions.Select(s => s.Id).ToHashSet();
        foreach (var gone in _playingSince.Keys.Where(k => !ids.Contains(k)).ToList())
        {
            _playingSince.Remove(gone);
        }

        foreach (var gone in _lastActive.Keys.Where(k => !ids.Contains(k)).ToList())
        {
            _lastActive.Remove(gone);
        }

        foreach (var session in sessions)
        {
            if (session.Playback == MediaPlayback.Playing)
            {
                _playingSince.TryAdd(session.Id, now);
                _lastActive[session.Id] = now;
            }
            else
            {
                _playingSince.Remove(session.Id);
            }
        }
    }

    private string? Choose(IReadOnlyList<MediaSessionInfo> sessions)
    {
        var playing = sessions.Where(s => s.Playback == MediaPlayback.Playing).ToList();
        if (playing.Count > 0)
        {
            // La dernière démarrée ; à égalité (relevé initial), la session déjà suivie, sinon la première.
            var latest = playing.Max(s => _playingSince[s.Id]);
            var candidates = playing.Where(s => _playingSince[s.Id] == latest).ToList();
            return candidates.FirstOrDefault(s => s.Id == _selectedId)?.Id ?? candidates[0].Id;
        }

        if (_selectedId is not null && sessions.Any(s => s.Id == _selectedId))
        {
            return _selectedId;
        }

        // Plus personne ne joue et la session suivie a disparu (Deezer ferme la sienne peu après une pause) : seule une session qui a
        // joué pendant cette exécution peut la remplacer. Une session restée en pause depuis avant n'est pas « la musique en cours » :
        // la prendre ferait annoncer un faux changement de morceau (essai PoC-3 : Chrome en pause après Deezer).
        return sessions
            .Where(s => _lastActive.ContainsKey(s.Id))
            .OrderByDescending(s => _lastActive[s.Id])
            .Select(s => s.Id)
            .FirstOrDefault();
    }

    /// <summary>Met à jour le morceau stabilisé ; renvoie vrai s'il a changé.</summary>
    private bool UpdateTrack(MediaSessionInfo? selected, DateTimeOffset now, List<Action> raised)
    {
        if (selected is null)
        {
            _candidateKey = null;
            if (_published is null)
            {
                return false;
            }

            var previous = _published;
            _published = null;
            _publishedKey = null;
            raised.Add(() => TrackChanged?.Invoke(this, new TrackChange(null, previous)));
            return true;
        }

        if (string.IsNullOrWhiteSpace(selected.Title))
        {
            // Transition d'un lecteur (titre momentanément vide) : on garde le morceau publié et on attend.
            _candidateKey = null;
            return false;
        }

        var key = Key(selected);
        if (key == _publishedKey)
        {
            _candidateKey = null;
            return false;
        }

        if (key != _candidateKey)
        {
            _candidateKey = key;
            _candidateSince = now;
        }

        if (now - _candidateSince < Stabilization)
        {
            return false;
        }

        var old = _published;
        var track = new NowPlayingTrack(selected.Title.Trim(), selected.Artist.Trim(), selected.Album.Trim(), selected.App, selected.Duration);
        _published = track;
        _publishedKey = key;
        _candidateKey = null;
        _logger.LogInformation("Morceau : « {Titre} » — {Artiste} ({Application})", track.Title, track.Artist, track.App);
        raised.Add(() => TrackChanged?.Invoke(this, new TrackChange(track, old)));
        return true;
    }

    private void OnSourceChanged(object? sender, EventArgs e) => Guard();

    /// <summary>Aucune exception ne doit sortir d'un timer ni d'un événement de la source (elle arrêterait l'application).</summary>
    private void Guard()
    {
        try
        {
            Poll();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur dans le suivi de la lecture en cours");
        }
    }
}
