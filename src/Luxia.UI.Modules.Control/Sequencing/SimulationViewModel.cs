using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Engine;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Show.Runtime;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control.Sequencing;

/// <summary>
/// Essai d'une séquence ou d'un show sans musique (SHOW-007, SHOW-027) : jouer / arrêter le brouillon, métronome, événements
/// musicaux provoqués (drop, break, montée, silence, morceau suivant), énergie et style imposés. Agit sur le moteur de sortie,
/// ou sur l'aperçu quand la case Aveugle est cochée.
/// </summary>
public sealed partial class SimulationViewModel : ViewModelBase
{
    private static readonly double[] EnergyOfLevel = [0.15, 0.41, 0.62, 0.86];
    private readonly LuxiaRuntime _runtime;
    private readonly Func<bool> _blind;
    private readonly Func<bool, SequencerCommand> _play;
    private bool _releasing;
    private (TempoSourceKind Source, double Bpm)? _liveTempo;

    [ObservableProperty]
    private bool _useMetronome;

    [ObservableProperty]
    private decimal _metronomeBpm = 120;

    [ObservableProperty]
    private string _style = string.Empty;

    [ObservableProperty]
    private int _energyLevel = -1;

    [ObservableProperty]
    private string _status = "À l'arrêt.";

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private double? _playheadBars;

    /// <summary>Crée le panneau d'essai.</summary>
    /// <param name="runtime">Application.</param>
    /// <param name="blind">Case Aveugle cochée.</param>
    /// <param name="play">Commande qui lance (faux) ou arrête (vrai) le brouillon.</param>
    public SimulationViewModel(LuxiaRuntime runtime, Func<bool> blind, Func<bool, SequencerCommand> play)
    {
        _runtime = runtime;
        _blind = blind;
        _play = play;
    }

    /// <summary>Tempo de l'essai en clair.</summary>
    public string TempoText => UseMetronome ? "métronome" : string.Create(CultureInfo.CurrentCulture, $"celui du direct ({Target.Bpm:0} BPM)");

    private RenderEngine Target => _blind() ? _runtime.Preview : _runtime.Engine;

    private Sequencer TargetSequencer => _blind() ? _runtime.PreviewSequencer : _runtime.Sequencer;

    /// <summary>Joue le brouillon (au métronome si demandé).</summary>
    [RelayCommand]
    private void Play()
    {
        if (UseMetronome)
        {
            // Sur la sortie, le métronome change l'horloge du direct : on retient sa source et son tempo pour les rendre à la fin.
            if (!_blind() && _liveTempo is null)
            {
                var tempo = _runtime.Engine.Snapshot.Tempo;
                _liveTempo = (tempo.Source, tempo.Bpm);
            }

            _runtime.PreviewOwnTempo = _blind();
            Target.Send(new SetTempoSourceCommand(CommandOrigin.User, TempoSourceKind.Fixed, (double)Math.Clamp(MetronomeBpm, 20, 400)));
        }

        Target.Send(_play(false));
        _runtime.TraceUi("Essai", _blind() ? "jouer en aveugle" : "jouer sur la sortie");
    }

    /// <summary>Arrête le brouillon.</summary>
    [RelayCommand]
    private void Stop() => Target.Send(_play(true));

    /// <summary>Provoque un événement musical (drop, break, montee, silence, morceau).</summary>
    [RelayCommand]
    private void Cue(string? name)
    {
        var cue = name switch
        {
            "drop" => SimulatedCue.Drop,
            "break" => SimulatedCue.Break,
            "montee" => SimulatedCue.BuildUp,
            "silence" => SimulatedCue.Silence,
            "morceau" => SimulatedCue.SongChanged,
            _ => SimulatedCue.None,
        };
        if (cue != SimulatedCue.None)
        {
            Target.Send(new SimulateMusicCommand(CommandOrigin.User, cue));
            _runtime.TraceUi("Essai", $"simuler {name}");
        }
    }

