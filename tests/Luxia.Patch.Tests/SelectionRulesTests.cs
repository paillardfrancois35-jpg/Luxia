using Luxia.Patch.Model;
using Luxia.Patch.Rules;

namespace Luxia.Patch.Tests;

public sealed class SelectionRulesTests
{
    private static readonly SelectionItem A = new(Guid.NewGuid());
    private static readonly SelectionItem B = new(Guid.NewGuid());
    private static readonly SelectionItem C = new(Guid.NewGuid());
    private static readonly SelectionItem D = new(Guid.NewGuid());

    [Fact]
    [Trait("Exigence", "INST-033")]
    public void Reverse_ReversesOrder() => SelectionRules.Reverse([A, B, C]).ShouldBe([C, B, A]);

    [Fact]
    [Trait("Exigence", "INST-033")]
    public void OddAndEven_SplitByRank()
    {
        SelectionRules.Odd([A, B, C, D]).ShouldBe([A, C]);
        SelectionRules.Even([A, B, C, D]).ShouldBe([B, D]);
    }

    [Fact]
    [Trait("Exigence", "INST-033")]
    public void FirstAndSecondHalf_SplitInTheMiddle()
    {
        SelectionRules.FirstHalf([A, B, C, D]).ShouldBe([A, B]);
        SelectionRules.SecondHalf([A, B, C, D]).ShouldBe([C, D]);
    }

    [Fact]
    [Trait("Exigence", "INST-033")]
    public void OrderByPosition_LeftToRight_SortsByX()
    {
        var venue = new Venue
        {
            Name = "Salle",
            Placements =
            [
                new FixturePlacement { FixtureId = A.FixtureId, X = 5, Y = 0 },
                new FixturePlacement { FixtureId = B.FixtureId, X = 1, Y = 0 },
            ],
        };

        var ordered = SelectionRules.OrderByPosition([A, B], venue, PositionOrder.LeftToRight);

        ordered.ShouldBe([B, A]);
    }

    [Fact]
    [Trait("Exigence", "INST-033")]
    public void OrderByPosition_UnplacedFixture_KeepsOriginalRankAtTheEnd()
    {
        var venue = new Venue { Name = "Salle", Placements = [new FixturePlacement { FixtureId = A.FixtureId, X = 1, Y = 0 }] };

        var ordered = SelectionRules.OrderByPosition([B, A], venue, PositionOrder.LeftToRight);

        ordered.ShouldBe([A, B]);
    }
}
