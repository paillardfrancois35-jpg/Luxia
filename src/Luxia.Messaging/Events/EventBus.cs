using System.Collections.Immutable;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Luxia.Messaging.Events;

/// <summary>
/// Bus d'événements asynchrone : file non bornée, distribution sur une tâche de fond.
/// Un abonné lent ou défaillant ne retarde pas l'émetteur (GEN-013) ; ses exceptions sont journalisées (GEN-093).
/// </summary>
public sealed class EventBus : IEventBus, IAsyncDisposable
{
    private readonly Channel<object> _queue = Channel.CreateUnbounded<object>(new UnboundedChannelOptions { SingleReader = true });
    private readonly ILogger _logger;
    private readonly Task _dispatcher;
    private ImmutableDictionary<Type, ImmutableList<Delegate>> _handlers = ImmutableDictionary<Type, ImmutableList<Delegate>>.Empty;

    /// <summary>Crée le bus et démarre la distribution.</summary>
    public EventBus(ILogger<EventBus>? logger = null)
    {
        _logger = logger ?? NullLogger<EventBus>.Instance;
        _dispatcher = Task.Run(DispatchAsync);
    }

    /// <inheritdoc />
    public void Publish<TEvent>(TEvent evt)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(evt);
        _queue.Writer.TryWrite(evt);
    }

    /// <inheritdoc />
    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(handler);
        ImmutableInterlocked.AddOrUpdate(ref _handlers, typeof(TEvent), [handler], (_, list) => list.Add(handler));
        return new Subscription(() =>
            ImmutableInterlocked.AddOrUpdate(ref _handlers, typeof(TEvent), [], (_, list) => list.Remove(handler)));
    }

    /// <summary>Attend que tous les événements déjà publiés aient été distribués (tests).</summary>
    public async Task FlushAsync()
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Writer.TryWrite(done);
        await done.Task.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _queue.Writer.TryComplete();
        await _dispatcher.ConfigureAwait(false);
    }

    private async Task DispatchAsync()
    {
        await foreach (var evt in _queue.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            if (evt is TaskCompletionSource flush)
            {
                flush.TrySetResult();
                continue;
            }

            // Les abonnés à un type de base reçoivent aussi les événements dérivés.
            foreach (var (type, handlers) in _handlers)
            {
                if (!type.IsInstanceOfType(evt))
                {
                    continue;
                }

                foreach (var handler in handlers)
                {
                    try
                    {
                        handler.DynamicInvoke(evt);
                    }
#pragma warning disable CA1031 // Un abonné défaillant ne doit jamais arrêter la distribution (GEN-093).
                    catch (Exception ex)
#pragma warning restore CA1031
                    {
                        _logger.LogError(ex, "Abonné en erreur sur l'événement {Evenement}", evt.GetType().Name);
                    }
                }
            }
        }
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;

        public void Dispose() => Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
    }
}
