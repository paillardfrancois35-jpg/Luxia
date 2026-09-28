using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Media;
using Luxia.UI.Controls;

namespace Luxia.Tools.Prototype;

/// <summary>
/// État de démonstration partagé par tous les panneaux (un appareil visé dans « Position » l'est aussi dans une
/// fenêtre détachée, la couleur choisie colore les points de la grille…). Joue le rôle du modèle de vue : il reçoit
/// les demandes des composants, les retient et prévient les vues (<see cref="Changed"/>).
/// </summary>
internal sealed class DemoState
{
    private readonly List<DemoFixture> _fixtures =
    [
        new("lyre1", "Lyre 1", 0.50, 0.55, true),
        new("lyre2", "Lyre 2", 0.62, 0.55, true),
        new("lyre3", "Lyre 3", 0.30, 0.40, false),
        new("lyre4", "Lyre 4", 0.75, 0.35, false),
    ];

    private readonly List<PanTiltZoneMarker> _zones =
    [
        new("z1", "Public", new PanTiltRect(0.35, 0.65, 0.05, 0.25), PanTiltZoneKind.Forbidden),
    ];

    private readonly List<LightColor> _favorites =
    [
        new(0, 1, 1), new(30, 1, 1), new(55, 1, 1), new(120, 1, 1), new(200, 1, 1), new(240, 1, 1), new(285, 1, 1), new(30, 0.35, 1),
    ];

    private int _nextZone = 2;

    /// <summary>Instance de l'application (les vues sont reconstruites par Dock à volonté).</summary>
    public static DemoState Shared { get; } = new();

    /// <summary>Prévient les vues d'un changement.</summary>
    public event EventHandler? Changed;

    /// <summary>Couleur des appareils sélectionnés.</summary>
    public LightColor Color { get; private set; } = new(30, 1, 1);

    /// <summary>Favoris du sélecteur de couleur.</summary>
    public IReadOnlyList<LightColor> Favorites => _favorites;

    /// <summary>Appareils de démonstration.</summary>
    public IReadOnlyList<DemoFixture> Fixtures => _fixtures;

    /// <summary>Zones de la grille.</summary>
    public IReadOnlyList<PanTiltZoneMarker> Zones => _zones;

    /// <summary>Zone sélectionnée.</summary>
    public string? SelectedZoneId { get; private set; }

    /// <summary>La grille édite les zones plutôt que les visées.</summary>
    public bool IsZoneEditing { get; private set; }

    /// <summary>Genre des zones dessinées ensuite.</summary>
    public PanTiltZoneKind NewZoneKind { get; set; } = PanTiltZoneKind.Forbidden;

    /// <summary>Journal, le plus récent en tête.</summary>
    public ObservableCollection<string> Log { get; } = [];

    /// <summary>Nombre de demandes reçues des composants (mesure de fluidité).</summary>
    public long RequestCount { get; private set; }

    /// <summary>Points de la grille, à la couleur choisie pour les appareils sélectionnés.</summary>
    public IReadOnlyList<PanTiltMarker> Markers() =>
        _fixtures.Select(f => new PanTiltMarker(f.Id, f.Label, f.Pan, f.Tilt, f.IsSelected ? Color.ToColor() : Colors.Gray, f.IsSelected)).ToList();

    /// <summary>Sélectionne ou retire un appareil.</summary>
    public void SetSelected(string id, bool selected)
    {
        var fixture = _fixtures.First(f => f.Id == id);
        if (fixture.IsSelected != selected)
        {
            fixture.IsSelected = selected;
            Write($"Sélection : {fixture.Label} {(selected ? "ajoutée" : "retirée")}");
        }
    }

