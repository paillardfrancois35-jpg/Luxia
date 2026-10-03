using Luxia.Music.Base;
using Luxia.Music.Normalization;

namespace Luxia.Music.Identification;

/// <summary>Comment un style a été trouvé (chaîne du doc 21 §3.3), publié avec la confiance dans <c>StyleDétecté</c> (EVT-042).</summary>
public enum IdentificationMethod
{
    /// <summary>Rien trouvé : famille « Inconnu ».</summary>
    None,

    /// <summary>Style corrigé à la main sur le morceau en cours (confiance 1) ; rejoué plus tard, il est retrouvé comme n'importe quel titre ou artiste connu.</summary>
    Correction,

    /// <summary>Titre exact connu (0,95).</summary>
    ExactTitle,

    /// <summary>Titre approché d'un artiste connu (0,7 à 0,9 selon le score).</summary>
    FuzzyTitle,

    /// <summary>Artiste exact ou alias connu (0,8).</summary>
    ExactArtist,

    /// <summary>Artiste approché (0,5 à 0,75 selon le score).</summary>
    FuzzyArtist,

    /// <summary>Artiste invité connu (0,5).</summary>
    Guest,

    /// <summary>Genre fourni par le lecteur, par la taxonomie (0,4).</summary>
    PlayerGenre,

    /// <summary>Style imposé à la main (CMD-062, confiance 1).</summary>
    Forced,
}

/// <summary>Résultat de l'identification d'un morceau.</summary>
/// <param name="FamilyId">Identifiant de la famille ; <see cref="Taxonomy.UnknownId"/> si rien n'est trouvé.</param>
/// <param name="FamilyName">Nom de la famille.</param>
/// <param name="Confidence">Confiance de 0 à 1.</param>
/// <param name="Method">Méthode.</param>
/// <param name="Detail">Texte court pour le journal et l'écran (« artiste Queen », « titre exact »).</param>
/// <param name="Hypothesis">Rang de la lecture du titre brut qui a donné ce résultat (0 = la plus probable).</param>
public sealed record StyleResult(string FamilyId, string FamilyName, double Confidence, IdentificationMethod Method, string Detail, int Hypothesis = 0)
{
    /// <summary>Un style a été trouvé.</summary>
    public bool IsKnown => FamilyId != Taxonomy.UnknownId;

    /// <summary>Résultat « inconnu ».</summary>
    public static StyleResult Unknown { get; } = new(Taxonomy.UnknownId, Taxonomy.UnknownName, 0, IdentificationMethod.None, "non identifié");
}

/// <summary>Réglages de l'identification.</summary>
public sealed record IdentifierOptions
{
    /// <summary>Score minimal d'un rapprochement flou (0 à 1) : plus bas, plus tolérant, plus de faux positifs.</summary>
    public double FuzzyThreshold { get; init; } = 0.8;
}

/// <summary>
/// Chaîne d'identification du style (doc 21 §3.3, MUS-021) : titre exact, titre approché, artiste exact ou alias, artiste approché,
/// artistes invités, genre du lecteur, sinon inconnu. Un titre dont le style est vide suit celui de son artiste. Plusieurs lectures du titre
/// brut sont essayées (normalisation) ; la plus sûre l'emporte. Entièrement locale, sans réseau (MUS-022 : moins de 200 ms).
/// </summary>
public sealed class StyleIdentifier
{
    private readonly MusicBase _base;
    private readonly IdentifierOptions _options;

    /// <summary>Crée l'identification.</summary>
    /// <param name="musicBase">Base musicale.</param>
    /// <param name="options">Réglages ; ceux par défaut.</param>
    public StyleIdentifier(MusicBase musicBase, IdentifierOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(musicBase);
        _base = musicBase;
        _options = options ?? new IdentifierOptions();
    }

    /// <summary>Identifie le style d'un titre normalisé.</summary>
    /// <param name="track">Titre normalisé.</param>
    /// <param name="genres">Genres fournis par le lecteur, séparés par « ; » (souvent vides).</param>
    /// <returns>Le style trouvé, ou « Inconnu ».</returns>
    public StyleResult Identify(NormalizedTrack track, string genres = "")
    {
        ArgumentNullException.ThrowIfNull(track);
        StyleResult? best = null;
        for (var i = 0; i < track.Hypotheses.Count; i++)
        {
            var result = Evaluate(track.Hypotheses[i]);
            if (result is not null && (best is null || result.Confidence > best.Confidence))
            {
                best = result with { Hypothesis = i };
            }
        }

        if (best is not null && best.Confidence >= 0.5)
        {
            return best;
        }

        // Genre donné par le lecteur : en dernier recours seulement (0,4), il ne bat jamais un artiste connu.
        var fromGenre = FromGenres(genres);
        if (fromGenre is not null && (best is null || fromGenre.Confidence > best.Confidence))
        {
            return fromGenre;
        }

        return best ?? StyleResult.Unknown;
    }

