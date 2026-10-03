using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.Hosting;
using Luxia.Music.Base;
using Luxia.Music.Identification;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>
/// Bloc « Morceau en cours » de l'écran de jeu (P9, LIVE-022, Q50) : le titre et l'artiste que le lecteur annonce, l'application, la
/// position, le style identifié avec sa confiance (vert : sûr, orange : moyen, rouge : incertain, gris : inconnu), « imposé » quand le
/// style a été forcé à la main. Trois actions : <b>Corriger</b> le style (ce titre ou cet artiste, mémorisé : MUS-024), <b>Imposer</b>
/// un style jusqu'à la fin du morceau (CMD-062, MUS-026) et <b>Saisir</b> le morceau à la main quand aucun lecteur ne l'annonce
/// (MUS-007). Ne fait que lire l'état du service de style et lui donner des ordres.
/// </summary>
public sealed partial class NowPlayingBarViewModel : ViewModelBase
{
    private const string Green = "#3FB950";
    private const string Orange = "#D29922";
    private const string Red = "#F85149";
    private const string Gray = "#8B949E";

    private readonly LuxiaRuntime _runtime;
    private readonly IDialogService? _dialogs;
    private readonly JournalPanelViewModel _journal;

    [ObservableProperty]
    private bool _hasTrack;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _artist = string.Empty;

    [ObservableProperty]
    private string _source = string.Empty;

    [ObservableProperty]
    private string _position = string.Empty;

    [ObservableProperty]
    private string _playIcon = "♫";

    [ObservableProperty]
    private string _style = string.Empty;

    [ObservableProperty]
    private string _confidence = string.Empty;

    [ObservableProperty]
    private string _styleColor = Gray;

    [ObservableProperty]
    private string _styleTip = "Aucun style : aucun morceau n'est annoncé par un lecteur";

    [ObservableProperty]
    private bool _isForced;

    [ObservableProperty]
    private string _emptyText = string.Empty;

