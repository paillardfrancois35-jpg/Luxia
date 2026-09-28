using Avalonia.Controls;
using Luxia.Tools.Prototype.Docking;

namespace Luxia.Tools.Prototype;

/// <summary>Fenêtre du prototype : bandeau, zone d'ancrage Dock, barre d'état.</summary>
public sealed partial class MainWindow : Window
{
    /// <summary>Pour le concepteur visuel uniquement.</summary>
    public MainWindow()
        : this(new ShellViewModel(DemoState.Shared, LayoutStore.ForCurrentUser()))
    {
    }

    internal MainWindow(ShellViewModel shell)
    {
        InitializeComponent();
        DataContext = shell;
        WindowState = WindowState.Maximized;
        var menu = (MenuFlyout)PanelsButton.Flyout!;
        menu.Opening += (_, _) => FillPanelsMenu(menu, shell);
        Closing += (_, _) => shell.Close();

        // ERG-007 : chaque maquette s'ouvre dans sa fenêtre, pour la regarder en vrai (panneaux déplaçables).
        var mockups = (MenuFlyout)MockupsButton.Flyout!;
        foreach (var scenario in Mockups.MockScenario.All)
        {
            var item = new MenuItem { Header = scenario.Title };
            item.Click += (_, _) => new Mockups.MockupWindow(scenario).Show();
            mockups.Items.Add(item);
        }
    }

    // Le menu est refait à chaque ouverture : il dit où est chaque panneau (affiché, replié, fermé).
    private static void FillPanelsMenu(MenuFlyout menu, ShellViewModel shell)
    {
        menu.Items.Clear();
        foreach (var (panel, place) in shell.PanelStates())
        {
            var suffix = place switch
            {
                PanelPlace.Hidden => "  (fermé)",
                PanelPlace.Pinned => "  (replié)",
                PanelPlace.Absent => "  (absent de cette disposition)",
                _ => "",
            };
            menu.Items.Add(new MenuItem
            {
                Header = panel.Title + suffix,
                Command = shell.ShowPanelCommand,
                CommandParameter = panel.Id,
                Icon = new TextBlock { Text = place == PanelPlace.Visible ? "✓" : "" },
            });
        }
    }
}
