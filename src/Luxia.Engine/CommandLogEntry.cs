using Luxia.Messaging.Commands;

namespace Luxia.Engine;

/// <summary>Entrée du journal des commandes (GEN-112, GEN-010).</summary>
/// <param name="ReceivedAt">Réception de la commande (horloge du moteur).</param>
/// <param name="AppliedAt">Application, au tick suivant.</param>
/// <param name="Command">Commande, avec son origine.</param>
/// <param name="Rejection">Motif de refus, <c>null</c> si appliquée (GEN-012).</param>
/// <param name="Repeat">Nombre de commandes identiques consécutives regroupées (glissé de fader : une entrée, pas vingt par seconde).</param>
public readonly record struct CommandLogEntry(TimeSpan ReceivedAt, TimeSpan AppliedAt, Command Command, string? Rejection, int Repeat = 1);
