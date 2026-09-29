using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Luxia.Engine;
using Luxia.Messaging.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Luxia.Midi;

/// <summary>
/// Contrôleurs MIDI du poste (doc 18b) : détection automatique des APC mini MK1 et MK2 (MIDI-001, GEN-072), branchement et
/// débranchement à chaud (MIDI-006, GEN-073), plusieurs contrôleurs à la fois (MIDI-005), traduction en commandes
/// (MIDI-002) et retour lumineux (MIDI-003, moins de 100 ms). Rien ne touche la sortie directement (GEN-070).
/// </summary>
public sealed class MidiService : IDisposable
{
    private static readonly TimeSpan ScanPeriod = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan LedPeriod = TimeSpan.FromMilliseconds(40);

    private readonly IMidiPorts _ports;
    private readonly ICommandSink _sink;
    private readonly Func<EngineSnapshot> _snapshot;
    private readonly Func<MidiLayout> _layout;
    private readonly ILogger _logger;
    private readonly List<Device> _devices = [];
    private readonly Lock _lock = new();
    private readonly BlockingCollection<(Device Device, MidiMessage Message)> _incoming = new(1024);
    private readonly Thread _worker;
    private Timer? _scan;
    private Timer? _leds;
    private bool _disposed;

    /// <summary>Crée le service (arrêté).</summary>
    /// <param name="ports">Ports MIDI du poste.</param>
    /// <param name="sink">Destination des commandes (moteur).</param>
    /// <param name="snapshot">État du moteur (retour lumineux, bascules).</param>
    /// <param name="layout">Disposition à jouer (colonnes du Live, affectations).</param>
    /// <param name="logger">Journal.</param>
    public MidiService(IMidiPorts ports, ICommandSink sink, Func<EngineSnapshot> snapshot, Func<MidiLayout> layout, ILogger<MidiService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(ports);
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(layout);
        _ports = ports;
        _sink = sink;
        _snapshot = snapshot;
        _layout = layout;
        _logger = logger ?? NullLogger<MidiService>.Instance;
        _worker = new Thread(Work) { IsBackground = true, Name = "MIDI" };
    }

    /// <summary>Contrôleurs branchés (nom court, port et rôle), pour l'affichage.</summary>
    public IReadOnlyList<string> Connected
    {
        get
        {
            ApplyRoles(_layout());
            lock (_lock)
            {
                return [.. _devices.Select(d => $"{d.Controller.Profile.ShortName} ({d.Controller.PortName}){(d.Controller.Role == MidiRole.Dimmers ? " – dimmers de groupe" : string.Empty)}")];
            }
        }
    }

    /// <summary>Démarre la détection et le retour lumineux.</summary>
    public void Start()
    {
        _worker.Start();
        _scan = new Timer(_ => Scan(), null, TimeSpan.Zero, ScanPeriod);
        _leds = new Timer(_ => UpdateLeds(), null, LedPeriod, LedPeriod);
    }

    /// <summary>Recherche des contrôleurs apparus ou disparus (appelé périodiquement ; public pour les tests).</summary>
    public void Scan()
    {
        IReadOnlyList<string> inputs;
        IReadOnlyList<string> outputs;
        try
        {
            inputs = _ports.Inputs();
            outputs = _ports.Outputs();
        }
        catch (Exception exception) when (exception is IOException or ExternalException or DllNotFoundException or EntryPointNotFoundException)
        {
            _logger.LogWarning(exception, "MIDI : liste des ports illisible");
            return;
        }

        lock (_lock)
        {
            foreach (var gone in _devices.Where(d => !inputs.Contains(d.Controller.PortName)).ToList())
            {
                _devices.Remove(gone);
                gone.Dispose();
                _logger.LogWarning("MIDI : {Controleur} débranché ({Port})", gone.Controller.Profile.ShortName, gone.Controller.PortName);
            }

            foreach (var input in inputs)
            {
                if (_devices.Any(d => d.Controller.PortName == input) || ControllerProfiles.Match(input) is not { } profile)
                {
                    continue;
                }

                Open(input, profile, outputs);
            }
        }
    }

