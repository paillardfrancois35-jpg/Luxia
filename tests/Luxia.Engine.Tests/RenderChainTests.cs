using Luxia.Messaging.Commands;
using static Luxia.Engine.Tests.ShowBuilder;

namespace Luxia.Engine.Tests;

/// <summary>Suite « chaîne de rendu » (GEN-040, MOT-001) : un cas par étape, T-MOT-03, T-MOT-04.</summary>
public sealed class RenderChainTests
{
    [Fact]
    [Trait("Exigence", "MOT-032")]
    [Trait("Exigence", "GEN-040")]
    public void Untouched_Parameter_EmitsChannelDefault()
    {
        var show = new ShowBuilder();
        show.Lyre(111);
        var engine = new EngineHarness(show.Build());

        engine.Tick();

        // Pan au centre par défaut : 0,5 en 16 bits → 0x80 / 0x00 (MOT-090).
        engine[111].ShouldBe((byte)0x80);
        engine[112].ShouldBe((byte)0x00);
        engine[116].ShouldBe((byte)0);
    }

    [Fact]
    [Trait("Exigence", "MOT-090")]
    public void Scene_Values_AreConvertedTo8And16Bits_WithInversion()
    {
        var show = new ShowBuilder();
        var lyre = show.Lyre(111, invertPan: true);
        var layer = show.Layer("Mouvements", 3);
        var scene = show.Scene("Position", layer, Step(0, 1, V(lyre["pan"], 0.25), V(lyre["tilt"], 0.5), V(lyre["dim"], 1)));
        var engine = new EngineHarness(show.Build());

        engine.Launch(scene);
        engine.Tick();

        // Pan inversé : 0,25 → 0,75 → 49151 = 0xBF / 0xFF.
        engine[111].ShouldBe((byte)0xBF);
        engine[112].ShouldBe((byte)0xFF);
        engine[113].ShouldBe((byte)0x80);
        engine[114].ShouldBe((byte)0x00);
        engine[116].ShouldBe((byte)255);
    }

    [Fact]
    [Trait("Exigence", "MOT-070")]
    [Trait("Exigence", "GEN-041")]
    [Trait("Exigence", "CMD-001")]
    public void Blackout_ZeroesIntensitiesOnly_AndReleaseRestoresInstantly()
    {
        var show = new ShowBuilder();
        var par = show.Par7(1);
        var lyre = show.Lyre(111);
        var layer = show.Layer("Tout", 1);
        var scene = show.Scene("Mix", layer, Step(0, 1, V(par["dim"], 1), V(par["r"], 1), V(lyre["pan"], 0.2), V(lyre["dim"], 0.8)));
        var engine = new EngineHarness(show.Build());
        engine.Launch(scene);
        engine.Tick();
        var panBefore = (engine[111], engine[112]);

        engine.Send(new BlackoutCommand(CommandOrigin.User, true));
        engine.Tick();

        engine[1].ShouldBe((byte)0);
        engine[116].ShouldBe((byte)0);
        engine[2].ShouldBe((byte)255);
        (engine[111], engine[112]).ShouldBe(panBefore);

        engine.Send(new BlackoutCommand(CommandOrigin.User, false));
        engine.Tick();

        engine[1].ShouldBe((byte)255);
        engine[116].ShouldBe((byte)204);
        (engine[111], engine[112]).ShouldBe(panBefore);
    }

    [Fact]
    [Trait("Exigence", "MOT-071")]
    [Trait("Exigence", "CMD-002")]
    public void GrandMaster_MultipliesIntensities_NotColors()
    {
        var show = new ShowBuilder();
        var par = show.Par7(1);
        var layer = show.Layer("Tout", 1);
        var scene = show.Scene("Rouge", layer, Step(0, 1, V(par["dim"], 1), V(par["r"], 1)));
        var engine = new EngineHarness(show.Build());
        engine.Launch(scene);

        engine.Send(new SetGrandMasterCommand(CommandOrigin.User, 0.5));
        engine.Tick();

        engine[1].ShouldBe((byte)128);
        engine[2].ShouldBe((byte)255);
    }

    [Fact]
    [Trait("Exigence", "MOT-040")]
    [Trait("Exigence", "BIB-006")]
    public void FollowsIntensity_Rgb3Channels_WhiteAt80Percent_ThenGrandMasterHalf()
    {
        // Critère de MOT-040 : PAR RGB 3CH, blanc, intensité 80 % → 204/204/204 ; Grand Master 50 % → 102/102/102.
        var show = new ShowBuilder();
        var par = show.Par3(1);
        var layer = show.Layer("Tout", 1);
        var scene = show.Scene("Blanc", layer, Step(
            0,
            1,
            V(par[Luxia.Engine.Model.RigParameter.VirtualIntensityKey], 0.8),
            V(par["r"], 1),
            V(par["g"], 1),
            V(par["b"], 1)));
        var engine = new EngineHarness(show.Build());
        engine.Launch(scene);
        engine.Tick();

        new[] { engine[1], engine[2], engine[3] }.ShouldAllBe(v => v == 204);

        engine.Send(new SetGrandMasterCommand(CommandOrigin.User, 0.5));
        engine.Tick();

        new[] { engine[1], engine[2], engine[3] }.ShouldAllBe(v => v == 102);

        engine.Send(new BlackoutCommand(CommandOrigin.User, true));
        engine.Tick();

        new[] { engine[1], engine[2], engine[3] }.ShouldAllBe(v => v == 0);
    }

