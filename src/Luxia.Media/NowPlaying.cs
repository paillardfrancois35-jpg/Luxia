namespace Luxia.Media;

/// <summary>Le morceau en cours, tel que le lecteur le décrit (MUS-003).</summary>
/// <param name="Title">Titre brut.</param>
/// <param name="Artist">Artiste brut.</param>
/// <param name="Album">Album brut.</param>
/// <param name="App">Application source.</param>
/// <param name="Duration">Durée ; zéro si inconnue.</param>
/// <param name="Genres">Genres donnés par le lecteur, séparés par « ; » (souvent vide).</param>
public sealed record NowPlayingTrack(string Title, string Artist, string Album, string App, TimeSpan Duration, string Genres = "");

/// <summary>État de la lecture en cours à un instant donné (MUS-003).</summary>
/// <param name="Track">Morceau stabilisé ; <c>null</c> si aucune session ne fournit de titre (MUS-006).</param>
/// <param name="Playing">Une lecture est en cours (sinon pause ou arrêt).</param>
/// <param name="Position">Position estimée dans le morceau ; <c>null</c> si le lecteur n'en donne pas.</param>
public sealed record NowPlayingState(NowPlayingTrack? Track, bool Playing, TimeSpan? Position)
{
    /// <summary>Aucune lecture.</summary>
    public static NowPlayingState None { get; } = new(null, false, null);
}

/// <summary>Changement de morceau (MUS-002).</summary>
/// <param name="Track">Nouveau morceau ; <c>null</c> si plus aucune session ne fournit de titre.</param>
/// <param name="Previous">Morceau précédent ; <c>null</c> au premier.</param>
public sealed record TrackChange(NowPlayingTrack? Track, NowPlayingTrack? Previous);
