using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Fixtures.Model;
using Luxia.Hosting;
using Luxia.Patch.Rules;
using Luxia.UI.Controls;
using Luxia.UI.Modules.Simulator;

namespace Luxia.UI.Modules.Control;

/// <summary>Une sélection rapide au-dessus du plan (automatique ou enregistrée).</summary>
/// <param name="Label">Libellé.</param>
/// <param name="Members">Appareils.</param>
/// <param name="Color">Couleur de la pastille.</param>
public sealed record QuickSelection(string Label, IReadOnlyList<Guid> Members, string Color);

/// <summary>
/// Panneau Plan des appareils (doc 60 §5, E5, SIM-010) : le lieu vu de dessus, avec les couleurs réellement émises
/// (ou l'aperçu en AVEUGLE) ; c'est <b>la</b> sélection de tous les panneaux : clic, Ctrl + clic, rectangle, sélections
/// rapides.
/// </summary>
public sealed partial class PlanPanelViewModel : ViewModelBase
{
    private readonly LuxiaRuntime _runtime;
    private readonly ControlSession _session;
    private readonly Dictionary<int, byte[]> _frames = [];

    [ObservableProperty]
    private IReadOnlyList<SimulatorFixtureVisual> _fixtures = [];

    [ObservableProperty]
    private IReadOnlyCollection<Guid> _selectedIds = [];

    [ObservableProperty]
    private double _roomWidthM = 12;

    [ObservableProperty]
    private double _roomDepthM = 8;

    [ObservableProperty]
    private string _source = string.Empty;

    [ObservableProperty]
    private bool _isPreview;

    [ObservableProperty]
    private string _summary = string.Empty;

    /// <summary>Crée le panneau.</summary>
    public PlanPanelViewModel(LuxiaRuntime runtime, ControlSession session)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(session);
        _runtime = runtime;
        _session = session;
        runtime.Project.Changed += (_, _) => RebuildQuick();
        runtime.Show.Compiled += (_, _) => RebuildQuick();
        session.Changed += (_, _) => OnSessionChanged();
        RebuildQuick();
        OnSessionChanged();
    }

    /// <summary>Sélections rapides.</summary>
    public ObservableCollection<QuickSelection> QuickSelections { get; } = [];

    /// <summary>Relit la trame du moteur affiché.</summary>
    public void Refresh()
    {
        var venue = _runtime.Project.Venues.Active;
        RoomWidthM = venue.WidthM;
        RoomDepthM = venue.DepthM;
        IsPreview = _session.Mode == EditMode.Blind;
        Source = IsPreview ? $"APERÇU 👁 — la sortie ne change pas · {venue.Name}" : $"Sortie · lieu {venue.Name}";
        Fixtures = SimulatorVisuals.Build(_runtime, _session.DisplayEngine, _frames);
    }

    /// <summary>Sélection faite à la souris sur le plan.</summary>
    public void OnSelectionRequested(FixtureSelectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var current = _session.Selection.ToList();
        if (!request.Additive)
        {
            _session.Select(request.Fixtures);
        }
        else if (request.Fixtures.Count == 1)
        {
            var id = request.Fixtures[0];
            _session.Select(current.Contains(id) ? current.Where(f => f != id) : [.. current, id]);
        }
        else
        {
            _session.Select([.. current, .. request.Fixtures]);
        }

        _runtime.TraceUi("Contrôle", $"sélection : {_session.Selection.Count} appareil(s)");
    }

    /// <summary>Sélection rapide.</summary>
    [RelayCommand]
    private void Quick(QuickSelection? selection)
    {
        if (selection is not null)
        {
            _session.Select(selection.Members);
        }
    }

    /// <summary>Plus rien de sélectionné.</summary>
    [RelayCommand]
    private void SelectNone() => _session.Select([]);

    /// <summary>Inverse la sélection parmi les appareils présents.</summary>
    [RelayCommand]
    private void Invert()
    {
        var current = _session.Selection.ToHashSet();
        _session.Select(Present().Where(id => !current.Contains(id)));
    }

    /// <summary>Garde un appareil sur deux (½), trois (⅓) ou quatre (¼) de la sélection, dans l'ordre (paramètre : 2, 3, 4).</summary>
    [RelayCommand]
    private void Every(string? step)
    {
        if (int.TryParse(step, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > 1)
        {
            _session.Select(_session.Selection.Where((_, i) => i % n == 0));
        }
    }

    private void OnSessionChanged()
    {
        SelectedIds = [.. _session.Selection];
        var names = _session.Selection
            .Select(id => _runtime.Project.Installation.Fixtures.FirstOrDefault(f => f.Id == id)?.Name)
            .OfType<string>()
            .ToList();
        Summary = names.Count == 0
            ? "Aucun appareil sélectionné · clic : un appareil · Ctrl + clic : ajouter / retirer · glisser : rectangle"
            : string.Create(CultureInfo.CurrentCulture, $"{names.Count} sélectionné(s) : {string.Join(", ", names.Take(8))}{(names.Count > 8 ? "…" : string.Empty)}");
    }

    private List<Guid> Present()
    {
        var venue = _runtime.Project.Venues.Active;
        return [.. _runtime.Project.Installation.Fixtures.Where(f => venue.PlacementOf(f.Id) is { Absent: false }).Select(f => f.Id)];
    }

    private void RebuildQuick()
    {
        QuickSelections.Clear();
        var present = Present();
        QuickSelections.Add(new QuickSelection("Tous", present, "#8B949E"));
        var library = _runtime.Project.FixtureLibrary;
        var fixtures = _runtime.Project.Installation.Fixtures.Where(f => present.Contains(f.Id)).ToList();
        foreach (var auto in AutoSelections.Build(fixtures, f => library?.Find(f.FixtureTypeId)).Where(a => a.Kind == AutoSelectionKind.ByCategory))
        {
            QuickSelections.Add(new QuickSelection(auto.Title(CategoryLabel), [.. auto.Items.Select(f => f.Id)], "#8B949E"));
        }

        foreach (var selection in _runtime.Project.Installation.Selections)
        {
            QuickSelections.Add(new QuickSelection(selection.Name, [.. selection.Items.Select(i => i.FixtureId)], selection.Color));
        }
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
