namespace Luxia.Messaging.Commands;

/// <summary>
/// CMD-012 <c>ArrêterCouche</c> : arrête toutes les scènes d'une couche, ou de toutes les couches (« Tout arrêter »).
/// </summary>
/// <param name="Origin">Origine.</param>
/// <param name="LayerId">Couche ; <c>null</c> = toutes.</param>
/// <param name="Fade">Fondu de sortie imposé ; <c>null</c> = fondu de sortie de chaque scène.</param>
public sealed record StopLayerCommand(CommandOrigin Origin, Guid? LayerId = null, TimeSpan? Fade = null) : Command(Origin);
