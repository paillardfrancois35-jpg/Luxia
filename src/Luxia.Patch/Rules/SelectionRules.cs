using Luxia.Patch.Model;

namespace Luxia.Patch.Rules;

/// <summary>Opérations sur une sélection (INST-033).</summary>
public static class SelectionRules
{
    /// <summary>Inverse l'ordre.</summary>
    public static IReadOnlyList<SelectionItem> Reverse(IReadOnlyList<SelectionItem> items) =>
        [.. items.Reverse()];

    /// <summary>Éléments de rang pair (2, 4, 6… en base 1) dans leur ordre d'origine.</summary>
    public static IReadOnlyList<SelectionItem> Even(IReadOnlyList<SelectionItem> items) =>
        [.. items.Where((_, i) => (i + 1) % 2 == 0)];

    /// <summary>Éléments de rang impair (1, 3, 5…) dans leur ordre d'origine.</summary>
    public static IReadOnlyList<SelectionItem> Odd(IReadOnlyList<SelectionItem> items) =>
        [.. items.Where((_, i) => (i + 1) % 2 != 0)];

    /// <summary>Première moitié (arrondie au-dessus) de la sélection.</summary>
    public static IReadOnlyList<SelectionItem> FirstHalf(IReadOnlyList<SelectionItem> items) =>
        [.. items.Take((items.Count + 1) / 2)];

    /// <summary>Seconde moitié de la sélection.</summary>
    public static IReadOnlyList<SelectionItem> SecondHalf(IReadOnlyList<SelectionItem> items) =>
        [.. items.Skip((items.Count + 1) / 2)];

    /// <summary>
    /// Réordonne selon la position au plan du lieu actif (INST-033) : de gauche à droite (X croissant),
    /// d'avant en arrière (Y croissant), ou du centre vers l'extérieur (distance au centre de la salle croissante).
    /// Un appareil non placé dans le lieu garde son rang d'origine, en fin de liste.
    /// </summary>
    public static IReadOnlyList<SelectionItem> OrderByPosition(
        IReadOnlyList<SelectionItem> items, Venue venue, PositionOrder order)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(venue);
        var placed = new List<(SelectionItem Item, FixturePlacement Placement)>();
        var unplaced = new List<SelectionItem>();
        foreach (var item in items)
        {
            if (venue.PlacementOf(item.FixtureId) is { } placement)
            {
                placed.Add((item, placement));
            }
            else
            {
                unplaced.Add(item);
            }
        }

        var centerX = venue.WidthM / 2;
        var centerY = venue.DepthM / 2;
        IEnumerable<(SelectionItem Item, FixturePlacement Placement)> sorted = order switch
        {
            PositionOrder.LeftToRight => placed.OrderBy(p => p.Placement.X),
            PositionOrder.FrontToBack => placed.OrderBy(p => p.Placement.Y),
            PositionOrder.CenterOutward => placed.OrderBy(p => Distance(p.Placement.X, p.Placement.Y, centerX, centerY)),
            _ => throw new NotSupportedException(),
        };

        return [.. sorted.Select(p => p.Item), .. unplaced];
    }

    private static double Distance(double x1, double y1, double x2, double y2) =>
        Math.Sqrt(((x1 - x2) * (x1 - x2)) + ((y1 - y2) * (y1 - y2)));
}

/// <summary>Critère de tri par position (INST-033).</summary>
public enum PositionOrder
{
    /// <summary>Gauche → droite (X croissant).</summary>
    LeftToRight,

    /// <summary>Avant → arrière (Y croissant).</summary>
    FrontToBack,

    /// <summary>Du centre vers l'extérieur.</summary>
    CenterOutward,
}
