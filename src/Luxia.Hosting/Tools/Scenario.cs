using System.Globalization;
using Luxia.Messaging.Commands;
using Luxia.Scenes.Model;

namespace Luxia.Hosting.Tools;

/// <summary>
/// Scénario de commandes horodatées pour piloter le moteur sans interface (MOT-103, GEN-132). Format texte, une
/// commande par ligne : <c>temps commande arguments</c>, le temps en secondes depuis le début ; <c>#</c> = commentaire ;
/// une scène se désigne par son nom (entre guillemets s'il a des espaces) ou son identifiant.
/// </summary>
/// <remarks>
/// Commandes : <c>lancer "scène" [solo]</c>, <c>arreter "scène"</c>, <c>tout-arreter [tout]</c> (sans « tout », les
/// couches protégées comme Ambiance continuent, COU-007), <c>arreter-couche "couche"</c>, <c>etape-suivante "scène"</c>,
/// <c>etape-precedente "scène"</c>, <c>vitesse "scène" 2</c>, <c>blackout oui|non</c>, <c>grand-master 50</c> (%),
/// <c>master-couche "couche" 50</c> (%), <c>flash "scène" appui|relache</c> (CMD-014), <c>figer oui|non [suspendre]</c>
/// (CMD-003), <c>fumee appui|relache</c> ou <c>fumee rafale 3</c> (s, CMD-030), <c>canal 180 255</c> (surcharge brute de
/// l'univers 1, pour éprouver les limites de sûreté), <c>liberer-canaux</c>, <c>fin</c> (arrête le déroulé à cet instant).
/// </remarks>
public sealed class Scenario
{
    private Scenario(IReadOnlyList<ScenarioStep> steps, TimeSpan? end)
    {
        Steps = steps;
        End = end;
    }

    /// <summary>Commandes, dans l'ordre des temps.</summary>
    public IReadOnlyList<ScenarioStep> Steps { get; }

    /// <summary>Fin demandée par la commande <c>fin</c>, sinon <c>null</c>.</summary>
    public TimeSpan? End { get; }

    /// <summary>Scénario d'une seule scène lancée à 0 s (GEN-132 : « joue une scène »).</summary>
    public static Scenario ForScene(Guid sceneId) =>
        new([new ScenarioStep(TimeSpan.Zero, new LaunchSceneCommand(CommandOrigin.Tool, sceneId), 0, "lancer")], null);

