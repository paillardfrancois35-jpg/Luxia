namespace Luxia.Scenes.Model;

/// <summary>Effet d'un clic sur la scène qui joue déjà (LIVE-003).</summary>
public enum ActiveSceneClick
{
    /// <summary>L'arrêter (avec son fondu de sortie).</summary>
    Stop,

    /// <summary>La relancer depuis sa première étape.</summary>
    Restart,
}
