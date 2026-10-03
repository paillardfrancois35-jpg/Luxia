using Luxia.Music.Base;
using Luxia.Music.Normalization;

namespace Luxia.Music.Identification;

/// <summary>État du style du morceau en cours (doc 21 §3, LIVE-022).</summary>
/// <param name="HasTrack">Un morceau est connu (sinon : aucune lecture, MUS-006).</param>
/// <param name="Title">Titre brut du lecteur.</param>
/// <param name="Artist">Artiste brut du lecteur.</param>
/// <param name="App">Application source.</param>
/// <param name="Normalized">Titre normalisé ; <c>null</c> sans morceau.</param>
/// <param name="Detected">Style trouvé par la chaîne d'identification.</param>
/// <param name="Effective">Style retenu : le style imposé à la main (CMD-062) s'il y en a un, sinon le style trouvé.</param>
/// <param name="Forced">Le style retenu a été imposé à la main.</param>
/// <param name="Serial">Numéro du morceau : augmente à chaque nouveau morceau (ou saisie) et ne change pas quand seul le style change (imposé, corrigé).</param>
public sealed record StyleState(bool HasTrack, string Title, string Artist, string App, NormalizedTrack? Normalized, StyleResult Detected, StyleResult Effective, bool Forced, int Serial = 0)
{
    /// <summary>Aucun morceau.</summary>
    public static StyleState None { get; } = new(false, string.Empty, string.Empty, string.Empty, null, StyleResult.Unknown, StyleResult.Unknown, false);

    /// <summary>
    /// Valeur de style donnée aux shows : le nom de la famille (« Rock ») ; « Inconnu » si le morceau n'est pas identifié ;
    /// <c>null</c> sans morceau.
    /// </summary>
    public string? StyleName => HasTrack ? Effective.FamilyName : null;
}

/// <summary>Étendue d'une correction faite en Live (MUS-024).</summary>
public enum CorrectionScope
{
    /// <summary>Ce titre seulement.</summary>
    Title,

    /// <summary>Cet artiste (tous ses titres, sauf ceux corrigés à part).</summary>
    Artist,
}

/// <summary>
/// Le style du morceau en cours : normalise le titre brut, l'identifie, garde le style imposé à la main (CMD-062, MUS-026) et
/// applique les corrections faites en Live (MUS-024, prise en compte immédiate). Sans lien avec le moteur : l'hébergeur lit
/// <see cref="StyleChanged"/> et en tire les commandes.
/// </summary>
public sealed class StyleSession
{
    private readonly object _gate = new();
    private readonly TrackNormalizer _normalizer;
    private MusicBase _base;
    private StyleIdentifier _identifier;
    private StyleState _state = StyleState.None;
    private StyleResult? _forced;
    private string _genres = string.Empty;
    private int _serial;
    private bool _corrected;

    /// <summary>Crée la session.</summary>
    /// <param name="musicBase">Base musicale.</param>
    /// <param name="normalizer">Normaliseur de titres.</param>
    /// <param name="options">Réglages de l'identification.</param>
    public StyleSession(MusicBase musicBase, TrackNormalizer normalizer, IdentifierOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(musicBase);
        ArgumentNullException.ThrowIfNull(normalizer);
        _base = musicBase;
        _normalizer = normalizer;
        Options = options ?? new IdentifierOptions();
        _identifier = new StyleIdentifier(musicBase, Options);
    }

    /// <summary>Levé (sur le fil de l'appelant) à chaque changement d'état : nouveau morceau, style imposé, correction.</summary>
    public event EventHandler<StyleState>? StyleChanged;

    /// <summary>Réglages de l'identification.</summary>
    public IdentifierOptions Options { get; }

    /// <summary>Un style imposé s'arrête avec le morceau (réglable, MUS-026) ; sinon il dure jusqu'à son annulation.</summary>
    public bool ForceUntilTrackEnd { get; set; } = true;

    /// <summary>État courant.</summary>
    public StyleState State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    /// <summary>La base utilisée.</summary>
    public MusicBase Base
    {
        get
        {
            lock (_gate)
            {
                return _base;
            }
        }
    }

    /// <summary>Remplace la base (ouverture d'un autre projet) et ré-identifie le morceau en cours.</summary>
    /// <param name="musicBase">Nouvelle base.</param>
    public void ReplaceBase(MusicBase musicBase)
    {
        ArgumentNullException.ThrowIfNull(musicBase);
        StyleState state;
        lock (_gate)
        {
            _base = musicBase;
            _identifier = new StyleIdentifier(musicBase, Options);
            if (!_state.HasTrack)
            {
                return;
            }

            state = Recompute();
        }

        StyleChanged?.Invoke(this, state);
    }

