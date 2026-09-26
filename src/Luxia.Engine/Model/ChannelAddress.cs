namespace Luxia.Engine.Model;

/// <summary>Emplacement DMX d'un paramètre : univers, canal grossier et, en 16 bits, canal fin (MOT-090).</summary>
/// <param name="Universe">Univers (1 = premier).</param>
/// <param name="Coarse">Canal de l'octet grossier (ou unique), 1 à 512.</param>
/// <param name="Fine">Canal de l'octet fin d'un attribut 16 bits ; 0 = attribut 8 bits.</param>
public readonly record struct ChannelAddress(int Universe, int Coarse, int Fine = 0)
{
    /// <summary>Attribut sur deux octets.</summary>
    public bool Is16Bit => Fine > 0;
}
