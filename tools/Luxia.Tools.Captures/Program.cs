using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using Luxia.App;
using Luxia.App.ViewModels;
using Luxia.Core.Time;
using Luxia.Hosting;
using Luxia.Output.Arduino;
using Luxia.Persistence;
using Luxia.UI.Controls;
using Luxia.UI.Modules.Scenes;
using Microsoft.Extensions.Logging.Abstractions;

// Rendu hors écran des écrans de LuXia (Avalonia.Headless) : luxia-captures "<projet>" "<dossier de sortie>".
// Travaille sur une copie du projet et des dossiers de données temporaires : rien n'est modifié chez l'utilisateur.
if (args.Length < 2)
{
    Console.Error.WriteLine("Usage : luxia-captures \"<dossier du projet>\" \"<dossier des images>\"");
    return 1;
}

var source = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(output);
var root = Path.Combine(Path.GetTempPath(), "luxia-captures", Guid.NewGuid().ToString("N"));
var project = Path.Combine(root, "Projet");
foreach (var file in Directory.EnumerateFiles(source, "*.json", SearchOption.AllDirectories))
{
    var target = Path.Combine(project, Path.GetRelativePath(source, file));
    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
    File.Copy(file, target);
}

AppBuilder.Configure<App>()
    .UseSkia()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
    .SetupWithoutStarting();

var clock = new VirtualClock();
var runtime = new LuxiaRuntime(new DataPaths(Path.Combine(root, "Documents"), Path.Combine(root, "AppData")), NullLoggerFactory.Instance, new NoSerialPorts(), clock);
runtime.Project.Open(project);
var vm = new MainWindowViewModel(runtime, new NoDialogs());
// Taille facultative (3e et 4e arguments) : vérifier un écran de portable (1366 × 768) ou plein HD.
var width = args.Length > 3 && int.TryParse(args[2], out var w) ? w : 1680;
var height = args.Length > 3 && int.TryParse(args[3], out var h) ? h : 1050;
var window = new MainWindow { DataContext = vm, Width = width, Height = height };
window.Show();

void Tick(int count = 2)
{
    for (var i = 0; i < count; i++)
    {
        clock.Advance(TimeSpan.FromMilliseconds(25));
        runtime.Engine.Tick();
    }

    vm.Refresh();
    Dispatcher.UIThread.RunJobs();
}

void Capture(string name, Avalonia.Controls.Window? other = null)
{
    Tick();
    AvaloniaHeadlessPlatform.ForceRenderTimerTick();
    Dispatcher.UIThread.RunJobs();
    var frame = (other ?? window).CaptureRenderedFrame();
    var path = Path.Combine(output, name + ".png");
#pragma warning disable CS0618 // Surcharge simple suffisante pour un PNG de contrôle.
    frame?.Save(path);
#pragma warning restore CS0618
    Console.WriteLine(path);
}

foreach (var page in vm.Pages)
{
    vm.SelectedPage = page;
    Capture(page.Title);
}

