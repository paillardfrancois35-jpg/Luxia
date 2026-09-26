using Luxia.Engine.Model;
using Luxia.Messaging.Commands;
using static Luxia.Engine.Tests.ShowBuilder;

namespace Luxia.Engine.Tests;

/// <summary>Commandes de jeu en direct (P5) : flash, figer, fumée manuelle, tout arrêter, scène de repos.</summary>
public sealed class LiveCommandTests
{
    private readonly ShowBuilder _builder = new();
    private readonly TestFixture _par;
    private readonly EngineLayer _colors;
    private readonly EngineLayer _flashes;
    private readonly EngineLayer _atmosphere;
    private readonly EngineScene _red;
    private readonly EngineScene _blue;
    private readonly EngineScene _whiteFlash;
    private readonly EngineScene _uv;

    public LiveCommandTests()
    {
        _par = _builder.Par7(1);
        _colors = _builder.Layer("Couleurs", 2);
        _atmosphere = _builder.Update(_builder.Layer("Ambiance", 6) with { KeepOnStopAll = true });
        _flashes = _builder.Update(_builder.Layer("Flashs", 99, mode: IntensityMode.Priority) with { Kind = LayerKind.Flash });
        _red = _builder.Scene("Rouge", _colors, Step(0, 1, V(_par["dim"], 1), V(_par["r"], 1), V(_par["g"], 0), V(_par["b"], 0)));
        _blue = _builder.Scene("Bleu", _colors, Step(0, 1, V(_par["dim"], 1), V(_par["r"], 0), V(_par["g"], 0), V(_par["b"], 1)));
        _whiteFlash = _builder.Scene("Flash blanc", _flashes, Step(0, 1, V(_par["dim"], 1), V(_par["r"], 1), V(_par["g"], 1), V(_par["b"], 1)));
        _uv = _builder.Scene("UV", _atmosphere, Step(0, 1, V(_par["b"], 0.5)));
    }

