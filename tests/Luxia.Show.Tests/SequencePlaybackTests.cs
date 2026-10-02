using Luxia.Engine.Model;
using Luxia.Messaging.Commands;
using Luxia.Show.Model;

namespace Luxia.Show.Tests;

/// <summary>T-SHOW-01 : séquences, commandes émises aux bons temps à 90, 120 et 140 BPM ; quantification ; boucle ; actions.</summary>
public sealed class SequencePlaybackTests
{
    private static SequenceTrack Track(Guid? layer, params SequenceBlock[] blocks) => new() { LayerId = layer, Blocks = blocks };

    private static SequenceBlock Block(double start, double length, Guid scene, BlockEnd end = BlockEnd.Stop) => new() { Start = start, Length = length, SceneId = scene, End = end };

    [Theory]
    [InlineData(90)]
    [InlineData(120)]
    [InlineData(140)]
    [Trait("Exigence", "SHOW-003")]
    [Trait("Exigence", "SHOW-001")]
    public void Blocks_StartAndStopOnTheirBars_AtAnyTempo(double bpm)
    {
        var h = new SequencerHarness();
        var blue = h.Scene("Bleu lent", h.Colors);
        var rainbow = h.Scene("Arc-en-ciel", h.Colors);
        var circle = h.Scene("Cercle", h.Movements);
        var sequence = new Sequence
        {
            Name = "Montée 16 mesures",
            Bars = 16,
            Quantize = ShowQuantize.None,
            Tracks = [Track(h.Colors.Id, Block(0, 8, blue.Id), Block(8, 4, rainbow.Id)), Track(h.Movements.Id, Block(8.25, 2, circle.Id))],
        };
        h.Sequences.Add(sequence);
        h.Load(bpm);

        var start = h.Seconds;
        h.Send(new LaunchSequenceCommand(CommandOrigin.Tool, sequence.Id));
        var bar = 4 * 60 / bpm;
        h.RunTo(start + (16.5 * bar));

        // La séquence part au tick qui suit la commande ; ses blocs tombent ensuite au tick près sur l'horloge.
        var tolerance = SequencerHarness.Period.TotalSeconds * 1.01;
        var first = h.Starts(blue).ShouldHaveSingleItem();
        first.ShouldBe(start + SequencerHarness.Period.TotalSeconds, tolerance);
        h.Starts(rainbow).ShouldHaveSingleItem().ShouldBe(first + (8 * bar), tolerance, "mesure 9");
        h.Starts(circle).ShouldHaveSingleItem().ShouldBe(first + (8.25 * bar), tolerance, "mesure 9, temps 2");
        h.Playing(rainbow).ShouldBeFalse("arrêtée à la fin de son bloc (mesure 13)");
        h.Sequencer.State.Sequences.ShouldBeEmpty("une seule fois");
    }

    [Fact]
    [Trait("Exigence", "SHOW-005")]
    public void Launch_IsQuantizedToTheNextBar()
    {
        var h = new SequencerHarness();
        var blue = h.Scene("Bleu", h.Colors);
        var sequence = new Sequence { Name = "Bleu", Bars = 2, Tracks = [Track(h.Colors.Id, Block(0, 2, blue.Id))] };
        h.Sequences.Add(sequence);
        h.Load(120);
        h.RunTo(0.5);

        h.Send(new LaunchSequenceCommand(CommandOrigin.Tool, sequence.Id));
        h.RunTo(1.9);
        h.Playing(blue).ShouldBeFalse("attend la mesure suivante");
        h.Sequencer.State.Sequences.ShouldHaveSingleItem().PositionBars.ShouldBeLessThan(0);
        h.RunTo(2.1);
        h.Starts(blue).ShouldHaveSingleItem().ShouldBe(2.0, 0.026);
    }

