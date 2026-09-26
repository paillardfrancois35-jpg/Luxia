namespace Luxia.Fixtures.Model;

/// <summary>Usage de l'émetteur blanc d'un appareil RVBW pour rendre une couleur logique (MOT-051).</summary>
public enum WhiteMode
{
    /// <summary>
    /// Défaut : le blanc reçoit la part commune min(R, V, B), retirée des couleurs (blanc logique → W = 100 %, RVB = 0).
    /// </summary>
    Extract,

    /// <summary>Blanc jamais utilisé par les couleurs logiques (seulement s'il est demandé explicitement).</summary>
    Off,

    /// <summary>Blanc prioritaire : le blanc reçoit min(R, V, B) sans rien retirer aux couleurs (plus lumineux, moins saturé).</summary>
    Boost,
}
