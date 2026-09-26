using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Luxia.UI.Modules.Scenes;

/// <summary>Écran « Scènes ».</summary>
public partial class ScenesView : UserControl
{
    /// <summary>Bordure de l'étape courante (bleue) ou des autres (grise).</summary>
    public static readonly IValueConverter CurrentBorder =
        new FuncValueConverter<bool, IBrush>(current => current ? Brushes.DeepSkyBlue : new SolidColorBrush(Color.Parse("#30363D")));

    /// <summary>Crée la vue.</summary>
    public ScenesView() => InitializeComponent();

    private ScenesViewModel? ViewModel => DataContext as ScenesViewModel;

    private void OnLaunchClicked(object? sender, RoutedEventArgs e) =>
        ViewModel?.LaunchCommand.Execute((sender as Control)?.Tag as SceneRowViewModel);

    private void OnStopClicked(object? sender, RoutedEventArgs e) =>
        ViewModel?.StopCommand.Execute((sender as Control)?.Tag as SceneRowViewModel);

    private void OnStepPressed(object? sender, PointerPressedEventArgs e)
    {
        // Un clic sur la case à cocher de l'étape ne doit pas changer d'étape courante.
        if (e.Source is Control source && source.FindAncestorOfType<CheckBox>(includeSelf: true) is not null)
        {
            return;
        }

        ViewModel?.Editor.SelectStepCommand.Execute((sender as Control)?.Tag as StepRowViewModel);
    }

    private async void OnLayersClicked(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.CreateLayersEditor() is not { } editor || TopLevel.GetTopLevel(this) is not Window owner)
        {
            return;
        }

        await new LayersWindow { DataContext = editor }.ShowDialog(owner).ConfigureAwait(true);
        if (editor.Saved)
        {
            ViewModel.ReloadAll();
        }
    }

    private void OnZonesClicked(object? sender, RoutedEventArgs e)
    {
        // Non modale : on vise avec le programmeur pendant que la fenêtre est ouverte.
        if (ViewModel?.CreateZonesEditor() is { } editor && TopLevel.GetTopLevel(this) is Window owner)
        {
            new ZonesWindow { DataContext = editor }.Show(owner);
        }
    }

    private void OnShortcutClicked(object? sender, RoutedEventArgs e) =>
        ViewModel?.Programmer.SelectCommand.Execute((sender as Control)?.Tag as SelectionShortcut);
}
