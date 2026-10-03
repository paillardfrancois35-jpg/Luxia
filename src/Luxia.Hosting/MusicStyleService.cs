using Luxia.Core.Time;
using Luxia.Engine;
using Luxia.Media;
using Luxia.Messaging.Commands;
using Luxia.Messaging.Events;
using Luxia.Music.Base;
using Luxia.Music.Identification;
using Luxia.Music.Normalization;
using Microsoft.Extensions.Logging;

namespace Luxia.Hosting;

/// <summary>
/// Le style du morceau en cours (P9, doc 21) : écoute la lecture en cours de Windows, normalise et identifie le titre avec la base
/// musicale du projet, informe le moteur (style pour les shows, changement de morceau réel) et le bus (EVT-042), garde le style
/// imposé (CMD-062) et applique les corrections faites en Live (MUS-024), enregistrées dans le projet. La base et ses règles se
/// chargent au premier morceau (démarrage de l'application inchangé) et se rechargent quand le projet change.
/// </summary>
public sealed class MusicStyleService : IDisposable
{
    private static readonly TimeSpan SaveDelay = TimeSpan.FromSeconds(1.5);

    private readonly ProjectSession _project;
    private readonly RenderEngine _engine;
    private readonly IEventBus _bus;
    private readonly IClock _clock;
    private readonly NowPlayingTracker? _tracker;
    private readonly ILogger _logger;
    private readonly object _gate = new();
    private StyleSession? _session;
    private string? _folder;
    private Timer? _saveTimer;
    private int _trackChangedPending;
    private bool _disposed;

