namespace Luxia.Core.Dmx;

/// <summary>
/// Destination des trames calculées par le moteur (implémentée par le routeur de sorties).
/// Le moteur ne connaît que cette interface (P2, GEN-001).
/// </summary>
public interface IFrameSink
{
    /// <summary>
    /// Reçoit la trame d'un univers pour le tick courant. Doit rendre la main immédiatement :
    /// la trame est copiée, jamais conservée par référence (SORT-002, SORT-003).
    /// </summary>
    /// <param name="universe">Numéro d'univers (1 = premier).</param>
    /// <param name="frame">Trame du tick.</param>
    /// <param name="timestamp">Instant du tick (horloge du moteur).</param>
    void Submit(int universe, DmxFrame frame, TimeSpan timestamp);
}
