using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Fixtures.Model;
using Luxia.Fixtures.Rules;
using Luxia.Hosting;
using Luxia.Patch.Model;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>
/// Panneau « Réglages des appareils » (doc 60 §2, §5) : les réglages de la sélection du plan, par famille
/// (Intensité, Couleur, Position, Faisceau et autres), avec les vrais composants (sélecteur de couleur, grille Pan /
/// Tilt) et les palettes. Ce qu'un réglage devient dépend du mode (<see cref="ControlSession"/>). Les valeurs
/// demandées sont gardées à l'écran une demi-seconde avant de relire le moteur (course écran / moteur, doc 03 §11).
/// </summary>
public sealed partial class SettingsPanelViewModel : ViewModelBase
{
    private const int HoldRefreshes = 10;

    private static readonly AttributeKind[] Emitters =
    [
        AttributeKind.Red, AttributeKind.Green, AttributeKind.Blue, AttributeKind.White, AttributeKind.WarmWhite,
        AttributeKind.Amber, AttributeKind.Uv, AttributeKind.Cyan, AttributeKind.Magenta, AttributeKind.Yellow, AttributeKind.Lime,
    ];

    private readonly LuxiaRuntime _runtime;
    private readonly ControlSession _session;
    private readonly IDialogService _dialogs;
    private string _signature = string.Empty;
    private int _holdIntensity;
    private int _holdColor;
    private int _holdPosition;
    private readonly Dictionary<AttributeKind, int> _holdOthers = [];
    private bool _syncing;

    [ObservableProperty]
    private bool _hasSelection;

    [ObservableProperty]
    private string _selectionText = string.Empty;

    [ObservableProperty]
    private string _targetText = string.Empty;

    [ObservableProperty]
    private string _targetColor = ControlColors.Live;

    [ObservableProperty]
    private string _actionText = "Libérer la sélection";

    [ObservableProperty]
    private string _actionTip = string.Empty;

    [ObservableProperty]
    private bool _hasIntensity;

    [ObservableProperty]
    private bool _hasColor;

    [ObservableProperty]
    private bool _hasPosition;

    [ObservableProperty]
    private bool _hasOthers;

    [ObservableProperty]
    private int _selectedTab;

    [ObservableProperty]
    private double _intensity;

    [ObservableProperty]
    private string _intensityText = "—";

    [ObservableProperty]
    private string _intensityStateColor = "#00000000";

    /// <summary>Vrai quand la sélection a une intensité réglable à 0 % : une couleur choisie ne se verrait pas.</summary>
    [ObservableProperty]
    private bool _isDark;

    [ObservableProperty]
    private LightColor _color = LightColor.White;

    [ObservableProperty]
    private IReadOnlyList<LightColor> _colorFavorites = [];

    [ObservableProperty]
    private string _colorStateColor = "#00000000";

    [ObservableProperty]
    private IReadOnlyList<PanTiltMarker> _markers = [];

    [ObservableProperty]
    private IReadOnlyList<PanTiltZoneMarker> _zoneMarkers = [];

    [ObservableProperty]
    private string? _selectedZoneId;

    [ObservableProperty]
    private bool _isZoneEditing;

    [ObservableProperty]
    private bool _newZoneAllowed;

    [ObservableProperty]
    private double _panRange = 540;

    [ObservableProperty]
    private double _tiltRange = 270;

    [ObservableProperty]
    private string _positionText = string.Empty;

    [ObservableProperty]
    private string _positionStateColor = "#00000000";

    [ObservableProperty]
    private string _zonesTitle = string.Empty;

    /// <summary>Nom de la zone choisie (modifiable en édition des zones).</summary>
    [ObservableProperty]
    private string _selectedZoneName = string.Empty;

    private bool _syncingZoneName;

    [ObservableProperty]
    private string? _message;

    /// <summary>Crée le panneau.</summary>
    public SettingsPanelViewModel(LuxiaRuntime runtime, ControlSession session, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(dialogs);
        _runtime = runtime;
        _session = session;
        _dialogs = dialogs;
        runtime.Show.Compiled += (_, _) => _signature = string.Empty;
        runtime.Project.Changed += (_, _) => IsZoneEditing = false;
        session.Changed += (_, _) =>
        {
            UpdateHeader();
            OnPropertyChanged(nameof(CanEditZones));
        };
        UpdateHeader();
    }

    /// <summary>Levé quand l'édition des zones commence ou finit (le bandeau passe en « ZONES », C4).</summary>
    public event EventHandler? ZoneEditingChanged;

