using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>Vue des looks : clic = jouer ; clic droit = Mettre à jour, Renommer, Couleur, Supprimer (§4.4).</summary>
public partial class LooksPanelView : UserControl
{
    /// <summary>Crée la vue.</summary>
    public LooksPanelView() => AvaloniaXamlLoader.Load(this);

    private void OnLookContext(object? sender, Avalonia.Input.ContextRequestedEventArgs e)
    {
        if (sender is not Avalonia.Controls.Control control || control.DataContext is not LookButtonViewModel look || DataContext is not LooksPanelViewModel vm)
        {
            return;
        }

        var colors = new MenuItem { Header = "Couleur", IsEnabled = vm.CanEdit };
        foreach (var color in ColumnsPanelViewModel.SceneColors)
        {
            colors.Items.Add(new MenuItem
            {
                Header = color == look.Color ? "✓" : " ",
                Icon = new Border { Width = 40, Height = 14, CornerRadius = new Avalonia.CornerRadius(3), Background = new SolidColorBrush(Color.Parse(color)) },
                Command = vm.SetColorCommand,
                CommandParameter = $"{look.Look.Id}|{color}",
            });
        }

        new ContextMenu
        {
            Items =
            {
                new MenuItem { Header = "▶ Jouer", Command = vm.PlayCommand, CommandParameter = look },
                new MenuItem { Header = "Mettre à jour avec ce qui joue…", Command = vm.UpdateFromStateCommand, CommandParameter = look, IsEnabled = vm.CanEdit },
                new MenuItem { Header = "Renommer…", Command = vm.RenameCommand, CommandParameter = look, IsEnabled = vm.CanEdit },
                colors,
                new Separator(),
                new MenuItem { Header = "Supprimer…", Foreground = new SolidColorBrush(Color.Parse("#F85149")), Command = vm.DeleteCommand, CommandParameter = look, IsEnabled = vm.CanEdit },
            },
        }.Open(control);
        e.Handled = true;
    }
}
