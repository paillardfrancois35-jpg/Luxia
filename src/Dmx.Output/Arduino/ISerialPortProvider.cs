namespace Dmx.Output.Arduino;

/// <summary>Port série présent sur le poste.</summary>
/// <param name="PortName">Nom (COM5).</param>
/// <param name="VendorId">Identifiant USB du fabricant, si connu.</param>
/// <param name="ProductId">Identifiant USB du produit, si connu.</param>
public sealed record SerialPortInfo(string PortName, int? VendorId, int? ProductId)
{
    /// <summary>Le port appartient à une carte Arduino (identifiants USB connus).</summary>
    public bool IsArduino => VendorId is 0x2341 or 0x2A03;

    /// <summary>Carte Arduino en mode programmation (chargeur de démarrage) : à ne pas sonder.</summary>
    public bool IsBootloader => IsArduino && ProductId is 0x0036;
}

/// <summary>Accès aux ports série, abstrait pour tester le pilote sans matériel.</summary>
public interface ISerialPortProvider
{
    /// <summary>Ports présents.</summary>
    IReadOnlyList<SerialPortInfo> GetPorts();

    /// <summary>Ouvre un port (DTR actif, jamais à 1200 bauds : SORT-012).</summary>
    ISerialConnection Open(string portName);
}

/// <summary>Liaison série ouverte.</summary>
public interface ISerialConnection : IDisposable
{
    /// <summary>Nom du port.</summary>
    string PortName { get; }

    /// <summary>Écrit des octets ; lève en cas d'échec (port disparu).</summary>
    void Write(ReadOnlySpan<byte> data);

    /// <summary>Lit les octets disponibles, en attendant au plus <paramref name="timeout"/> ; 0 si rien.</summary>
    int Read(Span<byte> buffer, TimeSpan timeout);

    /// <summary>Vide le tampon de réception.</summary>
    void DiscardInput();
}
