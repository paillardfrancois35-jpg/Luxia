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
        this.FindControl<Button>("LayersButton")!.Click += async (_, _) =>
        {
            if (ViewModel?.CreateLayersEditor() is { } editor && TopLevel.GetTopLevel(this) is Window owner)
            {
                await new LayersWindow { DataContext = editor }.ShowDialog(owner).ConfigureAwait(true);
            }
        };

        // Forcer ▾ : une entrée par transition possible du show principal, construite au clic (SHOW-026, CMD-051).
        var forceButton = this.FindControl<Button>("ForceButton")!;
        forceButton.Click += (_, _) =>
        {
            if (ViewModel is not { } vm)
            {
                return;
            }

            var menu = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedRight };
            foreach (var transition in vm.Band.Transitions)
            {
                menu.Items.Add(new MenuItem { Header = $"{transition.Condition} → {transition.Target}", Command = vm.Band.ForceCommand, CommandParameter = transition });
            }

            if (menu.Items.Count == 0)
            {
                menu.Items.Add(new MenuItem { Header = "Aucune transition possible depuis l'étape active", IsEnabled = false });
            }

            menu.ShowAt(forceButton);
        };
        WireNowPlayingButtons();

        // Saisie du BPM : l'affichage ne l'écrase pas pendant la frappe ; la valeur est appliquée en quittant le champ (Entrée l'applique aussi).
        var bpmBox = this.FindControl<TextBox>("BpmBox")!;
        bpmBox.GotFocus += (_, _) => ViewModel?.Tempo.BeginEdit();
        bpmBox.LostFocus += (_, _) => ViewModel?.Tempo.EndEdit();
        _autosave = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Background, (_, _) => SaveLayout());

        // Au niveau de l'application : un panneau détaché vit dans une autre fenêtre et doit y retrouver son contenu.
        if (Application.Current is { } app && !app.DataTemplates.OfType<ControlPanelTemplate>().Any())
        {
            app.DataTemplates.Add(new ControlPanelTemplate());
        }
    }

    private MusicBaseWindow? _musicBaseWindow;

    // Bloc « Morceau en cours » (P9) : menus construits au clic, avec les familles de la base musicale du projet.
    private void WireNowPlayingButtons()
    {
        var correct = this.FindControl<Button>("CorrectStyleButton")!;
        correct.Click += (_, _) =>
        {
            if (ViewModel?.NowPlaying is not { } bar)
            {
                return;
            }

            var menu = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedRight };
            foreach (var (scope, header) in new[] { ("title", "Pour ce titre seulement ▸"), ("artist", "Pour tous les titres de cet artiste ▸") })
            {
                var parent = new MenuItem { Header = header };
                foreach (var family in bar.Families)
                {
                    var item = new MenuItem { Header = family.Name };
                    var request = $"{scope}:{family.Id}";
                    item.Click += async (_, _) => await bar.CorrectAsync(request).ConfigureAwait(true);
                    parent.Items.Add(item);
                }

                menu.Items.Add(parent);
            }

            menu.ShowAt(correct);
        };

        var force = this.FindControl<Button>("ForceStyleButton")!;
        force.Click += (_, _) =>
        {
            if (ViewModel?.NowPlaying is not { } bar)
            {
                return;
            }

            var menu = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedRight };
            foreach (var family in bar.Families)
            {
                var item = new MenuItem { Header = family.Name };
                var id = family.Id;
                item.Click += (_, _) => bar.Force(id);
                menu.Items.Add(item);
            }

            menu.Items.Add(new Separator());
            var revert = new MenuItem { Header = "↺ Revenir à la détection automatique" };
            revert.Click += (_, _) => bar.Force(null);
            menu.Items.Add(revert);
            menu.ShowAt(force);
        };

        var baseButton = this.FindControl<Button>("MusicBaseButton")!;
        baseButton.Click += (_, _) =>
        {
            if (_musicBaseWindow is { IsVisible: true })
            {
                _musicBaseWindow.Activate();
                return;
            }

            if (ViewModel?.CreateMusicBase() is { } editor)
            {
                _musicBaseWindow = new MusicBaseWindow { DataContext = editor };
                _musicBaseWindow.Show(TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("Fenêtre principale absente"));
            }
        };

        var manual = this.FindControl<Button>("ManualTrackButton")!;
        manual.Click += (_, _) =>
        {
            if (ViewModel?.NowPlaying is not { } bar)
            {
                return;
            }

            var title = new TextBox { PlaceholderText = "Titre", Width = 280 };
            var artist = new TextBox { PlaceholderText = "Artiste", Width = 280 };
            var ok = new Button { Content = "Identifier ce morceau", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
            var flyout = new Flyout
            {
                Placement = PlacementMode.BottomEdgeAlignedRight,
                Content = new StackPanel { Spacing = 6, Children = { new TextBlock { Text = "Morceau joué (aucun lecteur ne l'annonce)" }, title, artist, ok } },
            };
            ok.Click += (_, _) =>
            {
                bar.SetManual(title.Text, artist.Text);
                flyout.Hide();
            };
            flyout.ShowAt(manual);
        };
    }

    private GameViewModel? ViewModel => DataContext as GameViewModel;

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (ViewModel is { } vm && _factory is null)
        {
            BuildLayout(vm);
        }

        if (IsShown())
        {
            Subscribe();
        }
    }

    // La coquille recrée l'écran à chaque retour dessus : les abonnements suivent l'affichage, sinon chaque ancienne vue
    // ouvrait sa propre fenêtre d'édition (essai 1.007.080 : deux fenêtres, puis trois…).
    private EventHandler<Guid>? _editHandler;
    private EventHandler<string>? _panelHandler;
    private EventHandler? _sequenceHandler;
    private EventHandler? _showHandler;

    private void Subscribe()
    {
        Unsubscribe();
        if (ViewModel is not { } vm)
        {
            return;
        }

        // ✎ : la fenêtre d'édition (ERG-033), non bloquante, sur un second écran si on veut ; une seule par modèle d'édition.
        _editHandler = (_, _) => EditorWindow.For(vm.Editor).Present(TopLevel.GetTopLevel(this) as Window);
        _panelHandler = (_, id) =>
        {
            if (_layout is not null && _factory is not null)
            {
                _factory.ShowPanel(_layout, id);
                AssignContexts(_layout);
                SaveLayout();
            }
        };
        vm.EditRequested += _editHandler;
        vm.PanelRequested += _panelHandler;

        // P8 : fenêtres d'édition d'une séquence et d'un show, non bloquantes, une de chaque.
        _sequenceHandler = (_, _) => SequencingWindow.Present(vm.SequenceEditor, () => new SequenceEditorWindow(), TopLevel.GetTopLevel(this) as Window);
        _showHandler = (_, _) => SequencingWindow.Present(vm.ShowEditor, () => new ShowEditorWindow(), TopLevel.GetTopLevel(this) as Window);
        vm.SequenceEditRequested += _sequenceHandler;
        vm.ShowEditRequested += _showHandler;
    }

    private void Unsubscribe()
    {
        if (ViewModel is { } vm)
        {
            if (_editHandler is not null)
            {
                vm.EditRequested -= _editHandler;
            }

            if (_panelHandler is not null)
            {
                vm.PanelRequested -= _panelHandler;
            }

            if (_sequenceHandler is not null)
            {
                vm.SequenceEditRequested -= _sequenceHandler;
            }

            if (_showHandler is not null)
            {
                vm.ShowEditRequested -= _showHandler;
            }
        }

        _editHandler = null;
        _panelHandler = null;
        _sequenceHandler = null;
        _showHandler = null;
    }

    private bool IsShown() => TopLevel.GetTopLevel(this) is not null;

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _autosave.Start();
        Subscribe();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Unsubscribe();
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

            // Q42 : T = tap tempo.
            if (e.Key == Key.T && e.KeyModifiers == KeyModifiers.None)
            {
                vm.Tempo.Tap();
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
