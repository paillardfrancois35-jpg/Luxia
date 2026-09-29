using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>Fenêtre « Thème de couleurs » (PAL-010), ouverte depuis le panneau Effets.</summary>
public partial class ThemeEditorWindow : Window
{
    /// <summary>Crée la fenêtre.</summary>
    public ThemeEditorWindow()
    {
        AvaloniaXamlLoader.Load(this);
        var picker = this.FindControl<ColorPicker>("Picker")!;
        picker.ColorRequested += (_, color) => (DataContext as ThemeEditorViewModel)?.RequestColor(color);
        DataContextChanged += (_, _) =>
        {
            if (DataContext is ThemeEditorViewModel editor)
            {
                editor.Closed += (_, _) => Close();
            }
        };
    }
}
