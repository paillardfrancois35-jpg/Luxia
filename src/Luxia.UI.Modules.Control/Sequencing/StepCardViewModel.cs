using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Engine.Model;
using Luxia.Show.Model;
using Luxia.Show.Rules;
using Luxia.UI.Controls;
using Luxia.UI.Modules.Scenes;

namespace Luxia.UI.Modules.Control.Sequencing;

/// <summary>
/// Carte d'une étape de show (Q45) : identifiant, nom, initiale, macro-étape, choix entre transitions (priorité ou tirage), actions,
/// et sous elle les transitions qui en partent.
/// </summary>
public sealed partial class StepCardViewModel : ViewModelBase
{
    private readonly ShowEditorViewModel _editor;
    private bool _loading;

    [ObservableProperty]
    private string _idText;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _initial;

    [ObservableProperty]
    private bool _random;

    [ObservableProperty]
    private bool _avoidRepeat;

    [ObservableProperty]
    private Choice<Guid>? _macro;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private Choice<ShowActionKind> _newKind = ShowOptions.Actions[0];

    [ObservableProperty]
    private Choice<Guid>? _newTarget;

    [ObservableProperty]
    private decimal _newValue = 1;

    [ObservableProperty]
    private IReadOnlyList<Choice<Guid>> _newTargets = [];

