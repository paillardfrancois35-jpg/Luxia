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

        foreach (var panel in ControlPanels.All.Where(p => p.Id != ControlPanels.Pilot))
        {
            DockTree.Find(layout, panel.Id).Place.ShouldBe(PanelPlace.Visible, panel.Title);
        }

        DockTree.Find(layout, ControlPanels.Pilot).Place.ShouldBe(PanelPlace.Absent, "le pilote est dans la disposition Spectacle");

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

    [Fact]
    [Trait("Exigence", "ERG-024")]
    public void ShowPreset_BigColumnsPilotLooksJournal_SavedSeparately()
    {
        var factory = new ControlDockFactory(_contexts) { Preset = ControlLayoutPreset.Show };
        var layout = factory.CreateLayout();
        factory.InitLayout(layout);

        foreach (var id in new[] { ControlPanels.Columns, ControlPanels.Pilot, ControlPanels.Looks, ControlPanels.Journal })
        {
            DockTree.Find(layout, id).Place.ShouldBe(PanelPlace.Visible, id);
        }

        DockTree.Find(layout, ControlPanels.Settings).Place.ShouldBe(PanelPlace.Absent, "le Spectacle ne montre pas les réglages");
        factory.ShowPanel(layout, ControlPanels.Settings).ShouldNotBeNull("mais on peut les réafficher");
        new ControlLayoutStore(_folder, ControlLayoutPreset.Show).Path.ShouldNotBe(new ControlLayoutStore(_folder).Path);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Exigence", "ERG-001")]
    public void Floating_OrClosedInItsWindow_ComesBackHome_AndTheEmptyWindowGoes(bool closedInWindow)
    {
        var factory = new ControlDockFactory(_contexts) { HostWindowFactory = () => null };
        var layout = factory.CreateLayout();
        factory.InitLayout(layout);
        var plan = DockTree.Find(layout, ControlPanels.Plan).Dockable!;
        var home = DockTree.FindOwner(layout, ControlPanels.Plan)!.Id;

        // Fenêtre détachée telle que Dock l'enregistre : une racine, un groupe d'onglets, le panneau.
        // Comme Dock en détachant : le groupe devenu vide disparaît de la fenêtre principale (cause vue en 1.005.214).
        factory.RemoveDockable(plan, collapse: true);
        DockTree.FindDock(layout, home!).ShouldBeNull("le groupe d'origine a disparu");
        var tools = new Dock.Model.Mvvm.Controls.ToolDock { Id = "flottant", VisibleDockables = factory.CreateList<Dock.Model.Core.IDockable>(plan), ActiveDockable = plan };
        var windowRoot = factory.CreateRootDock();
        windowRoot.VisibleDockables = factory.CreateList<Dock.Model.Core.IDockable>(tools);
        var window = factory.CreateDockWindow();
        window.Layout = windowRoot;
        layout.Windows = factory.CreateList(window);
        factory.InitLayout(layout);
        if (closedInWindow)
        {
            factory.CloseDockable(plan);
            DockTree.Find(layout, ControlPanels.Plan).Place.ShouldBe(PanelPlace.Hidden);
        }
        else
        {
            DockTree.Find(layout, ControlPanels.Plan).Place.ShouldBe(PanelPlace.Floating);
        }

        factory.ShowPanel(layout, ControlPanels.Plan).ShouldNotBeNull();

        DockTree.Find(layout, ControlPanels.Plan).Place.ShouldBe(PanelPlace.Visible, "remis dans la fenêtre principale");
        DockTree.FindOwner(layout, ControlPanels.Plan)!.Id.ShouldBe(home, "à sa place livrée");
        var group = DockTree.FindOwner(layout, ControlPanels.Plan)!;
        var settings = DockTree.FindOwner(layout, ControlPanels.Settings)!;
        var split = DockTree.ParentOf(layout, group)!;
        split.ShouldBeSameAs(DockTree.ParentOf(layout, settings), "à côté des Réglages, comme livré");
        split.VisibleDockables!.IndexOf(group).ShouldBeLessThan(split.VisibleDockables.IndexOf(settings), "à leur gauche");
        layout.Windows.ShouldBeEmpty("la fenêtre vide est fermée");
    }

    private IRootDock NewLayout(out ControlDockFactory factory)
    {
        factory = new ControlDockFactory(_contexts);
        var layout = factory.CreateLayout();
        factory.InitLayout(layout);
        return layout;
    }
}
