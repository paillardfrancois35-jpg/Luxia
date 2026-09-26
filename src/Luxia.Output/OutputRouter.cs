using System.Collections.Immutable;
using Luxia.Core.Dmx;
using Luxia.Messaging.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Luxia.Output;

/// <summary>
/// Routeur de sorties : chaque univers vers zéro, un ou plusieurs pilotes (SORT-001).
/// Les affectations sont modifiables à chaud (SORT-061) sans verrou sur le chemin du tick.
/// Les changements d'état des pilotes sont publiés sur le bus (EVT-001) et journalisés (GEN-110).
/// </summary>
public sealed class OutputRouter : IFrameSink, IDisposable
{
    private readonly IEventBus? _bus;
    private readonly ILogger _logger;
    private ImmutableArray<Route> _routes = [];

    /// <summary>Crée le routeur.</summary>
    public OutputRouter(IEventBus? bus = null, ILogger<OutputRouter>? logger = null)
    {
        _bus = bus;
        _logger = logger ?? NullLogger<OutputRouter>.Instance;
    }

    /// <summary>Pilotes affectés, avec leur univers.</summary>
    public IReadOnlyList<(int Universe, OutputDriver Driver)> Routes => [.. _routes.Select(r => (r.Universe, r.Driver))];

    /// <summary>Affecte un pilote à un univers et le démarre.</summary>
    public void Attach(int universe, OutputDriver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentOutOfRangeException.ThrowIfLessThan(universe, 1);
        driver.StateChanged += OnDriverStateChanged;
        ImmutableInterlocked.Update(ref _routes, routes => routes.Add(new Route(universe, driver)));
        driver.Start();
        _logger.LogInformation("Sortie {Sortie} affectée à l'univers {Univers}", driver.Name, universe);
    }

    /// <summary>Retire un pilote (toutes ses affectations), l'arrête et le libère.</summary>
    public void Detach(OutputDriver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ImmutableInterlocked.Update(ref _routes, routes => routes.RemoveAll(r => r.Driver == driver));
        driver.Stop();
        driver.StateChanged -= OnDriverStateChanged;
        driver.Dispose();
        _logger.LogInformation("Sortie {Sortie} retirée", driver.Name);
    }

    /// <inheritdoc />
    public void Submit(int universe, DmxFrame frame, TimeSpan timestamp)
    {
        // Chemin du tick : lecture d'un tableau immuable, aucune allocation, aucun verrou partagé.
        var routes = _routes;
        foreach (var route in routes)
        {
            if (route.Universe == universe)
            {
                route.Driver.Post(frame, timestamp);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var route in _routes)
        {
            Detach(route.Driver);
        }
    }

    private void OnDriverStateChanged(object? sender, OutputDriverStatus status)
    {
        if (sender is not OutputDriver driver)
        {
            return;
        }

        switch (status.State)
        {
            case OutputConnectionState.Connected:
                _logger.LogInformation("Sortie {Sortie} connectée ({Detail})", driver.Name, status.Message);
                break;
            case OutputConnectionState.Error:
                _logger.LogWarning("Sortie {Sortie} en erreur : {Detail}", driver.Name, status.Message);
                break;
            case OutputConnectionState.Disconnected:
                _logger.LogInformation("Sortie {Sortie} déconnectée", driver.Name);
                break;
            case OutputConnectionState.Connecting:
            default:
                break;
        }

        _bus?.Publish(new OutputStateChanged(driver.Id, driver.Name, status.State, status.Message));
    }

    private sealed record Route(int Universe, OutputDriver Driver);
}
