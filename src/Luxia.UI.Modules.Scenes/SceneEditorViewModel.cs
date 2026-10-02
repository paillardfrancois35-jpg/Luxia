using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Engine.Model;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;
using Luxia.UI.Controls;

using Luxia.UI.Modules.Control;

namespace Luxia.UI.Modules.Scenes;

/// <summary>
/// Éditeur d'une scène (doc 16 §2-4) : identité (nom, couleur, icône, catégorie, couche, visible en Live), paramètres
/// de lecture, étapes et enregistrement du programmeur dans les étapes. Chaque modification est enregistrée tout de
/// suite (comme l'installation) ; l'écran garde l'historique pour annuler / rétablir (SCN-039).
/// </summary>
public sealed partial class SceneEditorViewModel : ViewModelBase
{
    private readonly LuxiaRuntime _runtime;
    private readonly ScenesViewModel _owner;
    private bool _loading;

    [ObservableProperty]
    private bool _hasScene;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _color = "#58A6FF";

    [ObservableProperty]
    private string? _icon;

    [ObservableProperty]
    private string? _category;

    [ObservableProperty]
    private Choice<Guid>? _layer;

    [ObservableProperty]
    private bool _visibleInLive = true;

    [ObservableProperty]
    private Choice<LoopMode> _loop = SceneOptions.Loops[0];

    [ObservableProperty]
    private decimal _loopCount = 1;

    [ObservableProperty]
    private Choice<EndMode> _end = SceneOptions.Ends[0];

    [ObservableProperty]
    private Choice<Guid>? _chainScene;

    [ObservableProperty]
    private decimal _speed = 1;

    [ObservableProperty]
    private int _currentStep;

    [ObservableProperty]
    private string? _stepName;

    [ObservableProperty]
    private Choice<FadeCurve> _stepCurve = SceneOptions.Curves[0];

    [ObservableProperty]
    private Choice<DiscreteSwitch> _stepSwitch = SceneOptions.Switches[0];

    [ObservableProperty]
    private bool _lightWhenColoring = true;

    [ObservableProperty]
    private string? _hint;

