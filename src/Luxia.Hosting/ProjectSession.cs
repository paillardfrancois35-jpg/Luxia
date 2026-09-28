using Luxia.Core.Projects;
using Luxia.Core.Snapshots;
using Luxia.Patch;
using Luxia.Patch.Model;
using Luxia.Persistence;
using Luxia.Scenes;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Luxia.Hosting;

/// <summary>
/// Projet ouvert : dossier, fiche, parties chargées (instantanés de console, installation, lieux — doc 13 ;
/// scènes, palettes, couches — doc 16, 17).
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

    /// <summary>Scènes (doc 16).</summary>
    public SceneSet Scenes { get; private set; } = new();

    /// <summary>Palettes (doc 17 §2) ; jeu par défaut si le projet n'en a pas encore (PAL-009).</summary>
    public PaletteSet Palettes { get; private set; } = DefaultPalettes.Create();

    /// <summary>Couches (doc 17 §1) ; modèle par défaut si le projet n'en a pas encore (D28).</summary>
    public LayerSet Layers { get; private set; } = LayerSet.Default();

    /// <summary>Réglages MIDI (<c>midi.json</c>, affectations modifiées, MIDI-007) ; affectation par défaut si absents.</summary>
    public Midi.MidiSettings Midi { get; private set; } = new();

    /// <summary>Réglages de l'écran Live (<c>live.json</c>, doc 18) ; déduits des couches si absents.</summary>
    public LiveSettings Live { get; private set; } = new();

    /// <summary>Réglages des limites de sûreté (doc 02 §13, <c>sûreté.json</c>) ; valeurs par défaut si absents.</summary>
    public SafetySettings Safety { get; private set; } = new();

    /// <summary>Copie des modèles d'appareils utilisés par le projet (GEN-053) ; <c>null</c> hors projet ouvert.</summary>
    public ProjectFixtureLibrary? FixtureLibrary { get; private set; }

    /// <summary>Messages du dernier chargement (migrations, fichiers mis de côté).</summary>
    public IReadOnlyList<string> Messages { get; private set; } = [];

    /// <summary>Levé après ouverture, création ou fermeture d'un projet.</summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Levé après toute modification enregistrée de ce que joue le moteur (installation, lieux, scènes, palettes,
    /// couches, copie des modèles) : le modèle du moteur doit être recompilé (D26).
    /// </summary>
    public event EventHandler? ShowDataChanged;

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

        var (scenes, palettes, layers, safety) = LoadShowParts(folder, messages);
        var (live, liveMessage) = LiveStore.Load(folder);
        if (liveMessage is not null)
        {
            messages.Add(liveMessage);
        }

        var (midi, midiMessage) = Luxia.Midi.MidiStore.Load(folder);
        if (midiMessage is not null)
        {
            messages.Add(midiMessage);
        }

        var (looks, looksMessage) = LookStore.Load(folder);
        if (looksMessage is not null)
        {
            messages.Add(looksMessage);
        }

        Folder = folder;
        Info = report.Info;
        Console = console;
        Installation = installation;
        Venues = venues;
        Scenes = scenes;
        Palettes = palettes;
        Layers = layers;
        Safety = safety;
        Live = live;
        Midi = midi;
        Looks = looks;
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
        NotifyShowDataChanged();
    }

    /// <summary>Remplace les lieux et les enregistre (doc 13 §5).</summary>
    public void SaveVenues(VenueSet venues)
    {
        ArgumentNullException.ThrowIfNull(venues);
        var folder = Folder ?? throw new InvalidOperationException("Aucun projet ouvert.");
        VenueStore.Save(folder, venues);
        Venues = venues;
        Info = ProjectStore.Save(folder, Info!);
        NotifyShowDataChanged();
    }

    /// <summary>Remplace les scènes et les enregistre (doc 16).</summary>
    public void SaveScenes(SceneSet scenes)
    {
        ArgumentNullException.ThrowIfNull(scenes);
        var folder = Folder ?? throw new InvalidOperationException("Aucun projet ouvert.");
        SceneStore.Save(folder, scenes);
        Scenes = scenes;
        Info = ProjectStore.Save(folder, Info!);
        NotifyShowDataChanged();
    }

    /// <summary>Remplace les palettes et les enregistre (doc 17 §2).</summary>
    public void SavePalettes(PaletteSet palettes)
    {
        ArgumentNullException.ThrowIfNull(palettes);
        var folder = Folder ?? throw new InvalidOperationException("Aucun projet ouvert.");
        PaletteStore.Save(folder, palettes);
        Palettes = palettes;
        Info = ProjectStore.Save(folder, Info!);
        NotifyShowDataChanged();
    }

    /// <summary>Remplace les couches et les enregistre (doc 17 §1).</summary>
    public void SaveLayers(LayerSet layers)
    {
        ArgumentNullException.ThrowIfNull(layers);
        var folder = Folder ?? throw new InvalidOperationException("Aucun projet ouvert.");
        LayerStore.Save(folder, layers);
        Layers = layers;
        Info = ProjectStore.Save(folder, Info!);
        NotifyShowDataChanged();
    }

    /// <summary>Looks du projet (ERG-023, doc 60 §4.8).</summary>
    public LookSet Looks { get; private set; } = new();

    /// <summary>Remplace les looks et les enregistre (<c>looks.json</c>).</summary>
    public void SaveLooks(LookSet looks)
    {
        ArgumentNullException.ThrowIfNull(looks);
        var folder = Folder ?? throw new InvalidOperationException("Aucun projet ouvert.");
        LookStore.Save(folder, looks);
        Looks = looks;
        Info = ProjectStore.Save(folder, Info!);
        NotifyShowDataChanged();
    }

    /// <summary>Remplace les réglages du Live et les enregistre (doc 18).</summary>
    public void SaveLive(LiveSettings live)
    {
        ArgumentNullException.ThrowIfNull(live);
        var folder = Folder ?? throw new InvalidOperationException("Aucun projet ouvert.");
        LiveStore.Save(folder, live);
        Live = live;
        Info = ProjectStore.Save(folder, Info!);
        NotifyShowDataChanged();
    }

    /// <summary>Remplace les réglages de sûreté et les enregistre (GEN-083, GEN-084).</summary>
    public void SaveSafety(SafetySettings safety)
    {
        ArgumentNullException.ThrowIfNull(safety);
        var folder = Folder ?? throw new InvalidOperationException("Aucun projet ouvert.");
        SafetyStore.Save(folder, safety);
        Safety = safety;
        Info = ProjectStore.Save(folder, Info!);
        NotifyShowDataChanged();
    }

    /// <summary>
    /// Relit les scènes, palettes, couches et réglages de sûreté depuis le disque, sans rouvrir le projet (GEN-133) : pour reprendre du
    /// contenu écrit par une IA de conception ou à la main pendant que l'application tourne.
    /// </summary>
    /// <returns>Messages de lecture (fichiers mis de côté, migrations).</returns>
    public IReadOnlyList<string> ReloadShowData()
    {
        var folder = Folder ?? throw new InvalidOperationException("Aucun projet ouvert.");
        var messages = new List<string>();
        (Scenes, Palettes, Layers, Safety) = LoadShowParts(folder, messages);
        var (live, liveMessage) = LiveStore.Load(folder);
        var (midi, midiMessage) = Luxia.Midi.MidiStore.Load(folder);
        var (looks, looksMessage) = LookStore.Load(folder);
        Live = live;
        Midi = midi;
        Looks = looks;
        messages.AddRange(new[] { liveMessage, midiMessage, looksMessage }.OfType<string>());
        FixtureLibrary = new ProjectFixtureLibrary(folder);
        _logger.LogInformation("Scènes, palettes et couches relues : {Scenes} scènes, {Palettes} palettes", Scenes.Scenes.Count, Palettes.Palettes.Count);
        NotifyShowDataChanged();
        return messages;
    }

    /// <summary>Signale une modification de ce que joue le moteur (ex. copie d'un modèle mise à jour, GEN-053).</summary>
    public void NotifyShowDataChanged() => ShowDataChanged?.Invoke(this, EventArgs.Empty);

    private static (SceneSet, PaletteSet, LayerSet, SafetySettings) LoadShowParts(string folder, List<string> messages)
    {
        var (scenes, scenesMessage) = SceneStore.Load(folder);
        var (palettes, palettesMessage) = PaletteStore.Load(folder);
        var (layers, layersMessage) = LayerStore.Load(folder);
        var (safety, safetyMessage) = SafetyStore.Load(folder);
        messages.AddRange(new[] { scenesMessage, palettesMessage, layersMessage, safetyMessage }.OfType<string>());
        return (scenes, palettes, layers, safety);
    }
}
