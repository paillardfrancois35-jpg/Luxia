using System.Globalization;

namespace Luxia.Show.Model;

/// <summary>Comparaison du style d'un morceau avec celui qu'une condition de show attend (P9, doc 21 §3.4).</summary>
public static class StyleMatching
{
    /// <summary>
    /// Le style courant est celui que veut la condition : même nom, ou l'une des parties du nom de la famille (« Électro » pour
    /// « Électro / Dance »), sans tenir compte de la casse ni des accents (les familles ont des noms composés).
    /// </summary>
    /// <param name="current">Style courant (nom de famille : « Électro / Dance »).</param>
    /// <param name="wanted">Style attendu par la condition (« Électro »).</param>
    /// <returns>Vrai s'ils correspondent.</returns>
    public static bool Matches(string current, string wanted)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(wanted);
        return wanted.Trim().Length > 0 && (Same(current, wanted) || current.Split('/').Any(part => Same(part, wanted)));
    }

    private static bool Same(string a, string b) =>
        CultureInfo.InvariantCulture.CompareInfo.Compare(a.Trim(), b.Trim(), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0;
}
