using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using Luxia.Tools.Prototype.Docking;

namespace Luxia.Tools.Prototype.Mockups;

/// <summary>
/// Fenêtre d'une maquette de la disposition Contrôle (ERG-007, doc 60 §7.3) : en-tête et bandeau de mode, panneaux
/// ancrés par Dock comme dans le prototype, barre d'état. Données fictives, aucune action réelle.
/// </summary>
internal sealed class MockupWindow : Window
{
    public MockupWindow(MockScenario scenario)
    {
        Title = "LuXia – maquette : " + scenario.Title;
        Width = 1920;
        Height = 1080;
        Background = Tokens.Brush(Tokens.Background);

        var factory = new PrototypeDockFactory();
        var layout = CreateLayout(factory, scenario);
        factory.InitLayout(layout);

        var status = new Border
        {
            Background = Tokens.Brush(Tokens.Surface),
            BorderBrush = Tokens.Brush(Tokens.Border),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(10, 3),
            Child = new TextBlock
            {
                Text = "Show de travail  ·  lieu Générique  ·  14 appareils  ·  sortie Arduino COM5, 40 trames / s  ·  enregistré à l'instant (Ctrl+Z pour annuler)",
                FontSize = 12,
                Foreground = Tokens.Brush(Tokens.Secondary),
            },
        };

        var root = new DockPanel();
        var header = HeaderView.Create(scenario);
        DockPanel.SetDock(header, Avalonia.Controls.Dock.Top);
        DockPanel.SetDock(status, Avalonia.Controls.Dock.Bottom);
        root.Children.Add(header);
        root.Children.Add(status);
        root.Children.Add(new DockControl { Layout = layout, InitializeLayout = true, InitializeFactory = true });
        Content = root;
    }

    // Contrôle (doc 60 §6) : colonnes au centre, propriétés à droite, plan + réglages en bas, journal en onglet.
    private static IRootDock CreateLayout(PrototypeDockFactory factory, MockScenario scenario)
    {
        MockPanel Panel(string id) => new() { Id = id, Title = PanelCatalog.Get(id).Title, Scenario = scenario, CanClose = true, CanFloat = true, CanPin = true };

        ToolDock Tools(double proportion, params string[] ids)
        {
            var panels = ids.Select(Panel).ToArray();
            return new ToolDock { Id = "onglets-" + ids[0], Proportion = proportion, ActiveDockable = panels[0], VisibleDockables = factory.CreateList<IDockable>(panels) };
        }

        ProportionalDock Split(double proportion, Orientation orientation, IDockable a, IDockable b) =>
            new() { Proportion = proportion, Orientation = orientation, VisibleDockables = factory.CreateList<IDockable>(a, new ProportionalDockSplitter(), b) };

        var bottom = Split(0.44, Orientation.Horizontal, Tools(0.34, PanelCatalog.FixturePlan), Tools(0.66, PanelCatalog.Settings, PanelCatalog.Log));
        var left = Split(0.76, Orientation.Vertical, Tools(0.56, PanelCatalog.Columns), bottom);
        var main = Split(double.NaN, Orientation.Horizontal, left, Tools(0.24, PanelCatalog.Properties));
        var root = factory.CreateRootDock();
        root.IsCollapsable = false;
        root.VisibleDockables = factory.CreateList<IDockable>(main);
        root.ActiveDockable = main;
        root.DefaultDockable = main;
        return root;
    }
}
