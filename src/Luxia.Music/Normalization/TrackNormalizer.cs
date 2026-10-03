using System.Text.RegularExpressions;

namespace Luxia.Music.Normalization;

/// <summary>
/// Normalisation d'un titre brut du lecteur (doc 21 §3.1, MUS-005, MUS-020) : sépare « Artiste - Titre » quand l'artiste du lecteur
/// est une chaîne YouTube, retire les mentions parasites (« Official Video », « Remastered 2011 »), extrait les invités
/// (« feat. X ») et les versions (« Extended Mix », « Live at… »), puis calcule les clés de comparaison. Sans état : un même
/// objet sert à tous les fils.
/// </summary>
/// <remarks>
/// Le résultat est une liste d'hypothèses de la plus probable à la moins probable : « Titre | Artiste » et « Artiste | Titre » se
/// ressemblent, c'est l'identification (chaîne du §3.3) qui départage avec la base musicale.
/// </remarks>
public sealed partial class TrackNormalizer
{
    private static readonly char[] PipeLike = ['|', '¦', '｜'];

    private readonly NormalizationRules _rules;
    private readonly HashSet<string> _noiseWords;
    private readonly HashSet<string> _noisePhrases;
    private readonly HashSet<string> _versionWords;
    private readonly string[] _noisePrefixes;
    private readonly Regex _extension;
    private readonly Regex _guestGroup;
    private readonly Regex _guestTail;
    private readonly Regex _artistSuffix;

