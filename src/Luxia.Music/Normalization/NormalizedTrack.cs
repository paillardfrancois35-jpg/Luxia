namespace Luxia.Music.Normalization;

/// <summary>D'où vient l'artiste d'une hypothèse de lecture d'un titre.</summary>
public enum HypothesisOrigin
{
    /// <summary>L'artiste donné par le lecteur, tel quel (Deezer, Spotify, fichier étiqueté).</summary>
    PlayerArtist,

    /// <summary>L'artiste est la partie « Artiste - Titre » (ou « Titre | Artiste ») du titre ; le champ « artiste » est une chaîne.</summary>
    TitleSplit,

    /// <summary>Le champ « artiste » du lecteur, nom de chaîne nettoyé (« XYZ - Topic », « XYZVEVO »), avec le titre entier.</summary>
    ChannelAsArtist,

    /// <summary>Aucun artiste trouvé : le titre seul.</summary>
    NoArtist,
}

/// <summary>Une façon de lire un titre brut : un artiste, un titre, des invités et des versions (MUS-020).</summary>
/// <param name="Artist">Clé de l'artiste complet (« earth wind et fire ») ; vide si inconnu.</param>
/// <param name="ArtistDisplay">Artiste nettoyé, casse d'origine.</param>
/// <param name="Credits">Clés des artistes pris un à un (« earth », « wind », « fire ») : le premier est l'artiste principal ; un seul s'il n'y a pas de séparateur.</param>
/// <param name="Guests">Clés des artistes invités (« feat. X »).</param>
/// <param name="Title">Clé du titre sans les mentions parasites, invités et versions.</param>
/// <param name="TitleDisplay">Titre nettoyé, casse d'origine.</param>
/// <param name="Versions">Clés des mentions de version conservées à part (« extended mix », « kokwak hardstyle remix »).</param>
/// <param name="Origin">Origine de l'artiste.</param>
public sealed record TrackHypothesis(
    string Artist,
    string ArtistDisplay,
    IReadOnlyList<string> Credits,
    IReadOnlyList<string> Guests,
    string Title,
    string TitleDisplay,
    IReadOnlyList<string> Versions,
    HypothesisOrigin Origin)
{
    /// <summary>Le titre est une version remixée, en direct ou autrement modifiée (le style peut différer de l'original).</summary>
    public bool IsVersion => Versions.Count > 0;
}

/// <summary>Résultat de la normalisation d'un titre brut : les hypothèses de lecture, de la plus probable à la moins probable (MUS-020).</summary>
/// <param name="RawTitle">Titre brut du lecteur.</param>
/// <param name="RawArtist">Artiste brut du lecteur.</param>
/// <param name="App">Application source.</param>
/// <param name="Hypotheses">Au moins une hypothèse.</param>
public sealed record NormalizedTrack(string RawTitle, string RawArtist, string App, IReadOnlyList<TrackHypothesis> Hypotheses)
{
    /// <summary>L'hypothèse la plus probable.</summary>
    public TrackHypothesis Primary => Hypotheses[0];
}
