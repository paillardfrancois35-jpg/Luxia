using Luxia.Messaging.Commands;

namespace Luxia.Messaging.Events;

/// <summary>EVT-010 <c>ScèneDémarrée</c> (MOT-101).</summary>
/// <param name="SceneId">Scène.</param>
/// <param name="SceneName">Nom de la scène.</param>
/// <param name="LayerId">Couche où elle joue.</param>
/// <param name="Origin">Origine de la commande qui l'a lancée.</param>
/// <param name="At">Instant (horloge du moteur).</param>
public sealed record SceneStarted(Guid SceneId, string SceneName, Guid LayerId, CommandOrigin Origin, TimeSpan At);
