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
    private Choice<int> _frequency = SceneOptions.Frequencies[2];

    [ObservableProperty]
    private IReadOnlyList<Choice<int>> _frequencies = SceneOptions.Frequencies;

    [ObservableProperty]
    private bool _showFrequency;

    [ObservableProperty]
    private bool _showOwnClock;

    [ObservableProperty]
    private bool _showEnergySpeed = true;

    [ObservableProperty]
    private string _rhythmHeader = "Au rythme";

    private bool _sceneIsMusical;
    private bool _sceneHasEffects;

    [ObservableProperty]
    private Choice<LaunchQuantize> _quantize = SceneOptions.Quantizes[0];

    [ObservableProperty]
    private bool _energySpeed;

    [ObservableProperty]
    private bool _hasOwnClock;

    [ObservableProperty]
    private decimal _ownBpm = 120;

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
    private bool _stepHueFade;

    [ObservableProperty]
    private bool _stepAutoAdvance;

    [ObservableProperty]
    private Choice<FadeCurve> _stepCurve = SceneOptions.Curves[0];

    [ObservableProperty]
    private Choice<DiscreteSwitch> _stepSwitch = SceneOptions.Switches[0];

    [ObservableProperty]
    private Choice<string> _wizard = Wizards[0];

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
        WizardHold.Load(Duration.FromSeconds(1));
        WizardFade.Load(Duration.Zero);
        FadeIn.Edited += (_, _) => Update(s => s with { FadeIn = FadeIn.Value }, "Fondu d'entrée");
        FadeOut.Edited += (_, _) => Update(s => s with { FadeOut = FadeOut.Value }, "Fondu de sortie");
        StepFade.Edited += (_, _) => UpdateStep(s => s with { Fade = StepFade.Value ?? Duration.Zero }, "Fondu de l'étape");
        StepHold.Edited += (_, _) => UpdateStep(s => s with { Hold = StepHold.Value ?? Duration.Zero }, "Maintien de l'étape");
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

    /// <summary>Fondu d'entrée de la scène, en secondes, temps ou mesures (E2) ; vide = celui de la première étape.</summary>
    public DurationField FadeIn { get; } = new();

    /// <summary>Fondu de sortie de la scène (E2) ; vide = arrêt immédiat.</summary>
    public DurationField FadeOut { get; } = new();

    /// <summary>Fondu de l'étape choisie, en secondes, temps ou mesures (E2, GEN-023).</summary>
    public DurationField StepFade { get; } = new();

    /// <summary>Maintien de l'étape choisie (E2, GEN-023).</summary>
    public DurationField StepHold { get; } = new();

    /// <summary>Maintien des étapes générées par l'assistant (E2).</summary>
    public DurationField WizardHold { get; } = new();

    /// <summary>Fondu des étapes générées par l'assistant (E2).</summary>
    public DurationField WizardFade { get; } = new();

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
        var hold = WizardHold.Value ?? Duration.Zero;
        var fade = WizardFade.Value ?? Duration.Zero;
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

    partial void OnAdvanceChanged(Choice<StepAdvanceMode> value)
    {
        Update(s => s with { Advance = value.Value }, "Étape suivante au rythme");
        RefreshRhythmView();
    }

    partial void OnFrequencyChanged(Choice<int> value)
    {
        if (value is null)
        {
            return;
        }

        var multiplier = value.Value >= 100 ? value.Value - 100 : 1;
        var every = value.Value >= 100 ? 1 : value.Value;
        Update(s => s with { AdvanceEvery = every, AdvanceMultiplier = multiplier }, "Fréquence des étapes");
        RefreshRhythmView();
    }

    /// <summary>
    /// Volet « Au rythme » (essai P7, décision 4) : on ne montre que ce qui sert. La fréquence n'a de sens que si un événement fait
    /// avancer ; l'horloge propre seulement si la scène est musicale ; la vitesse selon l'énergie quand la durée des étapes ou des
    /// effets en dépend. Une phrase de résumé sur l'en-tête dit l'essentiel sans ouvrir le volet.
    /// </summary>
    private void RefreshRhythmView()
    {
        var onEvent = Advance.Value != StepAdvanceMode.Duration;
        ShowFrequency = onEvent;
        ShowOwnClock = HasOwnClock || onEvent || _sceneIsMusical || _sceneHasEffects;
        ShowEnergySpeed = !onEvent || _sceneHasEffects || EnergySpeed;

        var pulses = Advance.Value is StepAdvanceMode.BassPulse or StepAdvanceMode.TreblePulse;
        var wanted = SceneOptions.Frequencies.Where(f => !pulses || f.Value < 100).ToList();
        if (Frequency is { } current && wanted.All(f => f.Value != current.Value))
        {
            wanted.Add(current);
        }

        if (wanted.Count != Frequencies.Count)
        {
            Frequencies = wanted;
        }

        var parts = new List<string>();
        if (onEvent)
        {
            var unit = Advance.Value switch
            {
                StepAdvanceMode.Beat => "temps",
                StepAdvanceMode.Bar => "mesure",
                StepAdvanceMode.BassPulse => "kick",
                _ => "caisse claire",
            };
            parts.Add($"étapes sur le {unit}, {Frequency.Label.Split('(')[0].Trim()}");
        }

        if (Quantize.Value != LaunchQuantize.None)
        {
            parts.Add("démarrage " + Quantize.Label.ToLowerInvariant());
        }

        if (HasOwnClock)
        {
            parts.Add($"tempo propre {OwnBpm:0.#}");
        }

        if (EnergySpeed)
        {
            parts.Add("vitesse selon l'énergie");
        }

        RhythmHeader = parts.Count == 0 ? "Au rythme : rien de réglé" : "Au rythme : " + string.Join(" · ", parts);
    }

    partial void OnQuantizeChanged(Choice<LaunchQuantize> value)
    {
        Update(s => s with { Quantize = value.Value }, "Démarrage au rythme");
        RefreshRhythmView();
    }

    partial void OnEnergySpeedChanged(bool value)
    {
        Update(s => s with { EnergySpeed = value }, "Vitesse selon l'énergie");
        RefreshRhythmView();
    }

    partial void OnHasOwnClockChanged(bool value)
    {
        Update(s => s with { OwnBpm = value ? (double)Math.Clamp(OwnBpm, 20, 400) : null }, "Horloge propre de la scène");
        RefreshRhythmView();
    }

    partial void OnOwnBpmChanged(decimal value)
    {
        if (HasOwnClock)
        {
            Update(s => s with { OwnBpm = (double)Math.Clamp(value, 20, 400) }, "Tempo propre de la scène");
        }
    }

    partial void OnNotesChanged(string? value) => Update(s => s with { Notes = string.IsNullOrWhiteSpace(value) ? null : value }, "Notes");

    partial void OnStepNameChanged(string? value) => UpdateStep(s => s with { Name = string.IsNullOrWhiteSpace(value) ? null : value.Trim() }, "Nom de l'étape");

    partial void OnStepAutoAdvanceChanged(bool value) => UpdateStep(s => s with { AutoAdvance = value }, value ? "Étape brève au rythme" : "Étape au rythme jusqu'à l'événement");

    /// <summary>Courbes de fondu d'une étape (MOT-011), réglables ici depuis le retrait de l'écran Scènes (lot 7 de P8).</summary>
    public static IReadOnlyList<Choice<FadeCurve>> Curves => SceneOptions.Curves;

    /// <summary>Moment de bascule des attributs discrets (MOT-012), réglable ici depuis le lot 7 de P8.</summary>
    public static IReadOnlyList<Choice<DiscreteSwitch>> Switches => SceneOptions.Switches;

    partial void OnStepCurveChanged(Choice<FadeCurve> value) => UpdateStep(s => s with { Curve = value.Value }, "Courbe du fondu");

    partial void OnStepSwitchChanged(Choice<DiscreteSwitch> value) => UpdateStep(s => s with { Switch = value.Value }, "Bascule des attributs discrets");

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
            _sceneIsMusical = scene.Steps.Any(s => s.Fade.Unit != DurationUnit.Seconds || s.Hold.Unit != DurationUnit.Seconds);
            _sceneHasEffects = scene.Steps.Any(s => s.Effects.Count > 0);
            var code = scene.AdvanceMultiplier is 2 or 4 ? 100 + scene.AdvanceMultiplier : Math.Max(1, scene.AdvanceEvery);
            Frequency = SceneOptions.Frequencies.FirstOrDefault(f => f.Value == code) ?? new Choice<int>(code, $"÷ {code} (un sur {code})");
            Quantize = SceneOptions.Quantizes.FirstOrDefault(q => q.Value == scene.Quantize) ?? SceneOptions.Quantizes[0];
            EnergySpeed = scene.EnergySpeed;
            HasOwnClock = scene.OwnBpm is not null;
            OwnBpm = scene.OwnBpm is { } own ? (decimal)own : 120;
            FadeIn.Load(scene.FadeIn);
            FadeOut.Load(scene.FadeOut);
            Notes = scene.Notes;
            RefreshRhythmView();
            var layerName = _runtime.Project.Layers.Layers.FirstOrDefault(l => l.Id == scene.LayerId)?.Name ?? "?";
            Summary = string.Create(CultureInfo.CurrentCulture, $"Couche {layerName} · {scene.Steps.Count} étape(s){(scene.VisibleInLive ? string.Empty : " · masquée du Live")}");

            // Largeurs de la bande au tempo courant ; durées écrites dans leur unité (E2).
            var bpm = _runtime.Engine.Snapshot.Tempo.Bpm is > 0 and var live ? live : 120;
            Steps = [.. scene.Steps.Select((s, i) => new StepStripItem(
                string.Create(CultureInfo.CurrentCulture, $"{i + 1}{(s.Name is { } n ? " · " + n : string.Empty)}"),
                s.Fade.ToSeconds(bpm),
                s.Hold.ToSeconds(bpm),
                StepColor(s),
                false,
                DurationField.Describe(s.Fade, s.Hold)))];
            SelectedStep = _session.EditStep;
            var step = scene.Steps[Math.Clamp(_session.EditStep, 0, scene.Steps.Count - 1)];
            StepTitle = string.Create(CultureInfo.CurrentCulture, $"Étape {_session.EditStep + 1}{(step.Name is { } name ? " · " + name : string.Empty)}");
            StepName = step.Name;
            StepFade.Load(step.Fade);
            StepHold.Load(step.Hold);
            StepHueFade = step.HueFade;
            StepAutoAdvance = step.AutoAdvance;
            StepCurve = SceneOptions.Curves.FirstOrDefault(c => c.Value == step.Curve) ?? SceneOptions.Curves[0];
            StepSwitch = SceneOptions.Switches.FirstOrDefault(c => c.Value == step.Switch) ?? SceneOptions.Switches[0];
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
