using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>
/// Bloc BPM de l'écran de jeu (Q42, doc 19 §3, essai P7 décision 1) : un seul interrupteur **Audio** (gris : tempo réglé à la main ;
/// bleu : l'horloge suit la musique écoutée), le BPM (saisie directe, validée par Entrée), TAP, − et +, les corrections ×2, ÷ 2 et
/// « 1 ici », et les voyants des quatre temps. Audio actif : le champ BPM, TAP, − et + sont grisés (le tempo vient du son) ;
/// ×2, ÷ 2 et « 1 ici » restent actifs (ils corrigent l'analyse) ; la confiance s'affiche à droite de l'interrupteur.
/// Ne fait que lire l'horloge du moteur et lui envoyer des commandes (CMD-040 à 042) ; les réglages de tempo ne sont pas
/// enregistrés dans le projet.
/// </summary>
public sealed partial class TempoBarViewModel : ViewModelBase
{
    private const string BeatOn = "#3FB950";
    private const string BeatFirstOn = "#F0883E";
    private const string BeatOff = "#30363D";
    private const int FlashTicks = 2;

    private readonly LuxiaRuntime _runtime;
    private readonly JournalPanelViewModel _journal;
    private int _flashLeft;
    private bool _editing;

    [ObservableProperty]
    private string _bpmText = "120";

    [ObservableProperty]
    private string _bpmInput = "120";

    [ObservableProperty]
    private int _beatInBar = 1;

    [ObservableProperty]
    private bool _audioOn;

    [ObservableProperty]
    private bool _manualEnabled = true;

    [ObservableProperty]
    private bool _canListen;

    [ObservableProperty]
    private bool _tapFlash;

    [ObservableProperty]
    private string _confidenceText = string.Empty;

    [ObservableProperty]
    private string _confidenceColor = "#F85149";

    [ObservableProperty]
    private string _audioStatus = "Écoute du son du PC et suivi du tempo";

    private bool _refreshing;
    private string? _loggedNotice;

