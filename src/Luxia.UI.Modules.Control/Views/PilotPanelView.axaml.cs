using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>Vue du panneau Pilote automatique (F10, place réservée jusqu'à P10).</summary>
public partial class PilotPanelView : UserControl
{
    /// <summary>Crée la vue.</summary>
    public PilotPanelView() => AvaloniaXamlLoader.Load(this);
}