    /// <summary>Crée l'éditeur.</summary>
    public SceneEditorViewModel(LuxiaRuntime runtime, ScenesViewModel owner, ProgrammerViewModel programmer)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(programmer);
        _runtime = runtime;
        _owner = owner;
        Programmer = programmer;
        FadeIn.Edited += (_, _) => Update(s => s with { FadeIn = FadeIn.Value }, "Fondu d'entrée");
        FadeOut.Edited += (_, _) => Update(s => s with { FadeOut = FadeOut.Value }, "Fondu de sortie");
        StepFade.Edited += (_, _) => UpdateStep(s => s with { Fade = StepFade.Value ?? Duration.Zero }, "Fondu de l'étape");
        StepHold.Edited += (_, _) => UpdateStep(s => s with { Hold = StepHold.Value ?? Duration.Zero }, "Maintien de l'étape");
    }

    /// <summary>Programmeur partagé.</summary>
    public ProgrammerViewModel Programmer { get; }

    /// <summary>Scène éditée (version enregistrée).</summary>
    public Scene? Scene { get; private set; }

    /// <summary>Couches proposées.</summary>
    public ObservableCollection<Choice<Guid>> Layers { get; } = [];

    /// <summary>Scènes proposées pour l'enchaînement.</summary>
    public ObservableCollection<Choice<Guid>> ChainTargets { get; } = [];

    /// <summary>Fondu d'entrée par défaut (vide = fondu de la première étape).</summary>
    public DurationField FadeIn { get; } = new();

    /// <summary>Fondu de sortie (vide = arrêt immédiat).</summary>
    public DurationField FadeOut { get; } = new();

    /// <summary>Fondu de l'étape courante.</summary>
    public DurationField StepFade { get; } = new();

    /// <summary>Maintien de l'étape courante.</summary>
    public DurationField StepHold { get; } = new();

    /// <summary>Étapes.</summary>
    public ObservableCollection<StepRowViewModel> Steps { get; } = [];

    /// <summary>Choix de listes.</summary>
    public static IReadOnlyList<Choice<LoopMode>> LoopOptions => SceneOptions.Loops;

    /// <summary>Choix de listes.</summary>
    public static IReadOnlyList<Choice<EndMode>> EndOptions => SceneOptions.Ends;

    /// <summary>Choix de listes.</summary>
    public static IReadOnlyList<Choice<FadeCurve>> CurveOptions => SceneOptions.Curves;

    /// <summary>Choix de listes.</summary>
    public static IReadOnlyList<Choice<DiscreteSwitch>> SwitchOptions => SceneOptions.Switches;

    /// <summary>Affiche une scène (ou rien).</summary>
    public void Load(Scene? scene)
    {
        _loading = true;
        try
        {
            var sameScene = Scene?.Id == scene?.Id;
            Scene = scene;
            HasScene = scene is not null;
            Layers.Clear();
            foreach (var layer in _runtime.Project.Layers.Layers)
            {
                Layers.Add(new Choice<Guid>(layer.Id, layer.Name));
            }

            ChainTargets.Clear();
            foreach (var other in _runtime.Project.Scenes.Scenes.Where(s => s.Id != scene?.Id))
            {
                ChainTargets.Add(new Choice<Guid>(other.Id, other.Name));
            }

            if (scene is null)
            {
                Steps.Clear();
                return;
            }

            Name = scene.Name;
            Color = scene.Color;
            Icon = scene.Icon;
            Category = scene.Category;
            Layer = Layers.FirstOrDefault(l => l.Value == scene.LayerId) ?? Layers.FirstOrDefault();
            VisibleInLive = scene.VisibleInLive;
            Loop = SceneOptions.Loops.First(l => l.Value == scene.Loop);
            LoopCount = scene.LoopCount;
            End = SceneOptions.Ends.First(e => e.Value == scene.End);
            ChainScene = ChainTargets.FirstOrDefault(c => c.Value == scene.ChainSceneId);
            Speed = (decimal)scene.Speed;
            FadeIn.Load(scene.FadeIn);
            FadeOut.Load(scene.FadeOut);
            if (!sameScene)
            {
                CurrentStep = 0;
            }

            BuildSteps();
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>Met en évidence l'étape jouée par le moteur (état observable, MOT-100).</summary>
    public void ShowPlayingStep(int? index)
    {
        foreach (var step in Steps)
        {
            step.IsPlaying = step.Index == index;
        }
    }

    partial void OnNameChanged(string value) => Update(s => s with { Name = string.IsNullOrWhiteSpace(value) ? s.Name : value.Trim() }, "Nom");

    partial void OnColorChanged(string value)
    {
        if (value.Length == 7 && value[0] == '#' && int.TryParse(value.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            Update(s => s with { Color = value.ToUpperInvariant() }, "Couleur");
        }
    }

    partial void OnIconChanged(string? value) => Update(s => s with { Icon = string.IsNullOrWhiteSpace(value) ? null : value.Trim() }, "Icône");

    partial void OnCategoryChanged(string? value) => Update(s => s with { Category = string.IsNullOrWhiteSpace(value) ? null : value.Trim() }, "Catégorie");

    partial void OnLayerChanged(Choice<Guid>? value)
    {
        if (value is not null)
        {
            Update(s => s with { LayerId = value.Value }, "Couche");
        }
    }

    partial void OnVisibleInLiveChanged(bool value) => Update(s => s with { VisibleInLive = value }, "Visible en Live");

    partial void OnLoopChanged(Choice<LoopMode> value) => Update(s => s with { Loop = value.Value }, "Boucle");

    partial void OnLoopCountChanged(decimal value) => Update(s => s with { LoopCount = (int)Math.Max(1, value) }, "Nombre de passages");

    partial void OnEndChanged(Choice<EndMode> value) => Update(s => s with { End = value.Value }, "Fin de scène");

    partial void OnChainSceneChanged(Choice<Guid>? value) => Update(s => s with { ChainSceneId = value?.Value }, "Scène enchaînée");

    partial void OnSpeedChanged(decimal value) => Update(s => s with { Speed = Math.Clamp((double)value, 0.1, 10) }, "Vitesse");

    partial void OnStepNameChanged(string? value) => UpdateStep(s => s with { Name = string.IsNullOrWhiteSpace(value) ? null : value.Trim() }, "Nom de l'étape");

    partial void OnStepCurveChanged(Choice<FadeCurve> value) => UpdateStep(s => s with { Curve = value.Value }, "Courbe");

    partial void OnStepSwitchChanged(Choice<DiscreteSwitch> value) => UpdateStep(s => s with { Switch = value.Value }, "Bascule des attributs discrets");

    /// <summary>Choisit l'étape courante ; si le programmeur n'a rien de nouveau, il la charge (ce qu'on voit = l'étape).</summary>
    [RelayCommand]
    private void SelectStep(StepRowViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        CurrentStep = row.Index;
        MarkCurrentStep();
        if (!Programmer.IsModified)
        {
            LoadStep();
        }
    }

    /// <summary>Charge l'étape courante dans le programmeur.</summary>
    [RelayCommand]
    private void LoadStep()
    {
        if (Scene is { } scene && CurrentStep < scene.Steps.Count)
        {
            Programmer.Load(scene.Steps[CurrentStep].Values);
        }
    }

    /// <summary>« Remplacer » : l'étape courante prend le contenu du programmeur (SCN-033).</summary>
    [RelayCommand]
    private void ReplaceStep() => Record(RecordMode.Replace);

    /// <summary>« Fusionner » dans l'étape courante (SCN-033).</summary>
    [RelayCommand]
    private void MergeStep() => Record(RecordMode.Merge);

    /// <summary>Enregistrer comme nouvelle étape, après l'étape courante (SCN-033).</summary>
    [RelayCommand]
    private void RecordNewStep() => Record(RecordMode.NewStep);

    /// <summary>Ajoute une étape vide après l'étape courante (SCN-002).</summary>
    [RelayCommand]
    private void AddStep() => EditSteps((steps, i) => steps.Insert(i + 1, NewStepLike(steps[i])), +1, "Ajouter une étape");

    /// <summary>Insère une étape vide avant l'étape courante (SCN-002).</summary>
    [RelayCommand]
    private void InsertStep() => EditSteps((steps, i) => steps.Insert(i, NewStepLike(steps[i])), 0, "Insérer une étape");

    /// <summary>Duplique l'étape courante (SCN-002).</summary>
    [RelayCommand]
    private void DuplicateStep() => EditSteps((steps, i) => steps.Insert(i + 1, steps[i]), +1, "Dupliquer l'étape");

    /// <summary>Supprime l'étape courante (il en reste toujours une, SCN-002).</summary>
    [RelayCommand]
    private void DeleteStep()
    {
        if (Scene is { Steps.Count: > 1 })
        {
            EditSteps((steps, i) => steps.RemoveAt(i), 0, "Supprimer l'étape");
        }
    }

    /// <summary>Recule l'étape courante d'un rang (SCN-002).</summary>
    [RelayCommand]
    private void MoveStepLeft()
    {
        if (CurrentStep > 0)
        {
            EditSteps((steps, i) => (steps[i - 1], steps[i]) = (steps[i], steps[i - 1]), -1, "Déplacer l'étape");
        }
    }

    /// <summary>Avance l'étape courante d'un rang (SCN-002).</summary>
    [RelayCommand]
    private void MoveStepRight()
    {
        if (Scene is { } scene && CurrentStep < scene.Steps.Count - 1)
        {
            EditSteps((steps, i) => (steps[i + 1], steps[i]) = (steps[i], steps[i + 1]), +1, "Déplacer l'étape");
        }
    }

    /// <summary>Modification groupée (SCN-004) : fondu et maintien de l'étape courante appliqués aux étapes cochées (toutes si aucune).</summary>
    [RelayCommand]
    private void ApplyTimingToChecked()
    {
        if (Scene is not { } scene)
        {
            return;
        }

        var model = scene.Steps[CurrentStep];
        var checkedIndexes = Steps.Where(s => s.IsChecked).Select(s => s.Index).ToHashSet();
        var all = checkedIndexes.Count == 0;
        _owner.UpdateScene(
            scene.Id,
            s => s with { Steps = [.. s.Steps.Select((step, i) => all || checkedIndexes.Contains(i) ? step with { Fade = model.Fade, Hold = model.Hold, Curve = model.Curve } : step)] },
            "Durées groupées");
    }

    /// <summary>Tester la scène dans son contexte : les autres couches restent actives (SCN-034).</summary>
    [RelayCommand]
    private void Test() => Launch(solo: false);

    /// <summary>Tester la scène seule (SCN-034).</summary>
    [RelayCommand]
    private void TestSolo() => Launch(solo: true);

    /// <summary>Arrête l'essai.</summary>
    [RelayCommand]
    private void StopTest()
    {
        if (Scene is { } scene)
        {
            Programmer.TargetEngine.Send(new StopSceneCommand(CommandOrigin.User, scene.Id));
        }
    }

    /// <summary>Pas à pas pendant l'essai (CMD-015).</summary>
    [RelayCommand]
    private void NextStep() => StepPlayback(StepDirection.Next);

    /// <summary>Pas à pas pendant l'essai (CMD-015).</summary>
    [RelayCommand]
    private void PreviousStep() => StepPlayback(StepDirection.Previous);

    private void StepPlayback(StepDirection direction)
    {
        if (Scene is { } scene)
        {
            Programmer.TargetEngine.Send(new StepSceneCommand(CommandOrigin.User, scene.Id, direction));
        }
    }

    private void Launch(bool solo)
    {
        if (Scene is not { } scene)
        {
            return;
        }

        Programmer.TargetEngine.Send(new LaunchSceneCommand(CommandOrigin.User, scene.Id, Solo: solo));
        Hint = Programmer.Values.Count > 0
            ? "Le programmeur n'est pas vide : ses réglages passent au-dessus de la scène (étape 5 de la chaîne). Videz-le pour voir la scène telle qu'enregistrée."
            : null;
    }

    private void Record(RecordMode mode)
    {
        if (Scene is not { } scene)
        {
            return;
        }

        var palettes = Palettes();
        var values = LightWhenColoring ? ProgrammerRules.LightWhenColoring(Programmer.Values, palettes) : Programmer.Values;
        var index = CurrentStep;
        _owner.UpdateScene(
            scene.Id,
            s =>
            {
                var steps = s.Steps.ToList();
                switch (mode)
                {
                    case RecordMode.Merge:
                        steps[index] = ProgrammerRules.Merge(steps[index], values, palettes);
                        break;
                    case RecordMode.NewStep:
                        steps.Insert(index + 1, NewStepLike(steps[index]) with { Values = [.. values] });
                        break;
                    default:
                        steps[index] = ProgrammerRules.Replace(steps[index], values);
                        break;
                }

                return s with { Steps = steps };
            },
            mode switch
            {
                RecordMode.Merge => "Fusionner dans l'étape",
                RecordMode.NewStep => "Nouvelle étape",
                _ => "Remplacer l'étape",
            });

        if (mode == RecordMode.NewStep)
        {
            CurrentStep = index + 1;
            MarkCurrentStep();
        }

        Programmer.MarkRecorded();
    }

    private void EditSteps(Action<List<SceneStep>, int> edit, int move, string description)
    {
        if (Scene is not { } scene)
        {
            return;
        }

        var index = Math.Clamp(CurrentStep, 0, scene.Steps.Count - 1);
        _owner.UpdateScene(
            scene.Id,
            s =>
            {
                var steps = s.Steps.ToList();
                edit(steps, index);
                return s with { Steps = steps };
            },
            description);
        CurrentStep = Math.Clamp(index + move, 0, Math.Max(0, (Scene?.Steps.Count ?? 1) - 1));
        MarkCurrentStep();
    }

    private void Update(Func<Scene, Scene> change, string description)
    {
        if (!_loading && Scene is { } scene)
        {
            _owner.UpdateScene(scene.Id, change, description);
        }
    }

    private void UpdateStep(Func<SceneStep, SceneStep> change, string description)
    {
        if (_loading || Scene is not { } scene || CurrentStep >= scene.Steps.Count)
        {
            return;
        }

        var index = CurrentStep;
        _owner.UpdateScene(scene.Id, s => s with { Steps = [.. s.Steps.Select((step, i) => i == index ? change(step) : step)] }, description);
    }

    private void BuildSteps()
    {
        var scene = Scene!;
        var checkedIndexes = Steps.Where(s => s.IsChecked).Select(s => s.Index).ToHashSet();
        Steps.Clear();
        for (var i = 0; i < scene.Steps.Count; i++)
        {
            Steps.Add(new StepRowViewModel(i, scene.Steps[i]) { IsChecked = checkedIndexes.Contains(i) });
        }

        CurrentStep = Math.Clamp(CurrentStep, 0, scene.Steps.Count - 1);
        MarkCurrentStep();
    }

    private void MarkCurrentStep()
    {
        foreach (var row in Steps)
        {
            row.IsCurrent = row.Index == CurrentStep;
        }

        if (Scene is not { } scene || CurrentStep >= scene.Steps.Count)
        {
            return;
        }

        var wasLoading = _loading;
        _loading = true;
        var step = scene.Steps[CurrentStep];
        StepName = step.Name;
        StepFade.Load(step.Fade);
        StepHold.Load(step.Hold);
        StepCurve = SceneOptions.Curves.First(c => c.Value == step.Curve);
        StepSwitch = SceneOptions.Switches.First(c => c.Value == step.Switch);
        _loading = wasLoading;
    }

    private Func<Guid, Palette?> Palettes()
    {
        var map = _runtime.Project.Palettes.Palettes.ToDictionary(p => p.Id);
        return id => map.GetValueOrDefault(id);
    }

    private static SceneStep NewStepLike(SceneStep model) => new() { Fade = model.Fade, Hold = model.Hold, Curve = model.Curve, Switch = model.Switch };

    private enum RecordMode
    {
        Replace,
        Merge,
        NewStep,
    }
}
