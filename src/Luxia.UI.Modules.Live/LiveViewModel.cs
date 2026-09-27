using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Engine;
using Luxia.Engine.Model;
using Luxia.Fixtures.Model;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Messaging.Events;
using Luxia.Patch.Rules;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Live;

/// <summary>
/// Écran Live (doc 18) : jouer en soirée, d'une main, dans le noir. Colonnes de couches (LIVE-002, LIVE-003), actions
/// permanentes (LIVE-004 ; blackout et Grand Master restent dans l'en-tête de la fenêtre), palettes rapides (LIVE-005),
/// bandeau d'état avec limites de sûreté et alerte de sortie (LIVE-001, LIVE-008, LIVE-010), journal (LIVE-009, GEN-112),
/// raccourcis (LIVE-040). Toute action passe par une commande d'origine « Utilisateur » (GEN-070). Aucune fenêtre modale.
/// </summary>
public sealed partial class LiveViewModel : ViewModelBase, IRefreshable
{
    private const int JournalSize = 60;

    private readonly LuxiaRuntime _runtime;
    private readonly ConcurrentQueue<(string? Group, string Label, string Text)> _incoming = new();
    private readonly HashSet<(Guid Fixture, string Key)> _quickOverrides = [];
    private readonly HashSet<LiveKey> _held = [];
    private Guid? _flashScene;
    private Guid? _strobeScene;

    [ObservableProperty]
    private bool _hasProject;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private bool _outputAlert;

    [ObservableProperty]
    private string _outputColor = "#30363D";

    [ObservableProperty]
    private string _limitsDetail = string.Empty;

    [ObservableProperty]
    private double _columnsMinWidth;

    [ObservableProperty]
    private string _controllersText = string.Empty;

    [ObservableProperty]
    private bool _frozen;

    [ObservableProperty]
    private bool _smoking;

    [ObservableProperty]
    private string _venueText = string.Empty;

    [ObservableProperty]
    private string _limitsText = string.Empty;

    [ObservableProperty]
    private bool _hasLimits;

    [ObservableProperty]
    private string _calibrationText = string.Empty;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private bool _showCommands;

    [ObservableProperty]
    private bool _hasQuickOverrides;

    /// <summary>Crée l'écran.</summary>
    public LiveViewModel(LuxiaRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
        runtime.Project.Changed += (_, _) => Rebuild();
        runtime.Show.Compiled += (_, _) => Rebuild();

        // LIVE-009 : journal alimenté par les événements (reçus sur le fil du bus, affichés au rafraîchissement).
        runtime.Bus.Subscribe<SceneStarted>(e => Log($"▶ {e.SceneName} ({Origin(e.Origin)})"));
        runtime.Bus.Subscribe<SceneStopped>(e => Log($"■ {e.SceneName}"));
        runtime.Bus.Subscribe<SafetyLimitReached>(e =>
        {
            // Plusieurs appareils limités au même instant pour la même raison : une seule ligne au journal.
            var fixture = e.Label.Split(" – ")[0];
            _incoming.Enqueue(($"{e.Kind}|{e.Detail}", fixture, $"{DateTime.Now:HH:mm:ss}  ⚠ {{0}} : {e.Detail}"));
        });
        runtime.Bus.Subscribe<OutputStateChanged>(e => Log($"⇄ {e.DriverName} : {OutputState(e.State)}{(e.Message is { } m ? $" ({m})" : string.Empty)}"));
        runtime.Bus.Subscribe<CommandRejected>(e => Log($"✕ {e.Command.GetType().Name.Replace("Command", string.Empty, StringComparison.Ordinal)} refusée : {e.Reason}"));
        Rebuild();
    }

    /// <summary>Colonnes de couches.</summary>
    public ObservableCollection<LayerColumnViewModel> Columns { get; } = [];

    /// <summary>Sélections des palettes rapides.</summary>
    public ObservableCollection<QuickSelectionViewModel> QuickSelections { get; } = [];

    /// <summary>Palettes couleur rapides.</summary>
    public ObservableCollection<QuickPaletteViewModel> Colors { get; } = [];

    /// <summary>Palettes de position rapides.</summary>
    public ObservableCollection<QuickPaletteViewModel> Positions { get; } = [];