    [Fact]
    [Trait("Exigence", "CMD-021")]
    [Trait("Exigence", "CONS-022")]
    public void AttributeOverride_WinsOverScenes_AndStaysUnderGrandMaster()
    {
        // Critère de CONS-022 : Grand Master à 50 % → intensité surchargée divisée par 2.
        var show = new ShowBuilder();
        var par = show.Par7(1);
        var layer = show.Layer("Tout", 1);
        var scene = show.Scene("Plein", layer, Step(0, 1, V(par["dim"], 1), V(par["g"], 1)));
        var engine = new EngineHarness(show.Build());
        engine.Launch(scene);

        engine.Send(new OverrideAttributesCommand(CommandOrigin.User, [new AttributeValue(par.Id, "dim", 0.6), new AttributeValue(par.Id, "g", 0.2)]));
        engine.Send(new SetGrandMasterCommand(CommandOrigin.User, 0.5));
        engine.Tick();

        engine[1].ShouldBe((byte)77);
        engine[3].ShouldBe((byte)51);
        engine.Engine.Snapshot.Sources[par["g"]].Kind.ShouldBe(SourceKind.Override);

        engine.Send(new ReleaseAttributesCommand(CommandOrigin.User, par.Id, ["g"]));
        engine.Tick();

        engine[3].ShouldBe((byte)255);
        engine[1].ShouldBe((byte)77);
    }

    [Fact]
    [Trait("Exigence", "CONS-008")]
    [Trait("Exigence", "GEN-042")]
    public void RawOverrides_OfDimmedChannels_AreSilencedByBlackout()
    {
        var show = new ShowBuilder();
        show.Par7(1);
        show.Par3(30);
        var engine = new EngineHarness(show.Build());

        engine.Send(new OverrideChannelsCommand(CommandOrigin.User, 1, [new ChannelValue(1, 255), new ChannelValue(2, 200), new ChannelValue(30, 255), new ChannelValue(300, 99)]));
        engine.Send(new BlackoutCommand(CommandOrigin.User, true));
        engine.Tick();

        // Gradateur du PAR 7CH et rouge du PAR 3CH (qui suit l'intensité virtuelle) : coupés.
        engine[1].ShouldBe((byte)0);
        engine[30].ShouldBe((byte)0);

        // Rouge du PAR 7CH (sans lumière tant que son gradateur est à 0) et canal non patché : surcharge conservée.
        engine[2].ShouldBe((byte)200);
        engine[300].ShouldBe((byte)99);

        engine.Send(new BlackoutCommand(CommandOrigin.User, false));
        engine.Tick();

        engine[1].ShouldBe((byte)255);
        engine[30].ShouldBe((byte)255);
    }

    [Fact]
    [Trait("Exigence", "MOT-091")]
    [Trait("Exigence", "INST-052")]
    public void AbsentFixture_IsEmittedAtZero()
    {
        var show = new ShowBuilder();
        var present = show.Par7(1);
        var absent = show.Par7(8, absent: true);
        var layer = show.Layer("Tout", 1);
        var scene = show.Scene("Tous", layer, Step(0, 1, V(present["dim"], 1), V(absent["dim"], 1), V(absent["r"], 1)));
        var engine = new EngineHarness(show.Build());

        engine.Launch(scene);
        engine.Tick();

        engine[1].ShouldBe((byte)255);
        engine[8].ShouldBe((byte)0);
        engine[9].ShouldBe((byte)0);
    }

    [Fact]
    [Trait("Exigence", "MOT-092")]
    [Trait("Exigence", "INST-014")]
    public void Twins_ShareParameters_AndReceiveSameValues()
    {
        var show = new ShowBuilder();
        var reference = show.Par7(1);
        var twin = new TestFixture(Guid.NewGuid(), reference.Parameters);
        show.Twin(twin, reference);
        var engine = new EngineHarness(show.Build());

        engine.Send(new OverrideAttributesCommand(CommandOrigin.User, [new AttributeValue(twin.Id, "dim", 1)]));
        engine.Tick();

        engine[1].ShouldBe((byte)255);
        engine.Engine.Snapshot.Show.IndexOf(twin.Id, "dim").ShouldBe(reference["dim"]);
    }

    [Fact]
    [Trait("Exigence", "MOT-093")]
    public void EveryTick_SubmitsAFrame_EvenWhenNothingChanges()
    {
        var show = new ShowBuilder();
        show.Par7(1);
        var engine = new EngineHarness(show.Build());
        var before = engine.Sink.Frames.Count;

        engine.Run(1);

        (engine.Sink.Frames.Count - before).ShouldBe(40);
    }

    [Fact]
    [Trait("Exigence", "GEN-043")]
    [Trait("Exigence", "MOT-034")]
    public void Sources_ExplainWhereEachValueComesFrom()
    {
        var show = new ShowBuilder();
        var par = show.Par7(1);
        var layer = show.Layer("Couleurs", 2);
        var scene = show.Scene("Bleu profond", layer, Step(0, 1, V(par["b"], 1), V(par["dim"], 1)));
        var engine = new EngineHarness(show.Build());
        engine.Launch(scene);
        engine.Send(new BlackoutCommand(CommandOrigin.User, true));
        engine.Tick();

        var sources = engine.Engine.Snapshot.Sources;
        sources[par["b"]].ShouldBe(new ParameterSource(SourceKind.Scene, scene.Id, layer.Id));
        sources[par["r"]].Kind.ShouldBe(SourceKind.Default);
        sources[par["dim"]].Kind.ShouldBe(SourceKind.Blackout);
    }
}
