using Luxia.Fixtures.Model;
using Luxia.Messaging.Commands;
using Luxia.Scenes.Model;
using Luxia.UI.Controls;
using Luxia.UI.Modules.Control;

namespace Luxia.UI.Tests;

/// <summary>Écran de jeu : panneaux Colonnes, Looks, Journal, Stop / Tout stopper, verrou soirée (ERG-018, ERG-023, ERG-025, ERG-026).</summary>
public sealed class GamePanelsTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private GameViewModel _vm = null!;

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
        _vm = new GameViewModel(_host.Runtime, _host.Dialogs);
        _host.Tick();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    // ——— Colonnes ———

    [Fact]
    [Trait("Exigence", "ERG-018")]
    [Trait("Exigence", "LIVE-003")]
    public void Columns_AreAllLayersByPriority_WithAllTheirScenes()
    {
        _vm.Columns.Columns.Select(c => c.Layer.Name).ShouldBe(["Intensité", "Couleurs", "Mouvements", "Faisceau", "Effets", "Ambiance", "Libre", "Flashs"]);
        _vm.Columns.Columns.SelectMany(c => c.Scenes).Count().ShouldBe(_host.Runtime.Project.Scenes.Scenes.Count, "même les scènes masquées du Live");
    }

    [Fact]
    [Trait("Exigence", "ERG-018")]
    [Trait("Exigence", "LIVE-003")]
    public void Press_LaunchesThenStops_TheEngineDecides()
    {
        var button = Button("Plein feu");

        _vm.Columns.Press(button);
        button.IsActive.ShouldBeTrue("affiché tout de suite, avant le moteur");
        Ticks(3);
        _vm.Refresh();
        _host.Runtime.Engine.Snapshot.Playbacks.ShouldContain(p => p.SceneId == button.Scene.Id);

        _vm.Columns.Press(button);
        Ticks(40);
        _vm.Refresh();
        button.IsActive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "ERG-018")]
    [Trait("Exigence", "CMD-015")]
    public void StepButtons_MoveThePlayingScene()
    {
        var button = Button("Chenillard 4 couleurs");
        var column = _vm.Columns.Columns.Single(c => c.Scenes.Contains(button));
        _vm.Columns.Press(Button("Blanc chaud sur les 4 PAR"));
        Ticks(3);
        _vm.Refresh();
        column.CanStep.ShouldBeFalse("une seule étape : ◀ ▶ grisés");

        _vm.Columns.Press(button);
        Ticks(3);
        _vm.Refresh();
        column.CanStep.ShouldBeTrue();
        var before = _host.Runtime.Engine.Snapshot.ActivePlayback(button.Scene.Id)!.Value.StepIndex;

        _vm.Columns.NextStepCommand.Execute(column);
        Ticks(2);

        _host.Runtime.Engine.Snapshot.ActivePlayback(button.Scene.Id)!.Value.StepIndex.ShouldBe((before + 1) % button.Scene.Steps.Count);
    }

    [Fact]
    [Trait("Exigence", "ERG-018")]
    [Trait("Exigence", "COU-005")]
    public void FlashLayer_PlaysOnlyWhileHeld()
    {
        var flash = Button("Flash blanc");

        _vm.Columns.Press(flash);
        Ticks(2);
        _host.Runtime.Engine.Snapshot.Playbacks.ShouldContain(p => p.SceneId == flash.Scene.Id);

        _vm.Columns.Release(flash);
        Ticks(40);
        _host.Runtime.Engine.Snapshot.Playbacks.ShouldNotContain(p => p.SceneId == flash.Scene.Id && p.State != Engine.Model.PlaybackState.FadingOut);
    }

    [Fact]
    [Trait("Exigence", "ERG-018")]
    [Trait("Exigence", "SCN-001")]
    public async Task NewScene_InTheColumnLayer_OpensTheEditor_Undoable()
    {
        _host.Dialogs.TextAnswers.Enqueue("Ma couleur libre");
        var free = _vm.Columns.Columns.Single(c => c.Layer.Name == "Libre");

        await _vm.Columns.NewSceneCommand.ExecuteAsync(free);

        var scene = _host.Runtime.Project.Scenes.Scenes.Single(s => s.Name == "Ma couleur libre");
        scene.LayerId.ShouldBe(LayerSet.FreeLayerId);
        _vm.Editor.SceneId.ShouldBe(scene.Id, "une scène créée s'ouvre aussitôt dans la fenêtre d'édition");
        _vm.Columns.Columns.Single(c => c.Layer.Name == "Libre").Scenes.ShouldHaveSingleItem();

        _vm.Editor.Cancel();
        _vm.Undo();
        _host.Runtime.Project.Scenes.Scenes.ShouldNotContain(s => s.Name == "Ma couleur libre");
    }

    [Fact]
    [Trait("Exigence", "ERG-018")]
    public async Task ContextMenu_RenameDuplicateColorLayerHideDelete()
    {
        var button = Button("Intensité 50 %");
        _host.Dialogs.TextAnswers.Enqueue("Demi-intensité");
        await _vm.Columns.RenameCommand.ExecuteAsync(button);
        Scene("Demi-intensité").ShouldNotBeNull();

        _vm.Columns.DuplicateCommand.Execute(Button("Demi-intensité"));
        Scene("Demi-intensité (copie)").ShouldNotBeNull();

        _vm.Columns.SetColorCommand.Execute($"{Scene("Demi-intensité").Id}|#3FB950");
        Scene("Demi-intensité").Color.ShouldBe("#3FB950");

        _vm.Columns.MoveToLayerCommand.Execute($"{Scene("Demi-intensité (copie)").Id}|{LayerSet.FreeLayerId}");
        Scene("Demi-intensité (copie)").LayerId.ShouldBe(LayerSet.FreeLayerId);

        _vm.Columns.ToggleVisibleInLiveCommand.Execute(Button("Demi-intensité"));
        Scene("Demi-intensité").VisibleInLive.ShouldBeFalse();
        Button("Demi-intensité").HiddenInLive.ShouldBeTrue();

        await _vm.Columns.DeleteCommand.ExecuteAsync(Button("Demi-intensité (copie)"));
        _host.Runtime.Project.Scenes.Scenes.ShouldNotContain(s => s.Name == "Demi-intensité (copie)");
        _host.Dialogs.Confirmations.ShouldNotBeEmpty("une suppression demande confirmation (GEN-103)");

        _vm.Undo();
        Scene("Demi-intensité (copie)").ShouldNotBeNull("Ctrl+Z retrouve la scène supprimée");
    }

    [Fact]
    [Trait("Exigence", "ERG-018")]
    [Trait("Exigence", "LIVE-002")]
    public void LayerMaster_SendsCommand_StopLayer_Stops()
    {
        var colors = _vm.Columns.Columns.Single(c => c.Layer.Name == "Couleurs");
        colors.Master = 40;
        Ticks(2);
        _host.Runtime.Engine.Snapshot.LayerMasters[IndexOf(LayerSet.ColorsLayerId)].ShouldBe(0.4, 1e-9);

        _vm.Columns.Press(Button("Rouge – couleur seule"));
        Ticks(3);
        _vm.Columns.StopLayerCommand.Execute(_vm.Columns.Columns.Single(c => c.Layer.Name == "Couleurs"));
        Ticks(60);
        _host.Runtime.Engine.Snapshot.Playbacks.ShouldNotContain(p => p.SceneId == Scene("Rouge – couleur seule").Id);
    }

    // ——— Journal ———

    [Fact]
    [Trait("Exigence", "ERG-018")]
    [Trait("Exigence", "LIVE-009")]
    public async Task Journal_ShowsLaunchedScenes()
    {
        _vm.Columns.Press(Button("Plein feu"));
        Ticks(3);

        // Les événements arrivent sur le fil du bus : on laisse le temps de les recevoir (test instable sinon, vu le 2026-09-28).
        for (var i = 0; i < 50 && !_vm.Journal.Lines.Any(l => l.Contains("▶ Plein feu")); i++)
        {
            await Task.Delay(20);
            _vm.Refresh();
        }

        _vm.Journal.Lines.ShouldContain(l => l.Contains("▶ Plein feu"));
    }

    // ——— Looks ———

    [Fact]
    [Trait("Exigence", "ERG-023")]
    public async Task Looks_CaptureWhatPlays_ThenReplayItFromAnotherState()
    {
        _vm.Columns.Press(Button("Plein feu"));
        _vm.Columns.Press(Button("Bleu sur tout le parc"));
        _vm.Columns.Columns.Single(c => c.Layer.Name == "Couleurs").Master = 50;
        Ticks(40);
        _host.Dialogs.TextAnswers.Enqueue("Bleu calme");

        await _vm.Looks.CaptureCommand.ExecuteAsync(null);

        var look = _host.Runtime.Project.Looks.Looks.ShouldHaveSingleItem();
        look.Name.ShouldBe("Bleu calme");
        look.Actions[0].Kind.ShouldBe(LookActionKind.StopAll);
        look.Actions.Where(a => a.Kind == LookActionKind.LaunchScene).Select(a => a.SceneId).ShouldBe([Scene("Plein feu").Id, Scene("Bleu sur tout le parc").Id], ignoreOrder: true);
        look.Actions.ShouldContain(a => a.Kind == LookActionKind.LayerMaster && a.LayerId == LayerSet.ColorsLayerId && a.Level == 0.5);
        look.Actions.ShouldContain(a => a.Kind == LookActionKind.LayerMaster && a.LayerId == LayerSet.IntensityLayerId && a.Level == 1, "couche d'une scène relancée : remise à son niveau, même à 100 %");
        _vm.Looks.Looks.ShouldHaveSingleItem().Lines.ShouldContain("▶ lancer « Plein feu »");

        // Autre état, puis le look le refait.
        _host.Runtime.Engine.Send(new StopLayerCommand(CommandOrigin.User, Everything: true));
        _vm.Columns.Columns.Single(c => c.Layer.Name == "Couleurs").Master = 100;
        _vm.Columns.Press(Button("Rouge – couleur seule"));
        Ticks(60);

        _vm.Looks.PlayCommand.Execute(_vm.Looks.Looks[0]);
        Ticks(60);
        _vm.Refresh();

        var playing = _host.Runtime.Engine.Snapshot.Playbacks.Where(p => p.State != Engine.Model.PlaybackState.FadingOut).Select(p => p.SceneId).ToList();
        playing.ShouldContain(Scene("Plein feu").Id);
        playing.ShouldContain(Scene("Bleu sur tout le parc").Id);
        playing.ShouldNotContain(Scene("Rouge – couleur seule").Id);
        _host.Runtime.Engine.Snapshot.LayerMasters[IndexOf(LayerSet.ColorsLayerId)].ShouldBe(0.5, 1e-9);
        _vm.Journal.Lines.ShouldContain(l => l.Contains("look « Bleu calme »"));
    }

    [Fact]
    [Trait("Exigence", "ERG-018")]
    public void Compact_IsKeptOnThisComputer()
    {
        _vm.Columns.IsCompact.ShouldBeFalse();

        _vm.Columns.IsCompact = true;

        _host.Runtime.Preferences.Current.CompactScenes.ShouldBeTrue();
        new ColumnsPanelViewModel(_host.Runtime, _vm.Session, _host.Dialogs).IsCompact.ShouldBeTrue("repris au prochain lancement");
    }

    [Fact]
    [Trait("Exigence", "ERG-018")]
    [Trait("Exigence", "CMD-012")]
    public void StopAll_SparesTheAtmosphere_UnlessEverything_EvenLocked()
    {
        _vm.ToggleLockCommand.Execute(null);
        _vm.Columns.Press(Button("Plein feu"));
        _vm.Columns.Press(Button("UV plein"));
        Ticks(3);

        _vm.StopAllCommand.Execute("sauf-protegees");
        Ticks(60);
        Playing().ShouldBe([Scene("UV plein").Id], "l'Ambiance est protégée");

        _vm.StopAllCommand.Execute("tout");
        Ticks(60);
        Playing().ShouldBeEmpty();
    }

    private List<Guid> Playing() => [.. _host.Runtime.Engine.Snapshot.Playbacks.Where(p => p.State != Engine.Model.PlaybackState.FadingOut).Select(p => p.SceneId)];

    [Fact]
    [Trait("Exigence", "ERG-023")]
    public async Task Looks_Capture_LeavesTheGrandMasterToTheOperator()
    {
        _vm.Columns.Press(Button("Plein feu"));
        _host.Runtime.Engine.Send(new SetGrandMasterCommand(CommandOrigin.User, 0.4));
        Ticks(40);
        _host.Dialogs.TextAnswers.Enqueue("Sombre");

        await _vm.Looks.CaptureCommand.ExecuteAsync(null);

        _host.Runtime.Project.Looks.Looks.ShouldHaveSingleItem().Actions.ShouldNotContain(a => a.Kind == LookActionKind.GrandMaster);
    }

    [Fact]
    [Trait("Exigence", "ERG-023")]
    public async Task Looks_RenameColorDelete_AndLockRefusesEditingButNotPlaying()
    {
        _host.Dialogs.TextAnswers.Enqueue("Temps mort");
        await _vm.Looks.CaptureCommand.ExecuteAsync(null);
        var look = _vm.Looks.Looks[0];

        _host.Dialogs.TextAnswers.Enqueue("Pause");
        await _vm.Looks.RenameCommand.ExecuteAsync(look);
        _vm.Looks.SetColorCommand.Execute($"{look.Look.Id}|#F0883E");
        _host.Runtime.Project.Looks.Looks[0].Name.ShouldBe("Pause");
        _host.Runtime.Project.Looks.Looks[0].Color.ShouldBe("#F0883E");

        _vm.ToggleLockCommand.Execute(null);
        _vm.Looks.CanEdit.ShouldBeFalse();
        await _vm.Looks.DeleteCommand.ExecuteAsync(_vm.Looks.Looks[0]);
        _host.Runtime.Project.Looks.Looks.ShouldHaveSingleItem("verrou : pas de suppression");
        _vm.Looks.PlayCommand.Execute(_vm.Looks.Looks[0]);
        _vm.Refresh();
        _vm.Journal.Lines.ShouldContain(l => l.Contains("look « Pause »"), "verrou : on joue quand même");

        _vm.ToggleLockCommand.Execute(null);
        await _vm.Looks.DeleteCommand.ExecuteAsync(_vm.Looks.Looks[0]);
        _host.Runtime.Project.Looks.Looks.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "ERG-023")]
    public async Task Looks_FunctionKeys_PlayByRank()
    {
        _host.Dialogs.TextAnswers.Enqueue("Premier");
        await _vm.Looks.CaptureCommand.ExecuteAsync(null);
        _host.Dialogs.TextAnswers.Enqueue("Second");
        await _vm.Looks.CaptureCommand.ExecuteAsync(null);

        _vm.Looks.Looks.Select(l => l.Key).ShouldBe(["F1", "F2"]);
        _vm.Looks.PlayAt(1).ShouldBeTrue();
        _vm.Looks.PlayAt(5).ShouldBeFalse("pas de 6e look");
        _vm.Refresh();
        _vm.Journal.Lines.ShouldContain(l => l.Contains("look « Second »"));
    }

    private ControlSceneViewModel Button(string name) => _vm.Columns.Columns.SelectMany(c => c.Scenes).Single(s => s.Name == name);

    private Scene Scene(string name) => _host.Runtime.Project.Scenes.Scenes.Single(s => s.Name == name);

    private Guid Id(string name) => _host.Runtime.Project.Installation.Fixtures.Single(f => f.Name == name).Id;

    private void Select(params string[] names) => _vm.Session.Select(names.Select(Id));

    private int IndexOf(Guid layerId)
    {
        var layers = _host.Runtime.Engine.Snapshot.Show.Layers;
        for (var i = 0; i < layers.Count; i++)
        {
            if (layers[i].Id == layerId)
            {
                return i;
            }
        }

        return -1;
    }

    private void Ticks(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _host.Tick();
        }
    }
}