    /// <summary>Impose un niveau d'énergie (0 Calme à 3 Explosif) ou rend l'énergie à l'écoute (« ecoute »).</summary>
    [RelayCommand]
    private void Energy(string? level)
    {
        if (int.TryParse(level, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value is >= 0 and <= 3)
        {
            EnergyLevel = value;
            Target.Send(new SimulateMusicCommand(CommandOrigin.User, Energy: EnergyOfLevel[value]));
        }
        else
        {
            EnergyLevel = -1;
            Target.Send(new SimulateMusicCommand(CommandOrigin.User, Energy: double.NaN));
        }
    }

    partial void OnStyleChanged(string value)
    {
        if (!_releasing)
        {
            Target.Send(new SimulateMusicCommand(CommandOrigin.User, Style: value.Trim()));
        }
    }

    partial void OnUseMetronomeChanged(bool value) => OnPropertyChanged(nameof(TempoText));

    /// <summary>Arrête l'essai sur la sortie (<paramref name="output"/>) ou sur l'aperçu.</summary>
    public void StopOn(bool output) => (output ? _runtime.Engine : _runtime.Preview).Send(_play(true));

    /// <summary>Fin de l'édition : énergie et style rendus à l'écoute, l'aperçu reprend le tempo du direct.</summary>
    public void Release()
    {
        _runtime.PreviewOwnTempo = false;
        if (_liveTempo is { } live)
        {
            // L'horloge du direct retrouve ce qu'elle suivait avant l'essai (la musique écoutée, par exemple).
            // Un tempo donné passe l'horloge en fixe : on rend d'abord le tempo, puis la source (Tap, Audio).
            _runtime.Engine.Send(new SetTempoSourceCommand(CommandOrigin.User, TempoSourceKind.Fixed, live.Bpm));
            if (live.Source != TempoSourceKind.Fixed)
            {
                _runtime.Engine.Send(new SetTempoSourceCommand(CommandOrigin.User, live.Source));
            }

            _liveTempo = null;
        }

        foreach (var engine in new[] { _runtime.Engine, _runtime.Preview })
        {
            engine.Send(new SimulateMusicCommand(CommandOrigin.User, Energy: double.NaN, Style: string.Empty));
        }

        EnergyLevel = -1;
        _releasing = true;
        Style = string.Empty;
        _releasing = false;
    }

    /// <summary>Relit ce que joue l'essai : étape active d'un show, position d'une séquence.</summary>
    public void Refresh(Guid? id)
    {
        OnPropertyChanged(nameof(TempoText));
        var state = TargetSequencer.State;
        if (state.Shows.FirstOrDefault(s => s.ShowId == id) is { } show)
        {
            IsPlaying = true;
            PlayheadBars = null;
            var steps = string.Join(", ", show.ActiveSteps.Select(s => string.Create(CultureInfo.CurrentCulture, $"{s.Id} {s.Name} (depuis {Math.Floor(s.SinceBeats / Sequencer.BeatsPerBar)} mesure(s))")));
            var next = show.Transitions.Select(t => t.Armed
                ? string.Create(CultureInfo.CurrentCulture, $"{t.Condition} → {string.Join(", ", t.ToNames)} dans {Math.Ceiling(t.BeatsLeft)} temps")
                : $"{t.Condition} → {string.Join(", ", t.ToNames)}{(t.Hint.Length > 0 ? $" ({t.Hint})" : string.Empty)}");
            Status = $"Étape active : {steps}{(show.Ended ? " · terminé (tient sa dernière étape)" : string.Empty)}" +
                (show.Transitions.Count > 0 ? $"{Environment.NewLine}Ensuite : {string.Join(" · ", next)}" : string.Empty);
            ActiveSteps = [.. show.ActiveSteps.Select(s => s.Id)];
            return;
        }

        if (state.Sequences.FirstOrDefault(s => s.SequenceId == id && s.OwnerShowId is null) is { } sequence)
        {
            IsPlaying = true;
            PlayheadBars = sequence.PositionBars;
            Status = sequence.PositionBars < 0
                ? string.Create(CultureInfo.CurrentCulture, $"Attend la frontière musicale ({Math.Ceiling(-sequence.PositionBars * Sequencer.BeatsPerBar)} temps)")
                : string.Create(CultureInfo.CurrentCulture, $"Mesure {Math.Floor(sequence.PositionBars) + 1} · temps {Math.Floor((sequence.PositionBars % 1) * Sequencer.BeatsPerBar) + 1} sur {sequence.Bars:0.##}{(sequence.Loops > 0 ? $" · {sequence.Loops} passage(s) terminé(s)" : string.Empty)}");
            ActiveSteps = [];
            return;
        }

        IsPlaying = false;
        PlayheadBars = null;
        ActiveSteps = [];
        Status = "À l'arrêt. « ▶ Jouer » lance le brouillon " + (_blind() ? "sur l'aperçu." : "sur la sortie.");
    }

    /// <summary>Étapes actives du show essayé (diagramme).</summary>
    public IReadOnlyList<string> ActiveSteps { get; private set; } = [];
}