    /// <summary>Palettes d'intensité rapides.</summary>
    public ObservableCollection<QuickPaletteViewModel> Intensities { get; } = [];

    /// <summary>Journal des derniers événements (le plus récent en haut).</summary>
    public ObservableCollection<string> Journal { get; } = [];

    /// <summary>Journal des dernières commandes, avec leur origine (GEN-112).</summary>
    public ObservableCollection<string> Commands { get; } = [];

    /// <summary>Le bouton FLASH a une scène.</summary>
    public bool HasFlash => _flashScene is not null;

    /// <summary>Le bouton STROBE a une scène.</summary>
    public bool HasStrobe => _strobeScene is not null;

    /// <summary>Rafraîchi seulement quand l'écran est affiché.</summary>
    public bool NeedsBackgroundRefresh => false;

    /// <inheritdoc />
    public void Refresh()
    {
        var snapshot = _runtime.Engine.Snapshot;
        foreach (var column in Columns)
        {
            foreach (var button in column.Scenes)
            {
                var playback = FindPlayback(snapshot, button.Scene.Id);
                if (!button.WaitingForEngine(_runtime.Engine.TickCount))
                {
                    button.IsActive = playback is not null;
                }

                button.Progress = playback?.StepProgress ?? 0;
                button.State = playback is { StepCount: > 1 } p
                    ? string.Create(CultureInfo.CurrentCulture, $"{p.StepIndex + 1}/{p.StepCount}")
                    : string.Empty;
            }

            var index = IndexOfLayer(snapshot.Show, column.Layer.Id);
            if (index >= 0 && index < snapshot.LayerMasters.Length)
            {
                column.SyncMaster(Math.Round(snapshot.LayerMasters[index] * 100));
            }
        }

        Frozen = snapshot.Frozen;
        Smoking = snapshot.Smoking;
        RefreshOutput();
        RefreshLimits(snapshot);
        DrainJournal();
        if (ShowCommands)
        {
            RefreshCommands();
        }
    }

    /// <summary>Appui sur une scène (LIVE-003) : lancer, arrêter / relancer, ou flash maintenu dans une couche Flash.</summary>
    public void Press(LiveSceneViewModel scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        _runtime.TraceUi("Live", $"appui « {scene.Name} »{(scene.IsActive ? " (affichée active)" : string.Empty)}");
        if (scene.Column.IsFlash)
        {
            Send(new FlashSceneCommand(CommandOrigin.User, scene.Scene.Id, Pressed: true));
            return;
        }

        // Le moteur tranche « lancer ou arrêter » (course écran / moteur, vécue à l'essai P5) ; l'écran affiche tout de
        // suite l'état attendu et ne le relit du moteur qu'une fois la commande traitée.
        var toggle = _runtime.Project.Live.ActiveSceneClick == ActiveSceneClick.Stop;
        Send(new LaunchSceneCommand(CommandOrigin.User, scene.Scene.Id, StopIfPlaying: toggle));
        scene.ExpectActive(!toggle || !scene.IsActive, _runtime.Engine.TickCount);
    }

    /// <summary>Relâche d'une scène : fin du flash pour une couche Flash.</summary>
    public void Release(LiveSceneViewModel scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        _runtime.TraceUi("Live", $"relâche « {scene.Name} »");
        if (scene.Column.IsFlash)
        {
            Send(new FlashSceneCommand(CommandOrigin.User, scene.Scene.Id, Pressed: false));
        }
    }

    /// <summary>FLASH général maintenu (LIVE-004, touche F).</summary>
    public void Flash(bool pressed)
    {
        _runtime.TraceUi("Live", pressed ? "FLASH appuyé" : "FLASH relâché");
        if (_flashScene is { } id)
        {
            Send(new FlashSceneCommand(CommandOrigin.User, id, pressed));
        }
    }

    /// <summary>STROBE général maintenu (LIVE-004, touche S) : toujours borné par le limiteur (MOT-080).</summary>
    public void Strobe(bool pressed)
    {
        _runtime.TraceUi("Live", pressed ? "STROBE appuyé" : "STROBE relâché");
        if (_strobeScene is { } id)
        {
            Send(new FlashSceneCommand(CommandOrigin.User, id, pressed));
        }
    }

