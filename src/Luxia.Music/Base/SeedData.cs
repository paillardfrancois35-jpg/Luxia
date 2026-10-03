using System.Globalization;
using System.Reflection;
using Luxia.Music.Normalization;

namespace Luxia.Music.Base;

/// <summary>Base de départ livrée avec l'application (Q48) : quelques centaines d'artistes populaires en soirée (un style chacun : le dominant).</summary>
public static class SeedData
{
    private const string ResourceName = "Luxia.Music.Seed.artistes-initiaux.txt";

    /// <summary>Lit les artistes livrés.</summary>
    /// <returns>Les artistes de départ.</returns>
    public static ArtistSet Artists()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Ressource {ResourceName} absente");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    /// <summary>Lit un texte au format « Nom | famille[:poids], … | alias ; alias » (seule la famille dominante est gardée ; les lignes « # » sont des commentaires).</summary>
    /// <param name="text">Le texte.</param>
    /// <returns>Les artistes.</returns>
    public static ArtistSet Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var artists = new List<ArtistEntry>();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var parts = line.Split('|');
            if (parts.Length < 2 || parts[0].Trim().Length == 0)
            {
                continue;
            }

            // « Nom | famille[:poids], … » : un artiste n'a plus qu'un style, le plus pondéré (le premier à poids égal).
            var style = parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select((item, rank) =>
                {
                    var pair = item.Split(':', 2);
                    var weight = pair.Length == 2 && double.TryParse(pair[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var w) ? w : 1.0;
                    return (Id: pair[0].Trim(), Weight: weight, Rank: rank);
                })
                .OrderByDescending(x => x.Weight)
                .ThenBy(x => x.Rank)
                .Select(x => x.Id)
                .FirstOrDefault() ?? Taxonomy.UnknownId;

            var aliases = parts.Length > 2
                ? parts[2].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(a => TextKey.Of(a) != TextKey.Of(parts[0])).ToList()
                : [];
            artists.Add(new ArtistEntry { Name = parts[0].Trim(), Aliases = aliases, Style = style });
        }

        return new ArtistSet { Artists = artists };
    }
}
