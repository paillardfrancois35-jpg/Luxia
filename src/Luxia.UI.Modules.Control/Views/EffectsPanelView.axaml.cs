using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>Vue du panneau Effets.</summary>
public partial class EffectsPanelView : UserControl
{
    /// <summary>Crée la vue.</summary>
    public EffectsPanelView() => AvaloniaXamlLoader.Load(this);
}
