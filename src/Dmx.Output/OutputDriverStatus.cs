using Dmx.Messaging.Events;

namespace Dmx.Output;

/// <summary>État publié d'un pilote de sortie (SORT-004, SORT-023).</summary>
/// <param name="State">État de connexion.</param>
/// <param name="Message">Détail : port, firmware, ou message d'erreur.</param>
/// <param name="FramesPerSecond">Trames réellement émises par seconde (moyenne sur la dernière seconde).</param>
/// <param name="ErrorCount">Nombre d'erreurs depuis le démarrage.</param>
/// <param name="LastWriteDuration">Durée de la dernière écriture.</param>
public readonly record struct OutputDriverStatus(
    OutputConnectionState State,
    string? Message,
    double FramesPerSecond,
    long ErrorCount,
    TimeSpan LastWriteDuration);
