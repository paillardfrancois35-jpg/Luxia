namespace Luxia.Audio.Tests;

/// <summary>Conversion du son reçu de la capture (16, 24, 32 bits, flottant, plusieurs canaux) en mono (AUD-001, AUD-003).</summary>
public sealed class PcmMixerTests
{
    [Fact]
    [Trait("Exigence", "AUD-001")]
    public void Stereo16Bit_IsAveragedToMono()
    {
        byte[] data = [.. BitConverter.GetBytes((short)16384), .. BitConverter.GetBytes((short)-16384), .. BitConverter.GetBytes((short)32767), .. BitConverter.GetBytes((short)32767)];
        var mono = new float[4];

        var count = PcmMixer.ToMono(data, data.Length, 2, 16, false, mono);

        count.ShouldBe(2);
        mono[0].ShouldBe(0, 1e-6);
        mono[1].ShouldBe(32767 / 32768f, 1e-6);
    }

    [Fact]
    [Trait("Exigence", "AUD-001")]
    public void Mono24Bit_KeepsTheSignAndScale()
    {
        // +0,5 et −0,5 en 24 bits (4 194 304 = 0x400000).
        byte[] data = [0x00, 0x00, 0x40, 0x00, 0x00, 0xC0];
        var mono = new float[2];

        PcmMixer.ToMono(data, data.Length, 1, 24, false, mono).ShouldBe(2);

        mono[0].ShouldBe(0.5f, 1e-6);
        mono[1].ShouldBe(-0.5f, 1e-6);
    }

    [Fact]
    [Trait("Exigence", "AUD-001")]
    public void Float32AndInt32_AreBothSupported()
    {
        byte[] floats = [.. BitConverter.GetBytes(0.25f), .. BitConverter.GetBytes(0.75f)];
        var mono = new float[1];
        PcmMixer.ToMono(floats, floats.Length, 2, 32, true, mono).ShouldBe(1);
        mono[0].ShouldBe(0.5f, 1e-6);

        byte[] ints = BitConverter.GetBytes(int.MinValue / 2);
        PcmMixer.ToMono(ints, ints.Length, 1, 32, false, mono).ShouldBe(1);
        mono[0].ShouldBe(-0.5f, 1e-6);
    }

    [Fact]
    [Trait("Exigence", "AUD-001")]
    public void SurroundAndPartialFrames_AreHandled()
    {
        // Six canaux 16 bits, 1,5 image reçue : une seule image complète est convertie.
        var data = new byte[18];
        for (var c = 0; c < 6; c++)
        {
            BitConverter.GetBytes((short)(c % 2 == 0 ? 16384 : 0)).CopyTo(data, c * 2);
        }

        var mono = new float[2];

        PcmMixer.ToMono(data, data.Length, 6, 16, false, mono).ShouldBe(1);
        mono[0].ShouldBe(0.25f, 1e-6); // trois canaux à 0,5, trois à 0 : moyenne 0,25

        Should.Throw<ArgumentOutOfRangeException>(() => PcmMixer.ToMono(data, data.Length, 2, 8, false, mono));
    }
}
