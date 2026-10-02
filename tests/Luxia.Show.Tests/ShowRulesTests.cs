using Luxia.Engine.Model;
using Luxia.Fixtures.Rules;
using Luxia.Scenes.Model;
using Luxia.Show.Model;
using Luxia.Show.Rules;

namespace Luxia.Show.Tests;

/// <summary>T-SHOW-04 : un jeu de séquences et de shows invalides donne les erreurs attendues (SHOW-024).</summary>
public sealed class ShowRulesTests
{
    private static readonly Scene Blue = new() { Name = "Bleu", LayerId = LayerSet.ColorsLayerId };
    private static readonly Scene Circle = new() { Name = "Cercle", LayerId = LayerSet.MovementsLayerId };
    private static readonly SceneSet Scenes = new() { Scenes = [Blue, Circle] };
    private static readonly LayerSet Layers = LayerSet.Default();

    private static IReadOnlyList<Scenes.Compilation.CompileIssue> Check(SequenceSet? sequences = null, ShowSet? shows = null) =>
        ShowRules.Validate(sequences ?? new SequenceSet(), shows ?? new ShowSet(), Scenes, Layers);

    private static ShowDefinition Show(IReadOnlyList<ShowStep> steps, IReadOnlyList<ShowTransition> transitions, IReadOnlyList<ShowVariable>? variables = null) =>
        new() { Name = "Essai", Steps = steps, Transitions = transitions, Variables = variables ?? [] };

    private static ShowTransition T(string from, string to, ConditionKind kind = ConditionKind.Always, ShowQuantize quantize = ShowQuantize.None) =>
        new() { From = [from], To = [to], Condition = new ShowCondition { Kind = kind, Duration = Duration.FromBeats(4) }, Quantize = quantize };

