namespace Luxia.Messaging.Commands;

/// <summary>CMD-002 <c>RéglerGrandMaster</c> : multiplie toutes les intensités (MOT-071).</summary>
/// <param name="Origin">Origine.</param>
/// <param name="Level">Niveau 0 à 1 (borné).</param>
public sealed record SetGrandMasterCommand(CommandOrigin Origin, double Level) : Command(Origin);
