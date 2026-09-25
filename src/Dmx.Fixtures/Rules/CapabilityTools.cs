using System.Globalization;
using Dmx.Fixtures.Model;

namespace Dmx.Fixtures.Rules;

/// <summary>Outils de saisie rapide des plages (BIB-023) et de découverte (BIB-062).</summary>
public static class CapabilityTools
{
    /// <summary>Découpe 0-255 (ou une plage existante) en <paramref name="count"/> plages égales.</summary>
    public static IReadOnlyList<Capability> Split(int count, int min = 0, int max = 255, string labelPrefix = "Plage")
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, max - min + 1);
        var result = new List<Capability>(count);
        var span = max - min + 1;
        for (var i = 0; i < count; i++)
        {
            var from = min + (int)Math.Round(span * i / (double)count);
            var to = min + (int)Math.Round(span * (i + 1) / (double)count) - 1;
            result.Add(new Capability { Min = from, Max = to, Label = string.Create(CultureInfo.CurrentCulture, $"{labelPrefix} {i + 1}") });
        }

        return result;
    }

    /// <summary>Ajoute une plage couvrant le premier trou (0-255) ; renvoie les plages inchangées s'il n'y en a pas.</summary>
    public static IReadOnlyList<Capability> FillNextGap(IReadOnlyList<Capability> capabilities, string label = "Nouvelle plage")
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        var gap = FirstGap(capabilities);
        return gap is { } g
            ? [.. capabilities.Append(new Capability { Min = g.Min, Max = g.Max, Label = label }).OrderBy(c => c.Min)]
            : capabilities;
    }

    /// <summary>Premier intervalle de 0-255 non couvert.</summary>
    public static (int Min, int Max)? FirstGap(IReadOnlyList<Capability> capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        var next = 0;
        foreach (var c in capabilities.OrderBy(c => c.Min))
        {
            if (c.Min > next)
            {
                return (next, c.Min - 1);
            }

            next = Math.Max(next, c.Max + 1);
        }

        return next <= 255 ? (next, 255) : null;
    }

    /// <summary>
    /// Crée une borne à <paramref name="value"/> : la plage qui la contient est coupée en deux (« Nouvelle plage ici », BIB-062).
    /// Sans plage existante, on crée 0-(v-1) et v-255.
    /// </summary>
    public static IReadOnlyList<Capability> SplitAt(IReadOnlyList<Capability> capabilities, int value, string label = "Nouvelle plage")
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 255);
        if (capabilities.Count == 0)
        {
            return
            [
                new Capability { Min = 0, Max = value - 1, Label = "Plage 1" },
                new Capability { Min = value, Max = 255, Label = label },
            ];
        }

        var target = capabilities.FirstOrDefault(c => c.Contains(value));
        if (target is null)
        {
            return FillNextGap(capabilities, label);
        }

        if (target.Min == value)
        {
            return capabilities;
        }

        return [.. capabilities
            .Where(c => c != target)
            .Append(target with { Max = value - 1 })
            .Append(target with { Min = value, Label = label })
            .OrderBy(c => c.Min)];
    }

    /// <summary>Déplace la frontière entre la plage <paramref name="index"/> et la suivante (barre 0-255 redimensionnable, BIB-022).</summary>
    public static IReadOnlyList<Capability> MoveBoundary(IReadOnlyList<Capability> capabilities, int index, int newMax)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        var sorted = capabilities.OrderBy(c => c.Min).ToList();
        if (index < 0 || index >= sorted.Count - 1)
        {
            return capabilities;
        }

        var left = sorted[index];
        var right = sorted[index + 1];
        var max = Math.Clamp(newMax, left.Min, right.Max - 1);
        var adjacent = right.Min == left.Max + 1;
        sorted[index] = left with { Max = max };
        if (adjacent)
        {
            sorted[index + 1] = right with { Min = max + 1 };
        }

        return sorted;
    }
}
