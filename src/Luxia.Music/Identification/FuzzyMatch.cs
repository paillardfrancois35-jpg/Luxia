namespace Luxia.Music.Identification;

/// <summary>
/// Rapprochement flou de deux formes normalisées (doc 21 §3.3) : tolère les fautes de frappe, les mots inversés et les mots
/// manquants, sans accepter les petits mots ni les nombres approximatifs (faux positifs). Score de 0 à 1.
/// </summary>
public static class FuzzyMatch
{
    /// <summary>Longueur minimale d'un mot pour qu'une faute de frappe soit tolérée.</summary>
    private const int MinFuzzyLength = 5;

    /// <summary>Similarité de deux clés normalisées (mots séparés par des espaces) : 1 = identiques.</summary>
    /// <param name="a">Première clé.</param>
    /// <param name="b">Seconde clé.</param>
    /// <returns>Score de 0 à 1.</returns>
    public static double Score(string a, string b)
    {
        if (a.Length == 0 || b.Length == 0)
        {
            return 0;
        }

        if (a == b)
        {
            return 1;
        }

        var wordsA = a.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var wordsB = b.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var (shorter, longer) = wordsA.Length <= wordsB.Length ? (wordsA, wordsB) : (wordsB, wordsA);
        var used = new bool[longer.Length];
        double total = 0;
        foreach (var word in shorter)
        {
            var best = 0.0;
            var bestIndex = -1;
            for (var i = 0; i < longer.Length; i++)
            {
                if (used[i])
                {
                    continue;
                }

                var similarity = WordSimilarity(word, longer[i]);
                if (similarity > best)
                {
                    best = similarity;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0)
            {
                used[bestIndex] = true;
                total += best;
            }
        }

        // Comme le coefficient de Dice : les mots manquants d'un côté ou de l'autre font baisser le score.
        return 2 * total / (wordsA.Length + wordsB.Length);
    }

    /// <summary>Similarité de deux mots : 1 s'ils sont égaux ; une faute n'est tolérée que dans un mot assez long et sans chiffre.</summary>
    internal static double WordSimilarity(string a, string b)
    {
        if (a == b)
        {
            return 1;
        }

        if (a.Length < MinFuzzyLength || b.Length < MinFuzzyLength || a.Any(char.IsDigit) || b.Any(char.IsDigit))
        {
            return 0;
        }

        var distance = Levenshtein(a, b);
        var similarity = 1.0 - ((double)distance / Math.Max(a.Length, b.Length));
        return similarity >= 0.75 ? similarity : 0;
    }

    /// <summary>Distance d'édition (insertion, suppression, remplacement) entre deux mots.</summary>
    internal static int Levenshtein(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}
