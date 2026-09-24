namespace Dmx.Messaging.Commands;

/// <summary>
/// Intention envoyée au moteur (doc 02 §6). Seule façon d'agir sur la restitution (P3, GEN-002).
/// </summary>
/// <param name="Origin">Qui émet la commande (journalisation, priorités manuel / automatique).</param>
public abstract record Command(CommandOrigin Origin);
