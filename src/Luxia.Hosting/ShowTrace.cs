using System.Globalization;
using System.Text;
using Luxia.Messaging.Events;
using Luxia.Show.Rules;
using Luxia.Show.Runtime;

namespace Luxia.Hosting;

/// <summary>
/// TEMPORAIRE (essai P8, exemple 13) : trace de mise au point des shows, à retirer après l'essai. Écrit un fichier CSV
/// <c>Documents\LuXia\Journaux\trace-show-AAAAMMJJ-HHmmss.csv</c> : deux lignes « état » par seconde (tempo, mesure, énergie,
/// détections, étapes actives, transitions en attente), plus une ligne par événement musical, transition armée, étape activée
/// (motif et quantification), changement de tempo et démarrage ou arrêt d'un show.
/// </summary>
public sealed class ShowTrace : IDisposable
{
    private const int SampleMilliseconds = 100;
    private const int StateEverySamples = 5;
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    private readonly LuxiaRuntime _runtime;
    private readonly Lock _gate = new();
    private readonly List<IDisposable> _subscriptions = [];
    private readonly Dictionary<(Guid Show, int Index), DateTime> _armed = [];
    private StreamWriter? _writer;
    private Timer? _timer;
    private SequencerState _last = SequencerState.Empty;
    private int _samples;

    /// <summary>Crée la trace (arrêtée).</summary>
    public ShowTrace(LuxiaRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
    }

    /// <summary>Fichier en cours d'écriture (ou le dernier écrit), sinon <c>null</c>.</summary>
    public string? FilePath { get; private set; }

    /// <summary>La trace enregistre.</summary>
    public bool IsRunning => _writer is not null;

