using Luxia.Engine.Model;
using Luxia.Messaging.Commands;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;

namespace Luxia.Scenes.Tests;

public sealed class LookRulesTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "luxia-looks-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    [Trait("Exigence", "ERG-023")]
    public void Commands_FollowTheActionsInOrder_IncompleteOnesAreSkipped()
    {
        var scene = Guid.NewGuid();
        var layer = Guid.NewGuid();
        var look = new Look
        {
            Name = "Temps mort",
            Actions =
            [
                new() { Kind = LookActionKind.StopAll },
                new() { Kind = LookActionKind.LaunchScene, SceneId = scene },
                new() { Kind = LookActionKind.LayerMaster, LayerId = layer, Level = 1.4 },
                new() { Kind = LookActionKind.GrandMaster, Level = 0.4 },
                new() { Kind = LookActionKind.StopScene },
                new() { Kind = LookActionKind.StopLayer, LayerId = layer },
            ],
        };

        var commands = LookRules.Commands(look, CommandOrigin.User);

        commands.Count.ShouldBe(5, "l'arrêt de scène sans scène est ignoré");
        commands[0].ShouldBe(new StopLayerCommand(CommandOrigin.User));
        commands[1].ShouldBe(new LaunchSceneCommand(CommandOrigin.User, scene));
        commands[2].ShouldBe(new SetLayerMasterCommand(CommandOrigin.User, layer, 1), "niveau borné à 100 %");
        commands[3].ShouldBe(new SetGrandMasterCommand(CommandOrigin.User, 0.4));
        commands[4].ShouldBe(new StopLayerCommand(CommandOrigin.User, layer));
    }

    [Fact]
    [Trait("Exigence", "ERG-023")]
    public void Describe_And_Problems_UseNames()
    {
        var scenes = new SceneSet { Scenes = [new Scene { Name = "Ambre", LayerId = LayerSet.ColorsLayerId }] };
        var layers = LayerSet.Default();
        var look = new Look
        {
            Name = "Calme",
            Actions =
            [
                new() { Kind = LookActionKind.LaunchScene, SceneId = scenes.Scenes[0].Id },
                new() { Kind = LookActionKind.LayerMaster, LayerId = LayerSet.ColorsLayerId, Level = 0.5 },
                new() { Kind = LookActionKind.LaunchScene, SceneId = Guid.NewGuid() },
            ],
        };

        LookRules.Describe(look.Actions[0], scenes, layers).ShouldBe("▶ lancer « Ambre »");
        LookRules.Describe(look.Actions[1], scenes, layers).ShouldBe("master Couleurs à 50 %");
        LookRules.Problems(look, scenes, layers).ShouldHaveSingleItem().ShouldContain("scène introuvable");
    }

    [Fact]
    [Trait("Exigence", "ERG-023")]
    public void Store_Missing_IsEmpty_SaveThenLoad_RoundTrips()
    {
        Directory.CreateDirectory(_folder);
        LookStore.Load(_folder).Value.Looks.ShouldBeEmpty();

        var set = new LookSet { Looks = [new Look { Name = "Temps mort", Color = "#8957E5", Actions = [new() { Kind = LookActionKind.StopAll }] }] };
        LookStore.Save(_folder, set);
        var (loaded, message) = LookStore.Load(_folder);

        message.ShouldBeNull();
        loaded.Looks.ShouldHaveSingleItem().Name.ShouldBe("Temps mort");
        loaded.Looks[0].Actions.ShouldHaveSingleItem().Kind.ShouldBe(LookActionKind.StopAll);
        File.ReadAllText(Path.Combine(_folder, LookStore.FileName)).ShouldContain("\"kind\": \"stopAll\"");
    }
}
