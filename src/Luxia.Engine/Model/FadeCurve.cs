namespace Luxia.Engine.Model;

/// <summary>Courbe d'un fondu (MOT-011).</summary>
public enum FadeCurve
{
    /// <summary>Progression régulière.</summary>
    Linear,

    /// <summary>Départ et arrivée adoucis (« en S »).</summary>
    SCurve,

    /// <summary>Saut immédiat, quelle que soit la durée du fondu.</summary>
    Instant,
}
