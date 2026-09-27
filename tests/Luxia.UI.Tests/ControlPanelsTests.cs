using Luxia.Fixtures.Model;
using Luxia.Messaging.Commands;
using Luxia.Scenes.Model;
using Luxia.UI.Controls;
using Luxia.UI.Modules.Control;

namespace Luxia.UI.Tests;

/// <summary>Écran Contrôle : panneaux Colonnes, Plan, Réglages, Propriétés, Journal, et écriture différée des gestes.</summary>
public sealed class ControlPanelsTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private ControlViewModel _vm = null!;

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
        _vm = new ControlViewModel(_host.Runtime, _host.Dialogs);
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
    public void EditBand_ChoosesThenReleasesTheScene_AndOutlinesIt()
    {
        var button = Button("Chenillard 4 couleurs");

        _vm.Columns.ChooseForEditCommand.Execute(button);
        _vm.Session.EditScene!.Id.ShouldBe(button.Scene.Id);
        Button("Chenillard 4 couleurs").IsEditTarget.ShouldBeTrue();
        _vm.Properties.HasScene.ShouldBeTrue();
        _vm.Properties.Name.ShouldBe("Chenillard 4 couleurs");

        _vm.Columns.ChooseForEditCommand.Execute(Button("Chenillard 4 couleurs"));
        _vm.Session.EditScene.ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "ERG-018")]
    [Trait("Exigence", "SCN-001")]
    public async Task NewScene_InTheColumnLayer_ChosenForEdit_Undoable()
    {
        _host.Dialogs.TextAnswers.Enqueue("Ma couleur libre");
        var free = _vm.Columns.Columns.Single(c => c.Layer.Name == "Libre");

        await _vm.Columns.NewSceneCommand.ExecuteAsync(free);

        var scene = _host.Runtime.Project.Scenes.Scenes.Single(s => s.Name == "Ma couleur libre");
        scene.LayerId.ShouldBe(LayerSet.FreeLayerId);
        _vm.Session.EditScene!.Id.ShouldBe(scene.Id);
        _vm.Columns.Columns.Single(c => c.Layer.Name == "Libre").Scenes.ShouldHaveSingleItem();

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

    // ——— Plan ———

    [Fact]
    [Trait("Exigence", "ERG-014")]
    [Trait("Exigence", "SIM-010")]
    public void Plan_ClickCtrlClickRectangle_BuildTheSharedSelection()
    {
        Guid par1 = Id("PAR 1"), par2 = Id("PAR 2"), lyre = Id("Lyre 1");

        _vm.Plan.OnSelectionRequested(new FixtureSelectionRequest([par1], false));
        _vm.Session.Selection.ShouldBe([par1]);

        _vm.Plan.OnSelectionRequested(new FixtureSelectionRequest([par2], true));
        _vm.Session.Selection.ShouldBe([par1, par2]);

        _vm.Plan.OnSelectionRequested(new FixtureSelectionRequest([par1], true));
        _vm.Session.Selection.ShouldBe([par2], "Ctrl + clic sur un appareil déjà pris le retire");

        _vm.Plan.OnSelectionRequested(new FixtureSelectionRequest([lyre, par2], true));
        _vm.Session.Selection.ShouldBe([par2, lyre], "rectangle avec Ctrl : ajout, sans doublon");

        _vm.Plan.OnSelectionRequested(new FixtureSelectionRequest([], false));
        _vm.Session.Selection.ShouldBeEmpty("clic dans le vide : plus rien");
        _vm.Plan.SelectedIds.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "ERG-014")]
    public void Plan_QuickSelections_EveryOther_Invert_None()
    {
        var pars = _vm.Plan.QuickSelections.Single(q => q.Label == "Tous les PAR");
        _vm.Plan.QuickCommand.Execute(pars);
        var all = _vm.Session.Selection.ToList();
        all.Count.ShouldBeGreaterThan(3);

        _vm.Plan.EveryCommand.Execute("2");
        _vm.Session.Selection.ShouldBe(all.Where((_, i) => i % 2 == 0));

        _vm.Plan.InvertCommand.Execute(null);
        _vm.Session.Selection.ShouldNotContain(all[0]);

        _vm.Plan.SelectNoneCommand.Execute(null);
        _vm.Session.Selection.ShouldBeEmpty();
        _vm.Plan.QuickSelections.ShouldContain(q => q.Label == "PAR gauche → droite", "les sélections enregistrées sont proposées");
    }

    [Fact]
    [Trait("Exigence", "ERG-014")]
    [Trait("Exigence", "GEN-063")]
    public void Plan_ShowsOutput_OrPreviewInBlind()
    {
        _vm.Refresh();
        _vm.Plan.Fixtures.ShouldNotBeEmpty();
        _vm.Plan.IsPreview.ShouldBeFalse();

        _vm.Session.ChooseScene(Scene("Rouge – couleur seule").Id);
        _vm.SetModeCommand.Execute("aveugle");
        _vm.Refresh();

        _vm.Plan.IsPreview.ShouldBeTrue();
        _vm.Plan.Source.ShouldContain("APERÇU");
    }

    // ——— Réglages des appareils ———

    [Fact]
    [Trait("Exigence", "ERG-019")]
    public void Settings_TabsFollowTheSelection()
    {
        Select("PAR 1", "PAR 2");
        _vm.Refresh();
        _vm.Settings.HasColor.ShouldBeTrue();
        _vm.Settings.HasPosition.ShouldBeFalse();
        _vm.Settings.ColorRows.Select(r => r.Attribute).ShouldContain(AttributeKind.Red);

        Select("Lyre 1");
        _vm.Settings.SelectedTab = 2;
        _vm.Refresh();
        _vm.Settings.HasPosition.ShouldBeTrue();
        _vm.Settings.SelectedTab.ShouldBe(2);
        _vm.Settings.OtherRows.ShouldNotBeEmpty("une lyre a gobos, strobe, vitesse…");
        _vm.Settings.OtherRows.SelectMany(r => r.Ranges).ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "ERG-019")]
    [Trait("Exigence", "ERG-004")]
    public void Settings_ColorInLive_OverridesOutput_AndKeepsTheRequestedColorShown()
    {
        Select("PAR 1");
        _vm.Refresh();

        _vm.Settings.RequestColor(new LightColor(120, 1, 1));
        _vm.Refresh(); // avant le tick : la couleur demandée reste affichée (course écran / moteur)
        _vm.Settings.Color.Hue.ShouldBe(120, 0.5);
        Ticks(2);

        _host.Frame()[2].ShouldBe((byte)255, "vert du PAR 1 (canal 3)");
        _vm.Settings.ColorRows.Single(r => r.Attribute == AttributeKind.Green).State.ShouldBe(ParameterState.LiveOverride);
        _vm.Session.HasPendingCommit.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "ERG-019")]
    [Trait("Exigence", "ERG-011")]
    public void Settings_IntensityInEdit_IsWrittenAfterTheGesture()
    {
        var scene = Scene("Rouge – couleur seule");
        _vm.Session.ChooseScene(scene.Id);
        _vm.SetModeCommand.Execute("edition");
        Select("PAR 1");
        _vm.Refresh();

        _vm.Settings.Intensity = 30;
        _vm.Settings.Intensity = 60;
        _vm.Session.HasPendingCommit.ShouldBeTrue();

        for (var i = 0; i <= ControlViewModel.CommitDelayRefreshes; i++)
        {
            _vm.Refresh();
        }

        _vm.Session.HasPendingCommit.ShouldBeFalse("écrit une demi-seconde après le dernier mouvement");
        Scene("Rouge – couleur seule").Steps[0].Values.ShouldContain(v => v.Target.FixtureId == Id("PAR 1") && v.Attribute == AttributeKind.Intensity && v.Level == 0.6);
        _vm.Journal.Lines.ShouldContain(l => l.Contains("enregistré : Intensité"));
        _vm.UndoText.ShouldContain("Intensité");
    }

    [Fact]
    [Trait("Exigence", "ERG-019")]
    [Trait("Exigence", "ERG-003")]
    public void Settings_AimTwoLyres_Relative_EachGetsItsOwnValues()
    {
        Select("Lyre 1", "Lyre 2");
        _vm.Settings.SelectedTab = 2;
        _vm.Refresh();
        var l1 = Id("Lyre 1").ToString();
        var l2 = Id("Lyre 2").ToString();

        _vm.Settings.RequestAim([new PanTiltTarget(l1, 0.3, 0.6), new PanTiltTarget(l2, 0.5, 0.6)]);
        Ticks(3);
        _vm.Session.LiveValues.Count(v => v.Attribute == AttributeKind.Pan).ShouldBe(2);
        _vm.Session.LiveValues.Single(v => v.Attribute == AttributeKind.Pan && v.Target.FixtureId == Id("Lyre 2")).Level.ShouldBe(0.5);
        _vm.Settings.Markers.Single(m => m.Id == l1).Pan.ShouldBe(0.3, 1e-9, "la grille montre la visée demandée sans attendre");
    }

    [Fact]
    [Trait("Exigence", "ERG-019")]
    [Trait("Exigence", "ERG-013")]
    [Trait("Exigence", "ERG-017")]
    public void Settings_Zones_DrawAllowedAndForbidden_Modify_Delete()
    {
        Select("Lyre 2");
        _vm.Settings.SelectedTab = 2;
        _vm.Refresh();
        var existing = Modules.Control.Zones.Of(_host.Runtime.Project.Venues, Id("Lyre 2")).Count;
        _vm.Settings.IsZoneEditing = true;
        _vm.ModeTitle.ShouldBe("ZONES", "C4 : le bandeau dit que les zones vont au lieu");

        _vm.Settings.NewZoneAllowed = true;
        _vm.Settings.RequestZone(new PanTiltZoneRequest(null, new PanTiltRect(0.1, 0.9, 0.1, 0.9)));
        _vm.Settings.NewZoneAllowed = false;
        _vm.Settings.RequestZone(new PanTiltZoneRequest(null, new PanTiltRect(0.4, 0.6, 0.1, 0.3)));
        _vm.Flush();

        var zones = Modules.Control.Zones.Of(_host.Runtime.Project.Venues, Id("Lyre 2"));
        zones.Count.ShouldBe(existing + 2);
        zones.Count(z => z.Allowed).ShouldBe(1);
        _host.Runtime.Show.Last!.Model.Safety.Zones.ShouldContain(g => g.Limits != null, "la zone permise arrive au moteur");

        var last = (existing + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        _vm.Settings.RequestZone(new PanTiltZoneRequest(last, new PanTiltRect(0.45, 0.65, 0.1, 0.3)));
        _vm.Flush();
        Modules.Control.Zones.Of(_host.Runtime.Project.Venues, Id("Lyre 2"))[existing + 1].PanMin.ShouldBe(0.45);

        _vm.Settings.DeleteZone(last);
        Modules.Control.Zones.Of(_host.Runtime.Project.Venues, Id("Lyre 2")).Count.ShouldBe(existing + 1);

        _vm.Settings.IsZoneEditing = false;
        _vm.ModeTitle.ShouldBe("LIVE");
    }

    [Fact]
    [Trait("Exigence", "ERG-019")]
    [Trait("Exigence", "SCN-008")]
    public void Settings_PaletteAndRange_AreApplied()
    {
        Select("PAR 1");
        _vm.Refresh();
        var red = _vm.Settings.ColorPalettes.Single(p => p.Name == "Rouge");

        _vm.Settings.ApplyPaletteCommand.Execute(red);
        _vm.Session.LiveValues.ShouldContain(v => v.PaletteId == red.Id);

        Select("Lyre 1");
        _vm.Refresh();
        var row = _vm.Settings.OtherRows.First(r => r.HasRanges);
        _vm.Settings.ApplyRangeCommand.Execute(row.Ranges[^1]);
        _vm.Session.LiveValues.ShouldContain(v => v.Attribute == row.Attribute && v.Range != null);

        _vm.Settings.RemoveFamilyCommand.Execute("couleur");
        _vm.Settings.RemoveSelectionCommand.Execute(null);
        _vm.Session.LiveValues.ShouldNotContain(v => v.Target.FixtureId == Id("Lyre 1"));
    }

    [Fact]
    [Trait("Exigence", "ERG-019")]
    public void Settings_HeaderSaysWhereSettingsGo()
    {
        Select("PAR 1");
        _vm.Settings.TargetText.ShouldContain("LIVE");
        _vm.Settings.ActionText.ShouldBe("Libérer la sélection");

        _vm.Session.ChooseScene(Scene("Chenillard 4 couleurs").Id);
        _vm.Session.ChooseStep(1);
        _vm.SetModeCommand.Execute("edition");

        _vm.Settings.TargetText.ShouldBe("écrit dans « Chenillard 4 couleurs » › étape 2");
        _vm.Settings.ActionText.ShouldBe("Retirer de l'étape");
        _vm.ModeText.ShouldContain("étape 2");
    }

    // ——— Propriétés ———

    [Fact]
    [Trait("Exigence", "ERG-016")]
    [Trait("Exigence", "SCN-002")]
    public void Properties_EditFieldsAndSteps_AreSavedAndUndoable()
    {
        var scene = Scene("Chenillard 4 couleurs");
        _vm.Session.ChooseScene(scene.Id);
        var steps = scene.Steps.Count;

        _vm.Properties.Name = "Chenillard doux";
        _vm.Properties.SpeedPercent = 50;
        _vm.Flush();
        Scene("Chenillard doux").Speed.ShouldBe(0.5);

        _vm.Properties.AddStepCommand.Execute(null);
        Scene("Chenillard doux").Steps.Count.ShouldBe(steps + 1);
        _vm.Session.EditStep.ShouldBe(1);
        _vm.Properties.Steps.Count.ShouldBe(steps + 1);

        _vm.Properties.StepHoldSeconds = 3;
        _vm.Flush();
        Scene("Chenillard doux").Steps[1].Hold.ToSeconds(120).ShouldBe(3);

        _vm.Properties.MoveStepRightCommand.Execute(null);
        _vm.Session.EditStep.ShouldBe(2);
        _vm.Properties.DeleteStepCommand.Execute(null);
        Scene("Chenillard doux").Steps.Count.ShouldBe(steps);

        _vm.Undo();
        Scene("Chenillard doux").Steps.Count.ShouldBe(steps + 1);
    }

    [Fact]
    [Trait("Exigence", "ERG-016")]
    public void Properties_StepContent_IsReadable()
    {
        _vm.Session.ChooseScene(Scene("Rouge – couleur seule").Id);

        _vm.Properties.StepValues.ShouldNotBeEmpty();
        _vm.Properties.StepValues.ShouldAllBe(r => r.Who.Length > 0 && r.What.Length > 0);
    }

    // ——— Journal ———

    [Fact]
    [Trait("Exigence", "ERG-018")]
    [Trait("Exigence", "LIVE-009")]
    public void Journal_ShowsLaunchedScenes()
    {
        _vm.Columns.Press(Button("Plein feu"));
        Ticks(3);
        _vm.Refresh();

        _vm.Journal.Lines.ShouldContain(l => l.Contains("▶ Plein feu"));
    }

    // ——— Verrou soirée ———

    [Fact]
    [Trait("Exigence", "ERG-021")]
    public async Task Lock_PlayAndLiveStillWork_EditingIsRefused()
    {
        _vm.Session.ChooseScene(Scene("Chenillard 4 couleurs").Id);
        _vm.SetModeCommand.Execute("edition");

        _vm.ToggleLockCommand.Execute(null);

        _vm.IsLocked.ShouldBeTrue();
        _vm.Session.Mode.ShouldBe(EditMode.Live, "le verrou ramène en LIVE");
        _vm.SetModeCommand.Execute("edition");
        _vm.Session.Mode.ShouldBe(EditMode.Live);
        _vm.Message.ShouldBe(ControlSession.LockedReason);
        _vm.Columns.CanEdit.ShouldBeFalse();
        _vm.Properties.IsEditable.ShouldBeFalse();
        _vm.Settings.CanEditZones.ShouldBeFalse();
        _vm.ModeText.ShouldContain("Verrou soirée");

        // Jouer et retoucher en direct restent possibles.
        _vm.Columns.Press(Button("Plein feu"));
        Select("PAR 1");
        _vm.Settings.RequestColor(new LightColor(0, 1, 1));
        Ticks(3);
        _host.Runtime.Engine.Snapshot.Playbacks.ShouldContain(p => p.SceneId == Scene("Plein feu").Id);
        _vm.Session.LiveValues.ShouldNotBeEmpty();

        // Modifier une scène ou le projet est refusé.
        var count = _host.Runtime.Project.Scenes.Scenes.Count;
        _host.Dialogs.TextAnswers.Enqueue("Interdit");
        await _vm.Columns.NewSceneCommand.ExecuteAsync(_vm.Columns.Columns[0]);
        _vm.Columns.DuplicateCommand.Execute(Button("Plein feu"));
        _vm.Properties.Name = "Renommée malgré le verrou";
        _host.Runtime.Project.Scenes.Scenes.Count.ShouldBe(count);
        _host.Runtime.Project.Scenes.Scenes.ShouldNotContain(s => s.Name == "Renommée malgré le verrou");

        _vm.ToggleLockCommand.Execute(null);
        _vm.IsLocked.ShouldBeFalse();
        _vm.Columns.CanEdit.ShouldBeTrue();
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
