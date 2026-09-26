using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Luxia.UI.Modules.Installation;

/// <summary>Écran « Installation » (doc 13).</summary>
public partial class InstallationView : UserControl
{
    /// <summary>Crée la vue.</summary>
    public InstallationView() => InitializeComponent();

    private InstallationViewModel? ViewModel => DataContext as InstallationViewModel;

    private void OnRenameClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: PatchRowViewModel row })
        {
            ViewModel?.RenameFixture(row);
        }
    }

    private void OnMoveClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: PatchRowViewModel row })
        {
            ViewModel?.MoveFixture(row);
        }
    }

    private async void OnChangeModeClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: PatchRowViewModel row } && ViewModel is { } vm)
        {
            await vm.ChangeModeAsync(row).ConfigureAwait(true);
        }
    }

    private void OnIdentifyClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: PatchRowViewModel row })
        {
            ViewModel?.ToggleIdentifyFixture(row);
        }
    }

    private async void OnUpdateFromLibraryClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: PatchRowViewModel row } && ViewModel is { } vm)
        {
            await vm.UpdateFromLibraryAsync(row).ConfigureAwait(true);
        }
    }

    private async void OnDeleteClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: PatchRowViewModel row } && ViewModel is { } vm)
        {
            await vm.DeleteFixtureAsync(row).ConfigureAwait(true);
        }
    }
}
