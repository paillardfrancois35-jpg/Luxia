using Luxia.Output.Arduino;

namespace Luxia.Output.Tests;

/// <summary>
/// Faux ports série : chaque port peut héberger un faux firmware qui répond au protocole (labels 77, 10)
/// et mémorise les trames reçues. « Débrancher » fait échouer écritures et ouvertures.
/// </summary>
internal sealed class FakeSerialProvider : ISerialPortProvider
{
    private readonly Lock _lock = new();

    public Dictionary<string, FakeDevice> Devices { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> OpenAttempts { get; } = [];

    public FakeDevice Add(string port, int? vid, int? pid, FakeDeviceKind kind)
    {
        var device = new FakeDevice(new SerialPortInfo(port, vid, pid), kind);
        lock (_lock)
        {
            Devices[port] = device;
        }

        return device;
    }

    public IReadOnlyList<SerialPortInfo> GetPorts()
    {
        lock (_lock)
        {
            return [.. Devices.Values.Where(d => d.Plugged).Select(d => d.Info)];
        }
    }

    public ISerialConnection Open(string portName)
    {
        lock (_lock)
        {
            OpenAttempts.Add(portName);
            if (!Devices.TryGetValue(portName, out var device) || !device.Plugged)
            {
                throw new IOException($"Port {portName} absent");
            }

            return new FakeConnection(device);
        }
    }
}

internal enum FakeDeviceKind
{
    /// <summary>Firmware du projet (répond au label 77).</summary>
    ProjectFirmware,

    /// <summary>Interface Enttec (répond au label 10 seulement).</summary>
    Enttec,

    /// <summary>Appareil muet.</summary>
    Silent,
}

internal sealed class FakeDevice(SerialPortInfo info, FakeDeviceKind kind)
{
    private readonly Lock _lock = new();
    private readonly Queue<byte> _output = new();
    private readonly EnttecMessageParser _parser = new();

    public SerialPortInfo Info { get; } = info;

    public FakeDeviceKind Kind { get; } = kind;

    public string FirmwareVersion { get; set; } = "1.0";

    public volatile bool Plugged = true;

    public List<EnttecMessage> Received { get; } = [];

    public IReadOnlyList<EnttecMessage> Frames
    {
        get
        {
            lock (_lock)
            {
                return [.. Received.Where(m => m.Label is EnttecProtocol.LabelSendDmx or EnttecProtocol.LabelLegacySendDmx)];
            }
        }
    }

    public void Receive(ReadOnlySpan<byte> data)
    {
        lock (_lock)
        {
            foreach (var message in _parser.FeedAll(data))
            {
                Received.Add(message);
                Respond(message);
            }
        }
    }

    public int Transmit(Span<byte> buffer)
    {
        lock (_lock)
        {
            var n = 0;
            while (n < buffer.Length && _output.Count > 0)
            {
                buffer[n++] = _output.Dequeue();
            }

            return n;
        }
    }

    private void Respond(EnttecMessage message)
    {
        byte[]? reply = (Kind, message.Label) switch
        {
            (FakeDeviceKind.ProjectFirmware, EnttecProtocol.LabelIdentify) =>
                EnttecProtocol.Encode(EnttecProtocol.LabelIdentify, System.Text.Encoding.ASCII.GetBytes($"DMX-LEONARDO;fw={FirmwareVersion};ch=512")),
            (FakeDeviceKind.ProjectFirmware or FakeDeviceKind.Enttec, EnttecProtocol.LabelGetSerialNumber) =>
                EnttecProtocol.Encode(EnttecProtocol.LabelGetSerialNumber, [1, 2, 3, 4]),
            _ => null,
        };
        if (reply is not null)
        {
            foreach (var b in reply)
            {
                _output.Enqueue(b);
            }
        }
    }
}

internal sealed class FakeConnection(FakeDevice device) : ISerialConnection
{
    public string PortName => device.Info.PortName;

    public void Write(ReadOnlySpan<byte> data)
    {
        if (!device.Plugged)
        {
            throw new IOException("Périphérique débranché");
        }

        device.Receive(data);
    }

    public int Read(Span<byte> buffer, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        do
        {
            var n = device.Transmit(buffer);
            if (n > 0)
            {
                return n;
            }

            Thread.Sleep(2);
        }
        while (DateTime.UtcNow < deadline);

        return 0;
    }

    public void DiscardInput()
    {
        Span<byte> sink = stackalloc byte[64];
        while (device.Transmit(sink) > 0)
        {
        }
    }

    public void Dispose()
    {
    }
}
