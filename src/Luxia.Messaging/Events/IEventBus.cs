namespace Luxia.Messaging.Events;

/// <summary>
/// Bus d'événements entre modules (doc 02 §6). La publication ne bloque jamais l'émetteur (GEN-013) :
/// les abonnés sont appelés sur un fil dédié.
/// </summary>
public interface IEventBus
{
    /// <summary>Publie un événement (ne bloque pas).</summary>
    void Publish<TEvent>(TEvent evt)
        where TEvent : class;

    /// <summary>S'abonne aux événements d'un type ; disposer le résultat désabonne.</summary>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : class;
}
