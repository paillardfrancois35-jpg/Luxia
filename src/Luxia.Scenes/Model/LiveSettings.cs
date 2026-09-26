namespace Luxia.Scenes.Model;

/// <summary>
/// Réglages de l'écran Live du projet (doc 18), enregistrés dans <c>live.json</c> (doc 50). Tout est facultatif : sans
/// fichier, l'écran se déduit des couches et des scènes (une colonne par couche, scènes « visibles en Live » dans
/// l'ordre de <c>scènes.json</c>). Pas d'éditeur à l'écran en P5 (LIVE-006 reporté, Q32) : le fichier se règle à la main
/// ou par une IA de conception, et <c>valider</c> le vérifie.
/// </summary>
public sealed record LiveSettings
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Scène jouée en flash par le bouton « FLASH » et la touche F ; absente = « Flash blanc » d'une couche Flash.</summary>
    public Guid? FlashSceneId { get; init; }

    /// <summary>Scène jouée en flash par le bouton « STROBE » et la touche S ; absente = première scène « Strobe… » d'une couche Flash.</summary>
    public Guid? StrobeSceneId { get; init; }

    /// <summary>Durée d'une rafale de fumée (bouton « Rafale »), en secondes (toujours soumise au limiteur, GEN-084).</summary>
    public double SmokeBurstSeconds { get; init; } = 3;

    /// <summary>Clic sur la scène qui joue déjà : l'arrêter (défaut) ou la relancer (LIVE-003).</summary>
    public ActiveSceneClick ActiveSceneClick { get; init; } = ActiveSceneClick.Stop;

    /// <summary>Couches masquées en Live (aucune par défaut).</summary>
    public IReadOnlyList<Guid> HiddenLayerIds { get; init; } = [];
}
