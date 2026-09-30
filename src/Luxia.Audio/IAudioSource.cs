namespace Luxia.Audio;

/// <summary>
/// Source de son mono en flottants (−1 à 1) : boucle du son joué par le PC (AUD-001), périphérique d'entrée (AUD-003) ou
/// fichier pour les tests. Les échantillons arrivent sur le fil propre de la source (AUD-006).
/// </summary>
public interface IAudioSource : IDisposable
{
    /// <summary>Nom lisible du périphérique écouté.</summary>
    string Name { get; }

    /// <summary>Fréquence d'échantillonnage du son mono fourni.</summary>
    int SampleRate { get; }

    /// <summary>Échantillons mono disponibles (appelé sur le fil de la source).</summary>
    event EventHandler<AudioBlock>? BlockAvailable;

    /// <summary>La source s'est arrêtée : <c>null</c> = arrêt demandé ou changement de périphérique, sinon l'erreur.</summary>
    event EventHandler<Exception?>? Stopped;

    /// <summary>Démarre la capture.</summary>
    void StartCapture();

    /// <summary>Arrête la capture.</summary>
    void StopCapture();
}

/// <summary>Bloc d'échantillons mono.</summary>
/// <param name="Samples">Échantillons (valides seulement pendant l'appel de l'abonné).</param>
public readonly record struct AudioBlock(ReadOnlyMemory<float> Samples);

/// <summary>Périphérique audio proposé à l'écoute (AUD-003).</summary>
/// <param name="Id">Identifiant système.</param>
/// <param name="Name">Nom lisible.</param>
/// <param name="IsInput">Entrée (micro, ligne) ; sinon sortie dont on écoute le son joué.</param>
public sealed record AudioDeviceInfo(string Id, string Name, bool IsInput);

/// <summary>Fabrique de sources : l'écoute recrée la source à chaque reconnexion (AUD-002, AUD-006).</summary>
public interface IAudioSourceFactory
{
    /// <summary>Crée la source du son joué par le PC, sur le périphérique de sortie par défaut du moment.</summary>
    IAudioSource CreateLoopback();

    /// <summary>Crée la source d'un périphérique choisi (AUD-003) ; <c>null</c> = le son joué par le PC (sortie par défaut).</summary>
    IAudioSource Create(string? deviceId);

    /// <summary>Périphériques actifs : sorties dont on peut écouter le son, et entrées (micro, ligne).</summary>
    IReadOnlyList<AudioDeviceInfo> Devices();
}
