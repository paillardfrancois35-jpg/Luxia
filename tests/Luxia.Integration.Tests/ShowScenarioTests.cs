using Luxia.Hosting.Tools;
using Luxia.Scenes.Model;
using Luxia.Show.Model;

namespace Luxia.Integration.Tests;

/// <summary>
/// GEN-132 et SHOW-027 en temps virtuel : un show et une séquence joués par le scénario de commandes, avec événements simulés,
/// sur les scènes du show de référence.
/// </summary>
public sealed class ShowScenarioTests
{
    private static readonly string Reference = Path.Combine(AppContext.BaseDirectory, "samples", "Show de référence");

    [Fact]
    [Trait("Exigence", "GEN-132")]
    [Trait("Exigence", "SHOW-027")]
    public void Scenario_PlaysAShowWithSimulatedDrop_AndSummarizesItsSteps()
    {
        var content = ProjectFiles.Load(Reference);
        var blue = content.Scenes.Scenes.First(s => s.LayerId == LayerSet.ColorsLayerId);
        var other = content.Scenes.Scenes.Last(s => s.LayerId == LayerSet.ColorsLayerId);
        var show = new ShowDefinition
        {
            Name = "Couplet / Refrain",
            Steps =
            [
                new ShowStep { Id = "1", Name = "Couplet", Initial = true, Actions = [new ShowAction { Kind = ShowActionKind.Play, SceneId = blue.Id }] },
                new ShowStep { Id = "3", Name = "Refrain", Actions = [new ShowAction { Kind = ShowActionKind.Play, SceneId = other.Id }] },
            ],
            Transitions = [new ShowTransition { From = ["1"], To = ["3"], Condition = new ShowCondition { Kind = ConditionKind.Drop }, Quantize = ShowQuantize.Bar }],
        };
        var shows = new ShowSet { Shows = [show] };
        const string text = """
            0    tempo 120
            0    show "Couplet / Refrain"
            2,5  simuler drop
            5    arreter-show
            """;

        var scenario = Scenario.Parse(text, content.Scenes, content.Layers, out var errors, new SequenceSet(), shows);
        errors.ShouldBeEmpty();
        var report = ScenarioRunner.Run(content, scenario, TimeSpan.FromSeconds(6), sequences: new SequenceSet(), shows: shows);

        report.Rejections.ShouldBeEmpty();
        report.Lines.ShouldContain(l => l.Contains("◆ show « Couplet / Refrain » : étape 1 « Couplet » (lancement)"));
        report.Lines.ShouldContain(l => l.Contains("4,00 s  ◆ show « Couplet / Refrain » : étape 3 « Refrain » (au drop)") || l.Contains("4.00 s  ◆ show « Couplet / Refrain » : étape 3 « Refrain » (au drop)"), "drop à 2,5 s, franchi à la mesure suivante (4 s à 120 BPM)");
        report.Lines.ShouldContain(l => l.Contains("◆ show « Couplet / Refrain » arrêté"));
    }

    [Fact]
    [Trait("Exigence", "GEN-132")]
    public void Scenario_ReportsUnknownShowsAndBadSimulations()
    {
        var content = ProjectFiles.Load(Reference);
        const string text = """
            0 show "Inconnu"
            1 simuler tonnerre
            2 forcer "Inconnu" 1
            3 energie 50
            4 style "Électro"
            """;

        Scenario.Parse(text, content.Scenes, content.Layers, out var errors, new SequenceSet(), new ShowSet());

        errors.Count.ShouldBe(3);
        errors[0].ShouldContain("show inconnu(e) « Inconnu »");
        errors[1].ShouldContain("simuler drop|break|montee|silence|reprise|morceau");
    }
}
