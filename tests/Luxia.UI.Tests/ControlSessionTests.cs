using Luxia.Fixtures.Model;
using Luxia.Messaging.Commands;
using Luxia.Patch.Model;
using Luxia.Scenes.Model;
using Luxia.UI.Modules.Control;

namespace Luxia.UI.Tests;

/// <summary>Écran Contrôle : ce que devient un réglage selon le mode LIVE / ÉDITION / AVEUGLE (doc 60 §4.1-4.2).</summary>
public sealed class ControlSessionTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private ControlSession _session = null!;

    private Guid Par1 => _host.Runtime.Project.Installation.Fixtures.Single(f => f.Name == "PAR 1").Id;

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
        _session = new ControlSession(_host.Runtime);
        _host.Tick();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    [Trait("Exigence", "ERG-010")]
    public void Live_SettingIsTemporaryOverride_ReleasedByReleaseAll()
    {
        _session.Select([Par1]);

        _session.Apply(t => new SceneValue { Target = t, Color = new LogicalColor { R = 1 } }, "Couleur");
        _host.Tick();

        _host.Frame()[1].ShouldBe((byte)255, "rouge du PAR 1 (canal 2)");
        _session.StateOf(Info(Par1), AttributeKind.Red).ShouldBe(ParameterState.LiveOverride);
        _session.HasPendingCommit.ShouldBeFalse("rien n'est enregistré en LIVE");
        _session.LiveFixtureCount.ShouldBe(1);

        _session.ReleaseAll();
        _host.Tick();

        _host.Frame()[1].ShouldBe((byte)0);
        _session.StateOf(Info(Par1), AttributeKind.Red).ShouldBe(ParameterState.Unused);
    }

    [Fact]
    [Trait("Exigence", "ERG-010")]
    public void Live_OverrideStays_WhenASceneOnTheSameChannelIsLaunched()
    {
        _session.Select([Par1]);
        _session.Apply(t => new SceneValue { Target = t, Color = new LogicalColor { B = 1 } }, "Couleur");

        // F2 : la surcharge est une intervention voulue, elle ne cède pas à la scène.
        _host.Runtime.Engine.Send(new LaunchSceneCommand(CommandOrigin.User, Scene("Rouge – couleur seule").Id));
        for (var i = 0; i < 40; i++)
        {
            _host.Tick();
        }

        _host.Frame()[1].ShouldBe((byte)0, "rouge de la scène masqué");
        _host.Frame()[3].ShouldBe((byte)255, "bleu surchargé gardé");
    }

    [Fact]
    [Trait("Exigence", "ERG-010")]
    public void EditAndBlind_WithoutScene_AreRefusedWithAReason()
    {
        _session.SetMode(EditMode.Edit).ShouldNotBeNull();
        _session.SetMode(EditMode.Blind).ShouldNotBeNull();
        _session.Mode.ShouldBe(EditMode.Live);
    }

    [Fact]
    [Trait("Exigence", "ERG-011")]
    public void Edit_WritesIntoStep_OneUndoEntryPerGesture()
    {
        var scene = Scene("Rouge – couleur seule");
        _session.ChooseScene(scene.Id);
        _session.SetMode(EditMode.Edit).ShouldBeNull();
        _session.Select([Par1]);

        // Un glisser : plusieurs demandes, un seul enregistrement.
        _session.Apply(t => new SceneValue { Target = t, Attribute = AttributeKind.Intensity, Level = 0.3 }, "Intensité");
        _session.Apply(t => new SceneValue { Target = t, Attribute = AttributeKind.Intensity, Level = 0.5 }, "Intensité");
        _session.HasPendingCommit.ShouldBeTrue();
        _session.Commit();

        Stored(scene.Id).Steps[0].Values.ShouldContain(v => v.Target.FixtureId == Par1 && v.Attribute == AttributeKind.Intensity && v.Level == 0.5);
        _session.UndoDescription.ShouldBe("Intensité");

        _session.Undo();
        Stored(scene.Id).Steps[0].Values.ShouldNotContain(v => v.Target.FixtureId == Par1 && v.Attribute == AttributeKind.Intensity);
        _session.CanRedo.ShouldBeTrue();

        _session.Redo();
        Stored(scene.Id).Steps[0].Values.ShouldContain(v => v.Target.FixtureId == Par1 && v.Level == 0.5);
    }

    [Fact]
    [Trait("Exigence", "ERG-011")]
    public void Edit_ShowsTheStepOnOutput_UntilBackToLive()
    {
        _session.ChooseScene(Scene("Rouge – couleur seule").Id);
        _session.SetMode(EditMode.Edit);
        _host.Tick();

        // L'étape (rouge sur les PAR) est visible sans lancer la scène : ce qu'on voit = ce qu'on édite (C7).
        _host.Frame()[1].ShouldBe((byte)255);
        _session.StateOf(Info(Par1), AttributeKind.Red).ShouldBe(ParameterState.InScene);

        _session.SetMode(EditMode.Live);
        _host.Tick();
        _host.Frame()[1].ShouldBe((byte)0);
    }

    [Fact]
    [Trait("Exigence", "ERG-011")]
    [Trait("Exigence", "MOT-041")]
    public void Edit_ColorWithoutIntensity_AlsoLightsTheFixture()
    {
        var scene = Scene("Rouge – couleur seule");
        _session.ChooseScene(scene.Id);
        _session.SetMode(EditMode.Edit);
        _session.Select([_host.Runtime.Project.Installation.Fixtures.Single(f => f.Name == "Gros PAR 1").Id]);

        _session.Apply(t => new SceneValue { Target = t, Color = new LogicalColor { G = 1 } }, "Couleur");
        _session.Commit();

        var gros = _host.Runtime.Project.Installation.Fixtures.Single(f => f.Name == "Gros PAR 1").Id;
        Stored(scene.Id).Steps[0].Values.ShouldContain(v => v.Target.FixtureId == gros && v.Attribute == AttributeKind.Intensity && v.Level == 1);
    }

    [Fact]
    [Trait("Exigence", "ERG-012")]
    public void Blind_WritesStep_WithoutChangingTheOutput()
    {
        var scene = Scene("Rouge – couleur seule");
        _session.ChooseScene(scene.Id);
        _session.SetMode(EditMode.Blind).ShouldBeNull();
        _session.Select([Par1]);

        _session.Apply(t => new SceneValue { Target = t, Color = new LogicalColor { B = 1 } }, "Couleur");
        _session.Commit();
        _host.Tick();
        _host.Runtime.Preview.Tick();

        _host.Frame()[3].ShouldBe((byte)0, "la sortie ne change pas");
        var preview = new byte[512];
        _host.Runtime.Preview.CopyLastFrame(1, preview);
        preview[3].ShouldBe((byte)255, "l'aperçu montre l'étape");
        _host.Runtime.PreviewActive.ShouldBeTrue();
        Stored(scene.Id).Steps[0].Values.ShouldContain(v => v.Target.FixtureId == Par1 && v.Color != null);

        _session.SetMode(EditMode.Live);
        _host.Runtime.PreviewActive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "ERG-011")]
    public void Remove_InEdit_TakesTheFixtureOutOfTheStep()
    {
        var scene = Scene("Rouge – couleur seule");
        _session.ChooseScene(scene.Id);
        _session.SetMode(EditMode.Edit);
        _session.Select([Par1]);
        _session.Apply(t => new SceneValue { Target = t, Attribute = AttributeKind.Intensity, Level = 0.7 }, "Intensité");
        _session.Commit();

        _session.Remove(null, "Retirer de l'étape");
        _session.Commit();

        Stored(scene.Id).Steps[0].Values.ShouldNotContain(v => v.Target.FixtureId == Par1);
    }

    [Fact]
    [Trait("Exigence", "ERG-013")]
    public void EditVenues_IsOneUndoableGesture()
    {
        var before = _host.Runtime.Project.Venues;
        var lyre = _host.Runtime.Project.Installation.Fixtures.Single(f => f.Name == "Lyre 1").Id;

        for (var i = 1; i <= 3; i++)
        {
            var max = 0.2 + (i * 0.1);
            _session.EditVenues(v => Zones.Replace(v, lyre, [new ForbiddenZone { FixtureId = lyre, Name = "Public", PanMin = 0.1, PanMax = max, TiltMin = 0, TiltMax = 0.3 }]), "Zone");
        }

        _session.Commit();
        _host.Runtime.Project.Venues.Active.ForbiddenZones.Single(z => z.FixtureId == lyre && z.Name == "Public").PanMax.ShouldBe(0.5, 1e-9);

        _session.Undo();
        _host.Runtime.Project.Venues.Active.ForbiddenZones.Count(z => z.FixtureId == lyre && z.Name == "Public")
            .ShouldBe(before.Active.ForbiddenZones.Count(z => z.FixtureId == lyre && z.Name == "Public"));
    }

    [Fact]
    [Trait("Exigence", "ERG-010")]
    public void ProjectReopened_BackToLive_WithNothingPushed()
    {
        _session.ChooseScene(Scene("Rouge – couleur seule").Id);
        _session.SetMode(EditMode.Edit);

        _host.Runtime.Project.Open(_host.ProjectFolder).ShouldBeTrue();

        _session.Mode.ShouldBe(EditMode.Live);
        _session.EditScene.ShouldBeNull();
        _session.Selection.ShouldBeEmpty();
    }

    private Scene Scene(string name) => _host.Runtime.Project.Scenes.Scenes.Single(s => s.Name == name);

    private Scene Stored(Guid id) => _host.Runtime.Project.Scenes.Scenes.Single(s => s.Id == id);

    private Scenes.Compilation.FixtureInfo Info(Guid id) => _host.Runtime.Show.Patch.Find(id)!;
}
