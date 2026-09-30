using Dock.Model.Controls;
using Luxia.UI.Modules.Control.Docking;

namespace Luxia.UI.Tests;

/// <summary>Disposition des panneaux de l'écran de jeu (ERG-001, ERG-002, ERG-032, C10).</summary>
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
    [Trait("Exigence", "ERG-032")]
    public void DefaultLayout_IsTheGameScreen_FiveBigPanels_NoEditingPanel()
    {
        var layout = NewLayout(out _);

        foreach (var panel in ControlPanels.Game)
        {
            DockTree.Find(layout, panel.Id).Place.ShouldBe(PanelPlace.Visible, panel.Title);
        }

        ControlPanels.Game.Select(p => p.Id).ShouldBe([ControlPanels.Columns, ControlPanels.Dimmers, ControlPanels.Looks, ControlPanels.Pilot, ControlPanels.Journal]);
        foreach (var id in new[] { ControlPanels.Properties, ControlPanels.Plan, ControlPanels.Settings, ControlPanels.Effects })
        {
            DockTree.Find(layout, id).Place.ShouldBe(PanelPlace.Absent, id + " : dans la fenêtre d'édition, plus sur l'écran de jeu");
        }

        DockTree.FindOwner(layout, ControlPanels.Dimmers)!.Id.ShouldNotBe(DockTree.FindOwner(layout, ControlPanels.Looks)!.Id, "dimmers et looks : deux groupes, l'un au-dessus de l'autre");
    }

    [Fact]
    [Trait("Exigence", "ERG-002")]
    public void SaveThenLoad_ClosedPanel_StaysClosed_AndComesBackHome()
    {
        var layout = NewLayout(out var factory);
        var home = DockTree.FindOwner(layout, ControlPanels.Journal)!.Id;
        factory.CloseDockable(DockTree.Find(layout, ControlPanels.Journal).Dockable!);
        var store = new ControlLayoutStore(_folder);

        store.Save(store.Serialize(layout));
        var loaded = store.Load(out var message)!;
        message.ShouldBeNull();
        var reloaded = new ControlDockFactory(_contexts);
        reloaded.InitLayout(loaded);

        DockTree.Find(loaded, ControlPanels.Journal).Place.ShouldBe(PanelPlace.Hidden);
        reloaded.ShowPanel(loaded, ControlPanels.Journal).ShouldNotBeNull();
        DockTree.FindOwner(loaded, ControlPanels.Journal)!.Id.ShouldBe(home);
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

        DockTree.Find(layout, ControlPanels.Dimmers).Dockable!.Context.ShouldBe("Groupes dimmer");
    }

    [Fact]
    [Trait("Exigence", "ERG-032")]
    public void Store_KeepsTheGameLayoutInItsOwnFile_TheOldOnesAreIgnored()
    {
        new ControlLayoutStore(_folder).Path.ShouldEndWith("jeu.json");
        Directory.CreateDirectory(_folder);
        File.WriteAllText(System.IO.Path.Combine(_folder, "controle.json"), "disposition d'une version précédente");
        new ControlLayoutStore(_folder).Load(out var message).ShouldBeNull("l'ancienne disposition Contrôle est laissée de côté");
        message.ShouldBeNull();
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
        var plan = DockTree.Find(layout, ControlPanels.Pilot).Dockable!;
        var home = DockTree.FindOwner(layout, ControlPanels.Pilot)!.Id;

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
            DockTree.Find(layout, ControlPanels.Pilot).Place.ShouldBe(PanelPlace.Hidden);
        }
        else
        {
            DockTree.Find(layout, ControlPanels.Pilot).Place.ShouldBe(PanelPlace.Floating);
        }

        factory.ShowPanel(layout, ControlPanels.Pilot).ShouldNotBeNull();

        DockTree.Find(layout, ControlPanels.Pilot).Place.ShouldBe(PanelPlace.Visible, "remis dans la fenêtre principale");
        DockTree.FindOwner(layout, ControlPanels.Pilot)!.Id.ShouldBe(home, "à sa place livrée");
        var group = DockTree.FindOwner(layout, ControlPanels.Pilot)!;
        var settings = DockTree.FindOwner(layout, ControlPanels.Journal)!;
        var split = DockTree.ParentOf(layout, group)!;
        split.ShouldBeSameAs(DockTree.ParentOf(layout, settings), "à côté du Journal, comme livré");
        split.VisibleDockables!.IndexOf(group).ShouldBeLessThan(split.VisibleDockables.IndexOf(settings), "à sa gauche");
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