    /// <summary>Crée la carte.</summary>
    public StepCardViewModel(ShowEditorViewModel editor, ShowStep step, ShowDefinition show)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(show);
        _editor = editor;
        Id = step.Id;
        _idText = step.Id;
        _name = step.Name;
        _loading = true;
        Refresh(show);
        OnNewKindChanged(NewKind);
        _loading = false;
    }

    /// <summary>Identifiant de l'étape.</summary>
    public string Id { get; private set; }

    /// <summary>Actions de l'étape, en clair.</summary>
    public ObservableCollection<ActionRow> Actions { get; } = [];

    /// <summary>Transitions qui partent de l'étape.</summary>
    public ObservableCollection<TransitionRowViewModel> Transitions { get; } = [];

    /// <summary>Éditeur.</summary>
    public ShowEditorViewModel Editor => _editor;

    /// <summary>L'action à ajouter demande une cible (scène, séquence, couche).</summary>
    public bool NewNeedsTarget => NewKind.Value is not (ShowActionKind.Smoke or ShowActionKind.Blackout or ShowActionKind.Variable);

    /// <summary>L'action à ajouter demande une valeur (niveau en %, vitesse, secondes).</summary>
    public bool NewNeedsValue => NewKind.Value is ShowActionKind.LayerLevel or ShowActionKind.Speed or ShowActionKind.Flash or ShowActionKind.Smoke or ShowActionKind.Blackout;

    /// <summary>Unité de la valeur de l'action à ajouter.</summary>
    public string NewValueUnit => NewKind.Value switch
    {
        ShowActionKind.LayerLevel => "%",
        ShowActionKind.Speed => "×",
        _ => "s",
    };

    /// <summary>Relit l'étape (après une modification) sans refaire la carte.</summary>
    public void Refresh(ShowDefinition show)
    {
        ArgumentNullException.ThrowIfNull(show);
        var step = show.Steps.FirstOrDefault(s => s.Id == Id);
        if (step is null)
        {
            return;
        }

        var was = _loading;
        _loading = true;
        Initial = step.Initial;
        Random = step.Choice == StepChoice.Random;
        AvoidRepeat = step.AvoidRepeat;
        Macro = _editor.MacroChoices.FirstOrDefault(c => c.Value == (step.MacroShowId ?? Guid.Empty));
        _loading = was;
        Actions.Clear();
        for (var i = 0; i < step.Actions.Count; i++)
        {
            Actions.Add(new ActionRow(this, i, ShowTexts.Describe(step.Actions[i], _editor.NameOf)));
        }

        var transitions = show.Transitions.Select((t, i) => (t, i)).Where(x => (x.t.From.Count > 0 ? x.t.From[0] : null) == Id).ToList();
        if (Transitions.Select(t => t.Index).SequenceEqual(transitions.Select(x => x.i)))
        {
            foreach (var row in Transitions)
            {
                row.Refresh(show.Transitions[row.Index]);
            }
        }
        else
        {
            Transitions.Clear();
            foreach (var (transition, index) in transitions)
            {
                Transitions.Add(new TransitionRowViewModel(_editor, index, transition));
            }
        }
    }

    /// <summary>Supprime l'étape.</summary>
    [RelayCommand]
    private void Delete() => _editor.DeleteStep(Id);

    /// <summary>Nouvelle transition depuis l'étape.</summary>
    [RelayCommand]
    private void AddTransition() => _editor.AddTransition(Id);

    /// <summary>Ajoute l'action réglée en bas de la carte.</summary>
    [RelayCommand]
    private void AddAction()
    {
        var kind = NewKind.Value;
        if (NewNeedsTarget && NewTarget is null)
        {
            return;
        }

        var target = NewTarget?.Value;
        var value = (double)NewValue;
        var action = kind switch
        {
            ShowActionKind.Play or ShowActionKind.Launch or ShowActionKind.Stop => new ShowAction { Kind = kind, SceneId = target },
            ShowActionKind.Speed => new ShowAction { Kind = kind, SceneId = target, Value = value },
            ShowActionKind.Flash => new ShowAction { Kind = kind, SceneId = target, Seconds = value },
            ShowActionKind.PlaySequence or ShowActionKind.LaunchSequence or ShowActionKind.StopSequence => new ShowAction { Kind = kind, SequenceId = target },
            ShowActionKind.StopLayer => new ShowAction { Kind = kind, LayerId = target },
            ShowActionKind.LayerLevel => new ShowAction { Kind = kind, LayerId = target, Value = Math.Clamp(value, 0, 100) / 100 },
            ShowActionKind.Smoke or ShowActionKind.Blackout => new ShowAction { Kind = kind, Seconds = value },
            _ => new ShowAction { Kind = ShowActionKind.Variable, Variable = (_editor.Draft is { Variables.Count: > 0 } d ? d.Variables[0].Name : null) ?? "compteur", Value = 1 },
        };
        _editor.UpdateStep(Id, s => s with { Actions = [.. s.Actions, action] }, $"Action « {NewKind.Label} »");
        if (kind == ShowActionKind.Variable && _editor.Draft is { Variables.Count: 0 })
        {
            _editor.Variables = "compteur";
        }
    }

    /// <summary>Retire une action.</summary>
    public void RemoveAction(int index) => _editor.UpdateStep(Id, s => s with { Actions = [.. s.Actions.Where((_, i) => i != index)] }, "Retirer l'action");

    partial void OnIdTextChanged(string value)
    {
        if (!_loading && _editor.RenameStep(Id, value))
        {
            Id = value.Trim();
        }
    }

    partial void OnNameChanged(string value)
    {
        if (!_loading)
        {
            _editor.UpdateStep(Id, s => s with { Name = value }, "Nom de l'étape");
        }
    }

    partial void OnInitialChanged(bool value)
    {
        if (!_loading)
        {
            _editor.UpdateStep(Id, s => s with { Initial = value }, value ? "Étape initiale" : "Plus initiale");
        }
    }

    partial void OnRandomChanged(bool value)
    {
        if (!_loading)
        {
            _editor.UpdateStep(Id, s => s with { Choice = value ? StepChoice.Random : StepChoice.Priority }, value ? "Tirage au sort" : "Priorité");
        }
    }

    partial void OnAvoidRepeatChanged(bool value)
    {
        if (!_loading)
        {
            _editor.UpdateStep(Id, s => s with { AvoidRepeat = value }, "Éviter la même branche");
        }
    }

    partial void OnMacroChanged(Choice<Guid>? value)
    {
        if (!_loading && value is not null)
        {
            _editor.UpdateStep(Id, s => s with { MacroShowId = value.Value == Guid.Empty ? null : value.Value }, "Macro-étape");
        }
    }

    partial void OnNewKindChanged(Choice<ShowActionKind> value)
    {
        NewTargets = value.Value switch
        {
            ShowActionKind.PlaySequence or ShowActionKind.LaunchSequence or ShowActionKind.StopSequence => _editor.SequenceChoices,
            ShowActionKind.StopLayer or ShowActionKind.LayerLevel => _editor.LayerChoices,
            ShowActionKind.Smoke or ShowActionKind.Blackout or ShowActionKind.Variable => [],
            _ => _editor.SceneChoices,
        };
        NewTarget = NewTargets.FirstOrDefault();
        NewValue = value.Value switch
        {
            ShowActionKind.LayerLevel => 100,
            ShowActionKind.Smoke => 3,
            ShowActionKind.Flash or ShowActionKind.Blackout => 0.5m,
            _ => 1,
        };
        OnPropertyChanged(nameof(NewNeedsTarget));
        OnPropertyChanged(nameof(NewNeedsValue));
        OnPropertyChanged(nameof(NewValueUnit));
    }
}

