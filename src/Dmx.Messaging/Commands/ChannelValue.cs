namespace Dmx.Messaging.Commands;

/// <summary>Valeur brute d'un canal (1 à 512).</summary>
/// <param name="Channel">Canal.</param>
/// <param name="Value">Valeur 0-255.</param>
public readonly record struct ChannelValue(int Channel, byte Value);