    private StyleResult? Known(string familyId, double confidence, IdentificationMethod method, string detail)
    {
        // « Inconnu » n'est pas un résultat : un artiste injecté faute d'être connu ne doit pas masquer les repères suivants (genre du lecteur).
        var family = familyId == Taxonomy.UnknownId ? null : _base.FamilyById(familyId);
        return family is null ? null : new StyleResult(family.Id, family.Name, confidence, method, detail);
    }

    private StyleResult? Evaluate(TrackHypothesis h)
    {
        var versionKey = string.Join(' ', h.Versions);
        var threshold = _options.FuzzyThreshold;

        StyleResult? result = null;

        // 1. Titre exact connu (0,95) ; une version peut avoir son propre style, sinon on retombe sur l'original (0,85).
        if (h.Artist.Length > 0 && h.Title.Length > 0)
        {
            if (ExactTitle(h, versionKey) is { } exact && Known(exact.Style, 0.95, IdentificationMethod.ExactTitle, $"titre « {exact.Title} »") is { } t1)
            {
                return t1;
            }

            if (versionKey.Length > 0 && _base.FindTitle(h.Artist, h.Title, string.Empty) is { } original
                && Known(original.Style, 0.85, IdentificationMethod.ExactTitle, $"titre « {original.Title} » (version non connue)") is { } t1b)
            {
                result = t1b;
            }

            // 2. Titre approché d'un artiste connu (0,7 à 0,9).
            foreach (var (title, score) in _base.FuzzyTitles(h.Artist, h.Title, versionKey, threshold).Take(1))
            {
                var confidence = Scale(score, threshold, 0.7, 0.9);
                if (Known(title.Style, confidence, IdentificationMethod.FuzzyTitle, $"titre proche « {title.Title} »") is { } t2 && (result is null || t2.Confidence > result.Confidence))
                {
                    result = t2;
                }
            }

            if (result is not null)
            {
                return result;
            }
        }

        // 3. Artiste exact ou alias (0,8), puis ses autres crédits (0,7).
        if (h.Artist.Length > 0)
        {
            if (_base.FindArtist(h.Artist) is { } artist && FromArtist(artist, 0.8, IdentificationMethod.ExactArtist) is { } a1)
            {
                return a1;
            }

            foreach (var credit in h.Credits.Skip(h.Credits.Count > 1 ? 0 : 1))
            {
                if (_base.FindArtist(credit) is { } coArtist && FromArtist(coArtist, 0.7, IdentificationMethod.ExactArtist) is { } a1b)
                {
                    return a1b;
                }
            }

            // 4. Artiste approché (0,5 à 0,75).
            foreach (var (artistMatch, score) in _base.FuzzyArtists(h.Artist, threshold).Take(1))
            {
                if (FromArtist(artistMatch, Scale(score, threshold, 0.5, 0.75), IdentificationMethod.FuzzyArtist) is { } a2)
                {
                    return a2;
                }
            }
        }

        // 5. Artistes invités connus (0,5).
        foreach (var guest in h.Guests)
        {
            if (_base.FindArtist(guest) is { } guestArtist && FromArtist(guestArtist, 0.5, IdentificationMethod.Guest) is { } g)
            {
                return g;
            }
        }

        return null;
    }

    private TitleEntry? ExactTitle(TrackHypothesis h, string versionKey) => _base.FindTitle(h.Artist, h.Title, versionKey);

    private StyleResult? FromArtist(ArtistEntry artist, double confidence, IdentificationMethod method) =>
        Known(artist.Style, confidence, method, $"artiste {artist.Name}");

    private StyleResult? FromGenres(string genres)
    {
        foreach (var genre in genres.Split([';', ','], StringSplitOptions.RemoveEmptyEntries))
        {
            if (_base.FindFamily(genre) is { } family)
            {
                return new StyleResult(family.Id, family.Name, 0.4, IdentificationMethod.PlayerGenre, $"genre « {genre.Trim()} »");
            }
        }

        return null;
    }

    private static double Scale(double score, double threshold, double low, double high)
    {
        var span = 1 - threshold;
        var ratio = span <= 0 ? 1 : Math.Clamp((score - threshold) / span, 0, 1);
        return low + ((high - low) * ratio);
    }
}