    /// <summary>Crée un normaliseur.</summary>
    /// <param name="rules">Règles ; celles livrées par défaut.</param>
    public TrackNormalizer(NormalizationRules? rules = null)
    {
        _rules = rules ?? NormalizationRules.Default;
        _noiseWords = [.. _rules.NoiseWords.Select(TextKey.Of)];
        _noisePhrases = [.. _rules.NoisePhrases.Select(TextKey.Of)];
        _versionWords = [.. _rules.VersionWords.Select(TextKey.Of)];
        _noisePrefixes = [.. _rules.NoisePrefixes.Select(TextKey.Of)];
        _extension = new Regex(@"\.(?:" + string.Join('|', _rules.FileExtensions.Select(Regex.Escape)) + ")$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var markers = string.Join('|', _rules.GuestMarkers.Select(Regex.Escape));
        _guestGroup = new Regex(@"^(?:" + markers + @")\.?\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        _guestTail = new Regex(@"\s+(?:feat|ft|featuring)\.?\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        _artistSuffix = new Regex(@"\s*(?:" + string.Join('|', _rules.ArtistSuffixes.Select(s => Regex.Escape(s).Replace(@"\ ", @"\s*"))) + @")\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    /// <summary>Normalise un titre brut.</summary>
    /// <param name="title">Titre du lecteur.</param>
    /// <param name="artist">Artiste du lecteur (peut être vide).</param>
    /// <param name="app">Nom de l'application source (« Deezer », « Chrome »…).</param>
    /// <returns>Les hypothèses de lecture ; jamais vide.</returns>
    public NormalizedTrack Normalize(string? title, string? artist, string? app = null)
    {
        var rawTitle = title ?? string.Empty;
        var rawArtist = artist ?? string.Empty;
        var appName = app ?? string.Empty;

        var parts = new TitleParts();
        var text = PrepareTitle(rawTitle);
        text = StripGroups(text, parts);
        text = StripTrailingSegments(text, parts);
        text = Collapse(TrailingNoise().Replace(text, string.Empty));

        var credit = ParseArtist(rawArtist);
        var untrusted = IsUntrusted(appName);
        var hypotheses = new List<TrackHypothesis>();

        if (!untrusted && credit.Display.Length > 0)
        {
            // Lecteur fiable : l'artiste est celui du lecteur ; « Artiste - Titre » redondant dans le titre est retiré.
            var own = StripLeadingArtist(text, credit.Display);
            hypotheses.Add(Build(credit, own, parts, HypothesisOrigin.PlayerArtist));
        }
        else
        {
            var isTopic = credit.IsTopic;
            if (!isTopic)
            {
                foreach (var (left, right, pipe) in SplitCandidates(text))
                {
                    AddSplit(hypotheses, left, right, pipe, credit, parts, rawTitle);
                }
            }

            if (credit.Display.Length > 0)
            {
                hypotheses.Add(Build(credit, text, parts, isTopic ? HypothesisOrigin.PlayerArtist : HypothesisOrigin.ChannelAsArtist));
            }
            else if (hypotheses.Count == 0)
            {
                hypotheses.Add(Build(ArtistCredit.None, text, parts, HypothesisOrigin.NoArtist));
            }
        }

        var distinct = Distinct(hypotheses).ToList();
        if (distinct.Count == 0)
        {
            distinct.Add(Build(ArtistCredit.None, string.Empty, parts, HypothesisOrigin.NoArtist));
        }

        return new NormalizedTrack(rawTitle, rawArtist, appName, distinct);
    }

    private static IEnumerable<TrackHypothesis> Distinct(List<TrackHypothesis> hypotheses)
    {
        var seen = new HashSet<(string, string)>();
        foreach (var h in hypotheses)
        {
            if (h.Title.Length > 0 && seen.Add((h.Artist, h.Title)))
            {
                yield return h;
            }
        }
    }

    private static string Collapse(string text) => CollapseSpaces().Replace(text, " ").Trim();

    private static List<string> SplitNames(string text) =>
        [.. NameSeparators().Split(text).Select(n => n.Trim()).Where(n => n.Length > 0)];

    private static string StripLeadingArtist(string text, string artist)
    {
        var artistKey = TextKey.Of(artist);
        var index = text.IndexOf(" - ", StringComparison.Ordinal);
        if (index > 0 && artistKey.Length > 0 && TextKey.Of(text[..index]) == artistKey)
        {
            return text[(index + 3)..].Trim();
        }

        return text;
    }

    private static IEnumerable<(string Left, string Right, bool Pipe)> SplitCandidates(string text)
    {
        var dash = text.IndexOf(" - ", StringComparison.Ordinal);
        var pipe = text.IndexOfAny(PipeLike);
        if (dash > 0 && (pipe < 0 || dash < pipe))
        {
            yield return (text[..dash].Trim(), text[(dash + 3)..].Trim(), false);
        }
        else if (pipe > 0)
        {
            yield return (text[..pipe].Trim(), text[(pipe + 1)..].Trim(), true);
        }
    }

    private bool IsUntrusted(string app)
    {
        var key = TextKey.Of(app);
        return key.Length > 0 && _rules.UntrustedApps.Any(a => key.Contains(TextKey.Of(a), StringComparison.Ordinal));
    }

    private string PrepareTitle(string raw)
    {
        var text = raw.Trim();
        text = _extension.Replace(text, string.Empty);
        text = Dashes().Replace(text, " - ");
        text = TrackNumber().Replace(text, string.Empty);
        text = Underscores().Replace(text, " ");
        return Collapse(text);
    }

    /// <summary>Retire les groupes « (…) », « […] », « {…} » parasites ; extrait invités et versions ; garde les autres.</summary>
    private string StripGroups(string text, TitleParts parts)
    {
        string result;
        do
        {
            result = text;
            text = Groups().Replace(
                text,
                match =>
                {
                    var inner = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                    return Classify(inner, parts) ? string.Empty : match.Value;
                });
        }
        while (text != result);

        return Collapse(text);
    }

    /// <summary>Retire les derniers segments « - Remastered 2011 », « | Official Video », « - Live at… » ; garde le reste.</summary>
    private string StripTrailingSegments(string text, TitleParts parts)
    {
        while (true)
        {
            var dash = text.LastIndexOf(" - ", StringComparison.Ordinal);
            var pipe = text.LastIndexOfAny(PipeLike);
            var index = Math.Max(dash, pipe);
            if (index <= 0)
            {
                break;
            }

            var length = index == dash ? 3 : 1;
            var segment = text[(index + length)..].Trim();
            if (segment.Length == 0)
            {
                text = text[..index].Trim();
                continue;
            }

            if (!Classify(segment, parts))
            {
                break;
            }

            text = text[..index].Trim();
        }

        return text;
    }

    /// <summary>Classe un morceau de texte : vrai s'il est retiré du titre (parasite, invité ou version, ces deux derniers étant notés).</summary>
    private bool Classify(string inner, TitleParts parts)
    {
        var key = TextKey.Of(inner);
        if (key.Length == 0)
        {
            return true;
        }

        var words = TextKey.Words(key);
        var noiseOnly = words.All(w => _noiseWords.Contains(w) || w.All(char.IsDigit));
        if (noiseOnly || _noisePhrases.Contains(key) || _noisePrefixes.Any(p => key == p || key.StartsWith(p + " ", StringComparison.Ordinal))
            || words.Any(w => w.StartsWith("remaster", StringComparison.Ordinal)))
        {
            return true;
        }

        if (_guestGroup.Match(inner.Trim()) is { Success: true } guest)
        {
            parts.Guests.AddRange(SplitNames(guest.Groups[1].Value));
            return true;
        }

        if (words.Any(_versionWords.Contains))
        {
            parts.Versions.Add(key);
            return true;
        }

        return false;
    }

    private ArtistCredit ParseArtist(string raw)
    {
        var text = raw.Trim();
        if (text.Length == 0)
        {
            return ArtistCredit.None;
        }

        text = Dashes().Replace(text, " - ");
        var isTopic = TopicSuffix().IsMatch(text);
        var cleaned = text;
        string previous;
        do
        {
            previous = cleaned;
            cleaned = _artistSuffix.Replace(cleaned, string.Empty);
        }
        while (cleaned != previous);

        cleaned = Collapse(OfficialMark().Replace(cleaned, string.Empty));
        var guests = new List<string>();
        if (_guestTail.Match(cleaned) is { Success: true } feat)
        {
            guests.AddRange(SplitNames(feat.Groups[1].Value));
            cleaned = cleaned[..feat.Index].Trim();
        }

        return new ArtistCredit(cleaned, isTopic, guests);
    }

    private void AddSplit(List<TrackHypothesis> hypotheses, string left, string right, bool pipe, ArtistCredit channel, TitleParts parts, string rawTitle)
    {
        if (left.Length == 0 || right.Length == 0)
        {
            return;
        }

        if (!pipe)
        {
            hypotheses.Add(BuildSplit(left, right, channel, parts));
            return;
        }

        // « Titre | Artiste » ou « Artiste | Titre » : si le nom de la chaîne figure entre parenthèses dans la partie de gauche du
        // titre brut (« (Kokwak Hardstyle Remix) » : la chaîne a fait ce remix), c'est le titre qui est à gauche ; sinon on suppose
        // « Artiste | Titre ».
        var rawPipe = rawTitle.IndexOfAny(PipeLike);
        var titleOnLeft = channel.Display.Length > 0 && rawPipe > 0
            && Groups().Matches(rawTitle[..rawPipe]).Any(m => m.Value.Contains(channel.Display, StringComparison.OrdinalIgnoreCase));
        if (titleOnLeft)
        {
            hypotheses.Add(BuildSplit(right, left, channel, parts));
            hypotheses.Add(BuildSplit(left, right, channel, parts));
        }
        else
        {
            hypotheses.Add(BuildSplit(left, right, channel, parts));
            hypotheses.Add(BuildSplit(right, left, channel, parts));
        }
    }

    /// <summary>Hypothèse « artiste = <paramref name="artistText"/>, titre = <paramref name="titleText"/> ».</summary>
    private TrackHypothesis BuildSplit(string artistText, string titleText, ArtistCredit channel, TitleParts parts)
    {
        // Les groupes déjà extraits (versions, invités) appartiennent au titre entier : ils restent associés à cette hypothèse.
        var credit = ParseArtist(artistText);
        var own = new TitleParts();
        own.Guests.AddRange(parts.Guests);
        own.Versions.AddRange(parts.Versions);
        if (channel.Guests.Count > 0)
        {
            own.Guests.AddRange(channel.Guests);
        }

        // Les mentions restées dans la partie « titre » ou « artiste » après le découpage (« feat. X » en fin de titre) sont extraites ici.
        return Build(credit, titleText, own, HypothesisOrigin.TitleSplit);
    }

    private TrackHypothesis Build(ArtistCredit credit, string titleText, TitleParts parts, HypothesisOrigin origin)
    {
        var title = Collapse(titleText);
        var guests = new List<string>(parts.Guests);
        if (_guestTail.Match(title) is { Success: true } feat)
        {
            guests.AddRange(SplitNames(feat.Groups[1].Value));
            title = title[..feat.Index].Trim();
        }

        guests.AddRange(credit.Guests);
        var credits = SplitNames(credit.Display).Select(TextKey.Of).Where(k => k.Length > 0).ToList();
        return new TrackHypothesis(
            TextKey.Of(credit.Display),
            credit.Display,
            credits,
            [.. guests.Select(TextKey.Of).Where(k => k.Length > 0).Distinct()],
            TextKey.Of(title),
            title,
            [.. parts.Versions.Distinct()],
            origin);
    }

    private sealed class TitleParts
    {
        public List<string> Guests { get; } = [];

        public List<string> Versions { get; } = [];
    }

    private sealed record ArtistCredit(string Display, bool IsTopic, List<string> Guests)
    {
        public static ArtistCredit None { get; } = new(string.Empty, false, []);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex CollapseSpaces();

    [GeneratedRegex(@"\s*[–—‒―−]\s*")]
    private static partial Regex Dashes();

    [GeneratedRegex(@"^\s*\d{1,3}\s*[-._)]\s+(?=\p{L})")]
    private static partial Regex TrackNumber();

    [GeneratedRegex(@"_+")]
    private static partial Regex Underscores();

    [GeneratedRegex(@"\(([^()]*)\)|\[([^\[\]]*)\]|\{([^{}]*)\}")]
    private static partial Regex Groups();

    // Mentions parasites laissées en fin de titre, sans parenthèses (« Believer (Official Video) HD »).
    [GeneratedRegex(@"(?:\s+(?:hd|hq|4k|1080p|720p|lyrics?(?:\s+video)?|paroles|(?:official|officiel)(?:\s+(?:music|lyric|audio|video|clip|vidéo|musique))*))+$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingNoise();

    [GeneratedRegex(@"\s*,\s*|\s*;\s*|\s+&\s+|\s*\+\s*|\s+[xX]\s+")]
    private static partial Regex NameSeparators();

    [GeneratedRegex(@"-\s*topic\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex TopicSuffix();

    [GeneratedRegex(@"\(\s*official\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex OfficialMark();
}
