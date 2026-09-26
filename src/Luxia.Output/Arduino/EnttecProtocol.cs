using System.Text;
using Luxia.Core.Dmx;

namespace Luxia.Output.Arduino;

/// <summary>
/// Sous-ensemble du protocole Enttec DMX USB Pro utilisé entre le PC et l'Arduino (doc 10 §5).
/// Message : <c>0x7E | label | longueur (LSB, MSB) | données | 0xE7</c>.
/// </summary>
public static class EnttecProtocol
{
    /// <summary>Début de message.</summary>
    public const byte StartOfMessage = 0x7E;

    /// <summary>Fin de message.</summary>
    public const byte EndOfMessage = 0xE7;

    /// <summary>Longueur maximale des données (start code + 512 canaux).</summary>
    public const int MaxDataLength = DmxConstants.ChannelCount + 1;

    /// <summary>Taille de l'enveloppe (début, label, 2 octets de longueur, fin).</summary>
    public const int EnvelopeLength = 5;

    /// <summary>Label 3 : <i>Get Widget Parameters</i>.</summary>
    public const byte LabelGetParameters = 3;

    /// <summary>Label 6 : <i>Output Only Send DMX</i> (start code + valeurs).</summary>
    public const byte LabelSendDmx = 6;

    /// <summary>Label 10 : <i>Get Widget Serial Number</i>.</summary>
    public const byte LabelGetSerialNumber = 10;

    /// <summary>Label 0x11 : ancien format du POC (valeurs sans start code), accepté pendant la transition.</summary>
    public const byte LabelLegacySendDmx = 0x11;

    /// <summary>Label 77 : identification étendue propre au projet.</summary>
    public const byte LabelIdentify = 77;

    /// <summary>Préfixe de la réponse d'identification du firmware du projet.</summary>
    public const string IdentityPrefix = "DMX-LEONARDO";

    /// <summary>Encode un message complet dans <paramref name="destination"/> ; renvoie le nombre d'octets écrits.</summary>
    public static int Encode(byte label, ReadOnlySpan<byte> data, Span<byte> destination)
    {
        if (data.Length > MaxDataLength)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "Données trop longues pour un message.");
        }

        destination[0] = StartOfMessage;
        destination[1] = label;
        destination[2] = (byte)(data.Length & 0xFF);
        destination[3] = (byte)(data.Length >> 8);
        data.CopyTo(destination[4..]);
        destination[4 + data.Length] = EndOfMessage;
        return data.Length + EnvelopeLength;
    }

    /// <summary>Encode un message dans un nouveau tableau (hors chemin temps réel).</summary>
    public static byte[] Encode(byte label, ReadOnlySpan<byte> data)
    {
        var buffer = new byte[data.Length + EnvelopeLength];
        Encode(label, data, buffer);
        return buffer;
    }

    /// <summary>
    /// Encode une trame DMX au label 6 (start code 0 + <paramref name="channelCount"/> canaux) sans allocation.
    /// </summary>
    public static int EncodeSendDmx(ReadOnlySpan<byte> frame, int channelCount, Span<byte> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(channelCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(channelCount, DmxConstants.ChannelCount);
        var length = channelCount + 1;
        destination[0] = StartOfMessage;
        destination[1] = LabelSendDmx;
        destination[2] = (byte)(length & 0xFF);
        destination[3] = (byte)(length >> 8);
        destination[4] = 0; // start code « éclairage »
        frame[..channelCount].CopyTo(destination[5..]);
        destination[4 + length] = EndOfMessage;
        return length + EnvelopeLength;
    }

    /// <summary>Encode une trame au format du POC (label 0x11, sans start code), pour SORT-014.</summary>
    public static int EncodeLegacySendDmx(ReadOnlySpan<byte> frame, int channelCount, Span<byte> destination) =>
        Encode(LabelLegacySendDmx, frame[..channelCount], destination);

    /// <summary>Taille maximale d'un message encodé.</summary>
    public const int MaxMessageLength = MaxDataLength + EnvelopeLength;

    /// <summary>Informations d'identification renvoyées par le firmware (label 77).</summary>
    /// <param name="Name">Nom du firmware (« DMX-LEONARDO »).</param>
    /// <param name="FirmwareVersion">Version (ex. 1.0).</param>
    /// <param name="ChannelCount">Nombre de canaux gérés.</param>
    public sealed record Identity(string Name, Version FirmwareVersion, int ChannelCount);

    /// <summary>Lit la réponse texte du label 77 : « DMX-LEONARDO;fw=1.0;ch=512 ».</summary>
    public static bool TryParseIdentity(ReadOnlySpan<byte> data, out Identity? identity)
    {
        identity = null;
        var text = Encoding.ASCII.GetString(data);
        var parts = text.Split(';', StringSplitOptions.TrimEntries);
        if (parts.Length == 0 || !parts[0].Equals(IdentityPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var version = new Version(0, 0);
        var channels = DmxConstants.ChannelCount;
        foreach (var part in parts.Skip(1))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2)
            {
                continue;
            }

            if (kv[0] == "fw" && Version.TryParse(kv[1], out var v))
            {
                version = v;
            }
            else if (kv[0] == "ch" && int.TryParse(kv[1], System.Globalization.CultureInfo.InvariantCulture, out var c))
            {
                channels = c;
            }
        }

        identity = new Identity(parts[0], version, channels);
        return true;
    }
}
