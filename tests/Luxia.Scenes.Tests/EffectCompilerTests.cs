using Luxia.Core.Dmx;
using Luxia.Core.Time;
using Luxia.Engine;
using Luxia.Engine.Model;
using Luxia.Fixtures.Model;
using Luxia.Messaging.Commands;
using Luxia.Patch.Rules;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;

namespace Luxia.Scenes.Tests;

/// <summary>Compilation des effets de scène sur le parc réel du show de référence (doc 16 §6, T-EFF-01).</summary>
public sealed class EffectCompilerTests
{
    private static readonly ValueTarget Pars = new() { Auto = new AutoSelectionTarget(AutoSelectionKind.ByModel, Model: "Betopper LPC008S") };
    private static readonly ValueTarget Bars = new() { Auto = new AutoSelectionTarget(AutoSelectionKind.ByModel, Model: "BeamZ LCB803") };
    private readonly ReferenceProject _project = new();

    private (ShowModel Model, IReadOnlyList<CompileIssue> Issues) Compile(SceneEffect effect, PaletteSet? palettes = null, params SceneValue[] values)
    {
        var scene = new Scene
        {
            Name = "Effet",
            LayerId = LayerSet.ColorsLayerId,
            Steps = [new SceneStep { Values = values, Effects = [effect] }],
        };
        var result = ShowCompiler.Compile(_project.Content(new SceneSet { Scenes = [scene] }, palettes));
        return (result.Model, result.Issues);
    }

    private static EngineEffect Only(ShowModel model) => model.Scenes.Single().Steps.Single().Effects.Single();

    [Theory]
    [InlineData(EffectPhaseMode.Linear, 360, new[] { 0, 0.25, 0.5, 0.75 })]
    [InlineData(EffectPhaseMode.Linear, 180, new[] { 0, 0.125, 0.25, 0.375 })]
    [InlineData(EffectPhaseMode.Mirror, 360, new[] { 0.5, 0, 0, 0.5 })]
    [InlineData(EffectPhaseMode.Groups, 360, new[] { 0, 0.5, 0, 0.5 })]
    [Trait("Exigence", "EFF-005")]
    public void Lag_PhaseModes(EffectPhaseMode mode, double spread, double[] expected)
    {
        var effect = new SceneEffect { PhaseMode = mode, Spread = spread };
        Enumerable.Range(0, 4).Select(i => EffectCompiler.Lag(effect, i, 4)).ShouldBe(expected, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "EFF-005")]
    public void Lag_Mirror_OddCount_CenterLeads()
    {
        var effect = new SceneEffect { PhaseMode = EffectPhaseMode.Mirror };
        Enumerable.Range(0, 5).Select(i => EffectCompiler.Lag(effect, i, 5)).ShouldBe([2.0 / 3, 1.0 / 3, 0, 1.0 / 3, 2.0 / 3], 1e-9);
    }

    [Fact]
    [Trait("Exigence", "EFF-005")]
    public void Lag_Random_SameEffect_SameOrder_AllDistinct()
    {
        var effect = new SceneEffect { PhaseMode = EffectPhaseMode.Random };
        var first = Enumerable.Range(0, 8).Select(i => EffectCompiler.Lag(effect, i, 8)).ToList();
        Enumerable.Range(0, 8).Select(i => EffectCompiler.Lag(effect, i, 8)).ShouldBe(first);
        first.Order().ShouldBe(Enumerable.Range(0, 8).Select(i => i / 8.0), 1e-9);
    }

    [Fact]
    [Trait("Exigence", "EFF-001")]
    [Trait("Exigence", "EFF-002")]
    public void Wave_OnFourPars_OneDimmerPerMember_InPatchOrder()
    {
        var (model, issues) = Compile(new SceneEffect { Name = "Vague", Targets = [Pars], Shape = SceneEffectShape.Sine });
        issues.ShouldBeEmpty();
        var effect = Only(model);
        effect.Shape.ShouldBe(EffectShape.Sine);
        effect.Channels.Count.ShouldBe(4);
        effect.Channels.Select(c => model.Parameters[c.Parameter].Label).ShouldBe(["PAR 1 – Intensité", "PAR 2 – Intensité", "PAR 3 – Intensité", "PAR 4 – Intensité"]);
        effect.Channels.Select(c => c.Lag).ShouldBe([0, 0.25, 0.5, 0.75]);
    }

