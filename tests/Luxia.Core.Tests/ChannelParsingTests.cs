using Luxia.Core.Dmx;

namespace Luxia.Core.Tests;

public sealed class ChannelParsingTests
{
    [Theory]
    [InlineData("1-16", 1, 16)]
    [InlineData(" 5 ", 5, 5)]
    [InlineData("1 - 512", 1, 512)]
    public void ChannelRange_TryParse_ValidText(string text, int first, int last)
    {
        ChannelRange.TryParse(text, out var range).ShouldBeTrue();
        range.ShouldBe(new ChannelRange(first, last));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0-5")]
    [InlineData("10-5")]
    [InlineData("1-513")]
    [InlineData("a")]
    [InlineData("1-2-3")]
    public void ChannelRange_TryParse_InvalidText(string text) =>
        ChannelRange.TryParse(text, out _).ShouldBeFalse();

    [Fact]
    public void ChannelList_TryParse_MergesAndSorts()
    {
        ChannelList.TryParse("180, 3-5; 4 1", out var channels).ShouldBeTrue();
        channels.ShouldBe([1, 3, 4, 5, 180]);
    }

    [Fact]
    public void ChannelList_TryParse_EmptyIsValid()
    {
        ChannelList.TryParse("  ", out var channels).ShouldBeTrue();
        channels.ShouldBeEmpty();
    }

    [Fact]
    public void ChannelList_Format_GroupsContiguousChannels() =>
        ChannelList.Format([180, 1, 2, 3, 7]).ShouldBe("1-3, 7, 180");

    [Fact]
    public void DmxFrame_Indexer_IsOneBased()
    {
        var frame = new DmxFrame { [1] = 10, [512] = 20 };

        frame.Values[0].ShouldBe((byte)10);
        frame.Values[511].ShouldBe((byte)20);
        Should.Throw<ArgumentOutOfRangeException>(() => frame[0]);
        Should.Throw<ArgumentOutOfRangeException>(() => frame[513]);
    }
}
