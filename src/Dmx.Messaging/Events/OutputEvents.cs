namespace Dmx.Messaging.Events;

/// <summary>État de connexion d'un pilote de sortie (SORT-004).</summary>
public enum OutputConnectionState
{
    /// <summary>Aucune liaison.</summary>
    Disconnected,

    /// <summary>Recherche ou ouverture de la liaison en cours.</summary>
    Connecting,

    /// <summary>Liaison établie, trames émises.</summary>
    Connected,

    /// <summary>Erreur (le pilote tente de se reconnecter).</summary>
    Error,
}

/// <summary>
/// EVT-001 <c>SortieConnectée</c> / <c>SortieDéconnectée</c> : changement d'état d'un pilote de sortie.
/// </summary>
/// <param name="DriverId">Identifiant du pilote.</param>
/// <param name="DriverName">Nom affiché.</param>
/// <param name="State">Nouvel état.</param>
/// <param name="Message">Détail (port, version du firmware, message d'erreur).</param>
public sealed record OutputStateChanged(string DriverId, string DriverName, OutputConnectionState State, string? Message);
