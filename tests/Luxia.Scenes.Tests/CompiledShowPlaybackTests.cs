using Luxia.Core.Dmx;
using Luxia.Core.Time;
using Luxia.Engine;
using Luxia.Fixtures.Model;
using Luxia.Messaging.Commands;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;

namespace Luxia.Scenes.Tests;

/// <summary>Bout en bout : projet réel compilé, joué par le moteur en temps virtuel, octets vérifiés au plan d'adresses.</summary>
public sealed class CompiledShowPlaybackTests
{
    private readonly ReferenceProject _project = new();
    private readonly VirtualClock _clock = new();
    private readonly LastFrameSink _sink = new();

    [Fact]
    [Trait("Exigence", "MOT-041")]
    [Trait("Exigence", "PAL-005")]
    public void WarmWhiteOnFourPars_ThenPaletteChange_UpdatesOutput()
    {
        var warm = DefaultPalettes.Colors.Single(p => p.Name == "Blanc chaud");
        var pars = new ValueTarget { Auto = new AutoSelectionTarget(Patch.Rules.AutoSelectionKind.ByModel, Model: "Betopper LPC008S") };
        var scene = new Scene
        {
            Name = "Blanc chaud sur les 4 PAR",
            LayerId = LayerSet.ColorsLayerId,
            Steps =
            [
                new SceneStep
                {
                    Values =
                    [
                        new SceneValue { Target = pars, PaletteId = warm.Id },
                        new SceneValue { Target = pars, Attribute = AttributeKind.Intensity, Level = 1 },
                    ],
                },
            ],
        };
        var palettes = DefaultPalettes.Create();
        var engine = new RenderEngine(_sink, _clock, seed: 1);
        engine.LoadShow(ShowCompiler.Compile(_project.Content(new SceneSet { Scenes = [scene] }, palettes)).Model);
        engine.Send(new LaunchSceneCommand(CommandOrigin.Tool, scene.Id));
        engine.Tick();

        foreach (var address in new[] { 1, 8, 15, 22 })
        {
            _sink.Frame[address - 1].ShouldBe((byte)255);
            _sink.Frame[address].ShouldBe((byte)255);
            _sink.Frame[address + 1].ShouldBe((byte)199);
            _sink.Frame[address + 2].ShouldBe((byte)115);
        }

        // PAL-005 : la palette change pendant que la scène joue → la sortie suit, sans relancer la scène.
        var changed = palettes with
        {
            Palettes = [.. palettes.Palettes.Select(p => p.Id == warm.Id ? p with { Light = new LogicalColor { R = 1, G = 0.6, B = 0.2 } } : p)],
        };
        engine.LoadShow(ShowCompiler.Compile(_project.Content(new SceneSet { Scenes = [scene] }, changed)).Model);
        _clock.Advance(TimeSpan.FromMilliseconds(25));
        engine.Tick();

        _sink.Frame[1].ShouldBe((byte)255);
        _sink.Frame[2].ShouldBe((byte)153);
        _sink.Frame[3].ShouldBe((byte)51);
    }

    private sealed class LastFrameSink : IFrameSink
    {
        public byte[] Frame { get; } = new byte[DmxConstants.ChannelCount];

        public void Submit(int universe, DmxFrame frame, TimeSpan timestamp) => frame.ReadOnlyValues.CopyTo(Frame);
    }
}
