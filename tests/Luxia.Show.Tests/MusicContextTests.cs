using Luxia.Messaging.Commands;
using Luxia.Show.Model;
using Luxia.Show.Runtime;

namespace Luxia.Show.Tests;

/// <summary>
/// Style et changement de morceau venus de la lecture en cours (P9) : CMD-062 (style imposé), CMD-063 (contexte musical), condition
/// « style » sur les noms composés des familles, « au morceau suivant » (Q49 : le titre réel remplace l'écoute, qui reste le repli).
/// </summary>
public sealed class MusicContextTests
{
    [Theory]
    [InlineData("Électro / Dance", "Électro", true)]
    [InlineData("Électro / Dance", "electro", true)]
    [InlineData("Électro / Dance", "Dance", true)]
    [InlineData("Électro / Dance", "ÉLECTRO / DANCE", true)]
    [InlineData("Électro / Dance", "House", false)]
    [InlineData("House / Disco moderne", "House", true)]
    [InlineData("Disco / Funk / Soul", "Funk", true)]
    [InlineData("Rock", "rock", true)]
    [InlineData("Rock", "Rock'n'roll", false)]
    [InlineData("Rock", "", false)]
    [InlineData("Inconnu", "Inconnu", true)]
    [Trait("Exigence", "SHOW-022")]
    public void StyleCondition_MatchesTheFamilyNameOrOneOfItsParts(string current, string wanted, bool expected) =>
        ShowRun.StyleMatches(current, wanted).ShouldBe(expected);

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void DetectedStyle_DrivesAShowTransition()
    {
        var h = new SequencerHarness();
        var show = ShowWith(ConditionKind.Style, styles: ["Rock"]);
        Start(show, h);
        h.RunTo(0.5);
        h.ActiveSteps(show).ShouldBe(["0"]);

        h.Send(new SetMusicContextCommand(CommandOrigin.Tool, "Électro / Dance", MediaActive: true));
        h.Tick();
        h.Tick();
        h.ActiveSteps(show).ShouldBe(["0"], "un autre style ne déclenche rien");

        h.Send(new SetMusicContextCommand(CommandOrigin.Tool, "Rock", MediaActive: true));
        h.Tick();
        h.Tick();
        h.ActiveSteps(show).ShouldBe(["1"]);
    }

    [Fact]
    [Trait("Exigence", "MUS-026")]
    public void ForcedStyle_BeatsTheDetected_AndTheSimulatedBeatsBoth()
    {
        var h = new SequencerHarness();
        var show = ShowWith(ConditionKind.Style, styles: ["Latino"]);
        Start(show, h);
        h.Send(new SetMusicContextCommand(CommandOrigin.Tool, "Rock", MediaActive: true));
        h.Tick();
        h.ActiveSteps(show).ShouldBe(["0"]);

        h.Send(new ForceStyleCommand(CommandOrigin.User, "Latino"));
        h.Tick();
        h.Tick();
        h.ActiveSteps(show).ShouldBe(["1"], "le style imposé l'emporte sur le style détecté");
    }

