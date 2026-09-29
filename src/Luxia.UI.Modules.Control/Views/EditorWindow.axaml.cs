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
            Hide();
        };
    }

    /// <summary>Montre la fenêtre (ou la ramène au premier plan) ; démarre son rafraîchissement.</summary>
    public void Present(Window? owner)
    {
        _timer.Start();
        if (!IsVisible)
        {
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

        Activate();
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
        if (e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown)
        {
            _vm?.Abandon();
            _timer.Stop();
            return;
        }

        // La croix ne perd jamais un brouillon : sans modification elle ferme (en cachant), sinon elle laisse choisir.
        e.Cancel = true;
        if (_vm?.TryClose() ?? true)
        {
            _timer.Stop();
            Hide();
        }
    }
}