    /// <summary>FUMÉE maintenue (LIVE-004, touche Z) : toujours bornée par le limiteur (GEN-084).</summary>
    public void Smoke(bool pressed)
    {
        _runtime.TraceUi("Live", pressed ? "FUMÉE appuyé" : "FUMÉE relâché");
        if (!HasSmoke)
        {
            Message = pressed ? "Aucune machine à fumée dans le patch." : Message;
            return;
        }

        Send(new SmokeCommand(CommandOrigin.User, pressed));
    }

    /// <summary>Le patch contient une machine à fumée.</summary>
    public bool HasSmoke => _runtime.Engine.Snapshot.Show.Safety.SmokeChannels.Count > 0 || _runtime.Show.Last?.Model.Safety.SmokeChannels.Count > 0;

    /// <summary>
    /// Raccourci clavier (LIVE-040) : <paramref name="down"/> à l'appui, <c>false</c> au relâchement. Renvoie <c>true</c> si
    /// la touche est prise en charge. La répétition automatique d'une touche maintenue est ignorée.
    /// </summary>
    public bool OnKey(LiveKey key, bool down)
    {
        if (down && !_held.Add(key))
        {
            return true;
        }

        if (!down)
        {
            _held.Remove(key);
        }

        switch (key)
        {
            case LiveKey.Flash:
                Flash(down);
                return true;
            case LiveKey.Strobe:
                Strobe(down);
                return true;
            case LiveKey.Smoke:
                Smoke(down);
                return true;
        }

        if (!down)
        {
            // Relâche d'une scène au clavier : fin du flash pour une couche Flash.
            if (key >= LiveKey.Scene1 && SceneFor(key) is { } released)
            {
                Release(released);
            }

            return key >= LiveKey.Scene1 || key is LiveKey.Freeze or LiveKey.Release or LiveKey.PreviousLayer or LiveKey.NextLayer or LiveKey.MasterUp or LiveKey.MasterDown;
        }

        switch (key)
        {
            case LiveKey.Freeze:
                ToggleFreeze();
                break;
            case LiveKey.Release:
                ReleaseQuick();
                break;
            case LiveKey.PreviousLayer:
                MoveSelection(-1);
                break;
            case LiveKey.NextLayer:
                MoveSelection(1);
                break;
            case LiveKey.MasterUp or LiveKey.MasterDown:
                var level = _runtime.Engine.Snapshot.GrandMaster + (key == LiveKey.MasterUp ? 0.1 : -0.1);
                _runtime.SetGrandMaster(Math.Clamp(Math.Round(level, 2), 0, 1));
                break;
            default:
                if (SceneFor(key) is { } pressed)
                {
                    Press(pressed);
                }

                break;
        }

        return true;
    }

    /// <summary>Figer / dégeler (LIVE-004, touche G).</summary>
    [RelayCommand]
    private void ToggleFreeze() => Send(new FreezeCommand(CommandOrigin.User, !_runtime.Engine.Snapshot.Frozen));

    /// <summary>Rafale de fumée (LIVE-004).</summary>
    [RelayCommand]
    private void SmokeBurst()
    {
        if (HasSmoke)
        {
            Send(new SmokeCommand(CommandOrigin.User, false, TimeSpan.FromSeconds(Math.Clamp(_runtime.Project.Live.SmokeBurstSeconds, 0.5, 60))));
        }
    }

    /// <summary>Tout arrêter, sauf les couches protégées (Ambiance) (COU-007).</summary>
    [RelayCommand]
    private void StopAll() => Send(new StopLayerCommand(CommandOrigin.User));

    /// <summary>Arrêter une couche (LIVE-002).</summary>
    [RelayCommand]
    private void StopLayer(LayerColumnViewModel? column)
    {
        if (column is not null)
        {
            Send(new StopLayerCommand(CommandOrigin.User, column.Layer.Id));
        }
    }

    /// <summary>Choisit la sélection des palettes rapides.</summary>
    [RelayCommand]
    private void SelectQuick(QuickSelectionViewModel? selection)
    {
        foreach (var item in QuickSelections)
        {
            item.IsSelected = ReferenceEquals(item, selection);
        }
    }

