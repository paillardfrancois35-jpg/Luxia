using Luxia.Patch.Model;
using Luxia.Patch.Rules;

namespace Luxia.Patch.Tests;

/// <summary>ERG-036 : arbre des groupes d'appareils (ordre, fichier fautif, enregistrement).</summary>
public sealed class GroupRulesTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "luxia-groups-tests-" + Guid.NewGuid());

    public GroupRulesTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private static FixtureGroup G(string name, Guid? parent = null, params Guid[] fixtures) =>
        new() { Name = name, ParentId = parent, HasDimmer = true, FixtureIds = fixtures };

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public void Layout_ParentBeforeChildren_SiblingsKeepFileOrder_EvenWhenChildIsListedFirst()
    {
        var parc = G("Parc");
        var face = G("Face", parc.Id);
        var uv = G("UV");
        var barres = G("Barres", parc.Id);

        // Le fichier cite l'enfant avant son parent : l'ordre de l'arbre le remet d'aplomb.
        var layout = GroupRules.Layout(new FixtureGroupSet { Groups = [face, uv, parc, barres] });

        layout.Select(n => n.Group.Name).ShouldBe(["UV", "Parc", "Face", "Barres"]);
        layout.Select(n => n.Parent).ShouldBe([-1, -1, 1, 1]);
        layout.Select(n => n.Depth).ShouldBe([0, 0, 1, 1]);
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public void Layout_UnknownParent_OrSelfParent_OrLoop_GoesBackToTheRoot()
    {
        var lost = G("Perdu", Guid.NewGuid());
        var a = G("A");
        var b = G("B", a.Id);
        var loopA = a with { ParentId = b.Id };
        var self = G("Soi");
        self = self with { ParentId = self.Id };

        var layout = GroupRules.Layout(new FixtureGroupSet { Groups = [lost, loopA, b, self] });

        layout.Count.ShouldBe(4);
        layout.Single(n => n.Group.Name == "Perdu").Parent.ShouldBe(-1);
        layout.Single(n => n.Group.Name == "Soi").Parent.ShouldBe(-1);
        layout.Count(n => n.Parent < 0).ShouldBeGreaterThanOrEqualTo(3);
        foreach (var node in layout.Select((n, i) => (n, i)))
        {
            node.n.Parent.ShouldBeLessThan(node.i);
        }
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public void Problems_ReportUnknownParentLoopUnknownFixtureAndFixtureInTwoGroups()
    {
        var known = Guid.NewGuid();
        var a = G("A", null, known);
        var b = G("B", null, known, Guid.NewGuid());
        var lost = G("Perdu", Guid.NewGuid());
        var x = G("X");
        var y = G("Y", x.Id);
        var loopX = x with { ParentId = y.Id };

        var problems = GroupRules.Problems(new FixtureGroupSet { Groups = [a, b, lost, loopX, y] }, [known]);

        problems.ShouldContain(p => p.Item.Contains("Perdu") && p.Message.Contains("parent introuvable"));
        problems.ShouldContain(p => p.Item.Contains('X') && p.Message.Contains("boucle"));
        problems.ShouldContain(p => p.Item.Contains('B') && p.Message.Contains("introuvable dans le patch"));
        problems.ShouldContain(p => p.Item.Contains('B') && p.Message.Contains("déjà rangé"));
        GroupRules.Problems(new FixtureGroupSet { Groups = [a] }, [known]).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public void GroupOf_ReturnsTheFirstGroupHoldingTheFixture_OrNullWhenUnassigned()
    {
        var fixture = Guid.NewGuid();
        var set = new FixtureGroupSet { Groups = [G("A", null, fixture), G("B", null, fixture)] };

        GroupRules.GroupOf(set, fixture)!.Name.ShouldBe("A");
        GroupRules.GroupOf(set, Guid.NewGuid()).ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    [Trait("Exigence", "GEN-050")]
    public void GroupStore_Missing_ReturnsNoGroup_ThenSaveAndLoadRoundTrips()
    {
        var (empty, message) = GroupStore.Load(_folder);
        empty.Groups.ShouldBeEmpty();
        empty.UnassignedName.ShouldBe("Non assigné");
        message.ShouldBeNull();

        var parc = G("Parc", null, Guid.NewGuid());
        var set = new FixtureGroupSet { UnassignedName = "Divers", Groups = [parc, G("Face", parc.Id)] };
        GroupStore.Save(_folder, set);
        var (loaded, _) = GroupStore.Load(_folder);

        loaded.UnassignedName.ShouldBe("Divers");
        loaded.Groups.Select(g => g.Name).ShouldBe(["Parc", "Face"]);
        loaded.Groups[1].ParentId.ShouldBe(parc.Id);
        loaded.Groups[0].FixtureIds.ShouldBe(parc.FixtureIds);
        loaded.Groups[0].HasDimmer.ShouldBeTrue();
        File.ReadAllText(Path.Combine(_folder, GroupStore.FileName)).ShouldContain("\"formatVersion\"");
    }
}
