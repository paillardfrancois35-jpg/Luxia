using Luxia.Core.Dmx;
using Luxia.Core.Time;
using Luxia.Messaging.Commands;

namespace Luxia.Engine.Tests;

public sealed class RenderEngineTests
{
    private static readonly TimeSpan Period = TimeSpan.FromMilliseconds(25);

    private readonly VirtualClock _clock = new();
    private readonly CapturingSink _sink = new();

    [Fact]
    [Trait("Exigence", "GEN-001")]
    [Trait("Exigence", "GEN-060")]
    public void Tick_WithoutAnything_ProducesBlackoutFrame()
    {
        var engine = new RenderEngine(_sink, _clock);

        engine.Tick();

        _sink.Frames.Count.ShouldBe(1);
        _sink.Last().ShouldAllBe(v => v == 0);
    }

    [Fact]
    [Trait("Exigence", "GEN-001")]
    public void Tick_WithSeveralUniverses_SubmitsOneFramePerUniverse()
    {
        var engine = new RenderEngine(_sink, _clock, universeCount: 3);

        engine.Tick();

        _sink.Frames.Select(f => f.Universe).ShouldBe([1, 2, 3]);
    }

    [Fact]
    [Trait("Exigence", "GEN-010")]
    [Trait("Exigence", "CMD-024")]
    public void Send_BetweenTwoTicks_TakesEffectOnNextTick()
    {
        var engine = new RenderEngine(_sink, _clock);
        engine.Tick();

        engine.Send(Test(new ChannelRange(1, 4), [], TimeSpan.FromSeconds(1)));
        _clock.Advance(Period);
        engine.Tick();

        _sink.Last()[0].ShouldBe((byte)128);
    }

    [Fact]
    [Trait("Exigence", "SORT-007")]
    [Trait("Exigence", "GEN-032")]
    public void TestPattern_WalksChannelsUsingElapsedTime()
    {
        var engine = new RenderEngine(_sink, _clock);
        engine.Send(Test(new ChannelRange(1, 3), [], TimeSpan.FromMilliseconds(500)));

        var lit = new List<int>();
        foreach (var t in new[] { 0, 250, 500, 999, 1000, 1500 })
        {
            _clock.Advance(TimeSpan.FromMilliseconds(t) - (_clock.Now - TimeSpan.Zero));
            engine.Tick();
            lit.Add(LitChannel(_sink.Last()));
        }

        // 0-499 ms : canal 1 ; 500-999 : canal 2 ; 1000-1499 : canal 3 ; puis boucle.
        lit.ShouldBe([1, 1, 2, 2, 3, 1]);
    }

    [Fact]
    [Trait("Exigence", "SORT-007")]
    public void TestPattern_SkipsExcludedChannels()
    {
        var engine = new RenderEngine(_sink, _clock);
        engine.Send(Test(new ChannelRange(178, 181), [180], TimeSpan.FromMilliseconds(100)));

        var lit = new List<int>();
        for (var i = 0; i < 4; i++)
        {
            engine.Tick();
            lit.Add(LitChannel(_sink.Last()));
            _clock.Advance(TimeSpan.FromMilliseconds(100));
        }

        lit.ShouldBe([178, 179, 181, 178]);
        _sink.Frames.ShouldAllBe(f => f.Values[179] == 0);
    }

    [Fact]
    [Trait("Exigence", "SORT-007")]
    public void TestPattern_OnlyCurrentChannelIsLit_WithConfiguredValue()
    {
        var engine = new RenderEngine(_sink, _clock);
        engine.Send(Test(new ChannelRange(1, 16), [], TimeSpan.FromSeconds(1), value: 77));

        engine.Tick();

        var frame = _sink.Last();
        frame.Count(v => v != 0).ShouldBe(1);
        frame[0].ShouldBe((byte)77);
    }

