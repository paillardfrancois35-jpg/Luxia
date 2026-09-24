using Dmx.Messaging.Events;
using Microsoft.Extensions.Logging;

namespace Dmx.Output.Drivers;

/// <summary>Pilote « Nul » : accepte tout, n'émet rien. Permet de tout faire tourner sans matériel (doc 10 §2).</summary>
public sealed class NullOutputDriver : OutputDriver
{
    /// <summary>Identifiant du pilote Nul dans les préférences.</summary>
    public const string DriverId = "nul";

    private long _framesWritten;

    /// <summary>Crée le pilote.</summary>
    public NullOutputDriver(ILogger<NullOutputDriver>? logger = null)
        : base(DriverId, "Nul", logger)
    {
    }

    /// <summary>Nombre de trames « émises » (tests).</summary>
    public long FramesWritten => Interlocked.Read(ref _framesWritten);

    /// <inheritdoc />
    protected override bool Connect()
    {
        SetState(OutputConnectionState.Connected, "aucun matériel");
        return true;
    }

    /// <inheritdoc />
    protected override void Write(ReadOnlySpan<byte> frame, TimeSpan timestamp) => Interlocked.Increment(ref _framesWritten);

    /// <inheritdoc />
    protected override void Disconnect()
    {
    }
}
