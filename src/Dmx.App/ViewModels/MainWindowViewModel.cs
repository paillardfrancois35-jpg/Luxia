using Dmx.Hosting;

namespace Dmx.App.ViewModels;

/// <summary>Fenêtre principale : en P0, l'écran Sorties seul (Q18).</summary>
public sealed class MainWindowViewModel : ViewModelBase
{
    /// <summary>Crée la fenêtre.</summary>
    public MainWindowViewModel(DmxRuntime runtime)
    {
        Outputs = new OutputsViewModel(runtime);
    }

    /// <summary>Écran Sorties.</summary>
    public OutputsViewModel Outputs { get; }

    /// <summary>Rafraîchissement périodique.</summary>
    public void Refresh() => Outputs.Refresh();
}