/// <summary>Une action d'une carte.</summary>
/// <param name="card">Carte.</param>
/// <param name="index">Rang dans l'étape.</param>
/// <param name="text">Texte.</param>
public sealed partial class ActionRow(StepCardViewModel card, int index, string text) : ViewModelBase
{
    /// <summary>Texte de l'action.</summary>
    public string Text { get; } = text;

    /// <summary>Retire l'action.</summary>
    [RelayCommand]
    private void Remove() => card.RemoveAction(index);
}

/// <summary>
/// Une transition sous la carte de son étape amont : étapes amont et aval (identifiants séparés par des virgules : convergence ou
/// divergence en ET), réceptivité simple et ses réglages, quantification, poids, priorité (ordre).
/// </summary>
public sealed partial class TransitionRowViewModel : ViewModelBase
{
    private readonly ShowEditorViewModel _editor;
    private bool _loading;

    [ObservableProperty]
    private string _from = string.Empty;

    [ObservableProperty]
    private string _to = string.Empty;

    [ObservableProperty]
    private Choice<ConditionKind>? _kind;

    [ObservableProperty]
    private decimal _durationValue = 8;

    [ObservableProperty]
    private Choice<DurationUnit> _durationUnit = ShowOptions.DurationUnits[0];

    [ObservableProperty]
    private decimal _percent = 50;

    [ObservableProperty]
    private Choice<int> _levelMin = ShowOptions.EnergyLevels[1];

    [ObservableProperty]
    private Choice<int> _levelMax = ShowOptions.EnergyLevels[3];

    [ObservableProperty]
    private decimal _tempoMin = 90;

    [ObservableProperty]
    private decimal _tempoMax = 140;

    [ObservableProperty]
    private Choice<Guid>? _scene;

    [ObservableProperty]
    private Choice<Guid>? _sequence;

    [ObservableProperty]
    private decimal _count = 1;

    [ObservableProperty]
    private string _variable = string.Empty;

    [ObservableProperty]
    private Choice<Comparison> _comparison = ShowOptions.Comparisons[0];

    [ObservableProperty]
    private decimal _number = 1;

    [ObservableProperty]
    private string _styles = string.Empty;

    [ObservableProperty]
    private Choice<ShowQuantize> _every = ShowOptions.Quantizes[2];

    [ObservableProperty]
    private Choice<ShowQuantize> _quantize = ShowOptions.Quantizes[0];

    [ObservableProperty]
    private decimal _weight = 1;

    [ObservableProperty]
    private string _summary = string.Empty;

    /// <summary>Crée la ligne.</summary>
    public TransitionRowViewModel(ShowEditorViewModel editor, int index, ShowTransition transition)
    {
        _editor = editor;
        Index = index;
        Refresh(transition);
    }

    /// <summary>Rang de la transition dans le show.</summary>
    public int Index { get; }

    /// <summary>La réceptivité est une combinaison ET / OU / NON : affichée seulement (elle s'écrit dans le fichier).</summary>
    public bool IsComposite { get; private set; }

    /// <summary>Réceptivité simple, éditable.</summary>
    public bool IsSimple => !IsComposite;

    /// <summary>Réglages visibles selon la nature de la réceptivité.</summary>
    public bool ShowsDuration => Kind?.Value == ConditionKind.After;

    /// <summary>Pourcentage (seuil d'énergie, probabilité).</summary>
    public bool ShowsPercent => Kind?.Value is ConditionKind.EnergyAbove or ConditionKind.EnergyBelow or ConditionKind.Random;

    /// <summary>Niveaux d'énergie.</summary>
    public bool ShowsLevels => Kind?.Value == ConditionKind.EnergyLevel;

    /// <summary>Tempo.</summary>
    public bool ShowsTempo => Kind?.Value == ConditionKind.Tempo;

    /// <summary>Scène.</summary>
    public bool ShowsScene => Kind?.Value == ConditionKind.SceneEnded;

    /// <summary>Séquence.</summary>
    public bool ShowsSequence => Kind?.Value is ConditionKind.SequenceEnded or ConditionKind.SequenceLoops;

    /// <summary>Nombre de passages.</summary>
    public bool ShowsCount => Kind?.Value == ConditionKind.SequenceLoops;

