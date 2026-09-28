using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Luxia.Tools.Prototype.Docking;

namespace Luxia.Tools.Prototype.Panels;

/// <summary>Fabrique des contenus de panneaux, chacun avec son bouton « ? » (F6).</summary>
internal static class PanelViews
{
    public static Control Create(string? id, DemoState state)
    {
        var info = PanelCatalog.Get(id);
        Control content = info.Id switch
        {
            PanelCatalog.Position => PositionPanel.Create(state),
            PanelCatalog.Color => ColorPanel.Create(state),
            PanelCatalog.Log => LogPanel.Create(state),
            PanelCatalog.Gallery => GalleryPanel.Create(),
            PanelCatalog.Metrics => MetricsPanel.Create(state),
            _ => Placeholder(info),
        };
        return WithHelp(content, info);
    }

    /// <summary>Tient à jour une vue tant qu'elle est affichée (Dock la recrée à chaque déplacement : pas de fuite).</summary>
    public static void Follow(Control view, DemoState state, Action refresh)
    {
        void OnChanged(object? sender, EventArgs e) => refresh();
        view.AttachedToVisualTree += (_, _) =>
        {
            state.Changed += OnChanged;
            refresh();
        };
        view.DetachedFromVisualTree += (_, _) => state.Changed -= OnChanged;
    }

    /// <summary>Ajoute le bouton « ? » (F6) en haut à droite d'un contenu de panneau.</summary>
    public static Panel WithHelp(Control content, PanelInfo info)
    {
        var help = new Button
        {
            Content = "?",
            Width = 24,
            Height = 24,
            Padding = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 4, 6, 0),
            CornerRadius = new CornerRadius(12),
            Flyout = new Flyout
            {
                Content = new StackPanel
                {
                    MaxWidth = 320,
                    Spacing = 6,
                    Children =
                    {
                        new TextBlock { Text = info.Title, FontWeight = FontWeight.SemiBold },
                        new TextBlock { Text = info.Help, TextWrapping = TextWrapping.Wrap },
                    },
                },
            },
        };
        ToolTip.SetTip(help, "À quoi sert ce panneau ?");
        return new Panel { Children = { content, help } };
    }

    private static Border Placeholder(PanelInfo info) =>
        new Border
        {
            Background = new SolidColorBrush(Color.Parse("#0D1117")),
            Child = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                MaxWidth = 420,
                Children =
                {
                    new TextBlock { Text = info.Title, FontSize = 18, FontWeight = FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Center },
                    new TextBlock { Text = info.Help, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Classes = { "secondary" } },
                    new TextBlock { Text = "Panneau prévu : son contenu viendra avec les maquettes.", TextAlignment = TextAlignment.Center, Classes = { "secondary" }, FontStyle = FontStyle.Italic },
                },
            },
        };

    /// <summary>Case à cocher compacte.</summary>
    public static CheckBox Check(string text, bool value, Action<bool> changed)
    {
        var check = new CheckBox { Content = text, IsChecked = value, Margin = new Thickness(0, 0, 12, 0) };
        check.IsCheckedChanged += (_, _) => changed(check.IsChecked == true);
        return check;
    }

    /// <summary>Bouton bascule compact.</summary>
    public static ToggleButton Toggle(string text, bool value, Action<bool> changed)
    {
        var toggle = new ToggleButton { Content = text, IsChecked = value, Margin = new Thickness(0, 0, 8, 0) };
        toggle.IsCheckedChanged += (_, _) => changed(toggle.IsChecked == true);
        return toggle;
    }
}
