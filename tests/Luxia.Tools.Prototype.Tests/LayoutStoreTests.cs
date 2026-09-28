using Dock.Model.Controls;
using Dock.Model.Core;
using Luxia.Tools.Prototype.Docking;

namespace Luxia.Tools.Prototype.Tests;

public sealed class LayoutStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "luxia-prototype-tests", Guid.NewGuid().ToString("N"));
    private readonly LayoutStore _store;

    public LayoutStoreTests() => _store = new LayoutStore(_folder);

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Theory]
    [Trait("Exigence", "ERG-002")]
    [InlineData(false)]
    [InlineData(true)]
    public void SaveThenLoad_DefaultLayout_KeepsEveryPanelAndProportion(bool show)
    {
        var preset = show ? LayoutPreset.Show : LayoutPreset.Control;
        var factory = new PrototypeDockFactory();
        var layout = factory.CreateLayout(preset);
        factory.InitLayout(layout);

        _store.Save(preset, layout);
        var loaded = _store.Load(preset, out var message);

        message.ShouldBeNull();
        loaded.ShouldNotBeNull();
        Panels(loaded).ShouldBe(Panels(layout));
        Proportions(loaded).ShouldBe(Proportions(layout));
        _store.Serialize(loaded).ShouldBe(_store.Serialize(layout));
    }

    [Fact]
    [Trait("Exigence", "ERG-002")]
    public void SaveThenLoad_ClosedPanel_StaysHiddenAndCanBeRestored()
    {
        var factory = new PrototypeDockFactory();
        var layout = factory.CreateLayout(LayoutPreset.Control);
        factory.InitLayout(layout);
        var (journal, _) = DockTree.Find(layout, PanelCatalog.Log);
        factory.CloseDockable(journal!);
        DockTree.Find(layout, PanelCatalog.Log).Place.ShouldBe(PanelPlace.Hidden);

        _store.Save(LayoutPreset.Control, layout);
        var loaded = _store.Load(LayoutPreset.Control, out _)!;
        var reloadedFactory = new PrototypeDockFactory();
        reloadedFactory.InitLayout(loaded);

        DockTree.Find(loaded, PanelCatalog.Log).Place.ShouldBe(PanelPlace.Hidden);
        reloadedFactory.ShowPanel(loaded, LayoutPreset.Control, PanelCatalog.Log).ShouldNotBeNull();
        DockTree.Find(loaded, PanelCatalog.Log).Place.ShouldBe(PanelPlace.Visible);

        // Revenu dans son groupe d'origine (avec Position et Couleur), pas n'importe où.
        DockTree.FindOwner(loaded, PanelCatalog.Log)!.Id.ShouldBe(DockTree.FindOwner(loaded, PanelCatalog.Position)!.Id);
    }

    [Fact]
    [Trait("Exigence", "ERG-001")]
    public void ShowPanel_AbsentFromLayout_IsCreatedInItsHomeGroup()
    {
        var factory = new PrototypeDockFactory();
        var layout = factory.CreateLayout(LayoutPreset.Show);
        factory.InitLayout(layout);
        DockTree.Find(layout, PanelCatalog.Color).Place.ShouldBe(PanelPlace.Absent);

        factory.ShowPanel(layout, LayoutPreset.Show, PanelCatalog.Color).ShouldNotBeNull();

        DockTree.Find(layout, PanelCatalog.Color).Place.ShouldBe(PanelPlace.Visible);
    }

    [Fact]
    [Trait("Exigence", "ERG-002")]
    public void SaveThenLoad_ChangedProportion_IsKept()
    {
        var factory = new PrototypeDockFactory();
        var layout = factory.CreateLayout(LayoutPreset.Control);
        factory.InitLayout(layout);
        var tools = DockTree.FirstToolDock(layout)!;
        tools.Proportion = 0.42;

        _store.Save(LayoutPreset.Control, layout);
        var loaded = _store.Load(LayoutPreset.Control, out _)!;

        DockTree.FirstToolDock(loaded)!.Proportion.ShouldBe(0.42);
    }

    [Fact]
    [Trait("Exigence", "ERG-002")]
    public void Load_MissingFile_GivesNothingAndNoMessage()
    {
        _store.Load(LayoutPreset.Show, out var message).ShouldBeNull();
        message.ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "ERG-002")]
    public void Load_UnreadableFile_IsSetAsideWithMessage()
    {
        Directory.CreateDirectory(_folder);
        var path = _store.PathFor(LayoutPreset.Control);
        File.WriteAllText(path, "{ ceci n'est pas une disposition");

        _store.Load(LayoutPreset.Control, out var message).ShouldBeNull();

        message.ShouldNotBeNull();
        message.ShouldContain("illisible");
        File.Exists(path).ShouldBeFalse();
        Directory.GetFiles(_folder, "*.illisible-*").Length.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "ERG-002")]
    public void Load_ValidEnvelopeWithBrokenDock_GivesMessageInsteadOfThrowing()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(_store.PathFor(LayoutPreset.Control), """{ "formatVersion": 1, "preset": "Control", "dock": { "$type": "Inconnu" } }""");

        _store.Load(LayoutPreset.Control, out var message).ShouldBeNull();

        message.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Exigence", "ERG-002")]
    public void Delete_ThenLoad_GivesNothing()
    {
        var factory = new PrototypeDockFactory();
        _store.Save(LayoutPreset.Control, factory.CreateLayout(LayoutPreset.Control));

        _store.Delete(LayoutPreset.Control);

        _store.Load(LayoutPreset.Control, out _).ShouldBeNull();
    }

    private static List<string> Panels(IDock dock)
    {
        var ids = new List<string>();
        foreach (var child in dock.VisibleDockables ?? [])
        {
            if (child is IDock inner)
            {
                ids.AddRange(Panels(inner));
            }
            else if (child is not IProportionalDockSplitter)
            {
                ids.Add(child.Id);
            }
        }

        return ids;
    }

    private static List<double> Proportions(IDock dock)
    {
        var values = new List<double> { dock.Proportion };
        foreach (var child in dock.VisibleDockables ?? [])
        {
            if (child is IDock inner)
            {
                values.AddRange(Proportions(inner));
            }
        }

        return values;
    }
}
