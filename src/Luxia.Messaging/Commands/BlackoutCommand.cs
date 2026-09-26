namespace Luxia.Messaging.Commands;

/// <summary>
/// CMD-001 <c>Blackout</c> : toutes les intensités à 0 au tick suivant, les autres attributs inchangés (MOT-070, GEN-041) ;
/// la désactivation restaure instantanément.
/// </summary>
/// <param name="Origin">Origine.</param>
/// <param name="Active">Blackout actif.</param>
public sealed record BlackoutCommand(CommandOrigin Origin, bool Active) : Command(Origin);
