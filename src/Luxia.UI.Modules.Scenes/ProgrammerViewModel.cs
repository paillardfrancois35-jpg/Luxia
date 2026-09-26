using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Engine;
using Luxia.Engine.Model;
using Luxia.Fixtures.Model;
using Luxia.Fixtures.Rules;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Patch.Rules;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Scenes;

/// <summary>
/// Programmeur (doc 16 §4) : on sélectionne des appareils, on règle leurs attributs avec l'outil adapté, puis on
/// enregistre dans une étape. Seuls les attributs <b>touchés</b> sont retenus (SCN-032). Hors aveugle, les réglages
/// sont des surcharges d'attributs (étape 5 de la chaîne, CMD-021) : on les voit sur la sortie et au simulateur ;
/// en aveugle, ils vont au moteur d'aperçu seulement (SCN-035, GEN-063).
/// </summary>
public sealed partial class ProgrammerViewModel : ViewModelBase
{
    private static readonly AttributeKind[] ColorAttributes =
    [
        AttributeKind.Red, AttributeKind.Green, AttributeKind.Blue, AttributeKind.White, AttributeKind.WarmWhite,
        AttributeKind.Amber, AttributeKind.Uv, AttributeKind.Cyan, AttributeKind.Magenta, AttributeKind.Yellow, AttributeKind.Lime,
    ];

    private readonly LuxiaRuntime _runtime;
    private IReadOnlyList<SceneValue> _values = [];
    private IReadOnlyList<SceneValue> _clipboard = [];
    private Dictionary<(Guid Fixture, string Key), double> _pushed = [];
    private RenderEngine? _pushedTo;
    private ValueTarget? _selectionTarget;
    private bool _blind;

    [ObservableProperty]
    private string _selectionSummary = "Aucun appareil sélectionné";

    [ObservableProperty]
    private bool _hasSelection;

    [ObservableProperty]
    private bool _isModified;

    [ObservableProperty]
    private string _content = "Programmeur vide";

    /// <summary>Fondu propre des prochains réglages, en secondes (SCN-011) ; vide = fondu de l'étape.</summary>
    [ObservableProperty]
    private decimal? _valueFade;

    /// <summary>Retard réparti sur la sélection, en secondes (« fan », SCN-010) ; vide = aucun.</summary>
    [ObservableProperty]
    private decimal? _valueSpread;

    /// <summary>Crée le programmeur.</summary>
    public ProgrammerViewModel(LuxiaRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
        Color = new ColorToolViewModel(this);
    }

    /// <summary>Levé quand le contenu ou la sélection change (grilles de palettes automatiques, boutons).</summary>
    public event EventHandler? Changed;

    /// <summary>Appareils patchés.</summary>
    public ObservableCollection<ProgrammerFixtureViewModel> Fixtures { get; } = [];

    /// <summary>Sélections enregistrées et automatiques.</summary>
    public ObservableCollection<SelectionShortcut> Shortcuts { get; } = [];

    /// <summary>Outil intensité (null si la sélection n'a pas d'intensité).</summary>
    public ObservableCollection<LevelToolViewModel> IntensityTools { get; } = [];

    /// <summary>Outil couleur.</summary>
    public ColorToolViewModel Color { get; }

    /// <summary>La sélection a des émetteurs de couleur ou une roue.</summary>
    [ObservableProperty]
    private bool _hasColor;

    /// <summary>Outils Pan / Tilt.</summary>
    public ObservableCollection<LevelToolViewModel> PositionTools { get; } = [];

    /// <summary>Outils des autres attributs (faisceau, strobe, programmes…).</summary>
    public ObservableCollection<LevelToolViewModel> OtherTools { get; } = [];

    /// <summary>Contenu du programmeur (valeurs touchées, dans l'ordre).</summary>
    public IReadOnlyList<SceneValue> Values => _values;

    /// <summary>Appareils cochés.</summary>
    public IReadOnlyList<FixtureInfo> SelectedFixtures => [.. Fixtures.Where(f => f.IsSelected).Select(f => f.Info)];

