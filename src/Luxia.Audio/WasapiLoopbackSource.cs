using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;

namespace Luxia.Audio;

/// <summary>
/// Son joué par le PC (AUD-001) : boucle WASAPI sur le périphérique de sortie par défaut. Quand celui-ci change
/// (casque, enceintes, Bluetooth), la source s'arrête avec <c>null</c> et l'écoute la recrée sur le nouveau (AUD-002).
/// </summary>
public sealed class WasapiLoopbackSource : IAudioSource, IMMNotificationClient
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private readonly WasapiLoopbackCapture _capture;
    private readonly MMDevice _device;
    private float[] _mono = new float[4096];
    private bool _disposed;
    private bool _stopping;

    /// <summary>Ouvre la boucle sur le périphérique de sortie par défaut.</summary>
    public WasapiLoopbackSource()
    {
        _device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        Name = _device.FriendlyName;
        _capture = new WasapiLoopbackCapture(_device);
        SampleRate = _capture.WaveFormat.SampleRate;
        _capture.DataAvailable += OnData;
        _capture.RecordingStopped += OnStopped;
        _enumerator.RegisterEndpointNotificationCallback(this);
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public int SampleRate { get; }

    /// <inheritdoc />
    public event EventHandler<AudioBlock>? BlockAvailable;

    /// <inheritdoc />
    public event EventHandler<Exception?>? Stopped;

    /// <inheritdoc />
    public void StartCapture() => _capture.StartRecording();

    /// <inheritdoc />
    public void StopCapture()
    {
        _stopping = true;
        try
        {
            _capture.StopRecording();
        }
        catch (InvalidOperationException)
        {
            // Déjà arrêtée.
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            _enumerator.UnregisterEndpointNotificationCallback(this);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            // Le système ne connaît plus l'abonnement : rien à défaire.
        }

        _capture.DataAvailable -= OnData;
        _capture.RecordingStopped -= OnStopped;
        _capture.Dispose();
        _device.Dispose();
        _enumerator.Dispose();
    }

    /// <inheritdoc />
    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow == DataFlow.Render && role == Role.Multimedia && defaultDeviceId != _device.ID && !_stopping)
        {
            // AUD-002 : on s'arrête proprement ; l'écoute se rebranche sur le nouveau périphérique par défaut.
            StopCapture();
        }
    }

    /// <inheritdoc />
    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        if (deviceId == _device.ID && newState != DeviceState.Active && !_stopping)
        {
            StopCapture();
        }
    }

    /// <inheritdoc />
    public void OnDeviceAdded(string pwstrDeviceId)
    {
    }

    /// <inheritdoc />
    public void OnDeviceRemoved(string deviceId)
    {
        if (deviceId == _device.ID && !_stopping)
        {
            StopCapture();
        }
    }

    /// <inheritdoc />
    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {
    }

    private void OnData(object? sender, WaveInEventArgs e)
    {
        var format = _capture.WaveFormat;
        var channels = Math.Max(1, format.Channels);
        var bytesPerSample = format.BitsPerSample / 8;
        var frames = e.BytesRecorded / (bytesPerSample * channels);
        if (frames <= 0)
        {
            return;
        }

        if (_mono.Length < frames)
        {
            _mono = new float[frames];
        }

        var standard = format is WaveFormatExtensible extensible ? extensible.ToStandardWaveFormat() : format;
        var floatFormat = standard.Encoding == WaveFormatEncoding.IeeeFloat;
        var buffer = e.Buffer;
        for (var i = 0; i < frames; i++)
        {
            float sum = 0;
            for (var c = 0; c < channels; c++)
            {
                var offset = ((i * channels) + c) * bytesPerSample;
                sum += floatFormat && bytesPerSample == 4
                    ? BitConverter.ToSingle(buffer, offset)
                    : bytesPerSample == 2
                        ? BitConverter.ToInt16(buffer, offset) / 32768f
                        : bytesPerSample == 3
                            ? (((buffer[offset] << 8) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 24)) >> 8) / 8388608f
                            : BitConverter.ToInt32(buffer, offset) / 2147483648f;
            }

            _mono[i] = sum / channels;
        }

        BlockAvailable?.Invoke(this, new AudioBlock(_mono.AsMemory(0, frames)));
    }

    private void OnStopped(object? sender, StoppedEventArgs e) => Stopped?.Invoke(this, e.Exception);
}