    [Fact]
    [Trait("Exigence", "CMD-024")]
    public void TestPattern_Stop_ReturnsToBlackout()
    {
        var engine = new RenderEngine(_sink, _clock);
        engine.Send(Test(new ChannelRange(1, 16), [], TimeSpan.FromSeconds(1)));
        engine.Tick();

        engine.Send(TestOutputCommand.Stop(CommandOrigin.User));
        engine.Tick();

        _sink.Last().ShouldAllBe(v => v == 0);
        engine.TestState.Active.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "CMD-024")]
    public void TestPattern_WithoutLoop_StopsAfterLastChannel()
    {
        var engine = new RenderEngine(_sink, _clock);
        engine.Send(Test(new ChannelRange(1, 2), [], TimeSpan.FromMilliseconds(100)) with { Loop = false });

        engine.Tick();
        _clock.Advance(TimeSpan.FromMilliseconds(250));
        engine.Tick();

        _sink.Last().ShouldAllBe(v => v == 0);
        engine.TestState.Active.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "SORT-008")]
    public void TestPattern_HeldChannels_StayLitForTheWholeChase()
    {
        var engine = new RenderEngine(_sink, _clock);
        engine.Send(Test(new ChannelRange(2, 4), [], TimeSpan.FromMilliseconds(100)) with { HeldChannels = [1, 8] });

        var lit = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            engine.Tick();
            var frame = _sink.Last();
            frame[0].ShouldBe((byte)128);
            frame[7].ShouldBe((byte)128);
            lit.Add(LitChannel(frame[1..]) + 1);
            _clock.Advance(TimeSpan.FromMilliseconds(100));
        }

        lit.ShouldBe([2, 3, 4]);
    }

    [Fact]
    [Trait("Exigence", "SORT-008")]
    public void TestPattern_HeldChannels_RespectExcludedChannels()
    {
        var engine = new RenderEngine(_sink, _clock);
        engine.Send(Test(new ChannelRange(1, 3), [180], TimeSpan.FromSeconds(1)) with { HeldChannels = [180] });

        engine.Tick();

        _sink.Last()[179].ShouldBe((byte)0);
    }

    [Fact]
    [Trait("Exigence", "GEN-011")]
    public void Commands_InSameTick_AreAppliedInArrivalOrder()
    {
        var engine = new RenderEngine(_sink, _clock);
        engine.Send(Test(new ChannelRange(1, 1), [], TimeSpan.FromSeconds(1)));
        engine.Send(Test(new ChannelRange(5, 5), [], TimeSpan.FromSeconds(1)));

        engine.Tick();

        LitChannel(_sink.Last()).ShouldBe(5);
    }

    [Fact]
    public void CopyLastFrame_ReturnsLastComputedFrame()
    {
        var engine = new RenderEngine(_sink, _clock);
        engine.Send(Test(new ChannelRange(7, 7), [], TimeSpan.FromSeconds(1)));
        engine.Tick();

        var copy = new byte[DmxConstants.ChannelCount];
        engine.CopyLastFrame(1, copy);

        copy[6].ShouldBe((byte)128);
    }

    [Fact]
    [Trait("Exigence", "CMD-024")]
    public void TestPattern_Ramp_VariesAllChannelsBelowValue_AndSkipsExcluded()
    {
        var engine = new RenderEngine(_sink, _clock);
        engine.Send(Test(new ChannelRange(1, 512), [180], TimeSpan.FromSeconds(4), value: 128) with { Mode = TestPatternMode.Ramp });

        engine.Tick();
        var first = _sink.Last();
        _clock.Advance(Period);
        engine.Tick();
        var second = _sink.Last();

        first.ShouldAllBe(v => v <= 128);
        first[179].ShouldBe((byte)0);
        second[179].ShouldBe((byte)0);
        first.Count(v => v > 0).ShouldBeGreaterThan(400);
        Enumerable.Range(0, 512).Count(i => first[i] != second[i]).ShouldBeGreaterThan(300);
    }

    private static TestOutputCommand Test(ChannelRange range, int[] excluded, TimeSpan step, byte value = 128) =>
        new(CommandOrigin.Tool, true, 1, range, excluded, value, step);

    private static int LitChannel(byte[] frame) => Array.FindIndex(frame, v => v != 0) + 1;
}
