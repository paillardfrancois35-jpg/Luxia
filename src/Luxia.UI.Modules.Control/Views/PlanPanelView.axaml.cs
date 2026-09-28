using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>Vue du plan des appareils : la sélection faite à la souris part au modèle de vue.</summary>
public partial class PlanPanelView : UserControl
{
    /// <summary>Crée la vue.</summary>
    public PlanPanelView()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<SimulatorCanvas>("Plan")!.SelectionRequested += (_, request) => (DataContext as PlanPanelViewModel)?.OnSelectionRequested(request);
    }
}
