using Luxia.Engine.Model;
using Luxia.Messaging.Commands;
using static Luxia.Midi.Tests.MidiTestData;

namespace Luxia.Midi.Tests;

/// <summary>Traduction des messages en commandes (T-MIDI-01), reprise douce (T-MIDI-02), LED (T-MIDI-03).</summary>
public sealed class MidiControllerTests
{
    [Theory]
    [InlineData("APC MINI", "APC mini MK1")]
    [InlineData("APC mini mk2", "APC mini MK2")]
    [InlineData("MIDIIN2 (APC mini mk2)", null)]
    [InlineData("Microsoft GS Wavetable Synth", null)]
    [Trait("Exigence", "MIDI-001")]
    [Trait("Exigence", "GEN-072")]
    public void Profiles_RecognizeBothModels(string port, string? expected)
    {
        ControllerProfiles.Match(port)?.ShortName.ShouldBe(expected);
        (ControllerProfiles.Match(port) is null).ShouldBe(expected is null);
    }

    [Theory]
    [InlineData("MK1")]
    [InlineData("MK2")]
    [Trait("Exigence", "MIDI-002")]
    [Trait("Exigence", "LIVE-003")]
    public void Pad_LaunchesTheSceneOfItsColumnAndRow_WithMidiOrigin(string model)
    {
        var profile = model == "MK1" ? Mk1 : Mk2;
        var controller = new MidiController(profile, "port");

        var launch = Single<LaunchSceneCommand>(controller.Handle(Press(Pad(profile, 1, 2)), Layout(), Snapshot()));

        launch.SceneId.ShouldBe(Blue);
        launch.Origin.ShouldBe(CommandOrigin.Midi);
    }

    [Fact]
    [Trait("Exigence", "LIVE-003")]
    public void Pad_OfPlayingScene_StopsIt()
    {
        var controller = new MidiController(Mk2, "port");
        var snapshot = Snapshot([Playing(Red, Colors)]);

        Single<StopSceneCommand>(controller.Handle(Press(Pad(Mk2, 1, 1)), Layout(), snapshot)).SceneId.ShouldBe(Red);
    }

