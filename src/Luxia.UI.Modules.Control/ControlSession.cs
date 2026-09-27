using Luxia.Engine;
using Luxia.Fixtures.Model;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Patch.Model;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>
/// Cœur de l'écran Contrôle (doc 60 §4.1-4.2, E1, E3) : la sélection d'appareils partagée par tous les panneaux, le
/// mode d'édition, la scène et l'étape éditées, et ce qu'un réglage devient selon le mode.
/// <list type="bullet">
/// <item>LIVE : surcharge temporaire sur la sortie, gardée jusqu'à « Libérer » (F2 : elle ne cède pas à une scène).</item>
/// <item>ÉDITION : écrit dans l'étape choisie ; l'étape est montrée sur la sortie, par-dessus les scènes (C7).</item>
/// <item>AVEUGLE : écrit dans l'étape ; l'étape est envoyée au moteur d'aperçu seulement (GEN-063).</item>
/// </list>
/// Plus de programmeur-brouillon ni de « Charger / Remplacer / Fusionner » : ce qu'on règle est enregistré. Un geste
/// (glisser, molette) est gardé en mémoire et écrit une seule fois, par <see cref="Commit"/>, appelé par l'écran peu
/// après le dernier mouvement : une entrée d'annulation par geste (C7). Sans interface : testable seul.
/// </summary>
public sealed class ControlSession
{
    private readonly LuxiaRuntime _runtime;
    private readonly UndoHistory<ControlSnapshot> _history = new();
    private readonly Dictionary<RenderEngine, Dictionary<(Guid Fixture, string Key), double>> _pushed = [];
    private IReadOnlyList<SceneValue> _live = [];
    private Dictionary<(Guid Fixture, string Key), double> _liveMap = [];
    private Dictionary<(Guid Fixture, string Key), double> _stepMap = [];
    private List<Guid> _selection = [];
    private Scene? _working;
    private VenueSet? _workingVenues;
    private ControlSnapshot? _pendingBefore;
    private string _pendingDescription = string.Empty;
    private bool _saving;

    /// <summary>Branche la session sur le projet ; démarre en LIVE (C9).</summary>
    public ControlSession(LuxiaRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
        runtime.Project.Changed += (_, _) => Reset();
        runtime.Project.ShowDataChanged += (_, _) => OnShowDataChanged();
    }

    /// <summary>Levé après tout changement (mode, sélection, valeurs, scène éditée, historique).</summary>
    public event EventHandler? Changed;

    /// <summary>Mode d'édition courant.</summary>
    public EditMode Mode { get; private set; }

    /// <summary>Appareils sélectionnés (identifiants du patch), dans l'ordre de sélection.</summary>
    public IReadOnlyList<Guid> Selection => _selection;

    /// <summary>Scène choisie pour l'édition (avec les modifications du geste en cours), ou nulle.</summary>
    public Scene? EditScene => _working;

    /// <summary>Étape choisie de la scène éditée.</summary>
    public int EditStep { get; private set; }

    /// <summary>Valeurs de l'étape éditée.</summary>
    public IReadOnlyList<SceneValue> StepValues =>
        _working is { } scene && EditStep < scene.Steps.Count ? scene.Steps[EditStep].Values : [];

    /// <summary>Surcharges LIVE en cours.</summary>
    public IReadOnlyList<SceneValue> LiveValues => _live;

    /// <summary>Lieux, avec les zones du geste en cours.</summary>
    public VenueSet Venues => _workingVenues ?? _runtime.Project.Venues;

    /// <summary>Nombre d'appareils surchargés en LIVE.</summary>
    public int LiveFixtureCount => _liveMap.Keys.Select(k => k.Fixture).Distinct().Count();

    /// <summary>Un geste attend d'être écrit (<see cref="Commit"/>).</summary>
    public bool HasPendingCommit => _pendingBefore is not null;

    /// <summary>Annuler est possible.</summary>
    public bool CanUndo => _history.CanUndo || HasPendingCommit;

    /// <summary>Rétablir est possible.</summary>
    public bool CanRedo => _history.CanRedo && !HasPendingCommit;