    /// <summary>Les zones sont modifiables (pas de verrou soirée).</summary>
    public bool CanEditZones => !_session.IsLocked;

    /// <summary>Palettes d'intensité du projet.</summary>
    public ObservableCollection<Palette> IntensityPalettes { get; } = [];

    /// <summary>Palettes de couleur du projet.</summary>
    public ObservableCollection<Palette> ColorPalettes { get; } = [];

    /// <summary>Palettes de position du projet.</summary>
    public ObservableCollection<Palette> PositionPalettes { get; } = [];

    /// <summary>Émetteurs de couleur de la sélection (pastilles et valeurs).</summary>
    public ObservableCollection<ParameterRowViewModel> ColorRows { get; } = [];

    /// <summary>Faisceau, strobe, programmes… (tout sauf intensité, couleur et position).</summary>
    public ObservableCollection<ParameterRowViewModel> OtherRows { get; } = [];

    /// <summary>Zones de l'appareil (liste à côté de la grille : un clic choisit la zone en édition).</summary>
    public ObservableCollection<ZoneLine> ZoneLines { get; } = [];

    /// <summary>Vrai quand une zone est choisie en édition des zones.</summary>
    public bool HasSelectedZone => IsZoneEditing && SelectedZoneIndex() is not null;

    /// <summary>Premier appareil sélectionné qui a Pan et Tilt (zones), ou nul.</summary>
    public FixtureInfo? ZoneFixture { get; private set; }

    /// <summary>Relit les valeurs affichées (20 fois par seconde, écran Contrôle affiché).</summary>
    public void Refresh()
    {
        var infos = SelectedInfos();
        var signature = string.Join(",", infos.Select(i => i.Fixture.Id)) + "|" + _runtime.Project.Palettes.Palettes.Count;
        if (signature != _signature)
        {
            _signature = signature;
            Rebuild(infos);
        }

        if (infos.Count == 0)
        {
            return;
        }

        var first = infos[0];
        _syncing = true;
        try
        {
            RefreshIntensity(infos);
            RefreshColor(first);
            RefreshPosition();
            RefreshOthers(first);
        }
        finally
        {
            _syncing = false;
        }
    }

    /// <summary>Couleur demandée par le sélecteur.</summary>
    public void RequestColor(LightColor color)
    {
        Color = color;
        _holdColor = HoldRefreshes;
        var (r, g, b) = color.ToRgb();
        _session.Apply(t => new SceneValue { Target = t, Color = new LogicalColor { R = r / 255.0, G = g / 255.0, B = b / 255.0 } }, "Couleur");
    }

    /// <summary>« + » du sélecteur : nouvelle palette de couleur avec la couleur courante.</summary>
    public async Task AddColorPaletteAsync()
    {
        if (_runtime.Project.Folder is null)
        {
            return;
        }

        var name = await _dialogs.AskTextAsync("Nouvelle palette de couleur", "Nom de la palette :").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var (r, g, b) = Color.ToRgb();
        var light = new LogicalColor { R = r / 255.0, G = g / 255.0, B = b / 255.0 };
        var palettes = _runtime.Project.Palettes;
        _runtime.Project.SavePalettes(palettes with { Palettes = [.. palettes.Palettes, new Palette { Name = name.Trim(), Kind = PaletteKind.Color, Light = light, Color = light.Hex }] });
        _signature = string.Empty;
    }

    /// <summary>Nouvelles visées demandées par la grille (une valeur Pan et une Tilt par appareil).</summary>
    public void RequestAim(IReadOnlyList<PanTiltTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        _holdPosition = HoldRefreshes;
        Markers = [.. Markers.Select(m => targets.FirstOrDefault(t => t.Id == m.Id) is { } t ? m with { Pan = t.Pan, Tilt = t.Tilt } : m)];
        var values = new List<SceneValue>();
        foreach (var target in targets)
        {
            if (Guid.TryParse(target.Id, out var id))
            {
                values.Add(new SceneValue { Target = ValueTarget.Fixture(id), Attribute = AttributeKind.Pan, Level = target.Pan });
                values.Add(new SceneValue { Target = ValueTarget.Fixture(id), Attribute = AttributeKind.Tilt, Level = target.Tilt });
            }
        }

        _session.ApplyValues(values, "Position");
    }

