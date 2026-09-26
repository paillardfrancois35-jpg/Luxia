using Luxia.Messaging.Events;

namespace Luxia.Engine.Tests;

/// <summary>Bus d'événements synchrone qui garde tout ce qui est publié (tests).</summary>
internal sealed class CapturingBus : IEventBus
{
    private readonly List<object> _events = [];

    public IReadOnlyList<T> Of<T>() => [.. _events.OfType<T>()];

    public void Publish<TEvent>(TEvent evt)
        where TEvent : class => _events.Add(evt);

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : class => throw new NotSupportedException();
}