    /// <summary>Variable.</summary>
    public bool ShowsVariable => Kind?.Value == ConditionKind.Variable;

    /// <summary>Styles.</summary>
    public bool ShowsStyles => Kind?.Value == ConditionKind.Style;

    /// <summary>Fréquence du tirage.</summary>
    public bool ShowsEvery => Kind?.Value == ConditionKind.Random;

    /// <summary>Relit la transition.</summary>
    public void Refresh(ShowTransition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);
        _loading = true;
        try
        {
            From = string.Join(", ", transition.From);
            To = string.Join(", ", transition.To);
            var condition = transition.Condition;
            IsComposite = condition.Kind is ConditionKind.All or ConditionKind.Any or ConditionKind.Not;
            Kind = ShowOptions.Conditions.FirstOrDefault(c => c.Value == condition.Kind);
            if (condition.Duration is { } duration)
            {
                DurationValue = (decimal)duration.Value;
                DurationUnit = ShowOptions.DurationUnits.FirstOrDefault(u => u.Value == duration.Unit) ?? ShowOptions.DurationUnits[0];
            }

            Percent = (decimal)Math.Round(condition.Value * 100);
            LevelMin = ShowOptions.EnergyLevels[Math.Clamp((int)Math.Round(condition.Min ?? 0), 0, 3)];
            LevelMax = ShowOptions.EnergyLevels[Math.Clamp((int)Math.Round(condition.Max ?? 3), 0, 3)];
            TempoMin = (decimal)(condition.Min ?? 90);
            TempoMax = (decimal)(condition.Max ?? 140);
            Scene = _editor.SceneChoices.FirstOrDefault(c => c.Value == condition.SceneId);
            Sequence = _editor.SequenceChoices.FirstOrDefault(c => c.Value == condition.SequenceId);
            Count = condition.Count;
            Variable = condition.Variable ?? string.Empty;
            Comparison = ShowOptions.Comparisons.FirstOrDefault(c => c.Value == condition.Comparison) ?? ShowOptions.Comparisons[0];
            Number = (decimal)condition.Value;
            Styles = string.Join(", ", condition.Styles);
            Every = ShowOptions.Quantizes.FirstOrDefault(q => q.Value == condition.Every) ?? ShowOptions.Quantizes[2];
            Quantize = ShowOptions.Quantizes.FirstOrDefault(q => q.Value == transition.Quantize) ?? ShowOptions.Quantizes[0];
            Weight = (decimal)transition.Weight;
            Summary = $"{ShowTexts.Label(transition, _editor.NameOf)} → {string.Join(", ", transition.To)}{(transition.Quantize == ShowQuantize.None ? string.Empty : " · " + ShowTexts.Quantize(transition.Quantize))}";
            NotifyVisibility();
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>Supprime la transition.</summary>
    [RelayCommand]
    private void Delete() => _editor.DeleteTransition(Index);

    /// <summary>Monte la transition d'un rang (plus prioritaire).</summary>
    [RelayCommand]
    private void MoveUp() => _editor.MoveTransition(Index, -1);

    /// <summary>Descend la transition d'un rang.</summary>
    [RelayCommand]
    private void MoveDown() => _editor.MoveTransition(Index, +1);

    partial void OnFromChanged(string value) => Edit(t => t with { From = Ids(value) is { Count: > 0 } ids ? ids : t.From }, "Étapes amont");

    partial void OnToChanged(string value) => Edit(t => t with { To = Ids(value) is { Count: > 0 } ids ? ids : t.To }, "Étapes aval");

    partial void OnKindChanged(Choice<ConditionKind>? value)
    {
        NotifyVisibility();
        if (value is not null)
        {
            EditCondition(c => Defaults(c with { Kind = value.Value }), "Condition");
        }
    }

    partial void OnDurationValueChanged(decimal value) => EditCondition(c => c with { Duration = new Duration((double)value, DurationUnit.Value) }, "Durée");

    partial void OnDurationUnitChanged(Choice<DurationUnit> value) => EditCondition(c => c with { Duration = new Duration((double)DurationValue, value.Value) }, "Unité de la durée");

    partial void OnPercentChanged(decimal value) => EditCondition(c => c with { Value = (double)Math.Clamp(value, 0, 100) / 100 }, "Seuil");

    partial void OnLevelMinChanged(Choice<int> value) => EditCondition(c => c with { Min = value.Value }, "Énergie minimale");

    partial void OnLevelMaxChanged(Choice<int> value) => EditCondition(c => c with { Max = value.Value }, "Énergie maximale");

    partial void OnTempoMinChanged(decimal value) => EditCondition(c => c with { Min = (double)value }, "Tempo minimal");

    partial void OnTempoMaxChanged(decimal value) => EditCondition(c => c with { Max = (double)value }, "Tempo maximal");

    partial void OnSceneChanged(Choice<Guid>? value) => EditCondition(c => c with { SceneId = value?.Value }, "Scène");

    partial void OnSequenceChanged(Choice<Guid>? value) => EditCondition(c => c with { SequenceId = value?.Value }, "Séquence");

    partial void OnCountChanged(decimal value) => EditCondition(c => c with { Count = Math.Max(1, (int)value) }, "Nombre de passages");

    partial void OnVariableChanged(string value) => EditCondition(c => c with { Variable = value.Trim() }, "Variable");

    partial void OnComparisonChanged(Choice<Comparison> value) => EditCondition(c => c with { Comparison = value.Value }, "Comparaison");

    partial void OnNumberChanged(decimal value) => EditCondition(c => c with { Value = (double)value }, "Valeur");

    partial void OnStylesChanged(string value) => EditCondition(c => c with { Styles = [.. value.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0)] }, "Styles");

