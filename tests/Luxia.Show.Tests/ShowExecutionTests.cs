using Luxia.Engine.Model;
using Luxia.Messaging.Commands;
using Luxia.Messaging.Events;
using Luxia.Show.Model;

namespace Luxia.Show.Tests;

/// <summary>T-SHOW-02 et T-SHOW-03 : règles R1 à R6 et réceptivités, en temps virtuel avec événements simulés (SHOW-023).</summary>
public sealed class ShowExecutionTests
{
    private static ShowStep Step(string id, bool initial = false, params ShowAction[] actions) => new() { Id = id, Name = id.ToUpperInvariant(), Initial = initial, Actions = actions };

    private static ShowAction Play(Engine.Model.EngineScene scene) => new() { Kind = ShowActionKind.Play, SceneId = scene.Id };

    private static ShowTransition T(string from, string to, ShowCondition condition, ShowQuantize quantize = ShowQuantize.None, double weight = 1) =>
        new() { From = [from], To = [to], Condition = condition, Quantize = quantize, Weight = weight };

    private static ShowCondition When(ConditionKind kind) => new() { Kind = kind };

    private static ShowCondition After(double value, DurationUnit unit) => new() { Kind = ConditionKind.After, Duration = new Duration(value, unit) };

    private static SequencerHarness Start(ShowDefinition show, SequencerHarness? harness = null)
    {
        var h = harness ?? new SequencerHarness();
        h.Shows.Add(show);
        h.Load(120);
        h.Send(new LaunchShowCommand(CommandOrigin.Tool, show.Id));
        h.Tick();
        return h;
    }

    [Fact]
    [Trait("Exigence", "SHOW-023")]
    [Trait("Exigence", "SHOW-021")]
    public void Launch_ActivatesTheInitialStepAndPlaysItsScenes()
    {
        var h = new SequencerHarness();
        var blue = h.Scene("Bleu", h.Colors);
        var sweep = h.Scene("Balayage", h.Movements);
        var show = new ShowDefinition { Name = "Intro", Steps = [Step("0", true, Play(blue), Play(sweep))] };
        Start(show, h);

        h.ActiveSteps(show).ShouldBe(["0"]);
        h.Playing(blue).ShouldBeTrue();
        h.Playing(sweep).ShouldBeTrue();
        h.Bus.Of<ShowStepActivated>().ShouldHaveSingleItem().Reason.ShouldBe("lancement");
    }

