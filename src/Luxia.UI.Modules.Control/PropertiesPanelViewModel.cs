using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Engine.Model;
using Luxia.Fixtures.Model;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;
using Luxia.UI.Controls;
using Luxia.UI.Modules.Scenes;

namespace Luxia.UI.Modules.Control;

/// <summary>
/// Panneau Propriétés (doc 60 §5) de la scène choisie pour l'édition : identité, lecture, bande d'étapes, contenu de
/// l'étape. Tout s'enregistre à la saisie (§4.2, E3) et s'annule par Ctrl+Z ; aucun bouton « Enregistrer ».
/// </summary>
public sealed partial class PropertiesPanelViewModel : ViewModelBase
{
    private readonly LuxiaRuntime _runtime;
    private readonly ControlSession _session;
    private bool _loading;

    [ObservableProperty]
    private bool _hasScene;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _color = "#58A6FF";

    [ObservableProperty]
    private string _summary = string.Empty;

    [ObservableProperty]
    private Choice<Guid>? _layer;

    [ObservableProperty]
    private bool _visibleInLive = true;

    [ObservableProperty]
    private decimal _speedPercent = 100;

    [ObservableProperty]
    private Choice<LoopMode> _loop = SceneOptions.Loops[0];

    [ObservableProperty]
    private Choice<EndMode> _end = SceneOptions.Ends[0];

    [ObservableProperty]
    private Choice<StepAdvanceMode> _advance = SceneOptions.Advances[0];

    [ObservableProperty]
    private decimal _advanceEvery = 1;

    [ObservableProperty]
    private Choice<LaunchQuantize> _quantize = SceneOptions.Quantizes[0];

    [ObservableProperty]
    private bool _hasOwnClock;

    [ObservableProperty]
    private decimal _ownBpm = 120;

    [ObservableProperty]
    private decimal? _fadeInSeconds;

    [ObservableProperty]
    private decimal? _fadeOutSeconds;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private IReadOnlyList<StepStripItem> _steps = [];

    [ObservableProperty]
    private int _selectedStep;

    [ObservableProperty]
    private string _stepTitle = string.Empty;

    [ObservableProperty]
    private string? _stepName;

    [ObservableProperty]
    private decimal _stepFadeSeconds;

    [ObservableProperty]
    private decimal _stepHoldSeconds = 1;

    [ObservableProperty]
    private bool _stepHueFade;

    [ObservableProperty]
    private Choice<string> _wizard = Wizards[0];

    [ObservableProperty]
    private decimal _wizardHoldSeconds = 1;

    [ObservableProperty]
    private decimal _wizardFadeSeconds;

    [ObservableProperty]
    private string? _wizardMessage;

    [ObservableProperty]
    private string _accent = ControlColors.Accent;

    [ObservableProperty]
    private string _emptyText = "Choisissez une scène à éditer : bande ✎ à droite d'un bouton de scène, dans les colonnes.";

