namespace Luxia.Messaging.Commands;

/// <summary>Porte d'entrée unique des commandes (P3). Implémentée par le moteur.</summary>
public interface ICommandSink
{
    /// <summary>
    /// Dépose une commande ; elle sera appliquée au plus tard au tick suivant (GEN-010), dans l'ordre d'arrivée (GEN-011).
    /// Ne bloque jamais.
    /// </summary>
    void Send(Command command);
}
