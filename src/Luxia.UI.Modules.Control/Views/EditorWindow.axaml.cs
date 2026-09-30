using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Luxia.UI.Modules.Control.Docking;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>
/// Fenêtre d'édition d'une scène (ERG-033) : disposition fixe (plan, réglages et effets, propriétés et étapes), brouillon
/// avec Appliquer / Annuler / Valider et case Aveugle. Cachée, jamais détruite : on la rouvre à la demande (✎). La croix ne
/// ferme que sans modification ; sinon elle le dit et laisse choisir.
/// </summary>
public partial class EditorWindow : Window
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<EditorViewModel, EditorWindow> Windows = [];

    /// <summary>La fenêtre d'édition de ce modèle (créée à la première demande : jamais deux pour le même brouillon).</summary>
    public static EditorWindow For(EditorViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);
        if (!Windows.TryGetValue(vm, out var window))
        {
            window = new EditorWindow();
            window.Attach(vm);
            Windows.Add(vm, window);
        }

        return window;
    }

    private bool _maximized;

    private readonly DispatcherTimer _timer;
    private EditorViewModel? _vm;

    /// <summary>Crée la fenêtre, cachée ; <see cref="Attach"/> la branche sur son modèle de vue.</summary>
    public EditorWindow()
    {
        AvaloniaXamlLoader.Load(this);

        // Le geste en cours est écrit et les panneaux rafraîchis 20 fois par seconde, même si l'écran de jeu n'est plus affiché.
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background, (_, _) => _vm?.Refresh());
        Closing += OnClosing;
    }

    /// <summary>Branche la fenêtre sur le modèle de vue et y place les panneaux d'édition de l'établi, chacun avec son « ? » (F6).</summary>
    public void Attach(EditorViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);
        _vm = vm;
        DataContext = vm;
        var work = vm.Workbench;
        this.FindControl<ContentControl>("PlanHost")!.Content = ControlPanelTemplate.WithHelp(new PlanPanelView { DataContext = work.Plan }, ControlPanels.Get(ControlPanels.Plan));
        this.FindControl<ContentControl>("SettingsHost")!.Content = ControlPanelTemplate.WithHelp(new SettingsPanelView { DataContext = work.Settings }, ControlPanels.Get(ControlPanels.Settings));
        this.FindControl<ContentControl>("EffectsHost")!.Content = ControlPanelTemplate.WithHelp(new EffectsPanelView { DataContext = work.Effects }, ControlPanels.Get(ControlPanels.Effects));
        this.FindControl<ContentControl>("PropertiesHost")!.Content = ControlPanelTemplate.WithHelp(new PropertiesPanelView { DataContext = work.Properties }, ControlPanels.Get(ControlPanels.Properties));
        vm.Closed += (_, _) =>
        {
            _timer.Stop();
            Remember();
            Hide();
        };
    }

    /// <summary>Montre la fenêtre (ou la ramène au premier plan) ; démarre son rafraîchissement.</summary>
    public void Present(Window? owner)
    {
        _timer.Start();
        if (!IsVisible)
        {
            FitToScreen(owner);
            if (owner is not null)
            {
                Show(owner);
            }
            else
            {
                Show();
            }
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        if (_maximized)
        {
            WindowState = WindowState.Maximized;
        }

        Activate();
    }

    // À chaque ouverture : la fenêtre n'est jamais plus grande que la fenêtre principale ni que l'écran (essai 1.007.092 : encore
    // trop grande quand la fenêtre principale est réduite à 1366 × 768) ; si elle était maximisée, elle le redevient.
    private void FitToScreen(Window? owner)
    {
        var screen = (owner is not null ? Screens.ScreenFromWindow(owner) : null) ?? Screens.Primary;
        var maxWidth = double.PositiveInfinity;
        var maxHeight = double.PositiveInfinity;
        if (screen is not null)
        {
            var scaling = screen.Scaling <= 0 ? 1 : screen.Scaling;
            maxWidth = (screen.WorkingArea.Width / scaling) - 40;
            maxHeight = (screen.WorkingArea.Height / scaling) - 40;
        }

        if (owner is { WindowState: not WindowState.Minimized, Bounds: { Width: > 0, Height: > 0 } bounds })
        {
            maxWidth = Math.Min(maxWidth, bounds.Width);
            maxHeight = Math.Min(maxHeight, bounds.Height);
        }

        // MinWidth / MinHeight cèdent : mieux vaut une fenêtre un peu serrée qu'une fenêtre qui déborde.
        MinWidth = Math.Min(1000, maxWidth);
        MinHeight = Math.Min(620, maxHeight);
        Width = Math.Min(Width, maxWidth);
        Height = Math.Min(Height, maxHeight);
    }

    private void Remember()
    {
        _maximized = WindowState == WindowState.Maximized;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        // Dans un champ de texte, Ctrl+Z reste celui du champ.
        if (FocusManager?.GetFocusedElement() is TextBox)
        {
            base.OnKeyDown(e);
            return;
        }

        if ((e.KeyModifiers & KeyModifiers.Control) != 0 && e.Key is Key.Z or Key.Y)
        {
            if (e.Key == Key.Z)
            {
                _vm?.Undo();
            }
            else
            {
                _vm?.Redo();
            }

            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // Fermeture de l'application ou de sa fenêtre principale : le brouillon n'a jamais été écrit, il est abandonné, sans
        // question (essai 1.007.080 : la fenêtre restait ouverte et LuXia ne se fermait plus).
        if (e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown or WindowCloseReason.OwnerWindowClosing)
        {
            _vm?.Abandon();
            _timer.Stop();
            return;
        }

        // La croix ne perd jamais un brouillon par mégarde : sans modification elle ferme (en cachant), sinon elle demande.
        e.Cancel = true;
        _ = CloseWithConfirmationAsync();
    }

    private async Task CloseWithConfirmationAsync()
    {
        if (_vm is null || await _vm.ConfirmCloseAsync().ConfigureAwait(true))
        {
            _timer.Stop();
            Remember();
            Hide();
        }
    }
}
