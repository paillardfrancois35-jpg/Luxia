using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>
/// Bloc BPM de l'écran de jeu (Q42, doc 19 §3) : source du tempo (Fixe, Tap, Audio), valeur, TAP, ×2, ÷2, « 1 ici »
/// (recalage de la mesure) et compteur 1-2-3-4. Ne fait que lire l'état de l'horloge du moteur et lui envoyer des commandes
/// (CMD-040 à 042) ; les réglages de tempo ne sont pas enregistrés dans le projet.
/// </summary>
public sealed partial class TempoBarViewModel : ViewModelBase
{
    private const string BeatOn = "#3FB950";
    private const string BeatFirstOn = "#F0883E";
    private const string BeatOff = "#30363D";

    private readonly LuxiaRuntime _runtime;
    private readonly JournalPanelViewModel _journal;

    [ObservableProperty]
    private string _bpmText = "120";

    [ObservableProperty]
    private string _sourceText = "Fixe";

    [ObservableProperty]
    private string _detail = string.Empty;

    [ObservableProperty]
    private string _beat1 = BeatFirstOn;

    [ObservableProperty]
    private string _beat2 = BeatOff;

    [ObservableProperty]
    private string _beat3 = BeatOff;

    [ObservableProperty]
    private string _beat4 = BeatOff;

    [ObservableProperty]
    private string _barText = "Mesure 1";

    [ObservableProperty]
    private bool _isFixed = true;

    [ObservableProperty]
    private bool _isTap;

    [ObservableProperty]
    private string _bpmInput = "120";

    /// <summary>Crée le bloc sur le moteur en service.</summary>
    public TempoBarViewModel(LuxiaRuntime runtime, JournalPanelViewModel journal)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(journal);
        _runtime = runtime;
        _journal = journal;
        Refresh();
    }

    /// <summary>Relit l'horloge du moteur (appelé par le rafraîchissement de l'écran, 20 fois par seconde).</summary>
    public void Refresh()
    {
        var tempo = _runtime.Engine.Snapshot.Tempo;
        var text = tempo.Bpm.ToString("0.#", CultureInfo.CurrentCulture);
        if (BpmText != text)
        {
            BpmText = text;
        }

        SourceText = tempo.Source switch
        {
            TempoSourceKind.Tap => "Tap",
            TempoSourceKind.Audio => "Audio",
            _ => "Fixe",
        };
        IsFixed = tempo.Source == TempoSourceKind.Fixed;
        IsTap = tempo.Source == TempoSourceKind.Tap;
        Detail = tempo.Source == TempoSourceKind.Audio ? $"confiance {tempo.Confidence * 100:0} %" : string.Empty;
        Beat1 = tempo.BeatInBar == 1 ? BeatFirstOn : BeatOff;
        Beat2 = tempo.BeatInBar == 2 ? BeatOn : BeatOff;
        Beat3 = tempo.BeatInBar == 3 ? BeatOn : BeatOff;
        Beat4 = tempo.BeatInBar == 4 ? BeatOn : BeatOff;
        BarText = $"Mesure {tempo.Bar}";
    }

    /// <summary>TAP (touche T) : une frappe ; quatre frappes régulières donnent le tempo (AUD-025).</summary>
    [RelayCommand]
    public void Tap()
    {
        _runtime.Engine.Send(new TapTempoCommand(CommandOrigin.User));
        _runtime.TraceUi("Contrôle", "tap tempo");
    }

    /// <summary>×2 (AUD-023).</summary>
    [RelayCommand]
    private void TimesTwo() => Adjust(TempoAdjustment.TimesTwo, 0, "tempo × 2");

    /// <summary>÷2 (AUD-023).</summary>
    [RelayCommand]
    private void DivideByTwo() => Adjust(TempoAdjustment.DivideByTwo, 0, "tempo ÷ 2");

    /// <summary>+1 BPM.</summary>
    [RelayCommand]
    private void Increase() => Adjust(TempoAdjustment.AddBpm, 1, "tempo +1 BPM");

    /// <summary>−1 BPM.</summary>
    [RelayCommand]
    private void Decrease() => Adjust(TempoAdjustment.AddBpm, -1, "tempo −1 BPM");

    /// <summary>« 1 ici » : le temps en cours devient le premier de la mesure (AUD-024).</summary>
    [RelayCommand]
    private void ResyncBar() => Adjust(TempoAdjustment.ResyncBar, 0, "le 1 est maintenant");

    /// <summary>Passe en tempo fixe, avec le BPM saisi s'il est valide, sinon avec le tempo courant.</summary>
    [RelayCommand]
    private void UseFixed()
    {
        var parsed = double.TryParse(BpmInput, NumberStyles.Float, CultureInfo.CurrentCulture, out var bpm)
            || double.TryParse(BpmInput, NumberStyles.Float, CultureInfo.InvariantCulture, out bpm);
        _runtime.Engine.Send(new SetTempoSourceCommand(CommandOrigin.User, TempoSourceKind.Fixed, parsed ? bpm : _runtime.Engine.Bpm));
        _journal.Log(parsed ? $"♪ tempo fixe {bpm:0.#} BPM" : "♪ tempo fixe au tempo courant");
        _runtime.TraceUi("Contrôle", "tempo fixe");
    }

    private void Adjust(TempoAdjustment adjustment, double value, string text)
    {
        _runtime.Engine.Send(new AdjustTempoCommand(CommandOrigin.User, adjustment, value));
        _journal.Log("♪ " + text);
        _runtime.TraceUi("Contrôle", text);
    }
}
