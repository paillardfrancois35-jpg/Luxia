using Luxia.Core.Dmx;
using Luxia.Core.Time;
using Luxia.Messaging.Commands;

namespace Luxia.Engine.Tests;

/// <summary>T-CONS-01 : prise / libération des faders de la console (surcharges brutes, étape 11).</summary>
public sealed class ChannelOverrideTests
{
    private readonly VirtualClock _clock = new();
    private readonly CapturingSink _sink = new();

    [Fact]
    [Trait("Exigence", "CONS-003")]
    [Trait("Exigence", "CMD-020")]
    public void Override_SetsChannelValueOnNextTick()
    {
        var engine = new RenderEngine(_sink, _clock);

        engine.Send(Override(1, (5, 200), (6, 10)));
        engine.Tick();

        _sink.Last()[4].ShouldBe((byte)200);
        _sink.Last()[5].ShouldBe((byte)10);
        engine.OverrideCount(1).ShouldBe(2);
    }

    [Fact]
    [Trait("Exigence", "CONS-003")]
    [Trait("Exigence", "CMD-022")]
    public void Release_ReturnsChannelToChainValue()
    {
        var engine = new RenderEngine(_sink, _clock);
        engine.Send(Override(1, (5, 200), (6, 10)));
        engine.Tick();

        engine.Send(new ReleaseOverridesCommand(CommandOrigin.User, 1, [5]));
        engine.Tick();

        _sink.Last()[4].ShouldBe((byte)0);
        _sink.Last()[5].ShouldBe((byte)10);
        engine.OverrideCount(1).ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "CONS-004")]
    public void ReleaseAll_ClearsEveryUniverse()
    {
        var engine = new RenderEngine(_sink, _clock, universeCount: 2);
        engine.Send(Override(1, (1, 1)));
        engine.Send(Override(2, (2, 2)));
        engine.Tick();

        engine.Send(ReleaseOverridesCommand.All(CommandOrigin.User));
        engine.Tick();

        engine.OverrideCount(1).ShouldBe(0);
        engine.OverrideCount(2).ShouldBe(0);
        _sink.Frames.TakeLast(2).ShouldAllBe(f => f.Values.All(v => v == 0));
    }

    [Fact]
    [Trait("Exigence", "CONS-003")]
    public void Override_ToZero_IsStillAnOverride()
    {
        var engine = new RenderEngine(_sink, _clock);
        engine.Send(Override(1, (3, 0)));
        engine.Tick();

        var state = new short[DmxConstants.ChannelCount];
        engine.CopyOverrides(1, state);

        state[2].ShouldBe((short)0);
        state[3].ShouldBe((short)-1);
    }

    [Fact]
    public void Override_InvalidChannels_AreIgnored()
    {
        var engine = new RenderEngine(_sink, _clock);

        engine.Send(Override(1, (0, 9), (513, 9), (1, 9)));
        engine.Tick();

        engine.OverrideCount(1).ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "CMD-024")]
    public void TestPattern_ReplacesOverrides_WhileActive()
    {
        var engine = new RenderEngine(_sink, _clock);
        engine.Send(Override(1, (10, 255)));
        engine.Send(new TestOutputCommand(CommandOrigin.User, true, 1, new ChannelRange(1, 1), [], 50, TimeSpan.FromSeconds(1)));
        engine.Tick();
        _sink.Last()[9].ShouldBe((byte)0);

        engine.Send(TestOutputCommand.Stop(CommandOrigin.User));
        engine.Tick();
        _sink.Last()[9].ShouldBe((byte)255);
    }

    private static OverrideChannelsCommand Override(int universe, params (int Channel, byte Value)[] values) =>
        new(CommandOrigin.User, universe, [.. values.Select(v => new ChannelValue(v.Channel, v.Value))]);
}
