using Luxia.Hosting;
using Luxia.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace Luxia.UI.Tests;

/// <summary>Versions du projet (GEN-054, GEN-055) et reprise après arrêt brutal (MOT-102, GEN-095, SC-11).</summary>
public sealed class ReliabilityTests : IAsyncLifetime
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
    [Trait("Exigence", "GEN-054")]
    [Trait("Exigence", "GEN-055")]
    public void Versions_OnlyWhenChanged_KeepTen()
    {
        var folder = _host.ProjectFolder;
        var t = new DateTime(2026, 9, 27, 21, 0, 0, DateTimeKind.Local);

        ProjectVersions.Save(folder, "automatique", t).ShouldNotBeNull();
        ProjectVersions.Save(folder, "automatique", t.AddMinutes(2)).ShouldBeNull("rien n'a changé");

        for (var i = 0; i < 12; i++)
        {
            File.WriteAllText(Path.Combine(folder, "notes.json"), $"{{ \"formatVersion\": 1, \"n\": {i} }}");
            ProjectVersions.Save(folder, "automatique", t.AddMinutes(4 + (2 * i))).ShouldNotBeNull();
        }

        var versions = ProjectVersions.List(folder);
        versions.Count.ShouldBe(ProjectVersions.Keep);
        versions[0].Reason.ShouldBe("automatique");
        Directory.EnumerateFiles(Path.Combine(folder, ProjectVersions.FolderName, versions[0].Name), "*.json", SearchOption.AllDirectories)
            .ShouldContain(f => f.EndsWith("scènes.json", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Exigence", "GEN-055")]
    public void Restore_BringsBackTheFiles_AndKeepsTheCurrentStateAsAVersion()
    {
        var folder = _host.ProjectFolder;
        var scenes = Path.Combine(folder, "scènes.json");
        var original = File.ReadAllText(scenes);
        var name = ProjectVersions.Save(folder, "automatique", new DateTime(2026, 9, 27, 21, 0, 0, DateTimeKind.Local))!;
        File.WriteAllText(scenes, original.Replace("Blanc chaud", "Blanc froid", StringComparison.Ordinal));

        ProjectVersions.Restore(folder, name, new DateTime(2026, 9, 27, 21, 5, 0, DateTimeKind.Local));

        File.ReadAllText(scenes).ShouldBe(original);
        ProjectVersions.List(folder)[0].Reason.ShouldStartWith("avant restauration");
    }

    [Fact]
    [Trait("Exigence", "MOT-102")]
    [Trait("Exigence", "GEN-095")]
    public async Task AbruptStop_ThenRestart_OffersToResumeTheSameScenes()
    {
        var scene = _host.Runtime.Project.Scenes.Scenes.First(s => s.Name == "Bleu sur tout le parc");
        _host.Runtime.LaunchScene(scene.Id);
        _host.Tick();
        _host.Runtime.SaveResumePoint();

        // « Arrêt brutal » : pas de DisposeAsync (pas de marque d'arrêt propre) ; une nouvelle session s'ouvre sur le même projet.
        await using var restarted = new LuxiaRuntime(_host.Paths, NullLoggerFactory.Instance, clock: _host.Clock);
        restarted.Project.Folder.ShouldBe(_host.ProjectFolder);
        var pending = restarted.PendingResume.ShouldNotBeNull();
        pending.Scenes.ShouldBe([scene.Id]);

        restarted.Resume(pending);
        restarted.Engine.Tick();
        restarted.Engine.Snapshot.Playbacks.ShouldContain(p => p.SceneId == scene.Id);
        restarted.PendingResume.ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "GEN-095")]
    public async Task CleanStop_OffersNothing()
    {
        var scene = _host.Runtime.Project.Scenes.Scenes[0];
        _host.Runtime.LaunchScene(scene.Id);
        _host.Tick();
        _host.Runtime.SaveResumePoint();
        await _host.Runtime.DisposeAsync();

        await using var restarted = new LuxiaRuntime(_host.Paths, NullLoggerFactory.Instance, clock: _host.Clock);
        restarted.PendingResume.ShouldBeNull();
    }
}