// Écran Contrôle (doc 60, E2) en situation : LIVE, ÉDITION, AVEUGLE, zones.
if (vm.Pages.FirstOrDefault(p => p.Page is Luxia.UI.Modules.Control.ControlViewModel) is { Page: Luxia.UI.Modules.Control.ControlViewModel control } controlPage)
{
    vm.SelectedPage = controlPage;
    var fixtures = runtime.Project.Installation.Fixtures;
    Guid Id(string name) => fixtures.First(f => f.Name == name).Id;
    Luxia.Scenes.Model.Scene SceneNamed(string name) => runtime.Project.Scenes.Scenes.First(s => s.Name == name);

    // LIVE : deux scènes jouent, les 4 PAR sont retouchés en bleu.
    runtime.Engine.Send(new Luxia.Messaging.Commands.LaunchSceneCommand(Luxia.Messaging.Commands.CommandOrigin.User, SceneNamed("Plein feu").Id));
    runtime.Engine.Send(new Luxia.Messaging.Commands.LaunchSceneCommand(Luxia.Messaging.Commands.CommandOrigin.User, SceneNamed("Lyres sur 3 positions").Id));
    control.Session.Select([Id("PAR 1"), Id("PAR 2"), Id("PAR 3"), Id("PAR 4")]);
    Tick(40);
    control.Settings.SelectedTab = 1;
    control.Settings.RequestColor(new LightColor(215, 1, 1));
    Tick(40);
    Capture("Contrôle - LIVE surcharge");

    // Boutons de scène resserrés (essai 1.005.226).
    control.Columns.IsCompact = true;
    Tick(5);
    Capture("Contrôle - scènes resserrées");
    control.Columns.IsCompact = false;

    // ÉDITION : étape 2 du chenillard, PAR sélectionnés.
    control.Session.ReleaseAll();
    control.Session.ChooseScene(SceneNamed("Chenillard 4 couleurs").Id);
    control.Session.ChooseStep(1);
    control.SetModeCommand.Execute("edition");
    Tick(10);
    Capture("Contrôle - ÉDITION");

    // P6 : panneau Effets, un chenillard sur les PAR (courbe et points des membres), puis des cercles opposés sur les lyres.
    control.Session.ChooseScene(SceneNamed("Plein feu").Id);
    control.SetModeCommand.Execute("edition");
    control.Session.Select([Id("PAR 1"), Id("PAR 2"), Id("PAR 3"), Id("PAR 4")]);
    control.Effects.SelectedTemplate = control.Effects.Templates.First(t => t.Template.Name == "Vague douce");
    control.Effects.AddEffectCommand.Execute(null);
    control.Session.Commit();
    control.RequestPanel(Luxia.UI.Modules.Control.Docking.ControlPanels.Effects);
    Tick(20);
    Capture("Contrôle - Effets vague");
    control.Session.Select([Id("Lyre 1"), Id("Lyre 2")]);
    control.Effects.SelectedTemplate = control.Effects.Templates.First(t => t.Template.Name == "Cercles opposés");
    control.Effects.AddEffectCommand.Execute(null);
    control.Session.Commit();
    Tick(20);
    Capture("Contrôle - Effets cercles");
    control.Session.Undo();
    control.Session.Undo();
    control.RequestPanel(Luxia.UI.Modules.Control.Docking.ControlPanels.Settings);

    // AVEUGLE : les lyres, onglet Position.
    control.Session.ChooseScene(SceneNamed("Lyres sur 3 positions").Id);
    control.SetModeCommand.Execute("aveugle");
    control.Session.Select([Id("Lyre 1"), Id("Lyre 2")]);
    Tick(10);
    control.Settings.SelectedTab = 2;
    Tick(10);
    Capture("Contrôle - AVEUGLE lyres");

    // Zones de la lyre 1 (lieu).
    control.SetModeCommand.Execute("live");
    control.Session.Select([Id("Lyre 1")]);
    Tick(5);
    control.Settings.SelectedTab = 2;
    control.Settings.IsZoneEditing = true;
    control.Settings.NewZoneAllowed = true;
    control.Settings.RequestZone(new PanTiltZoneRequest(null, new PanTiltRect(0.1, 0.9, 0.15, 0.95)));
    control.Settings.NewZoneAllowed = false;
    control.Settings.RequestZone(new PanTiltZoneRequest(null, new PanTiltRect(0.35, 0.65, 0.05, 0.3)));
    Tick(5);
    Capture("Contrôle - zones");
    control.Settings.IsZoneEditing = false;
    control.Flush();

    // Disposition Spectacle (F10) avec deux looks.
    runtime.Project.SaveLooks(new Luxia.Scenes.Model.LookSet
    {
        Looks =
        [
            new() { Name = "Temps mort", Color = "#FFB000", Actions = [new() { Kind = Luxia.Scenes.Model.LookActionKind.StopAll }, new() { Kind = Luxia.Scenes.Model.LookActionKind.LaunchScene, SceneId = SceneNamed("Ambre – couleur seule").Id }] },
            new() { Name = "Retour de piste", Color = "#DB61A2", Actions = [new() { Kind = Luxia.Scenes.Model.LookActionKind.StopAll }, new() { Kind = Luxia.Scenes.Model.LookActionKind.LaunchScene, SceneId = SceneNamed("Chenillard 4 couleurs").Id }] },
        ],
    });
    control.SetLayoutCommand.Execute("spectacle");
    Tick(5);
    Capture("Contrôle - disposition Spectacle");
    control.SetLayoutCommand.Execute("controle");

    // F8 : la même chose à 125 %.
    vm.SetUiScaleCommand.Execute("1.25");
    Capture("Contrôle - taille 125 %");
    vm.SetUiScaleCommand.Execute("1");
}

