namespace Dmx.Core.Time;

/// <summary>
/// Horloge monotone injectable (GEN-033). Jamais l'heure murale : elle sert à cadencer et à mesurer des durées.
/// </summary>
public interface IClock
{
    /// <summary>Temps écoulé depuis une origine arbitraire et fixe.</summary>
    TimeSpan Now { get; }
}
