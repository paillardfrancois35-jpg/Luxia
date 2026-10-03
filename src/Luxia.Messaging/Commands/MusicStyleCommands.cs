namespace Luxia.Messaging.Commands;

/// <summary>
/// CMD-062 <c>ForcerStyle</c> (MUS-026) : impose le style du morceau aux shows, quelle que soit la détection, jusqu'à son annulation
/// (ou la fin du morceau, selon le réglage de la session de style). Le style du mode simulation (CMD-053) reste prioritaire.
/// </summary>
/// <param name="Origin">Origine.</param>
/// <param name="Style">Nom de la famille (« Rock ») ; <c>null</c> ou vide = retour à la détection.</param>
public sealed record ForceStyleCommand(CommandOrigin Origin, string? Style) : Command(Origin);

/// <summary>
/// CMD-064 <c>FixerContexteMusical</c> (P9) : ce que la lecture en cours de Windows et l'identification du style apprennent au moteur :
/// le style détecté du morceau, un changement de morceau réel (qui devient la source de « au morceau suivant », Q49) et la présence
/// d'une lecture suivie (la détection de changement par l'écoute n'est alors plus utilisée).
/// </summary>
/// <param name="Origin">Origine.</param>
/// <param name="Style">Style détecté (nom de famille, « Inconnu ») ; <c>null</c> = aucun morceau.</param>
/// <param name="TrackChanged">Le morceau vient de changer : lève « au morceau suivant » au tick suivant.</param>
/// <param name="MediaActive">Un morceau est suivi par la lecture en cours de Windows (en lecture).</param>
public sealed record SetMusicContextCommand(CommandOrigin Origin, string? Style, bool TrackChanged = false, bool MediaActive = false) : Command(Origin);
