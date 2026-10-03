namespace Luxia.Music.Base;

/// <summary>Une famille de styles de la taxonomie (doc 21 §3.4).</summary>
public sealed record MusicFamily
{
    /// <summary>Identifiant stable (« electro », « rock »).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Nom affiché, qui sert aussi de valeur de style dans les shows (« Électro / Dance »).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Étiquettes brutes de genre qui renvoient à cette famille (« eurodance », « french house »).</summary>
    public IReadOnlyList<string> Labels { get; init; } = [];
}

/// <summary>Taxonomie des familles de styles, enregistrée dans <c>taxonomie.json</c> (MUS-023).</summary>
public sealed record Taxonomy
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Identifiant de la famille des morceaux non identifiés (gérée par l'énergie seule).</summary>
    public const string UnknownId = "inconnu";

    /// <summary>Nom de la famille des morceaux non identifiés.</summary>
    public const string UnknownName = "Inconnu";

    /// <summary>Familles, dans l'ordre d'affichage.</summary>
    public IReadOnlyList<MusicFamily> Families { get; init; } = [];
}

/// <summary>
/// Un artiste de la base musicale (doc 21 §3.2, doc 50 §12j) : un code stable, un nom, des alias et <b>un seul style</b>. Les alias sont
/// uniques dans toute la base (jamais le nom d'un autre artiste). « Inconnu » est un style comme un autre : un morceau d'un artiste absent
/// de la base y est injecté pour être classé plus tard.
/// </summary>
public sealed record ArtistEntry
{
    /// <summary>Code stable (« A00012 »), attribué à la création ; ne change jamais, même si le nom change.</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Nom affiché.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Autres orthographes et noms courts.</summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>Identifiant de la famille (le style de l'artiste) ; <see cref="Taxonomy.UnknownId"/> si pas encore classé.</summary>
    public string Style { get; init; } = Taxonomy.UnknownId;
}

/// <summary>Artistes de la base, enregistrés dans <c>artistes.json</c>.</summary>
public sealed record ArtistSet
{
    /// <summary>Version courante du format de fichier (2 : un style par artiste, code stable, sans poids ni origine).</summary>
    public const int CurrentFormatVersion = 2;

    /// <summary>Artistes.</summary>
    public IReadOnlyList<ArtistEntry> Artists { get; init; } = [];
}

/// <summary>Un titre de la base, dont le style peut différer de celui de l'artiste (le slow d'un groupe de rock).</summary>
public sealed record TitleEntry
{
    /// <summary>Nom de l'artiste.</summary>
    public string Artist { get; init; } = string.Empty;

    /// <summary>Titre.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Autres écritures du titre.</summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>Version (« extended mix ») si le style est celui de cette version seulement ; <c>null</c> pour l'original.</summary>
    public string? Version { get; init; }

    /// <summary>Identifiant de la famille propre à ce titre ; vide = le titre suit le style de son artiste.</summary>
    public string Style { get; init; } = string.Empty;

    /// <summary>Tempo mémorisé (AUD-028) ; <c>null</c> si inconnu.</summary>
    public double? Bpm { get; init; }
}

/// <summary>Titres de la base, enregistrés dans <c>titres.json</c>.</summary>
public sealed record TitleSet
{
    /// <summary>Version courante du format de fichier (2 : sans origine ; le style est facultatif).</summary>
    public const int CurrentFormatVersion = 2;

    /// <summary>Titres.</summary>
    public IReadOnlyList<TitleEntry> Titles { get; init; } = [];
}
