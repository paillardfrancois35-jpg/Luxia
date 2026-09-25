namespace Dmx.Fixtures.Model;

/// <summary>Résolution d'un canal.</summary>
public enum ChannelResolution
{
    /// <summary>Un octet.</summary>
    Bit8,

    /// <summary>Deux octets (grossier + fin), positionnés indépendamment dans chaque mode (BIB-003).</summary>
    Bit16,
}
