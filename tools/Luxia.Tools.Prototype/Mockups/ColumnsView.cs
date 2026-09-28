using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Luxia.Tools.Prototype.Mockups;

/// <summary>
/// Panneau Colonnes (doc 60 §2, §5, E2) : une colonne par couche ; bouton de scène en deux zones — la grande
/// joue / arrête, la bande étroite de droite (✎) choisit la scène à éditer. Scène qui joue : fond à sa couleur et
/// progression ; scène en édition : contour à la couleur du mode.
/// </summary>
internal static class ColumnsView
{
    public static Control Create(MockScenario scenario)
    {
        var grid = new Grid { Margin = new Thickness(8, 34, 8, 8) };
        var layers = MockShow.Layers;
        for (var i = 0; i < layers.Count; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            var column = Column(layers[i], scenario);
            Grid.SetColumn(column, i);
            grid.Children.Add(column);
        }

        return new ScrollViewer { Content = grid };
    }

    private static Border Column(MockLayer layer, MockScenario scenario)
    {
        var stack = new StackPanel { Spacing = 6 };
        stack.Children.Add(Header(layer));
        foreach (var scene in layer.Scenes)
        {
            stack.Children.Add(SceneButton(scene, scene.Name == scenario.EditScene, scenario.Mode));
        }

        stack.Children.Add(new Border
        {
            Height = 34,
            CornerRadius = new CornerRadius(6),
            BorderBrush = Tokens.Brush(Tokens.Border),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = layer.Scenes.Count == 0 ? "+ scène  (aucune pour l'instant)" : "+ scène",
                Foreground = Tokens.Brush(Tokens.Secondary),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        });

        return new Border
        {
            Margin = new Thickness(0, 0, 8, 0),
            Background = Tokens.Brush(Tokens.Surface),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(6),
            Child = stack,
        };
    }

    private static Border Header(MockLayer layer)
    {
        var playing = layer.Scenes.Any(s => s.Progress is not null);
        var controls = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };
        foreach (var glyph in new[] { "⏸", "◀", "▶", "■" })
        {
            controls.Children.Add(new Border
            {
                Width = 30,
                Height = 24,
                CornerRadius = new CornerRadius(4),
                Background = Tokens.Brush(Tokens.Raised),
                Child = new TextBlock
                {
                    Text = glyph,
                    FontSize = 12,
                    Foreground = Tokens.Brush(glyph == "■" && playing ? Tokens.Danger : Tokens.Text),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            });
        }

        return new Border
        {
            BorderBrush = Tokens.Brush(layer.Color),
            BorderThickness = new Thickness(0, 3, 0, 0),
            Padding = new Thickness(4, 6, 4, 4),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock { Text = $"{layer.Icon}  {layer.Name}", FontWeight = FontWeight.SemiBold, FontSize = 14, Foreground = Tokens.Brush(Tokens.Text), HorizontalAlignment = HorizontalAlignment.Center },
                    controls,
                    MasterBar(layer),
                },
            },
        };
    }

    private static Grid MasterBar(MockLayer layer)
    {
        var bar = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), Margin = new Thickness(0, 2, 0, 0) };
        var label = new TextBlock { Text = "Master", FontSize = 11, Foreground = Tokens.Brush(Tokens.Secondary), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) };
        var track = new Border
        {
            Height = 8,
            CornerRadius = new CornerRadius(4),
            Background = Tokens.Brush(Tokens.Background),
            Child = new Border { CornerRadius = new CornerRadius(4), Background = Tokens.Brush(layer.Color, 0.9), HorizontalAlignment = HorizontalAlignment.Left },
            VerticalAlignment = VerticalAlignment.Center,
        };
        track.SizeChanged += (_, e) => ((Border)track.Child!).Width = e.NewSize.Width * layer.Master / 100.0;
        var value = new TextBlock { Text = $"{layer.Master} %", FontSize = 11, Foreground = Tokens.Brush(Tokens.Text), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) };
        Grid.SetColumn(track, 1);
        Grid.SetColumn(value, 2);
        bar.Children.Add(label);
        bar.Children.Add(track);
        bar.Children.Add(value);
        return bar;
    }

    private static Border SceneButton(MockScene scene, bool editTarget, MockMode mode)
    {
        var playing = scene.Progress is not null;
        var editColor = mode == MockMode.Live ? Tokens.Accent : Tokens.ModeColor(mode);
        var textColor = playing && Luminance(scene.Color) > 0.6 ? Tokens.Background : Tokens.Text;

        var name = new TextBlock
        {
            Text = scene.Name,
            FontSize = 13,
            FontWeight = playing ? FontWeight.SemiBold : FontWeight.Normal,
            Foreground = Tokens.Brush(textColor),
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 2,
        };
        var info = new TextBlock
        {
            Text = playing ? (scene.StepInfo ?? "joue") : "",
            FontSize = 11,
            Foreground = Tokens.Brush(textColor, 0.8),
        };
        var progress = new Border
        {
            Height = 3,
            CornerRadius = new CornerRadius(2),
            Background = Tokens.Brush(textColor, 0.85),
            HorizontalAlignment = HorizontalAlignment.Left,
            IsVisible = playing && scene.Progress < 1,
        };

        var playArea = new Border
        {
            Background = playing ? Tokens.Brush(scene.Color, 0.85) : Tokens.Brush(Tokens.Raised),
            BorderBrush = Tokens.Brush(scene.Color),
            BorderThickness = new Thickness(4, 0, 0, 0),
            CornerRadius = new CornerRadius(6, 0, 0, 6),
            Padding = new Thickness(8, 6, 6, 4),
            MinHeight = 46,
            Child = new DockPanel
            {
                Children =
                {
                    Bottom(progress),
                    Bottom(info),
                    name,
                },
            },
        };
        playArea.SizeChanged += (_, e) => progress.Width = Math.Max(e.NewSize.Width - 18, 0) * (scene.Progress ?? 0);
        ToolTip.SetTip(playArea, "Clic : lancer / arrêter · clic droit : Renommer, Dupliquer, Couleur…, Supprimer");

        var editBand = new Border
        {
            Width = 22,
            Background = editTarget ? Tokens.Brush(editColor, 0.9) : Tokens.Brush(Tokens.Surface),
            CornerRadius = new CornerRadius(0, 6, 6, 0),
            Child = new TextBlock
            {
                Text = "✎",
                FontSize = 12,
                Foreground = editTarget ? Tokens.Brush(Tokens.Background) : Tokens.Brush(Tokens.Secondary),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        ToolTip.SetTip(editBand, "Choisir cette scène pour l'éditer");

        var content = new DockPanel();
        DockPanel.SetDock(editBand, Avalonia.Controls.Dock.Right);
        content.Children.Add(editBand);
        content.Children.Add(playArea);
        return new Border
        {
            CornerRadius = new CornerRadius(7),
            BorderBrush = editTarget ? Tokens.Brush(editColor) : null,
            BorderThickness = new Thickness(editTarget ? 2 : 0),
            Padding = new Thickness(editTarget ? 0 : 2),
            Child = content,
        };
    }

    private static Control Bottom(Control control)
    {
        DockPanel.SetDock(control, Avalonia.Controls.Dock.Bottom);
        return control;
    }

    private static double Luminance(Color c) => ((0.299 * c.R) + (0.587 * c.G) + (0.114 * c.B)) / 255;
}
