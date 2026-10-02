#pragma warning disable CA1859 // Maquettes jetables : des méthodes renvoyant Control valent mieux que des types précis à chaque assemblage.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace Luxia.Tools.Prototype.Mockups;

/// <summary>
/// Maquettes de P8 « Show &amp; séquences » (Q44, solution C ; Q45, cartes et diagramme automatique) : écran de jeu avec la
/// colonne « Shows » et le bandeau « Show en cours » (replié, puis déplié), fenêtre d'édition d'une séquence (pistes en
/// mesures) et d'un show (cartes, diagramme, simulation). Données fictives (exemple du doc 20 §3.4), aucune action réelle ;
/// à valider par l'utilisateur avant le développement de l'interface.
/// </summary>
internal static class ShowMockups
{
    /// <summary>Maquettes de P8, rendues en 1920 × 1080.</summary>
    public static IReadOnlyList<Controle2Mockups.Mockup> All { get; } =
    [
        new("maquette-9-jeu-show", 1920, 1080, () => Game(expanded: false)),
        new("maquette-10-jeu-show-deplie", 1920, 1080, () => Game(expanded: true)),
        new("maquette-11-edition-sequence", 1920, 1080, Sequence),
        new("maquette-12-edition-show", 1920, 1080, ShowEditor),
    ];

    private static readonly Color ShowColor = Color.Parse("#39C5CF");

    // ------------------------------------------------------------------ éléments communs

    private static TextBlock T(string text, double size = 13, Color? color = null, FontWeight weight = FontWeight.Normal, TextWrapping wrap = TextWrapping.NoWrap) =>
        new() { Text = text, FontSize = size, Foreground = Tokens.Brush(color ?? Tokens.Text), FontWeight = weight, TextWrapping = wrap, VerticalAlignment = VerticalAlignment.Center };

    private static Border Btn(string text, Color? bg = null, Color? fg = null, double minHeight = 32, Color? border = null, double size = 13, FontWeight weight = FontWeight.Normal, string? tip = null)
    {
        var button = new Border
        {
            Background = Tokens.Brush(bg ?? Tokens.Raised),
            BorderBrush = Tokens.Brush(border ?? Tokens.Border),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            MinHeight = minHeight,
            Padding = new Thickness(12, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = text, FontSize = size, FontWeight = weight, Foreground = Tokens.Brush(fg ?? Tokens.Text), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
        };
        if (tip is not null)
        {
            ToolTip.SetTip(button, tip);
        }

        return button;
    }

    private static Control Panel(string title, Control content, string? hint = null)
    {
        var head = new DockPanel { Background = Tokens.Brush(Tokens.Surface), Height = 30 };
        var close = T("▾  ✕", 12, Tokens.Secondary);
        close.Margin = new Thickness(0, 0, 10, 0);
        head.Children.Add(Dock(close, Avalonia.Controls.Dock.Right));
        if (hint is not null)
        {
            var h = T(hint, 11, Tokens.Secondary);
            h.Margin = new Thickness(0, 0, 16, 0);
            head.Children.Add(Dock(h, Avalonia.Controls.Dock.Right));
        }

        var t = T(title, 13);
        t.Margin = new Thickness(10, 0);
        head.Children.Add(t);
        var root = new DockPanel();
        root.Children.Add(Dock(head, Avalonia.Controls.Dock.Top));
        root.Children.Add(content);
        return new Border { BorderBrush = Tokens.Brush(Tokens.Border), BorderThickness = new Thickness(1), Margin = new Thickness(2), Child = root, ClipToBounds = true };
    }

    private static Control Section(string title, Control content, Color? accent = null) =>
        new Border
        {
            Background = Tokens.Brush(Tokens.Surface),
            CornerRadius = new CornerRadius(8),
            BorderBrush = Tokens.Brush(accent ?? Tokens.Border),
            BorderThickness = new Thickness(accent is null ? 0 : 1),
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 0, 0, 8),
            Child = new StackPanel { Spacing = 6, Children = { T(title, 14, Tokens.Text, FontWeight.SemiBold), content } },
        };

    private static Control Dock(Control control, Avalonia.Controls.Dock dock)
    {
        DockPanel.SetDock(control, dock);
        return control;
    }

    private static Control Dot(Color color, double size = 10) => new Ellipse { Width = size, Height = size, Fill = Tokens.Brush(color), VerticalAlignment = VerticalAlignment.Center };

