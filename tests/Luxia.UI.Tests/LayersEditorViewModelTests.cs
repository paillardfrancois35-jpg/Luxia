using Luxia.Engine.Model;
using Luxia.Scenes.Model;
using Luxia.UI.Modules.Scenes;

namespace Luxia.UI.Tests;

/// <summary>Éditeur de couches (COU-001) sur une copie du show de référence.</summary>
public sealed class LayersEditorViewModelTests : IAsyncLifetime
{
    private readonly TestHost _host = new();

    public ValueTask InitializeAsync()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "samples", "Show de référence");
        foreach (var file in Directory.EnumerateFiles(source, "*.json", SearchOption.AllDirectories))
        {
            var target = Path.Combine(_host.ProjectFolder, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }

        _host.Runtime.Project.Open(_host.ProjectFolder).ShouldBeTrue();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    [Trait("Exigence", "COU-001")]
    [Trait("Exigence", "COU-006")]
    public void Editor_ListsDefaultLayers_InPriorityOrder()
    {
        var editor = new LayersEditorViewModel(_host.Runtime, _host.Dialogs);

        editor.Layers.Select(l => l.Name).ShouldBe(["Intensité", "Couleurs", "Mouvements", "Faisceau", "Effets", "Ambiance", "Libre", "Flashs"]);
        editor.Layers.Single(l => l.Name == "Flashs").Kind.Value.ShouldBe(LayerKind.Flash);
        editor.Layers.Single(l => l.Name == "Ambiance").KeepOnStopAll.ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "COU-001")]
    public async Task AddRenameReorderSave_UpdatesPrioritiesAndFile()
    {
        var editor = new LayersEditorViewModel(_host.Runtime, _host.Dialogs);
        _host.Dialogs.TextAnswers.Enqueue("Lasers");
        await editor.AddCommand.ExecuteAsync(null);
        editor.MoveUpCommand.Execute(null);
        editor.Layers.Single(l => l.Name == "Couleurs").Name = "Couleurs fixes";
        editor.Layers.Single(l => l.Name == "Couleurs fixes").CrossFadeSeconds = 2;

        editor.SaveCommand.Execute(null);

        var layers = _host.Runtime.Project.Layers.Layers.OrderBy(l => l.Priority).ToList();
        layers.Select(l => l.Name).ShouldBe(["Intensité", "Couleurs fixes", "Mouvements", "Faisceau", "Effets", "Ambiance", "Libre", "Lasers", "Flashs"]);
        layers.Select(l => l.Priority).ShouldBe([1, 2, 3, 4, 5, 6, 7, 8, 9]);
        layers[1].Id.ShouldBe(LayerSet.ColorsLayerId, "l'identifiant ne change pas : les scènes gardent leur couche");
        layers[1].CrossFade.ShouldBe(Duration.FromSeconds(2));
        File.Exists(Path.Combine(_host.ProjectFolder, "couches.json")).ShouldBeTrue();
        editor.Saved.ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "COU-001")]
    [Trait("Exigence", "COU-002")]
    public async Task Delete_LayerWithScenes_IsRefused()
    {
        var editor = new LayersEditorViewModel(_host.Runtime, _host.Dialogs);
        editor.Selected = editor.Layers.Single(l => l.Name == "Couleurs");

        await editor.DeleteCommand.ExecuteAsync(null);

        editor.Layers.ShouldContain(l => l.Name == "Couleurs");
        _host.Dialogs.ShownInfo.ShouldHaveSingleItem().ShouldContain("Déplacez-les");
    }

    [Fact]
    [Trait("Exigence", "COU-009")]
    public void RestScene_ChoicesAreTheLayerScenes()
    {
        var editor = new LayersEditorViewModel(_host.Runtime, _host.Dialogs);
        var colors = editor.Layers.Single(l => l.Name == "Couleurs");

        colors.RestChoices[0].Label.ShouldBe("Aucune");
        colors.RestChoices.Skip(1).ShouldAllBe(c => _host.Runtime.Project.Scenes.Scenes.Single(s => s.Id == c.Value).LayerId == LayerSet.ColorsLayerId);
    }
}
