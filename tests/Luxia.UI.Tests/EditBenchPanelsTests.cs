using Luxia.Fixtures.Model;
using Luxia.Messaging.Commands;
using Luxia.Scenes.Model;
using Luxia.UI.Controls;
using Luxia.UI.Modules.Control;

namespace Luxia.UI.Tests;

/// <summary>Établi d'édition : panneaux Plan, Réglages, Effets, Propriétés et écriture différée des gestes (fenêtre d'édition).</summary>
public sealed class EditBenchPanelsTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private EditBenchViewModel _vm = null!;

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
        _vm = new EditBenchViewModel(_host.Runtime, _host.Dialogs);
        _host.Tick();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

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

    [Fact]
    [Trait("Exigence", "ERG-019")]
    public void Settings_WarnWhenIntensityIsZero_ColorWouldNotShow()
    {
        Select("PAR 1", "PAR 2");
        _vm.Refresh();
        _vm.Settings.IsDark.ShouldBeTrue("rien ne joue : intensité à 0");

        _host.Runtime.Engine.Send(new LaunchSceneCommand(CommandOrigin.User, Scene("Plein feu").Id));
        Ticks(40);
        _vm.Refresh();
        _vm.Settings.IsDark.ShouldBeFalse();
    }

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

        for (var i = 0; i <= EditBenchViewModel.CommitDelayRefreshes; i++)
        {
            _vm.Refresh();
        }

        _vm.Session.HasPendingCommit.ShouldBeFalse("écrit une demi-seconde après le dernier mouvement");
        Scene("Rouge – couleur seule").Steps[0].Values.ShouldContain(v => v.Target.FixtureId == Id("PAR 1") && v.Attribute == AttributeKind.Intensity && v.Level == 0.6);
        _vm.Journal.Lines.ShouldContain(l => l.Contains("enregistré : Intensité"));
        _vm.Session.UndoDescription.ShouldNotBeNull().ShouldContain("Intensité");
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
        _vm.Settings.IsZoneEditing.ShouldBeTrue("C4 : les zones vont au lieu, pas à la scène");

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
        _vm.Settings.IsZoneEditing.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "ERG-017")]
    public void Settings_Zones_ChosenFromTheList_Renamed_Deleted()
    {
        Select("Lyre 2");
        _vm.Settings.SelectedTab = 2;
        _vm.Refresh();
        _vm.Settings.IsZoneEditing = true;
        _vm.Settings.RequestZone(new PanTiltZoneRequest(null, new PanTiltRect(0.4, 0.6, 0.1, 0.3)));
        _vm.Flush();
        var id = _vm.Settings.ZoneLines[^1].Id;

        _vm.Settings.SelectZoneCommand.Execute(null);
        _vm.Settings.HasSelectedZone.ShouldBeFalse();
        _vm.Settings.SelectZoneCommand.Execute(id);
        _vm.Settings.HasSelectedZone.ShouldBeTrue();
        _vm.Settings.ZoneLines[^1].IsSelected.ShouldBeTrue("la ligne choisie est mise en évidence");

        _vm.Settings.SelectedZoneName = "Régie";
        Modules.Control.Zones.Of(_host.Runtime.Project.Venues, Id("Lyre 2"))[^1].Name.ShouldBe("Régie");
        _vm.Undo();
        Modules.Control.Zones.Of(_host.Runtime.Project.Venues, Id("Lyre 2"))[^1].Name.ShouldNotBe("Régie", "Ctrl+Z annule le renommage");

        var count = _vm.Settings.ZoneLines.Count;
        _vm.Settings.SelectZoneCommand.Execute(id);
        _vm.Settings.DeleteSelectedZoneCommand.Execute(null);
        _vm.Settings.ZoneLines.Count.ShouldBe(count - 1);
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
