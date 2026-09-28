using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Luxia.Tools.Prototype.Panels;

/// <summary>Panneau Journal : ce que les composants ont demandé, le plus récent en tête.</summary>
internal static class LogPanel
{
    public static Control Create(DemoState state) =>
        new ListBox
        {
            ItemsSource = state.Log,
            Margin = new Thickness(4, 32, 4, 4),
            FontFamily = new FontFamily("Consolas, Cascadia Mono, monospace"),
            FontSize = 12,
            Background = Brushes.Transparent,
        };
}
