using Luxia.Engine.Model;
using Luxia.Scenes.Model;
using Luxia.Show.Model;
using Luxia.UI.Modules.Control;
using Luxia.UI.Modules.Control.Sequencing;

namespace Luxia.UI.Tests;

/// <summary>
/// P8 à l'écran (Q44 solution C) : colonne « Shows », bandeau « Show en cours », fenêtres d'édition d'une séquence et d'un show
/// (brouillon, essai, Appliquer / Annuler), sur une copie du show de référence.
/// </summary>
public sealed class SequencingScreensTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private GameViewModel _game = null!;

    public ValueTask InitializeAsync()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "samples", "Show de référence");
        foreach (var file in Directory.EnumerateFiles(source, "*.json", SearchOption.AllDirectories))
        {
            var target = Path.Combine(_host.ProjectFolder, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }

        _host.Runtime.Project.Open(_host.ProjectFolder).ShouldBeTrue();
        _game = new GameViewModel(_host.Runtime, _host.Dialogs);
        _host.Tick();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private void Run(int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            _host.Tick();
            if (_host.Runtime.PreviewActive)
            {
                _host.Runtime.Preview.Tick();
            }
        }

        _game.Refresh();
    }

    private ShowItemViewModel ShowItem(string name) => _game.Columns.Shows.Shows.Single(s => s.Name == name);

    [Fact]
    [Trait("Exigence", "SHOW-006")]
    [Trait("Exigence", "LIVE-023")]
    [Trait("Exigence", "SHOW-026")]
    public void ShowsColumn_LaunchesAShow_AndTheBandSuperviseIt()
    {
        _game.Columns.Shows.Shows.Count.ShouldBe(7);
        _game.Columns.Shows.Sequences.Count.ShouldBe(6);
        _game.Band.IsVisible.ShouldBeFalse("rien ne joue : pas de bandeau");

        _game.Columns.Shows.Press(ShowItem("Couplet / Refrain / Drop"));
        Run(10);

        var item = ShowItem("Couplet / Refrain / Drop");
        item.IsActive.ShouldBeTrue();
        item.State.ShouldBe("étape Intro");
        _game.Band.IsVisible.ShouldBeTrue();
        _game.Band.Title.ShouldBe("Couplet / Refrain / Drop");
        _game.Band.Steps.ShouldBe("Intro");
        _game.Band.Next.ShouldHaveSingleItem().Condition.ShouldBe("énergie ≥ Groove");

        _game.Columns.Shows.Press(ShowItem("Couplet / Refrain / Drop"));
        Run(10);
        ShowItem("Couplet / Refrain / Drop").IsActive.ShouldBeFalse("second clic : le show s'arrête (bascule)");
        _game.Band.IsVisible.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "SHOW-024")]
    public void Validate_AShowWithAnError_AsksFirst_AndLeavesTheErrorInTheJournal()
    {
        var trap = _host.Runtime.Project.Shows.Shows.Single(s => s.Name == "Piège : boucle sans condition");
        _game.EditShow(trap.Id);
        var editor = _game.ShowEditor;
        editor.HasErrors.ShouldBeTrue();
        editor.IssueRows[0].Text.ShouldStartWith("⛔");
        editor.IssueRows[0].Color.ShouldBe(ControlColors.Error);

        _host.Dialogs.ConfirmAnswer = false;
        editor.ValidateCommand.Execute(null);
        editor.IsOpen.ShouldBeTrue("Non : la fenêtre reste ouverte pour corriger");
        _host.Dialogs.Confirmations.ShouldContain(m => m.Contains("refusé au lancement", StringComparison.Ordinal));

        _host.Dialogs.ConfirmAnswer = true;
        editor.ValidateCommand.Execute(null);
        editor.IsOpen.ShouldBeFalse();
        _game.Journal.Refresh();
        _game.Journal.Lines.ShouldContain(l => l.Contains("⛔ « Piège : boucle sans condition » enregistré(e) avec une erreur", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Exigence", "CMD-051")]
    [Trait("Exigence", "LIVE-023")]
    public void Band_ForcesATransition_AtTheNextBar()
    {
        _game.Columns.Shows.Press(ShowItem("Couplet / Refrain / Drop"));
        Run(4);

        _game.Band.ForceCommand.Execute(_game.Band.Transitions[0]);
        Run(84);

        _game.Band.Steps.ShouldBe("Couplet", "forcée à la mesure suivante (2 s à 120 BPM)");

        // Le bus livre les événements sur son propre fil : la ligne du Journal arrive un peu après (attente bornée).
        var waited = System.Diagnostics.Stopwatch.StartNew();
        while (!_game.Journal.Lines.Any(l => l.Contains("étape 1 « Couplet » (forcée)", StringComparison.Ordinal)) && waited.Elapsed < TimeSpan.FromSeconds(2))
        {
            Thread.Sleep(20);
            _game.Journal.Refresh();
        }

        _game.Journal.Lines.ShouldContain(l => l.Contains("étape 1 « Couplet » (forcée)"));
    }

    [Fact]
    [Trait("Exigence", "SHOW-002")]
    [Trait("Exigence", "SHOW-001")]
    public void NewSequence_DropMoveResize_UndoAndValidate()
    {
        _host.Dialogs.TextAnswers.Enqueue("Essai P8");
        _game.Columns.Shows.NewSequenceCommand.Execute(null);
        var editor = _game.SequenceEditor;
        editor.IsOpen.ShouldBeTrue();
        editor.ItemName.ShouldBe("Essai P8");

        // Une scène de la couche Couleurs lâchée sur la piste Intensité va sur la piste Couleurs (D39).
        var blue = _host.Runtime.Project.Scenes.Scenes.Single(s => s.Name == "Bleu sur tout le parc");
        var item = editor.Library.SelectMany(g => g.Items).Single(i => i.SceneId == blue.Id);
        editor.Drop(item, 0, 2).ShouldBeNull();
        var track = editor.Draft!.Tracks.ShouldHaveSingleItem();
        track.LayerId.ShouldBe(LayerSet.ColorsLayerId);
        track.Blocks.ShouldHaveSingleItem().Start.ShouldBe(2);
        editor.Blocks.ShouldHaveSingleItem().Row.ShouldBe(editor.Tracks.ToList().FindIndex(t => t.LayerId == LayerSet.ColorsLayerId));
        editor.SelectedIsScene.ShouldBeTrue();

        editor.Move(editor.Selected!, 3);
        editor.Resize(editor.Selected!, 2);
        editor.Draft.Tracks[0].Blocks[0].ShouldBe(new SequenceBlock { Start = 3, Length = 2, SceneId = blue.Id });
        editor.BlockStartBar.ShouldBe(4, "mesure 4 (début à 3 mesures)");
        editor.Undo();
        editor.Draft.Tracks[0].Blocks[0].Length.ShouldBe(1);

        var smoke = editor.Library.SelectMany(g => g.Items).Single(i => i.ActionKind == BlockActionKind.Smoke);
        editor.Drop(smoke, 1, 7);
        editor.Draft.Tracks.Single(t => t.LayerId is null).Blocks.ShouldHaveSingleItem().Action!.Kind.ShouldBe(BlockActionKind.Smoke);

        editor.ValidateCommand.Execute(null);
        editor.IsOpen.ShouldBeFalse();
        var saved = _host.Runtime.Project.Sequences.Sequences.Single(s => s.Name == "Essai P8");
        saved.Tracks.Count.ShouldBe(2);
        _game.Columns.Shows.Sequences.ShouldContain(s => s.Name == "Essai P8");
    }

    [Fact]
    [Trait("Exigence", "SHOW-002")]
    public void SequenceEditor_Cancel_LeavesTheProjectUntouched()
    {
        var groove = _host.Runtime.Project.Sequences.Sequences.Single(s => s.Name == "Groove 8 mesures");
        _game.EditSequence(groove.Id);
        var editor = _game.SequenceEditor;
        editor.Bars = 4;
        editor.HasChanges.ShouldBeTrue();

        editor.CancelCommand.Execute(null);

        _host.Runtime.Project.Sequences.Sequences.Single(s => s.Id == groove.Id).Bars.ShouldBe(8);
        editor.IsOpen.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "SHOW-020")]
    [Trait("Exigence", "SHOW-022")]
    public void ShowEditor_AddStepAndTransition_RenameStep_Validate()
    {
        var show = _host.Runtime.Project.Shows.Shows.Single(s => s.Name == "Couplet / Refrain / Drop");
        _game.EditShow(show.Id);
        var editor = _game.ShowEditor;
        editor.Cards.Count.ShouldBe(5);
        editor.Nodes.Single(n => n.Id == "5").Row.ShouldBe(2, "Final : deux transitions après l'intro (Couplet, puis au silence)");

        editor.AddStepCommand.Execute(null);
        var card = editor.Cards[^1];
        card.Id.ShouldBe("2");
        card.AddTransitionCommand.Execute(null);
        card = editor.Cards.Single(c => c.Id == "2");
        var row = card.Transitions.ShouldHaveSingleItem();
        row.Kind = ShowOptions.Conditions.Single(c => c.Value == ConditionKind.After);
        row.DurationValue = 4;
        editor.Draft!.Transitions[^1].Condition.ShouldBe(new ShowCondition { Kind = ConditionKind.After, Duration = new Duration(4, DurationUnit.Bars) });

        editor.Cards.Single(c => c.Id == "2").IdText = "pont";
        editor.Draft.Transitions[^1].From.ShouldBe(["pont"]);
        editor.Issues.ShouldContain(i => i.Message.Contains("inatteignable"), "aucune transition n'y mène encore");

        editor.ValidateCommand.Execute(null);
        _host.Runtime.Project.Shows.Shows.Single(s => s.Id == show.Id).Steps.ShouldContain(s => s.Id == "pont");
    }

    [Fact]
    [Trait("Exigence", "SHOW-027")]
    [Trait("Exigence", "SHOW-007")]
    public void ShowEditor_BlindTrial_RunsOnThePreviewOnly_WithSimulatedMusic()
    {
        var show = _host.Runtime.Project.Shows.Shows.Single(s => s.Name == "Couplet / Refrain / Drop");
        _game.EditShow(show.Id);
        var editor = _game.ShowEditor;
        editor.IsBlind = true;
        _host.Runtime.PreviewActive.ShouldBeTrue();

        editor.Simulation.UseMetronome = true;
        editor.Simulation.PlayCommand.Execute(null);
        editor.Simulation.EnergyCommand.Execute("1");
        Run(100);
        editor.Refresh();

        editor.Simulation.Status.ShouldStartWith("Étape active : 1 Couplet");
        editor.Cards.Single(c => c.Id == "1").IsActive.ShouldBeTrue();
        _host.Runtime.Sequencer.State.Shows.ShouldBeEmpty("aveugle : la sortie ne joue rien");

        editor.CancelCommand.Execute(null);
        _host.Runtime.PreviewActive.ShouldBeFalse();
        _host.Runtime.PreviewOwnTempo.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "SHOW-020")]
    public void Lock_RefusesTheEditors()
    {
        _game.Session.SetLocked(true);

        _game.EditShow(_host.Runtime.Project.Shows.Shows[0].Id);

        _game.ShowEditor.IsOpen.ShouldBeFalse();
        _game.Message.ShouldBe(ControlSession.LockedReason);
    }

    [Fact]
    [Trait("Exigence", "SHOW-020")]
    public void BothEditorsOpen_KeepTheirOwnDrafts()
    {
        var show = _host.Runtime.Project.Shows.Shows.Single(s => s.Name == "Tirage au sort (variantes)");
        _game.EditShow(show.Id);
        _game.ShowEditor.Name = "Tirage retouché";
        _game.EditSequence(_host.Runtime.Project.Sequences.Sequences[0].Id);
        _game.SequenceEditor.Bars = 12;

        _host.Runtime.Engine.Send(new Messaging.Commands.LaunchShowCommand(Messaging.Commands.CommandOrigin.User, show.Id));
        Run(4);

        _host.Runtime.Sequencer.State.MainShow!.Name.ShouldBe("Tirage retouché", "le brouillon du show reste joué quand l'éditeur de séquence change le sien");
    }

    [Fact]
    [Trait("Exigence", "SHOW-020")]
    public void ReopeningTheProject_AbandonsTheDraft()
    {
        var show = _host.Runtime.Project.Shows.Shows[0];
        _game.EditShow(show.Id);
        _game.ShowEditor.Name = "Ne doit pas être écrit";

        _host.Runtime.Project.Open(_host.ProjectFolder).ShouldBeTrue();

        _game.ShowEditor.IsOpen.ShouldBeFalse();
        _host.Runtime.Project.Shows.Shows.ShouldNotContain(s => s.Name == "Ne doit pas être écrit");
    }

    [Fact]
    [Trait("Exigence", "SHOW-007")]
    public void MetronomeOnTheOutput_GivesTheLiveTempoBack_WhenTheEditorCloses()
    {
        _host.Runtime.Engine.Send(new Messaging.Commands.SetTempoSourceCommand(Messaging.Commands.CommandOrigin.User, Messaging.Commands.TempoSourceKind.Fixed, 97));
        _host.Runtime.Engine.Send(new Messaging.Commands.SetTempoSourceCommand(Messaging.Commands.CommandOrigin.User, Messaging.Commands.TempoSourceKind.Tap));
        Run(2);
        _game.EditSequence(_host.Runtime.Project.Sequences.Sequences[0].Id);
        var simulation = _game.SequenceEditor.Simulation;
        simulation.UseMetronome = true;
        simulation.MetronomeBpm = 140;
        simulation.PlayCommand.Execute(null);
        Run(2);
        _host.Runtime.Engine.Snapshot.Tempo.Bpm.ShouldBe(140);

        _game.SequenceEditor.CancelCommand.Execute(null);
        Run(2);

        _host.Runtime.Engine.Snapshot.Tempo.Bpm.ShouldBe(97, 0.01);
        _host.Runtime.Engine.Snapshot.Tempo.Source.ShouldBe(Messaging.Commands.TempoSourceKind.Tap);
    }
}