    /// <summary>Vrai pendant l'affichage des valeurs lues au moteur : les outils ne renvoient rien.</summary>
    internal bool IsRefreshing { get; private set; }

    /// <summary>Aveugle (SCN-035) : les réglages ne vont plus à la sortie mais au moteur d'aperçu.</summary>
    public bool Blind
    {
        get => _blind;
        set
        {
            if (_blind == value)
            {
                return;
            }

            // Les surcharges déjà envoyées au moteur quitté sont libérées, puis renvoyées à l'autre.
            ReleasePushed();
            _blind = value;
            _runtime.PreviewActive = value;
            OnPropertyChanged();
            Push();
        }
    }

    /// <summary>Moteur qui reçoit les réglages : la sortie, ou l'aperçu en aveugle.</summary>
    public RenderEngine TargetEngine => _blind ? _runtime.Preview : _runtime.Engine;

    /// <summary>Relit le patch (ouverture, modification de l'installation) en gardant la sélection.</summary>
    public void ReloadPatch()
    {
        var selected = Fixtures.Where(f => f.IsSelected).Select(f => f.Id).ToHashSet();
        Fixtures.Clear();
        var patch = _runtime.Show.Patch;
        foreach (var info in patch.Fixtures)
        {
            var row = new ProgrammerFixtureViewModel(this, info);
            row.SetSelected(selected.Contains(info.Fixture.Id));
            Fixtures.Add(row);
        }

        Shortcuts.Clear();
        var present = patch.Fixtures.Where(f => !f.Absent).Select(f => f.Fixture).ToList();
        var library = _runtime.Project.FixtureLibrary;
        foreach (var auto in AutoSelections.Build(present, f => library?.Find(f.FixtureTypeId)))
        {
            var target = new ValueTarget { Auto = new AutoSelectionTarget(auto.Kind, auto.Category, auto.ModelDisplayName) };
            Shortcuts.Add(new SelectionShortcut(auto.Title(CategoryLabel), target, [.. auto.Items.Select(f => f.Id)], "#8B949E"));
        }

        foreach (var selection in _runtime.Project.Installation.Selections)
        {
            Shortcuts.Add(new SelectionShortcut(selection.Name, ValueTarget.Selection(selection.Id), [.. selection.Items.Select(i => i.FixtureId)], selection.Color));
        }

        RebuildTools();
    }

    /// <summary>Choisit une sélection enregistrée ou automatique : les réglages suivants la visent.</summary>
    [RelayCommand]
    private void Select(SelectionShortcut? shortcut)
    {
        if (shortcut is null)
        {
            return;
        }

        var members = shortcut.Members.ToHashSet();
        foreach (var fixture in Fixtures)
        {
            fixture.SetSelected(members.Contains(fixture.Id));
        }

        _selectionTarget = shortcut.Target;
        RebuildTools();
    }

    /// <summary>Désélectionne tout.</summary>
    [RelayCommand]
    private void SelectNone()
    {
        foreach (var fixture in Fixtures)
        {
            fixture.SetSelected(false);
        }

        _selectionTarget = null;
        RebuildTools();
    }

    /// <summary>Une case d'appareil a été cochée ou décochée à la main : les réglages visent désormais chaque appareil.</summary>
    internal void OnFixtureToggled()
    {
        _selectionTarget = null;
        RebuildTools();
    }

    /// <summary>Remplace tout le contenu (charger une étape).</summary>
    public void Load(IReadOnlyList<SceneValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = values;
        IsModified = false;
        Push();
    }

    /// <summary>Le contenu a été enregistré dans une étape.</summary>
    public void MarkRecorded() => IsModified = false;

    /// <summary>Vide le programmeur : les surcharges sont libérées.</summary>
    [RelayCommand]
    public void Clear()
    {
        _values = [];
        IsModified = false;
        Push();
    }

    /// <summary>Règle un attribut sur la sélection (fader d'un outil).</summary>
    internal void SetLevel(LevelToolViewModel tool, double level)
    {
        SetForTargets(target => new SceneValue { Target = target, Attribute = tool.Attribute, Level = level });
        Push();
    }

