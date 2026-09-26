using Luxia.Messaging.Commands;

namespace Luxia.Messaging.Events;

/// <summary>EVT-011 <c>CommandeRefusée</c> (GEN-012, MOT-101) : la commande n'a pas été appliquée, pour le motif donné.</summary>
/// <param name="Command">Commande refusée.</param>
/// <param name="Reason">Motif, en français.</param>
/// <param name="At">Instant (horloge du moteur).</param>
public sealed record CommandRejected(Command Command, string Reason, TimeSpan At);
