using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Luxia.App.ViewModels;

namespace Luxia.App;

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

        // GEN-082 : touche B = blackout, depuis n'importe quel écran, sauf pendant une saisie de texte.
        AddHandler(KeyDownEvent, OnKeyDownTunnel, RoutingStrategies.Tunnel);
    }

    private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.B || e.KeyModifiers != KeyModifiers.None || DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var focused = FocusManager?.GetFocusedElement() as Control;
        if (focused is TextBox || focused?.FindAncestorOfType<TextBox>() is not null || focused?.FindAncestorOfType<NumericUpDown>() is not null)
        {
            return;
        }

        vm.ToggleBlackoutCommand.Execute(null);
        e.Handled = true;
    }
}
