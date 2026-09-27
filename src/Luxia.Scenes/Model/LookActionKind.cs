namespace Luxia.Scenes.Model;

/// <summary>Ce que fait une action d'un look (doc 60 §4.8, F1).</summary>
public enum LookActionKind
{
    /// <summary>Lance une scène (dans sa couche).</summary>
    LaunchScene,

    /// <summary>Arrête une scène (fondu de sortie).</summary>
    StopScene,

    /// <summary>Arrête une couche.</summary>
    StopLayer,

    /// <summary>« Tout arrêter » (sauf les couches épargnées, comme Ambiance, COU-007).</summary>
    StopAll,

    /// <summary>Règle le master d'une couche (<see cref="LookAction.Level"/>, 0-1).</summary>
    LayerMaster,

    /// <summary>Règle le Grand Master (<see cref="LookAction.Level"/>, 0-1).</summary>
    GrandMaster,
}
