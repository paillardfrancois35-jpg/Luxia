using Avalonia.Controls;
using Avalonia.Input.Platform;

namespace Luxia.UI.Modules.Outputs;

/// <summary>Écran « Sorties ».</summary>
public partial class OutputsView : UserControl
{
    /// <summary>Crée la vue.</summary>
    public OutputsView() => InitializeComponent();

    private async void OnCopyPathClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // SORT-065 : le chemin de l'enregistrement se copie d'un clic (à coller dans un message ou l'explorateur).
        if (DataContext is OutputsViewModel { RecordingPath: { Length: > 0 } path } && TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(path).ConfigureAwait(true);
        }
    }
}
