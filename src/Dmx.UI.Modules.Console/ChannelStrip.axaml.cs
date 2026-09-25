using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Dmx.UI.Modules.Console;

/// <summary>Tranche de console : numéro, fader, saisie directe, pourcentage.</summary>
public partial class ChannelStrip : UserControl
{
    /// <summary>Crée la tranche.</summary>
    public ChannelStrip()
    {
        InitializeComponent();
        Fader.ValueRequested += (_, request) => WithContext((console, channel) => console.OnFaderRequest(channel, request));
        Fader.Pressed += (_, modifiers) => WithContext((console, channel) =>
            console.OnFaderPressed(channel, (modifiers & KeyModifiers.Control) != 0, (modifiers & KeyModifiers.Shift) != 0));
        Fader.EditRequested += (_, _) =>
        {
            ValueBox.Focus();
            ValueBox.SelectAll();
        };
        ValueBox.KeyDown += OnValueBoxKeyDown;
        ValueBox.LostFocus += (_, _) => Commit();
    }

    private void OnValueBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Commit();
            Fader.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && DataContext is ChannelViewModel channel)
        {
            ValueBox.Text = channel.ValueText;
            Fader.Focus();
            e.Handled = true;
        }
    }

    private void Commit()
    {
        if (DataContext is ChannelViewModel channel && ValueBox.Text != channel.Value.ToString(System.Globalization.CultureInfo.CurrentCulture))
        {
            WithContext((console, c) => console.OnValueTyped(c, ValueBox.Text ?? string.Empty));
        }
    }

    private void WithContext(Action<ConsoleViewModel, ChannelViewModel> action)
    {
        if (DataContext is ChannelViewModel channel
            && this.FindAncestorOfType<ConsoleView>()?.DataContext is ConsoleViewModel console)
        {
            action(console, channel);
        }
    }
}
