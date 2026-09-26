using Luxia.Core.Dmx;
using Luxia.Core.Settings;
using Luxia.Messaging.Events;
using Luxia.Output.Arduino;

namespace Luxia.Output.Tests;

/// <summary>Pilote Arduino contre un faux firmware (détection, reconnexion, protocole).</summary>
public sealed class ArduinoOutputDriverTests
{
    private readonly FakeSerialProvider _ports = new();

    [Fact]
    [Trait("Exigence", "SORT-010")]
    public async Task Connect_FindsProjectFirmwareAmongArduinoPorts()
    {
        _ports.Add("COM3", 0x2341, 0x8036, FakeDeviceKind.Silent);
        var device = _ports.Add("COM7", 0x2341, 0x8036, FakeDeviceKind.ProjectFirmware);
        using var driver = new ArduinoOutputDriver(new ArduinoPreferences(), _ports);

        driver.Start();
        await OutputRouterTests.Eventually(() => driver.Status.State == OutputConnectionState.Connected);

        driver.ConnectedPort.ShouldBe("COM7");
        driver.Firmware!.FirmwareVersion.ShouldBe(new Version(1, 0));
        driver.Status.Message!.ShouldContain("COM7");
        device.Received.ShouldContain(m => m.Label == EnttecProtocol.LabelIdentify);
    }