    [Fact]
    [Trait("Exigence", "EFF-008")]
    public void PerCell_OnBars_EachSegmentIsAMember()
    {
        var bar = _project.Type("Barre 1");
        var mode = bar.Modes.Single(m => m.Name == _project.Fixture("Barre 1").ModeName);
        var cells = Fixtures.Rules.FixtureRules.ChannelsOf(bar, mode).Select(c => c.Cell).Where(c => c > 0).Distinct().Count();
        cells.ShouldBeGreaterThan(1);

        var (model, issues) = Compile(new SceneEffect { Targets = [Bars], PerCell = true, Shape = SceneEffectShape.Square, DutyCycle = 0.125 });
        issues.ShouldBeEmpty();
        var effect = Only(model);
        effect.Channels.Select(c => c.Member).Distinct().Count().ShouldBe(2 * cells);
        effect.Channels.Select(c => c.Lag).Distinct().Count().ShouldBe(2 * cells);
    }

    [Fact]
    [Trait("Exigence", "EFF-008")]
    [Trait("Exigence", "EFF-004")]
    public void MultiHead_64Channels_TwelveHeads_Rainbow()
    {
        var multi = _project.Fixture("Effet multi-têtes");
        multi.ModeName.ShouldBe("64 canaux");
        var (model, issues) = Compile(new SceneEffect { Targets = [ValueTarget.Fixture(multi.Id)], PerCell = true, Shape = SceneEffectShape.Rainbow });
        issues.ShouldBeEmpty();
        var effect = Only(model);
        effect.Channels.Select(c => c.Member).Distinct().Count().ShouldBe(12);

        // RVBW : 4 tables par tête (le blanc reste à 0 sur des teintes pures).
        effect.Channels.Count.ShouldBe(48);
        effect.Channels.ShouldAllBe(c => c.Table != null && c.Table.Count == 36);
    }

    [Fact]
    [Trait("Exigence", "EFF-003")]
    public void Circle_AroundPositionPalette_CenterFromPalette_SizeInDegrees()
    {
        var palettes = PaletteStore.Load(ReferenceProject.Folder).Value;
        var track = palettes.Palettes.Single(p => p.Name == "Piste centre");
        var lyre = _project.Fixture("Lyre 1");
        var (model, issues) = Compile(
            new SceneEffect { Targets = [ValueTarget.Fixture(lyre.Id)], Shape = SceneEffectShape.Circle, Size = 60, PositionPaletteId = track.Id, Relative = true },
            palettes);
        issues.ShouldBeEmpty();
        var effect = Only(model);

        // Autour d'une palette : absolu (le centre est la palette), même si « relatif » est coché.
        effect.Relative.ShouldBeFalse();
        effect.Channels.Count.ShouldBe(2);
        var pan = effect.Channels.Single(c => c.Axis == EffectAxis.X);
        var tilt = effect.Channels.Single(c => c.Axis == EffectAxis.Y);
        var expected = ValueResolver.ResolveMember(_project.Patch.Find(lyre.Id)!, 0, new SceneValue { Target = new() }, track, _project.Patch.VenueKey).ToDictionary(v => v.Key, v => v.Level);
        pan.Center.ShouldBe(expected[model.Parameters[pan.Parameter].ChannelKey], 1e-9);
        tilt.Center.ShouldBe(expected[model.Parameters[tilt.Parameter].ChannelKey], 1e-9);
        var physical = _project.Type("Lyre 1").Physical;
        pan.Size.ShouldBe(60 / (physical.PanRange ?? EffectCompiler.DefaultPanRange), 1e-9);
        tilt.Size.ShouldBe(60 / (physical.TiltRange ?? EffectCompiler.DefaultTiltRange), 1e-9);
    }

