using Luxia.Patch.Model;
using Luxia.Patch.Rules;

namespace Luxia.Patch.Tests;

/// <summary>ERG-036 : modifications de l'arbre des groupes (un appareil dans un seul groupe, pas de boucle).</summary>
public sealed class GroupEditsTests
{
    private static readonly Guid F1 = Guid.NewGuid();
    private static readonly Guid F2 = Guid.NewGuid();

    private static FixtureGroupSet Sample(out FixtureGroup parc, out FixtureGroup face, out FixtureGroup uv)
    {
        var set = new FixtureGroupSet();
        (set, parc) = GroupEdits.Add(set, "Parc");
        (set, face) = GroupEdits.Add(set, " Face ", parc.Id);
        (set, uv) = GroupEdits.Add(set, "UV");
        return GroupEdits.Assign(GroupEdits.Assign(set, F1, face.Id), F2, uv.Id);
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public void Add_TrimsTheName_IgnoresAnUnknownParent_AndKeepsFileOrder()
    {
        var (set, group) = GroupEdits.Add(new FixtureGroupSet(), "  Lyres ", Guid.NewGuid());

        group.Name.ShouldBe("Lyres");
        group.ParentId.ShouldBeNull();
        group.HasDimmer.ShouldBeTrue();
        set.Groups.ShouldHaveSingleItem();
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public void Assign_MovesTheFixtureOutOfItsPreviousGroup_AndNullMakesItUnassigned()
    {
        var set = Sample(out var parc, out var face, out var uv);

        set = GroupEdits.Assign(set, F1, uv.Id);
        set.Groups.Single(g => g.Id == face.Id).FixtureIds.ShouldBeEmpty();
        set.Groups.Single(g => g.Id == uv.Id).FixtureIds.ShouldBe([F2, F1]);

        set = GroupEdits.Assign(set, F1, null);
        set.Groups.SelectMany(g => g.FixtureIds).ShouldBe([F2]);
        set.Groups.Single(g => g.Id == parc.Id).FixtureIds.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public void Move_RefusesALoop_AcceptsARegroupingUnderAnotherParent()
    {
        var set = Sample(out var parc, out var face, out var uv);

        GroupEdits.Move(set, parc.Id, face.Id).ShouldBeNull();
        GroupEdits.Move(set, parc.Id, parc.Id).ShouldBeNull();
        GroupEdits.Move(set, uv.Id, Guid.NewGuid()).ShouldBeNull();
        GroupEdits.Move(set, uv.Id, face.Id)!.Groups.Single(g => g.Id == uv.Id).ParentId.ShouldBe(face.Id);
        GroupEdits.Move(set, face.Id, null)!.Groups.Single(g => g.Id == face.Id).ParentId.ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public void Delete_ChildrenGoUpToTheParent_AndFixturesFollow()
    {
        var set = Sample(out var parc, out var face, out var uv);

        var afterFace = GroupEdits.Delete(set, face.Id);
        afterFace.Groups.Select(g => g.Name).ShouldBe(["Parc", "UV"]);
        afterFace.Groups.Single(g => g.Id == parc.Id).FixtureIds.ShouldBe([F1]);

        var afterParc = GroupEdits.Delete(set, parc.Id);
        afterParc.Groups.Single(g => g.Id == face.Id).ParentId.ShouldBeNull();

        // Un groupe racine supprimé : ses appareils deviennent non assignés.
        GroupEdits.Delete(set, uv.Id).Groups.SelectMany(g => g.FixtureIds).ShouldBe([F1]);
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public void Shift_SwapsWithTheNextSibling_OnlyAmongSiblings()
    {
        var set = Sample(out var parc, out _, out var uv);

        var down = GroupEdits.Shift(set, parc.Id, +1);
        down.Groups.Select(g => g.Name).ShouldBe(["UV", "Face", "Parc"]);
        GroupEdits.Shift(set, parc.Id, -1).ShouldBe(set);
        GroupEdits.Shift(set, uv.Id, +1).ShouldBe(set);
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public void Prune_RemovesFixturesAbsentFromThePatch_RenameAndDimmerChangeOneGroup()
    {
        var set = Sample(out var parc, out _, out _);

        GroupEdits.Prune(set, [F1]).Groups.SelectMany(g => g.FixtureIds).ShouldBe([F1]);
        GroupEdits.Rename(set, parc.Id, " Grand parc ").Groups[0].Name.ShouldBe("Grand parc");
        GroupEdits.SetDimmer(set, parc.Id, false).Groups[0].HasDimmer.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0, "② 1")]
    [InlineData(7, "② 8")]
    [InlineData(8, "② p2·1")]
    [InlineData(17, "② p3·2")]
    [Trait("Exigence", "ERG-038")]
    public void FaderLabel_IsTheNumberOnTheFirstPage_ThenPageAndNumber(int index, string expected) =>
        GroupRules.FaderLabel(index).ShouldBe(expected);

    [Fact]
    [Trait("Exigence", "ERG-038")]
    public void PageFaderLabel_AlwaysNamesThePage() =>
        new[] { GroupRules.PageFaderLabel(0), GroupRules.PageFaderLabel(2), GroupRules.PageFaderLabel(9) }.ShouldBe(["② p1·1", "② p1·3", "② p2·2"]);
}
