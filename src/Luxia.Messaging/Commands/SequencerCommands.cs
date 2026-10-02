namespace Luxia.Messaging.Commands;

/// <summary>
/// Commande destinée au séquenceur de show (doc 20, D37) : le moteur la lui transmet dans l'ordre des autres commandes, au tick
/// suivant sa réception ; sans séquenceur branché, elle est refusée.
/// </summary>
/// <param name="Origin">Origine.</param>
public abstract record SequencerCommand(CommandOrigin Origin) : Command(Origin);

/// <summary>
/// CMD-050 <c>LancerShow</c> : lance un show (un seul show principal à la fois : il remplace le précédent, SHOW-025) ; un show
/// qui joue déjà continue.
/// </summary>
/// <param name="Origin">Origine.</param>
/// <param name="ShowId">Show.</param>
/// <param name="StopIfPlaying">Bascule (bouton de la colonne « Shows ») : arrête le show s'il joue déjà, tranché par le moteur.</param>
public sealed record LaunchShowCommand(CommandOrigin Origin, Guid ShowId, bool StopIfPlaying = false) : SequencerCommand(Origin);

/// <summary>CMD-050 <c>ArrêterShow</c> : arrête un show, ou tous (<paramref name="ShowId"/> nul), avec le fondu de ses scènes.</summary>
/// <param name="Origin">Origine.</param>
/// <param name="ShowId">Show ; <c>null</c> = tous les shows et toutes les séquences.</param>
/// <param name="KeepSecondary">Avec <paramref name="ShowId"/> nul : les shows secondaires continuent (« ■ Stop », qui épargne aussi les
/// couches protégées).</param>
public sealed record StopShowCommand(CommandOrigin Origin, Guid? ShowId = null, bool KeepSecondary = false) : SequencerCommand(Origin);

/// <summary>
/// CMD-051 <c>ForcerTransition</c> : franchit une transition d'un show qui joue, comme si sa condition était vraie (à sa
/// quantification, sauf <paramref name="Immediate"/>) ; refusée si ses étapes amont ne sont pas toutes actives.
/// </summary>
/// <param name="Origin">Origine.</param>
/// <param name="ShowId">Show.</param>
/// <param name="Transition">Rang de la transition dans le show (0 = la première).</param>
/// <param name="Immediate">Sans attendre la frontière musicale.</param>
public sealed record ForceTransitionCommand(CommandOrigin Origin, Guid ShowId, int Transition, bool Immediate = false) : SequencerCommand(Origin);

/// <summary>CMD-052 <c>LancerSéquence</c> : lance une séquence (à sa quantification, SHOW-005, SHOW-006).</summary>
/// <param name="Origin">Origine.</param>
/// <param name="SequenceId">Séquence.</param>
/// <param name="StopIfPlaying">Bascule : arrête la séquence si elle joue déjà.</param>
public sealed record LaunchSequenceCommand(CommandOrigin Origin, Guid SequenceId, bool StopIfPlaying = false) : SequencerCommand(Origin);

/// <summary>CMD-052 <c>ArrêterSéquence</c> : arrête une séquence et les scènes qu'elle a lancées et qui jouent encore.</summary>
/// <param name="Origin">Origine.</param>
/// <param name="SequenceId">Séquence.</param>
public sealed record StopSequenceCommand(CommandOrigin Origin, Guid SequenceId) : SequencerCommand(Origin);
