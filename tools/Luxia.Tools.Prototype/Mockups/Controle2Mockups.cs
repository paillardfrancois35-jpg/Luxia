#pragma warning disable CA1859 // Maquettes jetables : des méthodes renvoyant Control valent mieux que des types précis à chaque assemblage.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace Luxia.Tools.Prototype.Mockups;

/// <summary>
/// Maquettes du chantier « Contrôle 2 » (ERG-032 à ERG-039, Q38) : écran de jeu (sans mode), fenêtre d'édition de scène
/// en brouillon, onglet « Gestion des dimmers » de l'écran Installation. Données fictives, aucune action réelle ;
/// à valider par l'utilisateur avant tout développement.
/// </summary>
internal static class Controle2Mockups
{
    /// <summary>Une maquette : nom du fichier image, taille de la fenêtre, fabrique.</summary>
    internal sealed record Mockup(string FileName, double Width, double Height, Func<Window> Create);

    public static IReadOnlyList<Mockup> All { get; } =
    [
        new("maquette-5-jeu", 1920, 1080, () => Game(1920, 1080, compact: false)),
        new("maquette-5b-jeu-1366", 1366, 768, () => Game(1366, 768, compact: true)),
        new("maquette-6-edition", 1920, 1080, () => Edit(1920, 1080, blind: false)),
        new("maquette-7-edition-aveugle", 1920, 1080, () => Edit(1920, 1080, blind: true)),
        new("maquette-8-gestion-des-dimmers", 1920, 1080, () => Dimmers(1920, 1080)),
    ];

    // ------------------------------------------------------------------ données fictives

    /// <summary>Groupe de dimmers : nom, niveau (null = pas de dimmer), profondeur, appareils, fader de la platine 2.</summary>
    private sealed record Group(string Name, int? Level, int Depth, string Members, int? Fader);

    private static readonly Group[] Tree =
    [
        new("Parc lumineux", 80, 0, "", 1),
        new("Face (PAR)", 70, 1, "", 2),
        new("PAR scène", 50, 2, "PAR 1, PAR 2, PAR 3, PAR 4", 3),
        new("Gros PAR", null, 2, "Gros PAR 1, Gros PAR 2", null),
        new("Barres", 100, 1, "Barre 1, Barre 2", 4),
        new("Lyres", 100, 1, "Lyre 1, Lyre 2", 5),
        new("UV", 40, 0, "UV 1, UV 2", 6),
        new("Non assigné", null, 0, "Effet multi-têtes, Fumée", null),
    ];

    private static Color C(string hex) => Color.Parse(hex);

    // ------------------------------------------------------------------ éléments communs

    private static TextBlock T(string text, double size = 13, Color? color = null, FontWeight weight = FontWeight.Normal, TextWrapping wrap = TextWrapping.NoWrap) =>
        new() { Text = text, FontSize = size, Foreground = Tokens.Brush(color ?? Tokens.Text), FontWeight = weight, TextWrapping = wrap, VerticalAlignment = VerticalAlignment.Center };

    private static Border Btn(string text, Color? bg = null, Color? fg = null, double minHeight = 34, double minWidth = 0, Color? border = null, double size = 13, FontWeight weight = FontWeight.Normal) =>
        new()
        {
            Background = Tokens.Brush(bg ?? Tokens.Raised),
            BorderBrush = Tokens.Brush(border ?? Tokens.Border),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            MinHeight = minHeight,
            MinWidth = minWidth,
            Padding = new Thickness(12, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = text, FontSize = size, FontWeight = weight, Foreground = Tokens.Brush(fg ?? Tokens.Text), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
        };

    private static Control Card(string? title, Control content, string? hint = null)
    {
        var stack = new DockPanel();
        if (title is not null)
        {
            var head = new DockPanel { Margin = new Thickness(12, 8, 12, 4) };
            if (hint is not null)
            {
                var h = T(hint, 11, Tokens.Secondary);
                DockPanel.SetDock(h, Avalonia.Controls.Dock.Right);
                head.Children.Add(h);
            }

            var help = new Border { Width = 20, Height = 20, CornerRadius = new CornerRadius(10), Background = Tokens.Brush(Tokens.Raised), Margin = new Thickness(0, 0, 8, 0), Child = T("?", 12, Tokens.Accent, FontWeight.Bold).WithCenter() };
            ToolTip.SetTip(help, "Aide : ce que c'est, à quoi ça sert, un exemple.");
            DockPanel.SetDock(help, Avalonia.Controls.Dock.Left);
            head.Children.Add(help);
            head.Children.Add(T(title, 14, Tokens.Text, FontWeight.SemiBold));
            DockPanel.SetDock(head, Avalonia.Controls.Dock.Top);
            stack.Children.Add(head);
        }

        stack.Children.Add(content);
        return new Border
        {
            Background = Tokens.Brush(Tokens.Surface),
            BorderBrush = Tokens.Brush(Tokens.Border),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(4),
            ClipToBounds = true,
            Child = stack,
        };
    }

    /// <summary>Reprend une vue de panneau des premières maquettes, sans la marge réservée à l'onglet de Dock.</summary>
    private static Control Reuse(Control panel) => new Border { Margin = new Thickness(0, -30, 0, 0), Child = panel };

    private static Control Bar(int pct, Color color, double width, double height = 10) =>
        new Border
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(height / 2),
            Background = Tokens.Brush(Tokens.Background),
            BorderBrush = Tokens.Brush(Tokens.Border),
            BorderThickness = new Thickness(1),
            Child = new Border { Width = width * pct / 100.0, HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(height / 2), Background = Tokens.Brush(color) },
        };

