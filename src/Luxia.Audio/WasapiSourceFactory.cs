namespace Luxia.Audio;

/// <summary>Fabrique de la source réelle : boucle WASAPI sur le périphérique de sortie par défaut.</summary>
public sealed class WasapiSourceFactory : IAudioSourceFactory
{
    /// <inheritdoc />
    public IAudioSource CreateLoopback() => new WasapiLoopbackSource();
}
