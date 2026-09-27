using Luxia.Fixtures.Model;
using Luxia.Patch.Model;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Compilation;

/// <summary>Tout ce qu'il faut pour compiler un projet vers le moteur (D26).</summary>
/// <param name="Installation">Patch, univers, sélections manuelles.</param>
/// <param name="Venues">Lieux (le lieu actif dit quels appareils sont absents).</param>
/// <param name="TypeOf">Modèle d'un appareil par son identifiant (copie du projet, GEN-053).</param>
/// <param name="Layers">Couches.</param>
/// <param name="Scenes">Scènes.</param>
/// <param name="Palettes">Palettes.</param>
/// <param name="Safety">Réglages de sûreté (<c>null</c> = valeurs par défaut).</param>
public sealed record ProjectContent(
    Installation Installation,
    VenueSet Venues,
    Func<Guid, FixtureType?> TypeOf,
    LayerSet Layers,
    SceneSet Scenes,
    PaletteSet Palettes,
    SafetySettings? Safety = null);
