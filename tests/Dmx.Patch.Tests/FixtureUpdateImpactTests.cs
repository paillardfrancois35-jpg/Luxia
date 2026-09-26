using Dmx.Fixtures;
using Dmx.Patch.Rules;

namespace Dmx.Patch.Tests;

public sealed class FixtureUpdateImpactTests
{
    [Fact]
    [Trait("Exigence", "INST-016")]
    public void ForModeChange_FromRichToSimpleMode_ReportsLostChannels()
    {
        // Doc 13 §3, INST-016 : passer un PAR de 7CH à 3CH → rapport « Strobe, Programme perdus ».
        var impact = FixtureUpdateImpact.ForModeChange(GenericFixtures.Rgbw, "5 canaux", "4 canaux");

        impact.LostChannels.ShouldBe(["Intensité"]);
        impact.GainedChannels.ShouldBeEmpty();
        impact.ModeRemoved.ShouldBeFalse();
        impact.Summary().ShouldContain("Intensité perdus");
    }

    [Fact]
    [Trait("Exigence", "INST-016")]
    public void ForModeChange_SameMode_IsEmpty()
    {
        var impact = FixtureUpdateImpact.ForModeChange(GenericFixtures.Rgb, "3 canaux", "3 canaux");

        impact.IsEmpty.ShouldBeTrue();
        impact.Summary().ShouldBe("Aucun impact.");
    }

    [Fact]
    [Trait("Exigence", "GEN-053")]
    public void ForLibraryUpdate_ModeRemovedInNewDefinition_IsFlagged()
    {
        var updated = GenericFixtures.Rgb with { Modes = [GenericFixtures.Rgb.Modes[1]] };

        var impact = FixtureUpdateImpact.ForLibraryUpdate(GenericFixtures.Rgb, updated, "3 canaux");

        impact.ModeRemoved.ShouldBeTrue();
        impact.Summary().ShouldContain("repatché");
    }
}