    /// <summary>Crée le bloc sur le moteur en service.</summary>
    public NowPlayingBarViewModel(LuxiaRuntime runtime, JournalPanelViewModel journal, IDialogService? dialogs = null)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(journal);
        _runtime = runtime;
        _dialogs = dialogs;
        _journal = journal;
        Refresh();
    }

    /// <summary>Un morceau est annoncé par la lecture en cours de Windows (sinon, il faut le saisir ou lancer un lecteur).</summary>
    public bool CanFollow => _runtime.NowPlaying is not null;

    /// <summary>Le bloc propose de corriger ou d'imposer le style (il y a un morceau).</summary>
    public bool CanAct => HasTrack;

    /// <summary>Familles de styles pour les menus (charge la base musicale du projet au premier appel).</summary>
    public IReadOnlyList<MusicFamily> Families => [.. _runtime.Music.Families.Where(f => f.Id != Luxia.Music.Base.Taxonomy.UnknownId)];

    /// <summary>Relit l'état de la lecture en cours et du style (appelé par le rafraîchissement de l'écran, 20 fois par seconde).</summary>
    public void Refresh()
    {
        var state = _runtime.Music.State;
        var now = _runtime.NowPlaying?.Current;
        HasTrack = state.HasTrack;
        OnPropertyChanged(nameof(CanAct));
        if (!state.HasTrack)
        {
            Title = string.Empty;
            Artist = string.Empty;
            Source = string.Empty;
            Position = string.Empty;
            Style = string.Empty;
            Confidence = string.Empty;
            StyleColor = Gray;
            IsForced = false;
            PlayIcon = "♫";
            StyleTip = "Aucun style : aucun morceau n'est annoncé par un lecteur";
            EmptyText = CanFollow
                ? "Aucun morceau : lancez Deezer ou YouTube Music (le titre s'affiche ici), ou « Saisir… »"
                : "Lecture en cours de Windows indisponible : « Saisir… » le morceau à la main";
            return;
        }

        EmptyText = string.Empty;
        var hypothesis = state.Normalized?.Hypotheses[Math.Min(state.Detected.Hypothesis, (state.Normalized?.Hypotheses.Count ?? 1) - 1)];
        Title = hypothesis is { TitleDisplay.Length: > 0 } ? hypothesis.TitleDisplay : state.Title;
        Artist = hypothesis is { ArtistDisplay.Length: > 0 } ? hypothesis.ArtistDisplay : state.Artist;
        Source = state.App;
        PlayIcon = now is { Track: not null } && now.Playing ? "▶" : now is { Track: not null } ? "⏸" : "♫";
        Position = now is { Track: { } track, Position: { } position } ? Format(position, track.Duration) : string.Empty;
        IsForced = state.Forced;
        var effective = state.Effective;
        Style = effective.FamilyName;
        Confidence = effective.IsKnown ? string.Create(CultureInfo.CurrentCulture, $"{effective.Confidence:P0}") : string.Empty;
        StyleColor = !effective.IsKnown ? Gray : effective.Confidence >= 0.8 ? Green : effective.Confidence >= 0.5 ? Orange : Red;
        StyleTip = state.Forced
            ? $"Style imposé à la main : « {effective.FamilyName} ». Détecté : {state.Detected.FamilyName} ({state.Detected.Detail}). Il s'arrête avec le morceau."
            : effective.IsKnown
                ? $"Style détecté : {effective.FamilyName}, confiance {effective.Confidence:P0} ({effective.Detail}). « Corriger ▾ » pour le changer."
                : "Style inconnu : le show suit l'énergie seule. « Corriger ▾ » pour l'apprendre à LuXia.";
    }

    /// <summary>
    /// Comme <see cref="Correct"/>, mais quand la correction vise « ce titre » d'un artiste encore « Inconnu », demande d'abord s'il faut
    /// appliquer ce style à l'artiste (Oui) ou à ce titre seulement (Non) : classer un titre d'un inconnu, c'est souvent classer l'artiste.
    /// </summary>
    /// <param name="request">« title:identifiant » ou « artist:identifiant ».</param>
    /// <returns>Une tâche terminée quand la correction est faite (ou abandonnée).</returns>
    public async Task CorrectAsync(string request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_dialogs is not null && request.StartsWith("title:", StringComparison.Ordinal) && Artist.Length > 0 && IsUnclassified(Artist)
            && await _dialogs.ConfirmAsync("Artiste pas encore classé", $"« {Artist} » n'a pas encore de style dans la base.\n\nAppliquer ce style à l'artiste (tous ses titres) ?\nOui : l'artiste.  Non : ce titre seulement.").ConfigureAwait(true))
        {
            request = "artist:" + request["title:".Length..];
        }

        Correct(request);
    }

    private bool IsUnclassified(string artist) =>
        _runtime.Music.Base.OwnerOf(artist) is not { } owner || owner.Style == Luxia.Music.Base.Taxonomy.UnknownId;

    /// <summary>Corrige le style du morceau en cours (MUS-024).</summary>
    /// <param name="request">« title:identifiant » (ce titre) ou « artist:identifiant » (cet artiste).</param>
    public void Correct(string request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parts = request.Split(':', 2);
        if (parts.Length != 2)
        {
            return;
        }

        var scope = parts[0] == "title" ? CorrectionScope.Title : CorrectionScope.Artist;
        var error = _runtime.Music.Correct(scope, parts[1]);
        var state = _runtime.Music.State;
        _journal.Log(error is null
            ? $"♪ style corrigé : « {state.Title} » → {state.Effective.FamilyName} ({(scope == CorrectionScope.Title ? "ce titre" : "cet artiste")}, mémorisé)"
            : $"✕ correction du style : {error}");
        Refresh();
    }

    /// <summary>Impose un style jusqu'à la fin du morceau (CMD-062) ; <c>null</c> = retour à la détection.</summary>
    /// <param name="familyId">Famille.</param>
    public void Force(string? familyId)
    {
        if (!_runtime.Music.Force(familyId))
        {
            _journal.Log($"✕ style inconnu : « {familyId} »");
            return;
        }

        var state = _runtime.Music.State;
        _journal.Log(string.IsNullOrWhiteSpace(familyId) ? "♪ style : retour à la détection" : $"♪ style imposé : {state.Effective.FamilyName}");
        Refresh();
    }

    /// <summary>Saisit le morceau à la main quand aucun lecteur ne l'annonce (MUS-007).</summary>
    /// <param name="title">Titre.</param>
    /// <param name="artist">Artiste.</param>
    public void SetManual(string? title, string? artist)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        _runtime.Music.SetManualTrack(title.Trim(), artist?.Trim() ?? string.Empty);
        Refresh();
    }

    private static string Format(TimeSpan position, TimeSpan duration)
    {
        static string One(TimeSpan span) => span.TotalHours >= 1 ? span.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture) : span.ToString(@"m\:ss", CultureInfo.InvariantCulture);
        return duration > TimeSpan.Zero ? $"{One(position)} / {One(duration)}" : One(position);
    }
}