// Écran Live « en jeu » : couches combinées, strobe limité, zone interdite, figé, palette rapide.
if (vm.Pages.FirstOrDefault(p => p.Page is Luxia.UI.Modules.Live.LiveViewModel) is { Page: Luxia.UI.Modules.Live.LiveViewModel live } livePage)
{
    vm.SelectedPage = livePage;
    foreach (var name in new[] { "Plein feu", "Rouge – couleur seule", "Piège : lyre 1 vers le public", "Strobe PAR (plafonné à 10 s)" })
    {
        if (live.Columns.SelectMany(c => c.Scenes).FirstOrDefault(s => s.Name == name) is { } scene)
        {
            live.Press(scene);
        }
    }

    Tick(40 * 11);
    live.SelectQuickCommand.Execute(live.QuickSelections.FirstOrDefault(s => s.Label == "Toutes les lyres"));
    live.ApplyPaletteCommand.Execute(live.Positions.FirstOrDefault());
    live.ToggleFreezeCommand.Execute(null);
    Tick();
    Capture("Live - en jeu");
}

// Écran Scènes « en situation » : une scène choisie, les PAR sélectionnés au programmeur, puis une lyre.
if (vm.Pages.FirstOrDefault(p => p.Page is ScenesViewModel) is { Page: ScenesViewModel scenes } scenesPage)
{
    vm.SelectedPage = scenesPage;
    scenes.SelectedScene = scenes.Scenes.FirstOrDefault();
    if (scenes.Programmer.Shortcuts.FirstOrDefault(s => s.Label.StartsWith("Tous les PAR", StringComparison.Ordinal)) is { } pars)
    {
        scenes.Programmer.SelectCommand.Execute(pars);
    }

    scenes.Programmer.Color.Red = 100;
    Capture("Scènes - PAR sélectionnés");
    scenes.Programmer.SelectNoneCommand.Execute(null);
    var lyre = scenes.Programmer.Fixtures.FirstOrDefault(f => f.Name == "Lyre 1");
    if (lyre is not null)
    {
        lyre.IsSelected = true;
    }

    Capture("Scènes - lyre sélectionnée");

    // Éditeur de couches (COU-001), fenêtre à part.
    if (scenes.CreateLayersEditor() is { } editor)
    {
        var layers = new LayersWindow { DataContext = editor };
        layers.Show();
        Capture("Couches", layers);
        layers.Close();
    }

    // Zones interdites (INST-053), fenêtre à part, avec une zone d'exemple sur la lyre sélectionnée.
    if (scenes.CreateZonesEditor() is { } zonesEditor)
    {
        zonesEditor.AddFromProgrammerCommand.Execute(null);
        var zones = new ZonesWindow { DataContext = zonesEditor };
        zones.Show();
        Capture("Zones interdites", zones);
        zones.Close();
    }
}

// Pas de fermeture par le cycle de vie Avalonia en mode sans écran : on s'arrête directement une fois les images écrites.
Environment.Exit(0);
return 0;

/// <summary>Aucun port série : la sortie reste nulle.</summary>
internal sealed class NoSerialPorts : ISerialPortProvider
{
    public IReadOnlyList<SerialPortInfo> GetPorts() => [];

    public ISerialConnection Open(string portName) => throw new IOException("pas de port série pour les captures");
}

/// <summary>Aucune boîte de dialogue : les captures ne posent pas de question.</summary>
internal sealed class NoDialogs : IDialogService
{
    public Task<bool> ConfirmAsync(string title, string message) => Task.FromResult(false);

    public Task ShowInfoAsync(string title, string message) => Task.CompletedTask;

    public Task<string?> AskTextAsync(string title, string prompt, string? initialValue = null) => Task.FromResult<string?>(null);

    public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(null);

    public Task<IReadOnlyList<string>> PickFilesAsync(string title, bool allowMultiple, params string[] extensions) =>
        Task.FromResult<IReadOnlyList<string>>([]);
}
