using Luxia.Core.Dmx;
using Luxia.Engine;
using Luxia.Hosting;
using Luxia.Patch.Rules;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Simulator;

/// <summary>
/// Appareils du lieu actif tels que les montre la trame d'un moteur (SIM-003) : partagé par l'écran Simulateur et le
/// plan des appareils de l'écran Contrôle (E5).
/// </summary>
public static class SimulatorVisuals
{
    /// <summary>Décode la dernière trame du moteur donné pour chaque appareil présent du lieu actif.</summary>
    /// <param name="runtime">Application.</param>
    /// <param name="engine">Moteur lu (sortie, ou aperçu en aveugle).</param>
    /// <param name="frames">Tampons de trame réutilisés d'un appel à l'autre (un par univers).</param>
    public static List<SimulatorFixtureVisual> Build(LuxiaRuntime runtime, RenderEngine engine, Dictionary<int, byte[]> frames)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(frames);
        var result = new List<SimulatorFixtureVisual>();
        var venue = runtime.Project.Venues.Active;
        foreach (var fixture in runtime.Project.Installation.Fixtures)
        {
            var placement = venue.PlacementOf(fixture.Id);
            if (placement is not { Absent: false })
            {
                continue;
            }

            var type = runtime.Project.FixtureLibrary?.Find(fixture.FixtureTypeId);
            var mode = type?.Modes.FirstOrDefault(m => m.Name == fixture.ModeName);
            if (type is null || mode is null)
            {
                result.Add(new SimulatorFixtureVisual(fixture.Id, fixture.Name, placement.X, placement.Y, placement.OrientationDeg, false, null, [], HasError: true, Strobing: false));
                continue;
            }

            var decoded = FixtureDecoder.Decode(type, mode, fixture, FrameOf(engine, frames, fixture.Universe));
            var cells = decoded.Cells.Count > 0
                ? decoded.Cells.Select((c, i) => new SimulatorCellVisual(CellOffset(i, decoded.Cells.Count), c.Color.ToString(), c.Intensity)).ToList()
                : [new SimulatorCellVisual(0, "#58A6FF", 0)];
            result.Add(new SimulatorFixtureVisual(
                fixture.Id,
                fixture.Name,
                placement.X,
                placement.Y,
                placement.OrientationDeg,
                decoded.PanDegrees.HasValue,
                decoded.PanDegrees,
                cells,
                HasError: false,
                decoded.Cells.Any(c => c.Strobing)));
        }

        return result;
    }

    private static double CellOffset(int index, int count) => (index - ((count - 1) / 2.0)) * 0.15;

    private static byte[] FrameOf(RenderEngine engine, Dictionary<int, byte[]> frames, int universe)
    {
        if (!frames.TryGetValue(universe, out var frame))
        {
            frame = new byte[DmxConstants.ChannelCount];
            frames[universe] = frame;
        }

        if (universe >= 1 && universe <= engine.UniverseCount)
        {
            engine.CopyLastFrame(universe, frame);
        }

        return frame;
    }
}