    [Fact]
    [Trait("Exigence", "SORT-010")]
    public async Task Connect_DoesNotProbeNonArduinoPorts_ByDefault()
    {
        _ports.Add("COM1", null, null, FakeDeviceKind.ProjectFirmware);
        using var driver = new ArduinoOutputDriver(new ArduinoPreferences(), _ports);

        driver.Start();
        await Task.Delay(300);

        driver.Status.State.ShouldNotBe(OutputConnectionState.Connected);
        _ports.OpenAttempts.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "SORT-010")]
    public async Task Connect_ProbesAllPorts_WhenEnabled()
    {
        _ports.Add("COM1", null, null, FakeDeviceKind.ProjectFirmware);
        using var driver = new ArduinoOutputDriver(new ArduinoPreferences { ProbeAllPorts = true }, _ports);

        driver.Start();
        await OutputRouterTests.Eventually(() => driver.Status.State == OutputConnectionState.Connected);

        driver.ConnectedPort.ShouldBe("COM1");
    }

    [Fact]
    [Trait("Exigence", "SORT-011")]
    public async Task Connect_TriesLastPortFirst_AndReportsSelectedPort()
    {
        _ports.Add("COM3", 0x2341, 0x8036, FakeDeviceKind.ProjectFirmware);
        _ports.Add("COM9", 0x2341, 0x8036, FakeDeviceKind.ProjectFirmware);
        using var driver = new ArduinoOutputDriver(new ArduinoPreferences { LastPort = "COM9" }, _ports);
        string? selected = null;
        driver.PortSelected += (_, port) => selected = port;

        driver.Start();
        await OutputRouterTests.Eventually(() => driver.Status.State == OutputConnectionState.Connected);

        _ports.OpenAttempts[0].ShouldBe("COM9");
        selected.ShouldBe("COM9");
    }

    [Fact]
    public async Task Connect_SkipsBootloader()
    {
        _ports.Add("COM4", 0x2341, 0x0036, FakeDeviceKind.ProjectFirmware);
        using var driver = new ArduinoOutputDriver(new ArduinoPreferences(), _ports);

        driver.Start();
        await Task.Delay(300);

        _ports.OpenAttempts.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "SORT-020")]
    [Trait("Exigence", "SORT-021")]
    public async Task Post_SendsFullFrameWithConfiguredChannelCount()
    {
        var device = _ports.Add("COM5", 0x2341, 0x8036, FakeDeviceKind.ProjectFirmware);
        using var driver = new ArduinoOutputDriver(new ArduinoPreferences { ChannelCount = 120 }, _ports);
        driver.Start();
        await OutputRouterTests.Eventually(() => driver.Status.State == OutputConnectionState.Connected);

        driver.Post(new DmxFrame { [1] = 255, [120] = 7 }, TimeSpan.Zero);
        await OutputRouterTests.Eventually(() => device.Frames.Count > 0);

        var message = device.Frames[^1];
        message.Label.ShouldBe(EnttecProtocol.LabelSendDmx);
        message.Data.Length.ShouldBe(121);
        message.Data[0].ShouldBe((byte)0);
        message.Data[1].ShouldBe((byte)255);
        message.Data[120].ShouldBe((byte)7);
    }

    [Fact]
    [Trait("Exigence", "SORT-013")]
    [Trait("Exigence", "SORT-022")]
    [Trait("Exigence", "GEN-091")]
    public async Task Unplug_ThenReplug_ReconnectsWithinThreeSeconds()
    {
        var device = _ports.Add("COM5", 0x2341, 0x8036, FakeDeviceKind.ProjectFirmware);
        using var driver = new ArduinoOutputDriver(new ArduinoPreferences(), _ports);
        driver.Start();
        await OutputRouterTests.Eventually(() => driver.Status.State == OutputConnectionState.Connected);

        device.Plugged = false;
        using var cts = new CancellationTokenSource();
        var pump = Task.Run(async () =>
        {
            var frame = new DmxFrame();
            while (!cts.IsCancellationRequested)
            {
                driver.Post(frame, TimeSpan.Zero);
                await Task.Delay(25);
            }
        });

        await OutputRouterTests.Eventually(() => driver.Status.State != OutputConnectionState.Connected);
        var replugged = DateTime.UtcNow;
        device.Plugged = true;
        await OutputRouterTests.Eventually(() => driver.Status.State == OutputConnectionState.Connected, TimeSpan.FromSeconds(3));
        (DateTime.UtcNow - replugged).ShouldBeLessThan(TimeSpan.FromSeconds(3));

        await cts.CancelAsync();
        await pump;
        driver.Status.ErrorCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    [Trait("Exigence", "SORT-014")]
    public async Task ForcedPort_Legacy_SendsLabel0x11WithoutIdentification()
    {
        var device = _ports.Add("COM8", null, null, FakeDeviceKind.Silent);
        var settings = new ArduinoPreferences { ForcedPort = "COM8", Protocol = ArduinoProtocol.Legacy, ChannelCount = 16 };
        using var driver = new ArduinoOutputDriver(settings, _ports);
        driver.Start();
        await OutputRouterTests.Eventually(() => driver.Status.State == OutputConnectionState.Connected);

        driver.Post(new DmxFrame { [2] = 9 }, TimeSpan.Zero);
        await OutputRouterTests.Eventually(() => device.Frames.Count > 0);

        device.Received.ShouldNotContain(m => m.Label == EnttecProtocol.LabelIdentify);
        device.Frames[^1].Label.ShouldBe(EnttecProtocol.LabelLegacySendDmx);
        device.Frames[^1].Data.Length.ShouldBe(16);
        device.Frames[^1].Data[1].ShouldBe((byte)9);
    }

    [Fact]
    [Trait("Exigence", "SORT-015")]
    public async Task OldFirmware_IsAcceptedWithWarning()
    {
        var device = _ports.Add("COM5", 0x2341, 0x8036, FakeDeviceKind.ProjectFirmware);
        device.FirmwareVersion = "0.9";
        using var driver = new ArduinoOutputDriver(new ArduinoPreferences(), _ports);

        driver.Start();
        await OutputRouterTests.Eventually(() => driver.Status.State == OutputConnectionState.Connected);

        driver.Status.Message!.ShouldContain("firmware ancien");
    }

    [Fact]
    public async Task EnttecInterface_IsAcceptedViaSerialNumber()
    {
        _ports.Add("COM6", 0x2341, 0x8036, FakeDeviceKind.Enttec);
        using var driver = new ArduinoOutputDriver(new ArduinoPreferences(), _ports);

        driver.Start();
        await OutputRouterTests.Eventually(() => driver.Status.State == OutputConnectionState.Connected);

        driver.Firmware!.Name.ShouldBe("Compatible Enttec");
    }
}
