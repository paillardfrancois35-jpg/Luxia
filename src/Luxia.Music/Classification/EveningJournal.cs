using System.Globalization;
using System.Text;

namespace Luxia.Music.Classification;

/// <summary>Une ligne du journal de soirée (GEN-111, MUS-025).</summary>
/// <param name="Time">Heure (« 21:12:05 »).</param>
/// <param name="Title">Titre brut du lecteur.</param>
/// <param name="Artist">Artiste brut du lecteur.</param>
/// <param name="App">Application source ; vide dans les journaux anciens.</param>
/// <param name="Style">Style trouvé (« Rock », « Inconnu »).</param>
/// <param name="Confidence">Confiance, de 0 à 1.</param>
/// <param name="Method">Méthode de l'identification.</param>
/// <param name="Forced">Le style avait été imposé à la main.</param>
/// <param name="Show">Show en cours.</param>
public sealed record EveningEntry(string Time, string Title, string Artist, string App, string Style, double Confidence, string Method, bool Forced, string Show);

/// <summary>Lecture des journaux de soirée (<c>soiree-AAAAMMJJ.csv</c>) : séparateur « ; », guillemets doublés, colonnes repérées par leur nom.</summary>
public static class EveningJournal
{
    /// <summary>Lit le contenu d'un journal.</summary>
    /// <param name="csv">Texte du fichier.</param>
    /// <returns>Les lignes de morceaux ; un fichier sans en-tête reconnu donne une liste vide.</returns>
    public static IReadOnlyList<EveningEntry> Parse(string csv)
    {
        ArgumentNullException.ThrowIfNull(csv);
        var rows = Rows(csv.TrimStart('﻿'));
        if (rows.Count == 0)
        {
            return [];
        }

        var header = rows[0].Select(h => h.Trim().ToLowerInvariant()).ToList();
        int Col(params string[] names) => header.FindIndex(h => names.Contains(h, StringComparer.Ordinal));
        var (time, title, artist, app, style, confidence, method, forced, show) =
            (Col("heure"), Col("titre"), Col("artiste"), Col("application"), Col("style"), Col("confiance"), Col("méthode", "methode"), Col("imposé", "impose"), Col("show"));
        if (title < 0 || artist < 0 || style < 0)
        {
            return [];
        }

        string Cell(List<string> row, int index) => index >= 0 && index < row.Count ? row[index] : string.Empty;
        var entries = new List<EveningEntry>();
        foreach (var row in rows.Skip(1))
        {
            if (Cell(row, title).Length == 0 && Cell(row, artist).Length == 0)
            {
                continue;
            }

            _ = double.TryParse(Cell(row, confidence), NumberStyles.Float, CultureInfo.InvariantCulture, out var percent);
            entries.Add(new EveningEntry(
                Cell(row, time),
                Cell(row, title),
                Cell(row, artist),
                Cell(row, app),
                Cell(row, style),
                Math.Clamp(percent / 100.0, 0, 1),
                Cell(row, method),
                Cell(row, forced).Length > 0,
                Cell(row, show)));
        }

        return entries;
    }

    /// <summary>Découpe un texte CSV en lignes de cellules (séparateur « ; » ou « , » ou tabulation, guillemets doublés).</summary>
    /// <param name="text">Texte.</param>
    /// <returns>Les lignes, sans les lignes vides.</returns>
    internal static List<List<string>> Rows(string text)
    {
        var separator = DetectSeparator(text);
        var rows = new List<List<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    quoted = false;
                }
                else
                {
                    cell.Append(c);
                }

                continue;
            }

            if (c == '"' && cell.Length == 0)
            {
                quoted = true;
            }
            else if (c == separator)
            {
                row.Add(cell.ToString());
                cell.Clear();
            }
            else if (c is '\r' or '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                row.Add(cell.ToString());
                cell.Clear();
                if (row.Any(x => x.Length > 0))
                {
                    rows.Add(row);
                }

                row = [];
            }
            else
            {
                cell.Append(c);
            }
        }

        row.Add(cell.ToString());
        if (row.Any(x => x.Length > 0))
        {
            rows.Add(row);
        }

        return rows;
    }

    private static char DetectSeparator(string text)
    {
        var first = text.Split('\n', 2)[0];
        var best = ';';
        var count = first.Count(c => c == ';');
        foreach (var candidate in new[] { ',', '\t' })
        {
            var n = first.Count(c => c == candidate);
            if (n > count)
            {
                best = candidate;
                count = n;
            }
        }

        return best;
    }
}
