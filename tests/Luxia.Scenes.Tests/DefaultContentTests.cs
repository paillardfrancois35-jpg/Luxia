using Luxia.Engine.Model;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Tests;

/// <summary>Contenu d'un nouveau projet (MOT-042, COU-006).</summary>
public sealed class DefaultContentTests
{
    [Fact]
    [Trait("Exigence", "MOT-042")]
    public void NewProject_HasFullOnScene_InIntensityLayer_LightingEveryFixture()
    {
        var folder = Path.Combine(Path.GetTempPath(), "luxia-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var (scenes, message) = SceneStore.Load(folder);
            message.ShouldBeNull();
            var full = scenes.Scenes.ShouldHaveSingleItem();
            full.Name.ShouldBe("Plein feu");
            full.LayerId.ShouldBe(LayerSet.IntensityLayerId);

            var project = new ReferenceProject();
            var model = ShowCompiler.Compile(project.Content(scenes)).Model;
            var values = model.Scenes.Single().Steps.Single().Values;
            var intensities = model.Parameters.Select((p, i) => (p, i)).Where(x => x.p.Role == ParameterRole.Intensity && !x.p.Absent).Select(x => x.i);
            values.Where(v => v.Value == 1).Select(v => v.Parameter).ShouldBe(intensities, ignoreOrder: true);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
