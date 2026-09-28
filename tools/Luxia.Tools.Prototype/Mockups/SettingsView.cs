using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Luxia.UI.Controls;

namespace Luxia.Tools.Prototype.Mockups;

/// <summary>
/// Panneau Réglages des appareils (doc 60 §2, §5) : les réglages de la sélection du plan, par famille (onglets ; un
/// point « • » = la famille a des valeurs). Chaque paramètre porte sa pastille : 🟡 surcharge LIVE, 🟢 dans la
/// scène, ◯ non utilisé. Les composants sont les vrais (grille Pan / Tilt, sélecteur de couleur).
/// </summary>
internal static class SettingsView
{
    public static Control Create(MockScenario scenario)
    {
        var dot = scenario.Mode == MockMode.Live ? Tokens.Live : Tokens.ModeColor(scenario.Mode);
        var names = string.Join(", ", MockShow.Fixtures.Where(f => scenario.Selected.Contains(f.Id)).Select(f => f.Name));
        var target = scenario switch
        {
            { ZoneEditing: true } => "zones du lieu Générique : valent pour toutes les scènes",
            { Mode: MockMode.Live } => "surcharge LIVE, non enregistrée",
            _ => $"écrit dans « {scenario.EditScene} » › étape {scenario.EditStep + 1}",
        };

        var header = new DockPanel { Margin = new Thickness(10, 34, 40, 6) };
        var action = new Border
        {
            BorderBrush = Tokens.Brush(dot),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 3),
            Child = new TextBlock { Text = scenario.ZoneEditing ? "Terminer les zones" : scenario.Mode == MockMode.Live ? "Libérer la sélection" : "Retirer de l'étape", FontSize = 12, Foreground = Tokens.Brush(dot) },
        };
        ToolTip.SetTip(action, scenario.Mode == MockMode.Live ? "Rend la main aux scènes pour ces appareils" : "Ces appareils ne seront plus réglés par cette étape");
        DockPanel.SetDock(action, Avalonia.Controls.Dock.Right);
        header.Children.Add(action);
        var summary = new TextBlock { FontSize = 13, VerticalAlignment = VerticalAlignment.Center };
        summary.Inlines!.Add(new Avalonia.Controls.Documents.Run(names) { FontWeight = FontWeight.SemiBold, Foreground = Tokens.Brush(Tokens.Text) });
        summary.Inlines.Add(new Avalonia.Controls.Documents.Run($"   ·   {target}") { Foreground = Tokens.Brush(dot) });
        header.Children.Add(summary);

