using System.Globalization;
using Luxia.Engine;
using Luxia.Engine.Model;
using Luxia.Messaging.Commands;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Rules;

/// <summary>Règles des looks (doc 60 §4.8, ERG-023) : commandes à envoyer, capture de l'état joué, description lisible.</summary>
public static class LookRules
{
    /// <summary>Commandes du moteur qui réalisent le look, dans l'ordre de ses actions (actions incomplètes ignorées).</summary>
    public static IReadOnlyList<Command> Commands(Look look, CommandOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(look);
        var commands = new List<Command>();
        foreach (var action in look.Actions)
        {
            Command? command = action.Kind switch
            {
                LookActionKind.LaunchScene when action.SceneId is { } scene => new LaunchSceneCommand(origin, scene),
                LookActionKind.StopScene when action.SceneId is { } scene => new StopSceneCommand(origin, scene),
                LookActionKind.StopLayer when action.LayerId is { } layer => new StopLayerCommand(origin, layer),
                LookActionKind.StopAll => new StopLayerCommand(origin),
                LookActionKind.LayerMaster when action.LayerId is { } layer && action.Level is { } level => new SetLayerMasterCommand(origin, layer, Math.Clamp(level, 0, 1)),
                LookActionKind.GrandMaster when action.Level is { } level => new SetGrandMasterCommand(origin, Math.Clamp(level, 0, 1)),
                _ => null,
            };
            if (command is not null)
            {
                commands.Add(command);
            }
        }

        return commands;
    }

    /// <summary>
    /// « Capturer » : un look qui refait l'état joué — tout arrêter, relancer les scènes qui jouent (sauf les flashs,
    /// qui ne tiennent que maintenus), remettre le master des couches où il relance une scène (même à 100 %, sinon un
    /// look précédent qui l'a baissé la laisserait basse) et de celles qui ne sont pas à 100 %.
    /// Le Grand Master n'est pas capturé : il reste à l'opérateur, comme le fader de l'APC (essai 1.005.222 : un look à
    /// 40 % assombrissait les UV de l'ambiance, et un look capturé à 100 % ne le remontait pas).
    /// </summary>
    public static Look Capture(string name, EngineSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var actions = new List<LookAction> { new() { Kind = LookActionKind.StopAll } };
        foreach (var playback in snapshot.Playbacks)
        {
            if (!playback.Flash && playback.State is not (PlaybackState.FadingOut or PlaybackState.Done)
                && !actions.Any(a => a.SceneId == playback.SceneId))
            {
                actions.Add(new LookAction { Kind = LookActionKind.LaunchScene, SceneId = playback.SceneId });
            }
        }

        var layers = snapshot.Show.Layers;
        var launchedLayers = snapshot.Playbacks
            .Where(p => actions.Any(a => a.SceneId == p.SceneId))
            .Select(p => p.LayerId)
            .ToHashSet();
        for (var i = 0; i < layers.Count && i < snapshot.LayerMasters.Length; i++)
        {
            if (launchedLayers.Contains(layers[i].Id) || Math.Abs(snapshot.LayerMasters[i] - 1) > 0.005)
            {
                actions.Add(new LookAction { Kind = LookActionKind.LayerMaster, LayerId = layers[i].Id, Level = Math.Round(snapshot.LayerMasters[i], 2) });
            }
        }

        return new Look { Name = name, Actions = actions };
    }

    /// <summary>Une action en français, avec les noms des scènes et des couches.</summary>
    public static string Describe(LookAction action, SceneSet scenes, LayerSet layers)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(scenes);
        ArgumentNullException.ThrowIfNull(layers);
        string Scene() => scenes.Scenes.FirstOrDefault(s => s.Id == action.SceneId)?.Name ?? "scène introuvable";
        string Layer() => layers.Layers.FirstOrDefault(l => l.Id == action.LayerId)?.Name ?? "couche introuvable";
        string Percent() => string.Create(CultureInfo.CurrentCulture, $"{Math.Round((action.Level ?? 0) * 100)} %");
        return action.Kind switch
        {
            LookActionKind.LaunchScene => $"▶ lancer « {Scene()} »",
            LookActionKind.StopScene => $"■ arrêter « {Scene()} »",
            LookActionKind.StopLayer => $"■ arrêter la couche {Layer()}",
            LookActionKind.StopAll => "■ tout arrêter (sauf Ambiance)",
            LookActionKind.LayerMaster => $"master {Layer()} à {Percent()}",
            _ => $"Grand Master à {Percent()}",
        };
    }

    /// <summary>Problèmes d'un look (références introuvables), pour « Problèmes du projet » et <c>valider</c>.</summary>
    public static IEnumerable<string> Problems(Look look, SceneSet scenes, LayerSet layers)
    {
        ArgumentNullException.ThrowIfNull(look);
        ArgumentNullException.ThrowIfNull(scenes);
        ArgumentNullException.ThrowIfNull(layers);
        foreach (var action in look.Actions)
        {
            if (action.Kind is LookActionKind.LaunchScene or LookActionKind.StopScene && !scenes.Scenes.Any(s => s.Id == action.SceneId))
            {
                yield return $"look « {look.Name} » : scène introuvable ({action.SceneId})";
            }

            if (action.Kind is LookActionKind.StopLayer or LookActionKind.LayerMaster && !layers.Layers.Any(l => l.Id == action.LayerId))
            {
                yield return $"look « {look.Name} » : couche introuvable ({action.LayerId})";
            }
        }
    }
}
