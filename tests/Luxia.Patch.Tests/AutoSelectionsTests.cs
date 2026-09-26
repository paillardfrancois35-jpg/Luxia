using Luxia.Fixtures;
using Luxia.Fixtures.Model;
using Luxia.Patch.Model;
using Luxia.Patch.Rules;

namespace Luxia.Patch.Tests;

public sealed class AutoSelectionsTests
{
    [Fact]
    [Trait("Exigence", "INST-031")]
    public void Build_AllFixtures_IsOrderedByAddress()
    {
        var par2 = New(GenericFixtures.Rgb, 8);
        var par1 = New(GenericFixtures.Rgb, 1);
        var fixtures = new[] { par2, par1 };

        var selections = AutoSelections.Build(fixtures, TypeOf);

        var all = selections.Single(s => s.Kind == AutoSelectionKind.AllFixtures);
        all.Items.ShouldBe([par1, par2]);
    }

    [Fact]
    [Trait("Exigence", "INST-031")]
    public void Build_GroupsByCategoryAndByModel()
    {
        var par = New(GenericFixtures.Rgb, 1);
        var dimmer = New(GenericFixtures.Dimmer, 20);
        var fixtures = new[] { par, dimmer };

        var selections = AutoSelections.Build(fixtures, TypeOf);

        selections.Count(s => s.Kind == AutoSelectionKind.ByCategory).ShouldBe(2);
        selections.Single(s => s.Kind == AutoSelectionKind.ByCategory && s.Category == FixtureCategory.Par).Items.ShouldBe([par]);
        selections.Single(s => s.Kind == AutoSelectionKind.ByModel && s.ModelDisplayName == GenericFixtures.Dimmer.DisplayName).Items.ShouldBe([dimmer]);
    }

    [Fact]
    [Trait("Exigence", "INST-031")]
    public void Build_NewFixtureAdded_AppearsWithoutAnyStoredState()
    {
        // « Elles se mettent à jour seules » (doc 13 §4) : rien à persister, il suffit de reconstruire.
        var before = AutoSelections.Build([New(GenericFixtures.Rgb, 1)], TypeOf);
        var after = AutoSelections.Build([New(GenericFixtures.Rgb, 1), New(GenericFixtures.Rgb, 8)], TypeOf);

        before.Single(s => s.Kind == AutoSelectionKind.AllFixtures).Items.Count.ShouldBe(1);
        after.Single(s => s.Kind == AutoSelectionKind.AllFixtures).Items.Count.ShouldBe(2);
    }

    private static PatchedFixture New(FixtureType type, int address) =>
        new() { FixtureTypeId = type.Id, ModeName = type.Modes[0].Name, Address = address, Name = type.Model };

    private static FixtureType? TypeOf(PatchedFixture fixture) =>
        GenericFixtures.All.FirstOrDefault(f => f.Id == fixture.FixtureTypeId);
}
