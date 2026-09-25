using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Dmx.UI.Controls;

namespace Dmx.UI.Modules.Console;

/// <summary>Composant « faders d'un appareil » (CONS-060).</summary>
public partial class FixtureFadersView : UserControl
{
    /// <summary>Crée la vue.</summary>
    public FixtureFadersView() => InitializeComponent();

    private FixtureFadersViewModel? ViewModel => DataContext as FixtureFadersViewModel;

    private void OnFaderValueRequested(object? sender, FaderValueRequest request)
    {
        if (sender is Control { Tag: FixtureChannelViewModel channel })
        {
            ViewModel?.SetValue(channel, request.NewValue);
        }
    }

    private void OnRangeBarClicked(object? sender, (int Index, int Value) e)
    {
        if (sender is Control { Tag: FixtureChannelViewModel channel })
        {
            ViewModel?.SetValue(channel, e.Value);
        }
    }

    private void OnRangeBarBoundaryClicked(object? sender, int value)
    {
        if (sender is Control { Tag: FixtureChannelViewModel channel })
        {
            ViewModel?.SetValue(channel, value);
        }
    }

    private void OnDiscoverClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: FixtureChannelViewModel channel })
        {
            ViewModel?.StartDiscovery(channel);
        }
    }

    private void OnRangeButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { Tag: RangeButton range } button)
        {
            return;
        }

        // La tranche est le premier parent dont le contexte est le canal.
        var channel = button.GetVisualAncestors().OfType<Control>().Select(c => c.DataContext).OfType<FixtureChannelViewModel>().FirstOrDefault();
        if (channel is not null)
        {
            ViewModel?.SelectRange(channel, range.Capability);
        }
    }
}