    /// <summary>Zone dessinée, déplacée ou redimensionnée sur la grille (INST-053, F7).</summary>
    public void RequestZone(PanTiltZoneRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (ZoneFixture is not { } fixture)
        {
            return;
        }

        var zones = Zones.Of(_session.Venues, fixture.Fixture.Id).ToList();
        var area = request.Area;
        if (request.Id is null)
        {
            var count = zones.Count(z => z.Allowed == NewZoneAllowed) + 1;
            zones.Add(new ForbiddenZone
            {
                FixtureId = fixture.Fixture.Id,
                Name = NewZoneAllowed ? "Limites" : string.Create(CultureInfo.CurrentCulture, $"Zone {count}"),
                PanMin = area.PanMin,
                PanMax = area.PanMax,
                TiltMin = area.TiltMin,
                TiltMax = area.TiltMax,
                Allowed = NewZoneAllowed,
            });
            SelectedZoneId = (zones.Count - 1).ToString(CultureInfo.InvariantCulture);
        }
        else if (int.TryParse(request.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) && index < zones.Count)
        {
            zones[index] = zones[index] with { PanMin = area.PanMin, PanMax = area.PanMax, TiltMin = area.TiltMin, TiltMax = area.TiltMax };
        }

        _session.EditVenues(v => Zones.Replace(v, fixture.Fixture.Id, zones), request.Id is null ? "Nouvelle zone" : "Zone modifiée");
        RefreshZones();
    }

    /// <summary>Zone choisie sur la grille ou dans la liste.</summary>
    [RelayCommand]
    public void SelectZone(string? id)
    {
        SelectedZoneId = id;
        RefreshZones();
    }

    /// <summary>Bouton « Supprimer » de la zone choisie.</summary>
    [RelayCommand]
    private void DeleteSelectedZone()
    {
        if (SelectedZoneId is { } id)
        {
            DeleteZone(id);
        }
    }

