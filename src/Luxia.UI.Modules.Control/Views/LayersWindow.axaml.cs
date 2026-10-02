using Avalonia.Controls;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>Fenêtre de l'éditeur de couches (COU-001) : atelier seulement, jamais ouverte depuis le Live.</summary>
public partial class LayersWindow : Window
{
    /// <summary>Crée la fenêtre.</summary>
    public LayersWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is LayersEditorViewModel editor)
            {
                editor.Closed += (_, _) => Close();
            }
        };
    }
}