    /// <summary>Démarre un nouveau fichier ; rend son chemin.</summary>
    public string Start()
    {
        lock (_gate)
        {
            if (_writer is not null)
            {
                return FilePath!;
            }

            Directory.CreateDirectory(_runtime.Paths.Logs);
            FilePath = Path.Combine(_runtime.Paths.Logs, $"trace-show-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

            // BOM UTF-8 et point-virgule : le fichier s'ouvre tel quel dans Excel en français.
            _writer = new StreamWriter(FilePath, false, new UTF8Encoding(true)) { AutoFlush = true };
            _writer.WriteLine("heure;temps moteur (s);type;BPM;source tempo;confiance;mesure;temps;énergie;niveau;tendance;break en cours;silence;show;étapes actives;transitions en attente;détail");
            _armed.Clear();
            _last = _runtime.Sequencer.State;
            _samples = 0;
            _subscriptions.Add(_runtime.Bus.Subscribe<MusicEvent>(e => Row("événement", $"{Kind(e.Kind)} (niveau {e.EnergyLevel}, énergie {e.Energy:0.00})")));
            _subscriptions.Add(_runtime.Bus.Subscribe<ShowStepActivated>(OnStep));
            _subscriptions.Add(_runtime.Bus.Subscribe<ShowStateChanged>(e => Row("show", $"« {e.ShowName} » {(e.Running ? "démarre" : "s'arrête")}")));
            _subscriptions.Add(_runtime.Bus.Subscribe<TempoChanged>(e => Row("tempo", $"{e.Bpm:0.0} BPM ({e.Source}, confiance {e.Confidence:0.00})")));
            Row("début", "trace démarrée");
            _timer = new Timer(_ => Sample(), null, SampleMilliseconds, SampleMilliseconds);
            return FilePath;
        }
    }

    /// <summary>Arrête et ferme le fichier.</summary>
    public void Stop()
    {
        lock (_gate)
        {
            if (_writer is null)
            {
                return;
            }

            _timer?.Dispose();
            _timer = null;
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
            Write("fin", "trace arrêtée");
            _writer.Dispose();
            _writer = null;
        }
    }

    /// <inheritdoc />
    public void Dispose() => Stop();

    private void Sample()
    {
        lock (_gate)
        {
            if (_writer is null)
            {
                return;
            }

            var state = _runtime.Sequencer.State;
            foreach (var show in state.Shows)
            {
                foreach (var transition in show.Transitions.Where(t => t.Armed && !_armed.ContainsKey((show.ShowId, t.Index))))
                {
                    _armed[(show.ShowId, transition.Index)] = DateTime.Now;
                    Write("transition armée", $"« {show.Name} » {Transition(transition)} : condition vraie, part {ShowTexts.Quantize(transition.Quantize)} (dans {transition.BeatsLeft:0.0} temps)");
                }
            }

            foreach (var key in _armed.Keys.Where(k => !state.Shows.Any(s => s.ShowId == k.Show && s.Transitions.Any(t => t.Index == k.Index && t.Armed))).ToList())
            {
                _armed.Remove(key);
            }

            _last = state;
            if (++_samples >= StateEverySamples)
            {
                _samples = 0;
                Write("état", string.Empty);
            }
        }
    }

    private void OnStep(ShowStepActivated e)
    {
        lock (_gate)
        {
            // La transition qui a mené à l'étape : celle de l'état précédent dont l'aval contient l'étape.
            var show = _last.Shows.FirstOrDefault(s => s.ShowId == e.ShowId);
            var transition = show?.Transitions.FirstOrDefault(t => t.To.Contains(e.StepId));
            var detail = $"« {e.ShowName} » → {e.StepId} {e.StepName} ; motif : {e.Reason}";
            if (transition is not null)
            {
                detail += $" ; quantification : {ShowTexts.Quantize(transition.Quantize)}";
                if (show is not null && _armed.TryGetValue((show.ShowId, transition.Index), out var since))
                {
                    detail += string.Create(Fr, $" ; armée depuis {(DateTime.Now - since).TotalSeconds:0.00} s");
                }
            }

            Write("étape", detail);
        }
    }

    private void Row(string type, string detail)
    {
        lock (_gate)
        {
            Write(type, detail);
        }
    }

    // Appelé sous le verrou.
    private void Write(string type, string detail)
    {
        if (_writer is null)
        {
            return;
        }

        var tempo = _runtime.Engine.Snapshot.Tempo;
        var audio = _runtime.Audio?.State;
        var state = _runtime.Sequencer.State;
        var main = state.MainShow;
        string[] cells =
        [
            DateTime.Now.ToString("HH:mm:ss.fff", Fr),
            _runtime.Clock.Now.TotalSeconds.ToString("0.000", Fr),
            type,
            tempo.Bpm.ToString("0.0", Fr),
            tempo.Source.ToString(),
            tempo.Confidence.ToString("0.00", Fr),
            tempo.Bar.ToString(Fr),
            tempo.BeatInBar.ToString(Fr),
            audio is null ? string.Empty : audio.Energy.ToString("0.00", Fr),
            audio is null ? string.Empty : audio.EnergyLevel.ToString(),
            audio is null ? string.Empty : audio.Trend.ToString(),
            audio is null ? string.Empty : audio.InBreak ? "oui" : "non",
            audio is null ? string.Empty : audio.Silent ? "oui" : "non",
            main?.Name ?? string.Empty,
            main is null ? string.Empty : string.Join(" + ", main.ActiveSteps.Select(s => $"{s.Id} {s.Name}")),
            main is null ? string.Empty : string.Join(" | ", main.Transitions.Select(t => $"{Transition(t)} si {t.Condition}, {ShowTexts.Quantize(t.Quantize)}{(t.Armed ? " (armée)" : string.Empty)}")),
            detail,
        ];
        _writer.WriteLine(string.Join(';', cells.Select(Cell)));
    }

    private static string Transition(TransitionStatus t) => $"{string.Join("+", t.From)} → {string.Join("+", t.To)}";

    private static string Kind(MusicEventKind kind) => kind switch
    {
        MusicEventKind.Drop => "DROP",
        MusicEventKind.Break => "BREAK",
        MusicEventKind.BuildUp => "MONTÉE",
        MusicEventKind.Silence => "SILENCE",
        MusicEventKind.Resumed => "REPRISE DU SON",
        MusicEventKind.EnergyChanged => "niveau d'énergie",
        _ => kind.ToString(),
    };

    private static string Cell(string value) =>
        value.Contains(';', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal) ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
}
