using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Core.Settings;
using Luxia.Hosting;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Outputs;

/// <summary>
/// Écran « Sorties » (SORT-007) : état des pilotes, réglages de l'Arduino, chenillard de test, enregistreur.
/// </summary>
public sealed partial class OutputsViewModel : ViewModelBase, IRefreshable
{
    /// <summary>Libellé du choix « détection automatique » dans la liste des ports.</summary>
    public const string AutomaticPort = "Automatique";

    private readonly LuxiaRuntime _runtime;

    [ObservableProperty]
    private string _engineStatus = string.Empty;

    [ObservableProperty]
    private string _selectedPort = AutomaticPort;

    [ObservableProperty]
    private bool _legacyProtocol;

    [ObservableProperty]
    private decimal _channelCount;

    [ObservableProperty]
    private bool _probeAllPorts;

    [ObservableProperty]
    private string _testRange = string.Empty;

    [ObservableProperty]
    private string _testExcluded = string.Empty;

    [ObservableProperty]
    private string _testHeld = string.Empty;

    [ObservableProperty]
    private decimal _testValuePercent;

    [ObservableProperty]
    private decimal _testStepMilliseconds;

    [ObservableProperty]
    private string _testStatus = "Arrêté";

    [ObservableProperty]
    private bool _testRunning;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private bool _recording;

    [ObservableProperty]
    private string _recordingStatus = "Aucun enregistrement en cours";

    [ObservableProperty]
    private string? _recordingPath;

    /// <summary>Libellé du bouton : démarrer ou arrêter (SORT-065).</summary>
    public string RecordingButtonText => Recording ? "■ Arrêter l'enregistrement" : "● Enregistrer les trames";

    partial void OnRecordingChanged(bool value) => OnPropertyChanged(nameof(RecordingButtonText));

    [ObservableProperty]
    private decimal _tickRate;

    /// <summary>Crée l'écran.</summary>
    public OutputsViewModel(LuxiaRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;

        var prefs = runtime.Preferences.Current;
        var arduino = prefs.Outputs.Arduino;
        SelectedPort = string.IsNullOrWhiteSpace(arduino.ForcedPort) ? AutomaticPort : arduino.ForcedPort;
        LegacyProtocol = arduino.Protocol == ArduinoProtocol.Legacy;
        ChannelCount = arduino.ChannelCount;
        ProbeAllPorts = arduino.ProbeAllPorts;
        TestRange = prefs.TestOutput.Range;
        TestExcluded = prefs.TestOutput.ExcludedChannels;
        TestHeld = prefs.TestOutput.HeldChannels;
        TestValuePercent = prefs.TestOutput.ValuePercent;
        TestStepMilliseconds = prefs.TestOutput.StepMilliseconds;
        TickRate = (decimal)runtime.Loop.RateHz;

        RefreshPorts();
        Refresh();
    }

    /// <summary>Pilotes affectés.</summary>
    public ObservableCollection<DriverStatusViewModel> Drivers { get; } = [];

    /// <summary>Ports proposés (« Automatique » + ports présents).</summary>
    public ObservableCollection<string> Ports { get; } = [];

    /// <summary>Message de chargement des préférences (à signaler une fois).</summary>
    public string? PreferencesMessage => _runtime.PreferencesLoadMessage;

    /// <inheritdoc />
    public void Refresh()
    {
        var routes = _runtime.Router.Routes;
        if (routes.Count != Drivers.Count || routes.Where((r, i) => Drivers[i].Driver != r.Driver).Any())
        {
            Drivers.Clear();
            foreach (var (universe, driver) in routes)
            {
                Drivers.Add(new DriverStatusViewModel(driver, universe));
            }
        }

        foreach (var driver in Drivers)
        {
            driver.Refresh();
        }

        var stats = _runtime.Loop.Statistics;
        EngineStatus = string.Create(
            CultureInfo.CurrentCulture,
            $"Moteur : {stats.MeasuredRateHz:F1} Hz (visé {stats.TargetRateHz:F1}) – gigue p99 {stats.P99Lateness.TotalMilliseconds:F1} ms – max {stats.MaxLateness.TotalMilliseconds:F1} ms");

        var test = _runtime.Engine.TestState;
        TestRunning = test.Active;
        TestStatus = test.Active ? $"En cours : canal {test.CurrentChannel}" : "Arrêté";

        Recording = _runtime.Recorder is not null;
        if (_runtime.Recorder is { } recorder)
        {
            RecordingPath = recorder.FilePath;
            RecordingStatus = $"Fichier : {recorder.FilePath}";
        }
    }

    /// <summary>Relit la liste des ports série.</summary>
    [RelayCommand]
    private void RefreshPorts()
    {
        var selected = SelectedPort;
        Ports.Clear();
        Ports.Add(AutomaticPort);
        foreach (var port in _runtime.SerialPorts)
        {
            var label = port.PortName;
            Ports.Add(label);
        }

        if (!Ports.Contains(selected))
        {
            Ports.Add(selected);
        }

        SelectedPort = selected;
    }

    /// <summary>Applique les réglages de l'Arduino (SORT-014, SORT-021).</summary>
    [RelayCommand]
    private void ApplyArduino()
    {
        _runtime.UpdateArduinoSettings(a => a with
        {
            ForcedPort = SelectedPort == AutomaticPort ? null : SelectedPort,
            Protocol = LegacyProtocol ? ArduinoProtocol.Legacy : ArduinoProtocol.Enttec,
            ChannelCount = (int)Math.Clamp(ChannelCount, 1, 512),
            ProbeAllPorts = ProbeAllPorts,
        });
        Message = "Réglages de l'Arduino enregistrés.";
    }

    /// <summary>Force une nouvelle détection / connexion.</summary>
    [RelayCommand]
    private void Reconnect()
    {
        _runtime.ReconnectArduino();
        Message = "Reconnexion demandée.";
    }

    /// <summary>Change la fréquence du moteur.</summary>
    [RelayCommand]
    private void ApplyTickRate()
    {
        _runtime.SetTickRate((double)TickRate);
        TickRate = (decimal)_runtime.Loop.RateHz;
        _runtime.Loop.ResetStatistics();
    }

    /// <summary>Démarre le chenillard de test (CMD-024).</summary>
    [RelayCommand]
    private void StartTest()
    {
        var error = _runtime.StartTest(new TestOutputPreferences
        {
            Range = TestRange,
            ExcludedChannels = TestExcluded,
            HeldChannels = TestHeld,
            ValuePercent = (int)TestValuePercent,
            StepMilliseconds = (int)TestStepMilliseconds,
        });
        Message = error ?? "Test de sortie démarré.";
    }

    /// <summary>Arrête le chenillard de test.</summary>
    [RelayCommand]
    private void StopTest()
    {
        _runtime.StopTest();
        Message = "Test de sortie arrêté.";
    }

    /// <summary>Démarre ou arrête l'enregistrement (SORT-061).</summary>
    [RelayCommand]
    private void ToggleRecording()
    {
        if (_runtime.Recorder is null)
        {
            var path = _runtime.StartRecording();
            Message = $"Enregistrement démarré : {path}";
        }
        else
        {
            var path = _runtime.StopRecording();
            RecordingPath = path;
            RecordingStatus = $"Dernier enregistrement : {path}";
            Message = "Enregistrement arrêté.";
        }
    }
}