    [Fact]
    [Trait("Exigence", "SHOW-005")]
    [Trait("Exigence", "SHOW-003")]
    public void Loop_KeepsTheSceneWithoutRelaunch_AndHandsOverOnTheSameTrack()
    {
        var h = new SequencerHarness();
        var a = h.Scene("A", h.Colors);
        var b = h.Scene("B", h.Colors);
        var sequence = new Sequence { Name = "Boucle", Bars = 2, End = SequenceEnd.Loop, Quantize = ShowQuantize.None, Tracks = [Track(h.Colors.Id, Block(0, 1, a.Id), Block(1, 1, b.Id))] };
        h.Sequences.Add(sequence);
        h.Load(120);

        h.Send(new LaunchSequenceCommand(CommandOrigin.Tool, sequence.Id));
        h.RunTo(2.5);
        h.Playing(b).ShouldBeTrue("relais à la mesure 2");
        h.Playing(a).ShouldBeFalse("remplacée par le lancement de B (couche exclusive)");
        h.RunTo(4.5);
        h.Playing(a).ShouldBeTrue("deuxième passage");
        h.Sequencer.State.Sequences.ShouldHaveSingleItem().Loops.ShouldBe(1);
        h.Starts(a).Count.ShouldBe(2);

        h.Send(new StopSequenceCommand(CommandOrigin.Tool, sequence.Id));
        h.Tick();
        h.Tick();
        h.Playing(a).ShouldBeFalse("arrêter la séquence arrête ses scènes en cours");
        h.Sequencer.State.Sequences.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "SHOW-005")]
    public void Loop_ABlockCoveringThePass_KeepsItsSceneAcrossTheLoop()
    {
        var h = new SequencerHarness();
        var a = h.Scene("A", h.Colors);
        var sequence = new Sequence { Name = "Boucle", Bars = 2, End = SequenceEnd.Loop, Quantize = ShowQuantize.None, Tracks = [Track(h.Colors.Id, Block(0, 2, a.Id))] };
        h.Sequences.Add(sequence);
        h.Load(120);

        h.Send(new LaunchSequenceCommand(CommandOrigin.Tool, sequence.Id));
        h.RunTo(8.5);
        h.Sequencer.State.Sequences.ShouldHaveSingleItem().Loops.ShouldBe(2);
        h.Playing(a).ShouldBeTrue();
        h.Starts(a).Count.ShouldBe(1, "le bloc se relaie à lui-même : ni arrêt ni relance au rebouclage");
    }

    [Fact]
    [Trait("Exigence", "SHOW-003")]
    public void SameSceneOnConsecutiveBlocks_IsNotInterrupted()
    {
        var h = new SequencerHarness();
        var a = h.Scene("A", h.Colors);
        var sequence = new Sequence { Name = "A A", Bars = 4, Quantize = ShowQuantize.None, Tracks = [Track(h.Colors.Id, Block(0, 2, a.Id), Block(2, 2, a.Id), Block(4, 1, a.Id))] };
        h.Sequences.Add(sequence);
        h.Load(120);

        h.Send(new LaunchSequenceCommand(CommandOrigin.Tool, sequence.Id));
        h.RunTo(7.5);
        h.Starts(a).Count.ShouldBe(1);
        h.Playing(a).ShouldBeTrue();
        h.RunTo(8.2);
        h.Playing(a).ShouldBeFalse("fin de la séquence (le dernier bloc dépasse : coupé à la fin)");
    }

    [Fact]
    [Trait("Exigence", "SHOW-004")]
    public void LayerLevelRamp_OverFourBars()
    {
        var h = new SequencerHarness();
        var sequence = new Sequence
        {
            Name = "Rampe",
            Bars = 4,
            Quantize = ShowQuantize.None,
            Tracks = [Track(null, new SequenceBlock { Start = 0, Length = 4, Action = new BlockAction { Kind = BlockActionKind.LayerLevel, LayerId = h.Colors.Id, From = 1, To = 0.4 } })],
        };
        h.Sequences.Add(sequence);
        h.Load(120);

        h.Send(new LaunchSequenceCommand(CommandOrigin.Tool, sequence.Id));
        h.RunTo(4.0);
        h.LayerLevel(h.Colors).ShouldBe(0.7, 0.01, "à mi-parcours");
        h.RunTo(8.5);
        h.LayerLevel(h.Colors).ShouldBe(0.4, 1e-6, "la rampe finit à sa valeur, qui reste");
        h.Engine.CommandLog().Count(e => e.Command is SetLayerMasterCommand).ShouldBe(2, "seuls le début et la fin de la rampe sont journalisés");
    }

    [Fact]
    [Trait("Exigence", "SHOW-004")]
    public void FlashAndBlackoutBlocks_LastTheirBlock()
    {
        var h = new SequencerHarness();
        var white = h.Scene("Blanc", h.Effects);
        var sequence = new Sequence
        {
            Name = "Flash puis noir",
            Bars = 2,
            Quantize = ShowQuantize.None,
            Tracks =
            [
                Track(
                    null,
                    new SequenceBlock { Start = 0, Length = 0.5, Action = new BlockAction { Kind = BlockActionKind.Flash, SceneId = white.Id } },
                    new SequenceBlock { Start = 1, Length = 0.25, Action = new BlockAction { Kind = BlockActionKind.Blackout } }),
            ],
        };
        h.Sequences.Add(sequence);
        h.Load(120);

        h.Send(new LaunchSequenceCommand(CommandOrigin.Tool, sequence.Id));
        h.RunTo(0.5);
        h.Engine.Snapshot.Playbacks.ShouldContain(p => p.SceneId == white.Id && p.Flash);
        h.RunTo(1.2);
        h.Engine.Snapshot.Playbacks.ShouldNotContain(p => p.SceneId == white.Id && p.Flash);
        h.RunTo(2.2);
        h.Engine.Snapshot.Blackout.ShouldBeTrue();
        h.RunTo(2.7);
        h.Engine.Snapshot.Blackout.ShouldBeFalse("un temps de noir");
    }

    [Fact]
    [Trait("Exigence", "SHOW-008")]
    public void DoubleTime_PlaysTwiceAsFast()
    {
        var h = new SequencerHarness();
        var a = h.Scene("A", h.Colors);
        var b = h.Scene("B", h.Colors);
        var sequence = new Sequence { Name = "Double", Bars = 2, Speed = 2, Quantize = ShowQuantize.None, Tracks = [Track(h.Colors.Id, Block(0, 1, a.Id), Block(1, 1, b.Id))] };
        h.Sequences.Add(sequence);
        h.Load(120);

        h.Send(new LaunchSequenceCommand(CommandOrigin.Tool, sequence.Id));
        h.RunTo(1.1);
        h.Starts(b).ShouldHaveSingleItem().ShouldBe(1.0, 0.026, "une mesure de la séquence = une demi-mesure de l'horloge");
    }

    [Fact]
    [Trait("Exigence", "SHOW-006")]
    public void SequenceCommands_AreRejectedWithoutSequencer()
    {
        var engine = new Engine.RenderEngine(new NoFrames(), new Core.Time.VirtualClock(), 1, null, null, 1);
        engine.Send(new LaunchSequenceCommand(CommandOrigin.Tool, Guid.NewGuid()));
        engine.Tick();
        engine.CommandLog()[^1].Rejection.ShouldBe("aucun séquenceur de shows");
    }

    private sealed class NoFrames : Core.Dmx.IFrameSink
    {
        public void Submit(int universe, Core.Dmx.DmxFrame frame, TimeSpan timestamp)
        {
        }
    }
}