    /// <summary>Palette rapide (LIVE-005) : surcharge les attributs de la sélection jusqu'à « Libérer ».</summary>
    [RelayCommand]
    private void ApplyPalette(QuickPaletteViewModel? palette)
    {
        var selection = QuickSelections.FirstOrDefault(s => s.IsSelected);
        if (palette is null || selection is null)
        {
            Message = "Choisissez d'abord une sélection (à gauche des palettes).";
            return;
        }

        var resolver = new ValueResolver(_runtime.Show.Patch, _runtime.Project.Palettes);
        var resolved = resolver.Resolve(new SceneValue { Target = selection.Target, PaletteId = palette.Palette.Id }, out var problem);
        if (resolved.Count == 0)
        {
            Message = problem ?? $"« {palette.Name} » ne s'applique à aucun appareil de « {selection.Label} ».";
            return;
        }

        Message = null;
        Send(new OverrideAttributesCommand(CommandOrigin.User, [.. resolved.Select(r => new AttributeValue(r.FixtureId, r.ChannelKey, r.Level))]));
        foreach (var value in resolved)
        {
            _quickOverrides.Add((value.FixtureId, value.ChannelKey));
        }

        HasQuickOverrides = true;
    }

    /// <summary>Libérer (LIVE-005, touche Échap) : rend la main aux couches pour ce que les palettes rapides ont forcé.</summary>
    [RelayCommand]
    private void ReleaseQuick()
    {
        foreach (var group in _quickOverrides.GroupBy(o => o.Fixture))
        {
            Send(new ReleaseAttributesCommand(CommandOrigin.User, group.Key, [.. group.Select(o => o.Key)]));
        }

        _quickOverrides.Clear();
        HasQuickOverrides = false;
    }

    private void Send(Command command) => _runtime.Engine.Send(command);

    private LiveSceneViewModel? SceneFor(LiveKey key)
    {
        var column = Columns.FirstOrDefault(c => c.IsSelected) ?? Columns.FirstOrDefault();
        var index = key - LiveKey.Scene1;
        return column is not null && index >= 0 && index < column.Scenes.Count ? column.Scenes[index] : null;
    }

    private void MoveSelection(int delta)
    {
        if (Columns.Count == 0)
        {
            return;
        }

        var current = Columns.ToList().FindIndex(c => c.IsSelected);
        var next = Math.Clamp((current < 0 ? 0 : current) + delta, 0, Columns.Count - 1);
        for (var i = 0; i < Columns.Count; i++)
        {
            Columns[i].IsSelected = i == next;
        }
    }

    private void Rebuild()
    {
        var project = _runtime.Project;
        HasProject = project.Folder is not null;
        var selectedLayer = Columns.FirstOrDefault(c => c.IsSelected)?.Layer.Id;
        Columns.Clear();
        var live = project.Live;
        foreach (var (layer, scenes) in LiveRules.Columns(live, project.Layers, project.Scenes))
        {
            var column = new LayerColumnViewModel(layer, (c, value) => Send(new SetLayerMasterCommand(CommandOrigin.User, c.Layer.Id, value / 100)));
            var index = 1;
            foreach (var scene in scenes)
            {
                column.Scenes.Add(new LiveSceneViewModel(scene, column, index++));
            }

            column.IsSelected = layer.Id == selectedLayer;
            Columns.Add(column);
        }

        if (Columns.Count > 0 && !Columns.Any(c => c.IsSelected))
        {
            Columns[0].IsSelected = true;
        }

        // Les colonnes se partagent la largeur, sans descendre sous 110 px (défilement horizontal au-delà).
        ColumnsMinWidth = Columns.Count * 110;

        // Scènes des boutons FLASH et STROBE : réglées dans live.json, sinon déduites de la couche Flash.
        (_flashScene, _strobeScene) = LiveRules.PermanentScenes(live, project.Layers, project.Scenes);
        OnPropertyChanged(nameof(HasFlash));
        OnPropertyChanged(nameof(HasStrobe));

        RebuildQuick();
        VenueText = $"Lieu : {project.Venues.Active.Name}";
        var uncalibrated = _runtime.Show.Last?.Issues.Count(i => i.Message.Contains("PAL-008", StringComparison.Ordinal)) ?? 0;
        CalibrationText = uncalibrated == 0
            ? string.Empty
            : string.Create(CultureInfo.CurrentCulture, $"{uncalibrated} palette(s) de position non calibrée(s) ici (repli Générique)");
    }

