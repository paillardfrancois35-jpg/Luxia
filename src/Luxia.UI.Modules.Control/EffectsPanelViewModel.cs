using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Engine;
using Luxia.Engine.Model;
using Luxia.Fixtures.Model;
using Luxia.Hosting;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>
/// Panneau Effets (doc 16 §6, doc 60 §5) : les effets de l'étape éditée, la bibliothèque de modèles (EFF-007) et les
/// réglages de l'effet choisi, avec son dessin animé (forme, position de chaque membre). Comme partout dans l'écran
/// Contrôle, tout s'enregistre au réglage (un geste = une annulation) ; on écrit dans une scène en ÉDITION ou en AVEUGLE,
/// et l'effet se voit tout de suite, sur la sortie ou sur l'aperçu (EFF-006, CMD-017).
/// </summary>
public sealed partial class EffectsPanelViewModel : ViewModelBase
{
    private readonly LuxiaRuntime _runtime;
    private readonly ControlSession _session;
    private readonly IDialogService _dialogs;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private bool _loading;
    private SceneEffect? _effect;
    private IReadOnlyList<(string Name, double Lag)> _members = [];

    [ObservableProperty]
    private bool _hasScene;

    [ObservableProperty]
    private bool _canWrite;

    [ObservableProperty]
    private string _hint = string.Empty;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private EffectRow? _selectedEffect;

    [ObservableProperty]
    private EffectTemplateRow? _selectedTemplate;

    [ObservableProperty]
    private bool _newPerCell;

    [ObservableProperty]
    private bool _hasEffect;

    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private Choice<SceneEffectShape>? _shape;

    [ObservableProperty]
    private Choice<AttributeKind>? _attribute;

    [ObservableProperty]
    private double _period = 2;

    [ObservableProperty]
    private bool _inBeats;

    [ObservableProperty]
    private string _periodText = string.Empty;

    [ObservableProperty]
    private double _size = 100;

    [ObservableProperty]
    private double _center = 50;

    [ObservableProperty]
    private double _spread = 360;

    [ObservableProperty]
    private double _dutyPercent = 50;

    [ObservableProperty]
    private decimal _groupSize = 2;

    [ObservableProperty]
    private bool _relative;

    [ObservableProperty]
    private bool _perCell;

    [ObservableProperty]
    private Choice<EffectPhaseMode>? _phaseMode;

    [ObservableProperty]
    private Choice<EffectDirection>? _direction;

    [ObservableProperty]
    private Choice<Guid?>? _positionPalette;

    [ObservableProperty]
    private Choice<Guid?>? _theme;

    [ObservableProperty]
    private string _membersText = string.Empty;

    // Valeurs par défaut des molettes (clic droit) : celles du modèle d'où vient l'effet, sinon des valeurs neutres
    // (essai P6 : « Allumé % » valait 25 % à l'ajout du chenillard mais 50 % au double-clic).
    [ObservableProperty]
    private double _defaultPeriod = 2;

    [ObservableProperty]
    private double _defaultSize = 100;

    [ObservableProperty]
    private double _defaultCenter = 50;

    [ObservableProperty]
    private double _defaultSpread = 360;

    [ObservableProperty]
    private double _defaultDuty = 50;

    [ObservableProperty]
    private string _problems = string.Empty;

    [ObservableProperty]
    private string _accent = ControlColors.Accent;

    [ObservableProperty]
    private IReadOnlyList<Point>? _curve;

    [ObservableProperty]
    private IReadOnlyList<Point>? _dots;

    [ObservableProperty]
    private IReadOnlyList<Color>? _previewColors;

