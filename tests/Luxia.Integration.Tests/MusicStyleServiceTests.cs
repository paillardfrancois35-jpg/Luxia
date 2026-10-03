using Luxia.Hosting;
using Luxia.Media;
using Luxia.Messaging.Events;
using Luxia.Persistence;
using Luxia.Music.Identification;
using Microsoft.Extensions.Logging.Abstractions;

namespace Luxia.Integration.Tests;

/// <summary>Style du morceau en cours (P9) : de la lecture en cours de Windows (source simulée) jusqu'au bus et au moteur.</summary>
public sealed class MusicStyleServiceTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "luxia-music-service", Guid.NewGuid().ToString("N"));
    private readonly FakeMedia _media = new();
    private LuxiaRuntime _runtime = null!;

    public ValueTask InitializeAsync()
    {
        var paths = new DataPaths(Path.Combine(_root, "Documents"), Path.Combine(_root, "AppData"));
        _runtime = new LuxiaRuntime(paths, NullLoggerFactory.Instance, mediaSessions: _media);
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync() => await _runtime.DisposeAsync();

    [Fact]
    [Trait("Exigence", "MUS-021")]
    [Trait("Exigence", "EVT-042")]
    public async Task NewTrack_IsIdentified_AndPublishedOnTheBus()
    {
        var events = new List<StyleDetected>();
        using var subscription = _runtime.Bus.Subscribe<StyleDetected>(e => { lock (events) { events.Add(e); } });

        await PlayAsync("Radio Ga Ga", "Queen");

        _runtime.Music.State.StyleName.ShouldBe("Rock");
        await WaitUntilAsync(() => { lock (events) { return events.Count > 0; } });
        StyleDetected published;
        lock (events)
        {
            published = events[0];
        }

        published.FamilyName.ShouldBe("Rock");
        published.Confidence.ShouldBeGreaterThan(0.7);
        published.Method.ShouldBe("artiste");
        published.Forced.ShouldBeFalse();
        published.Title.ShouldBe("Radio Ga Ga");
    }

    [Fact]
    [Trait("Exigence", "MUS-026")]
    public async Task ForcedStyle_IsPublished_AndEndsWithTheTrack()
    {
        await PlayAsync("Radio Ga Ga", "Queen");

        _runtime.Music.Force("Latino").ShouldBeTrue();

        _runtime.Music.State.StyleName.ShouldBe("Latino");
        _runtime.Music.State.Forced.ShouldBeTrue();

        await PlayAsync("Dancing Queen", "ABBA");

        _runtime.Music.State.Forced.ShouldBeFalse();
        _runtime.Music.State.StyleName.ShouldBe("Disco / Funk / Soul");
    }

    [Fact]
    [Trait("Exigence", "MUS-024")]
    public async Task Correction_IsImmediate_AndRemembered()
    {
        await PlayAsync("Radio Ga Ga", "Queen");

        _runtime.Music.Correct(CorrectionScope.Artist, "Festif").ShouldBeNull();

        _runtime.Music.State.StyleName.ShouldBe("Festif / Tubes de soirée");
        _runtime.Music.State.Effective.Method.ShouldBe(IdentificationMethod.Correction);
        _runtime.Music.Base.ToCorrectionSet().Corrections.ShouldHaveSingleItem();
    }

    [Fact]
    [Trait("Exigence", "MUS-006")]
    public async Task NoMoreSession_GivesNoStyle()
    {
        await PlayAsync("Radio Ga Ga", "Queen");

        _media.Set();
        _runtime.NowPlaying!.Poll();

        _runtime.Music.State.HasTrack.ShouldBeFalse();
        _runtime.Music.State.StyleName.ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "MUS-007")]
    public void ManualTrack_IsIdentifiedLikeAPlayerTrack()
    {
        _runtime.Music.SetManualTrack("Dancing Queen", "ABBA");

        _runtime.Music.State.StyleName.ShouldBe("Disco / Funk / Soul");
    }

    private async Task PlayAsync(string title, string artist)
    {
        _media.Set(new MediaSessionInfo("Deezer", "Deezer", title, artist, string.Empty, MediaPlayback.Playing, null, null, TimeSpan.Zero));
        _runtime.NowPlaying!.Poll();

        // Le nouveau titre doit rester présent 1 s avant d'être publié (MUS-002), en temps réel.
        await Task.Delay(NowPlayingTracker.Stabilization + TimeSpan.FromMilliseconds(150));
        _runtime.NowPlaying.Poll();
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
        {
            await Task.Delay(50);
        }

        condition().ShouldBeTrue();
    }

    private sealed class FakeMedia : IMediaSessionSource
    {
        private IReadOnlyList<MediaSessionInfo> _sessions = [];

        public event EventHandler? Changed
        {
            add { }
            remove { }
        }

        public void Set(params MediaSessionInfo[] sessions) => _sessions = sessions;

        public IReadOnlyList<MediaSessionInfo> Sessions() => _sessions;

        public void Dispose()
        {
        }
    }
}
