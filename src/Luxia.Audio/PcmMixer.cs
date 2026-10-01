namespace Luxia.Audio;

/// <summary>Conversion d'un tampon PCM entrelacé (16, 24, 32 bits entiers ou 32 bits flottants) en son mono flottant (−1 à 1), par moyenne des canaux.</summary>
public static class PcmMixer
{
    /// <summary>Convertit <paramref name="bytes"/> octets de <paramref name="buffer"/> dans <paramref name="mono"/> ; renvoie le nombre d'échantillons mono écrits.</summary>
    /// <param name="buffer">Tampon reçu de la capture.</param>
    /// <param name="bytes">Nombre d'octets valides.</param>
    /// <param name="channels">Nombre de canaux entrelacés (1 ou plus).</param>
    /// <param name="bitsPerSample">Taille d'un échantillon : 16, 24 ou 32 bits.</param>
    /// <param name="isFloat">Les échantillons de 32 bits sont des flottants plutôt que des entiers.</param>
    /// <param name="mono">Destination, au moins aussi longue que le nombre d'images.</param>
    public static int ToMono(byte[] buffer, int bytes, int channels, int bitsPerSample, bool isFloat, float[] mono)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(mono);
        channels = Math.Max(1, channels);
        var bytesPerSample = bitsPerSample / 8;
        if (bytesPerSample is < 2 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(bitsPerSample), "16, 24 ou 32 bits attendus");
        }

        var frames = bytes / (bytesPerSample * channels);
        if (mono.Length < frames)
        {
            throw new ArgumentException("destination trop courte", nameof(mono));
        }

        for (var i = 0; i < frames; i++)
        {
            float sum = 0;
            for (var c = 0; c < channels; c++)
            {
                var offset = ((i * channels) + c) * bytesPerSample;
                sum += isFloat && bytesPerSample == 4
                    ? BitConverter.ToSingle(buffer, offset)
                    : bytesPerSample == 2
                        ? BitConverter.ToInt16(buffer, offset) / 32768f
                        : bytesPerSample == 3
                            ? (((buffer[offset] << 8) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 24)) >> 8) / 8388608f
                            : BitConverter.ToInt32(buffer, offset) / 2147483648f;
            }

            mono[i] = sum / channels;
        }

        return frames;
    }
}
