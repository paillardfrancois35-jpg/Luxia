using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Dmx.Messaging.Events;
using Dmx.Output;
using Dmx.UI.Controls;

namespace Dmx.UI.Modules.Outputs;

/// <summary>Ligne d'état d'un pilote de sortie (SORT-004, SORT-007).</summary>
public sealed partial class DriverStatusViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _state = string.Empty;

    [ObservableProperty]
    private string _stateColor = "#808080";

    [ObservableProperty]
    private string _detail = string.Empty;

    [ObservableProperty]
    private string _framesPerSecond = string.Empty;

    [ObservableProperty]
    private string _errors = string.Empty;

    [ObservableProperty]
    private string _writeDuration = string.Empty;

    /// <summary>Crée la ligne.</summary>
    public DriverStatusViewModel(OutputDriver driver, int universe)
    {
        ArgumentNullException.ThrowIfNull(driver);
        Driver = driver;
        Name = $"{driver.Name} (univers {universe})";
        Refresh();
    }

    /// <summary>Pilote suivi.</summary>
    public OutputDriver Driver { get; }

    /// <summary>Nom affiché.</summary>
    public string Name { get; }

    /// <summary>Met à jour l'affichage depuis l'état du pilote.</summary>
    public void Refresh()
    {
        var status = Driver.Status;
        State = status.State switch
        {
            OutputConnectionState.Connected => "Connecté",
            OutputConnectionState.Connecting => "Connexion…",
            OutputConnectionState.Error => "Erreur",
            _ => "Déconnecté",
        };
        StateColor = status.State switch
        {
            OutputConnectionState.Connected => "#3FB950",
            OutputConnectionState.Connecting => "#D29922",
            OutputConnectionState.Error => "#F85149",
            _ => "#8B949E",
        };
        Detail = status.Message ?? string.Empty;
        FramesPerSecond = status.FramesPerSecond.ToString("F1", CultureInfo.CurrentCulture);
        Errors = status.ErrorCount.ToString(CultureInfo.CurrentCulture);
        WriteDuration = $"{status.LastWriteDuration.TotalMilliseconds.ToString("F2", CultureInfo.CurrentCulture)} ms";
    }
}
