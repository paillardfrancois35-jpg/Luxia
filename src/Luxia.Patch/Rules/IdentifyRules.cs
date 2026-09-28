using Luxia.Fixtures.Model;

namespace Luxia.Patch.Rules;

/// <summary>
/// Calcule les canaux à allumer pour « Identifier » un appareil (CMD-023, INST-019, CONS-024, SIM-009) :
/// les canaux qui ont une valeur d'identification propre (<see cref="ChannelDefinition.Identify"/>), sinon
/// les canaux d'intensité (gradateur, intensité de cellule) poussés au maximum, ainsi que les émetteurs de
/// couleur (RVB, blanc, ambre, UV…, <see cref="AttributeInfo.IsEmitter"/>) : un gradateur seul ne rend rien
/// visible si le rouge/vert/bleu sont à 0, il faut aussi pousser la couleur. Les autres attributs
/// (position, roues, macros…) ne sont pas touchés : identifier un appareil ne doit ni le déplacer ni changer
/// sa teinte au-delà de la rendre visible.
/// </summary>
public static class IdentifyRules
{
    /// <summary>Canaux absolus (1-512) et valeur à émettre pendant l'identification.</summary>
    public static IReadOnlyList<(int Channel, byte Value)> IdentifyChannels(FixtureType fixture, FixtureMode mode, int address)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(mode);
        var result = new List<(int, byte)>();
        for (var i = 0; i < mode.Channels.Count; i++)
        {
            var slot = mode.Channels[i];
            if (slot.Part == ChannelPart.Fine)
            {
                continue;
            }

            var definition = fixture.Channel(slot.Channel);
            if (definition is null)
            {
                continue;
            }

            var info = AttributeCatalog.Get(definition.Attribute);
            byte? value = definition.Identify is { } identify
                ? (byte)Math.Clamp(identify, 0, 255)
                : info.Family == AttributeFamily.Intensity || info.IsEmitter
                    ? (byte)255
                    : null;

            if (value is { } v)
            {
                result.Add((address + i, v));
            }
        }

        return result;
    }
}
