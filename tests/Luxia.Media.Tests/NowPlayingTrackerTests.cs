using Luxia.Media;
using Microsoft.Extensions.Logging.Abstractions;

namespace Luxia.Media.Tests;

/// <summary>Lecture en cours (doc 21 §2) : choix de la session (MUS-001), événements stabilisés (MUS-002), position (MUS-003), repli (MUS-006).</summary>
public sealed class NowPlayingTrackerTests : IDisposable
{
    private static readonly DateTimeOffset Origin = new(2026, 10, 3, 21, 0, 0, TimeSpan.Zero);

    private readonly FakeTime _time = new(Origin);
    private readonly FakeSource _source = new();
    private readonly List<TrackChange> _changes = [];
    private readonly List<bool> _playback = [];
    private readonly NowPlayingTracker _tracker;

    public NowPlayingTrackerTests()
    {
        _tracker = new NowPlayingTracker(_source, NullLogger.Instance, _time);
        _tracker.TrackChanged += (_, c) => _changes.Add(c);
        _tracker.PlaybackChanged += (_, p) => _playback.Add(p);
    }

    public void Dispose() => _tracker.Dispose();

    [Fact]
    [Trait("Exigence", "MUS-006")]
    public void NoSession_NothingPlays_NoEvent()
    {
        // MUS-006 : sans application qui s'annonce, le système fonctionne (état « aucune lecture »).
        _tracker.Poll();
        _time.Advance(5);
        _tracker.Poll();

        _tracker.Current.ShouldBe(NowPlayingState.None);
        _changes.ShouldBeEmpty();
        _playback.ShouldBeEmpty();
        _tracker.SelectedSessionId.ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "MUS-002")]
    public void NewTrack_IsPublishedOnceAfterTheStabilization()
    {
        _source.Set(Session("Deezer", "Titre A", "Artiste A", MediaPlayback.Playing));
        _tracker.Poll();
        _changes.ShouldBeEmpty();
        _playback.ShouldBe([true]);

        _time.Advance(0.5);
        _tracker.Poll();
        _changes.ShouldBeEmpty();

        _time.Advance(0.6);
        _tracker.Poll();
        _time.Advance(3);
        _tracker.Poll();

        _changes.Count.ShouldBe(1);
        _changes[0].Track!.Title.ShouldBe("Titre A");
        _changes[0].Track!.Artist.ShouldBe("Artiste A");
        _changes[0].Previous.ShouldBeNull();
        _tracker.Current.Track!.App.ShouldBe("Deezer");
    }

    [Fact]
    [Trait("Exigence", "MUS-002")]
    public void TitleThatFlickers_IsNotPublished()
    {
        _source.Set(Session("Chrome", "Titre A", "Artiste", MediaPlayback.Playing));
        PollFor(2);
        _changes.Count.ShouldBe(1);

        // Un changement qui ne dure pas 1 s puis revient : aucun événement (MUS-002 : pas de doublon).
        _source.Set(Session("Chrome", "Titre B", "Artiste", MediaPlayback.Playing));
        PollFor(0.6);
        _source.Set(Session("Chrome", "Titre A", "Artiste", MediaPlayback.Playing));
        PollFor(3);

        _changes.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "EVT-040")]
    public void TrackChange_ReportsThePreviousTrack()
    {
        _source.Set(Session("Deezer", "Titre A", "Artiste A", MediaPlayback.Playing));
        PollFor(2);
        _source.Set(Session("Deezer", "Titre B", "Artiste B", MediaPlayback.Playing));
        PollFor(2);

        _changes.Count.ShouldBe(2);
        _changes[1].Track!.Title.ShouldBe("Titre B");
        _changes[1].Previous!.Title.ShouldBe("Titre A");
    }

    [Fact]
    [Trait("Exigence", "MUS-002")]
    public void EmptyTitle_DuringATransition_KeepsTheCurrentTrack()
    {
        _source.Set(Session("Deezer", "Titre A", "Artiste A", MediaPlayback.Playing));
        PollFor(2);
        _source.Set(Session("Deezer", string.Empty, string.Empty, MediaPlayback.Playing));
        PollFor(3);

        _changes.Count.ShouldBe(1);
        _tracker.Current.Track!.Title.ShouldBe("Titre A");
    }

    [Fact]
    [Trait("Exigence", "MUS-001")]
    public void TwoPlayingSessions_TheLastStartedIsFollowed()
    {
        _source.Set(Session("Deezer", "Titre A", "Artiste A", MediaPlayback.Playing));
        PollFor(2);
        _tracker.SelectedSessionId.ShouldBe("Deezer");

        _source.Set(
            Session("Deezer", "Titre A", "Artiste A", MediaPlayback.Playing),
            Session("VLC", "Titre V", "Artiste V", MediaPlayback.Playing));
        PollFor(2);

        _tracker.SelectedSessionId.ShouldBe("VLC");
        _tracker.Current.Track!.Title.ShouldBe("Titre V");
    }

    [Fact]
    [Trait("Exigence", "MUS-001")]
    public void NobodyPlaying_TheFollowedSessionIsKept_ThenTheLastActiveIsChosen()
    {
        _source.Set(
            Session("Deezer", "Titre A", "Artiste A", MediaPlayback.Playing),
            Session("VLC", "Titre V", "Artiste V", MediaPlayback.Paused));
        PollFor(2);
        _tracker.SelectedSessionId.ShouldBe("Deezer");

        // Deezer se met en pause : plus personne ne joue, on garde Deezer (dernière active), le morceau reste affiché.
        _source.Set(
            Session("Deezer", "Titre A", "Artiste A", MediaPlayback.Paused),
            Session("VLC", "Titre V", "Artiste V", MediaPlayback.Paused));
        PollFor(2);
        _tracker.SelectedSessionId.ShouldBe("Deezer");
        _tracker.Current.Playing.ShouldBeFalse();
        _tracker.Current.Track!.Title.ShouldBe("Titre A");

        // Deezer se ferme : VLC, en pause depuis avant et jamais vu en lecture, ne devient pas « le morceau en cours » (plus de morceau).
        _source.Set(Session("VLC", "Titre V", "Artiste V", MediaPlayback.Paused));
        PollFor(2);
        _tracker.SelectedSessionId.ShouldBeNull();
        _tracker.Current.ShouldBe(NowPlayingState.None);
        _changes[^1].Track.ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "MUS-001")]
    public void SessionThatPlayedEarlier_TakesOverWhenTheFollowedOneDisappears()
    {
        _source.Set(Session("Chrome", "Titre C", "Artiste C", MediaPlayback.Playing));
        PollFor(2);
        _source.Set(Session("Chrome", "Titre C", "Artiste C", MediaPlayback.Playing), Session("Deezer", "Titre A", "Artiste A", MediaPlayback.Playing));
        PollFor(2);
        _source.Set(Session("Chrome", "Titre C", "Artiste C", MediaPlayback.Paused), Session("Deezer", "Titre A", "Artiste A", MediaPlayback.Paused));
        PollFor(2);
        _tracker.SelectedSessionId.ShouldBe("Deezer");

        // Deezer ferme sa session après la pause : Chrome a joué avant, il peut reprendre la main (dernier actif).
        _source.Set(Session("Chrome", "Titre C", "Artiste C", MediaPlayback.Paused));
        PollFor(2);

        _tracker.SelectedSessionId.ShouldBe("Chrome");
    }

    [Theory]
    [InlineData("Chrome", "Chrome")]
    [InlineData("Spotify.exe", "Spotify")]
    [InlineData("Deezer.62021768415AF_q7m17pa7q8kj0", "Deezer")]
    [InlineData("Microsoft.ZuneMusic_8wekyb3d8bbwe!Microsoft.ZuneMusic", "Microsoft.ZuneMusic_8wekyb3d8bbwe")]
    public void FriendlyName_ShortensTheApplicationIdentifier(string appId, string expected) =>
        MediaApps.FriendlyName(appId).ShouldBe(expected);

    [Fact]
    [Trait("Exigence", "EVT-041")]
    public void Pause_RaisesPlaybackChanged_AndResumeToo()
    {
        _source.Set(Session("Deezer", "Titre A", "Artiste A", MediaPlayback.Playing));
        PollFor(2);
        _source.Set(Session("Deezer", "Titre A", "Artiste A", MediaPlayback.Paused));
        PollFor(1);
        _source.Set(Session("Deezer", "Titre A", "Artiste A", MediaPlayback.Playing));
        PollFor(1);

        _playback.ShouldBe([true, false, true]);
        _changes.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "MUS-006")]
    public void SessionDisappears_RaisesTrackChangedWithoutTrack()
    {
        _source.Set(Session("Deezer", "Titre A", "Artiste A", MediaPlayback.Playing));
        PollFor(2);
        _source.Set();
        PollFor(1);

        _changes.Count.ShouldBe(2);
        _changes[1].Track.ShouldBeNull();
        _changes[1].Previous!.Title.ShouldBe("Titre A");
        _tracker.Current.ShouldBe(NowPlayingState.None);
        _playback.ShouldBe([true, false]);
    }

    [Fact]
    [Trait("Exigence", "MUS-003")]
    public void Position_IsInterpolatedWhilePlayingAndFrozenInPause()
    {
        var session = Session("Deezer", "Titre A", "Artiste A", MediaPlayback.Playing) with
        {
            Position = TimeSpan.FromSeconds(60),
            PositionUpdatedUtc = Origin,
            Duration = TimeSpan.FromSeconds(200),
        };
        _source.Set(session);
        PollFor(2);

        // 2 s de lecture depuis la mise à jour du lecteur.
        _tracker.Current.Position.ShouldBe(TimeSpan.FromSeconds(62));

        _time.Advance(10);
        _tracker.Current.Position.ShouldBe(TimeSpan.FromSeconds(72));

        _source.Set(session with { Playback = MediaPlayback.Paused });
        _tracker.Poll();
        _time.Advance(30);
        _tracker.Current.Position.ShouldBe(TimeSpan.FromSeconds(60));
    }

    [Fact]
    [Trait("Exigence", "MUS-003")]
    public void Position_StaysWithinTheDurationAndHonoursTheRate()
    {
        var session = Session("VLC", "Titre V", "Artiste V", MediaPlayback.Playing) with
        {
            Position = TimeSpan.FromSeconds(190),
            PositionUpdatedUtc = Origin,
            Duration = TimeSpan.FromSeconds(200),
            Rate = 2.0,
        };
        _source.Set(session);
        PollFor(2);

        // 2 s à la vitesse 2 : 194 s ; puis la fin du morceau ne dépasse pas la durée.
        _tracker.Current.Position.ShouldBe(TimeSpan.FromSeconds(194));
        _time.Advance(60);
        _tracker.Current.Position.ShouldBe(TimeSpan.FromSeconds(200));
    }

    [Fact]
    [Trait("Exigence", "MUS-003")]
    public void PlayerWithoutPosition_GivesNoPosition()
    {
        _source.Set(Session("Chrome", "Titre A", "Artiste A", MediaPlayback.Playing));
        PollFor(2);

        _tracker.Current.Track.ShouldNotBeNull();
        _tracker.Current.Position.ShouldBeNull();
    }

    [Fact]
    public void FailingSubscriber_DoesNotStopTheOthers()
    {
        _tracker.TrackChanged += (_, _) => throw new InvalidOperationException("abonné défaillant");
        _tracker.TrackChanged += (_, c) => _changes.Add(c);
        _source.Set(Session("Deezer", "Titre A", "Artiste A", MediaPlayback.Playing));

        PollFor(2);

        _tracker.Current.Track.ShouldNotBeNull();
    }

    private static MediaSessionInfo Session(string id, string title, string artist, MediaPlayback playback) =>
        new(id, id, title, artist, string.Empty, playback, null, null, TimeSpan.Zero);

    private void PollFor(double seconds)
    {
        _tracker.Poll();
        _time.Advance(seconds);
        _tracker.Poll();
    }

    private sealed class FakeSource : IMediaSessionSource
    {
        private IReadOnlyList<MediaSessionInfo> _sessions = [];

        public event EventHandler? Changed;

        public void Set(params MediaSessionInfo[] sessions)
        {
            _sessions = sessions;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public IReadOnlyList<MediaSessionInfo> Sessions() => _sessions;

        public void Dispose()
        {
        }
    }

    private sealed class FakeTime(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public void Advance(double seconds) => _now += TimeSpan.FromSeconds(seconds);

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