    [Fact]
    [Trait("Exigence", "MUS-026")]
    public void ForcedStyle_IsReleasedBackToTheDetection()
    {
        var h = new SequencerHarness();
        var show = ShowWith(ConditionKind.Style, styles: ["Rock"]);
        Start(show, h);
        h.Send(new SetMusicContextCommand(CommandOrigin.Tool, "Rock", MediaActive: true));
        h.Send(new ForceStyleCommand(CommandOrigin.User, "Latino"));
        h.Tick();
        h.Tick();
        h.ActiveSteps(show).ShouldBe(["0"], "Rock est détecté mais Latino est imposé");

        h.Send(new ForceStyleCommand(CommandOrigin.User, null));
        h.Tick();
        h.Tick();
        h.ActiveSteps(show).ShouldBe(["1"], "retour à la détection : Rock");
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void RealTrackChange_RaisesSongChanged_OnTheNextTick()
    {
        var h = new SequencerHarness();
        var show = ShowWith(ConditionKind.SongChanged);
        Start(show, h);
        h.RunTo(0.5);
        h.ActiveSteps(show).ShouldBe(["0"]);

        h.Send(new SetMusicContextCommand(CommandOrigin.Tool, "Rock", TrackChanged: true, MediaActive: true));
        h.Tick();
        h.Tick();

        h.ActiveSteps(show).ShouldBe(["1"]);
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void WhileATrackIsFollowed_TheAudioHeuristicDoesNotRaiseSongChanged()
    {
        var h = new SequencerHarness();
        var feed = new ResumedFeed();
        h.Engine.SetAudioFeed(feed);
        var show = ShowWith(ConditionKind.SongChanged);
        Start(show, h);

        // Reprise du son après un silence : avant P9 elle valait « morceau changé » (D38) ; un titre suivi, seul son changement réel compte.
        h.Send(new SetMusicContextCommand(CommandOrigin.Tool, "Rock", MediaActive: true));
        h.Tick();
        feed.Resumed = true;
        h.Tick();
        h.Tick();
        h.Tick();

        h.ActiveSteps(show).ShouldBe(["0"], "un titre est suivi : seul son changement réel compte");
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void WithoutAFollowedTrack_TheAudioHeuristicIsStillTheFallback()
    {
        var h = new SequencerHarness();
        var feed = new ResumedFeed();
        h.Engine.SetAudioFeed(feed);
        var show = ShowWith(ConditionKind.SongChanged);
        Start(show, h);
        h.Send(new SetMusicContextCommand(CommandOrigin.Tool, null, MediaActive: false));

        feed.Resumed = true;
        h.Tick();
        h.Tick();

        h.ActiveSteps(show).ShouldBe(["1"], "pas de lecture suivie : repli sur l'écoute (D38)");
    }

    [Fact]
    [Trait("Exigence", "MUS-006")]
    public void NoTrack_GivesNoStyle()
    {
        var h = new SequencerHarness();
        var show = ShowWith(ConditionKind.Style, styles: ["Rock"]);
        Start(show, h);
        h.Send(new SetMusicContextCommand(CommandOrigin.Tool, "Rock", MediaActive: true));
        h.Tick();
        h.Tick();
        h.ActiveSteps(show).ShouldBe(["1"]);

        // Retour au point de départ pour vérifier qu'effacer le contexte retire le style.
        var h2 = new SequencerHarness();
        var show2 = ShowWith(ConditionKind.Style, styles: ["Rock"]);
        Start(show2, h2);
        h2.Send(new SetMusicContextCommand(CommandOrigin.Tool, "Rock", MediaActive: true));
        h2.Send(new SetMusicContextCommand(CommandOrigin.Tool, null));
        h2.Tick();
        h2.Tick();
        h2.ActiveSteps(show2).ShouldBe(["0"]);
    }

    /// <summary>Écoute simulée : lève « reprise du son » à la demande, comme après un silence (D38).</summary>
    private sealed class ResumedFeed : Luxia.Engine.Timing.IAudioFeed
    {
        public bool Resumed { get; set; }

        public Luxia.Engine.Timing.AudioReading Read()
        {
            var cues = Resumed ? Luxia.Engine.Timing.MusicCues.Resumed : Luxia.Engine.Timing.MusicCues.None;
            Resumed = false;
            return new Luxia.Engine.Timing.AudioReading(true, 120, 1, true, 0, 1, 0, 0, Cues: cues);
        }
    }

    private static ShowDefinition ShowWith(ConditionKind kind, IReadOnlyList<string>? styles = null) => new()
    {
        Name = "Contexte musical",
        Steps = [new ShowStep { Id = "0", Name = "0", Initial = true }, new ShowStep { Id = "1", Name = "1" }],
        Transitions = [new ShowTransition { From = ["0"], To = ["1"], Condition = new ShowCondition { Kind = kind, Styles = styles ?? [] } }],
    };

    private static void Start(ShowDefinition show, SequencerHarness h)
    {
        h.Shows.Add(show);
        h.Load();
        h.Send(new LaunchShowCommand(CommandOrigin.Tool, show.Id));
        h.Tick();
    }
}
