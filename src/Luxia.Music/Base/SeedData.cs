using System.Globalization;
using System.Reflection;
using Luxia.Music.Normalization;

namespace Luxia.Music.Base;

/// <summary>Base de départ livrée avec l'application (Q48) : quelques centaines d'artistes populaires en soirée, source « initial ».</summary>
public static class SeedData
{
    /// <summary>Source des entrées de la base livrée.</summary>
    public const string Source = "initial";

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

    /// <summary>Lit un texte au format « Nom | famille[:poids], … | alias ; alias » (les lignes « # » sont des commentaires).</summary>
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

            var styles = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var item in parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var pair = item.Split(':', 2);
                var weight = pair.Length == 2 && double.TryParse(pair[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var w) ? w : 1.0;
                styles[pair[0].Trim()] = Math.Clamp(weight, 0, 1);
            }

            var aliases = parts.Length > 2
                ? parts[2].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(a => TextKey.Of(a) != TextKey.Of(parts[0])).ToList()
                : [];
            artists.Add(new ArtistEntry { Name = parts[0].Trim(), Aliases = aliases, Styles = styles, Source = Source });
        }

        return new ArtistSet { Artists = artists };
    }
}
