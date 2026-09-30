using Luxia.Engine;
using Luxia.Engine.Model;
using Luxia.Messaging.Commands;
using static Luxia.Midi.Tests.MidiTestData;

namespace Luxia.Midi.Tests;

/// <summary>ERG-038 : seconde platine MIDI pour les dimmers de groupe (rôles, faders, remise à 100 %, pages, LED).</summary>
public sealed class MidiDimmerTests
{
    private static readonly MidiDimmerSlot[] Slots = [.. Enumerable.Range(1, 10).Select(i => new MidiDimmerSlot(Guid.NewGuid(), "Groupe " + i))];

    private static MidiLayout WithDimmers(string? controller = null) => Layout() with { Dimmers = Slots, DimmerController = controller };

    private static EngineSnapshot DimmerSnapshot(params double[] levels) =>
        Snapshot() with
        {
            Show = new ShowModel([], [], [], null, null, null, [.. Slots.Select(s => new DimmerGroup(s.GroupId, s.Name, -1, true))], null),
            DimmerLevels = levels,
        };

    private static MidiController DimmerPlatine(ControllerProfile? profile = null) => new(profile ?? Mk1, "port") { Role = MidiRole.Dimmers };

    [Fact]
    [Trait("Exigence", "ERG-038")]
    public void Roles_NoDimmerInTheProject_MeansEveryPlatineKeepsTheLayers()
    {
        var devices = new[] { (Mk1, "APC MINI"), (Mk2, "APC mini mk2") };

        MidiRoles.Assign(devices, Layout()).ShouldAllBe(r => r == MidiRole.Layers);
    }

    [Fact]
    [Trait("Exigence", "ERG-038")]
    public void Roles_WithDimmers_TheSecondPlatineByPortOrderServesTheDimmers_OnlyWhenTwoAreConnected()
    {
        var devices = new[] { (Mk2, "APC mini mk2"), (Mk1, "APC MINI") };

        // Ordre alphabétique des ports : « APC MINI » puis « APC mini mk2 » → la seconde est le MK2, même branché en premier.
        MidiRoles.Assign(devices, WithDimmers()).ShouldBe([MidiRole.Dimmers, MidiRole.Layers]);
        MidiRoles.Assign([devices[0]], WithDimmers()).ShouldBe([MidiRole.Layers]);
    }

    [Fact]
    [Trait("Exigence", "ERG-038")]
    public void Roles_TheSettingOfMidiJson_ChoosesThePlatine_ButNeverLeavesTheLayersWithoutOne()
    {
        var devices = new[] { (Mk1, "APC MINI"), (Mk2, "APC mini mk2") };

        MidiRoles.Assign(devices, WithDimmers("MK1")).ShouldBe([MidiRole.Dimmers, MidiRole.Layers]);
        MidiRoles.Assign(devices, WithDimmers("apc mini")).ShouldBe([MidiRole.Layers, MidiRole.Layers]);
        MidiRoles.Assign([devices[0]], WithDimmers("MK1")).ShouldBe([MidiRole.Layers], "une seule platine : elle garde les couches");
    }