    private static Control Dot(Color color, double size = 10) => new Ellipse { Width = size, Height = size, Fill = Tokens.Brush(color), VerticalAlignment = VerticalAlignment.Center };

    private static Control Status(string text) =>
        new Border
        {
            Background = Tokens.Brush(Tokens.Surface),
            BorderBrush = Tokens.Brush(Tokens.Border),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(10, 3),
            Child = T(text, 12, Tokens.Secondary),
        };

    private static Window Shell(string title, double width, double height, Control content) =>
        new()
        {
            Title = title,
            Width = width,
            Height = height,
            Background = Tokens.Brush(Tokens.Background),
            Content = content,
        };

    // ------------------------------------------------------------------ 1. écran de jeu

    private static Window Game(double width, double height, bool compact)
    {
        var root = new DockPanel();

        var header = GameHeader(compact);
        DockPanel.SetDock(header, Avalonia.Controls.Dock.Top);
        root.Children.Add(header);

        var status = Status("Show de travail  ·  lieu Générique  ·  14 appareils  ·  sortie Arduino COM5, 40 trames / s  ·  2 platines MIDI : APC mini MK2 (couches), APC mini MK1 (dimmers)");
        DockPanel.SetDock(status, Avalonia.Controls.Dock.Bottom);
        root.Children.Add(status);

        // Bas : pilote (place réservée) + journal, hauteur réduite.
        var bottom = new Grid { Height = compact ? 92 : 130, ColumnDefinitions = new ColumnDefinitions("*,*") };
        var pilot = Card("Pilote automatique", new StackPanel
        {
            Margin = new Thickness(12, 2),
            Children = { T("Titre, style, show choisi, raison — place réservée (P10). Interventions : les looks ci-dessus.", 12, Tokens.Secondary, wrap: TextWrapping.Wrap) },
        });
        var log = Card("Journal", new StackPanel
        {
            Margin = new Thickness(12, 0),
            Children =
            {
                Log("21:42:31  Lyre 1, Lyre 2 : couleur réglée (retouche en direct)"),
                Log("21:42:18  Lancer « Lyres sur 3 positions » (Mouvements)"),
                Log("21:42:02  Dimmer « PAR scène » : 50 %"),
            },
        });
        Grid.SetColumn(log, 1);
        bottom.Children.Add(pilot);
        bottom.Children.Add(log);
        DockPanel.SetDock(bottom, Avalonia.Controls.Dock.Bottom);
        root.Children.Add(bottom);

        // Centre : colonnes (grandes) à gauche, dimmers de groupe et looks à droite.
        var body = new Grid { ColumnDefinitions = new ColumnDefinitions(compact ? "*,340" : "*,470") };
        body.Children.Add(Card("Colonnes", ColumnsBig(compact), "un ascenseur par colonne · ✎ ouvre la fenêtre d'édition"));

        var right = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        right.Children.Add(Card("Groupes dimmer", DimmerFaders(compact), "platine 2 : faders 1 à 6"));
        var looks = Card("Looks  (F1 à F12)", LooksGrid(compact));
        Grid.SetRow(looks, 1);
        right.Children.Add(looks);
        Grid.SetColumn(right, 1);
        body.Children.Add(right);
        root.Children.Add(body);

        return Shell("LuXia – maquette : écran de jeu" + (compact ? " (1366 × 768)" : string.Empty), width, height, root);
    }

