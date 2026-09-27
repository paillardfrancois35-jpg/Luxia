using Luxia.Patch.Model;
using Luxia.Scenes.Model;

namespace Luxia.UI.Modules.Control;

/// <summary>
/// État annulable de l'écran Contrôle (F3) : les scènes et les lieux (zones). Ce qui a été <b>joué</b> ne s'annule
/// jamais : lancer une scène n'entre pas dans l'historique.
/// </summary>
/// <param name="Scenes">Scènes.</param>
/// <param name="Venues">Lieux (zones interdites et permises).</param>
public sealed record ControlSnapshot(SceneSet Scenes, VenueSet Venues);