    /// <summary>Crée le bloc sur le moteur en service.</summary>
    public TempoBarViewModel(LuxiaRuntime runtime, JournalPanelViewModel journal)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(journal);
        _runtime = runtime;
        _journal = journal;
        CanListen = runtime.Audio is not null;
        Refresh();
    }

    /// <summary>Texte de la source du tempo (« Fixe », « Tap » ou « Audio »), pour les tests et les info-bulles.</summary>
    public string SourceText => _runtime.Engine.Snapshot.Tempo.Source switch
    {
        TempoSourceKind.Tap => "Tap",
        TempoSourceKind.Audio => "Audio",
        _ => "Fixe",
    };

    /// <summary>Couleur du voyant de chacun des quatre temps.</summary>
    public string Beat1 => BeatInBar == 1 ? BeatFirstOn : BeatOff;

    /// <summary>Voyant du temps 2.</summary>
    public string Beat2 => BeatInBar == 2 ? BeatOn : BeatOff;

    /// <summary>Voyant du temps 3.</summary>
    public string Beat3 => BeatInBar == 3 ? BeatOn : BeatOff;

    /// <summary>Voyant du temps 4.</summary>
    public string Beat4 => BeatInBar == 4 ? BeatOn : BeatOff;

    /// <summary>Relit l'horloge du moteur (appelé par le rafraîchissement de l'écran, 20 fois par seconde).</summary>
    public void Refresh()
    {
        _refreshing = true;
        var tempo = _runtime.Engine.Snapshot.Tempo;
        var text = tempo.Bpm.ToString("0.#", CultureInfo.CurrentCulture);
        BpmText = text;
        if (!_editing)
        {
            BpmInput = text;
        }

        AudioOn = tempo.Source == TempoSourceKind.Audio;
        ManualEnabled = !AudioOn;
        BeatInBar = tempo.BeatInBar;
        OnPropertyChanged(nameof(SourceText));
        OnPropertyChanged(nameof(Beat1));
        OnPropertyChanged(nameof(Beat2));
        OnPropertyChanged(nameof(Beat3));
        OnPropertyChanged(nameof(Beat4));

        // Confiance : vert à partir de 70 %, orange de 30 à 70 %, rouge en dessous (l'horloge garde alors son tempo, AUD-022).
        ConfidenceText = AudioOn ? $"{tempo.Confidence * 100:0} %" : string.Empty;
        ConfidenceColor = tempo.Confidence >= 0.7 ? BeatOn : tempo.Confidence >= 0.3 ? "#D29922" : "#F85149";
        AudioStatus = AudioOn && _runtime.Audio?.Status is { } status ? status : "Écoute du son du PC et suivi du tempo";
        if (_flashLeft > 0 && --_flashLeft == 0)
        {
            TapFlash = false;
        }

        if (_runtime.Audio is { Notice: { } notice } audio && audio.NoticeAgeSeconds < 12 && _loggedNotice != notice)
        {
            // Changement de périphérique, reprise, erreur : une ligne au Journal de l'écran de jeu (essai P7, exemple 15).
            _loggedNotice = notice;
            _journal.Log("🎧 " + notice);
        }

        _refreshing = false;
    }

    /// <summary>Champ BPM : seuls les chiffres et une décimale (virgule ou point) sont gardés, même au collage (essai P7, E3).</summary>
    partial void OnBpmInputChanged(string value)
    {
        if (value is null)
        {
            return;
        }

        var clean = new System.Text.StringBuilder(value.Length);
        var decimalSeen = false;
        foreach (var c in value)
        {
            if (char.IsAsciiDigit(c))
            {
                clean.Append(c);
            }
            else if ((c == '.' || c == ',') && !decimalSeen)
            {
                decimalSeen = true;
                clean.Append(c);
            }
        }

        if (clean.Length > 6)
        {
            clean.Length = 6;
        }

        if (clean.ToString() != value)
        {
            BpmInput = clean.ToString();
        }
    }

    /// <summary>La saisie du BPM commence : l'affichage ne l'écrase plus pendant la frappe.</summary>
    public void BeginEdit() => _editing = true;

    /// <summary>La saisie se termine (sortie du champ) : la valeur saisie, si elle a changé, est appliquée.</summary>
    public void EndEdit()
    {
        if (!_editing)
        {
            return;
        }

        if (BpmInput != BpmText)
        {
            ApplyTypedBpm();
        }

        _editing = false;
    }

    partial void OnAudioOnChanged(bool value)
    {
        if (_refreshing || _runtime.Audio is null)
        {
            return;
        }

        // L'utilisateur a basculé l'interrupteur (le moteur confirmera la source au tick suivant).
        _runtime.SetAudioMode(value);
        _journal.Log(value ? "🎧 Audio : écoute du son du PC, tempo suivi" : "🎧 Audio coupé : tempo fixe");
        _runtime.TraceUi("Contrôle", value ? "audio activé" : "audio coupé");
        ManualEnabled = !value;
    }

    /// <summary>TAP (touche T) : une frappe ; quatre frappes régulières donnent le tempo (AUD-025).</summary>
    [RelayCommand]
    public void Tap()
    {
        _runtime.Engine.Send(new TapTempoCommand(CommandOrigin.User));
        _runtime.TraceUi("Contrôle", "tap tempo");
        TapFlash = true;
        _flashLeft = FlashTicks;
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

    /// <summary>Applique le BPM saisi comme tempo fixe (Entrée) ; une saisie invalide est ignorée et l'affichage revient au tempo courant.</summary>
    [RelayCommand]
    private void ApplyTypedBpm()
    {
        var parsed = double.TryParse(BpmInput, NumberStyles.Float, CultureInfo.CurrentCulture, out var bpm)
            || double.TryParse(BpmInput, NumberStyles.Float, CultureInfo.InvariantCulture, out bpm);
        if (!parsed || AudioOn)
        {
            BpmInput = BpmText;
            return;
        }

        _runtime.Engine.Send(new SetTempoSourceCommand(CommandOrigin.User, TempoSourceKind.Fixed, bpm));
        _journal.Log($"♪ tempo fixe {bpm:0.#} BPM");
        _runtime.TraceUi("Contrôle", "tempo fixe");
    }

    private void Adjust(TempoAdjustment adjustment, double value, string text)
    {
        _runtime.Engine.Send(new AdjustTempoCommand(CommandOrigin.User, adjustment, value));
        _journal.Log("♪ " + text);
        _runtime.TraceUi("Contrôle", text);
    }
}
