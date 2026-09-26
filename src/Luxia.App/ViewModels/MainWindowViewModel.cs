using System.Globalization;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.Messaging.Events;
using Luxia.UI.Controls;
using Luxia.UI.Modules.Console;
using Luxia.UI.Modules.Outputs;

namespace Luxia.App.ViewModels;

/// <summary>
/// Coquille de l'application : navigation entre écrans, menu Projet, barre d'état permanente (GEN-104).
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>Version affichée dans la barre de titre (`Directory.Build.props`, étiquette Git correspondante).</summary>
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

    /// <summary>Indicateurs non encore disponibles (blackout : P4, mode automatique : P10).</summary>
    [ObservableProperty]
    private string _engineIndicators = "Blackout : —  ·  Mode : manuel";

    /// <summary>Crée la coquille.</summary>
    public MainWindowViewModel(LuxiaRuntime runtime, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
        _dialogs = dialogs;
        Pages =
        [
            new NavigationItem("Console", "▥", new ConsoleViewModel(runtime, dialogs)),
            new NavigationItem("Bibliothèque", "▤", new Luxia.UI.Modules.Library.LibraryViewModel(runtime, dialogs)),
            new NavigationItem("Installation", "▦", new Luxia.UI.Modules.Installation.InstallationViewModel(runtime, dialogs)),
            new NavigationItem("Simulateur", "◎", new Luxia.UI.Modules.Simulator.SimulatorViewModel(runtime)),
            new NavigationItem("Sorties", "⇄", new OutputsViewModel(runtime)),
        ];
        _selectedPage = Pages[0];
        runtime.Project.Changed += (_, _) => UpdateProject();
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
            string.Empty,
            $"Exécutable : {Environment.ProcessPath ?? "?"}",
            $"Répertoire de travail : {Environment.CurrentDirectory}",
            $"Processus : PID {process.Id}, démarré {process.StartTime:yyyy-MM-dd HH:mm:ss}",
            $".NET : {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}",
            $"OS : {System.Runtime.InteropServices.RuntimeInformation.OSDescription}",
            string.Empty,
            $"Dossier Documents : {paths.DocumentsRoot}",
            $"Dossier AppData : {paths.AppDataRoot}",
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
        ProjectMessage = _runtime.Project.Messages.Count > 0 ? string.Join(" ", _runtime.Project.Messages) : null;
    }

    private void RefreshStatus()
    {
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
