namespace Luxia.Messaging.Commands;

/// <summary>CMD-013 <c>RéglerMasterCouche</c> : master d'une couche (MOT-033).</summary>
/// <param name="Origin">Origine.</param>
/// <param name="LayerId">Couche.</param>
/// <param name="Level">Niveau 0 à 1 (borné).</param>
public sealed record SetLayerMasterCommand(CommandOrigin Origin, Guid LayerId, double Level) : Command(Origin);
