using System.Reflection;

namespace Luxia.Architecture.Tests;

/// <summary>
/// Vérifie les règles de dépendance entre projets (doc 03 §3.1, GEN-001, GEN-003).
/// On lit les références réellement compilées dans chaque assemblage.
/// </summary>
public sealed class DependencyRulesTests
{
    private static readonly Dictionary<string, string[]> Allowed = new()
    {
        ["Luxia.Core"] = [],
        ["Luxia.Messaging"] = ["Luxia.Core"],
        ["Luxia.Engine"] = ["Luxia.Core", "Luxia.Messaging"],
        ["Luxia.Output"] = ["Luxia.Core", "Luxia.Messaging"],
        ["Luxia.Persistence"] = ["Luxia.Core"],
        ["Luxia.Fixtures"] = ["Luxia.Core", "Luxia.Persistence"],
        ["Luxia.Patch"] = ["Luxia.Core", "Luxia.Persistence", "Luxia.Fixtures"],
        ["Luxia.Scenes"] = ["Luxia.Core", "Luxia.Messaging", "Luxia.Engine", "Luxia.Persistence", "Luxia.Fixtures", "Luxia.Patch"],
        ["Luxia.Show"] = ["Luxia.Core", "Luxia.Messaging", "Luxia.Engine", "Luxia.Persistence", "Luxia.Fixtures", "Luxia.Scenes"],
        ["Luxia.Audio"] = ["Luxia.Core", "Luxia.Engine"],
        ["Luxia.Midi"] = ["Luxia.Core", "Luxia.Messaging", "Luxia.Engine", "Luxia.Persistence"],
        ["Luxia.Hosting"] = ["Luxia.Core", "Luxia.Messaging", "Luxia.Engine", "Luxia.Output", "Luxia.Persistence", "Luxia.Fixtures", "Luxia.Patch", "Luxia.Scenes", "Luxia.Show", "Luxia.Midi", "Luxia.Audio"],
    };

    public static TheoryData<string> Projects => [.. Allowed.Keys];

    [Theory]
    [MemberData(nameof(Projects))]
    [Trait("Exigence", "GEN-001")]
    public void Project_OnlyReferencesAllowedDmxProjects(string project)
    {
        var assembly = Assembly.Load(project);

        var dmxReferences = assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n.StartsWith("Luxia.", StringComparison.Ordinal));

        dmxReferences.ShouldBeSubsetOf(Allowed[project]);
    }

    [Theory]
    [InlineData("Luxia.UI.Controls")]
    [InlineData("Luxia.UI.Modules.Console")]
    [InlineData("Luxia.UI.Modules.Outputs")]
    [InlineData("Luxia.UI.Modules.Library")]
    [InlineData("Luxia.UI.Modules.Installation")]
    [InlineData("Luxia.UI.Modules.Simulator")]
    [InlineData("Luxia.UI.Modules.Scenes")]
    [InlineData("Luxia.UI.Modules.Live")]
    [InlineData("Luxia.UI.Modules.Control")]
    [InlineData("Luxia.UI.Modules.Audio")]
    [Trait("Exigence", "GEN-003")]
    public void UserInterfaceModules_DoNotReferenceApplication(string project)
    {
        var assembly = Assembly.Load(project);

        // L'exécutable s'appelle « LuXia » depuis le renommage (il s'appelait « DMX » : ce test ne vérifiait plus rien).
        var references = assembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();
        references.ShouldNotContain("LuXia");
        references.ShouldNotContain("Luxia.App");
    }

    [Theory]
    [MemberData(nameof(Projects))]
    [Trait("Exigence", "GEN-001")]
    public void Project_DoesNotReferenceUserInterface(string project)
    {
        var assembly = Assembly.Load(project);

        assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ShouldNotContain(n => n.StartsWith("Avalonia", StringComparison.Ordinal));
    }
}
