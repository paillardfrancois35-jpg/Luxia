using Luxia.Core.Settings;
using Luxia.Messaging.Events;
using Microsoft.Extensions.Logging;

namespace Luxia.Output.Arduino;

/// <summary>
/// Pilote Arduino Leonardo + shield DMX (doc 10 §4) : détection automatique du port par identification (SORT-010),
/// dernier port essayé en premier (SORT-011), port forcé (SORT-014), version du firmware (SORT-015),
/// trame complète à chaque tick (SORT-020), nombre de canaux réglable (SORT-021).
/// </summary>
public sealed class ArduinoOutputDriver : OutputDriver
{
    /// <summary>Identifiant du pilote Arduino dans les préférences.</summary>
    public const string DriverId = "arduino";

    /// <summary>Version minimale du firmware sans avertissement (SORT-015).</summary>
    public static readonly Version MinimumFirmwareVersion = new(1, 0);

    /// <summary>Délai d'attente d'une réponse d'identification.</summary>
    public static readonly TimeSpan IdentifyTimeout = TimeSpan.FromMilliseconds(400);

    private readonly ISerialPortProvider _ports;
    private readonly byte[] _buffer = new byte[EnttecProtocol.MaxMessageLength];
    private ArduinoPreferences _settings;
    private ISerialConnection? _connection;
    private ArduinoProtocol _protocol;

    /// <summary>Crée le pilote.</summary>
    public ArduinoOutputDriver(ArduinoPreferences settings, ISerialPortProvider ports, ILogger<ArduinoOutputDriver>? logger = null)
        : base(DriverId, "Arduino", logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(ports);
        _settings = settings;
        _ports = ports;
    }

    /// <summary>Levé quand un port a été retenu, pour le mémoriser dans les préférences (SORT-011).</summary>
    public event EventHandler<string>? PortSelected;

    /// <summary>Port connecté.</summary>
    public string? ConnectedPort => _connection?.PortName;

    /// <summary>Identification du firmware connecté (null si inconnue).</summary>
    public EnttecProtocol.Identity? Firmware { get; private set; }

    /// <summary>Réglages courants (le nombre de canaux est pris en compte à chaud).</summary>
    public ArduinoPreferences Settings
    {
        get => Volatile.Read(ref _settings);
        set => Volatile.Write(ref _settings, value ?? throw new ArgumentNullException(nameof(value)));
    }

    /// <inheritdoc />
    protected override bool Connect()
    {
        var settings = Settings;
        if (!string.IsNullOrWhiteSpace(settings.ForcedPort))
        {
            return ConnectForced(settings);
        }

        var ports = _ports.GetPorts().Where(p => !p.IsBootloader).ToList();
        var candidates = ports
            .Where(p => p.IsArduino || settings.ProbeAllPorts || string.Equals(p.PortName, settings.LastPort, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => string.Equals(p.PortName, settings.LastPort, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(p => p.IsArduino)
            .ToList();

        if (candidates.Count == 0)
        {
            SetState(OutputConnectionState.Disconnected, "aucune carte Arduino détectée");
            return false;
        }

        foreach (var candidate in candidates)
        {
            if (TryIdentify(candidate.PortName))
            {
                return true;
            }
        }

        SetState(OutputConnectionState.Disconnected, $"aucune réponse sur {string.Join(", ", candidates.Select(c => c.PortName))}");
        return false;
    }

    /// <inheritdoc />
    protected override void Write(ReadOnlySpan<byte> frame, TimeSpan timestamp)
    {
        var connection = _connection ?? throw new InvalidOperationException("Port non ouvert.");
        var channels = Math.Clamp(Settings.ChannelCount, 1, frame.Length);
        var length = _protocol == ArduinoProtocol.Legacy
            ? EnttecProtocol.EncodeLegacySendDmx(frame, channels, _buffer)
            : EnttecProtocol.EncodeSendDmx(frame, channels, _buffer);
        connection.Write(_buffer.AsSpan(0, length));
    }

    /// <inheritdoc />
    protected override void Disconnect()
    {
        _connection?.Dispose();
        _connection = null;
        Firmware = null;
    }

    private bool ConnectForced(ArduinoPreferences settings)
    {
        var port = settings.ForcedPort!;
        _connection = _ports.Open(port);
        _protocol = settings.Protocol;
        Firmware = _protocol == ArduinoProtocol.Enttec ? Identify(_connection) : null;
        SetState(OutputConnectionState.Connected, Describe(port, Firmware, forced: true));
        return true;
    }

    private bool TryIdentify(string port)
    {
        ISerialConnection? connection = null;
        try
        {
            connection = _ports.Open(port);
            var identity = Identify(connection);
            if (identity is null)
            {
                connection.Dispose();
                Logger.LogDebug("Port {Port} : pas de réponse d'identification", port);
                return false;
            }

            _connection = connection;
            _protocol = ArduinoProtocol.Enttec;
            Firmware = identity;
            if (identity.FirmwareVersion < MinimumFirmwareVersion)
            {
                Logger.LogWarning("Firmware {Version} plus ancien que la version attendue {Minimum}", identity.FirmwareVersion, MinimumFirmwareVersion);
            }

            PortSelected?.Invoke(this, port);
            SetState(OutputConnectionState.Connected, Describe(port, identity, forced: false));
            return true;
        }
#pragma warning disable CA1031 // Port occupé ou disparu : on passe au suivant.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            connection?.Dispose();
            Logger.LogDebug(ex, "Port {Port} : ouverture impossible", port);
            return false;
        }
    }

    /// <summary>Label 77 (identification du projet), à défaut label 10 (numéro de série Enttec).</summary>
    private static EnttecProtocol.Identity? Identify(ISerialConnection connection)
    {
        connection.DiscardInput();
        var reply = Ask(connection, EnttecProtocol.LabelIdentify);
        if (reply is not null && EnttecProtocol.TryParseIdentity(reply.Data, out var identity))
        {
            return identity;
        }

        reply = Ask(connection, EnttecProtocol.LabelGetSerialNumber);
        return reply is { Data.Length: 4 }
            ? new EnttecProtocol.Identity("Compatible Enttec", new Version(0, 0), 512)
            : null;
    }

    private static EnttecMessage? Ask(ISerialConnection connection, byte label)
    {
        connection.Write(EnttecProtocol.Encode(label, []));
        var parser = new EnttecMessageParser();
        var buffer = new byte[256];
        var deadline = DateTime.UtcNow + IdentifyTimeout;
        while (DateTime.UtcNow < deadline)
        {
            var read = connection.Read(buffer, deadline - DateTime.UtcNow);
            foreach (var message in parser.FeedAll(buffer.AsSpan(0, read)))
            {
                if (message.Label == label)
                {
                    return message;
                }
            }
        }

        return null;
    }

    private static string Describe(string port, EnttecProtocol.Identity? identity, bool forced)
    {
        var text = identity is null ? port : $"{port} – {identity.Name} {identity.FirmwareVersion}";
        if (identity is not null && identity.FirmwareVersion < MinimumFirmwareVersion && identity.Name == EnttecProtocol.IdentityPrefix)
        {
            text += " (firmware ancien)";
        }

        return forced ? $"{text} (port imposé)" : text;
    }
}