    [Fact]
    [Trait("Exigence", "COU-005")]
    [Trait("Exigence", "MIDI-002")]
    public void Pad_OfFlashLayer_FlashesWhileHeld()
    {
        var controller = new MidiController(Mk2, "port");

        Single<FlashSceneCommand>(controller.Handle(Press(Pad(Mk2, 3, 1)), Layout(), Snapshot())).Pressed.ShouldBeTrue();
        Single<FlashSceneCommand>(controller.Handle(Release(Pad(Mk2, 3, 1)), Layout(), Snapshot())).Pressed.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "MIDI-002")]
    public void BottomButton_StopsItsLayer_AndRightButtons_AreTheLiveActions()
    {
        var controller = new MidiController(Mk1, "port");

        Single<StopLayerCommand>(controller.Handle(Press(Mk1.BottomButtons[1]), Layout(), Snapshot())).LayerId.ShouldBe(Moves);
        Single<BlackoutCommand>(controller.Handle(Press(Mk1.RightButtons[0]), Layout(), Snapshot())).Active.ShouldBeTrue();
        Single<FlashSceneCommand>(controller.Handle(Press(Mk1.RightButtons[1]), Layout(), Snapshot())).SceneId.ShouldBe(WhiteFlash);
        Single<FlashSceneCommand>(controller.Handle(Press(Mk1.RightButtons[2]), Layout(), Snapshot())).SceneId.ShouldBe(Strobe);
        Single<SmokeCommand>(controller.Handle(Press(Mk1.RightButtons[3]), Layout(), Snapshot())).Pressed.ShouldBeTrue();
        Single<FreezeCommand>(controller.Handle(Press(Mk1.RightButtons[5]), Layout(), Snapshot())).Active.ShouldBeTrue();
        Single<StopLayerCommand>(controller.Handle(Press(Mk1.RightButtons[7]), Layout(), Snapshot())).LayerId.ShouldBeNull();
        controller.Handle(Press(Mk1.RightButtons[4]), Layout(), Snapshot()).ShouldBeEmpty("Tap : phase P7");
    }

    [Fact]
    [Trait("Exigence", "MIDI-011")]
    public void BlackoutNote_IsMomentary_EvenIfBlackoutWasAlreadyOnFromTheScreen()
    {
        var controller = new MidiController(Mk2, "port");
        var alreadyOn = Snapshot(blackout: true);

        // Écran : blackout déjà actif. Note appuyée : il le reste. Note relâchée : blackout annulé.
        Single<BlackoutCommand>(controller.Handle(Press(Mk2.RightButtons[0]), Layout(), alreadyOn)).Active.ShouldBeTrue();
        Single<BlackoutCommand>(controller.Handle(Release(Mk2.RightButtons[0]), Layout(), alreadyOn)).Active.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "MIDI-011")]
    [Trait("Exigence", "MIDI-007")]
    public void BlackoutToggle_ByBinding_TogglesOnPressOnly()
    {
        var bindings = new[] { new MidiBinding { Control = "droite 1", Action = MidiAction.BlackoutToggle } };
        var controller = new MidiController(Mk2, "port");

        Single<BlackoutCommand>(controller.Handle(Press(Mk2.RightButtons[0]), Layout(bindings), Snapshot(blackout: true))).Active.ShouldBeFalse();
        controller.Handle(Release(Mk2.RightButtons[0]), Layout(bindings), Snapshot()).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "MIDI-002")]
    public void ShiftBottom_ChangesTheScenePage()
    {
        var columns = Enumerable.Range(0, 10).Select(_ => new MidiSceneSlot(Guid.NewGuid(), "#FF0000")).ToList();
        var layout = Layout() with { Columns = [new MidiColumn(Colors, false, columns)] };
        var controller = new MidiController(Mk2, "port");

        controller.Handle(Press(Mk2.ShiftNote), layout, Snapshot());
        controller.Handle(Press(Mk2.BottomButtons[1]), layout, Snapshot()).ShouldBeEmpty();
        controller.Handle(Release(Mk2.ShiftNote), layout, Snapshot());

        controller.ScenePage.ShouldBe(1);
        Single<LaunchSceneCommand>(controller.Handle(Press(Pad(Mk2, 1, 1)), layout, Snapshot())).SceneId.ShouldBe(columns[8].SceneId);
    }

    [Fact]
    [Trait("Exigence", "MIDI-004")]
    public void Fader_TakesOverOnlyAfterCrossingTheCurrentValue()
    {
        var controller = new MidiController(Mk2, "port");
        var snapshot = Snapshot(grandMaster: 1);

        controller.Handle(Fader(Mk2, 9, 0), Layout(), snapshot).ShouldBeEmpty("Grand Master à 100 %, fader à 0 : aucun effet");
        controller.Handle(Fader(Mk2, 9, 0.5), Layout(), snapshot).ShouldBeEmpty();
        Single<SetGrandMasterCommand>(controller.Handle(Fader(Mk2, 9, 1), Layout(), snapshot)).Level.ShouldBe(1, 0.01);
        Single<SetGrandMasterCommand>(controller.Handle(Fader(Mk2, 9, 0.8), Layout(), Snapshot(grandMaster: 1))).Level.ShouldBe(0.8, 0.01);
    }

    [Fact]
    [Trait("Exigence", "MIDI-004")]
    public void Fader_LosesControl_WhenTheValueIsChangedElsewhere()
    {
        var controller = new MidiController(Mk2, "port");
        Single<SetLayerMasterCommand>(controller.Handle(Fader(Mk2, 1, 1), Layout(), Snapshot(colorsMaster: 1))).LayerId.ShouldBe(Colors);

        // Master de la couche baissé à 30 % à l'écran : le fader resté en haut ne le reprend qu'en le recroisant.
        controller.Handle(Fader(Mk2, 1, 0.9), Layout(), Snapshot(colorsMaster: 0.3)).ShouldBeEmpty();
        controller.Handle(Fader(Mk2, 1, 0.2), Layout(), Snapshot(colorsMaster: 0.3)).ShouldHaveSingleItem();
    }

    [Fact]
    [Trait("Exigence", "MIDI-003")]
    public void Leds_Mk1_YellowAvailable_GreenActive_BlinkingWhileFadingIn()
    {
        var controller = new MidiController(Mk1, "port");
        var snapshot = Snapshot([Playing(Red, Colors), Playing(Circle, Moves, PlaybackState.FadingIn)], frozen: true);

        var leds = controller.Leds(Layout(), snapshot).ToDictionary(m => (int)m.Data1, m => (int)m.Data2);

        leds[Pad(Mk1, 1, 1)].ShouldBe(1, "rouge joue : vert");
        leds[Pad(Mk1, 1, 2)].ShouldBe(5, "bleu disponible : jaune");
        leds[Pad(Mk1, 2, 1)].ShouldBe(2, "cercle en fondu d'entrée : vert clignotant");
        leds[Pad(Mk1, 8, 8)].ShouldBe(0, "emplacement vide : éteint");
        leds[Mk1.BottomButtons[0]].ShouldBe(1, "couche Couleurs qui joue : bouton stop allumé");
        leds[Mk1.RightButtons[5]].ShouldBe(1, "figé");
        controller.Leds(Layout(), snapshot).ShouldBeEmpty("seuls les changements sont renvoyés");
    }

    [Fact]
    [Trait("Exigence", "MIDI-003")]
    [Trait("Exigence", "MIDI-010")]
    public void Leds_Mk2_UseTheSceneColor_DimWhenAvailable_FullWhenActive()
    {
        var controller = new MidiController(Mk2, "port");

        var leds = controller.Leds(Layout(), Snapshot([Playing(Red, Colors)])).ToDictionary(m => (int)m.Data1);

        leds[Pad(Mk2, 1, 1)].ShouldBe(MidiMessage.NoteOn(6, Pad(Mk2, 1, 1), 5), "rouge, pleine luminosité");
        leds[Pad(Mk2, 1, 2)].ShouldBe(MidiMessage.NoteOn(1, Pad(Mk2, 1, 2), 45), "bleu, faible luminosité");
    }

    [Fact]
    [Trait("Exigence", "MIDI-003")]
    public void Leds_AfterReset_AreAllSentAgain()
    {
        var controller = new MidiController(Mk2, "port");
        controller.Leds(Layout(), Snapshot()).Count.ShouldBe(80);
        controller.ResetLeds();

        controller.Leds(Layout(), Snapshot()).Count.ShouldBe(80, "rebranchement : tout est renvoyé (MIDI-006)");
    }

    [Fact]
    [Trait("Exigence", "MIDI-007")]
    [Trait("Exigence", "MIDI-005")]
    public void Binding_ReplacesTheDefault_ForItsModelOnly()
    {
        var bindings = new[] { new MidiBinding { Model = "MK1", Control = "pad 1 1", Action = MidiAction.LaunchScene, SceneId = Circle } };
        var mk1 = new MidiController(Mk1, "a");
        var mk2 = new MidiController(Mk2, "b");

        Single<LaunchSceneCommand>(mk1.Handle(Press(Pad(Mk1, 1, 1)), Layout(bindings), Snapshot())).SceneId.ShouldBe(Circle);
        Single<LaunchSceneCommand>(mk2.Handle(Press(Pad(Mk2, 1, 1)), Layout(bindings), Snapshot())).SceneId.ShouldBe(Red);
    }

    [Theory]
    [InlineData("pad 3 2", MidiControlKind.Pad, 3, 2)]
    [InlineData("bas 1", MidiControlKind.Bottom, 1, 0)]
    [InlineData("droite:8", MidiControlKind.Right, 8, 0)]
    [InlineData("fader 9", MidiControlKind.Fader, 9, 0)]
    [Trait("Exigence", "MIDI-007")]
    public void Control_Parses(string text, MidiControlKind kind, int x, int y) =>
        MidiControl.Parse(text).ShouldBe(new MidiControl(kind, x, y));

    [Fact]
    [Trait("Exigence", "MIDI-007")]
    public void Control_Unreadable_IsNull() => MidiControl.Parse("pad 9 9").ShouldBeNull();

    [Fact]
    [Trait("Exigence", "MIDI-010")]
    public void NearestPalette_IgnoresBrightness()
    {
        MidiController.NearestPaletteIndex(Mk2.Pads.Palette, "#7F0000").ShouldBe(5);
        MidiController.NearestPaletteIndex(Mk2.Pads.Palette, "#FFA500").ShouldBe(96, "orange #FF7F00 de la palette complète du protocole");
        MidiController.NearestPaletteIndex(Mk2.Pads.Palette, "#FFFFFF").ShouldBe(3);
    }
}
