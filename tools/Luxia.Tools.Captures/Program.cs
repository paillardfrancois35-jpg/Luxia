using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
var runtime = new LuxiaRuntime(new DataPaths(Path.Combine(root, "Documents"), Path.Combine(root, "AppData")), NullLoggerFactory.Instance, new NoSerialPorts(), clock, null, new SyntheticSources());
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

// Écran de jeu (ERG-032) : des scènes jouent, des looks, un dimmer retouché (bandeau jaune) ; la bande ✎ vise la fenêtre d'édition.
if (vm.Pages.FirstOrDefault(p => p.Page is Luxia.UI.Modules.Control.GameViewModel) is { Page: Luxia.UI.Modules.Control.GameViewModel game } gamePage)
{
    vm.SelectedPage = gamePage;
    Luxia.Scenes.Model.Scene SceneNamed(string name) => runtime.Project.Scenes.Scenes.First(s => s.Name == name);
    foreach (var name in new[] { "Plein feu", "Chenillard 4 couleurs", "Lyres sur 3 positions", "UV plein" })
    {
        runtime.Engine.Send(new Luxia.Messaging.Commands.LaunchSceneCommand(Luxia.Messaging.Commands.CommandOrigin.User, SceneNamed(name).Id));
    }

    runtime.Project.SaveLooks(new Luxia.Scenes.Model.LookSet
    {
        Looks =
        [
            new() { Name = "Temps mort", Color = "#FFB000", Actions = [new() { Kind = Luxia.Scenes.Model.LookActionKind.StopAll }, new() { Kind = Luxia.Scenes.Model.LookActionKind.LaunchScene, SceneId = SceneNamed("Ambre – couleur seule").Id }] },
            new() { Name = "Retour de piste", Color = "#DB61A2", Actions = [new() { Kind = Luxia.Scenes.Model.LookActionKind.StopAll }, new() { Kind = Luxia.Scenes.Model.LookActionKind.LaunchScene, SceneId = SceneNamed("Chenillard 4 couleurs").Id }] },
        ],
    });
    Tick(40);
    Capture("Contrôle - écran de jeu");
    game.Columns.IsCompact = true;
    Tick(5);
    Capture("Contrôle - écran de jeu resserré");
    game.Columns.IsCompact = false;

    // F8 : la même chose à 125 %.
    vm.SetUiScaleCommand.Execute("1.25");
    Capture("Contrôle - écran de jeu 125 %");
    vm.SetUiScaleCommand.Execute("1");

    // Fenêtre d'édition (ERG-033, ERG-034) : brouillon du chenillard, puis aveugle sur les lyres.
    var editor = new Luxia.UI.Modules.Control.Views.EditorWindow { Width = 1440, Height = 860 };
    editor.Attach(game.Editor);
    editor.Show();
    Guid FixtureId(string name) => runtime.Project.Installation.Fixtures.First(f => f.Name == name).Id;
    var work = game.Editor.Workbench;
    game.Editor.Open(SceneNamed("Chenillard 4 couleurs").Id);
    work.Session.ChooseStep(1);
    work.Session.Select([FixtureId("PAR 1"), FixtureId("PAR 2"), FixtureId("PAR 3"), FixtureId("PAR 4")]);
    work.Settings.SelectedTab = 1;
    Tick(10);
    game.Editor.Refresh();
    Capture("Édition - brouillon", editor);
    game.Editor.Cancel();
    game.Editor.Open(SceneNamed("Lyres sur 3 positions").Id);
    editor.Present(null);
    game.Editor.IsBlind = true;
    work.Session.ChooseStep(2);
    work.Session.Select([FixtureId("Lyre 1"), FixtureId("Lyre 2")]);
    work.Settings.SelectedTab = 2;
    Tick(10);
    game.Editor.Refresh();
    Capture("Édition - aveugle", editor);
    game.Editor.Cancel();
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

// Onglet « Gestion des dimmers » (ERG-036) : un arbre d'exemple sur le parc du projet, dimmers réglés pour voir les niveaux.
if (vm.Pages.FirstOrDefault(p => p.Page is Luxia.UI.Modules.Installation.InstallationViewModel) is { Page: Luxia.UI.Modules.Installation.InstallationViewModel installation } installationPage)
{
    vm.SelectedPage = installationPage;
    var all = runtime.Project.Installation.Fixtures;
    Guid[] Named(params string[] names) => [.. all.Where(f => names.Contains(f.Name)).Select(f => f.Id)];
    var parc = new Luxia.Patch.Model.FixtureGroup { Name = "Parc lumineux", HasDimmer = true };
    var face = new Luxia.Patch.Model.FixtureGroup { Name = "Face (PAR)", ParentId = parc.Id, HasDimmer = true };
    var pars = new Luxia.Patch.Model.FixtureGroup { Name = "PAR scène", ParentId = face.Id, HasDimmer = true, FixtureIds = Named("PAR 1", "PAR 2", "PAR 3", "PAR 4") };
    var barres = new Luxia.Patch.Model.FixtureGroup { Name = "Barres", ParentId = parc.Id, HasDimmer = true, FixtureIds = Named("Barre 1", "Barre 2") };
    var uv = new Luxia.Patch.Model.FixtureGroup { Name = "UV", HasDimmer = true, FixtureIds = Named("UV 1", "UV 2") };
    runtime.Project.SaveGroups(new Luxia.Patch.Model.FixtureGroupSet { Groups = [parc, face, pars, barres, uv] });
    Tick(3);
    foreach (var (group, level) in new[] { (parc, 0.8), (face, 0.7), (pars, 0.5), (uv, 0.4) })
    {
        runtime.Engine.Send(new Luxia.Messaging.Commands.SetGroupDimmerCommand(Luxia.Messaging.Commands.CommandOrigin.User, group.Id, level));
    }

    Tick(3);
    installation.Dimmers.Reload();
    installation.Dimmers.SelectedRow = installation.Dimmers.Rows.First(r => r.Name == "PAR scène");
    var tabs = window.GetVisualDescendants().OfType<TabControl>().First();
    tabs.SelectedItem = tabs.Items.OfType<TabItem>().First(t => (t.Header as string) == "Gestion des dimmers");
    Capture("Installation - Gestion des dimmers");

    // Panneau « Groupes dimmer » de l'écran Contrôle (ERG-037) : mêmes groupes, mêmes niveaux.
    if (vm.Pages.FirstOrDefault(p => p.Page is Luxia.UI.Modules.Control.GameViewModel) is { Page: Luxia.UI.Modules.Control.GameViewModel dimmersControl } dimmersControlPage)
    {
        vm.SelectedPage = dimmersControlPage;
        dimmersControl.RequestPanel(Luxia.UI.Modules.Control.Docking.ControlPanels.Dimmers);
        Tick(5);
        Capture("Contrôle - Groupes dimmer");
    }
}

// Écran Audio « en écoute » (essai P7, décision 2) : une musique synthétique (kick, caisse claire, charleston, basse à 124 BPM) est jouée en temps réel.
if (vm.Pages.FirstOrDefault(p => p.Page is Luxia.UI.Modules.Audio.AudioViewModel) is { Page: Luxia.UI.Modules.Audio.AudioViewModel audio } audioPage)
{
    vm.SelectedPage = audioPage;
    runtime.SetAudioMode(true);
    for (var i = 0; i < 400; i++)
    {
        Tick(1);
        Thread.Sleep(25);
    }

    Capture("Audio - en écoute");
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

/// <summary>Source de son synthétique : un morceau à 124 BPM joué en temps réel (kick, caisse claire, charleston, basse).</summary>
internal sealed class SyntheticSources : Luxia.Audio.IAudioSourceFactory
{
    public Luxia.Audio.IAudioSource CreateLoopback() => new SyntheticSource();

    public Luxia.Audio.IAudioSource Create(string? deviceId) => new SyntheticSource();

    public IReadOnlyList<Luxia.Audio.AudioDeviceInfo> Devices() => [new("synthese", "Haut-parleurs (synthétique)", false)];
}

/// <summary>Voir <see cref="SyntheticSources"/>.</summary>
internal sealed class SyntheticSource : Luxia.Audio.IAudioSource
{
    private const int Rate = 44100;
    private const double Bpm = 124;
    private readonly Random _noise = new(7);
    private Thread? _thread;
    private volatile bool _run;

    public string Name => "Haut-parleurs (synthétique)";

    public int SampleRate => Rate;

    public event EventHandler<Luxia.Audio.AudioBlock>? BlockAvailable;

    public event EventHandler<Exception?>? Stopped
    {
        add { }
        remove { }
    }

    public void StartCapture()
    {
        _run = true;
        _thread = new Thread(Loop) { IsBackground = true, Name = "synthese" };
        _thread.Start();
    }

    public void StopCapture() => _run = false;

    public void Dispose() => _run = false;

    private void Loop()
    {
        const int block = 882; // 20 ms
        var buffer = new float[block];
        long position = 0;
        var started = System.Diagnostics.Stopwatch.StartNew();
        while (_run)
        {
            for (var i = 0; i < block; i++)
            {
                buffer[i] = Sample((position + i) / (double)Rate);
            }

            position += block;
            BlockAvailable?.Invoke(this, new Luxia.Audio.AudioBlock(buffer.AsMemory(0, block)));
            var wait = (int)((position * 1000.0 / Rate) - started.ElapsedMilliseconds);
            if (wait > 0)
            {
                Thread.Sleep(wait);
            }
        }
    }

    private float Sample(double t)
    {
        var beat = 60.0 / Bpm;
        var inBeat = t % beat;
        var beatNumber = (long)(t / beat);
        var value = 0.0;
        // Kick à chaque temps : sinus qui descend de 110 à 45 Hz.
        value += 0.7 * Math.Sin(2 * Math.PI * (45 * inBeat + (65 * (1 - Math.Exp(-inBeat * 30)) / 30 * 0) + (65 / 30.0 * (1 - Math.Exp(-30 * inBeat))))) * Math.Exp(-inBeat * 14);
        // Caisse claire sur les temps 2 et 4 : bruit.
        if (beatNumber % 2 == 1)
        {
            value += 0.35 * ((_noise.NextDouble() * 2) - 1) * Math.Exp(-inBeat * 22);
        }

        // Charleston sur les contretemps : bruit bref.
        var off = (t + (beat / 2)) % beat;
        value += 0.15 * ((_noise.NextDouble() * 2) - 1) * Math.Exp(-off * 60);
        // Basse tenue sur la fondamentale, qui monte d'une quarte toutes les quatre mesures.
        var note = (beatNumber / 16) % 2 == 0 ? 55.0 : 73.4;
        value += 0.25 * Math.Sin(2 * Math.PI * note * t) * (0.6 + (0.4 * Math.Exp(-inBeat * 4)));
        return (float)Math.Clamp(value * 0.7, -1, 1);
    }
}