    /// <summary>Recalcule et envoie les LED qui ont changé (appelé périodiquement ; public pour les tests).</summary>
    public void UpdateLeds()
    {
        var snapshot = _snapshot();
        var layout = _layout();
        ApplyRoles(layout);
        lock (_lock)
        {
            foreach (var device in _devices)
            {
                IReadOnlyList<MidiMessage> changes;
                lock (device.Controller)
                {
                    changes = device.Controller.Leds(layout, snapshot);
                }

                device.Send(changes, _logger);
            }
        }
    }

    /// <summary>Traite tout de suite les messages reçus (tests, sans le fil de travail).</summary>
    public void DrainForTests()
    {
        while (_incoming.TryTake(out var item))
        {
            Process(item.Device, item.Message);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _scan?.Dispose();
        _leds?.Dispose();
        _incoming.CompleteAdding();
        lock (_lock)
        {
            foreach (var device in _devices)
            {
                // Toutes les LED éteintes à la fermeture de l'application.
                device.Send(device.Controller.AllOff(), _logger);
                device.Dispose();
            }

            _devices.Clear();
        }

        _incoming.Dispose();
    }

    // ERG-038 : rôle de chaque platine d'après le projet (dimmers de groupe) et le réglage de midi.json.
    private void ApplyRoles(MidiLayout layout)
    {
        lock (_lock)
        {
            var roles = MidiRoles.Assign([.. _devices.Select(d => (d.Controller.Profile, d.Controller.PortName))], layout);
            for (var i = 0; i < _devices.Count; i++)
            {
                var controller = _devices[i].Controller;
                lock (controller)
                {
                    if (controller.Role != roles[i])
                    {
                        controller.Role = roles[i];
                        controller.ResetLeds();
                        _logger.LogInformation("MIDI : {Controleur} ({Port}) sert maintenant aux {Role}", controller.Profile.ShortName, controller.PortName, roles[i] == MidiRole.Dimmers ? "dimmers de groupe" : "couches");
                    }
                }
            }
        }
    }

    private void Open(string input, ControllerProfile profile, IReadOnlyList<string> outputs)
    {
        var controller = new MidiController(profile, input);
        var device = new Device(controller);
        try
        {
            device.Input = _ports.OpenInput(input, message =>
            {
                if (!_incoming.IsAddingCompleted)
                {
                    _incoming.TryAdd((device, message));
                }
            });

            // Sortie des LED : même nom que l'entrée, sinon le premier port de sortie du même modèle.
            var output = outputs.FirstOrDefault(o => o == input) ?? outputs.FirstOrDefault(profile.Matches);
            if (output is not null)
            {
                device.Output = _ports.OpenOutput(output);
            }

            _devices.Add(device);
            _logger.LogInformation("MIDI : {Controleur} branché ({Port}), retour lumineux {Sortie}", profile.ShortName, input, output ?? "absent");
        }
        catch (IOException exception)
        {
            device.Dispose();
            _logger.LogWarning(exception, "MIDI : {Controleur} détecté mais impossible à ouvrir ({Port}) ; nouvel essai plus tard", profile.ShortName, input);
        }
    }

    private void Work()
    {
        try
        {
            foreach (var (device, message) in _incoming.GetConsumingEnumerable())
            {
                Process(device, message);
            }
        }
        catch (ObjectDisposedException)
        {
            // Fermeture de l'application.
        }
    }

    private void Process(Device device, MidiMessage message)
    {
        IReadOnlyList<Command> commands;
        var layout = _layout();
        ApplyRoles(layout);
        lock (device.Controller)
        {
            commands = device.Controller.Handle(message, layout, _snapshot());
        }

        foreach (var command in commands)
        {
            _sink.Send(command);
        }
    }

    private sealed class Device(MidiController controller) : IDisposable
    {
        public MidiController Controller { get; } = controller;

        public IDisposable? Input { get; set; }

        public IMidiOutput? Output { get; set; }

        public void Send(IReadOnlyList<MidiMessage> messages, ILogger logger)
        {
            if (Output is not { } output)
            {
                return;
            }

            try
            {
                foreach (var message in messages)
                {
                    output.Send(message);
                }
            }
            catch (IOException exception)
            {
                // Débranché entre deux recherches : la prochaine détection le retire ; les LED seront renvoyées au rebranchement.
                logger.LogDebug(exception, "MIDI : envoi des LED impossible vers {Port}", Controller.PortName);
                Controller.ResetLeds();
            }
        }

        public void Dispose()
        {
            Input?.Dispose();
            Output?.Dispose();
        }
    }
}