    /// <summary>Lit un scénario ; les noms de scènes et de couches sont résolus avec les données du projet.</summary>
    /// <param name="text">Texte du scénario.</param>
    /// <param name="scenes">Scènes du projet.</param>
    /// <param name="layers">Couches du projet.</param>
    /// <param name="errors">Lignes refusées, avec leur numéro et le motif.</param>
    public static Scenario Parse(string text, SceneSet scenes, LayerSet layers, out IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(scenes);
        ArgumentNullException.ThrowIfNull(layers);
        var problems = new List<string>();
        var steps = new List<ScenarioStep>();
        TimeSpan? end = null;
        var lines = text.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
        for (var n = 0; n < lines.Length; n++)
        {
            var line = lines[n].Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var words = Split(line);
            string Where(string message) => string.Create(CultureInfo.CurrentCulture, $"ligne {n + 1} : {message} (« {line} »)");
            if (words.Count < 2 || !double.TryParse(words[0].Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) || seconds < 0)
            {
                problems.Add(Where("attendu « temps commande … », le temps en secondes"));
                continue;
            }

            var at = TimeSpan.FromSeconds(seconds);
            var verb = words[1].ToLowerInvariant();
            var rest = words.Skip(2).ToList();
            Command? command = null;
            string? error = null;
            switch (verb)
            {
                case "lancer":
                    command = SceneOf(rest, scenes, out error) is { } launched
                        ? new LaunchSceneCommand(CommandOrigin.Tool, launched, Solo: rest.Skip(1).Any(w => w.Equals("solo", StringComparison.OrdinalIgnoreCase)))
                        : null;
                    break;
                case "arreter":
                    command = SceneOf(rest, scenes, out error) is { } stopped ? new StopSceneCommand(CommandOrigin.Tool, stopped) : null;
                    break;
                case "tout-arreter":
                    command = new StopLayerCommand(CommandOrigin.Tool, Everything: rest.Any(w => w.Equals("tout", StringComparison.OrdinalIgnoreCase)));
                    break;
                case "arreter-couche":
                    var stopLayer = LayerOf(rest, layers, out error);
                    command = stopLayer is { } stoppedLayer ? new StopLayerCommand(CommandOrigin.Tool, stoppedLayer) : null;
                    break;
                case "flash":
                    var press = rest.Skip(1).FirstOrDefault()?.ToLowerInvariant();
                    command = SceneOf(rest, scenes, out error) is { } flashed && press is "appui" or "relache"
                        ? new FlashSceneCommand(CommandOrigin.Tool, flashed, press == "appui")
                        : null;
                    error ??= command is null ? "flash \"scène\" appui|relache" : null;
                    break;
                case "figer":
                    var frozen = rest.FirstOrDefault()?.ToLowerInvariant();
                    command = frozen is "oui" or "non"
                        ? new FreezeCommand(CommandOrigin.Tool, frozen == "oui", rest.Skip(1).Any(w => w.Equals("suspendre", StringComparison.OrdinalIgnoreCase)))
                        : null;
                    error = command is null ? "figer oui ou non" : null;
                    break;
                case "fumee":
                    var how = rest.FirstOrDefault()?.ToLowerInvariant();
                    if (how == "rafale")
                    {
                        command = Number(rest, 1, out var burst, ref error) ? new SmokeCommand(CommandOrigin.Tool, false, TimeSpan.FromSeconds(burst)) : null;
                    }
                    else
                    {
                        command = how is "appui" or "relache" ? new SmokeCommand(CommandOrigin.Tool, how == "appui") : null;
                        error = command is null ? "fumee appui|relache, ou fumee rafale <secondes>" : null;
                    }

                    break;
                case "canal":
                    command = Number(rest, 0, out var channel, ref error) && Number(rest, 1, out var value, ref error)
                        && channel is >= 1 and <= 512 && value is >= 0 and <= 255
                        ? new OverrideChannelsCommand(CommandOrigin.Tool, 1, [new ChannelValue((int)channel, (byte)value)])
                        : null;
                    error ??= command is null ? "canal <1-512> <0-255>" : null;
                    break;
                case "liberer-canaux":
                    command = new ReleaseOverridesCommand(CommandOrigin.Tool);
                    break;
                case "etape-suivante" or "etape-precedente":
                    command = SceneOf(rest, scenes, out error) is { } stepped
                        ? new StepSceneCommand(CommandOrigin.Tool, stepped, verb == "etape-suivante" ? StepDirection.Next : StepDirection.Previous)
                        : null;
                    break;
                case "vitesse":
                    command = SceneOf(rest, scenes, out error) is { } sped && Number(rest, 1, out var speed, ref error)
                        ? new SetSceneSpeedCommand(CommandOrigin.Tool, sped, speed)
                        : null;
                    break;
                case "tempo":
                    command = Number(rest, 0, out var tempo, ref error)
                        ? new SetTempoSourceCommand(CommandOrigin.Tool, TempoSourceKind.Fixed, tempo)
                        : null;
                    break;
                case "tap":
                    command = new TapTempoCommand(CommandOrigin.Tool);
                    break;
                case "ajuster-tempo":
                    var adjustment = rest.FirstOrDefault()?.ToLowerInvariant();
                    command = adjustment switch
                    {
                        "x2" => new AdjustTempoCommand(CommandOrigin.Tool, TempoAdjustment.TimesTwo),
                        "/2" => new AdjustTempoCommand(CommandOrigin.Tool, TempoAdjustment.DivideByTwo),
                        "un-ici" => new AdjustTempoCommand(CommandOrigin.Tool, TempoAdjustment.ResyncBar),
                        _ => null,
                    };
                    error ??= command is null ? "ajuster-tempo x2|/2|un-ici" : null;
                    break;
                case "blackout":
                    var on = rest.FirstOrDefault()?.ToLowerInvariant();
                    command = on is "oui" or "non" ? new BlackoutCommand(CommandOrigin.Tool, on == "oui") : null;
                    error = command is null ? "blackout oui ou non" : null;
                    break;
                case "grand-master":
                    command = Number(rest, 0, out var master, ref error) ? new SetGrandMasterCommand(CommandOrigin.Tool, master / 100) : null;
                    break;
                case "master-couche":
                    var layer = rest.Count > 0 ? layers.Layers.FirstOrDefault(l => string.Equals(l.Name, rest[0], StringComparison.CurrentCultureIgnoreCase)) : null;
                    error = layer is null ? "couche inconnue" : null;
                    command = layer is not null && Number(rest, 1, out var level, ref error) ? new SetLayerMasterCommand(CommandOrigin.Tool, layer.Id, level / 100) : null;
                    break;
                case "fin":
                    end = at;
                    continue;
                default:
                    error = $"commande inconnue « {words[1]} »";
                    break;
            }

            if (command is null)
            {
                problems.Add(Where(error ?? "arguments invalides"));
                continue;
            }

            steps.Add(new ScenarioStep(at, command, n + 1, line));
        }

        errors = problems;
        return new Scenario([.. steps.OrderBy(s => s.At)], end);
    }

    private static Guid? SceneOf(List<string> words, SceneSet scenes, out string? error)
    {
        error = null;
        if (words.Count == 0)
        {
            error = "scène attendue";
            return null;
        }

        var key = words[0];
        var scene = Guid.TryParse(key, out var id)
            ? scenes.Scenes.FirstOrDefault(s => s.Id == id)
            : scenes.Scenes.FirstOrDefault(s => string.Equals(s.Name, key, StringComparison.CurrentCultureIgnoreCase));
        error = scene is null ? $"scène inconnue « {key} »" : null;
        return scene?.Id;
    }

    private static Guid? LayerOf(List<string> words, LayerSet layers, out string? error)
    {
        var layer = words.Count > 0 ? layers.Layers.FirstOrDefault(l => string.Equals(l.Name, words[0], StringComparison.CurrentCultureIgnoreCase)) : null;
        error = layer is null ? "couche inconnue" : null;
        return layer?.Id;
    }

    private static bool Number(List<string> words, int index, out double value, ref string? error)
    {
        value = 0;
        if (index < words.Count && double.TryParse(words[index].Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        error ??= "nombre attendu";
        return false;
    }

    /// <summary>Découpe une ligne en mots, en gardant ensemble un texte entre guillemets.</summary>
    private static List<string> Split(string line)
    {
        var words = new List<string>();
        var current = new System.Text.StringBuilder();
        var quoted = false;
        foreach (var c in line)
        {
            if (c == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (char.IsWhiteSpace(c) && !quoted)
            {
                if (current.Length > 0)
                {
                    words.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
        {
            words.Add(current.ToString());
        }

        return words;
    }
}
