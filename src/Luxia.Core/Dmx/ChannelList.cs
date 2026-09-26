using System.Globalization;

namespace Luxia.Core.Dmx;

/// <summary>Lecture / écriture d'une liste de canaux au format texte « 1, 5-8, 180 ».</summary>
public static class ChannelList
{
    private static readonly char[] Separators = [',', ';', ' '];

    /// <summary>Lit une liste de canaux (triée, sans doublon) ; renvoie <c>false</c> si un élément est invalide.</summary>
    public static bool TryParse(string? text, out IReadOnlyList<int> channels)
    {
        channels = [];
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var result = new SortedSet<int>();
        foreach (var item in text.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!ChannelRange.TryParse(item, out var range))
            {
                return false;
            }

            for (var c = range.First; c <= range.Last; c++)
            {
                result.Add(c);
            }
        }

        channels = [.. result];
        return true;
    }

    /// <summary>Écrit une liste de canaux en regroupant les plages contiguës (« 1-4, 180 »).</summary>
    public static string Format(IEnumerable<int> channels)
    {
        ArgumentNullException.ThrowIfNull(channels);
        var sorted = channels.Distinct().Order().ToList();
        var parts = new List<string>();
        var i = 0;
        while (i < sorted.Count)
        {
            var j = i;
            while (j + 1 < sorted.Count && sorted[j + 1] == sorted[j] + 1)
            {
                j++;
            }

            parts.Add(i == j
                ? sorted[i].ToString(CultureInfo.InvariantCulture)
                : string.Create(CultureInfo.InvariantCulture, $"{sorted[i]}-{sorted[j]}"));
            i = j + 1;
        }

        return string.Join(", ", parts);
    }
}