    /// <summary>Crée le panneau.</summary>
    public PropertiesPanelViewModel(LuxiaRuntime runtime, ControlSession session)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(session);
        _runtime = runtime;
        _session = session;
        session.Changed += (_, _) => Load();
        Load();
    }

    /// <summary>Les champs sont modifiables (pas de verrou soirée).</summary>
    public bool IsEditable => !_session.IsLocked;

    /// <summary>
    /// En LIVE, la scène est seulement choisie (✎) : les réglages restent des retouches temporaires. On rappelle comment
    /// écrire dedans (question de l'utilisateur, essai 1.005.198).
    /// </summary>
    public bool ShowsModeHint => _session.Mode == EditMode.Live && !_session.IsLocked;

    /// <summary>Couches proposées.</summary>
    public ObservableCollection<Choice<Guid>> Layers { get; } = [];

    /// <summary>Contenu de l'étape choisie, lisible.</summary>
    public ObservableCollection<StepValueRow> StepValues { get; } = [];

    /// <summary>Couleurs proposées pour la scène.</summary>
    public static IReadOnlyList<string> Colors => ColumnsPanelViewModel.SceneColors;

    /// <summary>Choix de l'événement qui fait avancer d'étape (MOT-017).</summary>
    public static IReadOnlyList<Choice<StepAdvanceMode>> AdvanceOptions => SceneOptions.Advances;

    /// <summary>Choix de la quantification du lancement (MOT-018).</summary>
    public static IReadOnlyList<Choice<LaunchQuantize>> QuantizeOptions => SceneOptions.Quantizes;

    /// <summary>Choix de boucle.</summary>
    public static IReadOnlyList<Choice<LoopMode>> LoopOptions => SceneOptions.Loops;

    /// <summary>Assistants de création (SCN-014).</summary>
    public static IReadOnlyList<Choice<string>> Wizards { get; } =
    [
        new("chase", "Chenillard de couleurs"),
        new("alternate", "Alternance de 2 couleurs"),
        new("sweep", "Balayage de positions"),
    ];

    /// <summary>Palettes proposées à l'assistant choisi (couleurs, ou positions pour le balayage).</summary>
    public ObservableCollection<WizardPaletteChip> WizardPalettes { get; } = [];

    /// <summary>Choix de fin.</summary>
    public static IReadOnlyList<Choice<EndMode>> EndOptions => SceneOptions.Ends;

    /// <summary>Marque l'étape jouée (relu 20 fois par seconde).</summary>
    public void Refresh()
    {
        if (_session.EditScene is not { } scene)
        {
            return;
        }

        var playing = _runtime.Engine.Snapshot.Playbacks.Where(p => p.SceneId == scene.Id).Select(p => (int?)p.StepIndex).LastOrDefault();
        if (Steps.Select((s, i) => s.IsPlaying != (i == playing)).Any(changed => changed))
        {
            Steps = [.. Steps.Select((s, i) => s with { IsPlaying = i == playing })];
        }
    }

    /// <summary>Choisit une étape (bande d'étapes).</summary>
    public void ChooseStep(int index) => _session.ChooseStep(index);

    /// <summary>Couleur de la scène (clic sur une pastille).</summary>
    [RelayCommand]
    private void SetColor(string? color)
    {
        if (color is { Length: 7 })
        {
            Color = color;
        }
    }

    /// <summary>Ajoute une étape après l'étape choisie (SCN-002), avec ses durées.</summary>
    [RelayCommand]
    private void AddStep() => EditSteps((steps, i) => steps.Insert(i + 1, steps[i] with { Name = null, Values = [] }), +1, "Ajouter une étape");

    /// <summary>Duplique l'étape choisie (SCN-002).</summary>
    [RelayCommand]
    private void DuplicateStep() => EditSteps((steps, i) => steps.Insert(i + 1, steps[i]), +1, "Dupliquer l'étape");

    /// <summary>Supprime l'étape choisie ; il en reste toujours une (SCN-002).</summary>
    [RelayCommand]
    private void DeleteStep()
    {
        if (_session.EditScene is { Steps.Count: > 1 })
        {
            EditSteps((steps, i) => steps.RemoveAt(i), 0, "Supprimer l'étape");
        }
    }

    /// <summary>Recule l'étape choisie d'un rang.</summary>
    [RelayCommand]
    private void MoveStepLeft()
    {
        if (_session.EditStep > 0)
        {
            EditSteps((steps, i) => (steps[i - 1], steps[i]) = (steps[i], steps[i - 1]), -1, "Déplacer l'étape");
        }
    }

    /// <summary>Avance l'étape choisie d'un rang.</summary>
    [RelayCommand]
    private void MoveStepRight()
    {
        if (_session.EditScene is { } scene && _session.EditStep < scene.Steps.Count - 1)
        {
            EditSteps((steps, i) => (steps[i + 1], steps[i]) = (steps[i], steps[i + 1]), +1, "Déplacer l'étape");
        }
    }

    /// <summary>
    /// SCN-014 : remplace les étapes de la scène par celles de l'assistant, pour les appareils choisis au plan (de gauche à
    /// droite) et les palettes cochées (dans l'ordre de la liste). Ctrl+Z annule.
    /// </summary>
    [RelayCommand]
    private void GenerateSteps()
    {
        if (_session.EditScene is null)
        {
            return;
        }

        var members = _session.OrderedSelection();
        if (members.Count == 0)
        {
            WizardMessage = "Sélectionnez d'abord les appareils sur le plan.";
            return;
        }

        var palettes = WizardPalettes.Where(p => p.IsChecked).Select(p => p.PaletteId).ToList();
        var hold = Duration.FromSeconds((double)Math.Max(0, WizardHoldSeconds));
        var fade = Duration.FromSeconds((double)Math.Max(0, WizardFadeSeconds));
        IReadOnlyList<SceneStep> steps = Wizard.Value switch
        {
            "alternate" when palettes.Count >= 2 => SceneWizards.Alternate(members, palettes[0], palettes[1], hold, fade),
            "alternate" => [],
            "sweep" => SceneWizards.PositionSweep(members, palettes, hold, fade),
            _ => SceneWizards.ColorChase(members, palettes, hold, fade),
        };
        if (steps.Count == 0)
        {
            WizardMessage = Wizard.Value == "alternate" ? "Cochez deux couleurs." : "Cochez au moins une palette.";
            return;
        }

        _session.UpdateScene(s => s with { Steps = steps }, $"Assistant : {Wizard.Label}", 0);
        _session.Commit();
        WizardMessage = string.Create(CultureInfo.CurrentCulture, $"{steps.Count} étape(s) générée(s) pour {members.Count} appareil(s). Ctrl+Z pour revenir en arrière.");
    }

    partial void OnWizardChanged(Choice<string> value) => FillWizardPalettes();

    private void FillWizardPalettes()
    {
        var kind = Wizard.Value == "sweep" ? PaletteKind.Position : PaletteKind.Color;
        var palettes = _runtime.Project.Palettes.Palettes.Where(p => p.Kind == kind).ToList();
        if (WizardPalettes.Select(p => p.PaletteId).SequenceEqual(palettes.Select(p => p.Id)))
        {
            return;
        }

        WizardPalettes.Clear();
        foreach (var palette in palettes)
        {
            WizardPalettes.Add(new WizardPaletteChip(palette.Id, palette.Name, palette.DisplayColor()));
        }
    }

    /// <summary>Lance la scène éditée (essai dans son contexte, SCN-034).</summary>
    [RelayCommand]
    private void Test()
    {
        if (_session.EditScene is { } scene)
        {
            _session.Commit();

            // Fenêtre d'édition : on joue le brouillon là où on le voit (sortie en ÉDITION, aperçu en AVEUGLE), sans changer de mode.
            if (_session.IsDraft)
            {
                _session.SuspendShow();
                _session.DisplayEngine.Send(new LaunchSceneCommand(CommandOrigin.User, scene.Id));
                return;
            }

            // Essai P6 : en ÉDITION, l'étape éditée est montrée par-dessus les scènes et cachait celle qu'on lance.
            if (_session.Mode == EditMode.Edit)
            {
                _session.SetMode(EditMode.Live);
            }

            _runtime.Engine.Send(new LaunchSceneCommand(CommandOrigin.User, scene.Id));
        }
    }

    /// <summary>Arrête la scène éditée.</summary>
    [RelayCommand]
    private void StopTest()
    {
        if (_session.EditScene is { } scene)
        {
            _runtime.Engine.Send(new StopSceneCommand(CommandOrigin.User, scene.Id));
            if (_session.IsDraft)
            {
                _runtime.Preview.Send(new StopSceneCommand(CommandOrigin.User, scene.Id));
            }
        }
    }

    partial void OnNameChanged(string value) => Update(s => s with { Name = string.IsNullOrWhiteSpace(value) ? s.Name : value.Trim() }, "Nom de la scène");

    partial void OnColorChanged(string value) => Update(s => s with { Color = value }, "Couleur de la scène");

    partial void OnLayerChanged(Choice<Guid>? value)
    {
        if (value is not null)
        {
            Update(s => s with { LayerId = value.Value }, "Couche de la scène");
        }
    }

    partial void OnVisibleInLiveChanged(bool value) => Update(s => s with { VisibleInLive = value }, "Visible dans le Live");

    partial void OnSpeedPercentChanged(decimal value) => Update(s => s with { Speed = Math.Clamp((double)value / 100, 0.1, 10) }, "Vitesse");

    partial void OnLoopChanged(Choice<LoopMode> value) => Update(s => s with { Loop = value.Value }, "Enchaînement");

    partial void OnEndChanged(Choice<EndMode> value) => Update(s => s with { End = value.Value }, "Fin de scène");

    partial void OnAdvanceChanged(Choice<StepAdvanceMode> value) => Update(s => s with { Advance = value.Value }, "Étape suivante au rythme");

    partial void OnAdvanceEveryChanged(decimal value) => Update(s => s with { AdvanceEvery = (int)Math.Clamp(value, 1, 64) }, "Nombre d'événements entre deux étapes");

    partial void OnQuantizeChanged(Choice<LaunchQuantize> value) => Update(s => s with { Quantize = value.Value }, "Démarrage au rythme");

    partial void OnHasOwnClockChanged(bool value) => Update(s => s with { OwnBpm = value ? (double)Math.Clamp(OwnBpm, 20, 400) : null }, "Horloge propre de la scène");

    partial void OnOwnBpmChanged(decimal value)
    {
        if (HasOwnClock)
        {
            Update(s => s with { OwnBpm = (double)Math.Clamp(value, 20, 400) }, "Tempo propre de la scène");
        }
    }

    partial void OnFadeInSecondsChanged(decimal? value) => Update(s => s with { FadeIn = value is { } v ? Duration.FromSeconds((double)Math.Max(0, v)) : null }, "Fondu d'entrée");

    partial void OnFadeOutSecondsChanged(decimal? value) => Update(s => s with { FadeOut = value is { } v ? Duration.FromSeconds((double)Math.Max(0, v)) : null }, "Fondu de sortie");

    partial void OnNotesChanged(string? value) => Update(s => s with { Notes = string.IsNullOrWhiteSpace(value) ? null : value }, "Notes");

    partial void OnStepNameChanged(string? value) => UpdateStep(s => s with { Name = string.IsNullOrWhiteSpace(value) ? null : value.Trim() }, "Nom de l'étape");

    partial void OnStepFadeSecondsChanged(decimal value) => UpdateStep(s => s with { Fade = Duration.FromSeconds((double)Math.Max(0, value)) }, "Fondu de l'étape");

    partial void OnStepHoldSecondsChanged(decimal value) => UpdateStep(s => s with { Hold = Duration.FromSeconds((double)Math.Max(0, value)) }, "Maintien de l'étape");

    partial void OnStepHueFadeChanged(bool value) => UpdateStep(s => s with { HueFade = value }, value ? "Fondu par la teinte" : "Fondu direct des couleurs");

    private void Update(Func<Scene, Scene> change, string description)
    {
        if (!_loading && HasScene)
        {
            _session.UpdateScene(change, description);
        }
    }

    private void UpdateStep(Func<SceneStep, SceneStep> change, string description)
    {
        if (_loading || _session.EditScene is null)
        {
            return;
        }

        var index = _session.EditStep;
        _session.UpdateScene(s => s with { Steps = [.. s.Steps.Select((step, i) => i == index ? change(step) : step)] }, description);
    }

    private void EditSteps(Action<List<SceneStep>, int> edit, int move, string description)
    {
        if (_session.EditScene is not { } scene)
        {
            return;
        }

        var index = Math.Clamp(_session.EditStep, 0, scene.Steps.Count - 1);
        _session.UpdateScene(
            s =>
            {
                var steps = s.Steps.ToList();
                edit(steps, index);
                return s with { Steps = steps };
            },
            description,
            index + move);
        _session.Commit();
    }

    private void Load()
    {
        _loading = true;
        try
        {
            var scene = _session.EditScene;
            HasScene = scene is not null;
            Accent = _session.Mode == EditMode.Live ? ControlColors.Accent : ControlColors.Of(_session.Mode);
            OnPropertyChanged(nameof(IsEditable));
            OnPropertyChanged(nameof(ShowsModeHint));
            if (scene is null)
            {
                Steps = [];
                StepValues.Clear();
                return;
            }

            if (Layers.Count != _runtime.Project.Layers.Layers.Count)
            {
                Layers.Clear();
                foreach (var layer in _runtime.Project.Layers.Layers.OrderBy(l => l.Priority))
                {
                    Layers.Add(new Choice<Guid>(layer.Id, layer.Name));
                }
            }

            // Pendant la frappe, ne pas réécrire le champ qu'on est en train de taper.
            if (Name.Trim() != scene.Name)
            {
                Name = scene.Name;
            }

            Color = scene.Color;
            Layer = Layers.FirstOrDefault(l => l.Value == scene.LayerId);
            VisibleInLive = scene.VisibleInLive;
            SpeedPercent = (decimal)Math.Round(scene.Speed * 100);
            Loop = SceneOptions.Loops.FirstOrDefault(l => l.Value == scene.Loop) ?? SceneOptions.Loops[0];
            End = SceneOptions.Ends.FirstOrDefault(e => e.Value == scene.End) ?? SceneOptions.Ends[0];
            Advance = SceneOptions.Advances.FirstOrDefault(a => a.Value == scene.Advance) ?? SceneOptions.Advances[0];
            AdvanceEvery = scene.AdvanceEvery;
            Quantize = SceneOptions.Quantizes.FirstOrDefault(q => q.Value == scene.Quantize) ?? SceneOptions.Quantizes[0];
            HasOwnClock = scene.OwnBpm is not null;
            OwnBpm = scene.OwnBpm is { } own ? (decimal)own : 120;
            FadeInSeconds = scene.FadeIn is { } fadeIn ? (decimal)fadeIn.ToSeconds(120) : null;
            FadeOutSeconds = scene.FadeOut is { } fadeOut ? (decimal)fadeOut.ToSeconds(120) : null;
            Notes = scene.Notes;
            var layerName = _runtime.Project.Layers.Layers.FirstOrDefault(l => l.Id == scene.LayerId)?.Name ?? "?";
            Summary = string.Create(CultureInfo.CurrentCulture, $"Couche {layerName} · {scene.Steps.Count} étape(s){(scene.VisibleInLive ? string.Empty : " · masquée du Live")}");

            Steps = [.. scene.Steps.Select((s, i) => new StepStripItem(
                string.Create(CultureInfo.CurrentCulture, $"{i + 1}{(s.Name is { } n ? " · " + n : string.Empty)}"),
                s.Fade.ToSeconds(120),
                s.Hold.ToSeconds(120),
                StepColor(s),
                false))];
            SelectedStep = _session.EditStep;
            var step = scene.Steps[Math.Clamp(_session.EditStep, 0, scene.Steps.Count - 1)];
            StepTitle = string.Create(CultureInfo.CurrentCulture, $"Étape {_session.EditStep + 1}{(step.Name is { } name ? " · " + name : string.Empty)}");
            StepName = step.Name;
            StepFadeSeconds = (decimal)step.Fade.ToSeconds(120);
            StepHoldSeconds = (decimal)step.Hold.ToSeconds(120);
            StepHueFade = step.HueFade;
            FillStepValues(step);
            FillWizardPalettes();
        }
        finally
        {
            _loading = false;
        }
    }

    private void FillStepValues(SceneStep step)
    {
        StepValues.Clear();
        var color = _session.Mode == EditMode.Live ? ControlColors.Edit : ControlColors.Of(_session.Mode);
        var palettes = _runtime.Project.Palettes.Palettes.ToDictionary(p => p.Id);
        foreach (var value in step.Values)
        {
            StepValues.Add(new StepValueRow(Who(value.Target), What(value, palettes), color));
        }
    }

    private string Who(ValueTarget target)
    {
        if (target.FixtureId is { } id)
        {
            var name = _runtime.Project.Installation.Fixtures.FirstOrDefault(f => f.Id == id)?.Name ?? "?";
            return target.Cell > 0 ? string.Create(CultureInfo.CurrentCulture, $"{name} · cellule {target.Cell}") : name;
        }

        if (target.SelectionId is { } selection)
        {
            return _runtime.Project.Installation.Selections.FirstOrDefault(s => s.Id == selection)?.Name ?? "sélection ?";
        }

        return target.Auto is { } auto ? auto.Model ?? auto.Category?.ToString() ?? "tous" : "?";
    }

    private static string What(SceneValue value, Dictionary<Guid, Palette> palettes)
    {
        if (value.PaletteId is { } id)
        {
            return $"Palette « {(palettes.TryGetValue(id, out var p) ? p.Name : "?")} »";
        }

        if (value.Color is { } color)
        {
            return $"Couleur {color.Hex}";
        }

        var label = value.Attribute is { } attribute ? AttributeCatalog.Label(attribute) : value.Channel ?? "?";
        if (value.Range is { } range)
        {
            return string.Create(CultureInfo.CurrentCulture, $"{label} : plage {range.Min}-{range.Max}");
        }

        return value.Level is { } level ? string.Create(CultureInfo.CurrentCulture, $"{label} : {Math.Round(level * 100)} %") : label;
    }

    private static string? StepColor(SceneStep step) => step.Values.FirstOrDefault(v => v.Color is not null)?.Color?.Hex;
}
