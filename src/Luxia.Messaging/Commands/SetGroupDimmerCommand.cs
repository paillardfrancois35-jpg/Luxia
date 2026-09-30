namespace Luxia.Messaging.Commands;

/// <summary>
/// CMD-031 <c>RéglerDimmerGroupe</c> : niveau du dimmer d'un groupe d'appareils (ERG-037). Le moteur multiplie les
/// intensités des appareils du groupe, après la fusion des couches, par le niveau de chaque étage de l'arbre.
/// </summary>
/// <param name="Origin">Origine.</param>
/// <param name="GroupId">Groupe (il doit avoir un dimmer).</param>
/// <param name="Level">Niveau 0 à 1 (borné).</param>
public sealed record SetGroupDimmerCommand(CommandOrigin Origin, Guid GroupId, double Level) : Command(Origin);
