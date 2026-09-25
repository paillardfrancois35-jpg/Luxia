namespace Dmx.Fixtures.Model;

/// <summary>Position d'un mode : un canal (ou l'octet fin d'un canal 16 bits).</summary>
/// <param name="Channel">Clé de la définition de canal.</param>
/// <param name="Part">Octet grossier ou fin.</param>
public sealed record ModeChannel(string Channel, ChannelPart Part = ChannelPart.Coarse);
