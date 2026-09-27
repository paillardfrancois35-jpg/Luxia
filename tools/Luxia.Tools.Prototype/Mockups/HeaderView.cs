using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace Luxia.Tools.Prototype.Mockups;

/// <summary>
/// En-tête toujours visible (doc 60 §4.1, §4.3, GEN-082) : projet, sélecteur de mode, affectation, verrou, voyants,
/// Grand Master et blackout ; dessous, le bandeau qui rappelle le mode en toutes lettres.
/// </summary>
internal static class HeaderView
{
    public static Control Create(MockScenario scenario)
    {
        var bar = new DockPanel { Height = 52, Background = Tokens.Brush(Tokens.Surface), LastChildFill = false };

        var left = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(12, 0), VerticalAlignment = VerticalAlignment.Center };
        left.Children.Add(new TextBlock { Text = "LuXia", FontSize = 20, FontWeight = FontWeight.Bold, Foreground = Tokens.Brush(Tokens.Accent), VerticalAlignment = VerticalAlignment.Center });
        left.Children.Add(Chip("Show de travail ▾", Tokens.Raised, Tokens.Text, "Projet ouvert"));
        left.Children.Add(Chip("Disposition : Contrôle ▾", Tokens.Raised, Tokens.Text, null));
        left.Children.Add(new Border { Width = 16 });
        left.Children.Add(ModeSelector(scenario.Mode));
        left.Children.Add(new Border { Width = 8 });
        left.Children.Add(Chip("⌨ Affecter", Tokens.Raised, Tokens.Text, "Affecter une touche, un pad ou un fader MIDI (§4.7)"));
        left.Children.Add(Chip("🔒 Verrou soirée", Tokens.Raised, Tokens.Secondary, "Bloque l'édition et l'installation (§4.6)"));
        DockPanel.SetDock(left, Avalonia.Controls.Dock.Left);
        bar.Children.Add(left);

