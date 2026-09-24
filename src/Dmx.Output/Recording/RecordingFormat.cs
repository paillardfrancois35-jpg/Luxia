namespace Dmx.Output.Recording;

/// <summary>
/// Format binaire des enregistrements de trames (<c>.dmxrec</c>, SORT-060). Petit-boutiste.
/// </summary>
/// <remarks>
/// <code>
/// En-tête : "DMXREC" (6 octets ASCII) | version (uint16) | univers (uint16) | fréquence en Hz (float32)
///           | début de l'enregistrement, heure UTC en ticks .NET (int64) | nombre de canaux (uint16)
/// Trame   : temps écoulé depuis la première trame, en ms (uint32) | longueur (uint16) | valeurs
///           Longueur 0 = trame identique à la précédente (compacité : un univers au repos ne coûte que 6 octets par trame).
/// </code>
/// </remarks>
public static class RecordingFormat
{
    /// <summary>Signature du fichier.</summary>
    public static ReadOnlySpan<byte> Magic => "DMXREC"u8;

    /// <summary>Version courante du format.</summary>
    public const ushort Version = 1;

    /// <summary>Extension des fichiers.</summary>
    public const string Extension = ".dmxrec";
}

/// <summary>En-tête d'un enregistrement.</summary>
/// <param name="Version">Version du format.</param>
/// <param name="Universe">Univers enregistré.</param>
/// <param name="RateHz">Fréquence du moteur au moment de l'enregistrement.</param>
/// <param name="StartedUtc">Heure de début (information).</param>
/// <param name="ChannelCount">Nombre de canaux par trame.</param>
public sealed record RecordingHeader(ushort Version, ushort Universe, float RateHz, DateTime StartedUtc, ushort ChannelCount);

/// <summary>Trame relue.</summary>
/// <param name="Elapsed">Temps écoulé depuis la première trame (résolution 1 ms).</param>
/// <param name="Values">Valeurs des canaux.</param>
public sealed record RecordedFrame(TimeSpan Elapsed, byte[] Values);