    [Fact]
    [Trait("Exigence", "SHOW-024")]
    public void ValidShow_HasNoIssue()
    {
        var show = Show(
            [new ShowStep { Id = "0", Initial = true, Actions = [new ShowAction { Kind = ShowActionKind.Play, SceneId = Blue.Id }] }, new ShowStep { Id = "1" }],
            [T("0", "1", ConditionKind.Drop), T("1", "0", ConditionKind.After, ShowQuantize.Bar)]);

        Check(shows: new ShowSet { Shows = [show] }).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "SHOW-024")]
    public void Show_WithoutInitialStep_IsAnError()
    {
        var issues = Check(shows: new ShowSet { Shows = [Show([new ShowStep { Id = "0" }], [])] });

        issues.ShouldContain(i => i.Severity == IssueSeverity.Error && i.Message.Contains("aucune étape initiale"));
    }

    [Fact]
    [Trait("Exigence", "SHOW-024")]
    public void UnreachableStep_IsReported()
    {
        var issues = Check(shows: new ShowSet { Shows = [Show([new ShowStep { Id = "0", Initial = true }, new ShowStep { Id = "perdue" }], [])] });

        issues.ShouldContain(i => i.Item.EndsWith("étape perdue", StringComparison.Ordinal) && i.Message.Contains("inatteignable"));
    }

    [Fact]
    [Trait("Exigence", "SHOW-024")]
    public void LoopOfImmediateTransitions_IsAnError_ButAQuantizedOneIsNot()
    {
        var steps = new[] { new ShowStep { Id = "a", Initial = true }, new ShowStep { Id = "b" } };
        var loop = Check(shows: new ShowSet { Shows = [Show(steps, [T("a", "b"), T("b", "a")])] });
        loop.ShouldContain(i => i.Severity == IssueSeverity.Error && i.Message.Contains("boucle sans condition (a → b → a)"));

        var quantized = Check(shows: new ShowSet { Shows = [Show(steps, [T("a", "b"), T("b", "a", quantize: ShowQuantize.Bar)])] });
        quantized.ShouldBeEmpty("une frontière musicale casse la boucle : une évolution par mesure");
    }

    [Fact]
    [Trait("Exigence", "SHOW-024")]
    public void DeletedSceneAndUnknownReferences_AreErrors()
    {
        var show = Show(
            [new ShowStep
            {
                Id = "0",
                Initial = true,
                MacroShowId = Guid.NewGuid(),
                Actions =
                [
                    new ShowAction { Kind = ShowActionKind.Play, SceneId = Guid.NewGuid() },
                    new ShowAction { Kind = ShowActionKind.PlaySequence, SequenceId = Guid.NewGuid() },
                    new ShowAction { Kind = ShowActionKind.Variable, Variable = "inconnue" },
                ],
            }],
            [new ShowTransition { From = ["0"], To = ["9"] }]);

        var issues = Check(shows: new ShowSet { Shows = [show] });

        issues.ShouldContain(i => i.Field == "actions[0].sceneId" && i.Message.StartsWith("scène supprimée", StringComparison.Ordinal));
        issues.ShouldContain(i => i.Field == "actions[1].sequenceId");
        issues.ShouldContain(i => i.Field == "actions[2].variable");
        issues.ShouldContain(i => i.Field == "macroShowId");
        issues.ShouldContain(i => i.Message.Contains("étape « 9 » inconnue"));
    }

    [Fact]
    [Trait("Exigence", "SHOW-024")]
    public void MacroStepThatContainsItself_IsAnError()
    {
        var a = new ShowDefinition { Name = "A", Steps = [new ShowStep { Id = "0", Initial = true }] };
        var b = new ShowDefinition { Name = "B", Steps = [new ShowStep { Id = "0", Initial = true, MacroShowId = a.Id }] };
        a = a with { Steps = [new ShowStep { Id = "0", Initial = true, MacroShowId = b.Id }] };

        Check(shows: new ShowSet { Shows = [a, b] }).Count(i => i.Message.Contains("se contient elle-même")).ShouldBe(2);
    }

    [Fact]
    [Trait("Exigence", "SHOW-022")]
    public void BadConditions_AreReported()
    {
        var conditions = new[]
        {
            new ShowCondition { Kind = ConditionKind.After },
            new ShowCondition { Kind = ConditionKind.Random, Value = 1.5 },
            new ShowCondition { Kind = ConditionKind.Not, Conditions = [new ShowCondition(), new ShowCondition()] },
            new ShowCondition { Kind = ConditionKind.All, Conditions = [new ShowCondition { Kind = ConditionKind.Variable, Variable = "x" }] },
        };
        var show = Show(
            [new ShowStep { Id = "0", Initial = true }, new ShowStep { Id = "1" }],
            [.. conditions.Select(c => new ShowTransition { From = ["0"], To = ["1"], Condition = c })]);

        var issues = Check(shows: new ShowSet { Shows = [show] });

        issues.ShouldContain(i => i.Field == "condition.duration");
        issues.ShouldContain(i => i.Field == "condition.value" && i.Message.Contains("probabilité"));
        issues.ShouldContain(i => i.Message.Contains("NON attend exactement une condition"));
        issues.ShouldContain(i => i.Field == "condition.conditions[0].variable");
    }

    [Fact]
    [Trait("Exigence", "SHOW-001")]
    [Trait("Exigence", "SHOW-024")]
    public void Sequence_SceneOnTheWrongTrack_AndBlocksOutOfBounds_AreReported()
    {
        var sequence = new Sequence
        {
            Name = "Montée",
            Bars = 8,
            Tracks =
            [
                new SequenceTrack
                {
                    LayerId = LayerSet.ColorsLayerId,
                    Blocks =
                    [
                        new SequenceBlock { Start = 0, Length = 4, SceneId = Blue.Id },
                        new SequenceBlock { Start = 2, Length = 2, SceneId = Circle.Id },
                        new SequenceBlock { Start = 6, Length = 4, SceneId = Blue.Id },
                    ],
                },
                new SequenceTrack { Blocks = [new SequenceBlock { Start = 0, Length = 1, Action = new BlockAction { Kind = BlockActionKind.LayerLevel } }] },
            ],
        };

        var issues = Check(sequences: new SequenceSet { Sequences = [sequence] });

        issues.ShouldContain(i => i.Severity == IssueSeverity.Error && i.Message.Contains("appartient à la couche « Mouvements »"));
        issues.ShouldContain(i => i.Message.Contains("chevauche"));
        issues.ShouldContain(i => i.Message.Contains("dépasse la fin"));
        issues.ShouldContain(i => i.Field == "action.layerId");
    }

    [Fact]
    [Trait("Exigence", "SHOW-001")]
    public void Stores_RoundTrip_SequencesAndShows()
    {
        var folder = Directory.CreateTempSubdirectory("luxia-show-").FullName;
        try
        {
            var sequence = new Sequence
            {
                Name = "Montée 16 mesures",
                Bars = 16,
                End = SequenceEnd.Loop,
                Tracks = [new SequenceTrack { LayerId = LayerSet.ColorsLayerId, Blocks = [new SequenceBlock { Start = 8.25, Length = 4, SceneId = Blue.Id }] }],
            };
            var show = Show(
                [new ShowStep { Id = "0", Initial = true, Choice = StepChoice.Random, AvoidRepeat = true }],
                [new ShowTransition { From = ["0"], To = ["0"], Condition = new ShowCondition { Kind = ConditionKind.After, Duration = new Duration(16, DurationUnit.Bars) }, Quantize = ShowQuantize.Phrase8, Weight = 0.6 }],
                [new ShowVariable("refrains")]);

            SequenceStore.Save(folder, new SequenceSet { Sequences = [sequence] });
            ShowStore.Save(folder, new ShowSet { Shows = [show] });

            var (sequences, sequencesMessage) = SequenceStore.Load(folder);
            var (shows, showsMessage) = ShowStore.Load(folder);
            sequencesMessage.ShouldBeNull();
            showsMessage.ShouldBeNull();
            sequences.Sequences[0].Tracks[0].Blocks[0].ShouldBe(sequence.Tracks[0].Blocks[0]);
            sequences.Sequences[0].End.ShouldBe(SequenceEnd.Loop);
            shows.Shows[0].Transitions[0].Condition.Duration.ShouldBe(new Duration(16, DurationUnit.Bars));
            shows.Shows[0].Transitions[0].Quantize.ShouldBe(ShowQuantize.Phrase8);
            shows.Shows[0].Variables.ShouldBe([new ShowVariable("refrains")]);
            File.ReadAllText(Path.Combine(folder, ShowStore.FileName)).ShouldContain("\"kind\": \"after\"");
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
