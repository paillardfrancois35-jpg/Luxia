using Dmx.Fixtures.Model;
using Dmx.Fixtures.Rules;

namespace Dmx.Patch.Rules;

/// <summary>
/// Calcule les canaux à allumer pour « Identifier » un appareil (CMD-023, INST-019, CONS-024, SIM-009) :
/// les canaux qui ont une valeur d'identification propre (<see cref="ChannelDefinition.Identify"/>), sinon
/// les canaux d'intensité (gradateur, intensité de cellule) poussés au maximum. Les autres attributs
/// (couleur, position, roues…) ne sont pas touchés : identifier un appareil ne doit ni le déplacer ni le
/// décolorer, seulement le rendre visible.
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

            byte? value = definition.Identify is { } identify
                ? (byte)Math.Clamp(identify, 0, 255)
                : AttributeCatalog.Get(definition.Attribute).Family == AttributeFamily.Intensity
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
