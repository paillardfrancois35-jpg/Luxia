namespace Luxia.Messaging.Events;

/// <summary>
/// EVT-040 <c>MorceauChangé</c> : le morceau joué sur le PC change (lecture en cours de Windows, doc 21), une fois stabilisé
/// (MUS-002). Les textes sont ceux du lecteur, avant toute normalisation ; ils sont vides quand il n'y a plus de morceau.
/// </summary>
/// <param name="Title">Titre brut ; vide s'il n'y a plus de morceau.</param>
/// <param name="Artist">Artiste brut (ou nom de la chaîne YouTube).</param>
/// <param name="Album">Album brut.</param>
/// <param name="App">Application source (Deezer, VLC, navigateur…).</param>
/// <param name="Duration">Durée ; zéro si inconnue.</param>
/// <param name="At">Instant (horloge du moteur).</param>
public sealed record TrackChanged(string Title, string Artist, string Album, string App, TimeSpan Duration, TimeSpan At)
{
    /// <summary>Il y a un morceau (sinon plus aucune session média ne fournit de titre).</summary>
    public bool HasTrack => Title.Length > 0;
}

/// <summary>
/// EVT-042 <c>StyleDétecté</c> : le style du morceau en cours est trouvé, imposé ou corrigé (doc 21 §3, MUS-021). Reçu par le journal
/// de soirée, l'écran de jeu et, plus tard, le Directeur.
/// </summary>
/// <param name="FamilyId">Identifiant de la famille (« rock ») ; vide sans morceau.</param>
/// <param name="FamilyName">Nom de la famille (« Rock », « Inconnu ») ; vide sans morceau.</param>
/// <param name="Confidence">Confiance de 0 à 1.</param>
/// <param name="Method">Méthode : <c>correction</c>, <c>titre exact</c>, <c>titre approché</c>, <c>artiste</c>, <c>artiste approché</c>, <c>invité</c>, <c>genre du lecteur</c>, <c>imposé</c>, <c>aucune</c>.</param>
/// <param name="Forced">Le style a été imposé à la main.</param>
/// <param name="Title">Titre brut du morceau.</param>
/// <param name="Artist">Artiste brut du morceau.</param>
/// <param name="App">Application source (« Deezer », « Chrome », « saisie manuelle »).</param>
/// <param name="At">Instant (horloge du moteur).</param>
public sealed record StyleDetected(string FamilyId, string FamilyName, double Confidence, string Method, bool Forced, string Title, string Artist, string App, TimeSpan At);

/// <summary>EVT-041 <c>LectureDémarrée</c> / <c>LectureEnPause</c> : la lecture du morceau suivi démarre ou s'arrête.</summary>
/// <param name="Playing">La lecture est en cours (<c>true</c>) ; en pause ou arrêtée (<c>false</c>).</param>
/// <param name="At">Instant (horloge du moteur).</param>
public sealed record PlaybackChanged(bool Playing, TimeSpan At);