    /// <summary>Un nouveau morceau joue : normalise, identifie, publie.</summary>
    /// <param name="title">Titre brut du lecteur.</param>
    /// <param name="artist">Artiste brut du lecteur.</param>
    /// <param name="app">Application source.</param>
    /// <param name="genres">Genres fournis par le lecteur, séparés par « ; ».</param>
    /// <returns>Le nouvel état.</returns>
    public StyleState Update(string? title, string? artist, string? app, string? genres = null)
    {
        StyleState state;
        lock (_gate)
        {
            if (ForceUntilTrackEnd)
            {
                _forced = null;
            }

            _genres = genres ?? string.Empty;
            _corrected = false;
            _serial++;
            var normalized = _normalizer.Normalize(title, artist, app);
            _state = Identify(normalized, title ?? string.Empty, artist ?? string.Empty, app ?? string.Empty);
            state = _state;
        }

        StyleChanged?.Invoke(this, state);
        return state;
    }

    /// <summary>Plus de morceau (aucune session) : le style est inconnu, un style imposé qui s'arrête avec le morceau est levé.</summary>
    /// <returns>Le nouvel état.</returns>
    public StyleState Clear()
    {
        StyleState state;
        lock (_gate)
        {
            if (ForceUntilTrackEnd)
            {
                _forced = null;
            }

            _corrected = false;
            _serial++;
            _state = StyleState.None with { Serial = _serial };
            state = _state;
        }

        StyleChanged?.Invoke(this, state);
        return state;
    }

    /// <summary>Impose un style (CMD-062) ; <c>null</c> ou vide = retour à la détection.</summary>
    /// <param name="familyIdOrName">Identifiant, nom ou partie du nom d'une famille (« Rock », « electro »).</param>
    /// <returns><c>false</c> si la famille est inconnue.</returns>
    public bool Force(string? familyIdOrName)
    {
        StyleState state;
        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(familyIdOrName))
            {
                _forced = null;
            }
            else
            {
                var family = _base.FindFamily(familyIdOrName);
                if (family is null)
                {
                    return false;
                }

                _forced = new StyleResult(family.Id, family.Name, 1.0, IdentificationMethod.Forced, "style imposé");
            }

            state = Recompute();
        }

        StyleChanged?.Invoke(this, state);
        return true;
    }

    /// <summary>
    /// Corrige le style du morceau en cours (MUS-024) : mémorisé dans la base et pris en compte immédiatement ; rejouer le titre
    /// (ou un autre titre du même artiste) donne le nouveau style.
    /// </summary>
    /// <param name="scope">Ce titre ou cet artiste.</param>
    /// <param name="familyIdOrName">Famille choisie.</param>
    /// <returns>Un message d'erreur, ou <c>null</c> si la correction est faite.</returns>
    public string? Correct(CorrectionScope scope, string familyIdOrName)
    {
        StyleState state;
        lock (_gate)
        {
            if (!_state.HasTrack || _state.Normalized is null)
            {
                return "aucun morceau en cours";
            }

            var family = _base.FindFamily(familyIdOrName);
            if (family is null)
            {
                return $"famille inconnue : « {familyIdOrName} »";
            }

            var hypothesis = _state.Normalized.Hypotheses[Math.Min(_state.Detected.Hypothesis, _state.Normalized.Hypotheses.Count - 1)];
            if (hypothesis.Artist.Length == 0 || hypothesis.ArtistDisplay.Length == 0)
            {
                return "artiste inconnu : la correction a besoin d'un artiste";
            }

            if (scope == CorrectionScope.Title)
            {
                _base.SetTitleStyle(hypothesis.ArtistDisplay, hypothesis.TitleDisplay, string.Join(' ', hypothesis.Versions), family.Id);
            }
            else
            {
                _base.SetArtistStyle(hypothesis.ArtistDisplay, family.Id);
            }

            _corrected = true;
            _forced = null;
            state = Recompute();
        }

        StyleChanged?.Invoke(this, state);
        return null;
    }

    private StyleState Recompute()
    {
        if (_state.Normalized is null)
        {
            return _state;
        }

        _state = Identify(_state.Normalized, _state.Title, _state.Artist, _state.App);
        return _state;
    }

    private StyleState Identify(NormalizedTrack normalized, string title, string artist, string app)
    {
        var detected = _identifier.Identify(normalized, _genres);
        if (_corrected && detected.IsKnown)
        {
            detected = detected with { Confidence = 1.0, Method = IdentificationMethod.Correction, Detail = "corrigé à la main" };
        }

        var effective = _forced ?? detected;
        return new StyleState(true, title, artist, app, normalized, detected, effective, _forced is not null, _serial);
    }
}
