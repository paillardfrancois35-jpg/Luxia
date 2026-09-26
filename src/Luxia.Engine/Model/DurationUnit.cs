namespace Luxia.Engine.Model;

/// <summary>Unité d'une durée (GEN-023).</summary>
public enum DurationUnit
{
    /// <summary>Secondes (résolution 1 ms).</summary>
    Seconds,

    /// <summary>Temps musicaux (fractions acceptées : 0,5 = une croche à 4/4).</summary>
    Beats,

    /// <summary>Mesures de 4 temps (GEN-024 ; les mesures à 3 temps viendront avec GEN-025, P8).</summary>
    Bars,
}