        var tabs = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 0, 10, 0) };
        foreach (var (label, tab, hasValues) in new (string, MockTab?, bool)[]
        {
            ("Intensité", null, true), ("Couleur", MockTab.Color, true), ("Position", MockTab.Position, scenario.Tab == MockTab.Position || scenario.Selected[0].StartsWith("lyre", StringComparison.Ordinal)),
            ("Faisceau", null, false), ("Autres", null, false), ("Faders", null, false),
        })
        {
            var active = tab == scenario.Tab;
            tabs.Children.Add(new Border
            {
                Padding = new Thickness(14, 6),
                BorderBrush = active ? Tokens.Brush(Tokens.Accent) : Tokens.Brush(Tokens.Border),
                BorderThickness = new Thickness(0, 0, 0, active ? 2 : 1),
                Child = new TextBlock
                {
                    Text = hasValues ? label + " •" : label,
                    FontSize = 13,
                    FontWeight = active ? FontWeight.SemiBold : FontWeight.Normal,
                    Foreground = Tokens.Brush(active ? Tokens.Text : Tokens.Secondary),
                },
            });
        }

        Control body = scenario.Tab == MockTab.Position ? PositionTab(scenario, dot) : ColorTab(scenario, dot);
        var root = new DockPanel();
        DockPanel.SetDock(header, Avalonia.Controls.Dock.Top);
        DockPanel.SetDock(tabs, Avalonia.Controls.Dock.Top);
        root.Children.Add(header);
        root.Children.Add(tabs);
        root.Children.Add(body);
        return root;
    }

    private static Grid ColorTab(MockScenario scenario, Color dot)
    {
        var isPar = scenario.Selected[0].StartsWith("par", StringComparison.Ordinal);
        var color = isPar ? new LightColor(222, 1, 1) : new LightColor(30, 0.85, 1);
        var picker = new ColorPicker
        {
            SelectedColor = color,
            Favorites = [new(0, 1, 1), new(30, 1, 1), new(40, 0.55, 1), new(222, 1, 1), new(330, 0.7, 1), new(120, 1, 1)],
            Width = 330,
            Height = 230,
            VerticalAlignment = VerticalAlignment.Top,
        };

        var (r, g, b) = color.ToRgb();
        var parameters = Parameters(dot,
            ("Intensité", "100 %", true),
            ("Rouge", $"{r * 100 / 255} %", true),
            ("Vert", $"{g * 100 / 255} %", true),
            ("Bleu", $"{b * 100 / 255} %", true),
            ("Blanc", "—", false),
            ("Stroboscope", "—", false));

        var palettes = new WrapPanel { Orientation = Orientation.Horizontal, MaxWidth = 300 };
        foreach (var (name, c) in MockShow.ColorPalettes)
        {
            palettes.Children.Add(new StackPanel
            {
                Margin = new Thickness(0, 0, 8, 8),
                Spacing = 3,
                Width = 62,
                Children =
                {
                    new Border { Height = 26, CornerRadius = new CornerRadius(4), Background = Tokens.Brush(c), BorderBrush = Tokens.Brush(Tokens.Border), BorderThickness = new Thickness(1) },
                    new TextBlock { Text = name, FontSize = 11, Foreground = Tokens.Brush(Tokens.Secondary), HorizontalAlignment = HorizontalAlignment.Center },
                },
            });
        }

        var side = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Palettes du projet", FontWeight = FontWeight.SemiBold, FontSize = 13, Foreground = Tokens.Brush(Tokens.Text) },
                palettes,
                new TextBlock { Text = "Clic : appliquer · clic droit : mettre à jour avec ces réglages", FontSize = 11, Foreground = Tokens.Brush(Tokens.Secondary), TextWrapping = TextWrapping.Wrap, MaxWidth = 300 },
            },
        };
        return ThreeColumns(picker, parameters, side);
    }

    private static Grid PositionTab(MockScenario scenario, Color dot)
    {
        var markers = new List<PanTiltMarker>
        {
            new("lyre1", "Lyre 1", 0.42, 0.62, Color.Parse("#FFB347"), scenario.Selected.Contains("lyre1")),
            new("lyre2", "Lyre 2", 0.58, 0.62, Color.Parse("#FFB347"), scenario.Selected.Contains("lyre2")),
        };
        var zones = scenario.ZoneEditing
            ? new List<PanTiltZoneMarker>
            {
                new("limites", "Limites (zone permise)", new PanTiltRect(0.12, 0.88, 0.18, 0.92), PanTiltZoneKind.Allowed),
                new("public", "Public", new PanTiltRect(0.36, 0.66, 0.2, 0.4), PanTiltZoneKind.Forbidden),
            }
            : [new("public", "Public", new PanTiltRect(0.36, 0.66, 0.2, 0.4), PanTiltZoneKind.Forbidden)];
        var grid = new PanTiltGrid
        {
            Markers = markers,
            Zones = zones,
            IsZoneEditing = scenario.ZoneEditing,
            SelectedZoneId = scenario.ZoneEditing ? "public" : null,
            Width = 420,
            Height = 250,
            VerticalAlignment = VerticalAlignment.Top,
        };

        Control middle = scenario.ZoneEditing
            ? ZoneList()
            : Parameters(dot,
                ("Pan", "227°", true),
                ("Tilt", "167°", true),
                ("Vitesse", "—", false),
                ("Pan fin / Tilt fin", "auto", false));

        var palettes = new StackPanel { Spacing = 6 };
        palettes.Children.Add(new TextBlock { Text = "Palettes de position (lieu Générique)", FontWeight = FontWeight.SemiBold, FontSize = 13, Foreground = Tokens.Brush(Tokens.Text) });
        foreach (var name in MockShow.PositionPalettes)
        {
            palettes.Children.Add(new Border
            {
                Padding = new Thickness(10, 5),
                CornerRadius = new CornerRadius(4),
                Background = Tokens.Brush(Tokens.Raised),
                BorderBrush = Tokens.Brush(name.EndsWith('⚠') ? Tokens.Safety : Tokens.Border),
                BorderThickness = new Thickness(1),
                Width = 200,
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock { Text = name, FontSize = 12, Foreground = Tokens.Brush(Tokens.Text) },
            });
        }

        return ThreeColumns(grid, middle, palettes);
    }

    private static StackPanel ZoneList()
    {
        var list = new StackPanel { Spacing = 8, Width = 250 };
        list.Children.Add(new TextBlock { Text = "Zones de la lyre 1 (lieu Générique)", FontWeight = FontWeight.SemiBold, FontSize = 13, Foreground = Tokens.Brush(Tokens.Text) });
        foreach (var (name, kind, color, selected) in new[]
        {
            ("Public", "interdite : le faisceau n'y va jamais", Tokens.Danger, true),
            ("Limites", "permise : la lyre reste dedans", Tokens.Edit, false),
        })
        {
            list.Children.Add(new Border
            {
                Padding = new Thickness(8, 5),
                CornerRadius = new CornerRadius(4),
                Background = selected ? Tokens.Brush(Tokens.Accent, 0.15) : Tokens.Brush(Tokens.Raised),
                BorderBrush = Tokens.Brush(selected ? Tokens.Accent : Tokens.Border),
                BorderThickness = new Thickness(1),
                Child = new StackPanel
                {
                    Children =
                    {
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 6,
                            Children =
                            {
                                new Rectangle { Width = 12, Height = 12, Fill = Tokens.Brush(color, 0.5), Stroke = Tokens.Brush(color), VerticalAlignment = VerticalAlignment.Center },
                                new TextBlock { Text = name, FontWeight = FontWeight.SemiBold, FontSize = 12, Foreground = Tokens.Brush(Tokens.Text) },
                            },
                        },
                        new TextBlock { Text = kind, FontSize = 11, Foreground = Tokens.Brush(Tokens.Secondary) },
                    },
                },
            });
        }

        list.Children.Add(new TextBlock
        {
            Text = "Glisser dans le vide : nouvelle zone · poignées : ajuster · Suppr : retirer. S'applique tout de suite (Ctrl+Z).",
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Tokens.Brush(Tokens.Secondary),
        });
        return list;
    }

    private static StackPanel Parameters(Color dot, params (string Name, string Value, bool Used)[] rows)
    {
        var panel = new StackPanel { Spacing = 7, Width = 220 };
        panel.Children.Add(new TextBlock { Text = "Paramètres", FontWeight = FontWeight.SemiBold, FontSize = 13, Foreground = Tokens.Brush(Tokens.Text) });
        foreach (var (name, value, used) in rows)
        {
            panel.Children.Add(new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("18,*,Auto"),
                Children =
                {
                    new Ellipse { Width = 10, Height = 10, Fill = used ? Tokens.Brush(dot) : null, Stroke = Tokens.Brush(used ? dot : Tokens.Secondary), StrokeThickness = 1.5, VerticalAlignment = VerticalAlignment.Center },
                    WithColumn(new TextBlock { Text = name, FontSize = 12, Foreground = Tokens.Brush(used ? Tokens.Text : Tokens.Secondary) }, 1),
                    WithColumn(new TextBlock { Text = value, FontSize = 12, Foreground = Tokens.Brush(used ? Tokens.Text : Tokens.Secondary) }, 2),
                },
            });
        }

        var legend = dot == Tokens.Live ? "● surcharge LIVE" : "● dans la scène";
        panel.Children.Add(new TextBlock { Text = $"{legend}   ◯ non utilisé", FontSize = 11, Foreground = Tokens.Brush(Tokens.Secondary), Margin = new Thickness(0, 4, 0, 0) });
        panel.Children.Add(new TextBlock { Text = "Valeurs en %, bascule 0-255 dans l'onglet Faders.", FontSize = 11, Foreground = Tokens.Brush(Tokens.Secondary), TextWrapping = TextWrapping.Wrap });
        return panel;
    }

    private static Grid ThreeColumns(Control first, Control second, Control third)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*"), Margin = new Thickness(12, 12, 12, 8), ColumnSpacing = 24 };
        grid.Children.Add(first);
        grid.Children.Add(WithColumn(second, 1));
        grid.Children.Add(WithColumn(third, 2));
        return grid;
    }

    private static T WithColumn<T>(T control, int column)
        where T : Control
    {
        Grid.SetColumn(control, column);
        return control;
    }
}
