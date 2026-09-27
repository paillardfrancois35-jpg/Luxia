using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;

namespace Luxia.UI.Modules.Live;

/// <summary>Écran Live : appui / relâche des boutons à maintenir (flash, strobe, fumée, scènes des couches Flash).</summary>
public partial class LiveView : UserControl
{
    /// <summary>Bordure : liseré à gauche (couleur de la scène), cadre complet quand elle joue.</summary>
    public static readonly IValueConverter ActiveThickness =
        new FuncValueConverter<bool, Thickness>(active => active ? new Thickness(4, 2, 2, 2) : new Thickness(4, 0, 0, 0));

    /// <summary>Nom en gras quand la scène joue.</summary>
    public static readonly IValueConverter ActiveWeight =
        new FuncValueConverter<bool, FontWeight>(active => active ? FontWeight.Bold : FontWeight.Normal);

    /// <summary>Cadre de la couche choisie au clavier.</summary>
    public static readonly IValueConverter SelectedBorder =
        new FuncValueConverter<bool, IBrush>(selected => new SolidColorBrush(Color.Parse(selected ? "#58A6FF" : "#30363D")));

    /// <summary>Fond d'avertissement (limite de sûreté active).</summary>
    public static readonly IValueConverter WarningBackground =
        new FuncValueConverter<bool, IBrush>(warning => new SolidColorBrush(Color.Parse(warning ? "#9E6A03" : "#161B22")));

    /// <summary>Boutons de fumée estompés pendant le repos (MOT-081).</summary>
    public static readonly IValueConverter RestOpacity = new FuncValueConverter<bool, double>(resting => resting ? 0.45 : 1);

    /// <summary>Crée la vue.</summary>
    public LiveView()
    {
        InitializeComponent();

        // Colonnes réparties sur la largeur visible, sans descendre sous leur largeur minimale : au-delà, la zone défile
        // horizontalement (écran étroit, mise à l'échelle de Windows) au lieu de couper des colonnes.
        ColumnsScroll.SizeChanged += (_, _) => FitColumns();
        DataContextChanged += (_, _) =>
        {
            if (ViewModel is { } vm)
            {
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(LiveViewModel.ColumnsMinWidth))
                    {
                        FitColumns();
                    }
                };
            }

            FitColumns();
        };
    }

    private void FitColumns()
    {
        var available = ColumnsScroll.Bounds.Width;
        if (available > 0)
        {
            ColumnsList.Width = Math.Max(available, ViewModel?.ColumnsMinWidth ?? 0);
        }
    }

    private LiveViewModel? ViewModel => DataContext as LiveViewModel;

    private static LiveSceneViewModel? SceneOf(object? sender) => (sender as Control)?.Tag as LiveSceneViewModel;

    private void OnScenePressed(object? sender, PointerPressedEventArgs e)
    {
        if (SceneOf(sender) is { } scene && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            e.Pointer.Capture(sender as IInputElement);
            ViewModel?.Press(scene);
            e.Handled = true;
        }
    }

    private void OnSceneReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (SceneOf(sender) is { } scene)
        {
            ViewModel?.Release(scene);
            e.Pointer.Capture(null);
        }
    }

    private void OnSceneCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        // Sécurité : un flash ne doit jamais rester « coincé » si le pointeur est perdu.
        if (SceneOf(sender) is { } scene)
        {
            ViewModel?.Release(scene);
        }
    }

    private void OnFlashPressed(object? sender, PointerPressedEventArgs e) => Hold(sender, e, vm => vm.Flash(true));

    private void OnFlashReleased(object? sender, PointerReleasedEventArgs e) => ViewModel?.Flash(false);

    private void OnFlashLost(object? sender, PointerCaptureLostEventArgs e) => ViewModel?.Flash(false);

    private void OnStrobePressed(object? sender, PointerPressedEventArgs e) => Hold(sender, e, vm => vm.Strobe(true));

    private void OnStrobeReleased(object? sender, PointerReleasedEventArgs e) => ViewModel?.Strobe(false);

    private void OnStrobeLost(object? sender, PointerCaptureLostEventArgs e) => ViewModel?.Strobe(false);

    private void OnSmokePressed(object? sender, PointerPressedEventArgs e) => Hold(sender, e, vm => vm.Smoke(true));

    private void OnSmokeReleased(object? sender, PointerReleasedEventArgs e) => ViewModel?.Smoke(false);

    private void OnSmokeLost(object? sender, PointerCaptureLostEventArgs e) => ViewModel?.Smoke(false);

    private void Hold(object? sender, PointerPressedEventArgs e, Action<LiveViewModel> action)
    {
        if (ViewModel is { } vm && (sender as Control)?.IsEnabled != false)
        {
            e.Pointer.Capture(sender as IInputElement);
            action(vm);
            e.Handled = true;
        }
    }
}