    private static Control Row(double spacing, params Control[] children)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = spacing, VerticalAlignment = VerticalAlignment.Center };
        foreach (var child in children)
        {
            row.Children.Add(child);
        }

        return row;
    }

    private static Control Chip(string text, Color color, bool filled = false) =>
        new Border
        {
            Background = Tokens.Brush(color, filled ? 0.85 : 0.15),
            BorderBrush = Tokens.Brush(color),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 1),
            VerticalAlignment = VerticalAlignment.Center,
            Child = T(text, 11, filled ? Tokens.Background : color),
        };

    private static Window Shell(string title, Control content) =>
        new() { Title = title, Width = 1920, Height = 1080, Background = Tokens.Brush(Tokens.Background), Content = content };

    // ------------------------------------------------------------------ écran de jeu (maquettes 9 et 10)

    private static Window Game(bool expanded)
    {
        var root = new DockPanel();
        root.Children.Add(Dock(TopBar(), Avalonia.Controls.Dock.Top));
        root.Children.Add(Dock(Navigation(), Avalonia.Controls.Dock.Left));
        root.Children.Add(Dock(Toolbar(), Avalonia.Controls.Dock.Top));
        root.Children.Add(Dock(TempoBar(), Avalonia.Controls.Dock.Top));
        root.Children.Add(Dock(ShowBand(expanded), Avalonia.Controls.Dock.Top));
        if (expanded)
        {
            root.Children.Add(Dock(ShowDetail(), Avalonia.Controls.Dock.Top));
        }

        var status = new Border
        {
            Background = Tokens.Brush(Tokens.Surface),
            Padding = new Thickness(10, 4),
            Child = T("● Arduino COM5   ·   Blackout : non  ·  GM 100 %  ·  5 scène(s) en cours  ·  Show : Soirée électro (Refrain)  ·  CPU 2 %", 12, Tokens.Secondary),
        };
        root.Children.Add(Dock(status, Avalonia.Controls.Dock.Bottom));

        var bottom = new Grid { Height = 120, ColumnDefinitions = new ColumnDefinitions("*,*") };
        bottom.Children.Add(Panel("Pilote automatique", new StackPanel { Margin = new Thickness(12, 8), Children = { T("♪ Titre en cours : —      Style : —      Show choisi : — (P10)", 13, Tokens.Secondary) } }));
        var log = Panel("Journal", new StackPanel
        {
            Margin = new Thickness(12, 6),
            Spacing = 2,
            Children =
            {
                Mono("22:41:12  Show « Soirée électro » : étape Refrain (drop)"),
                Mono("22:40:31  Show « Soirée électro » : étape Montée (montée détectée)"),
                Mono("22:38:02  ▶ Show « Soirée électro » (utilisateur)"),
            },
        });
        Grid.SetColumn(log, 1);
        bottom.Children.Add(log);
        root.Children.Add(Dock(bottom, Avalonia.Controls.Dock.Bottom));

        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("*,420") };
        body.Children.Add(Panel("Colonnes", Columns(), "la colonne « Shows » se range comme les autres"));
        var right = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        right.Children.Add(Panel("Groupes dimmer", new StackPanel { Margin = new Thickness(12), Children = { T("(inchangé)", 12, Tokens.Secondary) } }));
        var looks = Panel("Looks", new WrapPanel { Margin = new Thickness(10), Children = { LookTile("Temps mort", "F1", Tokens.Live), LookTile("Retour de piste", "F2", Color.Parse("#DB61A2")) } });
        Grid.SetRow(looks, 1);
        right.Children.Add(looks);
        Grid.SetColumn(right, 1);
        body.Children.Add(right);
        root.Children.Add(body);

        return Shell(expanded ? "LuXia – maquette 10 : bandeau du show déplié" : "LuXia – maquette 9 : écran de jeu avec un show en cours", root);
    }

    private static Control Mono(string text) => new TextBlock { Text = text, FontSize = 12, FontFamily = new FontFamily("Consolas, Cascadia Mono, monospace"), Foreground = Tokens.Brush(Tokens.Text) };

    private static Control LookTile(string name, string key, Color color) =>
        new Border
        {
            Width = 180,
            Margin = new Thickness(0, 0, 8, 8),
            Background = Tokens.Brush(Tokens.Raised),
            BorderBrush = Tokens.Brush(color),
            BorderThickness = new Thickness(3, 0, 0, 0),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 6),
            Child = new StackPanel { Children = { T(name, 14, weight: FontWeight.SemiBold), T(key + "  2 action(s)", 11, Tokens.Secondary) } },
        };

    private static Control TopBar()
    {
        var bar = new DockPanel { Height = 54, Background = Tokens.Brush(Tokens.Background), LastChildFill = false };
        bar.Children.Add(Dock(Row(24, T("   Projet", 14), T("Aide", 14)), Avalonia.Controls.Dock.Left));
        bar.Children.Add(Dock(Row(14, T("Grand Master", 14), new Border { Width = 260, Height = 4, Background = Tokens.Brush(Tokens.Accent), CornerRadius = new CornerRadius(2) }, T("100 %", 14), Btn("BLACKOUT (B)", Color.FromArgb(38, 248, 81, 73), Tokens.Danger, 36, Tokens.Danger, weight: FontWeight.Bold), new Border { Width = 8 }), Avalonia.Controls.Dock.Right));
        return bar;
    }

    private static Control Navigation()
    {
        var stack = new StackPanel { Width = 170, Background = Tokens.Brush(Tokens.Background) };
        stack.Children.Add(new Border { Height = 64, Child = T("  LuXia", 30, Tokens.Accent, FontWeight.Bold) });
        foreach (var (name, selected) in new[] { ("◉  Contrôle", true), ("▤  Console", false), ("▤  Bibliothèque", false), ("▦  Installation", false), ("◎  Simulateur", false), ("♪  Audio", false), ("⇄  Sorties", false) })
        {
            stack.Children.Add(new Border { Height = 40, Padding = new Thickness(14, 0), Background = selected ? Tokens.Brush(Color.Parse("#1F6FEB"), 0.6) : null, Child = T(name, 14) });
        }

        stack.Children.Add(new Border { Margin = new Thickness(12, 16, 8, 0), Child = T("Live et Scènes : retirés à la fin de P8 (Q47)", 11, Tokens.Secondary, wrap: TextWrapping.Wrap) });
        return stack;
    }

    private static Control Toolbar()
    {
        var bar = new DockPanel { Height = 48, Background = Tokens.Brush(Tokens.Surface), LastChildFill = false };
        bar.Children.Add(Dock(Row(8, new Border { Width = 4 }, Btn("■ Stop", fg: Tokens.Danger), Btn("■ Tout stopper", fg: Tokens.Danger, tip: "Arrête aussi le show en cours"), Btn("🔒 Verrou soirée")), Avalonia.Controls.Dock.Left));
        bar.Children.Add(Dock(Row(8, Btn("↶"), Btn("↷"), Btn("Panneaux ▾"), Btn("Rétablir la disposition"), new Border { Width = 4 }), Avalonia.Controls.Dock.Right));
        return bar;
    }

    private static Control TempoBar() =>
        new Border
        {
            Height = 46,
            Background = Tokens.Brush(Tokens.Surface),
            BorderBrush = Tokens.Brush(Tokens.Border),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Child = Row(8, T("  ♪", 14), Btn("🎧 Audio", Color.Parse("#1F6FEB"), border: Color.Parse("#58A6FF")), T("●", 12, Tokens.Edit), new Border { Width = 40 }, Btn("128", Tokens.Background, size: 16, weight: FontWeight.SemiBold), T("BPM", 12, Tokens.Secondary), Btn("−", fg: Tokens.Secondary), Btn("+", fg: Tokens.Secondary), Btn("TAP", fg: Tokens.Secondary), Btn("×2"), Btn("÷ 2"), Btn("1 ici"), T("  ● ● ● ●", 13, Tokens.Live)),
        };

    /// <summary>Bandeau « Show en cours » : une ligne, visible seulement quand un show ou une séquence joue.</summary>
    private static Control ShowBand(bool expanded)
    {
        var right = Row(
            8,
            Btn("⏭ Forcer ▾", tip: "Choisir une transition à franchir tout de suite (ou à la prochaine mesure)"),
            Btn("■ Arrêter le show", fg: Tokens.Danger, border: Tokens.Danger),
            Btn(expanded ? "▴ Replier" : "▾ Détail", tip: "Étapes actives, transitions en attente, séquences en cours"),
            new Border { Width = 4 });
        var left = Row(
            10,
            new Border { Width = 4 },
            T("▶", 16, ShowColor),
            T("Soirée électro", 15, Tokens.Text, FontWeight.SemiBold),
            T("·", 15, Tokens.Secondary),
            Chip("Refrain", ShowColor, filled: true),
            T("depuis 12 mesures", 12, Tokens.Secondary),
            T("·", 15, Tokens.Secondary),
            T("ensuite :", 13, Tokens.Secondary),
            Chip("au break → Couplet", Tokens.Secondary),
            Chip("dans 4 mesures → Couplet", Tokens.Live),
            T("·", 15, Tokens.Secondary),
            T("séquence « Groove 8 mesures » 5 / 8", 13, Tokens.Secondary));
        var dock = new DockPanel { LastChildFill = true };
        dock.Children.Add(Dock(right, Avalonia.Controls.Dock.Right));
        dock.Children.Add(left);
        return new Border
        {
            Height = 46,
            Background = Tokens.Brush(ShowColor, 0.12),
            BorderBrush = Tokens.Brush(ShowColor),
            BorderThickness = new Thickness(0, 1, 0, 1),
            Child = dock,
        };
    }

    /// <summary>Détail déplié du bandeau : chemin parcouru, étape(s) active(s), transitions en attente avec leur compte à rebours.</summary>
    private static Control ShowDetail()
    {
        var path = Row(6, T("Parcours :", 12, Tokens.Secondary), Chip("Intro", Tokens.Secondary), T("→", 12, Tokens.Secondary), Chip("Couplet", Tokens.Secondary), T("→", 12, Tokens.Secondary), Chip("Montée", Tokens.Secondary), T("→", 12, Tokens.Secondary), Chip("Refrain", ShowColor, filled: true));

        var active = new Border
        {
            Width = 380,
            Background = Tokens.Brush(Tokens.Raised),
            BorderBrush = Tokens.Brush(ShowColor),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 8),
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    Row(8, T("Étape active", 11, Tokens.Secondary), Chip("3 · Refrain", ShowColor, filled: true)),
                    T("• Couleurs : « Arc-en-ciel rapide » (tant que l'étape dure)", 12),
                    T("• Effets : « Strobe croissant » (tant que l'étape dure)", 12),
                    T("• à l'entrée : flash blanc", 12),
                    T("Refrains joués : 2", 11, Tokens.Secondary),
                },
            },
        };

        var transitions = new StackPanel { Spacing = 6 };
        transitions.Children.Add(T("Transitions qui partent de l'étape", 12, Tokens.Secondary));
        transitions.Children.Add(TransitionLine("au break", "→ 1 · Couplet", "attend la condition", Tokens.Secondary, "à la prochaine mesure"));
        transitions.Children.Add(TransitionLine("après 16 mesures dans l'étape", "→ 1 · Couplet", "condition vraie dans 4 mesures", Tokens.Live, "à la prochaine mesure"));
        transitions.Children.Add(TransitionLine("refrains joués ≥ 3", "→ 5 · Final", "attend la condition (2 / 3)", Tokens.Secondary, "à la prochaine phrase (8 mesures)"));

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), Margin = new Thickness(12, 8) };
        grid.Children.Add(active);
        var tr = new Border { Margin = new Thickness(16, 0, 0, 0), Child = transitions };
        Grid.SetColumn(tr, 1);
        grid.Children.Add(tr);

        return new Border
        {
            Background = Tokens.Brush(Tokens.Surface),
            BorderBrush = Tokens.Brush(ShowColor),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(4, 6),
            Child = new StackPanel { Spacing = 4, Children = { new Border { Margin = new Thickness(12, 0), Child = path }, grid } },
        };
    }

    private static Control TransitionLine(string condition, string target, string state, Color stateColor, string quantize)
    {
        var dock = new DockPanel { LastChildFill = true };
        dock.Children.Add(Dock(Btn("⏭ Forcer", minHeight: 28, size: 12, tip: "Franchir cette transition (à sa quantification)"), Avalonia.Controls.Dock.Right));
        dock.Children.Add(Row(10, Dot(stateColor), T(condition, 13, weight: FontWeight.SemiBold), T(target, 13, ShowColor), T("· " + quantize, 12, Tokens.Secondary), T("· " + state, 12, stateColor)));
        return new Border { Background = Tokens.Brush(Tokens.Raised), CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 4), Child = dock };
    }

    private static Control Columns()
    {
        var grid = new Grid { Margin = new Thickness(6) };
        var columns = new List<Control> { ShowsColumn() };
        columns.AddRange(MockShow.Layers.Take(6).Select(LayerColumn));
        for (var i = 0; i < columns.Count; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(i == 0 ? 1.25 : 1, GridUnitType.Star));
            Grid.SetColumn(columns[i], i);
            grid.Children.Add(columns[i]);
        }

        return grid;
    }

    private static Control ColumnHead(string title, Color color, Control? under = null)
    {
        var stack = new StackPanel { Spacing = 6, Children = { new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeight.SemiBold, Foreground = Tokens.Brush(Tokens.Text), HorizontalAlignment = HorizontalAlignment.Center } } };
        if (under is not null)
        {
            stack.Children.Add(under);
        }

        return new Border { BorderBrush = Tokens.Brush(color), BorderThickness = new Thickness(0, 3, 0, 0), Padding = new Thickness(2, 6, 2, 6), Child = stack };
    }

    /// <summary>Colonne « Shows » : shows puis séquences, mêmes boutons que les scènes (clic = lancer / arrêter, ✎ = éditer).</summary>
    private static Control ShowsColumn()
    {
        var items = new StackPanel { Spacing = 5 };
        items.Children.Add(T("SHOWS", 10, Tokens.Secondary, FontWeight.SemiBold));
        items.Children.Add(ItemButton("Soirée électro", ShowColor, "étape Refrain", true));
        items.Children.Add(ItemButton("Soirée calme", Color.Parse("#8957E5")));
        items.Children.Add(ItemButton("Couplet / Refrain / Drop", Color.Parse("#DB61A2")));
        items.Children.Add(ItemButton("Tirage au sort", Color.Parse("#E3B341")));
        items.Children.Add(Btn("+ show", Tokens.Surface, Tokens.Secondary, 34, size: 12));
        items.Children.Add(new Border { Height = 6 });
        items.Children.Add(T("SÉQUENCES", 10, Tokens.Secondary, FontWeight.SemiBold));
        items.Children.Add(ItemButton("Groove 8 mesures", Color.Parse("#3FB950"), "mesure 5 / 8 (lancée par le show)", true, progress: 5 / 8.0));
        items.Children.Add(ItemButton("Montée 16 mesures", Color.Parse("#F0883E")));
        items.Children.Add(Btn("+ séquence", Tokens.Surface, Tokens.Secondary, 34, size: 12));

        var body = new DockPanel();
        body.Children.Add(Dock(ColumnHead("▶  Shows", ShowColor, T("un show à la fois (le nouveau remplace l'ancien)", 10, Tokens.Secondary, wrap: TextWrapping.Wrap)), Avalonia.Controls.Dock.Top));
        body.Children.Add(items);
        return new Border { Background = Tokens.Brush(Tokens.Background), CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 6, 0), Padding = new Thickness(6), Child = body };
    }

    private static Control LayerColumn(MockLayer layer)
    {
        var items = new StackPanel { Spacing = 5 };
        foreach (var scene in layer.Scenes.Take(5))
        {
            items.Children.Add(ItemButton(scene.Name, scene.Color, scene.StepInfo, scene.Progress is not null));
        }

        var transport = Row(4, Btn("◀", minHeight: 30), Btn("▶", minHeight: 30), Btn("■", fg: Tokens.Danger, minHeight: 30));
        ((StackPanel)transport).HorizontalAlignment = HorizontalAlignment.Center;
        var body = new DockPanel();
        body.Children.Add(Dock(ColumnHead($"{layer.Icon}  {layer.Name}", layer.Color, transport), Avalonia.Controls.Dock.Top));
        body.Children.Add(items);
        return new Border { Background = Tokens.Brush(Tokens.Background), CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 6, 0), Padding = new Thickness(6), Child = body };
    }

    private static Control ItemButton(string name, Color color, string? info = null, bool playing = false, double? progress = null)
    {
        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { new TextBlock { Text = name, FontSize = 13, FontWeight = playing ? FontWeight.SemiBold : FontWeight.Normal, Foreground = Tokens.Brush(playing ? Tokens.Background : Tokens.Text), TextWrapping = TextWrapping.Wrap, MaxLines = 2 } } };
        if (playing && info is not null)
        {
            text.Children.Add(T(info, 11, Tokens.Background));
        }

        if (progress is { } p)
        {
            text.Children.Add(new Border { Height = 4, Margin = new Thickness(0, 3, 0, 0), Background = Tokens.Brush(Tokens.Background, 0.3), Child = new Border { Width = 150 * p, HorizontalAlignment = HorizontalAlignment.Left, Background = Tokens.Brush(Tokens.Background) } });
        }

        var play = new Border
        {
            Background = playing ? Tokens.Brush(color, 0.85) : Tokens.Brush(Tokens.Raised),
            BorderBrush = Tokens.Brush(color),
            BorderThickness = new Thickness(4, 0, 0, 0),
            CornerRadius = new CornerRadius(6, 0, 0, 6),
            Padding = new Thickness(8, 5),
            MinHeight = 50,
            Child = text,
        };
        var edit = new Border
        {
            Width = 30,
            Background = Tokens.Brush(Tokens.Surface),
            CornerRadius = new CornerRadius(0, 6, 6, 0),
            Child = new TextBlock { Text = "✎", FontSize = 15, Foreground = Tokens.Brush(Tokens.Secondary), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
        };
        var row = new DockPanel();
        row.Children.Add(Dock(edit, Avalonia.Controls.Dock.Right));
        row.Children.Add(play);
        return row;
    }

    // ------------------------------------------------------------------ fenêtre d'édition (maquettes 11 et 12)

    private static DockPanel EditorFrame(string kind, string name, Color color, Control body, Control footerExtra)
    {
        var root = new DockPanel();
        var head = new Border
        {
            Background = Tokens.Brush(Tokens.Edit, 0.14),
            BorderBrush = Tokens.Brush(Tokens.Edit),
            BorderThickness = new Thickness(0, 0, 0, 2),
            Padding = new Thickness(12, 8),
            Child = Row(12, Chip("BROUILLON", Tokens.Edit, filled: true), new Border { Width = 22, Height = 22, CornerRadius = new CornerRadius(4), Background = Tokens.Brush(color) }, T(name, 18, weight: FontWeight.SemiBold), T("— " + kind + " · Appliquer écrit sans fermer ; Annuler revient à l'état d'origine.", 13, Tokens.Secondary), Chip("modifié", Tokens.Live, filled: true)),
        };
        root.Children.Add(Dock(head, Avalonia.Controls.Dock.Top));

        var footer = new DockPanel { Height = 60, Background = Tokens.Brush(Tokens.Surface), LastChildFill = true };
        footer.Children.Add(Dock(Row(10, Btn("Appliquer", minHeight: 40), Btn("Annuler", minHeight: 40), Btn("Valider", Tokens.Edit, Tokens.Background, 40, Tokens.Edit, weight: FontWeight.Bold), new Border { Width = 8 }), Avalonia.Controls.Dock.Right));
        footer.Children.Add(Row(10, new Border { Width = 4 }, Btn("☐ 👁 Aveugle", tip: "Essayer sur l'aperçu (plan, simulateur) sans toucher la sortie"), footerExtra));
        root.Children.Add(Dock(footer, Avalonia.Controls.Dock.Bottom));
        root.Children.Add(body);
        return root;
    }

    private static Control Labelled(string label, Control value) =>
        new Grid { ColumnDefinitions = new ColumnDefinitions("130,*"), Margin = new Thickness(0, 2), Children = { T(label, 12, Tokens.Secondary), new Border { [Grid.ColumnProperty] = 1, Child = value } } };

    private static Control Field(string text, double width = 150) =>
        new Border { Width = width, Height = 30, HorizontalAlignment = HorizontalAlignment.Left, Background = Tokens.Brush(Tokens.Background), BorderBrush = Tokens.Brush(Tokens.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 0), Child = T(text, 13) };

    private static Control Combo(string text, double width = 200) => Field(text + "   ▾", width);

    // --- 11. Séquence

    private sealed record Block(int Track, double Start, double Length, string Name, Color Color, bool Selected = false, string? Kind = null);

    private static readonly (string Name, string Icon, Color Color)[] Tracks =
    [
        ("Intensité", "☀", Color.Parse("#E3B341")),
        ("Couleurs", "◐", Color.Parse("#DB61A2")),
        ("Mouvements", "↻", Color.Parse("#58A6FF")),
        ("Effets", "✦", Color.Parse("#F0883E")),
        ("Actions", "⚙", Color.Parse("#8B949E")),
    ];

    private static readonly Block[] Blocks =
    [
        new(0, 0, 8, "Vague d'intensité", Color.Parse("#E3B341")),
        new(0, 8, 8, "Plein feu", Color.Parse("#E3B341")),
        new(1, 0, 8, "Bleu lent", Color.Parse("#1F6FEB")),
        new(1, 8, 4, "Arc-en-ciel", Color.Parse("#DB61A2"), Selected: true),
        new(1, 12, 4, "Blanc", Color.Parse("#E6EDF3")),
        new(2, 0, 8, "Balayage lent", Color.Parse("#58A6FF")),
        new(2, 8, 8, "Cercle rapide", Color.Parse("#79C0FF")),
        new(3, 12, 4, "Strobe croissant", Color.Parse("#F0883E")),
        new(4, 12, 4, "Niveau Couleurs 100 → 40 %", Color.Parse("#8B949E"), Kind: "rampe"),
        new(4, 15, 1, "Fumée 3 s", Color.Parse("#6E7681"), Kind: "rafale"),
    ];

    private static Window Sequence()
    {
        const double bars = 16;
        const double trackHead = 150;
        const double barWidth = 66;
        const double trackHeight = 64;

        var canvas = new Canvas { Width = trackHead + (bars * barWidth) + 20, Height = 34 + (Tracks.Length * trackHeight) };

        // Règle : mesures et temps.
        for (var b = 0; b <= bars; b++)
        {
            var x = trackHead + (b * barWidth);
            canvas.Children.Add(new Line { StartPoint = new Point(x, 22), EndPoint = new Point(x, canvas.Height), Stroke = Tokens.Brush(Tokens.Border), StrokeThickness = b % 4 == 0 ? 2 : 1 });
            if (b < bars)
            {
                var label = T((b + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), 12, b % 4 == 0 ? Tokens.Text : Tokens.Secondary, b % 4 == 0 ? FontWeight.SemiBold : FontWeight.Normal);
                Canvas.SetLeft(label, x + 4);
                Canvas.SetTop(label, 2);
                canvas.Children.Add(label);
                for (var beat = 1; beat < 4; beat++)
                {
                    var bx = x + (beat * barWidth / 4);
                    canvas.Children.Add(new Line { StartPoint = new Point(bx, 26), EndPoint = new Point(bx, canvas.Height), Stroke = Tokens.Brush(Tokens.Border, 0.35), StrokeThickness = 1 });
                }
            }
        }

        var mesure = T("Mesure", 12, Tokens.Secondary);
        Canvas.SetLeft(mesure, 8);
        Canvas.SetTop(mesure, 2);
        canvas.Children.Add(mesure);

        for (var t = 0; t < Tracks.Length; t++)
        {
            var (name, icon, color) = Tracks[t];
            var y = 34 + (t * trackHeight);
            var head = new Border
            {
                Width = trackHead - 8,
                Height = trackHeight - 8,
                Background = Tokens.Brush(Tokens.Surface),
                BorderBrush = Tokens.Brush(color),
                BorderThickness = new Thickness(3, 0, 0, 0),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 0),
                Child = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { T($"{icon}  {name}", 13, weight: FontWeight.SemiBold), T(t == 4 ? "niveaux, fumée, flash" : "couche", 11, Tokens.Secondary) } },
            };
            Canvas.SetLeft(head, 0);
            Canvas.SetTop(head, y + 4);
            canvas.Children.Add(head);
            canvas.Children.Add(new Line { StartPoint = new Point(0, y + trackHeight), EndPoint = new Point(canvas.Width, y + trackHeight), Stroke = Tokens.Brush(Tokens.Border, 0.5) });
        }

        foreach (var block in Blocks)
        {
            var y = 34 + (block.Track * trackHeight) + 8;
            var light = ((0.299 * block.Color.R) + (0.587 * block.Color.G) + (0.114 * block.Color.B)) / 255 > 0.6;
            var content = new StackPanel { Children = { T(block.Name, 12, light ? Tokens.Background : Tokens.Text, FontWeight.SemiBold) } };
            if (block.Kind == "rampe")
            {
                content.Children.Add(new Line { StartPoint = new Point(0, 2), EndPoint = new Point((block.Length * barWidth) - 20, 22), Stroke = Tokens.Brush(Tokens.Text), StrokeThickness = 2 });
            }
            else
            {
                content.Children.Add(T(string.Create(System.Globalization.CultureInfo.CurrentCulture, $"{block.Length:0.##} mesure{(block.Length > 1 ? "s" : string.Empty)}"), 11, light ? Tokens.Background : Tokens.Text));
            }

            var rect = new Border
            {
                Width = (block.Length * barWidth) - 4,
                Height = trackHeight - 16,
                Background = Tokens.Brush(block.Color, block.Kind is null ? 0.85 : 0.4),
                BorderBrush = Tokens.Brush(block.Selected ? Tokens.Text : block.Color),
                BorderThickness = new Thickness(block.Selected ? 2 : 1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(8, 4),
                Child = content,
                ClipToBounds = true,
            };
            Canvas.SetLeft(rect, trackHead + (block.Start * barWidth) + 2);
            Canvas.SetTop(rect, y);
            canvas.Children.Add(rect);
        }

        // Tête de lecture (essai au métronome) à la mesure 10, temps 3.
        var px = trackHead + (9.5 * barWidth);
        canvas.Children.Add(new Line { StartPoint = new Point(px, 20), EndPoint = new Point(px, canvas.Height), Stroke = Tokens.Brush(Tokens.Live), StrokeThickness = 2 });

        var timeline = new StackPanel
        {
            Margin = new Thickness(12),
            Spacing = 10,
            Children =
            {
                Row(
                    8,
                    Btn("▶ Essayer", tip: "Joue la séquence au tempo du métronome ou de la musique"),
                    Btn("■"),
                    T("  mesure 10 · temps 3", 13, Tokens.Live),
                    new Border { Width = 20 },
                    T("Grille :", 12, Tokens.Secondary),
                    Chip("mesure", Tokens.Accent, filled: true),
                    Chip("temps", Tokens.Accent),
                    Chip("½ temps", Tokens.Accent),
                    new Border { Width = 20 },
                    T("Zoom", 12, Tokens.Secondary),
                    Btn("−", minHeight: 26),
                    Btn("+", minHeight: 26)),
                new Border { Background = Tokens.Brush(Tokens.Surface), CornerRadius = new CornerRadius(8), Padding = new Thickness(8), Child = canvas },
                T("Glisser une scène de la liste de gauche sur la piste de sa couche ; tirer le bord d'un bloc pour sa durée ; Suppr pour l'enlever. Les blocs s'aimantent à la grille.", 12, Tokens.Secondary, wrap: TextWrapping.Wrap),
            },
        };

        // Gauche : scènes à glisser, rangées par couche.
        var library = new StackPanel { Spacing = 4, Margin = new Thickness(10) };
        library.Children.Add(Field("🔍 Chercher une scène", 250));
        foreach (var layer in MockShow.Layers.Take(5))
        {
            library.Children.Add(T($"{layer.Icon}  {layer.Name}", 12, layer.Color, FontWeight.SemiBold));
            foreach (var scene in layer.Scenes.Take(3))
            {
                library.Children.Add(new Border { Background = Tokens.Brush(Tokens.Raised), BorderBrush = Tokens.Brush(scene.Color), BorderThickness = new Thickness(3, 0, 0, 0), CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 4), Child = T("⠿  " + scene.Name, 12) });
            }
        }

        library.Children.Add(T("⚙  Actions", 12, Tokens.Secondary, FontWeight.SemiBold));
        foreach (var action in new[] { "Niveau de couche (rampe)", "Fumée (rafale)", "Flash d'une scène", "Noir court" })
        {
            library.Children.Add(new Border { Background = Tokens.Brush(Tokens.Raised), CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 4), Child = T("⠿  " + action, 12) });
        }

        // Droite : propriétés de la séquence et du bloc choisi.
        var properties = new StackPanel
        {
            Margin = new Thickness(10),
            Children =
            {
                Section(
                    "Séquence",
                    new StackPanel
                    {
                        Children =
                        {
                            Labelled("Nom", Field("Montée 16 mesures", 220)),
                            Labelled("Longueur", Row(4, Field("16", 70), Combo("mesures", 120))),
                            Labelled("À la fin", Combo("Reprendre au début (boucle)", 230)),
                            Labelled("Démarrage", Combo("À la prochaine mesure", 230)),
                            Labelled("Vitesse", Row(4, Chip("½ temps", Tokens.Accent), Chip("normale", Tokens.Accent, filled: true), Chip("double", Tokens.Accent))),
                        },
                    }),
                Section(
                    "Bloc choisi : « Arc-en-ciel »",
                    new StackPanel
                    {
                        Children =
                        {
                            Labelled("Scène", Combo("Arc-en-ciel", 220)),
                            Labelled("Piste", T("◐ Couleurs (couche de la scène)", 12)),
                            Labelled("Début", Row(4, T("mesure", 12, Tokens.Secondary), Field("9", 56), T("temps", 12, Tokens.Secondary), Field("1", 56))),
                            Labelled("Durée", Row(4, Field("4", 70), Combo("mesures", 120))),
                            Labelled("À la fin du bloc", Combo("Arrêter la scène", 220)),
                            T("Si un autre bloc suit sur la même piste, il prend le relais par fondu croisé (pas de noir).", 11, Tokens.Secondary, wrap: TextWrapping.Wrap),
                        },
                    },
                    Tokens.Text),
                Section("Où la lancer", T("Bouton dans la colonne « Shows » de l'écran de jeu, ou action « jouer la séquence » d'une étape de show.", 12, Tokens.Secondary, wrap: TextWrapping.Wrap)),
            },
        };

        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("280,*,380") };
        body.Children.Add(new Border { BorderBrush = Tokens.Brush(Tokens.Border), BorderThickness = new Thickness(0, 0, 1, 0), Child = new ScrollViewer { Content = library } });
        body.Children.Add(new Border { [Grid.ColumnProperty] = 1, Child = new ScrollViewer { Content = timeline, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto } });
        body.Children.Add(new Border { [Grid.ColumnProperty] = 2, BorderBrush = Tokens.Brush(Tokens.Border), BorderThickness = new Thickness(1, 0, 0, 0), Child = new ScrollViewer { Content = properties } });

        var metronome = Row(8, new Border { Width = 20 }, T("Tempo de l'essai :", 12, Tokens.Secondary), Chip("celui du direct (128 BPM)", Tokens.Accent, filled: true), Chip("métronome", Tokens.Accent), Field("120", 60), T("BPM", 12, Tokens.Secondary));
        return Shell("LuXia – maquette 11 : édition d'une séquence", EditorFrame("séquence", "Montée 16 mesures", Color.Parse("#F0883E"), body, metronome));
    }

    // --- 12. Show

    private sealed record StepCard(string Number, string Name, string[] Actions, (string Condition, string Target, string Extra)[] Transitions, bool Initial = false, bool Active = false);

    private static readonly StepCard[] Cards =
    [
        new("0", "Intro", ["Couleurs : Bleu lent", "Mouvements : Balayage lent"], [("énergie ≥ Groove", "→ 1 Couplet", "prochaine mesure")], Initial: true),
        new("1", "Couplet", ["Séquence « Groove 8 mesures » (en boucle)"], [("au drop", "→ 3 Refrain", "priorité 1"), ("à la montée", "→ 4 Montée", "priorité 2"), ("après 16 mesures", "→ 2a (60 %) ou 2b (40 %)", "tirage au sort")]),
        new("3", "Refrain", ["Couleurs : Arc-en-ciel rapide", "Effets : Strobe croissant", "À l'entrée : flash blanc ; refrains + 1"], [("au break", "→ 1 Couplet", "prochaine mesure"), ("refrains ≥ 3", "→ 5 Final", "prochaine phrase")], Active: true),
        new("4", "Montée", ["Séquence « Montée 16 mesures »"], [("au drop", "→ 3 Refrain", "prochaine mesure")]),
    ];

    private static Window ShowEditor()
    {
        // Gauche : cartes (une par étape), transitions sous chaque carte.
        var cards = new StackPanel { Spacing = 10, Margin = new Thickness(12) };
        cards.Children.Add(Row(8, Btn("+ étape"), Btn("+ transition"), Btn("+ branches en parallèle", tip: "Divergence ET : plusieurs étapes actives en même temps"), Btn("+ macro-étape", tip: "Étape qui joue un autre show (bloc réutilisable)")));
        foreach (var card in Cards)
        {
            cards.Children.Add(Card(card));
        }

        cards.Children.Add(T("2a Variante A, 2b Variante B, 5 Final : repliées", 12, Tokens.Secondary));

        // Droite : diagramme dessiné automatiquement (lecture seule ; clic = ouvrir la carte).
        var diagram = Diagram();

        var simulation = new Border
        {
            Background = Tokens.Brush(Tokens.Surface),
            BorderBrush = Tokens.Brush(Tokens.Live),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 8),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    Row(10, T("Simulation sans musique", 14, weight: FontWeight.SemiBold), T("le show joue au métronome ; les boutons provoquent les événements musicaux", 12, Tokens.Secondary)),
                    Row(
                        8,
                        Btn("▶ Jouer le show", Tokens.Live, Tokens.Background, border: Tokens.Live, weight: FontWeight.SemiBold),
                        Btn("■"),
                        T("Métronome", 12, Tokens.Secondary),
                        Field("128", 60),
                        T("BPM", 12, Tokens.Secondary),
                        new Border { Width = 16 },
                        Btn("Drop"),
                        Btn("Break"),
                        Btn("Montée"),
                        Btn("Silence"),
                        Btn("Morceau suivant"),
                        new Border { Width = 16 },
                        T("Énergie", 12, Tokens.Secondary),
                        Chip("Calme", Tokens.Accent),
                        Chip("Groove", Tokens.Accent, filled: true),
                        Chip("Énergique", Tokens.Accent),
                        Chip("Explosif", Tokens.Accent)),
                    Row(10, T("Étape active : 3 Refrain (depuis 6 mesures)", 13, ShowColor), T("· prochaine transition possible : au break, ou refrains ≥ 3 (2 / 3)", 12, Tokens.Secondary)),
                },
            },
        };

        var right = new DockPanel { Margin = new Thickness(12) };
        right.Children.Add(Dock(simulation, Avalonia.Controls.Dock.Bottom));
        right.Children.Add(Dock(Row(10, T("Diagramme", 14, weight: FontWeight.SemiBold), T("dessiné automatiquement · clic sur une étape = ouvrir sa carte", 12, Tokens.Secondary)), Avalonia.Controls.Dock.Top));
        right.Children.Add(new Border { Background = Tokens.Brush(Tokens.Surface), CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 8, 0, 8), Child = diagram });

        var meta = new StackPanel
        {
            Margin = new Thickness(12, 0, 12, 12),
            Children =
            {
                Section(
                    "Pour le pilote automatique (P10)",
                    new StackPanel
                    {
                        Children =
                        {
                            Labelled("Rôle", Combo("Principal", 180)),
                            Labelled("Styles visés", Row(4, Chip("Électro", ShowColor, filled: true), Chip("House", ShowColor, filled: true), Chip("+", Tokens.Secondary))),
                            Labelled("Énergie", T("Groove → Explosif", 12)),
                        },
                    }),
            },
        };

        var left = new DockPanel();
        left.Children.Add(Dock(meta, Avalonia.Controls.Dock.Bottom));
        left.Children.Add(new ScrollViewer { Content = cards });

        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("640,*") };
        body.Children.Add(new Border { BorderBrush = Tokens.Brush(Tokens.Border), BorderThickness = new Thickness(0, 0, 1, 0), Child = left });
        body.Children.Add(new Border { [Grid.ColumnProperty] = 1, Child = right });

        var extra = Row(8, new Border { Width = 20 }, T("À la fin du show :", 12, Tokens.Secondary), Combo("reprendre à l'étape initiale", 230));
        return Shell("LuXia – maquette 12 : édition d'un show", EditorFrame("show", "Soirée électro", ShowColor, body, extra));
    }

    private static Control Card(StepCard card)
    {
        var actions = new StackPanel { Spacing = 2 };
        foreach (var action in card.Actions)
        {
            actions.Children.Add(T("• " + action, 12));
        }

        actions.Children.Add(T("+ action", 12, Tokens.Accent));
        var transitions = new StackPanel { Spacing = 4, Margin = new Thickness(0, 6, 0, 0) };
        foreach (var (condition, target, extra) in card.Transitions)
        {
            transitions.Children.Add(new Border
            {
                Background = Tokens.Brush(Tokens.Background),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4),
                Child = Row(8, T("┼", 13, Tokens.Secondary), T(condition, 12, weight: FontWeight.SemiBold), T(target, 12, ShowColor), T("· " + extra, 11, Tokens.Secondary)),
            });
        }

        transitions.Children.Add(T("+ transition", 12, Tokens.Accent));
        var title = Row(8, Chip(card.Number, card.Active ? ShowColor : Tokens.Secondary, card.Active), T(card.Name, 15, weight: FontWeight.SemiBold));
        if (card.Initial)
        {
            ((StackPanel)title).Children.Add(Chip("initiale", Tokens.Edit));
        }

        if (card.Active)
        {
            ((StackPanel)title).Children.Add(Chip("active (simulation)", ShowColor));
        }

        return new Border
        {
            Background = Tokens.Brush(Tokens.Surface),
            BorderBrush = Tokens.Brush(card.Active ? ShowColor : Tokens.Border),
            BorderThickness = new Thickness(card.Active ? 2 : 1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 8),
            Child = new StackPanel { Spacing = 6, Children = { title, T("Pendant l'étape", 11, Tokens.Secondary), actions, T("Transitions (la première vraie l'emporte, dans cet ordre)", 11, Tokens.Secondary), transitions } },
        };
    }

    private static Control Diagram()
    {
        var canvas = new Canvas { Width = 1160, Height = 560 };
        var boxes = new Dictionary<string, Rect>
        {
            ["0"] = new(470, 20, 190, 56),
            ["1"] = new(470, 140, 190, 56),
            ["3"] = new(110, 300, 190, 56),
            ["4"] = new(390, 300, 190, 56),
            ["2a"] = new(640, 300, 190, 56),
            ["2b"] = new(880, 300, 190, 56),
            ["5"] = new(110, 460, 190, 56),
        };
        var names = new Dictionary<string, string> { ["0"] = "Intro", ["1"] = "Couplet", ["3"] = "Refrain", ["4"] = "Montée", ["2a"] = "Variante A", ["2b"] = "Variante B", ["5"] = "Final" };

        var line = Tokens.Brush(Tokens.Secondary);

        void Label(string label, Point at)
        {
            var text = T(label, 11, Tokens.Secondary);
            Canvas.SetLeft(text, at.X);
            Canvas.SetTop(text, at.Y);
            canvas.Children.Add(text);
        }

        // Vers le bas : du bas de l'étape amont au haut de l'étape aval, trait de transition sur la descente.
        void Down(string from, string to, string label)
        {
            var a = boxes[from];
            var b = boxes[to];
            var start = new Point(a.Center.X, a.Bottom);
            var end = new Point(b.Center.X, b.Top);
            var midY = (a.Bottom + b.Top) / 2;
            canvas.Children.Add(new Polyline { Points = [start, new Point(start.X, midY), new Point(end.X, midY), end], Stroke = line, StrokeThickness = 1.5 });
            canvas.Children.Add(new Line { StartPoint = new Point(end.X - 8, midY + 22), EndPoint = new Point(end.X + 8, midY + 22), Stroke = Tokens.Brush(Tokens.Text), StrokeThickness = 2 });
            Label(label, new Point(end.X + 12, midY + 12));
        }

        // Même rangée : de côté à côté.
        void Side(string from, string to, string label)
        {
            var a = boxes[from];
            var b = boxes[to];
            var start = new Point(a.Left, a.Center.Y);
            var end = new Point(b.Right, b.Center.Y);
            canvas.Children.Add(new Line { StartPoint = start, EndPoint = end, Stroke = line, StrokeThickness = 1.5 });
            var midX = (start.X + end.X) / 2;
            canvas.Children.Add(new Line { StartPoint = new Point(midX, end.Y - 8), EndPoint = new Point(midX, end.Y + 8), Stroke = Tokens.Brush(Tokens.Text), StrokeThickness = 2 });
            Label(label, new Point(midX - 14, end.Y - 28));
        }

        // Retour en arrière, en pointillés, par un couloir sur le côté.
        void Back(string from, string to, string label, double corridorX, bool left)
        {
            var a = boxes[from];
            var b = boxes[to];
            var start = new Point(a.Center.X, a.Bottom);
            var down = a.Bottom + 22;
            var end = left ? new Point(b.Left, b.Center.Y) : new Point(b.Right, b.Center.Y);
            canvas.Children.Add(new Polyline { Points = [start, new Point(start.X, down), new Point(corridorX, down), new Point(corridorX, end.Y), end], Stroke = line, StrokeThickness = 1.5, StrokeDashArray = [4, 3] });
            if (label.Length > 0)
            {
                Label(label, new Point(corridorX + (left ? 6 : -74), (down + end.Y) / 2));
            }
        }

        Down("0", "1", "énergie ≥ Groove");
        Down("1", "3", "drop");
        Down("1", "4", "montée");
        Down("1", "2a", "16 mesures · 60 %");
        Down("1", "2b", "40 %");
        Side("4", "3", "drop");
        Down("3", "5", "refrains ≥ 3");
        Back("3", "1", "break", 50, left: true);
        Back("2a", "1", "8 mesures", 1120, left: false);
        Back("2b", "1", string.Empty, 1120, left: false);

        foreach (var (id, rect) in boxes)
        {
            var active = id == "3";
            var box = new Border
            {
                Width = rect.Width,
                Height = rect.Height,
                Background = active ? Tokens.Brush(ShowColor, 0.85) : Tokens.Brush(Tokens.Raised),
                BorderBrush = Tokens.Brush(active ? ShowColor : Tokens.Border),
                BorderThickness = new Thickness(id == "0" ? 3 : 1.5),
                CornerRadius = new CornerRadius(6),
                Child = Row(8, new Border { Width = 4 }, T(id, 13, active ? Tokens.Background : Tokens.Secondary, FontWeight.Bold), T(names[id], 14, active ? Tokens.Background : Tokens.Text, FontWeight.SemiBold)),
            };
            Canvas.SetLeft(box, rect.X);
            Canvas.SetTop(box, rect.Y);
            canvas.Children.Add(box);
        }

        var legend = T("Double bord = étape initiale · bleu = étape active · pointillés = retour en arrière", 11, Tokens.Secondary);
        Canvas.SetLeft(legend, 20);
        Canvas.SetTop(legend, 530);
        canvas.Children.Add(legend);
        return new Viewbox { Stretch = Stretch.Uniform, Child = canvas, Margin = new Thickness(8) };
    }
}
