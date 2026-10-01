using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Audio;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Audio;

/// <summary>Périphérique proposé dans la liste de l'écran Audio.</summary>
/// <param name="Id">Identifiant système (<c>null</c> = le son joué par le PC, sortie par défaut).</param>
/// <param name="Label">Libellé.</param>
public sealed record AudioDeviceChoice(string? Id, string Label)
{
    /// <inheritdoc />
    public override string ToString() => Label;
}

/// <summary>
/// Écran « Audio » (AUD-080, AUD-081) : ce que LuXia entend (niveau, trois bandes, impulsions), le tempo et la confiance, l'énergie
/// et ses événements (break, drop, montée), les réglages de l'analyse et la calibration de la latence (AUD-027). Ne fait que
/// lire l'écoute et lui envoyer ses réglages.
/// </summary>
public sealed partial class AudioViewModel : ViewModelBase, IRefreshable
{
    /// <summary>Nom de la scène de calibration du show de référence : un flash sur chaque temps de l'horloge.</summary>
    public const string CalibrationSceneName = "Calibration de latence";

    private const string BeatOn = "#3FB950";
    private const string BeatFirstOn = "#F0883E";
    private const string BeatOff = "#30363D";

    private readonly LuxiaRuntime _runtime;
    private readonly Action<Action> _post;
    private bool _devicesRequested;
    private string? _shownNotice;
    private long _bassSeen;
    private long _trebleSeen;
    private int _dirty;
    private bool _loading;
    private int _lastEventCount = -1;

    [ObservableProperty]
    private string _status = "Écoute arrêtée";

    [ObservableProperty]
    private bool _listening;

    [ObservableProperty]
    private bool _available;

    [ObservableProperty]
    private double _level;

    [ObservableProperty]
    private double _bass;

    [ObservableProperty]
    private double _mid;

    [ObservableProperty]
    private double _treble;

    [ObservableProperty]
    private double _bassFlash;

    [ObservableProperty]
    private double _trebleFlash;

    [ObservableProperty]
    private string _bpmText = "—";

    [ObservableProperty]
    private double _confidence;

    [ObservableProperty]
    private string _confidenceText = "—";

    [ObservableProperty]
    private double _beatPhase;

    [ObservableProperty]
    private string _beatText = "—";

    [ObservableProperty]
    private string _sourceText = "Fixe";

    [ObservableProperty]
    private int _beatInBar;

    [ObservableProperty]
    private string _beat1 = BeatFirstOn;

    [ObservableProperty]
    private string _beat2 = BeatOff;

    [ObservableProperty]
    private string _beat3 = BeatOff;

    [ObservableProperty]
    private string _beat4 = BeatOff;

    [ObservableProperty]
    private double _energy;

    [ObservableProperty]
    private string _energyText = "—";

    [ObservableProperty]
    private string _trendText = string.Empty;

    [ObservableProperty]
    private string _breakText = string.Empty;

    [ObservableProperty]
    private AudioDeviceChoice? _selectedDevice;

    [ObservableProperty]
    private decimal _sensitivity = 50;

    [ObservableProperty]
    private decimal _smoothing = 2;

    [ObservableProperty]
    private decimal _minBpm = 70;

    [ObservableProperty]
    private decimal _maxBpm = 180;

    [ObservableProperty]
    private decimal _preferredBpm = 118;

    [ObservableProperty]
    private decimal _latencyMilliseconds;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private bool _calibrating;

    /// <summary>Crée l'écran sur l'écoute de l'application.</summary>
    /// <param name="runtime">Application.</param>
    /// <param name="post">Exécute une action sur le fil de l'interface (tests : tout de suite).</param>
    public AudioViewModel(LuxiaRuntime runtime, Action<Action>? post = null)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
        _post = post ?? (action => Avalonia.Threading.Dispatcher.UIThread.Post(action));
        Available = runtime.Audio is not null;
        LoadSettings();

