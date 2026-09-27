using Luxia.Fixtures.Model;
using Luxia.Patch.Model;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;

namespace Luxia.Scenes.Tests;

/// <summary>Palettes de position par lieu (T-PAL-02 : PAL-004, PAL-008, INST-054).</summary>
public sealed class VenuePaletteTests
{
    private readonly ReferenceProject _project = new();

    [Fact]
    [Trait("Exigence", "PAL-004")]
    [Trait("Exigence", "PAL-008")]
    public void SameScene_TwoVenues_DifferentPositions_WithGenericFallbackSignalled()
    {
        var lyre1 = _project.Fixture("Lyre 1").Id;
        var lyre2 = _project.Fixture("Lyre 2").Id;
        var generic = _project.Venues.Active;
        var salon = generic with { Id = Guid.NewGuid(), Name = "Salon" };
        var palette = new Palette
        {
            Name = "Piste centre",
            Kind = PaletteKind.Position,
            Values =
            [
                Pan(lyre1, 0.5), Pan(lyre2, 0.5),
                Pan(lyre1, 0.2, salon.Id),
            ],
        };
        var scene = new Scene
        {
            Name = "Lyres au centre",
            LayerId = LayerSet.MovementsLayerId,
            Steps = [new SceneStep { Values = [new SceneValue { Target = ValueTarget.Fixture(lyre1), PaletteId = palette.Id }, new SceneValue { Target = ValueTarget.Fixture(lyre2), PaletteId = palette.Id }] }],
        };
        var palettes = new PaletteSet { Palettes = [palette] };
        var scenes = new SceneSet { Scenes = [scene] };

        var inGeneric = ShowCompiler.Compile(_project.Content(scenes, palettes));
        var inSalon = ShowCompiler.Compile(_project.Content(scenes, palettes) with
        {
            Venues = _project.Venues with { Venues = [generic, salon], ActiveVenueId = salon.Id },
        });

        PanOf(inGeneric, lyre1).ShouldBe(0.5, 1e-9);
        PanOf(inSalon, lyre1).ShouldBe(0.2, 1e-9, "position calibrée pour le Salon");
        PanOf(inSalon, lyre2).ShouldBe(0.5, 1e-9, "lyre 2 jamais calibrée au Salon : repli sur Générique");
        inSalon.Issues.ShouldContain(i => i.Message.Contains("Lyre 2") && i.Message.Contains("PAL-008"));
        inGeneric.Issues.ShouldNotContain(i => i.Message.Contains("PAL-008"));
    }

    [Fact]
    [Trait("Exigence", "PAL-004")]
    public void Merge_ReplacesOnlyTheCapturedFixtures_InTheActiveVenue_AndAddsAFallback()
    {
        var lyre1 = Guid.NewGuid();
        var lyre2 = Guid.NewGuid();
        var salon = Guid.NewGuid();
        var palette = new Palette { Name = "Boule", Kind = PaletteKind.Position, Values = [Pan(lyre2, 0.7)] };

        var merged = VenuePalettes.Merge(palette, [Pan(lyre1, 0.3)], salon);

        merged.Values.ShouldContain(v => v.FixtureId == lyre2 && v.VenueId == null && v.Level == 0.7, "lyre 2 gardée");
        merged.Values.ShouldContain(v => v.FixtureId == lyre1 && v.VenueId == salon && v.Level == 0.3);
        merged.Values.ShouldContain(v => v.FixtureId == lyre1 && v.VenueId == null && v.Level == 0.3, "repli créé pour les autres lieux");
    }

    [Fact]
    [Trait("Exigence", "INST-054")]
    public void CopyVenue_CopiesTheEffectivePositions()
    {
        var lyre = Guid.NewGuid();
        var salon = Guid.NewGuid();
        var copy = Guid.NewGuid();
        var palettes = new PaletteSet
        {
            Palettes = [new Palette { Name = "Piste", Kind = PaletteKind.Position, Values = [Pan(lyre, 0.5), Pan(lyre, 0.1, salon)] }],
        };

        var result = VenuePalettes.CopyVenue(palettes, salon, copy);

        result.Palettes[0].Values.ShouldContain(v => v.FixtureId == lyre && v.VenueId == copy && v.Level == 0.1);
    }

    [Fact]
    [Trait("Exigence", "PAL-004")]
    public void GenericVenue_HasNoKey()
    {
        VenuePalettes.Key(new Venue { Name = "Générique" }).ShouldBeNull();
        var salon = new Venue { Name = "Salon" };
        VenuePalettes.Key(salon).ShouldBe(salon.Id);
    }

    private static PaletteValue Pan(Guid fixture, double level, Guid? venue = null) =>
        new() { FixtureId = fixture, Attribute = AttributeKind.Pan, Level = level, VenueId = venue };

    private static double PanOf(CompileResult result, Guid fixture)
    {
        var index = result.Model.IndexOf(fixture, "pan");
        return result.Model.Scenes[0].Steps[0].Values.Single(v => v.Parameter == index).Value;
    }
}
