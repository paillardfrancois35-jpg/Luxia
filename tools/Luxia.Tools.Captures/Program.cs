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
var window = new MainWindow { DataContext = vm, Width = 1680, Height = 1050 };
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
