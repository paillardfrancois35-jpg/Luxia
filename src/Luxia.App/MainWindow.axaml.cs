using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Luxia.App.Services;
using Luxia.App.ViewModels;

namespace Luxia.App;

/// <summary>Fenêtre principale.</summary>
public partial class MainWindow : Window
{
    private readonly DispatcherTimer _refresh = new() { Interval = TimeSpan.FromMilliseconds(50) };

    private void ApplyScale(double scale) =>
        this.FindControl<LayoutTransformControl>("Scaled")!.LayoutTransform = new Avalonia.Media.ScaleTransform(scale, scale);

    /// <summary>Crée la fenêtre.</summary>
    public MainWindow()
    {
        InitializeComponent();

        // F8 : taille de l'interface, appliquée à tout le contenu sous le menu.
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                ApplyScale(vm.UiScale);
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(MainWindowViewModel.UiScale))
                    {
                        ApplyScale(vm.UiScale);
                    }
                };
            }
        };

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

        // SORT-066 : chaque clic de bouton et chaque choix dans une liste est tracé « IHM – onglet – action », sans code
        // propre à chaque écran (journal technique et journal de l'enregistrement des trames).
        AddHandler(Button.ClickEvent, OnAnyClick, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(SelectingItemsControl.SelectionChangedEvent, OnAnySelection, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void OnAnyClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Button button && DataContext is MainWindowViewModel vm)
        {
            var state = button is ToggleButton toggle ? (toggle.IsChecked == true ? " → coché" : " → décoché") : string.Empty;
            vm.TraceUi($"clic « {UiLabel.Of(button)} »{state}");
        }
    }

    private void OnAnySelection(object? sender, SelectionChangedEventArgs e)
    {
        if (e.Source is SelectingItemsControl list && e.AddedItems.Count > 0 && DataContext is MainWindowViewModel vm)
        {
            var kind = list is ComboBox ? "choix" : "sélection";
            vm.TraceUi($"{kind} « {UiLabel.Describe(e.AddedItems[0])} »");
        }
    }

    private bool IsTyping()
    {
        var focused = FocusManager?.GetFocusedElement() as Control;
        return focused is TextBox || focused?.FindAncestorOfType<TextBox>() is not null || focused?.FindAncestorOfType<NumericUpDown>() is not null;
    }

    private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
    {
        // SORT-066 : touches tracées en Live (journal technique et journal de l'enregistrement), pour comprendre ce
        // que le clavier envoie réellement (essai P5 : Page ↑ ouvrait le menu Projet au lieu du Grand Master).
        if (DataContext is MainWindowViewModel { SelectedPage.Page: Luxia.UI.Modules.Live.LiveViewModel } traced && !IsTyping())
        {
            traced.TraceUi(e.KeyModifiers == KeyModifiers.None ? $"touche {e.Key}" : $"touche {e.KeyModifiers}+{e.Key}");
        }

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
