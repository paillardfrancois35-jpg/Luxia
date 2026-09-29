using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Luxia.UI.Modules.Control.Docking;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>
/// Vue de l'écran de jeu : ancrage des panneaux (Dock), disposition enregistrée sur le poste dès qu'elle change
/// (vérification toutes les 2 s, et en quittant l'écran), menu Panneaux, raccourcis Ctrl+Z / Ctrl+Y / Échap / F1 à F12.
/// </summary>
public partial class GameView : UserControl
{
    private readonly DockControl _dock;
    private readonly DispatcherTimer _autosave;
    private ControlDockFactory? _factory;
    private ControlLayoutStore? _store;
    private IRootDock? _layout;
    private string? _saved;

    /// <summary>Crée la vue.</summary>
    public GameView()
    {
        AvaloniaXamlLoader.Load(this);
        _dock = this.FindControl<DockControl>("DockHost")!;
        // Menu construit à chaque clic puis ouvert : rempli pendant « Opening », il s'ouvrait vide (essai 1.005.198).
        var panelsButton = this.FindControl<Button>("PanelsButton")!;
        panelsButton.Click += (_, _) =>
        {
            var menu = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedRight };
            FillPanelsMenu(menu);
            menu.ShowAt(panelsButton);
        };
        this.FindControl<Button>("ResetLayoutButton")!.Click += (_, _) => ResetLayout();
        _autosave = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Background, (_, _) => SaveLayout());

        // Au niveau de l'application : un panneau détaché vit dans une autre fenêtre et doit y retrouver son contenu.
        if (Application.Current is { } app && !app.DataTemplates.OfType<ControlPanelTemplate>().Any())
        {
            app.DataTemplates.Add(new ControlPanelTemplate());
        }
    }

    private GameViewModel? ViewModel => DataContext as GameViewModel;

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (ViewModel is { } vm && _factory is null)
        {
            BuildLayout(vm);

            // ✎ : la fenêtre d'édition (ERG-033), non bloquante, sur un second écran si on veut.
            vm.EditRequested += (_, _) => ShowEditor(vm);
            vm.PanelRequested += (_, id) =>
            {
                if (_layout is not null && _factory is not null)
                {
                    _factory.ShowPanel(_layout, id);
                    AssignContexts(_layout);
                    SaveLayout();
                }
            };
        }
    }

    private EditorWindow? _editor;

    private void ShowEditor(GameViewModel vm)
    {
        if (_editor is null)
        {
            _editor = new EditorWindow();
            _editor.Attach(vm.Editor);
        }

        _editor.Present(TopLevel.GetTopLevel(this) as Window);
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _autosave.Start();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _autosave.Stop();
        ViewModel?.Flush();
        SaveLayout();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        // Dans un champ de texte, Ctrl+Z reste celui du champ.
        if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is TextBox)
        {
            base.OnKeyDown(e);
            return;
        }

        var ctrl = (e.KeyModifiers & KeyModifiers.Control) != 0;
        if (ViewModel is { } vm)
        {
            if (ctrl && e.Key == Key.Z)
            {
                vm.Undo();
                e.Handled = true;
                return;
            }

            if (ctrl && e.Key == Key.Y)
            {
                vm.Redo();
                e.Handled = true;
                return;
            }

            // ERG-023 : F1 à F12 jouent les looks 1 à 12.
            if (e.Key is >= Key.F1 and <= Key.F12 && e.KeyModifiers == KeyModifiers.None && vm.Looks.PlayAt(e.Key - Key.F1))
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape && vm.HasRetouches)
            {
                vm.ReleaseAllCommand.Execute(null);
                e.Handled = true;
                return;
            }
        }

        base.OnKeyDown(e);
    }

    private void BuildLayout(GameViewModel vm)
    {
        var contexts = new Dictionary<string, Func<object?>>
        {
            [ControlPanels.Columns] = () => vm.Columns,
            [ControlPanels.Journal] = () => vm.Journal,
            [ControlPanels.Looks] = () => vm.Looks,
            [ControlPanels.Pilot] = () => vm.Looks,
            [ControlPanels.Dimmers] = () => vm.Dimmers,
        };
        _factory = new ControlDockFactory(contexts);
        _store = new ControlLayoutStore(vm.LayoutFolder);
        var loaded = _store.Load(out var message);
        if (message is not null)
        {
            vm.Message = message;
        }

        SetLayout(loaded ?? _factory.CreateLayout());
        _saved = loaded is null ? null : _store.Serialize(_layout!);
    }

    private void SetLayout(IRootDock layout)
    {
        _factory!.InitLayout(layout);
        AssignContexts(layout);
        _layout = layout;
        _dock.Layout = layout;
    }

    // Les modèles de vue ne sont pas enregistrés avec la disposition : on les rend à chaque panneau, partout où Dock
    // l'a rangé (fenêtres détachées et panneaux fermés compris).
    private void AssignContexts(IRootDock root)
    {
        // Dock lie ses boutons à ces objets de « capacités » : nuls, chaque liaison écrivait un avertissement au journal
        // technique (vu au premier lancement réel) ; vides, ils ne changent rien au comportement.
        foreach (var dockable in DockTree.All(root))
        {
            dockable.DockCapabilityOverrides ??= new DockCapabilityOverrides();
            if (dockable is IDock dock)
            {
                dock.DockCapabilityPolicy ??= new DockCapabilityPolicy();
            }
        }

        root.RootDockCapabilityPolicy ??= new DockCapabilityPolicy();

        if (ViewModel is not { } vm)
        {
            return;
        }

        foreach (var panel in ControlPanels.All)
        {
            var (dockable, _) = DockTree.Find(root, panel.Id);
            if (dockable is not null)
            {
                dockable.Context = panel.Id switch
                {
                    ControlPanels.Columns => vm.Columns,
                    ControlPanels.Looks or ControlPanels.Pilot => vm.Looks,
                    ControlPanels.Dimmers => vm.Dimmers,
                    _ => vm.Journal,
                };
            }
        }
    }

    private void SaveLayout()
    {
        if (_store is null || _layout is null)
        {
            return;
        }

        try
        {
            var text = _store.Serialize(_layout);
            if (text != _saved)
            {
                _store.Save(text);
                _saved = text;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException or System.Text.Json.JsonException)
        {
            if (ViewModel is { } vm)
            {
                vm.Message = $"Disposition des panneaux non enregistrée : {ex.Message}";
            }
        }
    }

    private void ResetLayout()
    {
        if (_factory is null || _store is null)
        {
            return;
        }

        foreach (var window in _layout?.Windows?.ToList() ?? [])
        {
            window.Host?.Exit();
        }

        _store.Delete();
        _saved = null;
        SetLayout(_factory.CreateLayout());
        SaveLayout();
    }

    private void FillPanelsMenu(MenuFlyout menu)
    {
        menu.Items.Clear();
        if (_layout is null || _factory is null)
        {
            return;
        }

        foreach (var panel in ControlPanels.Game)
        {
            var place = DockTree.Find(_layout, panel.Id).Place;
            var item = new MenuItem
            {
                Header = panel.Title + place switch
                {
                    PanelPlace.Hidden => "  (fermé)",
                    PanelPlace.Pinned => "  (replié)",
                    PanelPlace.Floating => "  (détaché : le remettre en place)",
                    PanelPlace.Absent => "  (absent)",
                    _ => string.Empty,
                },
                Icon = new TextBlock { Text = place == PanelPlace.Visible ? "✓" : string.Empty },
            };
            var id = panel.Id;
            item.Click += (_, _) =>
            {
                _factory.ShowPanel(_layout, id);
                AssignContexts(_layout);
                SaveLayout();
            };
            menu.Items.Add(item);
        }
    }
}
