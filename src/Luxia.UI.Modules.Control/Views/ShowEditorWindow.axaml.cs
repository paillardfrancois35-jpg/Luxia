using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Luxia.UI.Modules.Control.Sequencing;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>Fenêtre d'édition d'un show (maquette 12) : un clic sur une étape du diagramme amène sa carte à l'écran.</summary>
public partial class ShowEditorWindow : SequencingWindow
{
    /// <summary>Crée la fenêtre.</summary>
    public ShowEditorWindow()
    {
        AvaloniaXamlLoader.Load(this);
        var list = this.FindControl<ItemsControl>("CardsList")!;
        this.FindControl<ShowDiagram>("Diagram")!.NodeClicked += (_, id) =>
        {
            if (DataContext is ShowEditorViewModel vm && vm.Cards.ToList().FindIndex(c => c.Id == id) is var index and >= 0)
            {
                list.ContainerFromIndex(index)?.BringIntoView();
            }
        };
    }
}
