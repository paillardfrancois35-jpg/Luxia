using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;

namespace Luxia.Scenes.Tests;

/// <summary>GEN-133 : un lot de scènes générées s'ajoute sans rien écraser.</summary>
public sealed class SceneImportTests
{
    [Fact]
    [Trait("Exigence", "GEN-133")]
    public void Merge_AddsNewScenes_NeverOverwrites_AndCategorizes()
    {
        var mine = new Scene { Name = "Blanc chaud", Category = "Couleurs" };
        var existing = new SceneSet { Scenes = [mine] };
        var incoming = new SceneSet
        {
            Scenes =
            [
                mine with { Name = "Blanc chaud réécrit par l'IA" },
                new Scene { Name = "Blanc chaud" },
                new Scene { Name = "Vague bleue", Category = "Mouvements" },
            ],
        };

        var result = SceneImport.Merge(existing, incoming);

        result.Imported.ShouldBe(2);
        result.Scenes.Scenes.Single(s => s.Id == mine.Id).ShouldBe(mine);
        result.Scenes.Scenes.Select(s => s.Name).ShouldBe(["Blanc chaud", "Blanc chaud (2)", "Vague bleue"]);
        result.Scenes.Scenes[1].Category.ShouldBe(SceneSet.AiCategory);
        result.Scenes.Scenes[2].Category.ShouldBe("Mouvements");
        result.Report[0].ShouldContain("écartée");
    }
}
