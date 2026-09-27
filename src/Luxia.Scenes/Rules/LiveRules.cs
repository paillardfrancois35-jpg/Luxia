using Luxia.Engine.Model;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Rules;

/// <summary>
/// Disposition du jeu en direct, commune à l'écran Live et aux contrôleurs MIDI (doc 18, doc 18b) : mêmes colonnes,
/// mêmes scènes, mêmes boutons FLASH et STROBE.
/// </summary>
public static class LiveRules
{
    /// <summary>
    /// Colonnes du Live : couches par priorité croissante (hors couches masquées), chacune avec ses scènes « visibles en
    /// Live » dans l'ordre de <c>scènes.json</c>.
    /// </summary>
    public static IReadOnlyList<(Layer Layer, IReadOnlyList<Scene> Scenes)> Columns(LiveSettings live, LayerSet layers, SceneSet scenes)
    {
        ArgumentNullException.ThrowIfNull(live);
        ArgumentNullException.ThrowIfNull(layers);
        ArgumentNullException.ThrowIfNull(scenes);
        return [.. layers.Layers
            .OrderBy(l => l.Priority)
            .Where(l => !live.HiddenLayerIds.Contains(l.Id))
            .Select(l => (l, (IReadOnlyList<Scene>)[.. scenes.Scenes.Where(s => s.LayerId == l.Id && s.VisibleInLive)]))];
    }

    /// <summary>
    /// Scènes des boutons FLASH et STROBE : celles de <c>live.json</c>, sinon « Flash blanc » (ou la première scène) et
    /// la première scène « Strobe… » d'une couche de type Flash.
    /// </summary>
    public static (Guid? Flash, Guid? Strobe) PermanentScenes(LiveSettings live, LayerSet layers, SceneSet scenes)
    {
        ArgumentNullException.ThrowIfNull(live);
        ArgumentNullException.ThrowIfNull(layers);
        ArgumentNullException.ThrowIfNull(scenes);
        var flashLayers = layers.Layers.Where(l => l.Kind == LayerKind.Flash).Select(l => l.Id).ToHashSet();
        var flashScenes = scenes.Scenes.Where(s => flashLayers.Contains(s.LayerId)).ToList();
        var ids = scenes.Scenes.Select(s => s.Id).ToHashSet();
        var flash = live.FlashSceneId is { } f && ids.Contains(f)
            ? f
            : (flashScenes.FirstOrDefault(s => s.Name.Contains("flash", StringComparison.OrdinalIgnoreCase)
                && !s.Name.Contains("strobe", StringComparison.OrdinalIgnoreCase)) ?? flashScenes.FirstOrDefault())?.Id;
        var strobe = live.StrobeSceneId is { } s2 && ids.Contains(s2)
            ? s2
            : flashScenes.FirstOrDefault(s => s.Name.Contains("strobe", StringComparison.OrdinalIgnoreCase))?.Id;
        return (flash, strobe);
    }
}
