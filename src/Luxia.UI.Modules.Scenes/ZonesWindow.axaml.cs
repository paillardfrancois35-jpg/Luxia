using Avalonia.Controls;

namespace Luxia.UI.Modules.Scenes;

/// <summary>Fenêtre des zones interdites du lieu actif (INST-053) : non modale, pour viser avec le programmeur.</summary>
public partial class ZonesWindow : Window
{
    /// <summary>Crée la fenêtre.</summary>
    public ZonesWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is ZonesEditorViewModel editor)
            {
                editor.Closed += (_, _) => Close();
            }
        };
    }
}