    [ObservableProperty]
    private bool _previewStepped;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIntensityShape), nameof(IsPositionShape), nameof(IsColorShape), nameof(FamilyShapes), nameof(FamilyText), nameof(ShapeLabel), nameof(UsesDuty), nameof(UsesGroups), nameof(UsesCenter), nameof(UsesColorList), nameof(UsesOwnColors), nameof(SizeMaximum), nameof(SizeStep), nameof(SizeLabel))]
    private SceneEffectShape _kind;

    /// <summary>Crée le panneau.</summary>
    public EffectsPanelViewModel(LuxiaRuntime runtime, ControlSession session, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(dialogs);
        _runtime = runtime;
        _session = session;
        _dialogs = dialogs;
        session.Changed += (_, _) => Load();
        runtime.Project.Changed += (_, _) => LoadLibrary();
        LoadLibrary();
        Load();
    }

    /// <summary>Formes proposées, dans l'ordre : intensité, mouvement, couleur.</summary>
    public static IReadOnlyList<Choice<SceneEffectShape>> Shapes { get; } =
    [
        new(SceneEffectShape.Sine, "〜 Sinus (vague douce)"),
        new(SceneEffectShape.Triangle, "⋀ Triangle"),
        new(SceneEffectShape.Square, "⊓ Carré (on / off)"),
        new(SceneEffectShape.SawUp, "⟋ Dent de scie montante"),
        new(SceneEffectShape.SawDown, "⟍ Dent de scie descendante"),
        new(SceneEffectShape.Pulse, "⚡ Impulsion"),
        new(SceneEffectShape.Random, "✳ Scintillement (au hasard)"),
        new(SceneEffectShape.Circle, "◯ Cercle (lyres)"),
        new(SceneEffectShape.Eight, "∞ Huit (lyres)"),
        new(SceneEffectShape.SweepPan, "↔ Balayage horizontal (lyres)"),
        new(SceneEffectShape.SweepTilt, "↕ Balayage vertical (lyres)"),
        new(SceneEffectShape.RandomSlow, "〰 Errance (lyres, au hasard)"),
        new(SceneEffectShape.Rainbow, "🌈 Arc-en-ciel"),
        new(SceneEffectShape.Alternate, "▦ Alternance de couleurs"),
        new(SceneEffectShape.Gradient, "▭ Dégradé de couleurs"),
    ];

    /// <summary>Formes de la famille de l'effet édité (une forme d'une autre famille ne se choisit pas ici : on change de modèle).</summary>
    public IReadOnlyList<Choice<SceneEffectShape>> FamilyShapes => IsPositionShape ? [.. Shapes.Skip(7).Take(5)] : IsColorShape ? [.. Shapes.Skip(12)] : [.. Shapes.Take(7)];

    /// <summary>Type de l'effet édité, avec ce qu'il anime (doc 16b).</summary>
    public string FamilyText => IsPositionShape ? "Mouvement — anime Pan / Tilt (lyres)" : IsColorShape ? "Couleur — écrit la couleur des appareils" : "Intensité — anime un attribut (intensité, couleur…)";

    /// <summary>Libellé de la liste des formes selon le type.</summary>
    public string ShapeLabel => IsPositionShape ? "Trajectoire" : IsColorShape ? "Mode de couleur" : "Courbe";

    /// <summary>Attributs qu'une forme d'intensité peut animer.</summary>
    public static IReadOnlyList<Choice<AttributeKind>> Attributes { get; } =
    [
        new(AttributeKind.Intensity, "Intensité"),
        new(AttributeKind.Red, "Rouge"),
        new(AttributeKind.Green, "Vert"),
        new(AttributeKind.Blue, "Bleu"),
        new(AttributeKind.White, "Blanc"),
        new(AttributeKind.Amber, "Ambre"),
        new(AttributeKind.Uv, "UV"),
        new(AttributeKind.Zoom, "Zoom"),
        new(AttributeKind.Focus, "Focus"),
    ];

    /// <summary>Répartitions du décalage (EFF-005).</summary>
    public static IReadOnlyList<Choice<EffectPhaseMode>> PhaseModes { get; } =
    [
        new(EffectPhaseMode.Linear, "Du premier au dernier"),
        new(EffectPhaseMode.Mirror, "Miroir : du centre vers les bords"),
        new(EffectPhaseMode.Groups, "Par groupes (un sur N)"),
        new(EffectPhaseMode.Random, "Au hasard (ordre fixe)"),
    ];

    /// <summary>Sens.</summary>
    public static IReadOnlyList<Choice<EffectDirection>> Directions { get; } =
    [
        new(EffectDirection.Forward, "Avant"),
        new(EffectDirection.Backward, "Arrière"),
        new(EffectDirection.PingPong, "Aller-retour"),
    ];

    /// <summary>Effets de l'étape éditée.</summary>
    public ObservableCollection<EffectRow> Effects { get; } = [];

    /// <summary>Modèles de la bibliothèque (EFF-007).</summary>
    public ObservableCollection<EffectTemplateRow> Templates { get; } = [];

    /// <summary>Palettes de position proposées comme centre (EFF-003).</summary>
    public ObservableCollection<Choice<Guid?>> PositionPalettes { get; } = [];

    /// <summary>Thèmes de couleurs proposés (PAL-010).</summary>
    public ObservableCollection<Choice<Guid?>> Themes { get; } = [];

    /// <summary>Palettes couleur cochables pour une alternance ou un dégradé.</summary>
    public ObservableCollection<EffectColorChip> ColorChips { get; } = [];

    /// <summary>Forme d'intensité (courbe).</summary>
    public bool IsIntensityShape => !IsPositionShape && !IsColorShape;

    /// <summary>Forme de position (plan Pan / Tilt).</summary>
    public bool IsPositionShape => Kind is SceneEffectShape.Circle or SceneEffectShape.Eight or SceneEffectShape.SweepPan or SceneEffectShape.SweepTilt or SceneEffectShape.RandomSlow;

    /// <summary>Forme de couleur.</summary>
    public bool IsColorShape => Kind is SceneEffectShape.Rainbow or SceneEffectShape.Alternate or SceneEffectShape.Gradient;

    /// <summary>La forme a un rapport cyclique.</summary>
    public bool UsesDuty => Kind is SceneEffectShape.Square or SceneEffectShape.Pulse;

    /// <summary>La répartition « par groupes » est choisie.</summary>
    public bool UsesGroups => PhaseMode?.Value == EffectPhaseMode.Groups;

    /// <summary>Le centre compte (intensité en absolu).</summary>
    public bool UsesCenter => IsIntensityShape && !Relative;

    /// <summary>La forme de couleur prend des couleurs (pas l'arc-en-ciel).</summary>
    public bool UsesColorList => Kind is SceneEffectShape.Alternate or SceneEffectShape.Gradient;

    /// <summary>Taille maximale : 100 % (intensité) ou 360° (position).</summary>
    public double SizeMaximum => IsPositionShape ? 360 : 100;

    /// <summary>Pas de la molette de taille.</summary>
    public double SizeStep => IsPositionShape ? 5 : 5;

    /// <summary>Libellé de la taille.</summary>
    public string SizeLabel => IsPositionShape ? "Taille (°)" : "Taille (%)";

    /// <summary>Anime le dessin (relu 20 fois par seconde).</summary>
    public void Refresh()
    {
        if (_effect is { } effect && _members.Count > 0)
        {
            Dots = DotsAt(effect, _clock.Elapsed.TotalSeconds);
        }
    }

    /// <summary>Ajoute le modèle choisi à l'étape, sur les appareils sélectionnés au plan (EFF-007).</summary>
    [RelayCommand]
    private void AddEffect()
    {
        var library = _runtime.Project.Effects.Templates;
        var template = SelectedTemplate?.Template ?? (library.Count > 0 ? library[0] : null);
        if (template is null)
        {
            return;
        }

        var targets = _session.OrderedSelection();
        if (targets.Count == 0)
        {
            Message = "Sélectionnez d'abord les appareils sur le plan (clic, Ctrl + clic, rectangle), dans l'ordre voulu.";
            return;
        }

        var effect = DefaultEffects.Apply(template, targets, NewPerCell);
        Message = _session.EditEffects(effects => [.. effects, effect], $"Ajouter l'effet « {template.Name} »");
        if (Message is null && effect.IsColor)
        {
            // MOT-041 : la couleur d'un appareil éteint ne se verrait pas.
            _session.LightTargets(targets, $"Ajouter l'effet « {template.Name} »");
        }
        else if (Message is null && !effect.IsPosition && effect.Attribute == AttributeKind.Intensity)
        {
            // EFF-011 : une intensité qui varie sur un PAR sans couleur ne se verrait pas (essai P6, exemple 7).
            _session.ColorTargets(targets, $"Ajouter l'effet « {template.Name} »");
        }

        if (Message is null)
        {
            _runtime.TraceUi("Contrôle", $"effet ajouté : {template.Name}");
            SelectedEffect = Effects.FirstOrDefault(e => e.Id == effect.Id);
        }
    }

    /// <summary>Retire l'effet choisi de l'étape.</summary>
    [RelayCommand]
    private void DeleteEffect()
    {
        if (_effect is { } effect)
        {
            Message = _session.EditEffects(effects => [.. effects.Where(e => e.Id != effect.Id)], $"Retirer l'effet « {effect.Name} »");
            _session.Commit();
        }
    }

    /// <summary>Duplique l'effet choisi (nouvel identifiant : il tourne indépendamment).</summary>
    [RelayCommand]
    private void DuplicateEffect()
    {
        if (_effect is { } effect)
        {
            var copy = effect with { Id = Guid.NewGuid(), Name = $"{effect.Name} (copie)" };
            Message = _session.EditEffects(effects => [.. effects, copy], "Dupliquer l'effet");
            _session.Commit();
            SelectedEffect = Effects.FirstOrDefault(e => e.Id == copy.Id);
        }
    }

    /// <summary>L'effet choisi prend les appareils sélectionnés au plan, dans l'ordre du plan (de gauche à droite).</summary>
    [RelayCommand]
    private void TakeSelection()
    {
        var targets = _session.OrderedSelection();
        if (targets.Count == 0)
        {
            Message = "Sélectionnez d'abord les appareils sur le plan.";
            return;
        }

        UpdateEffect(e => e with { Targets = targets }, "Appareils de l'effet");
        _session.Commit();
    }

    /// <summary>Range l'effet choisi dans la bibliothèque du projet, pour le réutiliser ailleurs (EFF-007).</summary>
    [RelayCommand]
    private async Task SaveAsTemplate()
    {
        if (_effect is not { } effect)
        {
            return;
        }

        var name = await _dialogs.AskTextAsync("Enregistrer comme modèle", "Nom du modèle d'effet :", effect.Name).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var category = effect.IsColor ? DefaultEffects.ColorCategory : effect.IsPosition ? DefaultEffects.MovementCategory : DefaultEffects.IntensityCategory;
        var template = new EffectTemplate { Name = name.Trim(), Category = category, Effect = effect with { Id = Guid.NewGuid(), Name = name.Trim(), Targets = [] } };
        try
        {
            _runtime.Project.SaveEffects(_runtime.Project.Effects with { Templates = [.. _runtime.Project.Effects.Templates, template] });
            LoadLibrary();
            SelectedTemplate = Templates.FirstOrDefault(t => t.Template.Id == template.Id);
            Message = $"Modèle « {template.Name} » ajouté à la bibliothèque.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Message = $"Enregistrement impossible pour l'instant ({ex.Message}).";
        }
    }

    /// <summary>Levé pour ouvrir la fenêtre « Thème de couleurs » ; la vue rappelle <see cref="CompleteTheme"/> à sa fermeture.</summary>
    public event EventHandler<ThemeEditorViewModel>? ThemeEditorRequested;

    /// <summary>Le thème choisi est un thème du projet (les thèmes livrés ne se modifient ni ne se retirent).</summary>
    public bool CanEditTheme => Theme?.Value is { } id && !DefaultPalettes.Themes.Any(t => t.Id == id);

    /// <summary>La forme de couleur n'a pas de thème : ses couleurs sont les pastilles cochées.</summary>
    public bool UsesOwnColors => UsesColorList && Theme?.Value is null;

    private Guid? _editedTheme;

    /// <summary>« + » : nouveau thème, à partir des couleurs de l'effet (thème choisi ou pastilles cochées).</summary>
    [RelayCommand]
    private void NewTheme()
    {
        var start = _effect is { } effect ? ThemeOrColors(effect) : [];
        _editedTheme = null;
        ThemeEditorRequested?.Invoke(this, new ThemeEditorViewModel("Nouveau thème de couleurs", "Mon thème", start));
    }

    /// <summary>« ✎ » : modifier le thème choisi (thèmes du projet seulement).</summary>
    [RelayCommand]
    private void EditTheme()
    {
        if (!CanEditTheme || _runtime.Project.Palettes.Palettes.FirstOrDefault(p => p.Id == Theme!.Value) is not { } theme)
        {
            Message = "Les thèmes livrés ne se modifient pas : « + » en crée un nouveau à partir de celui-ci.";
            return;
        }

        _editedTheme = theme.Id;
        ThemeEditorRequested?.Invoke(this, new ThemeEditorViewModel("Modifier le thème", theme.Name, theme.Colors));
    }

    /// <summary>Enregistre le thème saisi dans la fenêtre et le donne à l'effet choisi.</summary>
    public void CompleteTheme(ThemeEditorViewModel editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        if (!editor.Saved)
        {
            return;
        }

        var palettes = _runtime.Project.Palettes;
        var theme = _editedTheme is { } id && palettes.Palettes.FirstOrDefault(p => p.Id == id) is { } existing
            ? existing with { Name = editor.Name.Trim(), Colors = editor.Colors }
            : new Palette { Name = editor.Name.Trim(), Kind = PaletteKind.Theme, Colors = editor.Colors };
        try
        {
            _runtime.Project.SavePalettes(palettes with
            {
                Palettes = palettes.Palettes.Any(p => p.Id == theme.Id)
                    ? [.. palettes.Palettes.Select(p => p.Id == theme.Id ? theme : p)]
                    : [.. palettes.Palettes, theme],
            });
            Load();
            UpdateEffect(e => e with { ThemeId = theme.Id, Colors = [] }, "Thème de couleurs");
            Message = $"Thème « {theme.Name} » enregistré ; l'effet l'utilise.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Message = $"Enregistrement impossible pour l'instant ({ex.Message}).";
        }
    }

    /// <summary>« 🗑 » : retire un thème du projet (jamais un thème livré), après confirmation.</summary>
    [RelayCommand]
    private async Task DeleteTheme()
    {
        if (!CanEditTheme || _runtime.Project.Palettes.Palettes.FirstOrDefault(p => p.Id == Theme!.Value) is not { } theme)
        {
            Message = "Les thèmes livrés ne se retirent pas.";
            return;
        }

        var users = _runtime.Project.Scenes.Scenes.Count(sc => sc.Steps.Any(st => st.Effects.Any(e => e.ThemeId == theme.Id)));
        var warning = users > 0 ? $" {users} scène(s) l'utilisent : leurs effets n'auront plus de couleurs." : string.Empty;
        if (!await _dialogs.ConfirmAsync("Retirer le thème", $"Retirer le thème « {theme.Name} » du projet ?{warning}").ConfigureAwait(true))
        {
            return;
        }

        try
        {
            UpdateEffect(e => e with { ThemeId = null }, "Thème de couleurs");
            var palettes = _runtime.Project.Palettes;
            _runtime.Project.SavePalettes(palettes with { Palettes = [.. palettes.Palettes.Where(p => p.Id != theme.Id)] });
            Load();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Message = $"Enregistrement impossible pour l'instant ({ex.Message}).";
        }
    }

    /// <summary>
    /// Remet dans la bibliothèque les modèles livrés qui en ont été retirés (essai P6 : un modèle retiré par erreur ne
    /// pouvait plus revenir) ; les modèles du projet sont gardés.
    /// </summary>
    [RelayCommand]
    private void RestoreDefaultTemplates()
    {
        var library = _runtime.Project.Effects;
        var missing = DefaultEffects.Templates.Where(t => library.Templates.All(x => x.Id != t.Id)).ToList();
        if (missing.Count == 0)
        {
            Message = "Tous les modèles livrés sont déjà dans la bibliothèque.";
            return;
        }

        try
        {
            // Dans l'ordre livré, les modèles du projet à la suite.
            var order = DefaultEffects.Templates.Select(t => t.Id).ToList();
            var restored = library.Templates.Concat(missing)
                .OrderBy(t => order.IndexOf(t.Id) is var i && i >= 0 ? i : int.MaxValue)
                .ToList();
            _runtime.Project.SaveEffects(library with { Templates = restored });
            LoadLibrary();
            Message = $"{missing.Count} modèle(s) livré(s) remis dans la bibliothèque.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Message = $"Enregistrement impossible pour l'instant ({ex.Message}).";
        }
    }

    /// <summary>Retire le modèle choisi de la bibliothèque (les scènes gardent leur copie).</summary>
    [RelayCommand]
    private async Task DeleteTemplate()
    {
        if (SelectedTemplate is not { } row)
        {
            return;
        }

        if (!await _dialogs.ConfirmAsync("Retirer le modèle de la bibliothèque", $"Retirer le modèle « {row.Template.Name} » de la bibliothèque du projet ?\n\nCe n'est pas l'effet de la scène : les scènes gardent leurs effets. Un modèle livré retiré revient avec « Rétablir les modèles livrés ».").ConfigureAwait(true))
        {
            return;
        }

        try
        {
            _runtime.Project.SaveEffects(_runtime.Project.Effects with { Templates = [.. _runtime.Project.Effects.Templates.Where(t => t.Id != row.Template.Id)] });
            LoadLibrary();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Message = $"Enregistrement impossible pour l'instant ({ex.Message}).";
        }
    }

    partial void OnSelectedEffectChanged(EffectRow? value) => LoadEffect();

    partial void OnNameChanged(string? value) => UpdateEffect(e => e with { Name = string.IsNullOrWhiteSpace(value) ? e.Name : value.Trim() }, "Nom de l'effet");

    partial void OnShapeChanged(Choice<SceneEffectShape>? value)
    {
        if (value is null || _loading || _effect is not { } effect)
        {
            return;
        }

        // Changer de famille (intensité, mouvement, couleur) remet des réglages qui ont un sens pour la nouvelle forme.
        var next = new SceneEffect { Shape = value.Value };
        UpdateEffect(
            e => next.IsPosition != e.IsPosition
                ? e with { Shape = value.Value, Size = next.IsPosition ? 60 : 1, Relative = next.IsPosition, Spread = next.IsPosition ? 0 : 360 }
                : e with { Shape = value.Value },
            "Forme de l'effet");
        if (next.IsColor && !effect.IsColor)
        {
            _session.LightTargets(effect.Targets, "Forme de l'effet");
        }
    }

    partial void OnAttributeChanged(Choice<AttributeKind>? value)
    {
        if (value is not null)
        {
            UpdateEffect(e => e with { Attribute = value.Value }, "Attribut animé");
        }
    }

    partial void OnPeriodChanged(double value) => UpdateEffect(e => e with { Period = new Duration(Math.Max(0.05, value), InBeats ? DurationUnit.Beats : DurationUnit.Seconds) }, "Vitesse de l'effet");

    partial void OnInBeatsChanged(bool value)
    {
        if (_loading || _effect is not { } effect)
        {
            return;
        }

        // Même vitesse, exprimée dans l'autre unité (120 temps par minute tant que le tempo est fixe, GEN-023).
        var seconds = effect.Period.ToSeconds(_runtime.Engine.Bpm);
        var converted = value ? Math.Max(0.25, Math.Round(seconds * _runtime.Engine.Bpm / 60 * 4) / 4) : Math.Round(seconds, 2);
        UpdateEffect(e => e with { Period = new Duration(converted, value ? DurationUnit.Beats : DurationUnit.Seconds) }, "Unité de vitesse");
    }

    partial void OnSizeChanged(double value) => UpdateEffect(e => e with { Size = e.IsPosition ? value : value / 100 }, "Taille de l'effet");

    partial void OnCenterChanged(double value) => UpdateEffect(e => e with { Center = value / 100 }, "Centre de l'effet");

    partial void OnSpreadChanged(double value) => UpdateEffect(e => e with { Spread = value }, "Décalage entre appareils");

    partial void OnDutyPercentChanged(double value) => UpdateEffect(e => e with { DutyCycle = Math.Clamp(value / 100, 0.01, 1) }, "Durée allumée");

    partial void OnGroupSizeChanged(decimal value) => UpdateEffect(e => e with { GroupSize = Math.Max(1, (int)value) }, "Taille des groupes");

    partial void OnRelativeChanged(bool value)
    {
        OnPropertyChanged(nameof(UsesCenter));
        UpdateEffect(e => e with { Relative = value }, value ? "Effet relatif" : "Effet absolu");
    }

    partial void OnPerCellChanged(bool value) => UpdateEffect(e => e with { PerCell = value }, "Effet par cellule");

    partial void OnPhaseModeChanged(Choice<EffectPhaseMode>? value)
    {
        OnPropertyChanged(nameof(UsesGroups));
        if (value is not null)
        {
            UpdateEffect(e => e with { PhaseMode = value.Value }, "Répartition du décalage");
        }
    }

    partial void OnDirectionChanged(Choice<EffectDirection>? value)
    {
        if (value is not null)
        {
            UpdateEffect(e => e with { Direction = value.Value }, "Sens de l'effet");
        }
    }

    partial void OnPositionPaletteChanged(Choice<Guid?>? value)
    {
        if (value is not null)
        {
            UpdateEffect(e => e with { PositionPaletteId = value.Value }, "Centre du mouvement");
        }
    }

    partial void OnThemeChanged(Choice<Guid?>? value)
    {
        OnPropertyChanged(nameof(CanEditTheme));
        OnPropertyChanged(nameof(UsesOwnColors));
        if (value is not null)
        {
            UpdateEffect(e => e with { ThemeId = value.Value }, "Thème de couleurs");
        }
    }

    /// <summary>Une pastille de couleur cochée ou décochée : la liste des couleurs suit, dans l'ordre des pastilles.</summary>
    internal void OnChipToggled()
    {
        var colors = ColorChips.Where(c => c.IsChecked).Select(c => EffectColor.Palette(c.PaletteId)).ToList();
        UpdateEffect(e => e with { Colors = colors, ThemeId = colors.Count > 0 ? null : e.ThemeId }, "Couleurs de l'effet");
    }

    private void UpdateEffect(Func<SceneEffect, SceneEffect> change, string description)
    {
        if (_loading || _effect is not { } effect)
        {
            return;
        }

        Message = _session.EditEffects(effects => [.. effects.Select(e => e.Id == effect.Id ? change(e) : e)], description);
    }

    private void LoadLibrary()
    {
        var selected = SelectedTemplate?.Template.Id;
        Templates.Clear();
        foreach (var template in _runtime.Project.Effects.Templates)
        {
            Templates.Add(new EffectTemplateRow(template, $"{Icon(template.Effect.Shape)} {template.Name}", template.Category ?? string.Empty, template.Description ?? string.Empty));
        }

        SelectedTemplate = Templates.FirstOrDefault(t => t.Template.Id == selected) ?? Templates.FirstOrDefault();
    }

    private void Load()
    {
        _loading = true;
        try
        {
            HasScene = _session.EditScene is not null;
            CanWrite = HasScene && _session.Mode != EditMode.Live && !_session.IsLocked;
            Accent = _session.Mode == EditMode.Live ? ControlColors.Accent : ControlColors.Of(_session.Mode);
            Hint = !HasScene
                ? "Choisissez une scène à éditer (bande ✎ d'un bouton de scène), puis passez en ÉDITION ou 👁 AVEUGLE."
                : _session.IsLocked ? ControlSession.LockedReason
                : _session.Mode == EditMode.Live ? "Passez en ÉDITION (sur la sortie) ou 👁 AVEUGLE (sur l'aperçu) pour ajouter ou régler un effet de l'étape."
                : $"Étape {_session.EditStep + 1} : choisissez les appareils sur le plan, un modèle, puis « + Ajouter ». L'effet joue tout de suite.";

            var palettes = _runtime.Project.Palettes.Palettes;
            SyncChoices(PositionPalettes, [new(null, "(aucune : autour de la position de l'étape)"), .. palettes.Where(p => p.Kind == PaletteKind.Position).Select(p => new Choice<Guid?>(p.Id, p.Name))]);
            SyncChoices(Themes, [new(null, "Aucun"), .. palettes.Where(p => p.Kind == PaletteKind.Theme).Select(p => new Choice<Guid?>(p.Id, p.Name))]);
            var colorPalettes = palettes.Where(p => p.Kind == PaletteKind.Color && p.Light is not null).ToList();
            if (ColorChips.Count != colorPalettes.Count || !ColorChips.Select(c => c.PaletteId).SequenceEqual(colorPalettes.Select(p => p.Id)))
            {
                ColorChips.Clear();
                foreach (var palette in colorPalettes)
                {
                    ColorChips.Add(new EffectColorChip(this, palette.Id, palette.Name, palette.DisplayColor()));
                }
            }

            var selected = SelectedEffect?.Id;
            var names = _runtime.Project.Installation.Fixtures.ToDictionary(f => f.Id, f => f.Name);
            var rows = _session.StepEffects.Select(e => new EffectRow(e.Id, $"{Icon(e.Shape)} {e.Name ?? "Effet"}{Modified(e)}", Describe(e, names), ShapeColor(e))).ToList();
            if (!rows.SequenceEqual(Effects))
            {
                Effects.Clear();
                foreach (var row in rows)
                {
                    Effects.Add(row);
                }
            }

            SelectedEffect = Effects.FirstOrDefault(e => e.Id == selected) ?? Effects.FirstOrDefault();
        }
        finally
        {
            _loading = false;
        }

        LoadEffect();
    }

    /// <summary>« (modifié) » quand la forme n'est plus celle du modèle dont l'effet porte le nom.</summary>
    private string Modified(SceneEffect effect)
    {
        var model = _runtime.Project.Effects.Templates.FirstOrDefault(t => t.Name == effect.Name)?.Effect
            ?? DefaultEffects.Templates.FirstOrDefault(t => t.Name == effect.Name)?.Effect;
        return model is not null && model.Shape != effect.Shape ? " (modifié)" : string.Empty;
    }

    private void LoadEffect()
    {
        _loading = true;
        try
        {
            _effect = SelectedEffect is { } row ? _session.StepEffects.FirstOrDefault(e => e.Id == row.Id) : null;
            HasEffect = _effect is not null;
            if (_effect is not { } effect)
            {
                _members = [];
                Curve = null;
                Dots = null;
                PreviewColors = null;
                return;
            }

            Kind = effect.Shape;
            var model = _runtime.Project.Effects.Templates.FirstOrDefault(t => t.Name == effect.Name)?.Effect
                ?? DefaultEffects.Templates.FirstOrDefault(t => t.Name == effect.Name)?.Effect
                ?? new SceneEffect { Shape = effect.Shape, Size = effect.IsPosition ? 60 : 1 };
            DefaultPeriod = model.Period.Unit == DurationUnit.Bars ? model.Period.Value * Duration.BeatsPerBar : model.Period.Value;
            DefaultSize = model.IsPosition ? model.Size : Math.Round(model.Size * 100);
            DefaultCenter = Math.Round(model.Center * 100);
            DefaultSpread = model.Spread;
            DefaultDuty = Math.Round(model.DutyCycle * 100);
            if (Name?.Trim() != effect.Name)
            {
                Name = effect.Name;
            }

            Shape = Shapes.FirstOrDefault(s => s.Value == effect.Shape);
            Attribute = Attributes.FirstOrDefault(a => a.Value == effect.Attribute) ?? Attributes[0];
            InBeats = effect.Period.Unit != DurationUnit.Seconds;
            Period = effect.Period.Unit == DurationUnit.Bars ? effect.Period.Value * Duration.BeatsPerBar : effect.Period.Value;
            var seconds = effect.Period.ToSeconds(_runtime.Engine.Bpm);
            PeriodText = InBeats
                ? string.Create(CultureInfo.CurrentCulture, $"{Period:0.##} temps ({seconds:0.##} s)")
                : string.Create(CultureInfo.CurrentCulture, $"{Period:0.##} s · {1 / Math.Max(0.05, seconds):0.##} Hz");
            Size = effect.IsPosition ? effect.Size : Math.Round(effect.Size * 100);
            Center = Math.Round(effect.Center * 100);
            Spread = effect.Spread;
            DutyPercent = Math.Round(effect.DutyCycle * 100);
            GroupSize = effect.GroupSize;
            Relative = effect.Relative;
            PerCell = effect.PerCell;
            PhaseMode = PhaseModes.FirstOrDefault(p => p.Value == effect.PhaseMode);
            Direction = Directions.FirstOrDefault(d => d.Value == effect.Direction);
            PositionPalette = PositionPalettes.FirstOrDefault(p => p.Value == effect.PositionPaletteId) ?? PositionPalettes.FirstOrDefault();
            Theme = Themes.FirstOrDefault(t => t.Value == effect.ThemeId) ?? Themes.FirstOrDefault();
            var chosen = effect.Colors.Where(c => c.PaletteId is not null).Select(c => c.PaletteId!.Value).ToHashSet();
            foreach (var chip in ColorChips)
            {
                chip.SetChecked(chosen.Contains(chip.PaletteId));
            }

            var resolver = new ValueResolver(_runtime.Show.Patch, _runtime.Project.Palettes);
            var compiler = new EffectCompiler(_runtime.Show.Patch, resolver);
            var members = compiler.Members(effect, out _);
            _members = [.. members.Select((m, i) => (m.Cell > 0 ? $"{m.Fixture.Fixture.Name}·{m.Cell}" : m.Fixture.Fixture.Name, EffectCompiler.Lag(effect, i, members.Count)))];
            MembersText = _members.Count == 0 ? "aucun appareil" : string.Join(" → ", _members.Select(m => m.Name));
            var compiled = compiler.Compile(effect, _runtime.Engine.Snapshot.Show, out var problem);
            Problems = string.Join(" · ", EffectRules.Problems(effect).Select(p => p.Message).Append(problem).OfType<string>());
            BuildPreview(effect, compiled);
        }
        finally
        {
            _loading = false;
        }
    }

    private void BuildPreview(SceneEffect effect, EngineEffect compiled)
    {
        PreviewStepped = effect.Shape == SceneEffectShape.Alternate;
        if (effect.IsColor)
        {
            var colors = effect.Shape == SceneEffectShape.Rainbow
                ? Enumerable.Range(0, 12).Select(i => EffectCompiler.Hue(i / 12.0)).ToList()
                : ThemeOrColors(effect);
            PreviewColors = [.. colors.Select(c => Color.Parse(c.Hex))];
            Curve = null;
            Dots = DotsAt(effect, _clock.Elapsed.TotalSeconds);
            return;
        }

        PreviewColors = null;
        var shape = EffectCompiler.ToEngine(effect.Shape);
        var seed = compiled.Seed;
        Curve = [.. Enumerable.Range(0, 121).Select(i => PointAt(effect, shape, i / 120.0, seed, i / 120.0))];
        Dots = DotsAt(effect, _clock.Elapsed.TotalSeconds);
    }

    private List<LogicalColor> ThemeOrColors(SceneEffect effect)
    {
        var palettes = _runtime.Project.Palettes.Palettes;
        if (effect.ThemeId is { } themeId && palettes.FirstOrDefault(p => p.Id == themeId) is { } theme)
        {
            return [.. theme.Colors];
        }

        return [.. effect.Colors.Select(c => c.Color ?? palettes.FirstOrDefault(p => p.Id == c.PaletteId)?.Light).OfType<LogicalColor>()];
    }

    // Point du dessin pour une position dans le cycle : courbe (temps, niveau) ou plan (Pan, Tilt).
    private static Point PointAt(SceneEffect effect, EffectShape shape, double cycles, ulong seed, double x)
    {
        if (effect.IsPosition)
        {
            // Forme seule, à taille fixe : la taille réelle se lit sur la molette.
            return new Point(0.5 + (0.8 * EffectShapes.Offset(shape, cycles, EffectAxis.X, effect.DutyCycle, seed)), 0.5 + (0.8 * EffectShapes.Offset(shape, cycles, EffectAxis.Y, effect.DutyCycle, seed)));
        }

        var offset = EffectShapes.Offset(shape, cycles, EffectAxis.X, effect.DutyCycle, seed);
        var level = effect.Relative ? 0.5 + (effect.Size * offset) : effect.Center + (effect.Size * offset);
        return new Point(x, Math.Clamp(level, 0, 1));
    }

    private List<Point> DotsAt(SceneEffect effect, double seconds)
    {
        var period = Math.Max(0.05, effect.Period.ToSeconds(_runtime.Engine.Bpm));
        var phase = EffectShapes.Directed(effect.Direction, seconds / period);
        var shape = EffectCompiler.ToEngine(effect.Shape);
        var seed = EffectCompiler.SeedOf(effect.Id);
        var dots = new List<Point>(_members.Count);
        for (var i = 0; i < _members.Count; i++)
        {
            var cycles = phase - _members[i].Lag;
            var within = cycles - Math.Floor(cycles);
            dots.Add(effect.IsColor
                ? new Point(within, 0.5)
                : PointAt(effect, shape, cycles, seed + (ulong)i, within));
        }

        return dots;
    }

    private static void SyncChoices<T>(ObservableCollection<Choice<T>> target, IReadOnlyList<Choice<T>> items)
    {
        if (target.SequenceEqual(items))
        {
            return;
        }

        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    private string Describe(SceneEffect effect, Dictionary<Guid, string> names)
    {
        var who = effect.Targets.Count == 1 && effect.Targets[0].FixtureId is null
            ? Who(effect.Targets[0])
            : string.Join(", ", effect.Targets.Select(t => t.FixtureId is { } id ? names.GetValueOrDefault(id, "?") : Who(t)));
        var seconds = effect.Period.ToSeconds(_runtime.Engine.Bpm);
        return string.Create(CultureInfo.CurrentCulture, $"{who}{(effect.PerCell ? " (cellules)" : string.Empty)} · {seconds:0.##} s");
    }

    private string Who(ValueTarget target) =>
        target.SelectionId is { } selection ? _runtime.Project.Installation.Selections.FirstOrDefault(s => s.Id == selection)?.Name ?? "sélection ?"
        : target.Auto is { } auto ? auto.Model ?? auto.Category?.ToString() ?? "tous les appareils"
        : "?";

    /// <summary>Icône d'une forme (liste des effets, bibliothèque).</summary>
    public static string Icon(SceneEffectShape shape) => Shapes.FirstOrDefault(s => s.Value == shape)?.Label.Split(' ')[0] ?? "•";

    private static string ShapeColor(SceneEffect effect) => effect.IsColor ? "#D2A8FF" : effect.IsPosition ? "#79C0FF" : "#E3B341";
}
