using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>Vue du journal.</summary>
public partial class JournalPanelView : UserControl
{
    /// <summary>Crée la vue.</summary>
    public JournalPanelView() => AvaloniaXamlLoader.Load(this);
}
