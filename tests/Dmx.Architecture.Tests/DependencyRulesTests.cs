using System.Reflection;

namespace Dmx.Architecture.Tests;

/// <summary>
/// Vérifie les règles de dépendance entre projets (doc 03 §3.1, GEN-001, GEN-003).
/// On lit les références réellement compilées dans chaque assemblage.
/// </summary>
public sealed class DependencyRulesTests
{
    private static readonly Dictionary<string, string[]> Allowed = new()
    {
        ["Dmx.Core"] = [],
        ["Dmx.Messaging"] = ["Dmx.Core"],
        ["Dmx.Engine"] = ["Dmx.Core", "Dmx.Messaging"],
        ["Dmx.Output"] = ["Dmx.Core", "Dmx.Messaging"],
        ["Dmx.Persistence"] = ["Dmx.Core"],
        ["Dmx.Fixtures"] = ["Dmx.Core", "Dmx.Persistence"],
        ["Dmx.Patch"] = ["Dmx.Core", "Dmx.Persistence", "Dmx.Fixtures"],
        ["Dmx.Hosting"] = ["Dmx.Core", "Dmx.Messaging", "Dmx.Engine", "Dmx.Output", "Dmx.Persistence", "Dmx.Fixtures", "Dmx.Patch"],
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
            .Where(n => n.StartsWith("Dmx.", StringComparison.Ordinal));

        dmxReferences.ShouldBeSubsetOf(Allowed[project]);
    }

    [Theory]
    [InlineData("Dmx.UI.Controls")]
    [InlineData("Dmx.UI.Modules.Console")]
    [InlineData("Dmx.UI.Modules.Outputs")]
    [InlineData("Dmx.UI.Modules.Library")]
    [InlineData("Dmx.UI.Modules.Installation")]
    [InlineData("Dmx.UI.Modules.Simulator")]
    [Trait("Exigence", "GEN-003")]
    public void UserInterfaceModules_DoNotReferenceApplication(string project)
    {
        var assembly = Assembly.Load(project);

        assembly.GetReferencedAssemblies().Select(a => a.Name!).ShouldNotContain("DMX");
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
