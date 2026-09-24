using Avalonia.Controls;
using Avalonia.Threading;
using Dmx.App.ViewModels;

namespace Dmx.App;

/// <summary>Fenêtre principale.</summary>
public partial class MainWindow : Window
{
    private readonly DispatcherTimer _refresh = new() { Interval = TimeSpan.FromMilliseconds(50) };

    /// <summary>Crée la fenêtre.</summary>
    public MainWindow()
    {
        InitializeComponent();

        // L'interface lit l'état du moteur à son propre rythme (doc 02 §6.1), jamais l'inverse.
        _refresh.Tick += (_, _) => (DataContext as MainWindowViewModel)?.Refresh();
        Opened += (_, _) => _refresh.Start();
        Closed += (_, _) => _refresh.Stop();
    }
}
