using Luxia.Fixtures.Model;
using Luxia.Scenes.Model;
using Luxia.UI.Modules.Scenes;

namespace Luxia.UI.Tests;

/// <summary>Écran Scènes : programmeur, enregistrement dans les étapes, essai, aveugle, palettes, annuler (doc 16, 17).</summary>
public sealed class ScenesViewModelTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private ScenesViewModel _vm = null!;

    public ValueTask InitializeAsync()
    {
        // Copie du show de référence (parc réel, patché) : on n'écrit jamais dans l'échantillon livré.
        var source = Path.Combine(AppContext.BaseDirectory, "samples", "Show de référence");
        foreach (var file in Directory.EnumerateFiles(source, "*.json", SearchOption.AllDirectories))
        {
            var target = Path.Combine(_host.ProjectFolder, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }

        _host.Runtime.Project.Open(_host.ProjectFolder).ShouldBeTrue();
        _vm = new ScenesViewModel(_host.Runtime, _host.Dialogs);
        _host.Tick();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    [Trait("Exigence", "SCN-030")]
    [Trait("Exigence", "SCN-031")]
    [Trait("Exigence", "CMD-021")]
    public void Programmer_SelectionShortcut_ThenColor_OverridesAttributesLive()
    {
        SelectShortcut("Tous les Betopper LPC008S");

        _vm.Programmer.SelectedFixtures.Count.ShouldBe(4);
        _vm.Programmer.HasColor.ShouldBeTrue();
        _vm.Programmer.IntensityTools.ShouldHaveSingleItem();

        _vm.Programmer.Color.Red = 100;
        _host.Tick();

        // Rouge des 4 PAR (canaux 2, 9, 16, 23) ; le gradateur reste à 0 : la couleur seule n'allume pas (D15).
        var frame = _host.Frame();
        new[] { frame[1], frame[8], frame[15], frame[22] }.ShouldAllBe(v => v == 255);
        frame[0].ShouldBe((byte)0);
        _vm.Programmer.Values.Single().Target.Auto.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Exigence", "SCN-001")]
    [Trait("Exigence", "SCN-033")]
    [Trait("Exigence", "MOT-041")]
    [Trait("Exigence", "SCN-036")]
    public async Task NewScene_RecordColor_LightsWhenColoring_ThenPlays()
    {
        var scene = await NewSceneAsync("Rouge sur les PAR");
        SelectShortcut("Tous les Betopper LPC008S");
        _vm.Programmer.Color.Red = 100;

        _vm.Editor.ReplaceStepCommand.Execute(null);

        var recorded = Saved(scene).Steps.Single().Values;
        recorded.Count.ShouldBe(2);
        recorded.ShouldContain(v => v.Color != null);
        recorded.ShouldContain(v => v.Attribute == AttributeKind.Intensity && v.Level == 1);

        _vm.Programmer.Clear();
        _vm.Editor.TestCommand.Execute(null);
        _host.Tick();
        _host.Tick();

        var frame = _host.Frame();
        frame[0].ShouldBe((byte)255);
        frame[1].ShouldBe((byte)255);
        _vm.Scenes.Single(s => s.Id == scene).IsPlaying.ShouldBeFalse();
        _vm.Refresh();
        _vm.Scenes.Single(s => s.Id == scene).IsPlaying.ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "SCN-032")]
    public async Task OnlyTouchedAttributes_AreRecorded_AndRemoveTakesOneOut()
    {
        var scene = await NewSceneAsync("Position");
        SelectFixture("Lyre 1");
        var pan = _vm.Programmer.PositionTools.Single(t => t.Attribute == AttributeKind.Pan);
        var dimmer = _vm.Programmer.IntensityTools.Single();
        pan.Percent = 25;
        dimmer.Percent = 80;
        _vm.Editor.LightWhenColoring = true;

        dimmer.RemoveCommand.Execute(null);
        _vm.Editor.ReplaceStepCommand.Execute(null);

        var values = Saved(scene).Steps.Single().Values;
        values.ShouldHaveSingleItem().Attribute.ShouldBe(AttributeKind.Pan);
        values[0].Level.ShouldBe(0.25);
    }

    [Fact]
    [Trait("Exigence", "SCN-002")]
    [Trait("Exigence", "SCN-004")]
    public async Task Steps_AddDuplicateMove_AndGroupTiming()
    {
        var scene = await NewSceneAsync("Chase");
        _vm.Editor.AddStepCommand.Execute(null);
        _vm.Editor.AddStepCommand.Execute(null);
        _vm.Editor.StepHold.Amount = 0.25m;

        _vm.Editor.ApplyTimingToCheckedCommand.Execute(null);

        var steps = Saved(scene).Steps;
        steps.Count.ShouldBe(3);
        steps.ShouldAllBe(s => s.Hold.Value == 0.25);

        _vm.Editor.MoveStepLeftCommand.Execute(null);
        _vm.Editor.CurrentStep.ShouldBe(1);
        _vm.Editor.DeleteStepCommand.Execute(null);
        Saved(scene).Steps.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Exigence", "SCN-035")]
    [Trait("Exigence", "GEN-063")]
    public void Blind_SendsProgrammerToPreviewOnly()
    {
        SelectShortcut("Tous les Betopper LPC008S");
        _vm.Programmer.Blind = true;

        _vm.Programmer.Color.Green = 100;
        _host.Tick();
        _host.Runtime.Preview.Tick();

        _host.Runtime.PreviewActive.ShouldBeTrue();
        _host.Frame()[2].ShouldBe((byte)0);
        var preview = new byte[512];
        _host.Runtime.Preview.CopyLastFrame(1, preview);
        preview[2].ShouldBe((byte)255);

        _vm.Programmer.Blind = false;
        _host.Tick();
        _host.Frame()[2].ShouldBe((byte)255);
        _host.Runtime.PreviewActive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "SCN-008")]
    [Trait("Exigence", "PAL-006")]
    public async Task PaletteReference_Recorded_ThenDeletedWithFreeze()
    {
        var scene = await NewSceneAsync("Bleu");
        SelectShortcut("Tous les Betopper LPC008S");
        var blue = _vm.Palettes.Colors.Single(p => p.Palette.Name == "Bleu");

        blue.ApplyCommand.Execute(null);
        _vm.Editor.ReplaceStepCommand.Execute(null);
        Saved(scene).Steps.Single().Values.ShouldContain(v => v.PaletteId == blue.Palette.Id);

        await blue.DeleteCommand.ExecuteAsync(null);

        _host.Dialogs.Confirmations.Last().ShouldContain("Figer");
        _host.Runtime.Project.Palettes.Palettes.ShouldNotContain(p => p.Id == blue.Palette.Id);
        var frozen = Saved(scene).Steps.Single().Values;
        frozen.ShouldNotContain(v => v.PaletteId == blue.Palette.Id);
        frozen.ShouldContain(v => v.Color != null && v.Color.B == 1);
    }

    [Fact]
    [Trait("Exigence", "PAL-001")]
    public async Task SaveAsPositionPalette_FromProgrammer()
    {
        SelectFixture("Lyre 1");
        _vm.Programmer.PositionTools.Single(t => t.Attribute == AttributeKind.Tilt).Percent = 30;
        _host.Tick();
        _host.Dialogs.TextAnswers.Enqueue("Essai position");

        await _vm.Palettes.SaveFromProgrammerCommand.ExecuteAsync("Position");

        var palette = _host.Runtime.Project.Palettes.Palettes.Single(p => p.Name == "Essai position");
        palette.Kind.ShouldBe(PaletteKind.Position);
        palette.Values.Single(v => v.Attribute == AttributeKind.Tilt).Level.ShouldBe(0.3, 0.01);
        _vm.Palettes.Positions.ShouldContain(b => b.Palette.Id == palette.Id);
    }

    [Fact]
    [Trait("Exigence", "SCN-039")]
    [Trait("Exigence", "SCN-013")]
    public async Task DeleteScene_ThenUndo_RestoresIt()
    {
        var scene = await NewSceneAsync("À supprimer");
        _vm.SelectedScene = _vm.Scenes.Single(s => s.Id == scene);

        await _vm.DeleteSceneCommand.ExecuteAsync(null);
        _host.Runtime.Project.Scenes.Scenes.ShouldNotContain(s => s.Id == scene);

        _vm.UndoCommand.Execute(null);
        _host.Runtime.Project.Scenes.Scenes.ShouldContain(s => s.Id == scene);
        _vm.RedoText.ShouldBe("Rétablir : Supprimer la scène");
    }

    [Fact]
    [Trait("Exigence", "SCN-012")]
    public async Task Filter_ByNameAndCategory()
    {
        await NewSceneAsync("Essai zèbre");
        await NewSceneAsync("Essai girafe");
        _vm.Editor.Category = "Catégorie d'essai";

        _vm.Filter = "zèbre";
        _vm.Scenes.Select(s => s.Scene.Name).ShouldBe(["Essai zèbre"]);

        _vm.Filter = string.Empty;
        _vm.CategoryFilter = "Catégorie d'essai";
        _vm.Scenes.Select(s => s.Scene.Name).ShouldBe(["Essai girafe"]);
    }

    [Fact]
    [Trait("Exigence", "SCN-037")]
    [Trait("Exigence", "SCN-038")]
    public void CaptureOutput_ThenCopyPasteMirror()
    {
        SelectFixture("Lyre 1");
        _vm.Programmer.PositionTools.Single(t => t.Attribute == AttributeKind.Pan).Percent = 20;
        _host.Tick();
        _vm.Programmer.Clear();
        _host.Tick();

        _vm.Programmer.CaptureOutputCommand.Execute(null);
        _vm.Programmer.Values.ShouldBeEmpty();

        // Rafraîchissement de l'écran (20 fois par seconde en vrai) : le fader reprend la valeur émise (Pan au centre).
        _vm.Refresh();
        _vm.Programmer.PositionTools.Single(t => t.Attribute == AttributeKind.Pan).Percent = 20;
        _vm.Programmer.CopyCommand.Execute(null);
        SelectFixture("Lyre 2");
        _vm.Programmer.PasteMirrorCommand.Execute(null);

        var lyre2 = _host.Runtime.Show.Patch.Fixtures.Single(f => f.Fixture.Name == "Lyre 2").Fixture.Id;
        _vm.Programmer.Values.Single(v => v.Target.FixtureId == lyre2).Level!.Value.ShouldBe(0.8, 1e-9);
    }

    private void SelectShortcut(string label) =>
        _vm.Programmer.SelectCommand.Execute(_vm.Programmer.Shortcuts.Single(s => s.Label == label));

    private void SelectFixture(string name)
    {
        _vm.Programmer.SelectNoneCommand.Execute(null);
        _vm.Programmer.Fixtures.Single(f => f.Name == name).IsSelected = true;
    }

    private async Task<Guid> NewSceneAsync(string name)
    {
        _host.Dialogs.TextAnswers.Enqueue(name);
        await _vm.NewSceneCommand.ExecuteAsync(null);
        return _vm.SelectedScene!.Id;
    }

    private Scene Saved(Guid id) => _host.Runtime.Project.Scenes.Scenes.Single(s => s.Id == id);
}