    private static Control Log(string text) => new TextBlock { Text = text, FontSize = 12, FontFamily = new FontFamily("Consolas, Cascadia Mono, monospace"), Foreground = Tokens.Brush(Tokens.Text), Margin = new Thickness(0, 1) };

    private static Control GameHeader(bool compact)
    {
        var left = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(12, 0), VerticalAlignment = VerticalAlignment.Center };
        left.Children.Add(T("LuXia", 20, Tokens.Accent, FontWeight.Bold));
        left.Children.Add(Btn(compact ? "Projet ▾" : "Show de travail ▾"));
        if (!compact)
        {
            left.Children.Add(Btn("Menu ▾"));
        }

        left.Children.Add(new Border { Width = 10 });
        left.Children.Add(Btn("■ Stop", fg: Tokens.Danger, border: Tokens.Danger));
        left.Children.Add(Btn(compact ? "■ Tout" : "■ Tout stopper", fg: Tokens.Danger, border: Tokens.Danger));
        left.Children.Add(new Border { Width = 10 });
        left.Children.Add(Btn(compact ? "⌨" : "⌨ Affecter"));
        left.Children.Add(Btn(compact ? "🔒" : "🔒 Verrou soirée", fg: Tokens.Secondary));

        var right = new StackPanel { Orientation = Orientation.Horizontal, Spacing = compact ? 8 : 12, Margin = new Thickness(8, 0), VerticalAlignment = VerticalAlignment.Center };
        right.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { Dot(Tokens.Edit), T("Sortie", 12), T(compact ? string.Empty : "Arduino COM5", 11, Tokens.Secondary) } });
        right.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { Dot(Tokens.Edit), T("MIDI ×2", 12) } });
        right.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { Dot(Tokens.Secondary), T("Sûreté", 12) } });
        right.Children.Add(T(compact ? "GM" : "Grand Master", 12, Tokens.Secondary));
        right.Children.Add(Bar(85, Tokens.Accent, compact ? 80 : 120, 14));
        right.Children.Add(T("85 %", 12, Tokens.Text, FontWeight.SemiBold));
        right.Children.Add(Btn("BLACKOUT", Color.FromArgb(38, 248, 81, 73), Tokens.Danger, minHeight: 36, border: Tokens.Danger, weight: FontWeight.Bold));

        var bar = new DockPanel { MinHeight = 52, Background = Tokens.Brush(Tokens.Surface), LastChildFill = false };
        DockPanel.SetDock(left, Avalonia.Controls.Dock.Left);
        DockPanel.SetDock(right, Avalonia.Controls.Dock.Right);
        bar.Children.Add(left);
        bar.Children.Add(right);

        // Retouches en direct : seul rappel d'état de l'écran de jeu (plus de sélecteur de modes).
        var live = new Border
        {
            Background = Tokens.Brush(Tokens.Live, 0.16),
            BorderBrush = Tokens.Brush(Tokens.Live),
            BorderThickness = new Thickness(0, 1, 0, 1),
            Padding = new Thickness(12, 4),
            Child = new DockPanel
            {
                LastChildFill = true,
                Children =
                {
                    Dock(Btn("Libérer tout  (Échap)", null, Tokens.Live, minHeight: 28, border: Tokens.Live, size: 12), Avalonia.Controls.Dock.Right),
                    T("Retouches en direct (temporaires, rien n'est enregistré) :  Lyre 1, Lyre 2 — couleur · Groupe UV — dimmer 40 %", 13),
                },
            },
        };
        return new StackPanel { Children = { bar, live } };
    }

    private static Control Dock(Control control, Avalonia.Controls.Dock dock)
    {
        DockPanel.SetDock(control, dock);
        return control;
    }

    private static Control ColumnsBig(bool compact)
    {
        var grid = new Grid { Margin = new Thickness(8, 0, 8, 8) };
        var layers = MockShow.Layers;
        for (var i = 0; i < layers.Count; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            var column = Column(layers[i], compact);
            Grid.SetColumn(column, i);
            grid.Children.Add(column);
        }

        return grid;
    }

    private static Control Column(MockLayer layer, bool compact)
    {
        var playing = layer.Scenes.Any(s => s.Progress is not null);
        var controls = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3, HorizontalAlignment = HorizontalAlignment.Center };
        foreach (var glyph in new[] { "⏸", "◀", "▶", "■" })
        {
            controls.Children.Add(new Border
            {
                MinWidth = compact ? 26 : 34,
                Height = 32,
                CornerRadius = new CornerRadius(5),
                Background = Tokens.Brush(Tokens.Raised),
                Child = new TextBlock { Text = glyph, FontSize = 14, Foreground = Tokens.Brush(glyph == "■" && playing ? Tokens.Danger : Tokens.Text), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
            });
        }

        var head = new Border
        {
            BorderBrush = Tokens.Brush(layer.Color),
            BorderThickness = new Thickness(0, 3, 0, 0),
            Padding = new Thickness(2, 6, 2, 4),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock { Text = $"{layer.Icon}  {layer.Name}", FontWeight = FontWeight.SemiBold, FontSize = 14, Foreground = Tokens.Brush(Tokens.Text), HorizontalAlignment = HorizontalAlignment.Center },
                    controls,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 4,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Children = { T("Niveau", 11, Tokens.Secondary), Bar(layer.Master, layer.Color, compact ? 44 : 70, 12), T($"{layer.Master} %", 11) },
                    },
                },
            },
        };

        var scenes = new StackPanel { Spacing = 5 };
        foreach (var scene in layer.Scenes)
        {
            scenes.Children.Add(SceneButton(scene, compact));
        }

        scenes.Children.Add(Btn(layer.Scenes.Count == 0 ? "+ scène (aucune)" : "+ scène", Tokens.Surface, Tokens.Secondary, minHeight: 36, size: 12));

        var body = new DockPanel { Margin = new Thickness(0, 0, 6, 0) };
        DockPanel.SetDock(head, Avalonia.Controls.Dock.Top);
        body.Children.Add(head);
        body.Children.Add(new ScrollViewer { Content = scenes, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto });
        return new Border { Background = Tokens.Brush(Tokens.Background), CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 4, 0), Padding = new Thickness(4), Child = body };
    }

    private static Control SceneButton(MockScene scene, bool compact)
    {
        var playing = scene.Progress is not null;
        var textColor = playing && ((0.299 * scene.Color.R) + (0.587 * scene.Color.G) + (0.114 * scene.Color.B)) / 255 > 0.6 ? Tokens.Background : Tokens.Text;
        var info = new TextBlock { Text = playing ? scene.StepInfo ?? "joue" : string.Empty, FontSize = 11, Foreground = Tokens.Brush(textColor, 0.8), IsVisible = playing };
        var play = new Border
        {
            Background = playing ? Tokens.Brush(scene.Color, 0.85) : Tokens.Brush(Tokens.Raised),
            BorderBrush = Tokens.Brush(scene.Color),
            BorderThickness = new Thickness(4, 0, 0, 0),
            CornerRadius = new CornerRadius(6, 0, 0, 6),
            Padding = new Thickness(8, 5),
            MinHeight = compact ? 44 : 50,
            Child = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock { Text = scene.Name, FontSize = 13, FontWeight = playing ? FontWeight.SemiBold : FontWeight.Normal, Foreground = Tokens.Brush(textColor), TextWrapping = TextWrapping.Wrap, MaxLines = 2 },
                    info,
                },
            },
        };
        var edit = new Border
        {
            Width = 34,
            Background = Tokens.Brush(Tokens.Surface),
            CornerRadius = new CornerRadius(0, 6, 6, 0),
            Child = new TextBlock { Text = "✎", FontSize = 16, Foreground = Tokens.Brush(Tokens.Secondary), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
        };
        ToolTip.SetTip(edit, "Ouvrir la fenêtre d'édition de cette scène");
        var row = new DockPanel();
        DockPanel.SetDock(edit, Avalonia.Controls.Dock.Right);
        row.Children.Add(edit);
        row.Children.Add(play);
        return row;
    }

    private static Control DimmerFaders(bool compact)
    {
        var faders = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = compact ? 2 : 6, Margin = new Thickness(6, 4) };
        // Niveau effectif = produit des dimmers de la racine au groupe (règle proportionnelle, Q37).
        for (var i = 0; i < Tree.Length; i++)
        {
            var g = Tree[i];
            if (g.Level is null)
            {
                continue;
            }

            var effective = 1.0;
            var depth = g.Depth;
            effective *= g.Level.Value / 100.0;
            for (var k = i - 1; k >= 0 && depth > 0; k--)
            {
                if (Tree[k].Depth < depth)
                {
                    depth = Tree[k].Depth;
                    if (Tree[k].Level is { } lv)
                    {
                        effective *= lv / 100.0;
                    }
                }
            }

            faders.Children.Add(VFader(g, (int)Math.Round(effective * 100), compact ? 52 : 68, compact ? 150 : 250));
        }

        var caption = T("Le niveau effectif multiplie chaque étage de l'arbre (Parc × Face × PAR scène). Le Grand Master reste à part.", 11, Tokens.Secondary, wrap: TextWrapping.Wrap);
        caption.Margin = new Thickness(12, 0, 12, 8);
        DockPanel.SetDock(caption, Avalonia.Controls.Dock.Bottom);
        return new DockPanel { Children = { caption, new ScrollViewer { Content = faders, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled } } };
    }

    private static Control VFader(Group g, int effectivePct, double width, double trackHeight)
    {
        var level = g.Level ?? 100;
        var color = g.Name == "UV" ? C("#8957E5") : Tokens.Accent;
        var retouched = g.Name == "UV";
        var track = new Border
        {
            Width = 36,
            Height = trackHeight,
            CornerRadius = new CornerRadius(8),
            Background = Tokens.Brush(Tokens.Background),
            BorderBrush = Tokens.Brush(retouched ? Tokens.Live : Tokens.Border),
            BorderThickness = new Thickness(retouched ? 2 : 1),
            Child = new Border { Height = trackHeight * level / 100.0, VerticalAlignment = VerticalAlignment.Bottom, CornerRadius = new CornerRadius(7), Background = Tokens.Brush(color, 0.85) },
        };
        var badge = new Border
        {
            Background = Tokens.Brush(Tokens.Raised),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(6, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = T($"② {g.Fader}", 10, Tokens.Secondary),
        };
        return new StackPanel
        {
            Width = width,
            Spacing = 4,
            Children =
            {
                T($"{level} %", 14, retouched ? Tokens.Live : Tokens.Text, FontWeight.SemiBold).WithCenter(),
                track,
                new TextBlock { Text = g.Name, FontSize = 12, FontWeight = FontWeight.SemiBold, Foreground = Tokens.Brush(Tokens.Text), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, MinHeight = 34 },
                T($"= {effectivePct} %", 11, Tokens.Secondary).WithCenter(),
                badge,
            },
        };
    }

    private static Control LooksGrid(bool compact)
    {
        var wrap = new WrapPanel { Margin = new Thickness(10, 2, 10, 10) };
        foreach (var (name, color) in new[] { ("F1  Accueil", "#E3B341"), ("F2  Danse", "#DB61A2"), ("F3  Temps mort", "#58A6FF"), ("F4  Slow", "#8957E5"), ("F5  Final", "#F85149"), ("F6  Noir + UV", "#30363D") })
        {
            wrap.Children.Add(new Border
            {
                Width = compact ? 148 : 140,
                MinHeight = 44,
                Margin = new Thickness(0, 0, 6, 6),
                CornerRadius = new CornerRadius(6),
                Background = Tokens.Brush(C(color), 0.35),
                BorderBrush = Tokens.Brush(C(color)),
                BorderThickness = new Thickness(0, 0, 0, 3),
                Child = T(name, 13, Tokens.Text, FontWeight.SemiBold).WithCenter(),
            });
        }

        return wrap;
    }

    private static TControl WithCenter<TControl>(this TControl control)
        where TControl : Control
    {
        control.HorizontalAlignment = HorizontalAlignment.Center;
        return control;
    }

    // ------------------------------------------------------------------ 2. fenêtre d'édition

    private static Window Edit(double width, double height, bool blind)
    {
        var scenario = blind
            ? new MockScenario("edition-aveugle", "Édition aveugle", MockMode.Blind, MockTab.Position, ["lyre1", "lyre2"], 2, false, MockShow.LyresScene)
            : new MockScenario("edition", "Édition", MockMode.Edit, MockTab.Color, ["par1", "par2", "par3", "par4"], 1, false);
        var scene = MockShow.Find(scenario.EditScene);
        var accent = blind ? Tokens.Blind : Tokens.Edit;

        var root = new DockPanel();

        // Titre : rappel que l'on travaille sur un brouillon.
        var title = new Border
        {
            Background = Tokens.Brush(accent, 0.16),
            BorderBrush = Tokens.Brush(accent),
            BorderThickness = new Thickness(0, 0, 0, 2),
            Padding = new Thickness(14, 8),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                Children =
                {
                    new Border { Background = Tokens.Brush(accent), CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 2), Child = T("BROUILLON", 12, Tokens.Background, FontWeight.Bold) },
                    new Border { Width = 22, Height = 22, CornerRadius = new CornerRadius(4), Background = Tokens.Brush(scene.Color) },
                    T(scene.Name, 18, Tokens.Text, FontWeight.SemiBold),
                    T("— rien n'est enregistré tant que vous n'avez pas validé  ·  3 modifications depuis l'ouverture", 13, Tokens.Secondary),
                },
            },
        };
        DockPanel.SetDock(title, Avalonia.Controls.Dock.Top);
        root.Children.Add(title);

        // Pied : aveugle, état de la sortie, Appliquer / Annuler / Valider.
        var check = new Border
        {
            Width = 20,
            Height = 20,
            CornerRadius = new CornerRadius(4),
            Background = blind ? Tokens.Brush(Tokens.Blind) : Tokens.Brush(Tokens.Background),
            BorderBrush = Tokens.Brush(blind ? Tokens.Blind : Tokens.Border),
            BorderThickness = new Thickness(1),
            Child = new TextBlock { Text = blind ? "✓" : string.Empty, Foreground = Tokens.Brush(Tokens.Background), FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center },
        };
        var footer = new DockPanel { Background = Tokens.Brush(Tokens.Surface), MinHeight = 60, LastChildFill = false };
        var leftFooter = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(14, 0), VerticalAlignment = VerticalAlignment.Center };
        leftFooter.Children.Add(check);
        leftFooter.Children.Add(T("👁 Aveugle", 14, blind ? Tokens.Blind : Tokens.Text, FontWeight.SemiBold));
        leftFooter.Children.Add(T(
            blind
                ? "Aperçu au plan seulement : la sortie sur scène ne change pas."
                : "Le brouillon est montré sur la sortie (un fondu de 0,5 s à chaque retouche). Cochez pour ne rien changer sur scène.",
            13,
            Tokens.Secondary));
        var rightFooter = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(14, 0), VerticalAlignment = VerticalAlignment.Center };
        rightFooter.Children.Add(T("Ctrl+Z / Ctrl+Y dans le brouillon", 12, Tokens.Secondary));
        rightFooter.Children.Add(Btn("Appliquer", minHeight: 40, minWidth: 110, size: 14));
        rightFooter.Children.Add(Btn("Annuler", minHeight: 40, minWidth: 110, size: 14));
        rightFooter.Children.Add(Btn("Valider", accent, Tokens.Background, minHeight: 40, minWidth: 130, border: accent, size: 14, weight: FontWeight.Bold));
        DockPanel.SetDock(leftFooter, Avalonia.Controls.Dock.Left);
        DockPanel.SetDock(rightFooter, Avalonia.Controls.Dock.Right);
        footer.Children.Add(leftFooter);
        footer.Children.Add(rightFooter);
        DockPanel.SetDock(footer, Avalonia.Controls.Dock.Bottom);
        root.Children.Add(footer);

        // Disposition fixe : plan à gauche, réglages au centre, propriétés (grandes) à droite.
        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("470,*,470") };
        body.Children.Add(Card("Plan des appareils", Reuse(FixturePlanView.Create(scenario))));
        var settings = Card("Réglages des appareils", Reuse(SettingsView.Create(scenario)), "onglets : Intensité · Couleur · Position · Faisceau · Effets");
        Grid.SetColumn(settings, 1);
        body.Children.Add(settings);
        var props = Card("Propriétés et étapes", Reuse(PropertiesView.Create(scenario)));
        Grid.SetColumn(props, 2);
        body.Children.Add(props);
        root.Children.Add(body);

        return Shell("LuXia – maquette : fenêtre d'édition de scène" + (blind ? " (aveugle)" : string.Empty), width, height, root);
    }

    // ------------------------------------------------------------------ 3. gestion des dimmers

    private static Window Dimmers(double width, double height)
    {
        var root = new DockPanel();

        var tabs = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Thickness(10, 8, 10, 0) };
        foreach (var (label, active) in new[] { ("Patch", false), ("Lieux", false), ("Sélections", false), ("Gestion des dimmers", true) })
        {
            tabs.Children.Add(new Border
            {
                Padding = new Thickness(16, 8),
                CornerRadius = new CornerRadius(6, 6, 0, 0),
                Background = Tokens.Brush(active ? Tokens.Surface : Tokens.Raised),
                BorderBrush = Tokens.Brush(active ? Tokens.Accent : Tokens.Border),
                BorderThickness = new Thickness(0, active ? 3 : 1, 0, 0),
                Child = T(label, 14, active ? Tokens.Text : Tokens.Secondary, active ? FontWeight.SemiBold : FontWeight.Normal),
            });
        }

        var head = new StackPanel { Children = { new Border { Background = Tokens.Brush(Tokens.Surface), Padding = new Thickness(14, 10), Child = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { T("LuXia", 20, Tokens.Accent, FontWeight.Bold), T("Installation", 16, Tokens.Text, FontWeight.SemiBold) } } }, tabs } };
        DockPanel.SetDock(head, Avalonia.Controls.Dock.Top);
        root.Children.Add(head);
        var status = Status("Les groupes se règlent ici ; les dimmers se jouent sur l'écran de jeu (panneau « Groupes dimmer ») et sur la platine MIDI n° 2.");
        DockPanel.SetDock(status, Avalonia.Controls.Dock.Bottom);
        root.Children.Add(status);

        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("560,*,440") };

        // Arbre.
        var tree = new StackPanel { Margin = new Thickness(10, 4), Spacing = 4 };
        foreach (var g in Tree)
        {
            tree.Children.Add(TreeRow(g, g.Name == "PAR scène"));
        }

        tree.Children.Add(new Border { Height = 6 });
        tree.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { Btn("+ Groupe", minHeight: 34), Btn("+ Sous-groupe", minHeight: 34), Btn("Renommer", minHeight: 34), Btn("Supprimer", fg: Tokens.Danger, minHeight: 34) } });
        body.Children.Add(Card("Arbre des groupes", tree, "un appareil dans un seul groupe"));

        // Détail du groupe choisi.
        var detail = new StackPanel { Margin = new Thickness(14, 6), Spacing = 12 };
        detail.Children.Add(Field("Nom", "PAR scène"));
        detail.Children.Add(Field("Groupe parent", "Face (PAR)"));
        detail.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { Check(true), T("Ce groupe a un dimmer", 14, Tokens.Text, FontWeight.SemiBold), T("(un fader dans « Groupes dimmer » et sur la platine 2)", 12, Tokens.Secondary) } });
        detail.Children.Add(Field("Fader de la platine MIDI 2", "3   (ordre du panneau « Groupes dimmer »)"));
        detail.Children.Add(T("Appareils du groupe", 13, Tokens.Secondary));
        foreach (var name in new[] { "PAR 1", "PAR 2", "PAR 3", "PAR 4" })
        {
            detail.Children.Add(Member(name, "PAR", withRemove: true));
        }

        detail.Children.Add(new Border { Height = 4 });
        detail.Children.Add(Flow());
        var center = Card("Groupe « PAR scène »", detail);
        Grid.SetColumn(center, 1);
        body.Children.Add(center);

        // Appareils libres et autres groupes.
        var free = new StackPanel { Margin = new Thickness(14, 6), Spacing = 8 };
        free.Children.Add(T("Non assignés : ils restent dans le groupe implicite « Non assigné » (renommable), sans dimmer.", 12, Tokens.Secondary, wrap: TextWrapping.Wrap));
        foreach (var (name, kind) in new[] { ("Effet multi-têtes", "Effet"), ("Fumée", "Fumée") })
        {
            free.Children.Add(Member(name, kind, withRemove: false));
        }

        free.Children.Add(new Border { Height = 8 });
        free.Children.Add(T("Ajouter au groupe « PAR scène » :", 13, Tokens.Secondary));
        foreach (var (name, kind) in new[] { ("Gros PAR 1", "PAR · groupe « Gros PAR »"), ("Gros PAR 2", "PAR · groupe « Gros PAR »"), ("Barre 1", "Barre · groupe « Barres »") })
        {
            free.Children.Add(Member(name, kind, withRemove: false, add: true));
        }

        free.Children.Add(T("Glisser-déposer vers l'arbre, ou « ← Ajouter ». Ajouter un appareil déjà rangé le déplace.", 11, Tokens.Secondary, wrap: TextWrapping.Wrap));
        var right = Card("Appareils", free);
        Grid.SetColumn(right, 2);
        body.Children.Add(right);

        root.Children.Add(body);
        return Shell("LuXia – maquette : Installation › Gestion des dimmers", width, height, root);
    }

    private static Control TreeRow(Group g, bool selected)
    {
        var row = new DockPanel { MinHeight = 38, Margin = new Thickness(g.Depth * 28, 0, 0, 0) };
        var members = g.Members.Length > 0 ? g.Members : "(sous-groupes)";
        if (g.Level is { } level)
        {
            var badge = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            badge.Children.Add(T("niveau actuel", 11, Tokens.Secondary));
            badge.Children.Add(Bar(level, Tokens.Accent, 70, 10));
            badge.Children.Add(T($"{level} %", 12, Tokens.Text, FontWeight.SemiBold));
            badge.Children.Add(new Border { Background = Tokens.Brush(Tokens.Raised), CornerRadius = new CornerRadius(8), Padding = new Thickness(6, 0), Child = T($"② {g.Fader}", 10, Tokens.Secondary) });
            DockPanel.SetDock(badge, Avalonia.Controls.Dock.Right);
            row.Children.Add(badge);
        }

        row.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                T(g.Depth > 0 ? "└" : "▾", 14, Tokens.Secondary),
                T(g.Name, 14, g.Name == "Non assigné" ? Tokens.Secondary : Tokens.Text, FontWeight.SemiBold),
                T(members, 11, Tokens.Secondary),
            },
        });
        return new Border
        {
            Background = selected ? Tokens.Brush(Tokens.Accent, 0.16) : Tokens.Brush(Tokens.Background),
            BorderBrush = selected ? Tokens.Brush(Tokens.Accent) : null,
            BorderThickness = new Thickness(selected ? 2 : 0),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 0),
            Child = row,
        };
    }

    private static Control Field(string label, string value) =>
        new StackPanel
        {
            Spacing = 3,
            Children =
            {
                T(label, 12, Tokens.Secondary),
                new Border { Background = Tokens.Brush(Tokens.Background), BorderBrush = Tokens.Brush(Tokens.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Padding = new Thickness(10, 7), Child = T(value, 14) },
            },
        };

    private static Control Check(bool on) =>
        new Border
        {
            Width = 20,
            Height = 20,
            CornerRadius = new CornerRadius(4),
            Background = Tokens.Brush(on ? Tokens.Accent : Tokens.Background),
            BorderBrush = Tokens.Brush(on ? Tokens.Accent : Tokens.Border),
            BorderThickness = new Thickness(1),
            Child = new TextBlock { Text = on ? "✓" : string.Empty, Foreground = Tokens.Brush(Tokens.Background), FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center },
        };

    private static Control Member(string name, string kind, bool withRemove, bool add = false)
    {
        var row = new DockPanel { MinHeight = 36 };
        if (withRemove || add)
        {
            var b = Btn(add ? "← Ajouter" : "Retirer", minHeight: 30, size: 12);
            DockPanel.SetDock(b, Avalonia.Controls.Dock.Right);
            row.Children.Add(b);
        }

        row.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center, Children = { T("⠿", 14, Tokens.Secondary), T(name, 14), T(kind, 11, Tokens.Secondary) } });
        return new Border { Background = Tokens.Brush(Tokens.Background), CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 0), Child = row };
    }

    private static Control Flow() =>
        new Border
        {
            Background = Tokens.Brush(Tokens.Background),
            BorderBrush = Tokens.Brush(Tokens.Border),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    T("Flux d'intensité jusqu'aux PAR 1 à 4 — exemple avec les niveaux actuels (lecture seule)", 13, Tokens.Text, FontWeight.SemiBold),
                    new WrapPanel
                    {
                        Children =
                        {
                            Step("Scène / couches", "100 %"), Arrow(), Step("Grand Master", "85 %"), Arrow(), Step("Parc lumineux", "80 %"), Arrow(), Step("Face (PAR)", "70 %"), Arrow(), Step("PAR scène", "50 %"), Arrow(), Step("Sortie DMX", "24 %", accent: true),
                        },
                    },
                    T("Chaque étage multiplie : 100 % × 85 % × 80 % × 70 % × 50 % ≈ 24 %. Les formes des effets et les fondus restent intacts.", 12, Tokens.Secondary, wrap: TextWrapping.Wrap),
                },
            },
        };

    private static Control Step(string name, string value, bool accent = false) =>
        new Border
        {
            Margin = new Thickness(0, 0, 0, 4),
            Padding = new Thickness(10, 4),
            CornerRadius = new CornerRadius(6),
            Background = Tokens.Brush(accent ? Tokens.Accent : Tokens.Raised, accent ? 0.9 : 1),
            Child = new StackPanel { Children = { T(name, 11, accent ? Tokens.Background : Tokens.Secondary), T(value, 15, accent ? Tokens.Background : Tokens.Text, FontWeight.SemiBold) } },
        };

    private static Control Arrow() => new TextBlock { Text = "→", FontSize = 16, Foreground = Tokens.Brush(Tokens.Secondary), Margin = new Thickness(6, 8), VerticalAlignment = VerticalAlignment.Center };
}