    /// <summary>Description de ce qu'annulerait Ctrl+Z.</summary>
    public string? UndoDescription => HasPendingCommit ? _pendingDescription : _history.UndoDescription;

    /// <summary>Description de ce que rétablirait Ctrl+Y.</summary>
    public string? RedoDescription => _history.RedoDescription;

    /// <summary>Dernier message pour l'utilisateur (enregistrement refusé…), ou nul.</summary>
    public string? Message { get; private set; }

    /// <summary>Moteur dont on lit les valeurs affichées : l'aperçu en AVEUGLE, la sortie sinon.</summary>
    public RenderEngine DisplayEngine => Mode == EditMode.Blind ? _runtime.Preview : _runtime.Engine;

    /// <summary>
    /// Change de mode. Renvoie la raison d'un refus (ÉDITION et AVEUGLE demandent une scène choisie), ou nul.
    /// </summary>
    public string? SetMode(EditMode mode)
    {
        if (mode != EditMode.Live && _working is null)
        {
            return "Choisissez d'abord la scène à éditer : bande ✎ à droite d'un bouton de scène.";
        }

        Commit();
        Mode = mode;
        Push();
        return null;
    }

    /// <summary>Remplace la sélection d'appareils (plan, sélections rapides).</summary>
    public void Select(IEnumerable<Guid> fixtures)
    {
        ArgumentNullException.ThrowIfNull(fixtures);
        _selection = [.. fixtures.Distinct()];
        Notify();
    }

    /// <summary>Choisit la scène à éditer (nulle : aucune ; le mode revient alors en LIVE).</summary>
    public void ChooseScene(Guid? sceneId)
    {
        Commit();
        _working = sceneId is { } id ? _runtime.Project.Scenes.Scenes.FirstOrDefault(s => s.Id == id) : null;
        EditStep = 0;
        if (_working is null)
        {
            Mode = EditMode.Live;
        }

        Push();
    }

    /// <summary>Choisit l'étape à éditer.</summary>
    public void ChooseStep(int index)
    {
        if (_working is not { } scene)
        {
            return;
        }

        Commit();
        EditStep = Math.Clamp(index, 0, scene.Steps.Count - 1);
        Push();
    }

    /// <summary>
    /// Règle une valeur sur chaque appareil sélectionné. En ÉDITION / AVEUGLE, une couleur donnée à un appareil
    /// sans intensité dans l'étape l'allume aussi (MOT-041, « allumer en coloriant »).
    /// </summary>
    public void Apply(Func<ValueTarget, SceneValue> make, string description)
    {
        ArgumentNullException.ThrowIfNull(make);
        var values = _selection.Select(id => make(ValueTarget.Fixture(id))).ToList();
        if (values.Count == 0)
        {
            return;
        }

        var palettes = PaletteLookup();
        if (Mode == EditMode.Live)
        {
            foreach (var value in values)
            {
                _live = ProgrammerRules.Set(_live, value, palettes);
            }
        }
        else
        {
            EditStepValues(
                step =>
                {
                    foreach (var value in values)
                    {
                        step = ProgrammerRules.Set(step, value, palettes);
                    }

                    return values.Any(v => ProgrammerRules.Slot(v, palettes) == ProgrammerRules.ColorSlot)
                        ? LightColored(step, values, palettes)
                        : step;
                },
                description);
        }

        Push();
    }

    /// <summary>
    /// Retire une famille de réglages de la sélection : en LIVE, rend la main aux scènes ; en ÉDITION / AVEUGLE, la
    /// retire de l'étape (« Retirer de l'étape »). Famille nulle : tout.
    /// </summary>
    public void Remove(string? family, string description)
    {
        var keys = _selection.Select(id => ProgrammerRules.TargetKey(ValueTarget.Fixture(id))).ToHashSet();
        var palettes = PaletteLookup();
        IReadOnlyList<SceneValue> Without(IReadOnlyList<SceneValue> values) => family is null
            ? [.. values.Where(v => !keys.Contains(ProgrammerRules.TargetKey(v.Target)))]
            : ProgrammerRules.Remove(values, keys, family, palettes);

        if (Mode == EditMode.Live)
        {
            _live = Without(_live);
        }
        else
        {
            EditStepValues(Without, description);
        }

        Push();
    }

