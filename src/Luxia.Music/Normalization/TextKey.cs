using System.Globalization;
using System.Text;

namespace Luxia.Music.Normalization;

/// <summary>
/// Forme normalisée d'un nom ou d'un titre (doc 21 §3.1, « Uniformiser ») : minuscules, sans accent, sans ponctuation, « &amp; » → « et »,
/// espaces simples. Sert de clé de comparaison dans toute la chaîne d'identification.
/// </summary>
public static class TextKey
{
    /// <summary>Calcule la forme normalisée.</summary>
    /// <param name="text">Texte brut ; <c>null</c> ou vide donne une chaîne vide.</param>
    /// <returns>La clé : « Don't Stop Me Now » → « dont stop me now », « Earth, Wind &amp; Fire » → « earth wind et fire ».</returns>
    public static string Of(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var pendingSpace = false;

        void Append(string piece)
        {
            if (pendingSpace && builder.Length > 0)
            {
                builder.Append(' ');
            }

            pendingSpace = false;
            builder.Append(piece);
        }

        foreach (var raw in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(raw);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var c = char.ToLowerInvariant(raw);
            switch (c)
            {
                case '\'' or '’' or '‘' or '`' or '´' or '.':
                    continue; // « don't » → « dont », « S.O.S. » → « sos »
                case '&' or '+':
                    pendingSpace = true;
                    Append("et");
                    pendingSpace = true;
                    continue;
                case 'œ':
                    Append("oe");
                    continue;
                case 'æ':
                    Append("ae");
                    continue;
                case 'ß':
                    Append("ss");
                    continue;
                case 'ø':
                    Append("o");
                    continue;
                case 'đ':
                    Append("d");
                    continue;
                case 'ł':
                    Append("l");
                    continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                Append(c.ToString());
            }
            else
            {
                pendingSpace = true;
            }
        }

        return builder.ToString();
    }

    /// <summary>Mots d'une clé.</summary>
    /// <param name="key">Clé (déjà normalisée).</param>
    /// <returns>Les mots, dans l'ordre.</returns>
    public static string[] Words(string key) => key.Split(' ', StringSplitOptions.RemoveEmptyEntries);
}
