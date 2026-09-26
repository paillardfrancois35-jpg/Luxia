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
        Opened += async (_, _) =>
        {
            _refresh.Start();
            if (DataContext is MainWindowViewModel vm)
            {
                vm.ReportReady();
                await vm.OfferResumeAsync().ConfigureAwait(true);
            }
        };
        Closed += (_, _) => _refresh.Stop();

        // GEN-082 : touche B = blackout, depuis n'importe quel écran, sauf pendant une saisie de texte.
        // GEN-071, LIVE-040 : en Live, les autres raccourcis (dont les touches à maintenir) quel que soit le focus.
        AddHandler(KeyDownEvent, OnKeyDownTunnel, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnKeyUpTunnel, RoutingStrategies.Tunnel);
    }

    private bool IsTyping()
    {
        var focused = FocusManager?.GetFocusedElement() as Control;
        return focused is TextBox || focused?.FindAncestorOfType<TextBox>() is not null || focused?.FindAncestorOfType<NumericUpDown>() is not null;
    }

    private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.None || DataContext is not MainWindowViewModel vm || IsTyping())
        {
            return;
        }

        if (e.Key == Key.B)
        {
            vm.ToggleBlackoutCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (vm.SelectedPage.Page is Luxia.UI.Modules.Live.LiveViewModel live && Luxia.UI.Modules.Live.LiveKeys.From(e.Key) is { } key)
        {
            e.Handled = live.OnKey(key, down: true);
        }
    }

    private void OnKeyUpTunnel(object? sender, KeyEventArgs e)
    {
        // Relâche d'une touche à maintenir : traitée même avec un modificateur, pour ne jamais laisser un flash coincé.
        if (DataContext is MainWindowViewModel { SelectedPage.Page: Luxia.UI.Modules.Live.LiveViewModel live }
            && Luxia.UI.Modules.Live.LiveKeys.From(e.Key) is { } key)
        {
            e.Handled = live.OnKey(key, down: false);
        }
    }
}