    [Fact]
    [Trait("Exigence", "MOT-072")]
    [Trait("Exigence", "COU-005")]
    [Trait("Exigence", "CMD-014")]
    public void Flash_WhileHeld_OverridesAllLayers_ThenReturnsInstantly()
    {
        var harness = new EngineHarness(_builder.Build());
        harness.Launch(_red);
        harness.Run(0.1);

        harness.Send(new FlashSceneCommand(CommandOrigin.User, _whiteFlash.Id, Pressed: true));
        harness.Tick();
        (harness[1], harness[2], harness[3], harness[4]).ShouldBe(((byte)255, (byte)255, (byte)255, (byte)255), "flash blanc instantané au-dessus des couleurs");

        harness.Run(1);
        harness.Send(new FlashSceneCommand(CommandOrigin.User, _whiteFlash.Id, Pressed: false));
        harness.Tick();
        (harness[2], harness[3], harness[4]).ShouldBe(((byte)255, (byte)0, (byte)0), "au relâchement, retour instantané au rouge qui jouait toujours");
        harness.Playback(_red).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Exigence", "MOT-072")]
    public void Flash_OfALayerScene_DoesNotReplaceTheScenePlayingInThatLayer()
    {
        var harness = new EngineHarness(_builder.Build());
        harness.Launch(_red);
        harness.Run(0.1);

        harness.Send(new FlashSceneCommand(CommandOrigin.User, _blue.Id, Pressed: true));
        harness.Tick();
        harness[4].ShouldBe((byte)255);

        harness.Send(new FlashSceneCommand(CommandOrigin.User, _blue.Id, Pressed: false));
        harness.Tick();
        harness[2].ShouldBe((byte)255, "le rouge de la couche Couleurs n'a pas été remplacé par le flash du bleu");
    }

    [Fact]
    [Trait("Exigence", "MOT-073")]
    [Trait("Exigence", "CMD-003")]
    public void Freeze_KeepsOutput_WhilePlaybacksGoOn_BlackoutStillActive()
    {
        var harness = new EngineHarness(_builder.Build());
        harness.Launch(_red);
        harness.Run(0.1);

        harness.Send(new FreezeCommand(CommandOrigin.User, Active: true));
        harness.Tick();
        harness.Launch(_blue);
        harness.Run(0.5);
        (harness[2], harness[4]).ShouldBe(((byte)255, (byte)0), "figé sur le rouge malgré le bleu lancé");
        harness.Engine.Snapshot.Frozen.ShouldBeTrue();

        harness.Send(new BlackoutCommand(CommandOrigin.User, Active: true));
        harness.Tick();
        harness[1].ShouldBe((byte)0, "le blackout reste actif pendant le gel");
        harness.Send(new BlackoutCommand(CommandOrigin.User, Active: false));

        harness.Send(new FreezeCommand(CommandOrigin.User, Active: false));
        harness.Tick();
        (harness[2], harness[4]).ShouldBe(((byte)0, (byte)255), "au dégel, reprise à l'état courant des lectures (le bleu)");
    }

    [Fact]
    [Trait("Exigence", "MOT-073")]
    public void Freeze_WithSuspendedPlaybacks_StopsTheirProgress()
    {
        var chase = _builder.Scene("Chenillard", _colors, Step(0, 1, V(_par["r"], 1)), Step(0, 1, V(_par["r"], 0.2)));
        var harness = new EngineHarness(_builder.Build());
        harness.Launch(chase);
        harness.Run(0.5);

        harness.Send(new FreezeCommand(CommandOrigin.User, Active: true, SuspendPlaybacks: true));
        harness.Run(3);
        harness.Playback(chase)!.Value.StepIndex.ShouldBe(0);
    }

    [Fact]
    [Trait("Exigence", "CMD-030")]
    [Trait("Exigence", "GEN-084")]
    public void Smoke_HoldAndBurst_GoThroughTheLimiter()
    {
        _builder.Safety = new SafetyModel
        {
            Smoke = new SmokeLimits { MaxEmissionSeconds = 2, MinRestSeconds = 5 },
            SmokeChannels = [new GuardedChannel { FixtureId = Guid.NewGuid(), Label = "Fumée", Universe = 1, Channel = 180, Active = [new ByteRange(1, 255)] }],
        };
        var harness = new EngineHarness(_builder.Build());

        harness.Send(new SmokeCommand(CommandOrigin.User, Pressed: true));
        harness.Tick();
        harness[180].ShouldBe((byte)255);
        harness.Engine.Snapshot.Smoking.ShouldBeTrue();

        harness.Run(2.2);
        harness[180].ShouldBe((byte)0, "maintien plus long que la limite → coupé");
        harness.Send(new SmokeCommand(CommandOrigin.User, Pressed: false));

        harness.Run(5.5);
        harness.Send(new SmokeCommand(CommandOrigin.User, Pressed: false, Burst: TimeSpan.FromSeconds(1)));
        harness.Tick();
        harness[180].ShouldBe((byte)255, "rafale après le repos");
        harness.Run(1.1);
        harness[180].ShouldBe((byte)0, "fin de la rafale");
    }

    [Fact]
    [Trait("Exigence", "CMD-030")]
    public void Smoke_WithoutSmokeMachine_IsRejected()
    {
        var harness = new EngineHarness(_builder.Build());

        harness.Send(new SmokeCommand(CommandOrigin.User, Pressed: true));
        harness.Tick();

        harness.Engine.CommandLog()[^1].Rejection.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Exigence", "COU-007")]
    [Trait("Exigence", "CMD-012")]
    public void StopAll_SparesProtectedLayers_UnlessEverything()
    {
        var harness = new EngineHarness(_builder.Build());
        harness.Launch(_red);
        harness.Launch(_uv);
        harness.Run(0.1);

        harness.Send(new StopLayerCommand(CommandOrigin.User));
        harness.Tick();
        harness.Playback(_red).ShouldBeNull();
        harness.Playback(_uv).ShouldNotBeNull("Ambiance est épargnée par « Tout arrêter »");

        harness.Send(new StopLayerCommand(CommandOrigin.User, Everything: true));
        harness.Tick();
        harness.Playback(_uv).ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "COU-009")]
    public void RestScene_PlaysWhenTheLayerIsEmpty()
    {
        _builder.Update(_colors with { RestSceneId = _blue.Id });
        var harness = new EngineHarness(_builder.Build());
        harness.Tick();
        harness.Playback(_blue).ShouldNotBeNull("scène de repos jouée dès le chargement");

        harness.Launch(_red);
        harness.Run(0.1);
        harness.Playback(_blue).ShouldBeNull();

        harness.Stop(_red);
        harness.Run(0.1);
        harness.Playback(_blue).ShouldNotBeNull("la couche revient à sa scène de repos");
    }
}
