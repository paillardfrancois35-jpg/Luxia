using Luxia.Fixtures.Model;
using Luxia.Fixtures.Rules;

namespace Luxia.Fixtures.Tests;

/// <summary>Opérations d'édition utilisées par l'éditeur de bibliothèque.</summary>
public sealed class FixtureEditsTests
{
    [Fact]
    public void NewFixture_IsValid() => FixtureValidator.IsValid(FixtureEdits.NewFixture()).ShouldBeTrue();

    [Fact]
    [Trait("Exigence", "BIB-010")]
    public void Derive_KeepsContent_WithNewIdAndOrigin()
    {
        var source = Samples.Par;

        var copy = FixtureEdits.Derive(source);

        copy.Id.ShouldNotBe(source.Id);
        copy.DerivedFrom.ShouldBe(source.Id);
        copy.Model.ShouldBe("PAR RGB (copie)");
        copy.Notes!.ShouldStartWith("Dérivé de Test PAR RGB.");
        copy.Modes.Count.ShouldBe(source.Modes.Count);
    }

    [Fact]
    [Trait("Exigence", "BIB-021")]
    public void MoveSlot_ReordersChannels()
    {
        var par = Samples.Par;

        var moved = FixtureEdits.MoveSlot(par, 1, from: 0, to: 3);

        moved.Modes[1].Channels.Select(c => c.Channel).Take(4).ShouldBe(["r", "g", "b", "dim"]);
    }

    [Fact]
    [Trait("Exigence", "BIB-021")]
    public void AddAndRemoveSlot()
    {
        var par = Samples.Par;

        var added = FixtureEdits.AddSlot(par, 0, new ModeChannel("dim"), position: 0);
        added.Modes[0].Channels.Select(c => c.Channel).ShouldBe(["dim", "r", "g", "b"]);

        var removed = FixtureEdits.RemoveSlot(added, 0, 0);
        removed.Modes[0].Channels.Select(c => c.Channel).ShouldBe(["r", "g", "b"]);
        removed.Channel("dim").ShouldNotBeNull(); // la définition reste pour les autres modes
    }

    [Fact]
    [Trait("Exigence", "BIB-003")]
    public void SetResolution_16Bit_AddsFineAfterCoarse_AndBackTo8BitRemovesIt()
    {
        var par = Samples.Par;

        var sixteen = FixtureEdits.SetResolution(par, "dim", ChannelResolution.Bit16);
        sixteen.Modes[1].Channels.Take(2).ShouldBe([new ModeChannel("dim"), new ModeChannel("dim", ChannelPart.Fine)]);
        FixtureValidator.IsValid(sixteen).ShouldBeTrue();

        var eight = FixtureEdits.SetResolution(sixteen, "dim", ChannelResolution.Bit8);
        eight.Modes[1].Channels.ShouldNotContain(c => c.Part == ChannelPart.Fine);
    }

    [Fact]
    public void NewChannel_HasUniqueKey_AndRemoveChannelCleansModes()
    {
        var (fixture, key) = FixtureEdits.NewChannel(FixtureEdits.NewFixture(), "Rouge", AttributeKind.Red);
        key.ShouldBe("canal-2");

        var withSlot = FixtureEdits.AddSlot(fixture, 0, new ModeChannel(key));
        var removed = FixtureEdits.RemoveChannel(withSlot, key);

        removed.Channel(key).ShouldBeNull();
        removed.Modes[0].Channels.ShouldNotContain(c => c.Channel == key);
    }

    [Fact]
    [Trait("Exigence", "BIB-025")]
    public void Wheels_AddUpdateRemove()
    {
        var fixture = FixtureEdits.AddWheel(FixtureEdits.NewFixture(), WheelKind.Color);
        fixture = FixtureEdits.UpdateWheel(fixture, 0, w => w with { Slots = [new WheelSlot("Rouge", ["#FF0000"])] });
        fixture = FixtureEdits.UpdateChannel(fixture, "canal-1", c => c with { Wheel = fixture.Wheels[0].Key });

        var removed = FixtureEdits.RemoveWheel(fixture, 0);

        removed.Wheels.ShouldBeEmpty();
        removed.Channels[0].Wheel.ShouldBeNull();
    }
}
