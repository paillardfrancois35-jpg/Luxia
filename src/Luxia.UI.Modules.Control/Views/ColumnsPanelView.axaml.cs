using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>Vue du panneau Colonnes : appui / relâche des scènes (flash maintenu), sous-menus Couleur et Couche.</summary>
public partial class ColumnsPanelView : UserControl
{
    /// <summary>Crée la vue.</summary>
    public ColumnsPanelView()
    {
        AvaloniaXamlLoader.Load(this);

        // Largeur des colonnes = largeur visible (partagée), au moins la largeur minimale : sans cela, dans une zone qui
        // défile, chaque colonne prend la largeur de son plus long nom de scène.
        var scroll = this.FindControl<ScrollViewer>("Scroll")!;
        var list = this.FindControl<ItemsControl>("ColumnsList")!;
        scroll.SizeChanged += (_, e) => list.Width = Math.Max(e.NewSize.Width - 4, list.MinWidth);
    }

    private ColumnsPanelViewModel? ViewModel => DataContext as ColumnsPanelViewModel;

    private static ControlSceneViewModel? SceneOf(object? sender) => (sender as Avalonia.Controls.Control)?.DataContext as ControlSceneViewModel;

    private void OnScenePressed(object? sender, PointerPressedEventArgs e)
    {
        if (SceneOf(sender) is { } scene && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            e.Pointer.Capture(sender as IInputElement);
            ViewModel?.Press(scene);
            e.Handled = true;
        }
    }

    private void OnSceneReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (SceneOf(sender) is { } scene)
        {
            ViewModel?.Release(scene);
            e.Pointer.Capture(null);
        }
    }

    private void OnSceneCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (SceneOf(sender) is { } scene)
        {
            ViewModel?.Release(scene);
        }
    }

    // Clic droit (doc 60 §4.4) : mêmes verbes, même ordre partout ; menu construit à l'ouverture (couleurs, couches).
    private void OnSceneContext(object? sender, ContextRequestedEventArgs e)
    {
        if (sender is not Avalonia.Controls.Control control || SceneOf(sender) is not { } scene || ViewModel is not { } vm)
        {
            return;
        }

        var colors = new MenuItem { Header = "Couleur" };
        foreach (var color in ColumnsPanelViewModel.SceneColors)
        {
            colors.Items.Add(new MenuItem
            {
                Header = color == scene.Color ? "✓" : " ",
                Icon = new Border { Width = 40, Height = 14, CornerRadius = new Avalonia.CornerRadius(3), Background = new SolidColorBrush(Color.Parse(color)) },
                Command = vm.SetColorCommand,
                CommandParameter = $"{scene.Scene.Id}|{color}",
            });
        }

        var layers = new MenuItem { Header = "Couche" };
        foreach (var layer in vm.Layers)
        {
            layers.Items.Add(new MenuItem
            {
                Header = (layer.Id == scene.Scene.LayerId ? "✓ " : "    ") + layer.Name,
                Command = vm.MoveToLayerCommand,
                CommandParameter = $"{scene.Scene.Id}|{layer.Id}",
            });
        }

        var menu = new ContextMenu
        {
            Items =
            {
                new MenuItem { Header = "✎ Éditer cette scène", Command = vm.ChooseForEditCommand, CommandParameter = scene },
                new MenuItem { Header = "Renommer…", Command = vm.RenameCommand, CommandParameter = scene },
                new MenuItem { Header = "Dupliquer", Command = vm.DuplicateCommand, CommandParameter = scene },
                colors,
                layers,
                new MenuItem { Header = scene.HiddenInLive ? "Montrer dans le Live" : "Masquer du Live", Command = vm.ToggleVisibleInLiveCommand, CommandParameter = scene },
                new Separator(),
                new MenuItem { Header = "Supprimer…", Foreground = new SolidColorBrush(Color.Parse("#F85149")), Command = vm.DeleteCommand, CommandParameter = scene },
            },
        };
        menu.Open(control);
        e.Handled = true;
    }
}