    /// <summary>« Libérer tout » : retire toutes les surcharges LIVE.</summary>
    public void ReleaseAll()
    {
        _live = [];
        Push();
    }

    /// <summary>
    /// Modifie la scène éditée (nom, lecture, étapes…) : geste écrit par <see cref="Commit"/>, annulable.
    /// <paramref name="step"/> donne l'étape choisie après la modification (ajout, déplacement, suppression d'étape).
    /// </summary>
    public void UpdateScene(Func<Scene, Scene> change, string description, int? step = null)
    {
        ArgumentNullException.ThrowIfNull(change);
        if (_working is not { } scene)
        {
            return;
        }

        BeginGesture(description);
        _working = change(scene);
        EditStep = Math.Clamp(step ?? EditStep, 0, Math.Max(_working.Steps.Count - 1, 0));
        Push();
    }

    /// <summary>
    /// Modifie l'ensemble des scènes (nouvelle, dupliquer, supprimer, déplacer de couche) : écrit tout de suite,
    /// une entrée d'annulation. La scène éditée est relue (et abandonnée si elle a disparu).
    /// </summary>
    public void ChangeScenes(Func<SceneSet, SceneSet> change, string description)
    {
        ArgumentNullException.ThrowIfNull(change);
        Commit();
        var before = Snapshot();
        if (Save(change(_runtime.Project.Scenes), null))
        {
            _history.Record(before, description);
        }

        ReloadWorking();
        Push();
    }

    /// <summary>Modifie les zones du lieu actif (geste en cours, écrit par <see cref="Commit"/>).</summary>
    public void EditVenues(Func<VenueSet, VenueSet> change, string description)
    {
        ArgumentNullException.ThrowIfNull(change);
        BeginGesture(description);
        _workingVenues = change(Venues);
        Notify();
    }

    /// <summary>Écrit le geste en cours (scène, zones) dans le projet ; une entrée d'annulation.</summary>
    public void Commit()
    {
        if (_pendingBefore is not { } before)
        {
            return;
        }

        var project = _runtime.Project;
        var scenes = _working is { } working && !ReferenceEquals(StoredScene(working.Id), working)
            ? project.Scenes with { Scenes = [.. project.Scenes.Scenes.Select(s => s.Id == working.Id ? working : s)] }
            : null;
        if (Save(scenes, _workingVenues))
        {
            _history.Record(before, _pendingDescription);
        }

        _pendingBefore = null;
        _workingVenues = null;
        Notify();
    }

    /// <summary>Annule le dernier geste (Ctrl+Z).</summary>
    public void Undo()
    {
        if (HasPendingCommit)
        {
            Commit();
        }

        if (_history.Undo(Snapshot()) is { } previous)
        {
            Restore(previous);
        }
    }

    /// <summary>Rétablit le dernier geste annulé (Ctrl+Y).</summary>
    public void Redo()
    {
        if (!HasPendingCommit && _history.Redo(Snapshot()) is { } next)
        {
            Restore(next);
        }
    }

    /// <summary>Pastille d'un attribut d'un appareil (doc 60 §4.3).</summary>
    public ParameterState StateOf(FixtureInfo fixture, AttributeKind attribute)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        var keys = ValueResolver.Keys(fixture, 0, attribute).ToList();
        if (keys.Count == 0)
        {
            return ParameterState.Unused;
        }

        if (Mode != EditMode.Live)
        {
            return keys.Any(k => _stepMap.ContainsKey((fixture.ReferenceId, k))) ? ParameterState.InScene : ParameterState.Unused;
        }

        if (keys.Any(k => _liveMap.ContainsKey((fixture.ReferenceId, k))))
        {
            return ParameterState.LiveOverride;
        }

        var snapshot = _runtime.Engine.Snapshot;
        foreach (var key in keys)
        {
            var index = snapshot.Show.IndexOf(fixture.ReferenceId, key);
            if (index >= 0 && index < snapshot.Sources.Length && snapshot.Sources[index].Kind == SourceKind.Scene)
            {
                return ParameterState.InScene;
            }
        }

