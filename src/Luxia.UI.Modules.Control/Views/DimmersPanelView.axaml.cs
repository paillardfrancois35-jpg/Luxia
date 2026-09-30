using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>Vue des dimmers de groupe : un fader par groupe ; double-clic = remettre ce dimmer à 100 %.</summary>
public partial class DimmersPanelView : UserControl
{
    /// <summary>Crée la vue.</summary>
    public DimmersPanelView() => AvaloniaXamlLoader.Load(this);

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Avalonia.Controls.Control { DataContext: DimmerFaderViewModel fader })
        {
            fader.Percent = 100;
            e.Handled = true;
        }
    }
}