        var right = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, Margin = new Thickness(12, 0), VerticalAlignment = VerticalAlignment.Center };
        right.Children.Add(Light("Sortie", Tokens.Edit, "Arduino COM5 · 40 Hz"));
        right.Children.Add(Light("MIDI", Tokens.Edit, "APC mini MK2"));
        right.Children.Add(Light("Sûreté", Tokens.Secondary, "rien en cours"));
        right.Children.Add(new TextBlock { Text = "CPU 3 %", Foreground = Tokens.Brush(Tokens.Secondary), VerticalAlignment = VerticalAlignment.Center, FontSize = 12 });
        right.Children.Add(GrandMaster());
        right.Children.Add(new Border
        {
            Background = Tokens.Brush(Tokens.Danger, 0.15),
            BorderBrush = Tokens.Brush(Tokens.Danger),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14, 6),
            Child = new TextBlock { Text = "BLACKOUT", FontWeight = FontWeight.Bold, Foreground = Tokens.Brush(Tokens.Danger) },
        });
        DockPanel.SetDock(right, Avalonia.Controls.Dock.Right);
        bar.Children.Add(right);

        var root = new StackPanel();
        root.Children.Add(bar);
        root.Children.Add(ModeBand(scenario));
        return root;
    }

    private static StackPanel ModeSelector(MockMode mode)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var (m, label) in new[] { (MockMode.Live, "LIVE"), (MockMode.Edit, "ÉDITION"), (MockMode.Blind, "👁 AVEUGLE") })
        {
            var active = m == mode;
            var color = Tokens.ModeColor(m);
            panel.Children.Add(new Border
            {
                Background = active ? Tokens.Brush(color) : Tokens.Brush(Tokens.Raised),
                BorderBrush = Tokens.Brush(active ? color : Tokens.Border),
                BorderThickness = new Thickness(1),
                CornerRadius = m == MockMode.Live ? new CornerRadius(6, 0, 0, 6) : m == MockMode.Blind ? new CornerRadius(0, 6, 6, 0) : new CornerRadius(0),
                Padding = new Thickness(14, 6),
                Child = new TextBlock
                {
                    Text = label,
                    FontWeight = FontWeight.Bold,
                    Foreground = active ? Tokens.Brush(Tokens.Background) : Tokens.Brush(Tokens.Secondary),
                },
            });
        }

        return panel;
    }

    private static Border ModeBand(MockScenario scenario)
    {
        var color = Tokens.ModeColor(scenario.Mode);
        var (title, text, action) = scenario.Mode switch
        {
            MockMode.Edit => ("ÉDITION", $"Vos réglages s'écrivent tout de suite dans « {scenario.EditScene} », étape {scenario.EditStep + 1}, et sortent. Ctrl+Z pour annuler.", "Revenir en LIVE"),
            MockMode.Blind => ("AVEUGLE 👁", $"Vos réglages s'écrivent dans « {scenario.EditScene} », étape {scenario.EditStep + 1}, sans changer la sortie. Le plan montre l'aperçu.", "Revenir en LIVE"),
            _ => ("LIVE", "Vos réglages sont des surcharges temporaires par-dessus les scènes : rien n'est enregistré. Ici : Lyre 1, Lyre 2 (couleur).", "Libérer tout"),
        };
        return new Border
        {
            Background = Tokens.Brush(color, 0.16),
            BorderBrush = Tokens.Brush(color),
            BorderThickness = new Thickness(0, 2, 0, 1),
            Padding = new Thickness(12, 4),
            Child = new DockPanel
            {
                Children =
                {
                    Docked(new Border
                    {
                        Background = Tokens.Brush(color),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(8, 1),
                        Margin = new Thickness(0, 0, 10, 0),
                        Child = new TextBlock { Text = title, FontWeight = FontWeight.Bold, FontSize = 12, Foreground = Tokens.Brush(Tokens.Background) },
                    }, Avalonia.Controls.Dock.Left),
                    Docked(new Border
                    {
                        BorderBrush = Tokens.Brush(color),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(10, 1),
                        Child = new TextBlock { Text = action, FontSize = 12, Foreground = Tokens.Brush(color) },
                    }, Avalonia.Controls.Dock.Right),
                    new TextBlock { Text = text, FontSize = 13, Foreground = Tokens.Brush(Tokens.Text), VerticalAlignment = VerticalAlignment.Center },
                },
            },
        };
    }

    private static Control Docked(Control control, Avalonia.Controls.Dock dock)
    {
        DockPanel.SetDock(control, dock);
        return control;
    }

    private static Border Chip(string text, Color background, Color foreground, string? tip)
    {
        var chip = new Border
        {
            Background = Tokens.Brush(background),
            BorderBrush = Tokens.Brush(Tokens.Border),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 5),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = text, Foreground = Tokens.Brush(foreground), FontSize = 13 },
        };
        if (tip is not null)
        {
            ToolTip.SetTip(chip, tip);
        }

        return chip;
    }

    private static StackPanel Light(string label, Color color, string detail) =>
        new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new Ellipse { Width = 9, Height = 9, Fill = Tokens.Brush(color), VerticalAlignment = VerticalAlignment.Center },
                new TextBlock { Text = label, FontSize = 12, Foreground = Tokens.Brush(Tokens.Text), VerticalAlignment = VerticalAlignment.Center },
                new TextBlock { Text = detail, FontSize = 11, Foreground = Tokens.Brush(Tokens.Secondary), VerticalAlignment = VerticalAlignment.Center },
            },
        };

    private static StackPanel GrandMaster() =>
        new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock { Text = "Grand Master", FontSize = 12, Foreground = Tokens.Brush(Tokens.Secondary), VerticalAlignment = VerticalAlignment.Center },
                new Border
                {
                    Width = 120,
                    Height = 14,
                    CornerRadius = new CornerRadius(7),
                    Background = Tokens.Brush(Tokens.Background),
                    BorderBrush = Tokens.Brush(Tokens.Border),
                    BorderThickness = new Thickness(1),
                    Child = new Border { Width = 102, HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(7), Background = Tokens.Brush(Tokens.Accent) },
                },
                new TextBlock { Text = "85 %", FontSize = 12, FontWeight = FontWeight.SemiBold, Foreground = Tokens.Brush(Tokens.Text), VerticalAlignment = VerticalAlignment.Center },
            },
        };
}
