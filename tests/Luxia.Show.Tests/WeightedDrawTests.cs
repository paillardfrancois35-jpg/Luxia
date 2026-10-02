using Luxia.Messaging.Commands;
using Luxia.Show.Model;

namespace Luxia.Show.Tests;

/// <summary>T-SHOW-05 : tirage pondéré et anti-répétition (SHOW-028).</summary>
public sealed class WeightedDrawTests
{
    // Une étape « 0 » qui part tout de suite vers A (poids 60) ou B (poids 40) ; A et B reviennent au temps suivant (une boucle
    // immédiate serait refusée, SHOW-024) : un tirage par temps, à 400 BPM.
    private static (SequencerHarness Harness, ShowDefinition Show) Branches(bool avoidRepeat)
    {
        var h = new SequencerHarness(seed: 11);
        var always = new ShowCondition { Kind = ConditionKind.Always };
        var show = new ShowDefinition
        {
            Name = "Tirage",
            Steps = [new ShowStep { Id = "0", Initial = true, Choice = StepChoice.Random, AvoidRepeat = avoidRepeat }, new ShowStep { Id = "a" }, new ShowStep { Id = "b" }],
            Transitions =
            [
                new() { From = ["0"], To = ["a"], Condition = always, Weight = 60 },
                new() { From = ["0"], To = ["b"], Condition = always, Weight = 40 },
                new() { From = ["a"], To = ["0"], Condition = always, Quantize = ShowQuantize.Beat },
                new() { From = ["b"], To = ["0"], Condition = always, Quantize = ShowQuantize.Beat },
            ],
        };
        h.Shows.Add(show);
        h.Load(400);
        h.Send(new LaunchShowCommand(CommandOrigin.Tool, show.Id));
        h.Tick();
        return (h, show);
    }

    [Fact]
    [Trait("Exigence", "SHOW-028")]
    public void Distribution_FollowsTheWeights_Within2Percent()
    {
        var (h, _) = Branches(avoidRepeat: false);
        while (h.Steps.Count(s => s is "a" or "b") < 10_000)
        {
            for (var i = 0; i < 500; i++)
            {
                h.Tick();
            }
        }

        var draws = h.Steps.Where(s => s is "a" or "b").Take(10_000).ToList();
        var share = draws.Count(s => s == "a") / (double)draws.Count;
        share.ShouldBe(0.6, 0.02);
    }

    [Fact]
    [Trait("Exigence", "SHOW-028")]
    public void AvoidRepeat_NeverTakesTheSameBranchTwiceInARow()
    {
        var (h, _) = Branches(avoidRepeat: true);
        for (var i = 0; i < 2_000; i++)
        {
            h.Tick();
        }

        var draws = h.Steps.Where(s => s is "a" or "b").ToList();
        draws.Zip(draws.Skip(1)).ShouldAllBe(pair => pair.First != pair.Second);
    }
}
