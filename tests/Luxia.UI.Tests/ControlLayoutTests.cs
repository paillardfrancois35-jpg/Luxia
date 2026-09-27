using Dock.Model.Controls;
using Luxia.UI.Modules.Control.Docking;

namespace Luxia.UI.Tests;

/// <summary>Disposition des panneaux de l'écran Contrôle (ERG-001, ERG-002, C10).</summary>
public sealed class ControlLayoutTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "luxia-controle-tests", Guid.NewGuid().ToString("N"));
    private readonly Dictionary<string, Func<object?>> _contexts = ControlPanels.All.ToDictionary(p => p.Id, p => (Func<object?>)(() => p.Title));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    [Trait("Exigence", "ERG-001")]
    public void DefaultLayout_HasEveryPanelOnce()
    {
        var layout = NewLayout(out _);

        foreach (var panel in ControlPanels.All)
        {
            DockTree.Find(layout, panel.Id).Place.ShouldBe(PanelPlace.Visible, panel.Title);
        }

        DockTree.FindOwner(layout, ControlPanels.Journal)!.Id.ShouldBe(DockTree.FindOwner(layout, ControlPanels.Settings)!.Id, "journal en onglet avec les réglages");
    }

    [Fact]
    [Trait("Exigence", "ERG-002")]
    public void SaveThenLoad_ClosedPanel_StaysClosed_AndComesBackHome()
    {
        var layout = NewLayout(out var factory);
        factory.CloseDockable(DockTree.Find(layout, ControlPanels.Journal).Dockable!);
        var store = new ControlLayoutStore(_folder);

        store.Save(store.Serialize(layout));
        var loaded = store.Load(out var message)!;
        message.ShouldBeNull();
        var reloaded = new ControlDockFactory(_contexts);
        reloaded.InitLayout(loaded);

        DockTree.Find(loaded, ControlPanels.Journal).Place.ShouldBe(PanelPlace.Hidden);
        reloaded.ShowPanel(loaded, ControlPanels.Journal).ShouldNotBeNull();
        DockTree.FindOwner(loaded, ControlPanels.Journal)!.Id.ShouldBe(DockTree.FindOwner(loaded, ControlPanels.Settings)!.Id);
    }

    [Fact]
    [Trait("Exigence", "ERG-002")]
    public void Load_Missing_IsNullWithoutMessage_Unreadable_IsSetAside()
    {
        var store = new ControlLayoutStore(_folder);
        store.Load(out var none).ShouldBeNull();
        none.ShouldBeNull();

        Directory.CreateDirectory(_folder);
        File.WriteAllText(store.Path, "pas du json");
        store.Load(out var message).ShouldBeNull();
        message.ShouldNotBeNull();
        File.Exists(store.Path).ShouldBeFalse("le fichier illisible est mis de côté");
    }

    [Fact]
    [Trait("Exigence", "ERG-001")]
    public void InitLayout_GivesEachPanelItsContext()
    {
        var layout = NewLayout(out _);

        DockTree.Find(layout, ControlPanels.Plan).Dockable!.Context.ShouldBe("Plan des appareils");
    }

    private IRootDock NewLayout(out ControlDockFactory factory)
    {
        factory = new ControlDockFactory(_contexts);
        var layout = factory.CreateLayout();
        factory.InitLayout(layout);
        return layout;
    }
}
