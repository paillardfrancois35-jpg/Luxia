using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>Vue des propriétés : le choix d'une étape dans la bande part au modèle de vue.</summary>
public partial class PropertiesPanelView : UserControl
{
    /// <summary>Crée la vue.</summary>
    public PropertiesPanelView()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<StepStrip>("Strip")!.StepSelected += (_, index) => (DataContext as PropertiesPanelViewModel)?.ChooseStep(index);
    }
}