    [Fact]
    [Trait("Exigence", "ERG-038")]
    public void Faders1To8_DriveTheFirstEightDimmers_AndFader9DoesNothing()
    {
        var controller = DimmerPlatine();
        var snapshot = DimmerSnapshot(Enumerable.Repeat(1.0, 10).ToArray());

        var first = Single<SetGroupDimmerCommand>(controller.Handle(Fader(Mk1, 1, 1), WithDimmers(), snapshot));
        first.GroupId.ShouldBe(Slots[0].GroupId);
        first.Origin.ShouldBe(CommandOrigin.Midi);
        Single<SetGroupDimmerCommand>(controller.Handle(Fader(Mk1, 8, 1), WithDimmers(), snapshot)).GroupId.ShouldBe(Slots[7].GroupId);
        controller.Handle(Fader(Mk1, 9, 1), WithDimmers(), snapshot).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "ERG-038")]
    public void DimmerFader_TakesOverOnlyAfterCrossingTheCurrentLevel()
    {
        var controller = DimmerPlatine();
        var snapshot = DimmerSnapshot(Enumerable.Repeat(0.5, 10).ToArray());

        controller.Handle(Fader(Mk1, 2, 0), WithDimmers(), snapshot).ShouldBeEmpty("dimmer à 50 %, fader à 0 : aucun effet");
        Single<SetGroupDimmerCommand>(controller.Handle(Fader(Mk1, 2, 0.5), WithDimmers(), snapshot)).Level.ShouldBe(0.5, 0.01);
        Single<SetGroupDimmerCommand>(controller.Handle(Fader(Mk1, 2, 0.3), WithDimmers(), snapshot)).Level.ShouldBe(0.3, 0.01);
    }

    [Fact]
    [Trait("Exigence", "ERG-038")]
    public void PadsDoNothing_BottomButtonsResetTheDimmerTo100_AndItsLedShowsARetouch()
    {
        var controller = DimmerPlatine();
        var retouched = DimmerSnapshot(0.4, 1, 1, 1, 1, 1, 1, 1, 1, 1);

        controller.Handle(Press(Pad(Mk1, 1, 1)), WithDimmers(), retouched).ShouldBeEmpty();
        var reset = Single<SetGroupDimmerCommand>(controller.Handle(Press(Mk1.BottomButtons[0]), WithDimmers(), retouched));
        reset.GroupId.ShouldBe(Slots[0].GroupId);
        reset.Level.ShouldBe(1);

        var leds = controller.Leds(WithDimmers(), retouched).ToDictionary(m => (int)m.Data1, m => m.Data2);
        leds[Mk1.BottomButtons[0]].ShouldBe((byte)Mk1.Buttons.On);
        leds[Mk1.BottomButtons[1]].ShouldBe((byte)0);
        Enumerable.Range(0, 64).ShouldAllBe(i => leds[Mk1.GridBottomLeftNote + i] == 0, "pads éteints : aucune scène sur cette platine");
    }

    [Fact]
    [Trait("Exigence", "ERG-038")]
    public void AfterTheResetButton_TheFaderMustCrossTheLevelAgain_NoJump()
    {
        var controller = DimmerPlatine();
        var snapshot = DimmerSnapshot(Enumerable.Repeat(1.0, 10).ToArray());
        Single<SetGroupDimmerCommand>(controller.Handle(Fader(Mk1, 1, 1), WithDimmers(), snapshot)).Level.ShouldBe(1, 0.01);
        Single<SetGroupDimmerCommand>(controller.Handle(Fader(Mk1, 1, 0.4), WithDimmers(), snapshot)).Level.ShouldBe(0.4, 0.01);

        // Remise à 100 % au bouton rond : le fader, resté à 40 %, ne reprend pas la main d'un coup.
        Single<SetGroupDimmerCommand>(controller.Handle(Press(Mk1.BottomButtons[0]), WithDimmers(), snapshot)).Level.ShouldBe(1);
        controller.Handle(Fader(Mk1, 1, 0.4), WithDimmers(), snapshot).ShouldBeEmpty("pas de saut à 40 %");
        controller.Handle(Fader(Mk1, 1, 0.7), WithDimmers(), snapshot).ShouldBeEmpty();
        Single<SetGroupDimmerCommand>(controller.Handle(Fader(Mk1, 1, 1), WithDimmers(), snapshot)).Level.ShouldBe(1, 0.01);
        Single<SetGroupDimmerCommand>(controller.Handle(Fader(Mk1, 1, 0.6), WithDimmers(), snapshot)).Level.ShouldBe(0.6, 0.01);
    }

    [Fact]
    [Trait("Exigence", "ERG-038")]
    public void ShiftBottom4_ShowsTheNextEightDimmers()
    {
        var controller = DimmerPlatine();
        var snapshot = DimmerSnapshot(Enumerable.Repeat(1.0, 10).ToArray());

        controller.Handle(Press(Mk1.ShiftNote), WithDimmers(), snapshot);
        controller.Handle(Press(Mk1.BottomButtons[3]), WithDimmers(), snapshot).ShouldBeEmpty();
        controller.Handle(Release(Mk1.ShiftNote), WithDimmers(), snapshot);

        controller.DimmerPage.ShouldBe(1);
        Single<SetGroupDimmerCommand>(controller.Handle(Fader(Mk1, 1, 1), WithDimmers(), snapshot)).GroupId.ShouldBe(Slots[8].GroupId);
        controller.Handle(Fader(Mk1, 3, 1), WithDimmers(), snapshot).ShouldBeEmpty("seulement 10 dimmers : les faders 3 à 8 de la page 2 sont libres");
    }

    [Fact]
    [Trait("Exigence", "ERG-038")]
    public void LayersPlatine_IsUnchanged_EvenWhenTheProjectHasDimmers()
    {
        var controller = new MidiController(Mk2, "port") { Role = MidiRole.Layers };

        Single<LaunchSceneCommand>(controller.Handle(Press(Pad(Mk2, 1, 1)), WithDimmers(), DimmerSnapshot())).SceneId.ShouldBe(Red);
        Single<SetGrandMasterCommand>(controller.Handle(Fader(Mk2, 9, 1), WithDimmers(), Snapshot(grandMaster: 1))).Level.ShouldBe(1, 0.01);
    }
}