    private void RebuildQuick()
    {
        var selected = QuickSelections.FirstOrDefault(s => s.IsSelected)?.Label;
        QuickSelections.Clear();
        var patch = _runtime.Show.Patch;
        var present = patch.Fixtures.Where(f => !f.Absent).Select(f => f.Fixture).ToList();
        var library = _runtime.Project.FixtureLibrary;
        foreach (var auto in AutoSelections.Build(present, f => library?.Find(f.FixtureTypeId)).Where(a => a.Kind != AutoSelectionKind.ByModel))
        {
            var target = new ValueTarget { Auto = new AutoSelectionTarget(auto.Kind, auto.Category, auto.ModelDisplayName) };
            QuickSelections.Add(new QuickSelectionViewModel(auto.Title(CategoryLabel), target, "#8B949E"));
        }

        foreach (var selection in _runtime.Project.Installation.Selections)
        {
            QuickSelections.Add(new QuickSelectionViewModel(selection.Name, ValueTarget.Selection(selection.Id), selection.Color));
        }

        SelectQuick(QuickSelections.FirstOrDefault(s => s.Label == selected) ?? QuickSelections.FirstOrDefault());

        Colors.Clear();
        Positions.Clear();
        Intensities.Clear();
        foreach (var palette in _runtime.Project.Palettes.Palettes)
        {
            var list = palette.Kind switch
            {
                PaletteKind.Color => Colors,
                PaletteKind.Position => Positions,
                PaletteKind.Intensity => Intensities,
                _ => null,
            };
            list?.Add(new QuickPaletteViewModel(palette));
        }
    }

    private void RefreshOutput()
    {
        // Contrôleurs MIDI branchés (MIDI-001, MIDI-006) : visibles d'un coup d'œil.
        var controllers = _runtime.Midi?.Connected ?? [];
        ControllersText = controllers.Count == 0 ? string.Empty : "🎛 " + string.Join(", ", controllers);

        var main = _runtime.Router.Routes.FirstOrDefault(r => r.Driver.Id != Output.Drivers.RecorderOutputDriver.DriverId);
        if (main.Driver is null)
        {
            OutputText = "Aucune sortie : jeu au simulateur seulement";
            OutputAlert = false;
            OutputColor = "#30363D";
            return;
        }

        var status = main.Driver.Status;
        OutputAlert = status.State != OutputConnectionState.Connected;
        OutputColor = OutputAlert ? "#DA3633" : "#1A7F37";
        OutputText = status.State switch
        {
            OutputConnectionState.Connected => $"● {main.Driver.Name} connecté",
            OutputConnectionState.Connecting => $"⚠ {main.Driver.Name} : connexion en cours…",
            OutputConnectionState.Error => $"⚠ SORTIE EN ERREUR ({main.Driver.Name}) : reconnexion automatique",
            _ => $"⚠ SORTIE DÉCONNECTÉE ({main.Driver.Name}) : rebranchez, reconnexion automatique",
        };
    }

    private void RefreshLimits(EngineSnapshot snapshot)
    {
        // Pastille courte (LIVE-008) : une phrase par nature de limite ; le détail par appareil est dans l'info-bulle.
        var limits = snapshot.ActiveLimits.DistinctBy(l => (l.Kind, l.FixtureId)).ToList();
        var parts = new List<string>();
        if (snapshot.Show.Safety.Strobe.Forbidden)
        {
            parts.Add("strobe interdit");
        }

        foreach (var group in limits.GroupBy(l => l.Kind))
        {
            var names = group.Select(l => l.Label.Split(" – ")[0]).Distinct().ToList();
            var who = names.Count <= 2 ? string.Join(", ", names) : string.Create(CultureInfo.CurrentCulture, $"{names.Count} appareils");

            // Temps avant la levée (retour d'essai P5 : « 30 s, c'est long ») : le plus long du groupe, arrondi au-dessus.
            var remaining = group.Max(l => l.RemainingSeconds);
            var left = remaining is { } seconds ? string.Create(CultureInfo.CurrentCulture, $" (encore {Math.Ceiling(seconds):0} s)") : string.Empty;
            parts.Add(group.Key switch
            {
                SafetyLimitKind.Strobe => $"strobe limité : {who}{left}",
                SafetyLimitKind.Smoke => $"fumée en repos : {who}{left}",
                _ => $"zone interdite : {who}",
            });
        }

        HasLimits = parts.Count > 0;
        LimitsText = HasLimits ? "⚠ " + string.Join("  ·  ", parts) : "Sûreté : aucune limite active";
        LimitsDetail = limits.Count == 0 ? "Aucune limite de sûreté n'agit en ce moment." : string.Join(Environment.NewLine, limits.Select(l => $"{l.Label} : {l.Detail}"));
    }

