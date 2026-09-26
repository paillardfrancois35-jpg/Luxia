using Dmx.Patch.Model;
using Dmx.Patch.Rules;

namespace Dmx.Patch.Tests;

public sealed class PatchRulesTests
{
    private static PatchedFixture Fixture(int address, int universe = 1, Guid? twinGroup = null, Guid? typeId = null, string mode = "7 canaux") =>
        new()
        {
            FixtureTypeId = typeId ?? Guid.NewGuid(),
            ModeName = mode,
            Universe = universe,
            Address = address,
            Name = "Appareil",
            TwinGroupId = twinGroup,
        };

    [Fact]
    [Trait("Exigence", "INST-012")]
    public void FindFreeAddress_SkipsOccupiedRanges()
    {
        var fixtures = new[] { Fixture(1), Fixture(8) };

        var free = PatchRules.FindFreeAddress(fixtures, 1, 7, _ => 7);

        // 1-7 et 8-14 occupés (7 canaux chacun) : la première libre est 15.
        free.ShouldBe(15);
    }

    [Fact]
    [Trait("Exigence", "INST-012")]
    public void FindFreeAddress_NoRoomLeft_ReturnsNull()
    {
        var fixtures = new[] { Fixture(1) };

        var free = PatchRules.FindFreeAddress(fixtures, 1, 1, _ => 512);

        free.ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "INST-013")]
    public void DetectOverlaps_FindsOverlappingRange()
    {
        var fixtures = new[] { Fixture(1), Fixture(5) };

        var overlaps = PatchRules.DetectOverlaps(fixtures, _ => 7);

        overlaps.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "INST-013")]
    public void DetectOverlaps_DifferentUniverses_NoOverlap()
    {
        var fixtures = new[] { Fixture(1, universe: 1), Fixture(1, universe: 2) };

        var overlaps = PatchRules.DetectOverlaps(fixtures, _ => 7);

        overlaps.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "INST-014")]
    public void DetectOverlaps_Twins_SameAddress_NoOverlap()
    {
        var group = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        var fixtures = new[]
        {
            Fixture(1, twinGroup: group, typeId: typeId),
            Fixture(1, twinGroup: group, typeId: typeId),
        };

        var overlaps = PatchRules.DetectOverlaps(fixtures, _ => 7);

        overlaps.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "INST-014")]
    public void DetectOverlaps_SameGroupButDifferentMode_StillOverlaps()
    {
        var group = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        var fixtures = new[]
        {
            Fixture(1, twinGroup: group, typeId: typeId, mode: "7 canaux"),
            Fixture(1, twinGroup: group, typeId: typeId, mode: "3 canaux"),
        };

        var overlaps = PatchRules.DetectOverlaps(fixtures, _ => 7);

        overlaps.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "INST-011")]
    public void PlanMultiple_FourParsSevenChannels_GivesReferenceShowAddresses()
    {
        // Plan d'adresses du show de référence (doc 41 §2) : 4 PAR 7CH depuis l'adresse 1 → 1, 8, 15, 22.
        var plan = PatchRules.PlanMultiple(1, 4, 7, "PAR");

        plan.Select(p => p.Address).ShouldBe([1, 8, 15, 22]);
        plan.Select(p => p.Name).ShouldBe(["PAR 1", "PAR 2", "PAR 3", "PAR 4"]);
        plan.Select(p => p.Number).ShouldBe([1, 2, 3, 4]);
    }

    [Fact]
    [Trait("Exigence", "INST-011")]
    public void PlanMultiple_WithGap_LeavesReserve()
    {
        var plan = PatchRules.PlanMultiple(1, 2, 7, "Barre", gap: 3);

        plan.Select(p => p.Address).ShouldBe([1, 11]);
    }
}
