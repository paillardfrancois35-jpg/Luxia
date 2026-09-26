using Luxia.Core.Projects;
using Luxia.Core.Snapshots;
using Luxia.Patch;
using Luxia.Patch.Model;
using Luxia.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Luxia.Hosting;

/// <summary>
/// Projet ouvert : dossier, fiche, parties chargées (instantanés de console, installation, lieux — doc 13).
/// Le dernier projet ouvert est mémorisé dans les préférences et rouvert au démarrage.
/// </summary>
public sealed class ProjectSession
{
    private readonly PreferencesStore _preferences;
    private readonly ILogger _logger;

    /// <summary>Crée la session (aucun projet ouvert).</summary>
    public ProjectSession(PreferencesStore preferences, ILogger<ProjectSession>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        _preferences = preferences;
        _logger = logger ?? NullLogger<ProjectSession>.Instance;
    }

    /// <summary>Dossier du projet ouvert.</summary>
    public string? Folder { get; private set; }

    /// <summary>Fiche du projet ouvert.</summary>
    public ProjectInfo? Info { get; private set; }

    /// <summary>Instantanés de console du projet.</summary>
    public ConsoleData Console { get; private set; } = new();

    /// <summary>Installation (univers, patch, sélections manuelles — doc 13).</summary>
    public Installation Installation { get; private set; } = new();

    /// <summary>Lieux du projet (doc 13 §5).</summary>
    public VenueSet Venues { get; private set; } = new();

    /// <summary>Copie des modèles d'appareils utilisés par le projet (GEN-053) ; <c>null</c> hors projet ouvert.</summary>
    public ProjectFixtureLibrary? FixtureLibrary { get; private set; }

    /// <summary>Messages du dernier chargement (migrations, fichiers mis de côté).</summary>
    public IReadOnlyList<string> Messages { get; private set; } = [];

    /// <summary>Levé après ouverture, création ou fermeture d'un projet.</summary>
    public event EventHandler? Changed;

    /// <summary>Rouvre le dernier projet s'il existe encore.</summary>
    public void OpenLast()
    {
        var last = _preferences.Current.LastProjectPath;
        if (string.IsNullOrWhiteSpace(last))
        {
            _logger.LogInformation("Démarrage : aucun dernier projet enregistré.");
            return;
        }

        if (!File.Exists(Path.Combine(last, ProjectStore.ProjectFileName)))
        {
            _logger.LogWarning("Démarrage : dernier projet introuvable, non rouvert : {Dossier}", last);
            return;
        }

        _logger.LogInformation("Démarrage : reprise automatique du dernier projet : {Dossier}", last);
        Open(last);
    }

    /// <summary>Ouvre un projet ; renvoie <c>false</c> (avec messages) s'il est illisible.</summary>
    public bool Open(string folder)
    {
        var report = ProjectStore.Open(folder);
        var messages = new List<string>(report.Messages);
        if (!report.Succeeded)
        {
            Messages = messages;
            _logger.LogWarning("Ouverture du projet impossible : {Dossier}", folder);
            return false;
        }

        var (console, consoleMessage) = ProjectPartStore.Load(folder, ProjectPartStore.ConsoleFileName, ProjectPartStore.ConsoleType, () => new ConsoleData());
        if (consoleMessage is not null)
        {
            messages.Add(consoleMessage);
        }

        var (installation, installationMessage) = InstallationStore.Load(folder);
        if (installationMessage is not null)
        {
            messages.Add(installationMessage);
        }

        var (venues, venuesMessage) = VenueStore.Load(folder);
        if (venuesMessage is not null)
        {
            messages.Add(venuesMessage);
        }

        Folder = folder;
        Info = report.Info;
        Console = console;
        Installation = installation;
        Venues = venues;
        FixtureLibrary = new ProjectFixtureLibrary(folder);
        Messages = messages;
        _preferences.Update(p => p with { LastProjectPath = folder });
        _logger.LogInformation("Projet ouvert : {Nom} ({Dossier})", Info!.Name, folder);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Crée un projet et l'ouvre.</summary>
    public void Create(string folder, string name)
    {
        ProjectStore.Create(folder, name);
        Open(folder);
    }

    /// <summary>Remplace les instantanés et les enregistre.</summary>
    public void SaveConsole(ConsoleData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var folder = Folder ?? throw new InvalidOperationException("Aucun projet ouvert.");
        ProjectPartStore.Save(folder, ProjectPartStore.ConsoleFileName, data, ProjectPartStore.ConsoleType);
        Console = data;
        Info = ProjectStore.Save(folder, Info!);
    }

    /// <summary>Remplace l'installation et l'enregistre (doc 13).</summary>
    public void SaveInstallation(Installation installation)
    {
        ArgumentNullException.ThrowIfNull(installation);
        var folder = Folder ?? throw new InvalidOperationException("Aucun projet ouvert.");
        InstallationStore.Save(folder, installation);
        Installation = installation;
        Info = ProjectStore.Save(folder, Info!);
    }

    /// <summary>Remplace les lieux et les enregistre (doc 13 §5).</summary>
    public void SaveVenues(VenueSet venues)
    {
        ArgumentNullException.ThrowIfNull(venues);
        var folder = Folder ?? throw new InvalidOperationException("Aucun projet ouvert.");
        VenueStore.Save(folder, venues);
        Venues = venues;
        Info = ProjectStore.Save(folder, Info!);
    }
}
