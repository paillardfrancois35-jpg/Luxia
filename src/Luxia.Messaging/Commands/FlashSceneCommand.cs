namespace Luxia.Messaging.Commands;

/// <summary>
/// CMD-014 <c>FlashScène</c> : la scène est appliquée instantanément au-dessus de toutes les couches tant que la commande
/// est maintenue (appui), et rend la main au relâchement (MOT-072, COU-005).
/// </summary>
/// <param name="Origin">Origine.</param>
/// <param name="SceneId">Scène.</param>
/// <param name="Pressed">Appui (<c>true</c>) ou relâche (<c>false</c>).</param>
/// <param name="ReleaseFade">Fondu au relâchement ; <c>null</c> = instantané.</param>
public sealed record FlashSceneCommand(CommandOrigin Origin, Guid SceneId, bool Pressed, TimeSpan? ReleaseFade = null) : Command(Origin);
