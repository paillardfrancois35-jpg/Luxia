using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Luxia.Tools.Prototype.Docking;
using Luxia.Tools.Prototype.Panels;

namespace Luxia.Tools.Prototype;

/// <summary>
/// Captures sans écran (Avalonia.Headless) : la galerie des composants (ERG-005) et les dispositions prêtes, en PNG.
/// Dispositions lues et écrites dans un dossier temporaire : rien n'est modifié chez l'utilisateur.
/// </summary>
internal static class HeadlessCaptures
{
    public static int Run(string output, bool mockups = false)
    {
        Directory.CreateDirectory(output);
        AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .SetupWithoutStarting();

        if (mockups)
        {
            // Maquettes de la disposition Contrôle (ERG-007), en 1920 × 1080.
            foreach (var scenario in Mockups.MockScenario.All)
            {
                var mockup = new Mockups.MockupWindow(scenario);
                mockup.Show();
                Capture(mockup, output, scenario.FileName);
                mockup.Close();
            }

            // Chantier « Contrôle 2 » (ERG-032 à ERG-039) : écran de jeu, fenêtre d'édition, gestion des dimmers.
            // P8 « Show & séquences » (Q44 solution C, Q45) : colonne Shows, bandeau, éditeurs de séquence et de show.
            foreach (var mockup in Mockups.Controle2Mockups.All.Concat(Mockups.ShowMockups.All))
            {
                var window2 = mockup.Create();
                window2.Show();
                Capture(window2, output, mockup.FileName);
                window2.Close();
            }

            Environment.Exit(0);
            return 0;
        }

        var store = new LayoutStore(Path.Combine(Path.GetTempPath(), "luxia-prototype", Guid.NewGuid().ToString("N")));

        var gallery = new Window { Width = 1100, Height = 1500, Content = PanelViews.Create(PanelCatalog.Gallery, DemoState.Shared), Background = Avalonia.Media.Brushes.Black };
        gallery.Show();
        Capture(gallery, output, "galerie");
        gallery.Close();

        var shell = new ShellViewModel(DemoState.Shared, store);
        var window = new MainWindow(shell) { WindowState = WindowState.Normal, Width = 1680, Height = 1050 };
        window.Show();
        Capture(window, output, "disposition-controle");
        shell.IsShow = true;
        Capture(window, output, "disposition-spectacle");

        // Panneau détaché dans une fenêtre (deuxième écran) : il doit revenir détaché à la relecture.
        shell.IsControl = true;
        var (color, _) = DockTree.Find(shell.Layout!, PanelCatalog.Color);
        shell.Factory.FloatDockable(color!);
        Capture(window, output, "disposition-controle-couleur-detachee");
        shell.SaveIfChanged();
        var reloaded = store.Load(LayoutPreset.Control, out var message);
        Console.WriteLine($"Fenêtres détachées : {shell.Layout!.Windows?.Count ?? 0} ; après relecture : {reloaded?.Windows?.Count ?? 0} ; " +
            $"panneau Couleur relu : {(reloaded is null ? "?" : DockTree.Find(reloaded, PanelCatalog.Color).Place)} {message}");
        window.Close();

        // Pas de fermeture par le cycle de vie Avalonia en mode sans écran.
        Environment.Exit(0);
        return 0;
    }

    private static void Capture(Window window, string output, string name)
    {
        for (var i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }

        var path = Path.Combine(output, name + ".png");
#pragma warning disable CS0618 // Surcharge simple suffisante pour un PNG de contrôle.
        window.CaptureRenderedFrame()?.Save(path);
#pragma warning restore CS0618
        Console.WriteLine(path);
    }
}