        return ParameterState.Unused;
    }

    /// <summary>Niveau affiché d'un attribut (0-1), lu au moteur affiché ; nul si l'appareil ne l'a pas.</summary>
    public double? LevelOf(FixtureInfo fixture, AttributeKind attribute)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        var key = ValueResolver.Keys(fixture, 0, attribute).FirstOrDefault();
        if (key is null)
        {
            return null;
        }

        var snapshot = DisplayEngine.Snapshot;
        var index = snapshot.Show.IndexOf(fixture.ReferenceId, key);
        return index >= 0 && index < snapshot.Values.Length ? snapshot.Values[index] : 0;
    }

    /// <summary>Relâche tout ce qui a été envoyé aux moteurs (fermeture de l'écran ou du projet).</summary>
    public void ReleasePushed()
    {
        foreach (var (engine, map) in _pushed)
        {
            foreach (var fixture in map.Keys.Select(k => k.Fixture).Distinct())
            {
                engine.Send(new ReleaseAttributesCommand(CommandOrigin.User, fixture, [.. map.Keys.Where(k => k.Fixture == fixture).Select(k => k.Key)]));
            }
        }

        _pushed.Clear();
    }

    private void Reset()
    {
        ReleasePushed();
        _live = [];
        _liveMap = [];
        _stepMap = [];
        _working = null;
        _workingVenues = null;
        _pendingBefore = null;
        _selection = [];
        EditStep = 0;
        Mode = EditMode.Live;
        Message = null;
        _history.Clear();
        _runtime.PreviewActive = false;
        Notify();
    }

    private void OnShowDataChanged()
    {
        if (_saving || HasPendingCommit)
        {
            return;
        }

        // Scènes modifiées ailleurs (autre écran, relecture des fichiers) : la scène éditée est relue.
        if (_working is { } working)
        {
            _working = StoredScene(working.Id);
            if (_working is null)
            {
                Mode = EditMode.Live;
                EditStep = 0;
            }
            else
            {
                EditStep = Math.Clamp(EditStep, 0, _working.Steps.Count - 1);
            }
        }

        Push();
    }

    private void BeginGesture(string description)
    {
        if (_pendingBefore is null)
        {
            _pendingBefore = Snapshot();
            _pendingDescription = description;
        }
    }

    private void EditStepValues(Func<IReadOnlyList<SceneValue>, IReadOnlyList<SceneValue>> change, string description)
    {
        if (_working is not { } scene || EditStep >= scene.Steps.Count)
        {
            return;
        }

        BeginGesture(description);
        var index = EditStep;
        _working = scene with { Steps = [.. scene.Steps.Select((s, i) => i == index ? s with { Values = change(s.Values) } : s)] };
    }

    /// <summary>MOT-041 : un appareil qui reçoit une couleur sans intensité dans l'étape reçoit 100 %.</summary>
    private static List<SceneValue> LightColored(IReadOnlyList<SceneValue> step, IReadOnlyList<SceneValue> colored, Func<Guid, Palette?> palettes)
    {
        var lit = step.Where(v => ProgrammerRules.Slot(v, palettes) == ProgrammerRules.IntensitySlot).Select(v => ProgrammerRules.TargetKey(v.Target)).ToHashSet();
        var result = step.ToList();
        foreach (var value in colored)
        {
            if (lit.Add(ProgrammerRules.TargetKey(value.Target)))
            {
                result.Add(new SceneValue { Target = value.Target, Attribute = AttributeKind.Intensity, Level = 1 });
            }
        }

        return result;
    }

    private ControlSnapshot Snapshot() => new(_runtime.Project.Scenes, _runtime.Project.Venues);

    private Scene? StoredScene(Guid id) => _runtime.Project.Scenes.Scenes.FirstOrDefault(s => s.Id == id);

    private void Restore(ControlSnapshot snapshot)
    {
        var project = _runtime.Project;
        Save(
            ReferenceEquals(snapshot.Scenes, project.Scenes) ? null : snapshot.Scenes,
            ReferenceEquals(snapshot.Venues, project.Venues) ? null : snapshot.Venues);
        ReloadWorking();
        Push();
    }

    private void ReloadWorking()
    {
        if (_working is { } working)
        {
            _working = StoredScene(working.Id);
            EditStep = _working is null ? 0 : Math.Clamp(EditStep, 0, _working.Steps.Count - 1);
            if (_working is null)
            {
                Mode = EditMode.Live;
            }
        }
    }

    /// <summary>Écrit dans le projet ; un refus passager du disque est dit en clair (doc 03 §11), pas levé.</summary>
    private bool Save(SceneSet? scenes, VenueSet? venues)
    {
        _saving = true;
        try
        {
            if (scenes is not null)
            {
                _runtime.Project.SaveScenes(scenes);
            }

            if (venues is not null)
            {
                _runtime.Project.SaveVenues(venues);
            }

            Message = null;
            return scenes is not null || venues is not null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Message = $"Enregistrement impossible pour l'instant ({ex.Message}). Refaites le réglage dans un instant.";
            return false;
        }
        finally
        {
            _saving = false;
        }
    }

    /// <summary>Envoie aux moteurs ce que le mode demande ; seuls les écarts avec le dernier envoi partent.</summary>
    private void Push()
    {
        var resolver = new ValueResolver(_runtime.Show.Patch, _runtime.Project.Palettes);
        _liveMap = Resolve(resolver, _live);
        _stepMap = Mode == EditMode.Live ? [] : Resolve(resolver, StepValues);
        var output = new Dictionary<(Guid, string), double>(_liveMap);
        var preview = new Dictionary<(Guid, string), double>();
        foreach (var (key, level) in _stepMap)
        {
            if (Mode == EditMode.Edit)
            {
                output[key] = level;
            }
            else
            {
                preview[key] = level;
            }
        }

        PushTo(_runtime.Engine, output);
        PushTo(_runtime.Preview, preview);
        _runtime.PreviewActive = Mode == EditMode.Blind;
        Notify();
    }

    private void PushTo(RenderEngine engine, Dictionary<(Guid Fixture, string Key), double> next)
    {
        var previous = _pushed.GetValueOrDefault(engine) ?? [];
        var released = previous.Keys.Where(k => !next.ContainsKey(k)).ToList();
        foreach (var fixture in released.Select(k => k.Fixture).Distinct())
        {
            engine.Send(new ReleaseAttributesCommand(CommandOrigin.User, fixture, [.. released.Where(k => k.Fixture == fixture).Select(k => k.Key)]));
        }

        var changed = next.Where(kv => !previous.TryGetValue(kv.Key, out var old) || Math.Abs(old - kv.Value) > 1e-9).ToList();
        if (changed.Count > 0)
        {
            engine.Send(new OverrideAttributesCommand(CommandOrigin.User, [.. changed.Select(kv => new AttributeValue(kv.Key.Fixture, kv.Key.Key, kv.Value))]));
        }

        _pushed[engine] = next;
    }

    /// <summary>Valeurs résolues par appareil et canal, dans l'ordre de la compilation : sélections, appareils, cellules.</summary>
    private static Dictionary<(Guid Fixture, string Key), double> Resolve(ValueResolver resolver, IReadOnlyList<SceneValue> values)
    {
        var map = new Dictionary<(Guid, string), double>();
        foreach (var value in values.Select((v, i) => (v, i)).OrderBy(x => x.v.Target.FixtureId is null ? 0 : x.v.Target.Cell == 0 ? 1 : 2).ThenBy(x => x.i).Select(x => x.v))
        {
            foreach (var resolved in resolver.Resolve(value, out _))
            {
                map[(resolved.FixtureId, resolved.ChannelKey)] = resolved.Level;
            }
        }

        return map;
    }

    private Func<Guid, Palette?> PaletteLookup()
    {
        var map = _runtime.Project.Palettes.Palettes.ToDictionary(p => p.Id);
        return id => map.GetValueOrDefault(id);
    }

    private void Notify() => Changed?.Invoke(this, EventArgs.Empty);
}
