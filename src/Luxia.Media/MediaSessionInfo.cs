namespace Luxia.Media;

/// <summary>État de lecture d'une session média.</summary>
public enum MediaPlayback
{
    /// <summary>Arrêtée, fermée ou en transition.</summary>
    Stopped,

    /// <summary>En pause.</summary>
    Paused,

    /// <summary>En lecture.</summary>
    Playing,
}

/// <summary>
/// Une session média du système (MUS-001) : une application qui joue ou a joué de la musique (Deezer, YouTube Music dans un
/// navigateur, VLC…), telle que Windows la décrit.
/// </summary>
/// <param name="Id">Identifiant de la session (application source).</param>
/// <param name="App">Nom lisible de l'application source.</param>
/// <param name="Title">Titre ; vide si le lecteur n'en donne pas.</param>
/// <param name="Artist">Artiste, ou nom de la chaîne YouTube ; vide si inconnu.</param>
/// <param name="Album">Album ; vide si inconnu.</param>
/// <param name="Playback">État de lecture.</param>
/// <param name="Position">Position dans le morceau lors de la dernière mise à jour du lecteur ; <c>null</c> si le lecteur n'en donne pas.</param>
/// <param name="PositionUpdatedUtc">Instant de cette mise à jour ; <c>null</c> si inconnu.</param>
/// <param name="Duration">Durée du morceau ; zéro si inconnue.</param>
/// <param name="Rate">Vitesse de lecture (1 = normale).</param>
/// <param name="Genres">Genres donnés par le lecteur, séparés par « ; » (souvent vide).</param>
public sealed record MediaSessionInfo(
    string Id,
    string App,
    string Title,
    string Artist,
    string Album,
    MediaPlayback Playback,
    TimeSpan? Position,
    DateTimeOffset? PositionUpdatedUtc,
    TimeSpan Duration,
    double Rate = 1.0,
    string Genres = "");
