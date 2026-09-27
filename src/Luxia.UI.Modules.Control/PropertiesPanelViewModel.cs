using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Engine.Model;
using Luxia.Fixtures.Model;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Scenes.Model;
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

    /// <summary>Couches proposées.</summary>
    public ObservableCollection<Choice<Guid>> Layers { get; } = [];

    /// <summary>Contenu de l'étape choisie, lisible.</summary>
    public ObservableCollection<StepValueRow> StepValues { get; } = [];

    /// <summary>Couleurs proposées pour la scène.</summary>
    public static IReadOnlyList<string> Colors => ColumnsPanelViewModel.SceneColors;

    /// <summary>Choix de boucle.</summary>
    public static IReadOnlyList<Choice<LoopMode>> LoopOptions => SceneOptions.Loops;

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

    /// <summary>Lance la scène éditée (essai dans son contexte, SCN-034).</summary>
    [RelayCommand]
    private void Test()
    {
        if (_session.EditScene is { } scene)
        {
            _session.Commit();
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

    partial void OnFadeInSecondsChanged(decimal? value) => Update(s => s with { FadeIn = value is { } v ? Duration.FromSeconds((double)Math.Max(0, v)) : null }, "Fondu d'entrée");

    partial void OnFadeOutSecondsChanged(decimal? value) => Update(s => s with { FadeOut = value is { } v ? Duration.FromSeconds((double)Math.Max(0, v)) : null }, "Fondu de sortie");

    partial void OnNotesChanged(string? value) => Update(s => s with { Notes = string.IsNullOrWhiteSpace(value) ? null : value }, "Notes");

    partial void OnStepNameChanged(string? value) => UpdateStep(s => s with { Name = string.IsNullOrWhiteSpace(value) ? null : value.Trim() }, "Nom de l'étape");

    partial void OnStepFadeSecondsChanged(decimal value) => UpdateStep(s => s with { Fade = Duration.FromSeconds((double)Math.Max(0, value)) }, "Fondu de l'étape");

    partial void OnStepHoldSecondsChanged(decimal value) => UpdateStep(s => s with { Hold = Duration.FromSeconds((double)Math.Max(0, value)) }, "Maintien de l'étape");

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
            FillStepValues(step);
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
