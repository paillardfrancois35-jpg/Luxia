using System.Globalization;
using System.IO.Ports;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Dmx.Output.Arduino;

/// <summary>
/// Ports série réels (<see cref="SerialPort"/>). Les identifiants USB sont lus dans le registre Windows
/// (<c>HKLM\SYSTEM\CurrentControlSet\Enum\USB\VID_xxxx&amp;PID_xxxx\…\Device Parameters\PortName</c>).
/// </summary>
public sealed partial class SystemSerialPortProvider : ISerialPortProvider
{
    /// <summary>Vitesse d'ouverture. Ignorée par l'USB natif du Leonardo, mais surtout pas 1200 (SORT-012).</summary>
    public const int BaudRate = 115200;

    /// <inheritdoc />
    public IReadOnlyList<SerialPortInfo> GetPorts()
    {
        var present = SerialPort.GetPortNames().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var usb = OperatingSystem.IsWindows() ? ReadUsbIdentifiers() : new Dictionary<string, (int, int)>();

        return [.. present
            .Select(p => usb.TryGetValue(p, out var id) ? new SerialPortInfo(p, id.Item1, id.Item2) : new SerialPortInfo(p, null, null))
            .OrderBy(p => p.PortName, StringComparer.OrdinalIgnoreCase)];
    }

    /// <inheritdoc />
    public ISerialConnection Open(string portName) => new Connection(portName);

    [SupportedOSPlatform("windows")]
    private static Dictionary<string, (int Vid, int Pid)> ReadUsbIdentifiers()
    {
        var result = new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase);
        using var usb = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB");
        if (usb is null)
        {
            return result;
        }

        foreach (var deviceName in usb.GetSubKeyNames())
        {
            var match = VidPidRegex().Match(deviceName);
            if (!match.Success)
            {
                continue;
            }

            var vid = int.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var pid = int.Parse(match.Groups[2].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            using var device = usb.OpenSubKey(deviceName);
            if (device is null)
            {
                continue;
            }

            foreach (var instanceName in device.GetSubKeyNames())
            {
                using var parameters = device.OpenSubKey($@"{instanceName}\Device Parameters");
                if (parameters?.GetValue("PortName") is string port)
                {
                    result[port] = (vid, pid);
                }
            }
        }

        return result;
    }

    [GeneratedRegex("^VID_([0-9A-F]{4})&PID_([0-9A-F]{4})", RegexOptions.IgnoreCase)]
    private static partial Regex VidPidRegex();

    private sealed class Connection : ISerialConnection
    {
        private readonly SerialPort _port;

        public Connection(string portName)
        {
            _port = new SerialPort(portName, BaudRate)
            {
                DtrEnable = true, // SORT-012 : sans DTR, l'USB natif du Leonardo n'échange pas.
                RtsEnable = true,
                WriteTimeout = 500,
                ReadTimeout = 50,
            };
            _port.Open();
        }

        public string PortName => _port.PortName;

        public void Write(ReadOnlySpan<byte> data) => _port.BaseStream.Write(data);

        public int Read(Span<byte> buffer, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                var available = _port.BytesToRead;
                if (available > 0)
                {
                    return _port.BaseStream.Read(buffer[..Math.Min(available, buffer.Length)]);
                }

                Thread.Sleep(5);
            }

            return 0;
        }

        public void DiscardInput() => _port.DiscardInBuffer();

        public void Dispose() => _port.Dispose();
    }
}