    [Fact]
    [Trait("Exigence", "EFF-004")]
    [Trait("Exigence", "PAL-010")]
    public void Alternate_FromTheme_SteppedTables()
    {
        var latino = DefaultPalettes.Themes.Single(t => t.Name == "Latino");
        var (model, issues) = Compile(new SceneEffect { Targets = [Pars], Shape = SceneEffectShape.Alternate, ThemeId = latino.Id });
        issues.ShouldBeEmpty();
        var effect = Only(model);
        effect.Stepped.ShouldBeTrue();
        effect.Shape.ShouldBe(EffectShape.Table);
        var red = effect.Channels.First(c => model.Parameters[c.Parameter].ChannelKey.Contains('r', StringComparison.Ordinal) && c.Member == 0);
        red.Table!.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Exigence", "EFF-004")]
    public void Gradient_FromPaletteColors()
    {
        var red = DefaultPalettes.Colors.Single(p => p.Name == "Rouge");
        var blue = DefaultPalettes.Colors.Single(p => p.Name == "Bleu");
        var (model, issues) = Compile(new SceneEffect { Targets = [Pars], Shape = SceneEffectShape.Gradient, Colors = [EffectColor.Palette(red.Id), EffectColor.Palette(blue.Id)] });
        issues.ShouldBeEmpty();
        var effect = Only(model);
        effect.Stepped.ShouldBeFalse();
        effect.Channels.Where(c => c.Member == 0).Select(c => c.Table!.Count).ShouldAllBe(n => n == 2);
    }

    [Fact]
    [Trait("Exigence", "EFF-001")]
    public void Problems_Reported_EffectDropped()
    {
        var (model, issues) = Compile(new SceneEffect { Name = "Cercle", Targets = [Pars], Shape = SceneEffectShape.Circle });
        issues.ShouldContain(i => i.Message.Contains("Pan", StringComparison.Ordinal) && i.Item.Contains("Cercle", StringComparison.Ordinal));
        model.Scenes.Single().Steps.Single().Effects.ShouldBeEmpty();

        (_, issues) = Compile(new SceneEffect { Targets = [Pars], Shape = SceneEffectShape.Alternate, ThemeId = Guid.NewGuid() });
        issues.ShouldContain(i => i.Message.Contains("thème", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Exigence", "MOT-054")]
    public void ColorGroups_OnePerRgbCell()
    {
        var (model, _) = Compile(new SceneEffect { Targets = [Pars] });
        model.ColorGroups.Count.ShouldBeGreaterThanOrEqualTo(4 + 2 + 8);
        model.ColorGroups.ShouldContain(g => model.Parameters[g.Red].Label == "PAR 1 – Rouge");
    }

    [Fact]
    [Trait("Exigence", "EFF-004")]
    [Trait("Exigence", "MOT-060")]
    public void Rainbow_PlayedByEngine_ColorChangesOverCycle()
    {
        var (model, _) = Compile(
            new SceneEffect { Targets = [Pars], Shape = SceneEffectShape.Rainbow, Period = Duration.FromSeconds(3), Spread = 0 },
            null,
            new SceneValue { Target = Pars, Attribute = AttributeKind.Intensity, Level = 1 });
        var sink = new FrameSink();
        var clock = new VirtualClock();
        var engine = new RenderEngine(sink, clock, seed: 1);
        engine.LoadShow(model);
        engine.Send(new LaunchSceneCommand(CommandOrigin.Tool, model.Scenes[0].Id));
        engine.Tick();

        // PAR 1 à l'adresse 1 : gradateur, rouge, vert, bleu. Départ au rouge, puis vert au tiers, bleu aux deux tiers.
        sink.Frame[0].ShouldBe((byte)255);
        sink.Frame[1].ShouldBe((byte)255);
        sink.Frame[2].ShouldBe((byte)0);
        for (var i = 0; i < 40; i++)
        {
            clock.Advance(TimeSpan.FromMilliseconds(25));
            engine.Tick();
        }

        sink.Frame[1].ShouldBe((byte)0);
        sink.Frame[2].ShouldBe((byte)255);
        sink.Frame[3].ShouldBe((byte)0);

        // PAR 4 (adresse 22) identique : décalage nul.
        sink.Frame[22].ShouldBe(sink.Frame[1]);
        sink.Frame[23].ShouldBe(sink.Frame[2]);
    }

    [Fact]
    [Trait("Exigence", "EFF-007")]
    public void Library_Apply_CopiesWithNewIdAndTarget()
    {
        var template = DefaultEffects.Templates.First(t => t.Name == "Vague douce");
        var applied = DefaultEffects.Apply(template, [Pars]);
        applied.Id.ShouldNotBe(template.Effect.Id);
        applied.Name.ShouldBe("Vague douce");
        applied.Targets.ShouldBe([Pars]);
        applied.Shape.ShouldBe(template.Effect.Shape);

        // Tous les modèles livrés se compilent sans problème sur le parc (les mouvements sur les lyres).
        var lyres = new ValueTarget { Auto = new AutoSelectionTarget(AutoSelectionKind.ByCategory, Category: FixtureCategory.MovingHead) };
        foreach (var t in DefaultEffects.Templates)
        {
            var (_, issues) = Compile(DefaultEffects.Apply(t, [t.Effect.IsPosition ? lyres : Pars]));
            issues.ShouldBeEmpty(t.Name);
        }
    }

    [Fact]
    [Trait("Exigence", "PAL-010")]
    public void Themes_AddedToProjectsWithoutAny_NotReAddedWhenOneExists()
    {
        var old = new PaletteSet { Palettes = [.. DefaultPalettes.Colors] };
        var upgraded = DefaultPalettes.WithThemes(old);
        upgraded.Palettes.Count(p => p.Kind == PaletteKind.Theme).ShouldBe(DefaultPalettes.Themes.Count);

        var trimmed = upgraded with { Palettes = [.. upgraded.Palettes.Where(p => p.Kind != PaletteKind.Theme || p.Name == "Froid")] };
        DefaultPalettes.WithThemes(trimmed).Palettes.Count(p => p.Kind == PaletteKind.Theme).ShouldBe(1);
    }

    private sealed class FrameSink : IFrameSink
    {
        public byte[] Frame { get; } = new byte[512];

        public void Submit(int universe, DmxFrame frame, TimeSpan timestamp) => frame.ReadOnlyValues.CopyTo(Frame);
    }
}
