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

/// <summary>Un artiste de la base musicale (doc 21 §3.2).</summary>
public sealed record ArtistEntry
{
    /// <summary>Nom affiché.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Autres orthographes et noms courts.</summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>Identifiant de famille → poids (0 à 1) ; le plus fort est le style dominant.</summary>
    public IReadOnlyDictionary<string, double> Styles { get; init; } = new Dictionary<string, double>();

    /// <summary>Origine : <c>initial</c> (base livrée), <c>manuel</c>, <c>correction</c>, <c>enrichissement</c>.</summary>
    public string Source { get; init; } = "manuel";
}

/// <summary>Artistes de la base, enregistrés dans <c>artistes.json</c>.</summary>
public sealed record ArtistSet
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

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

    /// <summary>Identifiant de la famille.</summary>
    public string Style { get; init; } = string.Empty;

    /// <summary>Tempo mémorisé (AUD-028) ; <c>null</c> si inconnu.</summary>
    public double? Bpm { get; init; }

    /// <summary>Origine, comme pour les artistes.</summary>
    public string Source { get; init; } = "manuel";
}

/// <summary>Titres de la base, enregistrés dans <c>titres.json</c>.</summary>
public sealed record TitleSet
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Titres.</summary>
    public IReadOnlyList<TitleEntry> Titles { get; init; } = [];
}

/// <summary>Une correction faite en Live (MUS-024) : historique conservé.</summary>
public sealed record StyleCorrection
{
    /// <summary>Date de la correction.</summary>
    public DateTimeOffset At { get; init; }

    /// <summary><c>title</c> (ce titre) ou <c>artist</c> (cet artiste).</summary>
    public string Scope { get; init; } = "artist";

    /// <summary>Artiste concerné.</summary>
    public string Artist { get; init; } = string.Empty;

    /// <summary>Titre concerné (étendue <c>title</c>).</summary>
    public string? Title { get; init; }

    /// <summary>Version du titre concernée, s'il y en a une.</summary>
    public string? Version { get; init; }

    /// <summary>Famille avant la correction ; <c>null</c> si inconnue.</summary>
    public string? OldStyle { get; init; }

    /// <summary>Famille choisie.</summary>
    public string NewStyle { get; init; } = string.Empty;
}

/// <summary>Corrections faites en Live, enregistrées dans <c>corrections.json</c>.</summary>
public sealed record CorrectionSet
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Corrections, de la plus ancienne à la plus récente.</summary>
    public IReadOnlyList<StyleCorrection> Corrections { get; init; } = [];
}