    partial void OnSelectedZoneNameChanged(string value)
    {
        if (_syncingZoneName || ZoneFixture is not { } fixture || SelectedZoneIndex() is not { } index || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var zones = Zones.Of(_session.Venues, fixture.Fixture.Id).ToList();
        if (zones[index].Name == value.Trim())
        {
            return;
        }

        zones[index] = zones[index] with { Name = value.Trim() };
        _session.EditVenues(v => Zones.Replace(v, fixture.Fixture.Id, zones), "Renommer la zone");
        _session.Commit();
        RefreshZones();
    }

    private int? SelectedZoneIndex()
    {
        if (ZoneFixture is not { } fixture || !int.TryParse(SelectedZoneId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
        {
            return null;
        }

        return index >= 0 && index < Zones.Of(_session.Venues, fixture.Fixture.Id).Count ? index : null;
    }

    /// <summary>Retire la zone choisie (Suppr).</summary>
    public void DeleteZone(string id)
    {
        if (ZoneFixture is not { } fixture || !int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
        {
            return;
        }

        var zones = Zones.Of(_session.Venues, fixture.Fixture.Id).ToList();
        if (index < zones.Count)
        {
            zones.RemoveAt(index);
            _session.EditVenues(v => Zones.Replace(v, fixture.Fixture.Id, zones), "Supprimer la zone");
            _session.Commit();
            SelectedZoneId = null;
            RefreshZones();
        }
    }

    /// <summary>Applique une palette à la sélection (la scène la référencera, SCN-008).</summary>
    [RelayCommand]
    private void ApplyPalette(Palette? palette)
    {
        if (palette is null)
        {
            return;
        }

        _session.Apply(t => new SceneValue { Target = t, PaletteId = palette.Id }, $"Palette « {palette.Name} »");
        _session.Commit();
        _holdIntensity = _holdColor = _holdPosition = 0;
    }

    /// <summary>Libérer la sélection (LIVE) ou la retirer de l'étape (ÉDITION / AVEUGLE).</summary>
    [RelayCommand]
    private void RemoveSelection()
    {
        _session.Remove(null, "Retirer de l'étape");
        _session.Commit();
    }

    /// <summary>Retire une famille (paramètre : intensité, couleur, position) de la sélection.</summary>
    [RelayCommand]
    private void RemoveFamily(string? family)
    {
        var slot = family switch
        {
            "intensite" => ProgrammerRules.IntensitySlot,
            "couleur" => ProgrammerRules.ColorSlot,
            "position" => ProgrammerRules.PositionSlot,
            _ => null,
        };
        if (slot is not null)
        {
            _session.Remove(slot, "Retirer " + family);
            _session.Commit();
        }
    }

    /// <summary>Clic sur une plage nommée d'un paramètre (sur chaque appareil sélectionné).</summary>
    [RelayCommand]
    private void ApplyRange(RangeChoice? range)
    {
        if (range is null)
        {
            return;
        }

        _holdOthers[range.Attribute] = HoldRefreshes;
        _session.Apply(t => new SceneValue { Target = t, Attribute = range.Attribute, Range = new RangeValue(range.Min, range.Max) }, AttributeCatalog.Label(range.Attribute));
        _session.Commit();
    }

    partial void OnIntensityChanged(double value)
    {
        if (_syncing)
        {
            return;
        }

        _holdIntensity = HoldRefreshes;
        IntensityText = string.Create(CultureInfo.CurrentCulture, $"{Math.Round(value)} %");
        _session.Apply(t => new SceneValue { Target = t, Attribute = AttributeKind.Intensity, Level = Math.Clamp(value / 100, 0, 1) }, "Intensité");
    }

    partial void OnIsZoneEditingChanged(bool value)
    {
        _session.Commit();
        UpdateHeader();
        RefreshZones();
        ZoneEditingChanged?.Invoke(this, EventArgs.Empty);
    }

    private List<FixtureInfo> SelectedInfos()
    {
        var patch = _runtime.Show.Patch;
        return [.. _session.Selection.Select(patch.Find).OfType<FixtureInfo>()];
    }

    private void UpdateHeader()
    {
        var names = SelectedInfos().Select(i => i.Fixture.Name).ToList();
        HasSelection = names.Count > 0;
        SelectionText = names.Count == 0
            ? "Aucun appareil sélectionné : choisissez-en sur le plan."
            : string.Join(", ", names.Take(6)) + (names.Count > 6 ? string.Create(CultureInfo.CurrentCulture, $"… ({names.Count})") : string.Empty);
        (TargetText, TargetColor, ActionText, ActionTip) = _session.Mode switch
        {
            _ when IsZoneEditing =>
                ("zones du lieu : valables pour toutes les scènes", ControlColors.Zones, ActionText, ActionTip),
            EditMode.Edit or EditMode.Blind when _session.EditScene is { } scene =>
                ($"écrit dans « {scene.Name} » › étape {_session.EditStep + 1}", ControlColors.Of(_session.Mode), "Retirer de l'étape",
                 "Ces appareils ne seront plus réglés par cette étape (ils gardent ce que donnent les autres couches)."),
            _ => ("surcharge LIVE, non enregistrée", ControlColors.Live, "Libérer la sélection",
                  "Rend la main aux scènes pour les appareils sélectionnés."),
        };
    }

    private void Rebuild(IReadOnlyList<FixtureInfo> infos)
    {
        UpdateHeader();
        var attributes = infos.SelectMany(f => f.Channels).Select(c => c.Attribute).Distinct().ToList();
        HasIntensity = infos.Any(f => ValueResolver.Keys(f, 0, AttributeKind.Intensity).Any());
        HasColor = attributes.Any(a => Emitters.Contains(a));
        HasPosition = attributes.Contains(AttributeKind.Pan) && attributes.Contains(AttributeKind.Tilt);

        var palettes = _runtime.Project.Palettes.Palettes;
        Fill(IntensityPalettes, palettes.Where(p => p.Kind == PaletteKind.Intensity));
        Fill(ColorPalettes, palettes.Where(p => p.Kind == PaletteKind.Color));
        Fill(PositionPalettes, palettes.Where(p => p.Kind == PaletteKind.Position));
        ColorFavorites = [.. ColorPalettes.Select(p => ToLight(p.DisplayColor()))];

        ColorRows.Clear();
        foreach (var attribute in Emitters.Where(attributes.Contains))
        {
            ColorRows.Add(new ParameterRowViewModel(attribute, [], (_, _) => { }));
        }

        OtherRows.Clear();
        foreach (var attribute in attributes.Where(a => !AttributeCatalog.IsIntensity(a) && !Emitters.Contains(a)
            && a is not (AttributeKind.Pan or AttributeKind.Tilt or AttributeKind.NoFunction)))
        {
            var channel = infos.SelectMany(f => f.Channels).First(c => c.Attribute == attribute);
            var ranges = channel.Capabilities
                .OrderBy(c => c.Min)
                .Select(c => new RangeChoice(attribute, c.Label, c.Min, c.Max, c.Colors.Count > 0 ? c.Colors[0] : "#30363D"))
                .ToList();
            OtherRows.Add(new ParameterRowViewModel(attribute, ranges, OnOtherLevelChanged));
        }

        HasOthers = OtherRows.Count > 0;
        ZoneFixture = infos.FirstOrDefault(f => f.Channels.Any(c => c.Attribute == AttributeKind.Pan) && f.Channels.Any(c => c.Attribute == AttributeKind.Tilt));
        PanRange = ZoneFixture?.Type.Physical.PanRange ?? 540;
        TiltRange = ZoneFixture?.Type.Physical.TiltRange ?? 270;
        if (ZoneFixture is null && IsZoneEditing)
        {
            IsZoneEditing = false;
        }

        // Onglet affiché : le premier qui a un sens pour la sélection, si l'actuel n'en a plus.
        bool[] available = [HasIntensity, HasColor, HasPosition, HasOthers];
        if (SelectedTab < 0 || SelectedTab >= available.Length || !available[SelectedTab])
        {
            SelectedTab = Math.Max(Array.IndexOf(available, true), 0);
        }

        _holdIntensity = _holdColor = _holdPosition = 0;
        _holdOthers.Clear();
        RefreshZones();
    }

    private void OnOtherLevelChanged(ParameterRowViewModel row, double percent)
    {
        if (_syncing)
        {
            return;
        }

        _holdOthers[row.Attribute] = HoldRefreshes;
        _session.Apply(t => new SceneValue { Target = t, Attribute = row.Attribute, Level = Math.Clamp(percent / 100, 0, 1) }, row.Name);
    }

    private void RefreshIntensity(IReadOnlyList<FixtureInfo> infos)
    {
        var first = infos.FirstOrDefault(f => ValueResolver.Keys(f, 0, AttributeKind.Intensity).Any());
        if (first is null)
        {
            return;
        }

        var state = _session.StateOf(first, AttributeKind.Intensity);
        IntensityStateColor = ControlColors.Of(state);
        IsDark = (_session.LevelOf(first, AttributeKind.Intensity) ?? 0) <= 0;
        if (_holdIntensity > 0)
        {
            _holdIntensity--;
            return;
        }

        var level = (_session.LevelOf(first, AttributeKind.Intensity) ?? 0) * 100;
        Intensity = Math.Round(level);
        IntensityText = string.Create(CultureInfo.CurrentCulture, $"{Math.Round(level)} %");
    }

    private void RefreshColor(FixtureInfo first)
    {
        var state = ParameterState.Unused;
        foreach (var row in ColorRows)
        {
            var level = _session.LevelOf(first, row.Attribute);
            var rowState = level is null ? ParameterState.Unused : _session.StateOf(first, row.Attribute);
            row.Show(Math.Round((level ?? 0) * 100), level is null ? "—" : string.Create(CultureInfo.CurrentCulture, $"{Math.Round(level.Value * 100)} %"), rowState);
            state = rowState > state ? rowState : state;
        }

        ColorStateColor = ControlColors.Of(state);
        if (_holdColor > 0)
        {
            _holdColor--;
            return;
        }

        var r = _session.LevelOf(first, AttributeKind.Red) ?? 0;
        var g = _session.LevelOf(first, AttributeKind.Green) ?? 0;
        var b = _session.LevelOf(first, AttributeKind.Blue) ?? 0;
        var shown = LightColor.FromRgb(ToByte(r), ToByte(g), ToByte(b));

        // Noir (tout à 0) : on garde la teinte affichée, seule l'intensité tombe (sinon le repère saute en haut à gauche).
        Color = shown.Brightness <= 0 ? Color with { Brightness = 0 } : shown;
    }

    private void RefreshPosition()
    {
        var patch = _runtime.Show.Patch;
        var selected = _session.Selection.ToHashSet();
        var heads = patch.Fixtures.Where(f => !f.Absent && f.Channels.Any(c => c.Attribute == AttributeKind.Pan) && f.Channels.Any(c => c.Attribute == AttributeKind.Tilt)).ToList();
        if (heads.Count == 0)
        {
            return;
        }

        var firstSelected = heads.FirstOrDefault(h => selected.Contains(h.Fixture.Id));
        if (firstSelected is not null)
        {
            var state = _session.StateOf(firstSelected, AttributeKind.Pan);
            PositionStateColor = ControlColors.Of(state);
        }

        if (_holdPosition > 0)
        {
            _holdPosition--;
            return;
        }

        Markers = [.. heads.Select(h => new PanTiltMarker(
            h.Fixture.Id.ToString(),
            h.Fixture.Name,
            _session.LevelOf(h, AttributeKind.Pan) ?? 0.5,
            _session.LevelOf(h, AttributeKind.Tilt) ?? 0.5,
            selected.Contains(h.Fixture.Id) ? Avalonia.Media.Color.Parse("#FFB347") : Avalonia.Media.Colors.Gray,
            selected.Contains(h.Fixture.Id)))];
        if (firstSelected is not null)
        {
            var marker = Markers.First(m => m.Id == firstSelected.Fixture.Id.ToString());
            PositionText = string.Create(
                CultureInfo.CurrentCulture,
                $"{firstSelected.Fixture.Name} : Pan {PanTiltGeometry.ToDegrees(marker.Pan, PanRange):0}°, Tilt {PanTiltGeometry.ToDegrees(marker.Tilt, TiltRange):0}°");
        }
    }

    private void RefreshOthers(FixtureInfo first)
    {
        foreach (var row in OtherRows)
        {
            if (_holdOthers.TryGetValue(row.Attribute, out var hold) && hold > 0)
            {
                _holdOthers[row.Attribute] = hold - 1;
                continue;
            }

            var level = _session.LevelOf(first, row.Attribute);
            var channel = first.Channels.FirstOrDefault(c => c.Attribute == row.Attribute);
            var text = level is null || channel is null ? "—" : DmxConversion.Describe(channel, DmxConversion.To8Bit(level.Value), first.Type.Physical);
            row.Show(Math.Round((level ?? 0) * 100), text, level is null ? ParameterState.Unused : _session.StateOf(first, row.Attribute));
        }
    }

    private void RefreshZones()
    {
        ZoneLines.Clear();
        if (ZoneFixture is not { } fixture)
        {
            ZoneMarkers = [];
            ZonesTitle = "Choisissez une lyre sur le plan pour voir ses zones.";
            return;
        }

        var zones = Zones.Of(_session.Venues, fixture.Fixture.Id);
        ZoneMarkers = [.. zones.Select((z, i) => new PanTiltZoneMarker(
            i.ToString(CultureInfo.InvariantCulture),
            z.Name ?? (z.Allowed ? "Limites" : "Zone"),
            new PanTiltRect(z.PanMin, z.PanMax, z.TiltMin, z.TiltMax),
            z.Allowed ? PanTiltZoneKind.Allowed : PanTiltZoneKind.Forbidden))];
        ZonesTitle = $"Zones de « {fixture.Fixture.Name} » — lieu {_session.Venues.Active.Name}, valables pour toutes les scènes";
        for (var i = 0; i < zones.Count; i++)
        {
            var zone = zones[i];
            var id = i.ToString(CultureInfo.InvariantCulture);
            ZoneLines.Add(new ZoneLine(
                id,
                string.Create(
                    CultureInfo.CurrentCulture,
                    $"{(zone.Allowed ? "▢ permise" : "■ interdite")} · {zone.Name ?? "sans nom"} · Pan {zone.PanMin * PanRange:0}-{zone.PanMax * PanRange:0}°, Tilt {zone.TiltMin * TiltRange:0}-{zone.TiltMax * TiltRange:0}°"),
                IsZoneEditing && id == SelectedZoneId));
        }

        _syncingZoneName = true;
        SelectedZoneName = SelectedZoneIndex() is { } selected ? zones[selected].Name ?? string.Empty : string.Empty;
        _syncingZoneName = false;
        OnPropertyChanged(nameof(HasSelectedZone));
    }

    private static void Fill(ObservableCollection<Palette> target, IEnumerable<Palette> source)
    {
        target.Clear();
        foreach (var palette in source)
        {
            target.Add(palette);
        }
    }

    private static LightColor ToLight(string hex)
    {
        var color = Avalonia.Media.Color.TryParse(hex, out var c) ? c : Avalonia.Media.Colors.White;
        return LightColor.FromRgb(color.R, color.G, color.B);
    }

    private static byte ToByte(double level) => (byte)Math.Clamp((int)Math.Round(level * 255), 0, 255);
}

/// <summary>Une zone dans la liste à côté de la grille Pan / Tilt.</summary>
/// <param name="Id">Identifiant (rang de la zone pour l'appareil).</param>
/// <param name="Text">Ligne lisible : type, nom, étendue.</param>
/// <param name="IsSelected">Zone choisie en édition des zones.</param>
public sealed record ZoneLine(string Id, string Text, bool IsSelected)
{
    /// <summary>Fond de la ligne (choisie : couleur des zones, estompée).</summary>
    public string Background => IsSelected ? "#33F0883E" : "#00000000";
}
