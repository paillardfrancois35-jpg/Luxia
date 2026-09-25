namespace Dmx.Core.Dmx;

/// <summary>Constantes du protocole DMX512 et de la cadence du moteur.</summary>
public static class DmxConstants
{
    /// <summary>Nombre de canaux d'un univers.</summary>
    public const int ChannelCount = 512;

    /// <summary>Fréquence de tick par défaut (GEN-030).</summary>
    public const double DefaultTickRateHz = 40.0;

    /// <summary>Fréquence de tick minimale réglable (GEN-030).</summary>
    public const double MinTickRateHz = 25.0;

    /// <summary>Fréquence de tick maximale réglable (GEN-030).</summary>
    public const double MaxTickRateHz = 44.0;
}
