using System.Globalization;
using Luxia.Core.Dmx;
using Luxia.Core.Time;
using Luxia.Engine;
using Luxia.Output.Recording;
using Luxia.Patch.Rules;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Rules;

namespace Luxia.Hosting.Tools;

/// <summary>Résultat d'un déroulé en temps virtuel.</summary>
/// <param name="Lines">Résumé lisible : qui s'allume, quelles couleurs, quels mouvements, à quel moment.</param>
/// <param name="Frames">Nombre de trames calculées (univers 1).</param>
/// <param name="Rejections">Commandes refusées par le moteur, avec leur motif (GEN-012).</param>
public sealed record ScenarioReport(IReadOnlyList<string> Lines, long Frames, IReadOnlyList<string> Rejections);

/// <summary>
/// Joue un scénario (ou une scène) sur le projet compilé, en temps virtuel (GEN-033) : une heure se simule en
/// quelques secondes (MOT-103). Produit un résumé lisible et, si demandé, un enregistrement de trames rejouable (GEN-132).
/// </summary>
public static class ScenarioRunner
{
    /// <summary>Déroule le scénario.</summary>
    /// <param name="content">Projet à jouer.</param>
    /// <param name="scenario">Commandes horodatées.</param>
    /// <param name="duration">Durée simulée (raccourcie par une commande <c>fin</c>).</param>
    /// <param name="recording">Fichier <c>.dmxrec</c> de l'univers 1 à écrire, ou <c>null</c>.</param>
    /// <param name="sampleEvery">Pas du résumé (0,25 s par défaut).</param>
    /// <param name="rateHz">Cadence du moteur.</param>
    public static ScenarioReport Run(
        ProjectContent content,
        Scenario scenario,
        TimeSpan duration,
        string? recording = null,
        TimeSpan? sampleEvery = null,
        double rateHz = DmxConstants.DefaultTickRateHz)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(scenario);
        var compiled = ShowCompiler.Compile(content);
        var patch = new PatchContext(content.Installation, content.Venues, content.TypeOf);
        var clock = new VirtualClock();
        var sink = new MemorySink();
        var engine = new RenderEngine(sink, clock, seed: 1);
        engine.LoadShow(compiled.Model);

        using var writer = recording is null ? null : new RecordingWriter(File.Create(recording), 1, rateHz, DateTime.UtcNow);
        var end = scenario.End is { } stop && stop < duration ? stop : duration;
        var period = TimeSpan.FromTicks((long)Math.Round(TimeSpan.TicksPerSecond / rateHz));
        var sample = sampleEvery ?? TimeSpan.FromSeconds(0.25);
        var lines = new List<string>();
        var previous = new Dictionary<Guid, string>();
        var next = 0;
        var nextSample = TimeSpan.Zero;
        long frames = 0;
        for (var now = TimeSpan.Zero; now <= end; now += period)
        {
            clock.Advance(now - clock.Now);
            while (next < scenario.Steps.Count && scenario.Steps[next].At <= now)
            {
                var step = scenario.Steps[next++];
                engine.Send(step.Command);
                lines.Add($"{Time(now)}  ▸ {step.Text}");
            }

            engine.Tick();
            frames++;
            writer?.Append(sink.Frame, now);
            if (now >= nextSample)
            {
                nextSample += sample;
                Describe(now, patch, sink.Frame, previous, lines);
            }
        }

        var rejections = engine.CommandLog()
            .Where(e => e.Rejection is not null)
            .Select(e => $"{Time(e.AppliedAt)}  {e.Command.GetType().Name} refusée : {e.Rejection}")
            .ToList();
        return new ScenarioReport(lines, frames, rejections);
    }

    /// <summary>Ajoute une ligne par appareil dont l'état visible a changé depuis le dernier échantillon.</summary>
    private static void Describe(TimeSpan now, PatchContext patch, byte[] frame, Dictionary<Guid, string> previous, List<string> lines)
    {
        foreach (var fixture in patch.Fixtures.Where(f => !f.Absent && f.Fixture.Universe == 1))
        {
            var decoded = FixtureDecoder.Decode(fixture.Type, fixture.Mode, fixture.Fixture, frame);
            var (color, intensity) = decoded.Overall();
            var text = intensity < 0.01 || (color.R + color.G + color.B) < 0.02
                ? "éteint"
                : string.Create(CultureInfo.CurrentCulture, $"{ColorName(color)} {color} à {Math.Round(intensity * 100)} %");
            if (decoded.Cells.Any(c => c.Strobing) && text != "éteint")
            {
                text += ", strobe";
            }

            if (decoded.PanDegrees is { } pan)
            {
                text += string.Create(CultureInfo.CurrentCulture, $", pan {Math.Round(pan)}°");
            }

            if (decoded.TiltDegrees is { } tilt)
            {
                text += string.Create(CultureInfo.CurrentCulture, $", tilt {Math.Round(tilt)}°");
            }

            if (!previous.TryGetValue(fixture.Fixture.Id, out var before) || before != text)
            {
                previous[fixture.Fixture.Id] = text;
                if (before is not null || !text.StartsWith("éteint", StringComparison.Ordinal))
                {
                    lines.Add($"{Time(now)}  {fixture.Fixture.Name} : {text}");
                }
            }
        }
    }

    /// <summary>Nom de la palette couleur par défaut la plus proche (lecture humaine du résumé).</summary>
    private static string ColorName(DisplayColor color)
    {
        var max = Math.Max(color.R, Math.Max(color.G, color.B));
        if (max <= 0)
        {
            return "noir";
        }

        var (r, g, b) = (color.R / max, color.G / max, color.B / max);
        return DefaultPalettes.Colors
            .Where(p => p.Light is not null)
            .MinBy(p => Math.Pow(p.Light!.R - r, 2) + Math.Pow(p.Light.G - g, 2) + Math.Pow(p.Light.B - b, 2))!
            .Name.ToLower(CultureInfo.CurrentCulture);
    }

    private static string Time(TimeSpan t) => string.Create(CultureInfo.CurrentCulture, $"{t.TotalSeconds,7:0.00} s");

    private sealed class MemorySink : IFrameSink
    {
        public byte[] Frame { get; } = new byte[DmxConstants.ChannelCount];

        public void Submit(int universe, DmxFrame frame, TimeSpan timestamp)
        {
            if (universe == 1)
            {
                frame.ReadOnlyValues.CopyTo(Frame);
            }
        }
    }
}
