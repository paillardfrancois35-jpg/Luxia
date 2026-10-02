using System.Text.RegularExpressions;

namespace Luxia.Media;

/// <summary>Noms d'applications des sessions média.</summary>
public static partial class MediaApps
{
    /// <summary>
    /// Nom court lisible d'après l'identifiant d'application de Windows : « Chrome », « Spotify.exe » → « Spotify »,
    /// « Deezer.62021768415AF_q7m17pa7q8kj0 » (application du Store) → « Deezer ».
    /// </summary>
    /// <param name="appId">Identifiant d'application source de la session.</param>
    /// <returns>Nom court ; l'identifiant lui-même s'il n'a pas de forme connue.</returns>
    public static string FriendlyName(string appId)
    {
        ArgumentNullException.ThrowIfNull(appId);
        var name = appId.Split('!')[0];
        if (StorePackage().Match(name) is { Success: true } store)
        {
            return store.Groups[1].Value;
        }

        return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
    }

    // Nom de famille de paquet du Store : « Application.<identifiant d'éditeur>_<13 caractères> » ; l'identifiant d'éditeur est un
    // hachage qui contient des chiffres (ce qui écarte « Microsoft.ZuneMusic_… », où le second mot est un vrai nom).
    [GeneratedRegex(@"^(.+?)\.(?=[0-9A-Za-z]*\d)[0-9A-Za-z]{8,}_[0-9a-z]{13}$")]
    private static partial Regex StorePackage();
}
