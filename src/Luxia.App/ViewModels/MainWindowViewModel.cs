using System.Globalization;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.Messaging.Events;
using Luxia.UI.Controls;
using Luxia.UI.Modules.Console;
using Luxia.UI.Modules.Outputs;
using Microsoft.Extensions.Logging;

namespace Luxia.App.ViewModels;

/// <summary>
/// Coquille de l'application : navigation entre écrans, menu Projet, barre d'état permanente (GEN-104).
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// Version affichée dans la barre de titre : « 1.003 » une fois validée, « 1.003.017 » en développement
    /// (17e compilation depuis la dernière validation, GEN-119).
    /// </summary>
    private static readonly string Version =
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";

    private readonly LuxiaRuntime _runtime;
    private readonly IDialogService _dialogs;
    private int _statusCountdown;

    [ObservableProperty]
    private NavigationItem _selectedPage;

    [ObservableProperty]
    private string _outputStatus = string.Empty;

    [ObservableProperty]
    private string _outputColor = "#8B949E";

    [ObservableProperty]
    private string _framesPerSecond = string.Empty;

    [ObservableProperty]
    private string _title = "LuXia";

    [ObservableProperty]
    private string _projectName = "Aucun projet";

    [ObservableProperty]
    private string? _projectMessage;

    /// <summary>Indicateurs du moteur (GEN-104) ; le mode automatique viendra en P10.</summary>
    [ObservableProperty]
    private string _engineIndicators = "Blackout : non  ·  GM 100 %  ·  Mode : manuel";

    /// <summary>Blackout (CMD-001, GEN-082) : bouton toujours visible, touche B.</summary>
    [ObservableProperty]
    private bool _blackout;

    /// <summary>Grand Master en % (CMD-002).</summary>
    [ObservableProperty]
    private double _grandMaster = 100;

    private bool _syncingFromEngine;

    /// <summary>Crée la coquille.</summary>
    public MainWindowViewModel(LuxiaRuntime runtime, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
        _dialogs = dialogs;
        Pages =
        [
            new NavigationItem("Live", "▶", new Luxia.UI.Modules.Live.LiveViewModel(runtime)),
            new NavigationItem("Console", "▥", new ConsoleViewModel(runtime, dialogs)),
            new NavigationItem("Bibliothèque", "▤", new Luxia.UI.Modules.Library.LibraryViewModel(runtime, dialogs)),
            new NavigationItem("Installation", "▦", new Luxia.UI.Modules.Installation.InstallationViewModel(runtime, dialogs)),
            new NavigationItem("Scènes", "✦", new Luxia.UI.Modules.Scenes.ScenesViewModel(runtime, dialogs)),
            new NavigationItem("Simulateur", "◎", new Luxia.UI.Modules.Simulator.SimulatorViewModel(runtime)),
            new NavigationItem("Sorties", "⇄", new OutputsViewModel(runtime)),
        ];
        _selectedPage = Pages[0];
        runtime.Project.Changed += (_, _) => UpdateProject();
        runtime.Show.Compiled += (_, _) => UpdateProject();
        UpdateProject();
        RefreshStatus();
    }

    /// <summary>Écrans de l'Atelier.</summary>
    public IReadOnlyList<NavigationItem> Pages { get; }

    /// <summary>
    /// Rafraîchissement de l'écran affiché (20 fois par seconde) et de la barre d'état (4 fois par seconde).
    /// Un écran non affiché qui pilote une surcharge de canaux réels (Identifier) est rafraîchi quand même :
    /// voir <see cref="IRefreshable.NeedsBackgroundRefresh"/>.
    /// </summary>
    public void Refresh()
    {
        var selected = SelectedPage.Page as IRefreshable;
        selected?.Refresh();
        foreach (var page in Pages)
        {
            if (page.Page is IRefreshable refreshable && !ReferenceEquals(refreshable, selected) && refreshable.NeedsBackgroundRefresh)
            {
                refreshable.Refresh();
            }
        }

        if (--_statusCountdown <= 0)
        {
            _statusCountdown = 5;
            RefreshStatus();
        }
    }

    [RelayCommand]
    private async Task NewProjectAsync()
    {
        var parent = await _dialogs.PickFolderAsync("Dossier où créer le projet").ConfigureAwait(true);
        if (parent is null)
        {
            return;
        }

        var name = await _dialogs.AskTextAsync("Nouveau projet", "Nom du projet :").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var folder = Path.Combine(parent, name.Trim());
        if (File.Exists(Path.Combine(folder, Persistence.ProjectStore.ProjectFileName)))
        {
            ProjectMessage = $"Un projet existe déjà dans {folder} : utilisez « Ouvrir ».";
            return;
        }

        _runtime.Project.Create(folder, name.Trim());
    }

    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        var folder = await _dialogs.PickFolderAsync("Dossier du projet à ouvrir").ConfigureAwait(true);
        if (folder is not null && !_runtime.Project.Open(folder))
        {
            ProjectMessage = string.Join(" ", _runtime.Project.Messages.DefaultIfEmpty($"Aucun projet lisible dans {folder}."));
        }
    }

    [RelayCommand]
    private Task ShowAboutAsync() => _dialogs.ShowInfoAsync("À propos de LuXia", BuildDiagnostics());

    /// <summary>
    /// Signale une erreur inattendue de l'interface dans la barre d'état (elle est déjà au journal technique, GEN-117).
    /// </summary>
    public void ReportError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ProjectMessage = $"Erreur inattendue : {exception.Message} — détails dans le journal technique (Aide → À propos : dossier des journaux).";
    }

    /// <summary>Bascule du blackout (bouton, touche B).</summary>
    [RelayCommand]
    private void ToggleBlackout() => Blackout = !Blackout;

    /// <summary>Temps entre le lancement du processus et la fenêtre prête (GEN-064), en secondes.</summary>
    public double? StartupSeconds { get; private set; }

    /// <summary>Fenêtre ouverte : mesure du démarrage (GEN-064), écrite au journal technique et dans « À propos ».</summary>
    public void ReportReady()
    {
        var started = System.Diagnostics.Process.GetCurrentProcess().StartTime;
        StartupSeconds = Math.Round((DateTime.Now - started).TotalSeconds, 1);
        _runtime.Loggers.CreateLogger("Démarrage").LogInformation("LuXia prêt en {Secondes} s (GEN-064 : moins de 10 s)", StartupSeconds);
    }

    /// <summary>
    /// Après un arrêt brutal (GEN-095, SC-11) : propose de reprendre les scènes qui jouaient. Appelé à l'ouverture de la
    /// fenêtre.
    /// </summary>
    public async Task OfferResumeAsync()
    {
        if (_runtime.PendingResume is not { } state)
        {
            return;
        }

        var names = state.Scenes
            .Select(id => _runtime.Project.Scenes.Scenes.FirstOrDefault(s => s.Id == id)?.Name)
            .OfType<string>()
            .ToList();
        var message = string.Create(
            CultureInfo.CurrentCulture,
            $"LuXia ne s'est pas fermé normalement (dernier état connu : {state.SavedAt:dd/MM à HH:mm:ss}).{Environment.NewLine}{Environment.NewLine}Reprendre là où il en était ?{Environment.NewLine}Scènes : {string.Join(", ", names)}{(state.Blackout ? " — blackout actif" : string.Empty)}");
        if (await _dialogs.ConfirmAsync("Reprise après arrêt brutal", message).ConfigureAwait(true))
        {
            _runtime.Resume(state);
        }
        else
        {
            _runtime.DismissResume();
        }
    }

    /// <summary>Versions du projet (GEN-055) : liste des 10 dernières, retour à l'une d'elles.</summary>
    [RelayCommand]
    private async Task ShowVersionsAsync()
    {
        if (_runtime.Project.Folder is not { } folder)
        {
            return;
        }

        var versions = ProjectVersions.List(folder);
        if (versions.Count == 0)
        {
            await _dialogs.ShowInfoAsync("Versions du projet", "Aucune version enregistrée pour l'instant : une version est gardée toutes les 2 minutes si le projet a changé, et à chaque passage en Live.").ConfigureAwait(true);
            return;
        }

        var list = string.Join(
            Environment.NewLine,
            versions.Select((v, i) => string.Create(CultureInfo.CurrentCulture, $"{i + 1}. {v.SavedAt:dd/MM/yyyy HH:mm:ss}  ({v.Reason})")));
        var answer = await _dialogs.AskTextAsync(
            "Versions du projet",
            $"Versions gardées (la plus récente en premier) :{Environment.NewLine}{list}{Environment.NewLine}{Environment.NewLine}Numéro de la version à rétablir (vide = aucune) :").ConfigureAwait(true);
        if (!int.TryParse(answer, NumberStyles.Integer, CultureInfo.CurrentCulture, out var number) || number < 1 || number > versions.Count)
        {
            return;
        }

        var chosen = versions[number - 1];
        var confirm = await _dialogs.ConfirmAsync(
            "Rétablir une version",
            $"Rétablir la version du {chosen.SavedAt:dd/MM/yyyy HH:mm:ss} ? L'état actuel est d'abord gardé comme version (« avant restauration »).").ConfigureAwait(true);
        if (!confirm)
        {
            return;
        }

        ProjectVersions.Restore(folder, chosen.Name, DateTime.Now);
        _runtime.Project.Open(folder);
        ProjectMessage = $"Version du {chosen.SavedAt:dd/MM/yyyy HH:mm:ss} rétablie.";
    }

    partial void OnSelectedPageChanged(NavigationItem value)
    {
        // GEN-054 : passage Atelier → Live = moment de garder une version du projet (sans bloquer l'écran).
        if (value.Page is Luxia.UI.Modules.Live.LiveViewModel)
        {
            _runtime.SaveProjectVersion("passage en Live");
        }
    }

    partial void OnBlackoutChanged(bool value)
    {
        if (!_syncingFromEngine)
        {
            _runtime.SetBlackout(value);
        }
    }

    partial void OnGrandMasterChanged(double value)
    {
        if (!_syncingFromEngine)
        {
            _runtime.SetGrandMaster(Math.Clamp(value, 0, 100) / 100);
        }
    }

    /// <summary>
    /// Relit scènes, palettes et couches sans rouvrir le projet (GEN-133) : pour reprendre du contenu écrit à côté
    /// de l'application, par exemple par une IA de conception.
    /// </summary>
    [RelayCommand]
    private async Task ReloadShowDataAsync()
    {
        if (_runtime.Project.Folder is null)
        {
            ProjectMessage = "Aucun projet ouvert.";
            return;
        }

        var messages = _runtime.Project.ReloadShowData();
        var scenes = _runtime.Project.Scenes.Scenes.Count;
        var palettes = _runtime.Project.Palettes.Palettes.Count;
        var text = string.Create(CultureInfo.CurrentCulture, $"{scenes} scène(s) et {palettes} palette(s) relues.");
        await _dialogs.ShowInfoAsync("Scènes et palettes relues", string.Join(Environment.NewLine, messages.Prepend(text))).ConfigureAwait(true);
    }

    /// <summary>
    /// Importe un lot de scènes (fichier au format de <c>scènes.json</c>, par exemple écrit par une IA de conception) :
    /// ajout seulement, rien n'est écrasé, catégorie « Proposé par IA » par défaut (GEN-133).
    /// </summary>
    [RelayCommand]
    private async Task ImportScenesAsync()
    {
        if (_runtime.Project.Folder is null)
        {
            ProjectMessage = "Aucun projet ouvert.";
            return;
        }

        var files = await _dialogs.PickFilesAsync("Scènes à importer", false, "json").ConfigureAwait(true);
        if (files.Count == 0)
        {
            return;
        }

        var loaded = Persistence.Json.VersionedJsonFile.Load(files[0], Scenes.SceneStore.DocumentType);
        if (!loaded.Succeeded)
        {
            await _dialogs.ShowInfoAsync("Import impossible", loaded.Message ?? "Fichier illisible.").ConfigureAwait(true);
            return;
        }

        var result = Scenes.Rules.SceneImport.Merge(_runtime.Project.Scenes, loaded.Value!);
        if (result.Imported > 0)
        {
            _runtime.Project.SaveScenes(result.Scenes);
        }

        var summary = string.Create(CultureInfo.CurrentCulture, $"{result.Imported} scène(s) importée(s).");
        await _dialogs.ShowInfoAsync("Import de scènes", string.Join(Environment.NewLine, result.Report.Prepend(summary))).ConfigureAwait(true);
    }

    /// <summary>Problèmes trouvés en compilant le projet (références introuvables, valeurs ignorées).</summary>
    [RelayCommand]
    private Task ShowProjectProblemsAsync()
    {
        var issues = _runtime.Show.Last?.Issues ?? [];
        var text = issues.Count == 0
            ? "Aucun problème : toutes les scènes sont jouables telles quelles."
            : string.Join(Environment.NewLine, issues.Select(i => i.ToString()));
        return _dialogs.ShowInfoAsync("Problèmes du projet", text);
    }

    /// <summary>Texte de diagnostic copiable (exécutable, dossiers, préférences, projet) : à donner en cas d'analyse.</summary>
    private string BuildDiagnostics()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var paths = _runtime.Paths;
        var prefs = _runtime.Preferences.Current;
        var project = _runtime.Project;
        var lines = new List<string>
        {
            $"LuXia v{Version}",
            $"Généré le {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"Démarrage : prêt en {StartupSeconds?.ToString("0.0", CultureInfo.CurrentCulture) ?? "?"} s ; processeur : {_runtime.CpuPercent:0.0} %",
            string.Empty,
            $"Exécutable : {Environment.ProcessPath ?? "?"}",
            $"Répertoire de travail : {Environment.CurrentDirectory}",
            $"Processus : PID {process.Id}, démarré {process.StartTime:yyyy-MM-dd HH:mm:ss}",
            $".NET : {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}",
            $"OS : {System.Runtime.InteropServices.RuntimeInformation.OSDescription}",
            string.Empty,
            $"Dossier Documents : {paths.DocumentsRoot}",
            $"Dossier AppData : {paths.AppDataRoot}",
            $"Dossier des journaux : {paths.Logs}",
            $"Fichier de préférences : {paths.PreferencesFile} (existe : {(File.Exists(paths.PreferencesFile) ? "oui" : "non")})",
            $"Dernier projet en mémoire (préférences) : {prefs.LastProjectPath ?? "(aucun)"}",
            string.Empty,
            $"Projet ouvert : {project.Info?.Name ?? "Aucun"}",
            $"Dossier du projet : {project.Folder ?? "—"}",
            string.Empty,
            $"Sortie Arduino : port {prefs.Outputs.Arduino.LastPort ?? "(détection auto)"}, protocole {prefs.Outputs.Arduino.Protocol}",
        };
        return string.Join(Environment.NewLine, lines);
    }

    private void UpdateProject()
    {
        ProjectName = _runtime.Project.Info?.Name ?? "Aucun projet";
        Title = _runtime.Project.Info is { } info ? $"LuXia v{Version} – {info.Name}" : $"LuXia v{Version}";
        var messages = _runtime.Project.Messages.ToList();
        if (_runtime.Show.Last is { Issues.Count: > 0 } compiled)
        {
            messages.Add(string.Create(CultureInfo.CurrentCulture, $"{compiled.Issues.Count} problème(s) dans le projet (menu Projet → Problèmes du projet)."));
        }

        ProjectMessage = messages.Count > 0 ? string.Join(" ", messages) : null;
    }

    private void RefreshStatus()
    {
        // L'état affiché vient du moteur (une autre origine, MIDI ou outil, a pu le changer) : relu sans renvoyer de commande.
        var snapshot = _runtime.Engine.Snapshot;
        _syncingFromEngine = true;
        try
        {
            Blackout = snapshot.Blackout;
            if (Math.Abs((GrandMaster / 100) - snapshot.GrandMaster) > 0.005)
            {
                GrandMaster = Math.Round(snapshot.GrandMaster * 100);
            }
        }
        finally
        {
            _syncingFromEngine = false;
        }

        EngineIndicators = string.Create(
            CultureInfo.CurrentCulture,
            $"Blackout : {(snapshot.Blackout ? "ACTIF" : "non")}  ·  GM {Math.Round(snapshot.GrandMaster * 100)} %  ·  {snapshot.Playbacks.Count} scène(s) en cours  ·  Mode : manuel  ·  CPU {_runtime.CpuPercent:0} %");

        var routes = _runtime.Router.Routes;
        var main = routes.FirstOrDefault(r => r.Driver.Id != Output.Drivers.RecorderOutputDriver.DriverId);
        if (main.Driver is null)
        {
            OutputStatus = "Aucune sortie";
            OutputColor = "#8B949E";
            FramesPerSecond = string.Empty;
            return;
        }

        var status = main.Driver.Status;
        OutputStatus = status.State switch
        {
            OutputConnectionState.Connected => $"{main.Driver.Name} connecté{(status.Message is { } m ? $" ({m})" : string.Empty)}",
            OutputConnectionState.Connecting => $"{main.Driver.Name} : connexion…",
            OutputConnectionState.Error => $"{main.Driver.Name} en erreur",
            _ => $"{main.Driver.Name} déconnecté",
        };
        OutputColor = status.State switch
        {
            OutputConnectionState.Connected => "#3FB950",
            OutputConnectionState.Connecting => "#D29922",
            OutputConnectionState.Error => "#F85149",
            _ => "#8B949E",
        };
        FramesPerSecond = string.Create(CultureInfo.CurrentCulture, $"{status.FramesPerSecond:F1} trames/s");
        if (_runtime.Recorder is not null)
        {
            OutputStatus += "  ·  ● enregistrement";
        }
    }
}
