using NAudio.CoreAudioApi;

namespace Luxia.Audio;

/// <summary>Fabrique de la source réelle : WASAPI (boucle du son joué par le PC, ou entrée choisie).</summary>
public sealed class WasapiSourceFactory : IAudioSourceFactory
{
    /// <inheritdoc />
    public IAudioSource CreateLoopback() => new WasapiSource();

    /// <inheritdoc />
    public IAudioSource Create(string? deviceId) => new WasapiSource(deviceId);

    /// <inheritdoc />
    public string? DefaultOutputId()
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return device.ID;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<AudioDeviceInfo> Devices()
    {
        using var enumerator = new MMDeviceEnumerator();
        var list = new List<AudioDeviceInfo>();
        foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active))
        {
            list.Add(new AudioDeviceInfo(device.ID, device.FriendlyName, device.DataFlow == DataFlow.Capture));
            device.Dispose();
        }

        return list;
    }
}