    [Fact]
    [Trait("Exigence", "SHOW-023")]
    [Trait("Exigence", "SHOW-022")]
    public void Drop_QuantizedOnTheBar_FiresAtTheNextBar_AndR5KeepsARepeatedScene()
    {
        var h = new SequencerHarness();
        var blue = h.Scene("Bleu", h.Colors);
        var white = h.Scene("Blanc", h.Effects);
        var show = new ShowDefinition
        {
            Name = "Couplet / Refrain",
            Steps = [Step("1", true, Play(blue)), Step("3", false, Play(blue), Play(white))],
            Transitions = [T("1", "3", When(ConditionKind.Drop), ShowQuantize.Bar)],
        };
        Start(show, h);
        h.RunTo(0.5);

        h.Send(new SimulateMusicCommand(CommandOrigin.Tool, SimulatedCue.Drop));
        h.RunTo(1.0);
        h.ActiveSteps(show).ShouldBe(["1"], "armée : attend la mesure suivante");
        var transition = h.Sequencer.State.MainShow!.Transitions.ShouldHaveSingleItem();
        transition.Armed.ShouldBeTrue();
        transition.BeatsLeft.ShouldBe(2, 0.25, "état publié dix fois par seconde");

        h.RunTo(2.05);
        h.ActiveSteps(show).ShouldBe(["3"]);
        h.Bus.Of<ShowStepActivated>()[^1].At.TotalSeconds.ShouldBe(2.0, 0.026);
        h.Starts(blue).Count.ShouldBe(1, "R5 : la scène rejouée par l'étape suivante n'est pas interrompue");
        h.Playing(white).ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "SHOW-023")]
    public void LeavingAStep_StopsItsContinuousScenes()
    {
        var h = new SequencerHarness();
        var blue = h.Scene("Bleu", h.Colors);
        var red = h.Scene("Rouge", h.Colors);
        var fog = h.Scene("Brouillard UV", h.Atmosphere);
        var show = new ShowDefinition
        {
            Name = "Deux étapes",
            Steps = [Step("a", true, Play(blue), Play(fog)), Step("b", false, Play(red))],
            Transitions = [T("a", "b", After(2, DurationUnit.Beats))],
        };
        Start(show, h);

        h.RunTo(1.1);
        h.ActiveSteps(show).ShouldBe(["b"]);
        h.Playing(red).ShouldBeTrue();
        h.Playing(blue).ShouldBeFalse();
        h.Playing(fog).ShouldBeFalse("couche non exclusive : arrêtée explicitement");
    }

    [Fact]
    [Trait("Exigence", "SHOW-023")]
    public void OrDivergence_FirstTrueTransitionWins_ByOrder()
    {
        var h = new SequencerHarness();
        var show = new ShowDefinition
        {
            Name = "Priorité",
            Steps = [Step("0", true), Step("1"), Step("2")],
            Transitions = [T("0", "1", When(ConditionKind.Drop)), T("0", "2", When(ConditionKind.Drop))],
        };
        Start(show, h);
        h.Send(new SimulateMusicCommand(CommandOrigin.Tool, SimulatedCue.Drop));
        h.Tick();

        h.ActiveSteps(show).ShouldBe(["1"]);
    }

    [Fact]
    [Trait("Exigence", "SHOW-023")]
    [Trait("Exigence", "SHOW-020")]
    public void AndDivergenceAndConvergence_RunBranchesInParallel_ThenJoin()
    {
        var h = new SequencerHarness();
        var colors = h.Scene("Couleurs lentes", h.Colors);
        var moves = h.Scene("Mouvements rapides", h.Movements);
        var show = new ShowDefinition
        {
            Name = "ET",
            Steps = [Step("0", true), Step("a", false, Play(colors)), Step("a2"), Step("b", false, Play(moves)), Step("b2"), Step("fin")],
            Transitions =
            [
                new() { From = ["0"], To = ["a", "b"], Condition = When(ConditionKind.Always) },
                T("a", "a2", After(1, DurationUnit.Bars)),
                T("b", "b2", After(2, DurationUnit.Bars)),
                new() { From = ["a2", "b2"], To = ["fin"], Condition = When(ConditionKind.Always) },
            ],
        };
        Start(show, h);
        h.Tick();
        h.ActiveSteps(show).Order().ToList().ShouldBe(["a", "b"]);
        h.Playing(colors).ShouldBeTrue();
        h.Playing(moves).ShouldBeTrue();

        h.RunTo(2.2);
        h.ActiveSteps(show).Order().ToList().ShouldBe(["a2", "b"], "les branches avancent chacune à son rythme");
        h.RunTo(4.2);
        h.ActiveSteps(show).ShouldBe(["fin"], "convergence : les deux branches sont arrivées");
    }

    [Fact]
    [Trait("Exigence", "SHOW-020")]
    [Trait("Exigence", "SHOW-023")]
    public void MacroStep_WaitsForItsSubShowToEnd()
    {
        var h = new SequencerHarness();
        var chorus = h.Scene("Refrain", h.Colors);
        var block = new ShowDefinition
        {
            Name = "Bloc refrain",
            Steps = [Step("r0", true, Play(chorus)), Step("r1")],
            Transitions = [T("r0", "r1", After(1, DurationUnit.Bars))],
        };
        var show = new ShowDefinition
        {
            Name = "Avec macro",
            Steps = [new ShowStep { Id = "m", Initial = true, MacroShowId = block.Id }, Step("x")],
            Transitions = [T("m", "x", When(ConditionKind.Always))],
        };
        h.Shows.Add(block);
        Start(show, h);

        h.Playing(chorus).ShouldBeTrue("le sous-show joue");
        h.Sequencer.State.MainShow!.ActiveSteps.ShouldHaveSingleItem().Macro!.ActiveSteps.ShouldHaveSingleItem().Id.ShouldBe("r0");
        h.RunTo(1.5);
        h.ActiveSteps(show).ShouldBe(["m"]);
        h.RunTo(2.2);
        h.ActiveSteps(show).ShouldBe(["x"], "sous-show arrivé à sa fin : la transition sortante est validée");
        h.Playing(chorus).ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "SHOW-023")]
    public void EndOfShow_HoldsStopsOrRestarts()
    {
        var h0 = new SequencerHarness();
        var final = h0.Scene("Final", h0.Colors);
        var hold = new ShowDefinition { Name = "Tient", Steps = [Step("0", true), Step("1", false, Play(final))], Transitions = [T("0", "1", After(1, DurationUnit.Beats))] };
        Start(hold, h0);
        h0.RunTo(2);
        h0.Playing(final).ShouldBeTrue("par défaut, la dernière étape reste allumée");
        h0.Sequencer.State.MainShow!.Ended.ShouldBeTrue();

        var h = new SequencerHarness();
        var blue = h.Scene("Bleu", h.Colors);
        var stop = new ShowDefinition { Name = "Une fois", AtEnd = ShowEnd.Stop, Steps = [Step("0", true, Play(blue)), Step("1")], Transitions = [T("0", "1", After(1, DurationUnit.Beats))] };
        Start(stop, h);
        h.RunTo(0.7);
        h.Sequencer.State.Shows.ShouldBeEmpty("R6 : toutes les étapes actives sont des fins");
        h.Playing(blue).ShouldBeFalse();
        h.Bus.Of<ShowStateChanged>()[^1].Running.ShouldBeFalse();

        var h2 = new SequencerHarness();
        var again = new ShowDefinition { Name = "En boucle", AtEnd = ShowEnd.Restart, Steps = [Step("0", true), Step("1")], Transitions = [T("0", "1", After(1, DurationUnit.Beats))] };
        Start(again, h2);
        h2.RunTo(1.4);
        h2.Steps.ShouldBe(["0", "1", "0", "1", "0"]);
    }

    [Fact]
    [Trait("Exigence", "SHOW-025")]
    [Trait("Exigence", "SHOW-031")]
    public void OnlyOneMainShow_SecondaryShowsRunAlongside()
    {
        var h = new SequencerHarness();
        var blue = h.Scene("Bleu", h.Colors);
        var red = h.Scene("Rouge", h.Colors);
        var uv = h.Scene("UV", h.Atmosphere);
        var first = new ShowDefinition { Name = "Premier", Steps = [Step("0", true, Play(blue))] };
        var second = new ShowDefinition { Name = "Second", Steps = [Step("0", true, Play(red))] };
        var ambiance = new ShowDefinition { Name = "Ambiance", Secondary = true, Steps = [Step("0", true, Play(uv))] };
        h.Shows.AddRange([first, second, ambiance]);
        h.Load(120);
        h.Send(new LaunchShowCommand(CommandOrigin.Tool, first.Id));
        h.Send(new LaunchShowCommand(CommandOrigin.Tool, ambiance.Id));
        h.Tick();
        h.Tick();
        h.Send(new LaunchShowCommand(CommandOrigin.Tool, second.Id));
        h.Tick();
        h.Tick();

        h.Sequencer.State.Shows.Select(s => s.Name).ShouldBe(["Second", "Ambiance"]);
        h.Playing(red).ShouldBeTrue();
        h.Playing(uv).ShouldBeTrue();
        h.Playing(blue).ShouldBeFalse();

        h.Send(new LaunchShowCommand(CommandOrigin.Tool, second.Id, StopIfPlaying: true));
        h.Send(new StopShowCommand(CommandOrigin.Tool));
        h.Tick();
        h.Tick();
        h.Sequencer.State.Shows.ShouldBeEmpty();
        h.Playing(uv).ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "SHOW-021")]
    [Trait("Exigence", "SHOW-029")]
    public void Variables_CountChoruses_ThenTheFinalVariant()
    {
        var h = new SequencerHarness();
        var show = new ShowDefinition
        {
            Name = "Trois refrains",
            Variables = [new ShowVariable("refrains")],
            Steps =
            [
                Step("couplet", true),
                Step("refrain", false, new ShowAction { Kind = ShowActionKind.Variable, Variable = "refrains", Value = 1 }),
                Step("final"),
            ],
            Transitions =
            [
                T("couplet", "refrain", When(ConditionKind.Drop)),
                T("refrain", "final", new ShowCondition { Kind = ConditionKind.Variable, Variable = "refrains", Value = 3 }),
                T("refrain", "couplet", When(ConditionKind.Break)),
            ],
        };
        Start(show, h);
        for (var i = 0; i < 3; i++)
        {
            h.Send(new SimulateMusicCommand(CommandOrigin.Tool, SimulatedCue.Drop));
            h.Tick();
            h.Tick();
            if (i < 2)
            {
                h.ActiveSteps(show).ShouldBe(["refrain"]);
                h.Send(new SimulateMusicCommand(CommandOrigin.Tool, SimulatedCue.Break));
                h.Tick();
                h.Tick();
            }
        }

        h.ActiveSteps(show).ShouldBe(["final"]);
        h.Steps.ShouldBe(["couplet", "refrain", "couplet", "refrain", "couplet", "refrain", "final"]);
    }

    [Fact]
    [Trait("Exigence", "SHOW-024")]
    public void ShowWithAnImmediateLoop_IsRefusedAtLaunch()
    {
        var h = new SequencerHarness();
        var show = new ShowDefinition
        {
            Name = "Piège",
            Steps = [Step("a", true), Step("b")],
            Transitions = [T("a", "b", When(ConditionKind.Always)), T("b", "a", When(ConditionKind.Always))],
        };
        Start(show, h);

        h.Engine.CommandLog().Single(e => e.Command is LaunchShowCommand).Rejection.ShouldBe("le show « Piège » ne peut pas jouer : boucle sans condition (a → b → a) : le show tournerait sans fin ; ajoutez une condition ou une quantification");
        h.Sequencer.State.Shows.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "SHOW-021")]
    public void PulseActions_FlashAndShortBlackout_EndOnTheirOwn()
    {
        var h = new SequencerHarness();
        var white = h.Scene("Blanc", h.Effects);
        var show = new ShowDefinition
        {
            Name = "Impulsions",
            Steps = [Step("0", true, new ShowAction { Kind = ShowActionKind.Flash, SceneId = white.Id, Seconds = 0.5 }, new ShowAction { Kind = ShowActionKind.Blackout, Seconds = 0.25 })],
        };
        Start(show, h);
        h.Engine.Snapshot.Blackout.ShouldBeTrue();
        h.Engine.Snapshot.Playbacks.ShouldContain(p => p.SceneId == white.Id && p.Flash);
        h.Run(0.3);
        h.Engine.Snapshot.Blackout.ShouldBeFalse();
        h.Run(0.3);
        h.Engine.Snapshot.Playbacks.ShouldNotContain(p => p.SceneId == white.Id && p.Flash);
    }

    [Fact]
    [Trait("Exigence", "SHOW-021")]
    public void PlaySequence_RunsWhileTheStepIsActive_AndSequenceEndedMovesOn()
    {
        var h = new SequencerHarness();
        var a = h.Scene("A", h.Colors);
        var sequence = new Sequence { Name = "Groove", Bars = 1, Quantize = ShowQuantize.None, Tracks = [new SequenceTrack { LayerId = h.Colors.Id, Blocks = [new SequenceBlock { Start = 0, Length = 1, SceneId = a.Id }] }] };
        h.Sequences.Add(sequence);
        var show = new ShowDefinition
        {
            Name = "Séquence puis fin",
            Steps = [Step("0", true, new ShowAction { Kind = ShowActionKind.PlaySequence, SequenceId = sequence.Id }), Step("1")],
            Transitions = [T("0", "1", new ShowCondition { Kind = ConditionKind.SequenceEnded, SequenceId = sequence.Id })],
        };
        Start(show, h);
        h.Playing(a).ShouldBeTrue();
        h.Sequencer.State.Sequences.ShouldHaveSingleItem().OwnerShowId.ShouldBe(show.Id);
        h.RunTo(2.1);
        h.Steps.ShouldBe(["0", "1"]);
        h.Playing(a).ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "SHOW-022")]
    public void Conditions_TimeEnergyStyleTempoSongAndLogic()
    {
        var h = new SequencerHarness();
        var show = new ShowDefinition
        {
            Name = "Conditions",
            Steps = [Step("0", true), Step("1"), Step("2"), Step("3"), Step("4"), Step("5"), Step("6")],
            Transitions =
            [
                T("0", "1", After(1, DurationUnit.Seconds)),
                T("1", "2", new ShowCondition { Kind = ConditionKind.EnergyLevel, Min = 2 }),
                T("2", "3", new ShowCondition { Kind = ConditionKind.EnergyBelow, Value = 0.4 }),
                T("3", "4", new ShowCondition { Kind = ConditionKind.Style, Styles = ["Électro"] }),
                T("4", "5", new ShowCondition { Kind = ConditionKind.All, Conditions = [new ShowCondition { Kind = ConditionKind.Tempo, Min = 125, Max = 130 }, new ShowCondition { Kind = ConditionKind.Not, Conditions = [When(ConditionKind.Silence)] }] }),
                T("5", "6", When(ConditionKind.SongChanged)),
            ],
        };
        Start(show, h);
        h.RunTo(0.9);
        h.ActiveSteps(show).ShouldBe(["0"]);
        h.RunTo(1.1);
        h.ActiveSteps(show).ShouldBe(["1"], "après 1 s");

        h.Send(new SimulateMusicCommand(CommandOrigin.Tool, Energy: 0.6));
        h.Tick();
        h.Tick();
        h.ActiveSteps(show).ShouldBe(["2"], "niveau Énergique");

        h.Send(new SimulateMusicCommand(CommandOrigin.Tool, Energy: 0.2));
        h.Tick();
        h.Tick();
        h.ActiveSteps(show).ShouldBe(["3"], "l'énergie passe sous 40 %");

        h.Send(new SimulateMusicCommand(CommandOrigin.Tool, Style: "électro"));
        h.Tick();
        h.Tick();
        h.ActiveSteps(show).ShouldBe(["4"]);

        h.Tick();
        h.ActiveSteps(show).ShouldBe(["4"], "120 BPM : hors de 125-130");
        h.Send(new SetTempoSourceCommand(CommandOrigin.Tool, TempoSourceKind.Fixed, 128));
        h.Tick();
        h.Tick();
        h.ActiveSteps(show).ShouldBe(["5"]);

        h.Send(new SimulateMusicCommand(CommandOrigin.Tool, SimulatedCue.Resumed));
        h.Tick();
        h.Tick();
        h.ActiveSteps(show).ShouldBe(["6"], "reprise après un silence = morceau changé (D38)");
    }

    [Fact]
    [Trait("Exigence", "SHOW-022")]
    public void ManualTransition_IsOnlyForced_AtItsQuantization()
    {
        var h = new SequencerHarness();
        var show = new ShowDefinition
        {
            Name = "Manuel",
            Steps = [Step("0", true), Step("1"), Step("2")],
            Transitions = [T("0", "1", When(ConditionKind.Manual), ShowQuantize.Bar), T("1", "2", When(ConditionKind.Manual))],
        };
        Start(show, h);
        h.RunTo(5);
        h.ActiveSteps(show).ShouldBe(["0"]);

        h.Send(new ForceTransitionCommand(CommandOrigin.Tool, show.Id, 0));
        h.RunTo(5.5);
        h.ActiveSteps(show).ShouldBe(["0"], "attend la mesure");
        h.RunTo(6.1);
        h.ActiveSteps(show).ShouldBe(["1"]);
        h.Bus.Of<ShowStepActivated>()[^1].Reason.ShouldBe("forcée");

        h.Send(new ForceTransitionCommand(CommandOrigin.Tool, show.Id, 0));
        h.Tick();
        h.Engine.CommandLog()[^1].Rejection.ShouldBe("ses étapes amont ne sont pas toutes actives");
        h.Send(new ForceTransitionCommand(CommandOrigin.Tool, show.Id, 1, Immediate: true));
        h.Tick();
        h.Tick();
        h.ActiveSteps(show).ShouldBe(["2"]);
    }

    [Fact]
    [Trait("Exigence", "SHOW-022")]
    public void RandomCondition_DrawsOncePerBar()
    {
        var h = new SequencerHarness(seed: 3);
        var show = new ShowDefinition
        {
            Name = "Une chance sur deux",
            Steps = [Step("0", true), Step("1")],
            Transitions = [T("0", "1", new ShowCondition { Kind = ConditionKind.Random, Value = 0.5, Every = ShowQuantize.Bar })],
        };
        Start(show, h);
        h.RunTo(1.9);
        h.ActiveSteps(show).ShouldBe(["0"], "pas de tirage avant la première frontière");
        h.RunTo(60);
        h.ActiveSteps(show).ShouldBe(["1"], "en 30 mesures, une chance sur deux finit par sortir");
        h.Bus.Of<ShowStepActivated>()[^1].At.TotalSeconds.ShouldSatisfyAllConditions(t => (t % 2.0).ShouldBeLessThan(0.03));
    }
}
