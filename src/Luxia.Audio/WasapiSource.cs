using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;

namespace Luxia.Audio;

/// <summary>
/// Source WASAPI. Sans périphérique choisi : le son joué par le PC (AUD-001), boucle sur la sortie par défaut ; quand celle-ci
/// change (casque, enceintes, Bluetooth), la source s'arrête avec <c>null</c> et l'écoute la recrée sur la nouvelle (AUD-002).
/// Avec un périphérique de sortie choisi : la boucle de celui-ci ; avec une entrée (micro, ligne) : sa capture (AUD-003).
/// </summary>
public sealed class WasapiSource : IAudioSource, IMMNotificationClient
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private IWaveIn _capture;
    private readonly bool _followsDefault;
    private readonly MMDevice _device;
    private float[] _mono = new float[4096];
    private bool _disposed;
    private bool _stopping;

    /// <summary>Ouvre la source.</summary>
    /// <param name="deviceId">Identifiant du périphérique (entrée ou sortie), ou <c>null</c> pour suivre la sortie par défaut.</param>
    public WasapiSource(string? deviceId = null)
    {
        _followsDefault = deviceId is null;
        _device = deviceId is null ? _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia) : _enumerator.GetDevice(deviceId);
        Name = _device.FriendlyName;
        _capture = CreateCapture(eventSync: true);
        SampleRate = _capture.WaveFormat.SampleRate;
        _enumerator.RegisterEndpointNotificationCallback(this);
    }

    /// <summary>
    /// Une entrée (micro, ligne) se capture sur événement, par blocs de 10 ms, au lieu des blocs d'environ 60 ms du mode par
    /// défaut : 50 ms de moins en moyenne entre le son et son analyse (essai P7, exemple 18). La boucle d'une sortie garde le mode par défaut.
    /// </summary>
    private IWaveIn CreateCapture(bool eventSync)
    {
        IWaveIn capture = _device.DataFlow == DataFlow.Capture
            ? (eventSync ? new WasapiCapture(_device, true, 30) : new WasapiCapture(_device))
            : new WasapiLoopbackCapture(_device);
        capture.DataAvailable += OnData;
        capture.RecordingStopped += OnStopped;
        return capture;
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
    public void StartCapture()
    {
        try
        {
            _capture.StartRecording();
        }
        catch (Exception) when (_device.DataFlow == DataFlow.Capture)
        {
            // Certains périphériques refusent le mode sur événement : on retombe sur le mode par défaut.
            _capture.DataAvailable -= OnData;
            _capture.RecordingStopped -= OnStopped;
            _capture.Dispose();
            _capture = CreateCapture(eventSync: false);
            _capture.StartRecording();
        }
    }

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
        if (_followsDefault && flow == DataFlow.Render && role == Role.Multimedia && defaultDeviceId != _device.ID && !_stopping)
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
        try
        {
            Convert(e);
        }
        catch (Exception ex)
        {
            // AUD-006 : une erreur de conversion ou d'abonné s'arrête ici, l'écoute la traite comme une capture interrompue.
            Stopped?.Invoke(this, ex);
        }
    }

    private void Convert(WaveInEventArgs e)
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