    /// <summary>
    /// Clic sur une plage : sur une sélection dont tous les appareils ont la même plage, la valeur vise la sélection ;
    /// sinon chaque appareil reçoit sa propre plage de même nom (bornes propres à son modèle).
    /// </summary>
    internal void SetRange(LevelToolViewModel tool, RangeChoice choice)
    {
        var selected = SelectedFixtures;
        var perFixture = selected
            .Select(f => (Fixture: f, Channel: f.Channels.FirstOrDefault(c => c.Attribute == tool.Attribute)))
            .Where(x => x.Channel is not null)
            .Select(x => (x.Fixture, x.Channel, Range: x.Channel!.Capabilities.FirstOrDefault(c => c.Label == choice.Label)))
            .Where(x => x.Range is not null)
            .ToList();
        var sameEverywhere = perFixture.Count == selected.Count && perFixture.All(x => x.Range!.Min == choice.Min && x.Range.Max == choice.Max);
        if (_selectionTarget is { } selection && sameEverywhere)
        {
            Set(new SceneValue { Target = selection, Attribute = tool.Attribute, Range = new RangeValue(choice.Min, choice.Max) });
        }
        else
        {
            foreach (var (fixture, channel, range) in perFixture)
            {
                Set(new SceneValue { Target = ValueTarget.Fixture(fixture.Fixture.Id), Attribute = tool.Attribute, Range = new RangeValue(range!.Min, range.Max) });
            }
        }

        Push();
    }

    /// <summary>Règle la couleur logique de la sélection.</summary>
    internal void SetColor(LogicalColor color)
    {
        SetForTargets(target => new SceneValue { Target = target, Color = color });
        Push();
    }

    /// <summary>Applique une palette à la sélection (la scène la référencera, SCN-008).</summary>
    public void ApplyPalette(Palette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        SetForTargets(target => new SceneValue { Target = target, PaletteId = palette.Id });
        Push();
    }

    /// <summary>Applique une palette automatique (PAL-003) aux appareils sélectionnés dont le modèle la connaît.</summary>
    public void ApplyAutoPalette(AutoPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        foreach (var value in AutoPalettes.Apply(palette, SelectedFixtures))
        {
            Set(value);
        }

        Push();
    }

    /// <summary>
    /// Applique le fondu propre et le retard réparti actuels aux réglages déjà faits sur la sélection (SCN-010, SCN-011).
    /// </summary>
    [RelayCommand]
    private void ApplyTiming()
    {
        var keys = TargetKeys();
        var mine = _values.Where(v => keys.Contains(ProgrammerRules.TargetKey(v.Target))).ToList();
        var fixtures = SelectedFixtures.Select(f => ProgrammerRules.TargetKey(ValueTarget.Fixture(f.Fixture.Id))).ToList();
        _values = [.. _values.Select(v => mine.Contains(v) ? WithTiming(v, fixtures.IndexOf(ProgrammerRules.TargetKey(v.Target)), fixtures.Count) : v)];
        IsModified = true;
        Push();
    }

    /// <summary>« Retirer » un groupe d'attributs de la sélection (SCN-032).</summary>
    internal void RemoveFamily(string family)
    {
        var keys = TargetKeys();
        _values = ProgrammerRules.Remove(_values, keys, family, PaletteLookup(_runtime.Project.Palettes));
        IsModified = true;
        Push();
    }

    /// <summary>
    /// « Capturer la sortie » (SCN-037, CONS-025) : ce que le moteur émet actuellement pour les appareils sélectionnés
    /// (tous si aucun), hors valeurs par défaut, entre dans le programmeur canal par canal.
    /// </summary>
    [RelayCommand]
    private void CaptureOutput()
    {
        var snapshot = _runtime.Engine.Snapshot;
        var wanted = SelectedFixtures.Select(f => f.ReferenceId).ToHashSet();
        var show = snapshot.Show;
        for (var p = 0; p < show.Parameters.Count && p < snapshot.Values.Length; p++)
        {
            var parameter = show.Parameters[p];
            if ((wanted.Count > 0 && !wanted.Contains(parameter.FixtureId)) || snapshot.Sources[p].Kind == SourceKind.Default)
            {
                continue;
            }

            Set(new SceneValue { Target = ValueTarget.Fixture(parameter.FixtureId), Channel = parameter.ChannelKey, Level = snapshot.Values[p] });
        }

        Push();
    }

