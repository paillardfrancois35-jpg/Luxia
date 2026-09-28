using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Dock.Avalonia.Controls;

namespace Luxia.UI.Modules.Control.Docking;

/// <summary>
/// Fenêtre d'un panneau détaché : celle de Dock, plus le double-clic sur la barre de titre qui agrandit ou restaure,
/// comme toute fenêtre Windows (essai 1.005.210 : il fallait viser le petit bouton).
/// </summary>
public sealed class ControlHostWindow : HostWindow
{
    // Parties du thème Fluent de Dock qui servent de barre de titre (poignée du panneau, barre de la fenêtre).
    private static readonly HashSet<string> TitleParts = ["PART_Grip", "PART_TitleBar", "PART_ToolHeader", "PART_Header"];

    /// <summary>Crée la fenêtre.</summary>
    public ControlHostWindow()
    {
        // En tunnel, avant la poignée qui lance le déplacement de la fenêtre.
        AddHandler(PointerPressedEvent, OnPointerPressedTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(HostWindow);

    private void OnPointerPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount != 2 || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || e.Source is not Visual source || !IsOnTitle(source))
        {
            return;
        }

        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        e.Handled = true;
    }

    // Sur une barre de titre, mais pas sur un de ses boutons (menu, épingle, fermer, agrandir).
    private static bool IsOnTitle(Visual source)
    {
        foreach (var visual in source.GetSelfAndVisualAncestors())
        {
            if (visual is Button)
            {
                return false;
            }

            if (visual is HostWindowTitleBar || (visual is Avalonia.Controls.Control { Name: { } name } && TitleParts.Contains(name)))
            {
                return true;
            }
        }

        return false;
    }
}
