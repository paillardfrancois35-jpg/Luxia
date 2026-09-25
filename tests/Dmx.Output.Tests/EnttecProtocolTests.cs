using Dmx.Output.Arduino;

namespace Dmx.Output.Tests;

/// <summary>T-SORT-01 : encodage / décodage des messages et cas limites.</summary>
public sealed class EnttecProtocolTests
{
    [Fact]
    [Trait("Exigence", "SORT-043")]
    public void EncodeSendDmx_ProducesLabel6WithStartCode()
    {
        var frame = new byte[512];
        frame[0] = 10;
        frame[2] = 30;
        var buffer = new byte[EnttecProtocol.MaxMessageLength];

        var length = EnttecProtocol.EncodeSendDmx(frame, 3, buffer);

        buffer[..length].ShouldBe(new byte[] { 0x7E, 6, 4, 0, 0, 10, 0, 30, 0xE7 });
    }

    [Fact]
    [Trait("Exigence", "SORT-020")]
    public void EncodeSendDmx_FullUniverse_Is518Bytes()
    {
        var buffer = new byte[EnttecProtocol.MaxMessageLength];

        var length = EnttecProtocol.EncodeSendDmx(new byte[512], 512, buffer);

        length.ShouldBe(518);
        buffer[2].ShouldBe((byte)(513 & 0xFF));
        buffer[3].ShouldBe((byte)(513 >> 8));
    }

    [Fact]
    [Trait("Exigence", "SORT-014")]
    public void EncodeLegacy_HasNoStartCode()
    {
        var buffer = new byte[EnttecProtocol.MaxMessageLength];

        var length = EnttecProtocol.EncodeLegacySendDmx([1, 2, 3, 4], 2, buffer);

        buffer[..length].ShouldBe(new byte[] { 0x7E, 0x11, 2, 0, 1, 2, 0xE7 });
    }

    [Theory]
    [InlineData(EnttecProtocol.LabelIdentify, 0)]
    [InlineData(EnttecProtocol.LabelGetSerialNumber, 0)]
    [InlineData(EnttecProtocol.LabelGetParameters, 2)]
    [InlineData(EnttecProtocol.LabelSendDmx, 513)]
    public void Parser_RoundTrip(byte label, int length)
    {
        var data = Enumerable.Range(0, length).Select(i => (byte)i).ToArray();
        var parser = new EnttecMessageParser();

        var messages = parser.FeedAll(EnttecProtocol.Encode(label, data));

        messages.Count.ShouldBe(1);
        messages[0].Label.ShouldBe(label);
        messages[0].Data.ShouldBe(data);
    }

    [Fact]
    public void Parser_Length514_IsRejected()
    {
        var parser = new EnttecMessageParser();
        byte[] message = [0x7E, 6, 0x02, 0x02, .. new byte[514], 0xE7];

        parser.FeedAll(message).ShouldBeEmpty();
        parser.RejectedCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "SORT-045")]
    public void Parser_WrongEndByte_IsRejected()
    {
        var parser = new EnttecMessageParser();

        parser.FeedAll([0x7E, 6, 2, 0, 0, 5, 0x00]).ShouldBeEmpty();
        parser.RejectedCount.ShouldBe(1);
    }

    [Fact]
    public void Parser_ResynchronizesOnStartByte()
    {
        var parser = new EnttecMessageParser();
        byte[] noise = [1, 2, 3, 0xE7];

        var messages = parser.FeedAll([.. noise, .. EnttecProtocol.Encode(10, [])]);

        messages.Single().Label.ShouldBe((byte)10);
    }

    [Fact]
    [Trait("Exigence", "SORT-015")]
    public void TryParseIdentity_ReadsVersionAndChannels()
    {
        EnttecProtocol.TryParseIdentity("DMX-LEONARDO;fw=1.2;ch=512"u8, out var identity).ShouldBeTrue();

        identity!.FirmwareVersion.ShouldBe(new Version(1, 2));
        identity.ChannelCount.ShouldBe(512);
    }

    [Fact]
    public void TryParseIdentity_OtherDevice_IsRejected() =>
        EnttecProtocol.TryParseIdentity("HELLO"u8, out _).ShouldBeFalse();

    [Fact]
    [Trait("Exigence", "SORT-012")]
    public void SerialPort_IsNeverOpenedAt1200Baud() =>
        SystemSerialPortProvider.BaudRate.ShouldNotBe(1200); // 1200 bauds = passage du Leonardo en programmation
}