    /// <summary>Copie les réglages du premier appareil sélectionné (SCN-038).</summary>
    [RelayCommand]
    private void Copy()
    {
        var selected = SelectedFixtures;
        if (selected.Count == 0)
        {
            return;
        }

        var first = selected[0];
        var key = ProgrammerRules.TargetKey(ValueTarget.Fixture(first.Fixture.Id));
        _clipboard = [.. _values.Where(v => ProgrammerRules.TargetKey(v.Target) == key)];
        Content = string.Create(CultureInfo.CurrentCulture, $"{_clipboard.Count} réglage(s) de « {first.Fixture.Name} » copiés");
    }

    /// <summary>Colle les réglages copiés sur chaque appareil sélectionné (SCN-038).</summary>
    [RelayCommand]
    private void Paste() => PasteCore(mirror: false);

    /// <summary>Colle en miroir : Pan inversé, pour des lyres symétriques (SCN-038).</summary>
    [RelayCommand]
    private void PasteMirror() => PasteCore(mirror: true);

    /// <summary>Met à jour l'affichage des outils depuis le moteur qui reçoit les réglages.</summary>
    public void Refresh()
    {
        var selected = SelectedFixtures;
        if (selected.Count == 0)
        {
            return;
        }

        var first = selected[0];
        var snapshot = TargetEngine.Snapshot;
        var palettes = PaletteLookup(_runtime.Project.Palettes);
        var keys = TargetKeys();
        var mine = _values.Where(v => keys.Contains(ProgrammerRules.TargetKey(v.Target))).ToList();
        IsRefreshing = true;
        try
        {
            foreach (var tool in IntensityTools.Concat(PositionTools).Concat(OtherTools))
            {
                var key = ValueResolver.Keys(first, 0, tool.Attribute).FirstOrDefault();
                var level = key is null ? 0 : Level(snapshot, first.ReferenceId, key);
                var modified = mine.Any(v => ProgrammerRules.Family(ProgrammerRules.Slot(v, palettes), v.Attribute) == tool.Family);
                tool.Show(level, modified, Describe(first, key, level));
            }

            if (HasColor)
            {
                var programmed = mine.LastOrDefault(v => v.Color is not null)?.Color;
                var shown = programmed ?? new LogicalColor
                {
                    R = LevelOf(snapshot, first, AttributeKind.Red),
                    G = LevelOf(snapshot, first, AttributeKind.Green),
                    B = LevelOf(snapshot, first, AttributeKind.Blue),
                };
                var colorModified = mine.Any(v => ProgrammerRules.Slot(v, palettes) == ProgrammerRules.ColorSlot);
                Color.Show(shown, colorModified);
            }
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>Libère les surcharges envoyées (fermeture du projet, sortie du programmeur).</summary>
    public void ReleasePushed()
    {
        if (_pushedTo is null)
        {
            return;
        }

        foreach (var fixture in _pushed.Keys.Select(k => k.Fixture).Distinct())
        {
            _pushedTo.Send(new ReleaseAttributesCommand(CommandOrigin.User, fixture, [.. _pushed.Keys.Where(k => k.Fixture == fixture).Select(k => k.Key)]));
        }

        _pushed = [];
    }

    private void PasteCore(bool mirror)
    {
        foreach (var fixture in SelectedFixtures)
        {
            foreach (var value in _clipboard)
            {
                var copy = value with { Target = ValueTarget.Fixture(fixture.Fixture.Id, value.Target.Cell) };
                Set(mirror ? ProgrammerRules.Mirror(copy) : copy);
            }
        }

        Push();
    }

    private void Set(SceneValue value)
    {
        _values = ProgrammerRules.Set(_values, value, PaletteLookup(_runtime.Project.Palettes));
        IsModified = true;
    }

    /// <summary>Crée une valeur par cible, avec le fondu propre et le retard réparti demandés.</summary>
    private void SetForTargets(Func<ValueTarget, SceneValue> make)
    {
        var targets = Targets();
        for (var i = 0; i < targets.Count; i++)
        {
            Set(WithTiming(make(targets[i]), i, targets.Count));
        }
    }

    /// <summary>
    /// Fondu propre (SCN-011) et « fan » (SCN-010) : sur une sélection enregistrée, la répartition est faite par le
    /// moteur dans l'ordre de la sélection ; sur des appareils cochés un à un, chacun reçoit sa part de retard, dans
    /// l'ordre de la liste.
    /// </summary>
    private SceneValue WithTiming(SceneValue value, int index, int count)
    {
        var fade = ValueFade is { } f ? Engine.Model.Duration.FromSeconds((double)Math.Max(0, f)) : (Engine.Model.Duration?)null;
        if (ValueSpread is not { } spread || spread <= 0)
        {
            return value with { Fade = fade ?? value.Fade };
        }

        if (value.Target.FixtureId is null)
        {
            return value with { Fade = fade ?? value.Fade, Spread = Engine.Model.Duration.FromSeconds((double)spread) };
        }

        var share = count > 1 && index >= 0 ? (double)spread * index / (count - 1) : 0;
        return value with { Fade = fade ?? value.Fade, Delay = Engine.Model.Duration.FromSeconds(share) };
    }

    /// <summary>Cibles des réglages : la sélection choisie, sinon chaque appareil coché.</summary>
    private List<ValueTarget> Targets() =>
        _selectionTarget is { } selection ? [selection] : [.. SelectedFixtures.Select(f => ValueTarget.Fixture(f.Fixture.Id))];

    private HashSet<string> TargetKeys()
    {
        var keys = SelectedFixtures.Select(f => ProgrammerRules.TargetKey(ValueTarget.Fixture(f.Fixture.Id))).ToHashSet();
        if (_selectionTarget is { } selection)
        {
            keys.Add(ProgrammerRules.TargetKey(selection));
        }

        return keys;
    }

    /// <summary>Envoie le contenu résolu au moteur visé ; les attributs qui n'y sont plus sont libérés.</summary>
    private void Push()
    {
        var engine = TargetEngine;
        var patch = _runtime.Show.Patch;
        var resolver = new ValueResolver(patch, _runtime.Project.Palettes);
        var next = new Dictionary<(Guid, string), double>();

        // Même ordre que la compilation : sélections, puis appareils, puis cellules ; à égalité, la dernière valeur.
        foreach (var value in _values.Select((v, i) => (v, i)).OrderBy(x => x.v.Target.FixtureId is null ? 0 : x.v.Target.Cell == 0 ? 1 : 2).ThenBy(x => x.i).Select(x => x.v))
        {
            foreach (var resolved in resolver.Resolve(value, out _))
            {
                next[(resolved.FixtureId, resolved.ChannelKey)] = resolved.Level;
            }
        }

        if (_pushedTo is not null && _pushedTo != engine)
        {
            ReleasePushed();
        }

        var released = _pushed.Keys.Where(k => !next.ContainsKey(k)).ToList();
        foreach (var fixture in released.Select(k => k.Fixture).Distinct())
        {
            engine.Send(new ReleaseAttributesCommand(CommandOrigin.User, fixture, [.. released.Where(k => k.Fixture == fixture).Select(k => k.Key)]));
        }

        if (next.Count > 0)
        {
            engine.Send(new OverrideAttributesCommand(CommandOrigin.User, [.. next.Select(kv => new AttributeValue(kv.Key.Item1, kv.Key.Item2, kv.Value))]));
        }

        _pushed = next;
        _pushedTo = engine;
        foreach (var fixture in Fixtures)
        {
            fixture.HasValues = next.Keys.Any(k => k.Item1 == fixture.Info.ReferenceId);
        }

        Content = _values.Count == 0
            ? "Programmeur vide"
            : string.Create(CultureInfo.CurrentCulture, $"{_values.Count} réglage(s) dans le programmeur{(_blind ? " (aveugle : aperçu seulement)" : string.Empty)}");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void RebuildTools()
    {
        var selected = SelectedFixtures;
        HasSelection = selected.Count > 0;
        SelectionSummary = selected.Count == 0
            ? "Aucun appareil sélectionné"
            : string.Create(CultureInfo.CurrentCulture, $"{selected.Count} appareil(s){(_selectionTarget is not null ? " – sélection enregistrée" : string.Empty)} : {string.Join(", ", selected.Take(6).Select(f => f.Fixture.Name))}{(selected.Count > 6 ? "…" : string.Empty)}");

        IntensityTools.Clear();
        PositionTools.Clear();
        OtherTools.Clear();
        if (selected.Count > 0 && selected.Any(f => ValueResolver.Keys(f, 0, AttributeKind.Intensity).Any()))
        {
            IntensityTools.Add(new LevelToolViewModel(this, "Intensité", AttributeKind.Intensity, ProgrammerRules.IntensitySlot, []));
        }

        var attributes = selected.SelectMany(f => f.Channels).Select(c => c.Attribute).Distinct().ToList();
        HasColor = attributes.Any(a => ColorAttributes.Contains(a) || a is AttributeKind.ColorWheel or AttributeKind.ColorMacro);
        Color.HasWhite = attributes.Contains(AttributeKind.White) || attributes.Contains(AttributeKind.WarmWhite);
        Color.HasAmber = attributes.Contains(AttributeKind.Amber);
        Color.HasUv = attributes.Contains(AttributeKind.Uv);

        foreach (var attribute in new[] { AttributeKind.Pan, AttributeKind.Tilt }.Where(attributes.Contains))
        {
            PositionTools.Add(new LevelToolViewModel(this, AttributeCatalog.Label(attribute), attribute, ProgrammerRules.PositionSlot, []));
        }

        foreach (var attribute in attributes.Where(a => !AttributeCatalog.IsIntensity(a) && !ColorAttributes.Contains(a)
            && a is not (AttributeKind.Pan or AttributeKind.Tilt or AttributeKind.NoFunction)))
        {
            var channel = selected.SelectMany(f => f.Channels).First(c => c.Attribute == attribute);
            var ranges = channel.Capabilities
                .OrderBy(c => c.Min)
                .Select(c => new RangeChoice(c.Label, c.Min, c.Max, c.Colors.Count > 0 ? c.Colors[0] : "#30363D"))
                .ToList();
            OtherTools.Add(new LevelToolViewModel(this, AttributeCatalog.Label(attribute), attribute, $"attr:{attribute}", ranges));
        }

        Changed?.Invoke(this, EventArgs.Empty);
        Refresh();
    }

    private static double Level(EngineSnapshot snapshot, Guid fixtureId, string key)
    {
        var index = snapshot.Show.IndexOf(fixtureId, key);
        return index >= 0 && index < snapshot.Values.Length ? snapshot.Values[index] : 0;
    }

    private static double LevelOf(EngineSnapshot snapshot, FixtureInfo fixture, AttributeKind attribute) =>
        ValueResolver.Keys(fixture, 0, attribute).FirstOrDefault() is { } key ? Level(snapshot, fixture.ReferenceId, key) : 0;

    /// <summary>Valeur dans l'unité la plus parlante (GEN-021) : %, degrés, nom de plage.</summary>
    private static string Describe(FixtureInfo fixture, string? key, double level)
    {
        if (key is null)
        {
            return "—";
        }

        return fixture.Channels.FirstOrDefault(c => c.Key == key) is { } channel
            ? DmxConversion.Describe(channel, DmxConversion.To8Bit(level), fixture.Type.Physical)
            : string.Create(CultureInfo.CurrentCulture, $"{Math.Round(level * 100)} %");
    }

    private static Func<Guid, Palette?> PaletteLookup(PaletteSet palettes)
    {
        var map = palettes.Palettes.ToDictionary(p => p.Id);
        return id => map.GetValueOrDefault(id);
    }

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