    /// <summary>Crée le service (rien n'est chargé avant le premier morceau ou le premier accès à la base).</summary>
    /// <param name="project">Projet ouvert (dossier de la base musicale).</param>
    /// <param name="engine">Moteur de sortie.</param>
    /// <param name="bus">Bus d'événements.</param>
    /// <param name="clock">Horloge du moteur.</param>
    /// <param name="tracker">Lecture en cours de Windows ; <c>null</c> sans elle (outils, tests).</param>
    /// <param name="logger">Journal.</param>
    public MusicStyleService(ProjectSession project, RenderEngine engine, IEventBus bus, IClock clock, NowPlayingTracker? tracker, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);
        _project = project;
        _engine = engine;
        _bus = bus;
        _clock = clock;
        _tracker = tracker;
        _logger = logger;
        if (tracker is not null)
        {
            tracker.TrackChanged += OnTrackChanged;
            tracker.PlaybackChanged += OnPlaybackChanged;
        }
    }

    /// <summary>Levé (sur un fil quelconque) à chaque changement de style : nouveau morceau, style imposé, correction.</summary>
    public event EventHandler<StyleState>? StateChanged;

    /// <summary>État du style du morceau en cours (sans morceau : <see cref="StyleState.None"/>).</summary>
    public StyleState State
    {
        get
        {
            lock (_gate)
            {
                return _session?.State ?? StyleState.None;
            }
        }
    }

    /// <summary>Familles de styles, pour les listes de correction et d'imposition (charge la base au premier appel).</summary>
    public IReadOnlyList<MusicFamily> Families => Session().Base.Taxonomy.Families;

    /// <summary>La base musicale du projet (charge la base au premier appel).</summary>
    public MusicBase Base => Session().Base;

    /// <summary>Un style imposé s'arrête avec le morceau (réglable, MUS-026).</summary>
    public bool ForceUntilTrackEnd
    {
        get => Session().ForceUntilTrackEnd;
        set => Session().ForceUntilTrackEnd = value;
    }

    /// <summary>Impose un style (CMD-062) ; <c>null</c> = retour à la détection.</summary>
    /// <param name="familyIdOrName">Famille (identifiant, nom ou partie du nom).</param>
    /// <returns><c>false</c> si la famille est inconnue.</returns>
    public bool Force(string? familyIdOrName) => Session().Force(familyIdOrName);

    /// <summary>Corrige le style du morceau en cours (MUS-024) ; enregistré dans le projet.</summary>
    /// <param name="scope">Ce titre ou cet artiste.</param>
    /// <param name="familyIdOrName">Famille choisie.</param>
    /// <returns>Un message d'erreur, ou <c>null</c> si la correction est faite.</returns>
    public string? Correct(CorrectionScope scope, string familyIdOrName) => Session().Correct(scope, familyIdOrName, DateTimeOffset.Now);

    /// <summary>
    /// Saisie à la main du morceau (MUS-007) quand aucune application n'en fournit : identifie ce titre comme s'il venait d'un lecteur.
    /// </summary>
    /// <param name="title">Titre.</param>
    /// <param name="artist">Artiste.</param>
    public void SetManualTrack(string title, string artist)
    {
        Interlocked.Exchange(ref _trackChangedPending, 1);
        Session().Update(title, artist, "saisie manuelle");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Timer? timer;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            timer = _saveTimer;
            _saveTimer = null;
        }

        if (_tracker is not null)
        {
            _tracker.TrackChanged -= OnTrackChanged;
            _tracker.PlaybackChanged -= OnPlaybackChanged;
        }

        if (timer is not null)
        {
            // Une correction non enregistrée ne doit pas se perdre à la fermeture.
            timer.Dispose();
            SaveNow();
        }
    }

    private StyleSession Session()
    {
        lock (_gate)
        {
            var folder = _project.Folder;
            if (_session is not null && folder == _folder)
            {
                return _session;
            }

            var (musicBase, messages) = folder is null ? (MusicStore.Default(), []) : MusicStore.Load(folder);
            foreach (var message in messages)
            {
                _logger.LogWarning("Base musicale : {Message}", message);
            }

            musicBase.Changed += (_, _) => ScheduleSave();
            if (_session is null)
            {
                var rules = folder is null ? NormalizationRules.Default : NormalizationStore.Load(folder).Value;
                _session = new StyleSession(musicBase, new TrackNormalizer(rules));
                _session.StyleChanged += OnStyleChanged;
            }
            else
            {
                _session.ReplaceBase(musicBase);
            }

            _folder = folder;
            _logger.LogInformation("Base musicale : {Artistes} artistes, {Titres} titres", musicBase.ArtistCount, musicBase.TitleCount);
            return _session;
        }
    }

    private void OnTrackChanged(object? sender, TrackChange change)
    {
        try
        {
            var session = Session();
            if (change.Track is { } track)
            {
                Interlocked.Exchange(ref _trackChangedPending, 1);
                session.Update(track.Title, track.Artist, track.App, track.Genres);
            }
            else
            {
                Interlocked.Exchange(ref _trackChangedPending, 0);
                session.Clear();
            }
        }
        catch (Exception ex)
        {
            // Une base illisible ou un titre inattendu ne doit jamais toucher le moteur ni l'écoute (MUS-006).
            _logger.LogError(ex, "Identification du style");
        }
    }

    private void OnPlaybackChanged(object? sender, bool playing)
    {
        // Le moteur ne se fie au changement de titre réel que pendant une lecture suivie.
        var state = State;
        _engine.Send(new SetMusicContextCommand(CommandOrigin.Tool, state.StyleName, false, state.HasTrack && playing));
    }

    private void OnStyleChanged(object? sender, StyleState state)
    {
        var changed = Interlocked.Exchange(ref _trackChangedPending, 0) == 1;
        var playing = _tracker?.Current.Playing ?? false;
        var detectedName = state.HasTrack ? state.Detected.FamilyName : null;
        _engine.Send(new SetMusicContextCommand(CommandOrigin.Tool, detectedName, changed, state.HasTrack && (playing || _tracker is null)));
        _engine.Send(new ForceStyleCommand(CommandOrigin.Tool, state.Forced ? state.Effective.FamilyName : null));
        _bus.Publish(new StyleDetected(
            state.HasTrack ? state.Effective.FamilyId : string.Empty,
            state.HasTrack ? state.Effective.FamilyName : string.Empty,
            state.HasTrack ? state.Effective.Confidence : 0,
            MethodText(state.Effective.Method),
            state.Forced,
            state.Title,
            state.Artist,
            _clock.Now));
        StateChanged?.Invoke(this, state);
    }

    private static string MethodText(IdentificationMethod method) => method switch
    {
        IdentificationMethod.Correction => "correction",
        IdentificationMethod.ExactTitle => "titre exact",
        IdentificationMethod.FuzzyTitle => "titre approché",
        IdentificationMethod.ExactArtist => "artiste",
        IdentificationMethod.FuzzyArtist => "artiste approché",
        IdentificationMethod.Guest => "invité",
        IdentificationMethod.PlayerGenre => "genre du lecteur",
        IdentificationMethod.Forced => "imposé",
        _ => "aucune",
    };

    private void ScheduleSave()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _saveTimer ??= new Timer(_ => SaveNow(), null, Timeout.Infinite, Timeout.Infinite);
            _saveTimer.Change(SaveDelay, Timeout.InfiniteTimeSpan);
        }
    }

    private void SaveNow()
    {
        try
        {
            MusicBase? musicBase;
            string? folder;
            lock (_gate)
            {
                musicBase = _session?.Base;
                folder = _folder;
            }

            if (musicBase is not null && folder is not null)
            {
                MusicStore.Save(folder, musicBase);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enregistrement de la base musicale");
        }
    }
}
