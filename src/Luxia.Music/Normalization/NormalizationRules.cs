namespace Luxia.Music.Normalization;

/// <summary>
/// Règles de normalisation des titres (doc 21 §3.1, MUS-020), stockées dans <c>normalisation.json</c> du projet : modifiables sans
/// recompiler. Toutes les comparaisons se font sur la forme normalisée (minuscules, sans accent ni ponctuation).
/// </summary>
public sealed record NormalizationRules
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>
    /// Applications dont le champ « artiste » est le nom d'une chaîne (navigateurs : YouTube, YouTube Music) : l'artiste est alors
    /// cherché d'abord dans le titre. Un nom d'application qui contient l'un de ces mots est concerné.
    /// </summary>
    public IReadOnlyList<string> UntrustedApps { get; init; } = ["chrome", "edge", "firefox", "brave", "opera", "vivaldi", "safari", "iexplore"];

    /// <summary>Suffixes d'un nom de chaîne à retirer (« Queen - Topic », « QueenVEVO », « Queen Official »).</summary>
    public IReadOnlyList<string> ArtistSuffixes { get; init; } = ["- topic", "vevo", "official", "officiel", "oficial"];

    /// <summary>Mots qui, seuls dans un groupe entre parenthèses ou après un tiret, désignent une mention parasite (« Official Video »).</summary>
    public IReadOnlyList<string> NoiseWords { get; init; } =
    [
        "official", "officiel", "officielle", "oficial", "video", "videoclip", "clip", "audio", "lyrics", "lyric", "paroles", "letra",
        "hd", "hq", "4k", "1080p", "720p", "visualizer", "visualiser", "mv", "m", "v", "music", "musique", "full", "explicit", "clean",
        "remaster", "remastered", "remasterise", "deluxe", "bonus", "track", "mono", "stereo", "with", "new", "nouveau", "version",
        "subtitulado", "subtitulada", "sub", "espanol", "english", "traduction", "traducida", "legendado", "sous", "titres", "titre",
    ];

    /// <summary>Expressions parasites entières (« Radio Edit »), plus précises que les mots ci-dessus.</summary>
    public IReadOnlyList<string> NoisePhrases { get; init; } =
    [
        "radio edit", "radio mix", "single version", "single edit", "album version", "album edit", "original version", "original mix",
        "original", "edit", "version originale",
    ];

    /// <summary>Débuts d'un groupe parasite (« From the Motion Picture… », « Original Motion Picture Soundtrack »).</summary>
    public IReadOnlyList<string> NoisePrefixes { get; init; } = ["from ", "original motion picture", "original soundtrack", "original score", "bande originale", "ost"];

    /// <summary>Mots qui désignent une version, conservée à part : un remix peut changer de style (« Extended Mix », « Live at… »).</summary>
    public IReadOnlyList<string> VersionWords { get; init; } =
    [
        "remix", "mix", "edit", "version", "live", "acoustic", "instrumental", "mashup", "bootleg", "cover", "remake", "slowed", "sped",
        "nightcore", "reprise", "unplugged", "karaoke", "dub", "vip", "extended", "rework", "flip", "refix", "rmx", "reverb",
    ];

    /// <summary>Mots qui introduisent un artiste invité (« feat. X »).</summary>
    public IReadOnlyList<string> GuestMarkers { get; init; } = ["feat", "ft", "featuring", "with", "avec", "w"];

    /// <summary>Extensions de fichier à retirer d'un titre (lecteurs qui affichent le nom du fichier).</summary>
    public IReadOnlyList<string> FileExtensions { get; init; } = ["mp3", "flac", "wav", "m4a", "ogg", "wma", "aac", "opus", "aiff"];

    /// <summary>Règles livrées avec l'application.</summary>
    public static NormalizationRules Default { get; } = new();
}
