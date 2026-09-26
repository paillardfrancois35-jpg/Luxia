using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Console;

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

    /// <summary>Saisie directe d'une valeur (BIB-096, comme la Console).</summary>
    private void OnValueBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not TextBox { Tag: FixtureChannelViewModel channel } box)
        {
            return;
        }

        Commit(box, channel);
        e.Handled = true;
    }

    private void OnValueBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox { Tag: FixtureChannelViewModel channel } box)
        {
            Commit(box, channel);
        }
    }

    private void Commit(TextBox box, FixtureChannelViewModel channel)
    {
        if (int.TryParse(box.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value) && value is >= 0 and <= 255)
        {
            ViewModel?.SetValue(channel, value);
        }
        else
        {
            box.Text = channel.Value.ToString(CultureInfo.CurrentCulture);
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