    private void RefreshCommands()
    {
        var log = _runtime.Engine.CommandLog();
        Commands.Clear();
        for (var i = log.Count - 1; i >= 0 && Commands.Count < 40; i--)
        {
            var entry = log[i];
            var name = entry.Command.GetType().Name.Replace("Command", string.Empty, StringComparison.Ordinal);
            var repeat = entry.Repeat > 1 ? string.Create(CultureInfo.CurrentCulture, $" ×{entry.Repeat}") : string.Empty;
            Commands.Add(string.Create(
                CultureInfo.CurrentCulture,
                $"{entry.AppliedAt:hh\\:mm\\:ss}  {name}{repeat} ({Origin(entry.Command.Origin)}){(entry.Rejection is { } r ? $" — refusée : {r}" : string.Empty)}"));
        }
    }

    private void DrainJournal()
    {
        var lines = new List<string>();
        string? group = null;
        var names = new List<string>();
        var format = string.Empty;
        void Flush()
        {
            if (group is not null)
            {
                lines.Add(string.Format(CultureInfo.CurrentCulture, format, string.Join(", ", names.Distinct())));
            }

            group = null;
            names.Clear();
        }

        while (_incoming.TryDequeue(out var item))
        {
            if (item.Group is not null && item.Group == group)
            {
                names.Add(item.Label);
                continue;
            }

            Flush();
            if (item.Group is null)
            {
                lines.Add(item.Text);
                continue;
            }

            group = item.Group;
            format = item.Text;
            names.Add(item.Label);
        }

        Flush();
        foreach (var line in lines)
        {
            Journal.Insert(0, line);
        }

        while (Journal.Count > JournalSize)
        {
            Journal.RemoveAt(Journal.Count - 1);
        }
    }

    private void Log(string text) => _incoming.Enqueue((null, string.Empty, $"{DateTime.Now:HH:mm:ss}  {text}"));

    private static PlaybackInfo? FindPlayback(EngineSnapshot snapshot, Guid sceneId)
    {
        PlaybackInfo? found = null;
        foreach (var playback in snapshot.Playbacks)
        {
            if (playback.SceneId == sceneId && playback.State is not (PlaybackState.FadingOut or PlaybackState.Done))
            {
                found = playback;
            }
        }

        return found;
    }

    private static int IndexOfLayer(ShowModel show, Guid id)
    {
        for (var i = 0; i < show.Layers.Count; i++)
        {
            if (show.Layers[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }

    private static string Origin(CommandOrigin origin) => origin switch
    {
        CommandOrigin.User => "utilisateur",
        CommandOrigin.Midi => "MIDI",
        CommandOrigin.Director => "Directeur",
        CommandOrigin.Show => "show",
        CommandOrigin.Timeline => "timeline",
        CommandOrigin.Remote => "télécommande",
        _ => "outil",
    };

    private static string OutputState(OutputConnectionState state) => state switch
    {
        OutputConnectionState.Connected => "connectée",
        OutputConnectionState.Connecting => "connexion…",
        OutputConnectionState.Error => "en erreur",
        _ => "déconnectée",
    };

    private static string CategoryLabel(FixtureCategory category) => category switch
    {
        FixtureCategory.Par => "PAR",
        FixtureCategory.LedBar => "barres LED",
        FixtureCategory.MovingHead => "lyres",
        FixtureCategory.Effect => "effets",
        FixtureCategory.Strobe => "stroboscopes",
        FixtureCategory.Uv => "UV",
        FixtureCategory.Smoke => "machines à fumée",
        FixtureCategory.Laser => "lasers",
        FixtureCategory.Dimmer => "gradateurs",
        _ => "autres",
    };
}
