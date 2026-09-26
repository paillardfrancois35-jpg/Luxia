using System.Collections.Concurrent;
using Luxia.Core.Dmx;
using Luxia.Core.Time;
using Luxia.Messaging.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Luxia.Engine;

/// <summary>
/// Moteur de rendu. À chaque <see cref="Tick"/> : applique les commandes reçues, calcule une trame par univers,
/// la remet au <see cref="IFrameSink"/>. Ne dépend d'aucune interface ni d'aucun pilote (GEN-001) :
/// le cadencement est fourni de l'extérieur (<see cref="Timing.TickLoop"/> en temps réel, appel direct en temps virtuel).
/// </summary>
/// <remarks>
/// P0 : sortie en blackout (GEN-060) et test de sortie (CMD-024). P1 : surcharges brutes de la console (étape 11).
/// Les autres étapes de la chaîne de rendu (doc 02 §9) s'ajoutent phase par phase.
/// </remarks>
public sealed class RenderEngine : ICommandSink
{
    private readonly IFrameSink _sink;
    private readonly IClock _clock;
    private readonly ILogger _logger;
    private readonly ConcurrentQueue<Command> _pending = new();
    private readonly DmxFrame[] _frames;
    private readonly DmxFrame[] _published;
    private readonly Lock _publishedLock = new();
    private readonly TestPattern _testPattern = new();
    private readonly ChannelOverrides[] _overrides;
    private long _tickCount;

    /// <summary>Crée un moteur.</summary>
    /// <param name="sink">Destination des trames.</param>
    /// <param name="clock">Horloge (réelle ou virtuelle).</param>
    /// <param name="universeCount">Nombre d'univers calculés.</param>
    /// <param name="logger">Journal.</param>
    public RenderEngine(IFrameSink sink, IClock clock, int universeCount = 1, ILogger<RenderEngine>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentOutOfRangeException.ThrowIfLessThan(universeCount, 1);
        _sink = sink;
        _clock = clock;
        _logger = logger ?? NullLogger<RenderEngine>.Instance;
        _frames = [.. Enumerable.Range(0, universeCount).Select(_ => new DmxFrame())];
        _published = [.. Enumerable.Range(0, universeCount).Select(_ => new DmxFrame())];
        _overrides = [.. Enumerable.Range(0, universeCount).Select(_ => new ChannelOverrides())];
    }

    /// <summary>Nombre d'univers calculés.</summary>
    public int UniverseCount => _frames.Length;

    /// <summary>Nombre de ticks exécutés.</summary>
    public long TickCount => Interlocked.Read(ref _tickCount);

    /// <summary>État du test de sortie (pour l'affichage).</summary>
    public TestPatternState TestState => _testPattern.State;

    /// <inheritdoc />
    public void Send(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _pending.Enqueue(command);
    }

    /// <summary>Exécute un tick à l'instant donné par l'horloge.</summary>
    public void Tick()
    {
        var now = _clock.Now;

        // GEN-010 / GEN-011 : commandes appliquées au tick suivant leur réception, dans l'ordre d'arrivée.
        while (_pending.TryDequeue(out var command))
        {
            Apply(command, now);
        }

        for (var u = 0; u < _frames.Length; u++)
        {
            var frame = _frames[u];

            // Étape 1 de la chaîne (P0) : tout à 0 = blackout de démarrage (GEN-060).
            frame.Clear();

            // Étape 11 : surcharges brutes de la console (CONS-003).
            // TODO(P4, GEN-042) : blackout et limites de sûreté appliqués aussi aux surcharges brutes.
            _overrides[u].ApplyTo(frame);

            // Test de sortie : remplace toute la restitution de l'univers testé (D19).
            _testPattern.Render(u + 1, frame, now);

            lock (_publishedLock)
            {
                frame.CopyTo(_published[u]);
            }

            _sink.Submit(u + 1, frame, now);
        }

        Interlocked.Increment(ref _tickCount);
    }

    /// <summary>Copie la dernière trame calculée d'un univers (lecture par l'interface, à son rythme).</summary>
    public void CopyLastFrame(int universe, Span<byte> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(universe, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(universe, _frames.Length);
        lock (_publishedLock)
        {
            _published[universe - 1].ReadOnlyValues.CopyTo(destination);
        }
    }

    /// <summary>Nombre de canaux surchargés dans un univers.</summary>
    public int OverrideCount(int universe) => _overrides[CheckUniverse(universe) - 1].Count;

    /// <summary>Copie les surcharges d'un univers : -1 = canal libre, sinon valeur imposée.</summary>
    public void CopyOverrides(int universe, Span<short> destination) => _overrides[CheckUniverse(universe) - 1].CopyTo(destination);

    private int CheckUniverse(int universe)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(universe, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(universe, _frames.Length);
        return universe;
    }

    private void Apply(Command command, TimeSpan now)
    {
        switch (command)
        {
            case OverrideChannelsCommand overrideCommand:
                if (overrideCommand.Universe < 1 || overrideCommand.Universe > _frames.Length)
                {
                    _logger.LogWarning("SurchargerCanal refusée : univers {Univers} inexistant", overrideCommand.Universe);
                    return;
                }

                _overrides[overrideCommand.Universe - 1].Set(overrideCommand.Values);
                break;

            case ReleaseOverridesCommand release:
                foreach (var (index, overrides) in _overrides.Index())
                {
                    if (release.Universe is null || release.Universe == index + 1)
                    {
                        overrides.Release(release.Channels);
                    }
                }

                _logger.LogDebug("LibérerSurcharges (origine {Origine})", release.Origin);
                break;

            case TestOutputCommand test:
                if (test.Universe < 1 || test.Universe > _frames.Length)
                {
                    _logger.LogWarning("TesterSortie refusée : univers {Univers} inexistant", test.Universe);
                    return;
                }

                _testPattern.Apply(test, now);
                _logger.LogInformation(
                    "TesterSortie {Etat} (origine {Origine}) : univers {Univers}, canaux {Plage}, exclus [{Exclus}], valeur {Valeur}",
                    test.Active ? "démarré" : "arrêté",
                    test.Origin,
                    test.Universe,
                    test.Range,
                    ChannelList.Format(test.ExcludedChannels),
                    test.Value);
                break;

            default:
                // GEN-012 (P4) : l'événement CommandeRefusée viendra avec le catalogue complet.
                _logger.LogWarning("Commande non prise en charge : {Commande}", command.GetType().Name);
                break;
        }
    }
}
