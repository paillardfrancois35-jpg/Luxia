using Luxia.Engine.Model;
using Luxia.Scenes.Model;
using Luxia.UI.Controls;
using Luxia.UI.Modules.Control;
using Luxia.UI.Modules.Control.Docking;

namespace Luxia.UI.Tests;

/// <summary>Panneau Effets de l'écran Contrôle (doc 16 §6, EFF-001, EFF-006, EFF-007), sur une copie du show de référence.</summary>
public sealed class EffectsPanelTests : IAsyncLifetime
{
    private static readonly string[] ParNames = ["PAR 1", "PAR 2", "PAR 3", "PAR 4"];
    private static readonly string[] ChaseColors = ["Rouge", "Vert", "Bleu"];
    private readonly TestHost _host = new();
    private ControlViewModel _vm = null!;

    private EffectsPanelViewModel Panel => _vm.Effects;

    private ControlSession Session => _vm.Session;

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

    [Fact]
    [Trait("Exigence", "EFF-001")]
    public void Live_AddIsRefused_WithTheWayToDoIt()
    {
        Session.ChooseScene(Scene("Plein feu").Id);
        SelectPars();
        Panel.CanWrite.ShouldBeFalse();
        Panel.AddEffectCommand.Execute(null);
        Panel.Message.ShouldNotBeNull();
        Panel.Message!.ShouldContain("ÉDITION");
        Session.StepEffects.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "EFF-001")]
    [Trait("Exigence", "EFF-007")]
    [Trait("Exigence", "EFF-006")]
    [Trait("Exigence", "ERG-029")]
    public void Edit_AddWaveFromLibrary_OnPlanSelection_PlaysOnOutputAtOnce_UndoRemovesIt()
    {
        var scene = Scene("Plein feu");
        Session.ChooseScene(scene.Id);
        Session.SetMode(EditMode.Edit).ShouldBeNull();
        SelectPars();
        Panel.CanWrite.ShouldBeTrue();
        Panel.SelectedTemplate = Panel.Templates.Single(t => t.Template.Name == "Chenillard on/off");
        Panel.AddEffectCommand.Execute(null);

        Panel.Message.ShouldBeNull();
        Session.StepEffects.Count.ShouldBe(1);
        var effect = Session.StepEffects[0];
        effect.Targets.Select(t => t.FixtureId).ShouldBe(Pars(), "de gauche à droite sur le plan (les PAR ne sont pas dans cet ordre de sélection)");
        Panel.HasEffect.ShouldBeTrue();
        Panel.MembersText.ShouldBe("PAR 1 → PAR 2 → PAR 3 → PAR 4");

        // Écrit après le geste, recompilé, montré sur la sortie (CMD-017) : le chenillard tourne.
        Session.Commit();
        var levels = new HashSet<byte>();
        for (var i = 0; i < 60; i++)
        {
            _host.Tick();
            levels.Add(_host.Frame()[0]);
        }

        levels.ShouldContain((byte)0, "PAR 1 éteint une partie du cycle");
        levels.ShouldContain((byte)255, "PAR 1 allumé une partie du cycle");

        Session.Undo();
        Session.StepEffects.ShouldBeEmpty();
        Stored(scene.Id).Steps[0].Effects.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "EFF-011")]
    public void IntensityEffect_OnNewScene_GivesWhiteToUncoloredTargets_KeepsExistingColors()
    {
        // Essai P6, exemple 7 : nouvelle scène vide, chenillard d'intensité sur les 4 PAR → ils restaient noirs.
        _host.Dialogs.TextAnswers.Enqueue("Mon effet");
        _vm.Columns.NewSceneCommand.Execute(_vm.Columns.Columns.Single(c => c.Layer.Name == "Effets"));
        Session.SetMode(EditMode.Edit).ShouldBeNull();
        SelectPars();
        Panel.SelectedTemplate = Panel.Templates.Single(t => t.Template.Name == "Chenillard on/off");
        Panel.AddEffectCommand.Execute(null);
        Session.StepValues.Count(v => v.PaletteId == Scenes.Rules.DefaultPalettes.Colors[0].Id).ShouldBe(4, "blanc sur les 4 PAR");
        Session.Commit();

        var levels = new HashSet<byte>();
        for (var i = 0; i < 60; i++)
        {
            _host.Tick();
            levels.Add(_host.Frame()[1]);
        }

        levels.ShouldContain((byte)255, "rouge du PAR 1 allumé (blanc) quand le chenillard passe");

        // Une étape qui a déjà une couleur la garde : la « Vague sur les PAR » est en bleu.
        Session.ChooseScene(Scene("Vague sur les PAR (gauche → droite)").Id);
        Session.SetMode(EditMode.Edit).ShouldBeNull();
        var before = Session.StepValues.Count;
        SelectPars();
        Panel.AddEffectCommand.Execute(null);
        Session.StepValues.Count.ShouldBe(before);
    }

    [Fact]
    [Trait("Exigence", "EFF-001")]
    [Trait("Exigence", "ERG-028")]
    public void Dials_WriteIntoTheEffect_OneGesture()
    {
        AddInEdit("Vague douce");
        Panel.Period = 0.5;
        Panel.Size = 40;
        Panel.Spread = 180;
        Session.StepEffects[0].Period.ShouldBe(Duration.FromSeconds(0.5));
        Session.StepEffects[0].Size.ShouldBe(0.4, 1e-9);
        Session.StepEffects[0].Spread.ShouldBe(180);
        Session.Commit();
        Session.Undo();
        Session.StepEffects[0].Period.ShouldBe(Duration.FromSeconds(3), "les trois réglages faits d'affilée = un seul geste");
    }

    [Fact]
    [Trait("Exigence", "EFF-004")]
    [Trait("Exigence", "MOT-041")]
    public void ColorEffect_LightsTargetsWithoutIntensity()
    {
        var scene = Scene("Rouge – couleur seule");
        Session.ChooseScene(scene.Id);
        Session.SetMode(EditMode.Edit).ShouldBeNull();
        var lyre = Fixture("Lyre 1");
        Session.Select([lyre]);
        Panel.SelectedTemplate = Panel.Templates.Single(t => t.Template.Name == "Arc-en-ciel");
        Panel.AddEffectCommand.Execute(null);

        Session.StepValues.ShouldContain(v => v.Target.FixtureId == lyre && v.Attribute == Fixtures.Model.AttributeKind.Intensity && v.Level == 1);
        Panel.IsColorShape.ShouldBeTrue();
        Panel.PreviewColors!.Count.ShouldBe(12);
    }

    [Fact]
    [Trait("Exigence", "EFF-006")]
    public void Blind_EffectPlaysOnPreviewOnly()
    {
        var scene = Scene("Plein feu");
        Session.ChooseScene(scene.Id);
        Session.SetMode(EditMode.Blind).ShouldBeNull();
        SelectPars();
        Panel.SelectedTemplate = Panel.Templates.Single(t => t.Template.Name == "Chenillard on/off");
        Panel.AddEffectCommand.Execute(null);
        Session.Commit();

        var output = new HashSet<byte>();
        var preview = new HashSet<byte>();
        var frame = new byte[512];
        for (var i = 0; i < 60; i++)
        {
            _host.Tick();
            _host.Runtime.Preview.Tick();
            output.Add(_host.Frame()[0]);
            _host.Runtime.Preview.CopyLastFrame(1, frame);
            preview.Add(frame[0]);
        }

        output.Count.ShouldBe(1, "la sortie ne bouge pas en AVEUGLE");
        preview.ShouldContain((byte)0);
        preview.ShouldContain((byte)255);
    }

    [Fact]
    [Trait("Exigence", "EFF-007")]
    public void SaveAsTemplate_AddsToProjectLibrary_WithoutTargets()
    {
        AddInEdit("Vague douce");
        Panel.Period = 5;
        _host.Dialogs.TextAnswers.Enqueue("Ma vague lente");
        Panel.SaveAsTemplateCommand.Execute(null);

        var template = _host.Runtime.Project.Effects.Templates.Single(t => t.Name == "Ma vague lente");
        template.Effect.Targets.ShouldBeEmpty();
        template.Effect.Period.ShouldBe(Duration.FromSeconds(5));
        File.Exists(Path.Combine(_host.ProjectFolder, "effets.json")).ShouldBeTrue();
        Panel.Templates.ShouldContain(t => t.Template.Id == template.Id);
    }

    [Fact]
    [Trait("Exigence", "PAL-010")]
    public void Colors_SavedAsTheme_UsedByTheEffect()
    {
        AddInEdit("Alternance Latino");
        Panel.UsesColorList.ShouldBeTrue();
        Panel.ColorChips.Single(c => c.Name == "Rouge").IsChecked = true;
        Panel.ColorChips.Single(c => c.Name == "Bleu").IsChecked = true;
        Session.StepEffects[0].ThemeId.ShouldBeNull("cocher des couleurs remplace le thème");
        _host.Dialogs.TextAnswers.Enqueue("Rouge et bleu");
        Panel.SaveColorsAsThemeCommand.Execute(null);

        var theme = _host.Runtime.Project.Palettes.Palettes.Single(p => p.Name == "Rouge et bleu");
        theme.Kind.ShouldBe(PaletteKind.Theme);
        theme.Colors.Count.ShouldBe(2);
        Session.StepEffects[0].ThemeId.ShouldBe(theme.Id);
    }

    [Fact]
    [Trait("Exigence", "EFF-003")]
    [Trait("Exigence", "ERG-029")]
    public void PositionShape_ShowsPlane_AndDegrees()
    {
        var scene = Scene("Plein feu");
        Session.ChooseScene(scene.Id);
        Session.SetMode(EditMode.Edit).ShouldBeNull();
        Session.Select([Fixture("Lyre 1"), Fixture("Lyre 2")]);
        Panel.SelectedTemplate = Panel.Templates.Single(t => t.Template.Name == "Cercles opposés");
        Panel.AddEffectCommand.Execute(null);

        Panel.IsPositionShape.ShouldBeTrue();
        Panel.SizeLabel.ShouldBe("Taille (°)");
        Panel.Size.ShouldBe(60);
        Panel.Curve!.Count.ShouldBeGreaterThan(100);
        _vm.Refresh();
        Panel.Dots!.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Exigence", "EFF-001")]
    [Trait("Exigence", "ERG-029")]
    public void DefaultLayout_HasEffectsPanel_NextToSettings()
    {
        var factory = new ControlDockFactory(ControlPanels.All.ToDictionary(p => p.Id, p => (Func<object?>)(() => null)));
        var layout = factory.CreateLayout();
        DockTree.Find(layout, ControlPanels.Effects).Place.ShouldBe(PanelPlace.Visible);
        DockTree.FindOwner(layout, ControlPanels.Effects)!.Id.ShouldBe(DockTree.FindOwner(layout, ControlPanels.Settings)!.Id);
    }

    [Fact]
    [Trait("Exigence", "MOT-054")]
    public void StepHueFade_WrittenFromProperties()
    {
        var scene = Scene("Chenillard 4 couleurs");
        Session.ChooseScene(scene.Id);
        _vm.Properties.StepHueFade.ShouldBeFalse();
        _vm.Properties.StepHueFade = true;
        Session.Commit();
        Stored(scene.Id).Steps[0].HueFade.ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "SCN-014")]
    public void Wizard_ColorChase_ReplacesSteps_Undoable()
    {
        var scene = Scene("Plein feu");
        Session.ChooseScene(scene.Id);
        SelectPars();
        var properties = _vm.Properties;
        properties.Wizard = PropertiesPanelViewModel.Wizards.Single(w => w.Value == "chase");
        foreach (var name in ChaseColors)
        {
            properties.WizardPalettes.Single(p => p.Name == name).IsChecked = true;
        }

        properties.GenerateStepsCommand.Execute(null);
        Stored(scene.Id).Steps.Count.ShouldBe(3);
        Stored(scene.Id).Steps[0].Values.First(v => v.PaletteId is not null).Target.FixtureId.ShouldBe(Pars()[0]);
        Session.Undo();
        Stored(scene.Id).Steps.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(50, 0, 100, 0.5)]
    [InlineData(-5, 0, 100, 0)]
    [InlineData(4, 1, 1, 0)]
    [Trait("Exigence", "EFF-001")]
    [Trait("Exigence", "ERG-028")]
    public void Dial_Fraction(double value, double minimum, double maximum, double expected) =>
        Dial.Fraction(value, minimum, maximum).ShouldBe(expected, 1e-9);

    private void AddInEdit(string template)
    {
        Session.ChooseScene(Scene("Plein feu").Id);
        Session.SetMode(EditMode.Edit).ShouldBeNull();
        SelectPars();
        Panel.SelectedTemplate = Panel.Templates.Single(t => t.Template.Name == template);
        Panel.AddEffectCommand.Execute(null);
        Session.Commit();
    }

    // Sélection dans le désordre : l'ordre des membres vient du plan (de gauche à droite).
    private void SelectPars() => Session.Select([.. Enumerable.Reverse(Pars()).Select(id => id!.Value)]);

    private List<Guid?> Pars() => [.. ParNames.Select(n => (Guid?)Fixture(n))];

    private Guid Fixture(string name) => _host.Runtime.Project.Installation.Fixtures.Single(f => f.Name == name).Id;

    private Scene Scene(string name) => _host.Runtime.Project.Scenes.Scenes.Single(s => s.Name == name);

    private Scene Stored(Guid id) => _host.Runtime.Project.Scenes.Scenes.Single(s => s.Id == id);
}