    partial void OnEveryChanged(Choice<ShowQuantize> value) => EditCondition(c => c with { Every = value.Value }, "Fréquence du tirage");

    partial void OnQuantizeChanged(Choice<ShowQuantize> value) => Edit(t => t with { Quantize = value.Value }, "Quantification");

    partial void OnWeightChanged(decimal value) => Edit(t => t with { Weight = Math.Max(0.01, (double)value) }, "Poids");

    private static List<string> Ids(string text) => [.. text.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.Ordinal)];

    // Valeurs de départ sensées quand on change la nature de la réceptivité.
    private ShowCondition Defaults(ShowCondition condition) => condition.Kind switch
    {
        ConditionKind.After => condition with { Duration = condition.Duration ?? new Duration(8, Engine.Model.DurationUnit.Bars) },
        ConditionKind.EnergyLevel => condition with { Min = condition.Min ?? 1, Max = condition.Max ?? 3 },
        ConditionKind.EnergyAbove or ConditionKind.EnergyBelow or ConditionKind.Random => condition with { Value = condition.Value is > 0 and <= 1 ? condition.Value : 0.5 },
        ConditionKind.Variable => condition with { Variable = condition.Variable ?? (_editor.Draft is { Variables.Count: > 0 } d ? d.Variables[0].Name : null), Value = condition.Value is 0 ? 1 : condition.Value },
        ConditionKind.SequenceEnded or ConditionKind.SequenceLoops => condition with { SequenceId = condition.SequenceId ?? (_editor.SequenceChoices.Count > 0 ? _editor.SequenceChoices[0].Value : null) },
        ConditionKind.SceneEnded => condition with { SceneId = condition.SceneId ?? (_editor.SceneChoices.Count > 0 ? _editor.SceneChoices[0].Value : null) },
        ConditionKind.Tempo => condition with { Min = condition.Min ?? 90, Max = condition.Max ?? 140 },
        _ => condition,
    };

    private void EditCondition(Func<ShowCondition, ShowCondition> change, string description) => Edit(t => t with { Condition = change(t.Condition) }, description);

    private void Edit(Func<ShowTransition, ShowTransition> change, string description)
    {
        if (!_loading && !IsComposite)
        {
            _editor.UpdateTransition(Index, change, description);
        }
        else if (!_loading && description is "Étapes amont" or "Étapes aval" or "Quantification" or "Poids")
        {
            _editor.UpdateTransition(Index, change, description);
        }
    }

    private void NotifyVisibility()
    {
        foreach (var name in new[] { nameof(IsComposite), nameof(IsSimple), nameof(ShowsDuration), nameof(ShowsPercent), nameof(ShowsLevels), nameof(ShowsTempo), nameof(ShowsScene), nameof(ShowsSequence), nameof(ShowsCount), nameof(ShowsVariable), nameof(ShowsStyles), nameof(ShowsEvery) })
        {
            OnPropertyChanged(name);
        }
    }

    /// <summary>Texte de priorité (« 1re », « 2e »).</summary>
    public string Rank => string.Create(CultureInfo.CurrentCulture, $"#{Index + 1}");
}
