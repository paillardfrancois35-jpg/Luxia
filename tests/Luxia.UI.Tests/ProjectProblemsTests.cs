namespace Luxia.UI.Tests;

/// <summary>Menu Projet → Problèmes du projet : mêmes règles que <c>luxia-headless valider</c> (essai P5, exemple 3).</summary>
public sealed class ProjectProblemsTests : IAsyncLifetime
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
    [Trait("Exigence", "COU-008")]
    public void ProjectProblems_ShowTheOutOfFamilyWarning()
    {
        _host.Runtime.ProjectProblems().ShouldContain(i => i.ToString().Contains("Lyres sur 3 positions", StringComparison.Ordinal) && i.ToString().Contains("COU-008", StringComparison.Ordinal));
    }
}
