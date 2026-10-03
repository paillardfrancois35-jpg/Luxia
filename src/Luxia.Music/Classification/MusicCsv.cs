using System.Globalization;
using System.Text;
using Luxia.Music.Base;
using Luxia.Music.Identification;
using Luxia.Music.Normalization;

namespace Luxia.Music.Classification;

/// <summary>Résultat d'un import de playlist ou de base (MUS-027, MUS-029).</summary>
/// <param name="Rows">Lignes lues.</param>
/// <param name="ArtistsSet">Artistes classés par une ligne qui n'avait qu'un artiste et un style.</param>
/// <param name="TitlesSet">Titres classés par une ligne qui avait un titre et un style.</param>
/// <param name="AlreadyKnown">Lignes dont la base connaît déjà le style (rien à faire).</param>
/// <param name="ToClassify">Lignes sans style que la base ne sait pas classer : elles vont dans « À classer ».</param>
/// <param name="Problems">Lignes ignorées ou remarques (style inconnu, ligne sans titre).</param>
public sealed record ImportReport(int Rows, int ArtistsSet, int TitlesSet, int AlreadyKnown, IReadOnlyList<PendingItem> ToClassify, IReadOnlyList<string> Problems);

/// <summary>
/// Import et export CSV de la base musicale (MUS-027) et des playlists (MUS-029) : les exports de Deezer, Spotify ou d'un tableur
/// (colonnes « Track Name », « Artist Name(s) » ; « artiste », « titre », « style »), séparateur « ; », « , » ou tabulation.
/// </summary>
public static class MusicCsv
{
    private static readonly string[] ArtistNames = ["artiste", "artist", "artists", "artist name", "artist names", "artist name s", "interprete", "performer", "auteur"];
    private static readonly string[] TitleNames = ["titre", "title", "track", "track name", "song", "name", "morceau"];
    private static readonly string[] StyleNames = ["style", "genre", "genres", "famille"];

    /// <summary>Exporte les artistes de la base : artiste, style dominant, poids, alias, source.</summary>
    /// <param name="musicBase">Base.</param>
    /// <returns>Le texte CSV (séparateur « ; », fin de ligne CRLF).</returns>
    public static string ExportArtists(MusicBase musicBase)
    {
        ArgumentNullException.ThrowIfNull(musicBase);
        var text = new StringBuilder("artiste;style;poids;alias;source\r\n");
        foreach (var artist in musicBase.SearchArtists(null, int.MaxValue))
        {
            var top = artist.Styles.OrderByDescending(s => s.Value).FirstOrDefault();
            var family = musicBase.FamilyById(top.Key);
            text.Append(string.Join(';', Cell(artist.Name), Cell(family?.Name ?? top.Key ?? string.Empty), top.Value.ToString("0.##", CultureInfo.InvariantCulture), Cell(string.Join(" | ", artist.Aliases)), Cell(artist.Source)));
            text.Append("\r\n");
        }

        return text.ToString();
    }

    /// <summary>
    /// Importe un CSV : une ligne avec un style classe l'artiste (ou le titre s'il y en a un) ; une ligne sans style est identifiée avec
    /// la base, et si elle reste inconnue elle part dans « À classer ».
    /// </summary>
    /// <param name="csv">Texte CSV.</param>
    /// <param name="musicBase">Base à enrichir.</param>
    /// <param name="normalizer">Normaliseur de titres.</param>
    /// <param name="identifier">Identification.</param>
    /// <returns>Le bilan.</returns>
    public static ImportReport Import(string csv, MusicBase musicBase, TrackNormalizer normalizer, StyleIdentifier identifier)
    {
        ArgumentNullException.ThrowIfNull(csv);
        ArgumentNullException.ThrowIfNull(musicBase);
        ArgumentNullException.ThrowIfNull(normalizer);
        ArgumentNullException.ThrowIfNull(identifier);
        var rows = EveningJournal.Rows(csv.TrimStart('﻿'));
        var problems = new List<string>();
        if (rows.Count == 0)
        {
            return new ImportReport(0, 0, 0, 0, [], ["fichier vide"]);
        }

        var header = rows[0].Select(h => TextKey.Of(h)).ToList();
        int Find(string[] names) => header.FindIndex(h => names.Contains(h, StringComparer.Ordinal));
        var (artistCol, titleCol, styleCol) = (Find(ArtistNames), Find(TitleNames), Find(StyleNames));
        var hasHeader = artistCol >= 0 || titleCol >= 0;
        if (!hasHeader)
        {
            // Sans en-tête : artiste, titre, style dans cet ordre ; une seule colonne = « Artiste - Titre ».
            (artistCol, titleCol, styleCol) = rows[0].Count >= 2 ? (0, 1, rows[0].Count >= 3 ? 2 : -1) : (-1, 0, -1);
        }

        string Cell(List<string> row, int index) => index >= 0 && index < row.Count ? row[index].Trim() : string.Empty;
        var (artistsSet, titlesSet, known, read) = (0, 0, 0, 0);
        var toClassify = new List<PendingItem>();
        for (var i = hasHeader ? 1 : 0; i < rows.Count; i++)
        {
            var (artist, title, style) = (Cell(rows[i], artistCol), Cell(rows[i], titleCol), Cell(rows[i], styleCol));
            read++;
            if (title.Length == 0 && artist.Length == 0)
            {
                problems.Add($"ligne {i + 1} : ni artiste ni titre");
                continue;
            }

            var family = style.Length > 0 ? musicBase.FindFamily(style.Split(';', '/', ',')[0]) : null;
            if (style.Length > 0 && family is null)
            {
                problems.Add($"ligne {i + 1} : style inconnu « {style} » (la ligne est traitée sans style)");
            }

            if (family is not null)
            {
                // Le titre et l'artiste sont nettoyés comme un titre de lecteur avant d'entrer dans la base.
                var cleaned = normalizer.Normalize(title, artist).Primary;
                var cleanArtist = cleaned.ArtistDisplay.Length > 0 ? cleaned.ArtistDisplay : artist;
                if (cleaned.TitleDisplay.Length > 0 && cleanArtist.Length > 0 && title.Length > 0)
                {
                    musicBase.UpsertTitle(new TitleEntry { Artist = cleanArtist, Title = cleaned.TitleDisplay, Style = family.Id, Source = "import", Version = cleaned.Versions.Count > 0 ? string.Join(' ', cleaned.Versions) : null });
                    titlesSet++;
                }
                else if (cleanArtist.Length > 0)
                {
                    musicBase.SetArtistStyle(cleanArtist, family.Id);
                    artistsSet++;
                }
                else
                {
                    problems.Add($"ligne {i + 1} : titre sans artiste, non classé");
                }

                continue;
            }

            var result = identifier.Identify(normalizer.Normalize(title, artist));
            if (result.IsKnown && result.Confidence >= ClassifyList.LowConfidence)
            {
                known++;
            }
            else if (title.Length > 0)
            {
                toClassify.Add(new PendingItem(artist, title, string.Empty, "playlist", 0));
            }
        }

        return new ImportReport(read, artistsSet, titlesSet, known, toClassify, problems);
    }

    private static string Cell(string value)
    {
        var text = value.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
        return text.Contains(';', StringComparison.Ordinal) || text.Contains('"', StringComparison.Ordinal) ? $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : text;
    }
}