    /// <summary>Nouvelles visées demandées par la grille.</summary>
    public void Aim(IReadOnlyList<PanTiltTarget> targets)
    {
        RequestCount++;
        foreach (var target in targets)
        {
            var fixture = _fixtures.First(f => f.Id == target.Id);
            fixture.Pan = target.Pan;
            fixture.Tilt = target.Tilt;
        }

        if (targets.Count > 0)
        {
            var first = _fixtures.First(f => f.Id == targets[0].Id);
            Write(string.Create(
                CultureInfo.CurrentCulture,
                $"Position : {string.Join(", ", targets.Select(t => _fixtures.First(f => f.Id == t.Id).Label))} → Pan {PanTiltGeometry.ToDegrees(first.Pan, 540):0.0}°, Tilt {PanTiltGeometry.ToDegrees(first.Tilt, 270):0.0}°"),
                replaceSameKind: true);
        }
    }

    /// <summary>Passe la grille en édition des zones, ou revient à la visée.</summary>
    public void SetZoneEditing(bool editing)
    {
        IsZoneEditing = editing;
        Write(editing ? "Grille : édition des zones" : "Grille : visée");
    }

    /// <summary>Crée ou modifie une zone.</summary>
    public void ChangeZone(PanTiltZoneRequest request)
    {
        RequestCount++;
        if (request.Id is null)
        {
            var id = "z" + _nextZone++.ToString(CultureInfo.InvariantCulture);
            var name = NewZoneKind == PanTiltZoneKind.Allowed ? "Limites" : $"Zone {_nextZone - 1}";
            _zones.Add(new PanTiltZoneMarker(id, name, request.Area, NewZoneKind));
            SelectedZoneId = id;
            Write($"Zone créée : {name}");
            return;
        }

        var index = _zones.FindIndex(z => z.Id == request.Id);
        if (index >= 0)
        {
            _zones[index] = _zones[index] with { Area = request.Area };
            Write($"Zone modifiée : {_zones[index].Name}", replaceSameKind: true);
        }
    }

    /// <summary>Sélectionne une zone.</summary>
    public void SelectZone(string? id)
    {
        SelectedZoneId = id;
        Notify();
    }

    /// <summary>Retire une zone.</summary>
    public void DeleteZone(string id)
    {
        var zone = _zones.FirstOrDefault(z => z.Id == id);
        if (zone is not null)
        {
            _zones.Remove(zone);
            SelectedZoneId = null;
            Write($"Zone supprimée : {zone.Name}");
        }
    }

    /// <summary>Couleur demandée par le sélecteur.</summary>
    public void SetColor(LightColor color)
    {
        RequestCount++;
        Color = color;
        var (r, g, b) = color.ToRgb();
        Write(string.Create(CultureInfo.CurrentCulture, $"Couleur : R {r * 100 / 255} % · V {g * 100 / 255} % · B {b * 100 / 255} %"), replaceSameKind: true);
    }

    /// <summary>Ajoute la couleur courante aux favoris.</summary>
    public void AddFavorite()
    {
        _favorites.Add(Color);
        Write("Favori ajouté");
    }

    /// <summary>Retire un favori.</summary>
    public void RemoveFavorite(int index)
    {
        if (index >= 0 && index < _favorites.Count)
        {
            _favorites.RemoveAt(index);
            Write("Favori retiré");
        }
    }

    /// <summary>Ajoute une ligne au journal (et prévient les vues).</summary>
    /// <param name="line">Texte.</param>
    /// <param name="replaceSameKind">Pendant un glisser, remplace la ligne précédente du même genre au lieu d'en empiler cent.</param>
    public void Write(string line, bool replaceSameKind = false)
    {
        var stamped = DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentCulture) + "  " + line;
        var kind = line.Split(':')[0];
        if (replaceSameKind && Log.Count > 0 && Log[0][10..].StartsWith(kind + ":", StringComparison.Ordinal))
        {
            Log[0] = stamped;
        }
        else
        {
            Log.Insert(0, stamped);
            while (Log.Count > 200)
            {
                Log.RemoveAt(Log.Count - 1);
            }
        }

        Notify();
    }

    private void Notify() => Changed?.Invoke(this, EventArgs.Empty);
}
