using Avalonia;
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
        // En-têtes et scènes partagent cette largeur : leurs colonnes restent alignées (ERG-030).
        var scroll = this.FindControl<ScrollViewer>("Scroll")!;
        var board = this.FindControl<DockPanel>("Board")!;
        scroll.SizeChanged += (_, e) => board.Width = Math.Max(e.NewSize.Width - 4, board.MinWidth);
    }

    // La colonne ne doit pas bouger quand la liste est recréée (essai P7 : l'édition validée remettait la barre en haut). La position est
    // mémorisée dans la colonne et rétablie dès que le contenu est mesuré.
    private void OnColumnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not ScrollViewer scroll || scroll.DataContext is not ControlColumnViewModel column || column.ScrollOffset <= 0)
        {
            return;
        }

        scroll.Tag = "restoring";
        var tries = 0;
        void Restore(object? s, EventArgs args)
        {
            scroll.Offset = new Vector(0, column.ScrollOffset);
            if (Math.Abs(scroll.Offset.Y - column.ScrollOffset) < 0.5 || ++tries > 20)
            {
                scroll.LayoutUpdated -= Restore;
                scroll.Tag = null;
            }
        }

        scroll.LayoutUpdated += Restore;
    }

    private void OnColumnScrolled(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is ScrollViewer { Tag: null } scroll && scroll.DataContext is ControlColumnViewModel column)
        {
            column.ScrollOffset = scroll.Offset.Y;
        }
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

    private static Sequencing.ShowItemViewModel? ShowOf(object? sender) => (sender as Avalonia.Controls.Control)?.DataContext as Sequencing.ShowItemViewModel;

    // Colonne « Shows » : clic = lancer ou arrêter (bascule tranchée par le séquenceur).
    private void OnShowPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ShowOf(sender) is { } item && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            ViewModel?.Shows.Press(item);
            e.Handled = true;
        }
    }

    // Clic droit d'un show ou d'une séquence : mêmes verbes que pour une scène (doc 60 §4.4).
    private void OnShowContext(object? sender, ContextRequestedEventArgs e)
    {
        if (sender is not Avalonia.Controls.Control control || ShowOf(sender) is not { } item || ViewModel is not { } vm)
        {
            return;
        }

        var shows = vm.Shows;
        var menu = new ContextMenu
        {
            Items =
            {
                new MenuItem { Header = item.IsShow ? "✎ Éditer ce show" : "✎ Éditer cette séquence", Command = shows.EditCommand, CommandParameter = item, IsEnabled = vm.CanEdit },
                new MenuItem { Header = "Renommer…", Command = shows.RenameCommand, CommandParameter = item, IsEnabled = vm.CanEdit },
                new MenuItem { Header = "Dupliquer", Command = shows.DuplicateCommand, CommandParameter = item, IsEnabled = vm.CanEdit },
                new Separator(),
                new MenuItem { Header = "Supprimer…", IsEnabled = vm.CanEdit, Foreground = new SolidColorBrush(Color.Parse("#F85149")), Command = shows.DeleteCommand, CommandParameter = item },
            },
        };
        menu.Open(control);
        e.Handled = true;
    }

    // Clic droit (doc 60 §4.4) : mêmes verbes, même ordre partout ; menu construit à l'ouverture (couleurs, couches).
    private void OnSceneContext(object? sender, ContextRequestedEventArgs e)
    {
        if (sender is not Avalonia.Controls.Control control || SceneOf(sender) is not { } scene || ViewModel is not { } vm)
        {
            return;
        }

        var colors = new MenuItem { Header = "Couleur", IsEnabled = vm.CanEdit };
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

        var layers = new MenuItem { Header = "Couche", IsEnabled = vm.CanEdit };
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
                new MenuItem { Header = "Renommer…", Command = vm.RenameCommand, CommandParameter = scene, IsEnabled = vm.CanEdit },
                new MenuItem { Header = "Dupliquer", Command = vm.DuplicateCommand, CommandParameter = scene, IsEnabled = vm.CanEdit },
                colors,
                layers,
                new MenuItem { Header = scene.HiddenInLive ? "Montrer dans le Live" : "Masquer du Live", Command = vm.ToggleVisibleInLiveCommand, CommandParameter = scene },
                new Separator(),
                new MenuItem { Header = "Supprimer…", IsEnabled = vm.CanEdit, Foreground = new SolidColorBrush(Color.Parse("#F85149")), Command = vm.DeleteCommand, CommandParameter = scene },
            },
        };
        menu.Open(control);
        e.Handled = true;
    }
}