        // L'énumération des périphériques prend de 0,5 à 1 s (Bluetooth compris) : elle ne se fait ni au démarrage de
        // l'application ni sur le fil de l'interface, mais au premier affichage de l'écran (contrôle du temps de chargement).
        Devices.Add(new AudioDeviceChoice(null, "Le son joué par le PC (sortie par défaut)"));
        _loading = true;
        SelectedDevice = Devices[0];
        _loading = false;
        Update();
    }

    /// <summary>Périphériques proposés.</summary>
    public ObservableCollection<AudioDeviceChoice> Devices { get; } = [];

    /// <summary>Derniers événements musicaux, du plus récent au plus ancien (AUD-080).</summary>
    public ObservableCollection<string> Events { get; } = [];

    /// <inheritdoc />
    public void Refresh()
    {
        if (!_devicesRequested && Available)
        {
            _ = LoadDevicesAsync();
        }

        Update();
    }

    private void Update()
    {
        var audio = _runtime.Audio;
        var tempo = _runtime.Engine.Snapshot.Tempo;
        SourceText = tempo.Source switch
        {
            TempoSourceKind.Tap => "Tap",
            TempoSourceKind.Audio => "Audio",
            _ => "Fixe",
        };
        _loading = true;
        Listening = audio?.IsListening ?? false;
        _loading = false;
        Status = audio?.Status ?? "Pas d'écoute dans cette configuration";
        if (audio is null)
        {
            return;
        }

        ShowNotice(audio);
        var state = audio.State;
        var live = Listening && !state.Silent;
        Level = live ? Meter(state.Level) : 0;
        Bass = live ? Meter(state.Bass) : 0;
        Mid = live ? Meter(state.Mid) : 0;
        Treble = live ? Meter(state.Treble) : 0;
        BassFlash = Decay(BassFlash, state.BassPulseCount, ref _bassSeen, state.BassPulseStrength);
        TrebleFlash = Decay(TrebleFlash, state.TreblePulseCount, ref _trebleSeen, state.TreblePulseStrength);
        BpmText = state.Bpm > 0 ? state.Bpm.ToString("0.0", CultureInfo.CurrentCulture) : "—";
        Confidence = state.Confidence;
        ConfidenceText = state.Bpm > 0 ? $"{state.Confidence * 100:0} %" : "—";
        BeatPhase = tempo.Phase;
        BeatText = tempo.BeatInBar.ToString(CultureInfo.CurrentCulture);
        BeatInBar = tempo.BeatInBar;
        Beat1 = tempo.BeatInBar == 1 ? BeatFirstOn : BeatOff;
        Beat2 = tempo.BeatInBar == 2 ? BeatOn : BeatOff;
        Beat3 = tempo.BeatInBar == 3 ? BeatOn : BeatOff;
        Beat4 = tempo.BeatInBar == 4 ? BeatOn : BeatOff;
        Energy = state.Energy;
        EnergyText = live ? state.EnergyLevel switch
        {
            EnergyLevel.Calm => "Calme",
            EnergyLevel.Groove => "Groove",
            EnergyLevel.Energetic => "Énergique",
            _ => "Explosif",
        }
        : "—";
        TrendText = !live ? string.Empty : state.Trend switch
        {
            EnergyTrend.Rising => "↗ monte",
            EnergyTrend.Falling => "↘ descend",
            _ => "→ stable",
        };
        BreakText = state.InBreak ? "BREAK" : string.Empty;

        var recent = audio.RecentEvents;
        if (recent.Count != _lastEventCount)
        {
            _lastEventCount = recent.Count;
            Events.Clear();
            foreach (var e in recent.Reverse().Take(12))
            {
                Events.Add(Describe(e));
            }
        }

        if (_dirty > 0 && --_dirty == 0)
        {
            ApplySettings();
        }
    }

    partial void OnListeningChanged(bool value)
    {
        if (_loading)
        {
            return;
        }

        _runtime.SetAudioMode(value);
    }

    partial void OnSelectedDeviceChanged(AudioDeviceChoice? value)
    {
        if (_loading || value is null || _runtime.Audio is null)
        {
            return;
        }

        _runtime.SetAudioDevice(value.Id);

        // La latence est celle du périphérique choisi.
        _loading = true;
        LatencyMilliseconds = (decimal)Math.Round(_runtime.Preferences.Current.Audio.LatencyFor(value.Id) * 1000);
        _loading = false;
        Message = value.Id is null ? "Écoute du son joué par le PC." : $"Écoute de « {value.Label} ».";
    }

    partial void OnSensitivityChanged(decimal value) => Touch();

    partial void OnSmoothingChanged(decimal value) => Touch();

    partial void OnMinBpmChanged(decimal value) => Touch();

    partial void OnMaxBpmChanged(decimal value) => Touch();

    partial void OnPreferredBpmChanged(decimal value) => Touch();

    partial void OnLatencyMillisecondsChanged(decimal value) => Touch();

    /// <summary>Relit la liste des périphériques (un casque branché depuis l'ouverture de l'écran).</summary>
    [RelayCommand]
    private void RefreshDevices() => _ = LoadDevicesAsync();

    /// <summary>Énumère les périphériques hors du fil de l'interface, puis remplit la liste (et retrouve le choix mémorisé).</summary>
    public async Task LoadDevicesAsync()
    {
        _devicesRequested = true;
        IReadOnlyList<AudioDeviceInfo> found = [];
        string? error = null;
        try
        {
            found = await Task.Run(() => _runtime.Audio?.Devices() ?? []).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            error = "Liste des périphériques indisponible : " + ex.Message;
        }

        _post(() =>
        {
            Devices.Clear();
            Devices.Add(new AudioDeviceChoice(null, "Le son joué par le PC (sortie par défaut)"));
            foreach (var device in found)
            {
                Devices.Add(new AudioDeviceChoice(device.Id, device.IsInput ? $"Entrée : {device.Name}" : $"Sortie : {device.Name}"));
            }

            if (error is not null)
            {
                Message = error;
            }

            _loading = true;
            SelectedDevice = Devices.FirstOrDefault(d => d.Id == _runtime.Preferences.Current.Audio.DeviceId) ?? Devices[0];
            _loading = false;
        });
    }

    /// <summary>×2 (AUD-023).</summary>
    [RelayCommand]
    private void TimesTwo() => _runtime.Engine.Send(new AdjustTempoCommand(CommandOrigin.User, TempoAdjustment.TimesTwo));

    /// <summary>÷2 (AUD-023).</summary>
    [RelayCommand]
    private void DivideByTwo() => _runtime.Engine.Send(new AdjustTempoCommand(CommandOrigin.User, TempoAdjustment.DivideByTwo));

    /// <summary>Lance la scène de calibration : un flash sur chaque temps de l'horloge (AUD-027).</summary>
    [RelayCommand]
    private void StartCalibration()
    {
        var scene = _runtime.Project.Scenes.Scenes.FirstOrDefault(s => s.Name == CalibrationSceneName);
        if (scene is null)
        {
            Message = $"La scène « {CalibrationSceneName} » n'existe pas dans ce projet (elle est dans le show de référence, catégorie Phase P7).";
            return;
        }

        _runtime.Engine.Send(new LaunchSceneCommand(CommandOrigin.User, scene.Id, Immediate: true));
        Calibrating = true;
        Message = "Flash sur chaque temps lancé : avancez ou retardez la latence jusqu'à ce que le flash tombe sur le kick.";
    }

    /// <summary>Arrête la scène de calibration.</summary>
    [RelayCommand]
    private void StopCalibration()
    {
        if (_runtime.Project.Scenes.Scenes.FirstOrDefault(s => s.Name == CalibrationSceneName) is { } scene)
        {
            _runtime.Engine.Send(new StopSceneCommand(CommandOrigin.User, scene.Id));
        }

        Calibrating = false;
    }

    /// <summary>Remet la latence à zéro.</summary>
    [RelayCommand]
    private void ResetLatency() => LatencyMilliseconds = 0;

    /// <summary>
    /// Un changement de périphérique ou une erreur se reconnecte en une fraction de seconde : l'état seul passe inaperçu. L'avis
    /// reste donc affiché une douzaine de secondes (essai P7, exemple 15).
    /// </summary>
    private void ShowNotice(AudioListener audio)
    {
        if (audio.Notice is { } notice && audio.NoticeAgeSeconds < 12)
        {
            if (_shownNotice != notice)
            {
                _shownNotice = notice;
                Message = notice;
            }
        }
        else if (_shownNotice is not null)
        {
            if (Message == _shownNotice)
            {
                Message = null;
            }

            _shownNotice = null;
        }
    }

    private static double Meter(double value) => Math.Clamp(Math.Sqrt(Math.Max(0, value)) * 1.4, 0, 1);

    private static double Decay(double current, long count, ref long seen, double strength)
    {
        if (count != seen)
        {
            seen = count;
            return Math.Clamp(0.35 + (0.65 * strength), 0, 1);
        }

        return current * 0.8;
    }

    private static string Describe(AudioEvent e)
    {
        var time = TimeSpan.FromSeconds(e.Seconds);
        var text = e.Kind switch
        {
            AudioEventKind.Silence => "silence",
            AudioEventKind.Resumed => "le son reprend",
            AudioEventKind.Break => "BREAK",
            AudioEventKind.Drop => "DROP",
            AudioEventKind.BuildUp => "montée",
            _ => "énergie : " + e.Level switch
            {
                EnergyLevel.Calm => "calme",
                EnergyLevel.Groove => "groove",
                EnergyLevel.Energetic => "énergique",
                _ => "explosif",
            },
        };
        return string.Create(CultureInfo.CurrentCulture, $"{(int)time.TotalMinutes}:{time.Seconds:00}  {text}");
    }

    private void Touch()
    {
        if (!_loading)
        {
            // Les réglages s'appliquent (et s'enregistrent) une demi-seconde après le dernier mouvement du curseur.
            _dirty = 10;
        }
    }

    private void LoadSettings()
    {
        var prefs = _runtime.Preferences.Current.Audio;
        _loading = true;
        Sensitivity = (decimal)Math.Round(prefs.PulseSensitivity * 100);
        Smoothing = (decimal)prefs.EnergySmoothingSeconds;
        MinBpm = (decimal)prefs.MinBpm;
        MaxBpm = (decimal)prefs.MaxBpm;
        PreferredBpm = (decimal)prefs.PreferredBpm;
        LatencyMilliseconds = (decimal)Math.Round(prefs.LatencyFor(prefs.DeviceId) * 1000);
        _loading = false;
    }

    private void ApplySettings()
    {
        var min = Math.Min((double)MinBpm, (double)MaxBpm - 10);
        var tuning = new AudioTuning
        {
            PulseSensitivity = Math.Clamp((double)Sensitivity / 100, 0, 1),
            EnergySmoothingSeconds = (double)Smoothing,
            MinBpm = min,
            MaxBpm = (double)MaxBpm,
            PreferredBpm = (double)PreferredBpm,
        };
        _runtime.SetAudioTuning(tuning, (double)LatencyMilliseconds / 1000);
    }
}
