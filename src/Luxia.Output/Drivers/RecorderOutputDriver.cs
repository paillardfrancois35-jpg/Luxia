using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Luxia.Messaging.Events;
using Luxia.Output.Recording;
using Microsoft.Extensions.Logging;

namespace Luxia.Output.Drivers;

/// <summary>
/// Pilote « Enregistreur » : écrit chaque trame horodatée dans un fichier <c>.dmxrec</c> (SORT-060).
/// S'active et se désactive à chaud en l'affectant au routeur ou en l'en retirant (SORT-061).
/// À côté, un journal lisible (<c>.journal.txt</c>, SORT-066) entrelace sur la même horloge les actions de l'utilisateur,
/// les commandes traitées par le moteur et chaque changement de canal envoyé aux sorties.
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
    private readonly ConcurrentQueue<(TimeSpan Elapsed, DateTime At, string Kind, string Text)> _notes = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DateTime _clockStart = DateTime.Now;
    private StreamWriter? _journal;
    private byte[]? _previous;
    private readonly StringBuilder _line = new();

    /// <summary>Nombre maximal de canaux détaillés sur une ligne DMX du journal.</summary>
    public const int MaxChannelsPerLine = 64;

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

    /// <summary>Journal lisible écrit à côté des trames (SORT-066).</summary>
    public string JournalPath => JournalPathFor(_path);

    /// <summary>Chemin du journal d'un enregistrement.</summary>
    public static string JournalPathFor(string recording) => Path.ChangeExtension(recording, ".journal.txt");

    /// <summary>
    /// Ajoute un événement au journal (thread-safe, non bloquant), horodaté à l'instant de l'appel : il s'intercale
    /// entre les lignes DMX dans l'ordre réel.
    /// </summary>
    /// <param name="kind">Nature : « IHM », « MOTEUR »…</param>
    /// <param name="text">Description.</param>
    public void Note(string kind, string text) => _notes.Enqueue((_clock.Elapsed, _clockStart + _clock.Elapsed, kind, text));

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
        _journal = new StreamWriter(JournalPath, false, new UTF8Encoding(true));
        _journal.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Journal de l'enregistrement {Path.GetFileName(_path)} – univers {_universe}, {_rateHz:0} trames/s"));
        _journal.WriteLine("Heure         Écart (s)  Nature  Détail   (DMX : canal:avant→après, seulement les canaux qui changent)");
        _previous = null;
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
        WriteJournal(frame);

        // Vidage périodique : un arrêt brutal ne perd qu'une seconde au plus.
        if (DateTime.UtcNow - _lastFlush > TimeSpan.FromSeconds(1))
        {
            writer.Flush();
            _journal?.Flush();
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
        DrainNotes();
        _journal?.Dispose();
        _journal = null;
    }

    private void WriteJournal(ReadOnlySpan<byte> frame)
    {
        if (_journal is null)
        {
            return;
        }

        // Les événements arrivés avant cette trame passent d'abord : l'ordre du fichier est l'ordre réel.
        DrainNotes();
        var now = _clock.Elapsed;
        var previous = _previous;
        _line.Clear();
        var changed = 0;
        for (var i = 0; i < frame.Length; i++)
        {
            var before = previous is null ? (byte)0 : previous[i];
            if (frame[i] != before || (previous is null && frame[i] != 0))
            {
                if (++changed <= MaxChannelsPerLine)
                {
                    _line.Append(CultureInfo.InvariantCulture, $"{i + 1}:{before}→{frame[i]} ");
                }
            }
        }

        if (changed > MaxChannelsPerLine)
        {
            _line.Append(CultureInfo.InvariantCulture, $"… ({changed} canaux au total)");
        }

        if (changed > 0)
        {
            WriteLine(now, _clockStart + now, previous is null ? "DMX*" : "DMX", _line.ToString().TrimEnd());
        }

        if (previous is null || previous.Length != frame.Length)
        {
            _previous = frame.ToArray();
        }
        else
        {
            frame.CopyTo(previous);
        }
    }

    private void DrainNotes()
    {
        while (_notes.TryDequeue(out var note))
        {
            WriteLine(note.Elapsed, note.At, note.Kind, note.Text);
        }
    }

    private void WriteLine(TimeSpan elapsed, DateTime at, string kind, string text) =>
        _journal?.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{at:HH:mm:ss.fff}  {elapsed.TotalSeconds,9:0.000}  {kind,-6}  {text}"));
}
