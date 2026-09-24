using Dmx.Messaging.Events;
using Dmx.Output.Recording;
using Microsoft.Extensions.Logging;

namespace Dmx.Output.Drivers;

/// <summary>
/// Pilote « Enregistreur » : écrit chaque trame horodatée dans un fichier <c>.dmxrec</c> (SORT-060).
/// S'active et se désactive à chaud en l'affectant au routeur ou en l'en retirant (SORT-061).
/// </summary>
public sealed class RecorderOutputDriver : OutputDriver
{
    /// <summary>Identifiant du pilote Enregistreur dans les préférences.</summary>
    public const string DriverId = "enregistreur";

    private readonly string _path;
    private readonly int _universe;
    private readonly double _rateHz;
    private RecordingWriter? _writer;
    private DateTime _lastFlush;

    /// <summary>Crée le pilote ; le fichier est créé à la connexion.</summary>
    public RecorderOutputDriver(string path, int universe, double rateHz, ILogger<RecorderOutputDriver>? logger = null)
        : base(DriverId, "Enregistreur", logger)
    {
        _path = path;
        _universe = universe;
        _rateHz = rateHz;
    }

    /// <summary>Chemin du fichier.</summary>
    public string FilePath => _path;

    /// <inheritdoc />
    protected override bool Connect()
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(_path));
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        var stream = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.Read, 64 * 1024);
        _writer = new RecordingWriter(stream, _universe, _rateHz, DateTime.UtcNow);
        _lastFlush = DateTime.UtcNow;
        SetState(OutputConnectionState.Connected, Path.GetFileName(_path));
        Logger.LogInformation("Enregistrement des trames dans {Fichier}", _path);
        return true;
    }

    /// <inheritdoc />
    protected override void Write(ReadOnlySpan<byte> frame, TimeSpan timestamp)
    {
        var writer = _writer ?? throw new InvalidOperationException("Enregistreur non ouvert.");
        writer.Append(frame, timestamp);

        // Vidage périodique : un arrêt brutal ne perd qu'une seconde au plus.
        if (DateTime.UtcNow - _lastFlush > TimeSpan.FromSeconds(1))
        {
            writer.Flush();
            _lastFlush = DateTime.UtcNow;
        }
    }

    /// <inheritdoc />
    protected override void Disconnect()
    {
        if (_writer is null)
        {
            return;
        }

        Logger.LogInformation("Enregistrement terminé : {Trames} trames dans {Fichier}", _writer.FrameCount, _path);
        _writer.Dispose();
        _writer = null;
    }
}
