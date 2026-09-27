using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace Luxia.Tools.Prototype.Mockups;

/// <summary>
/// Panneau Propriétés (doc 60 §5) de la scène choisie pour l'édition : sections repliables, étiquettes alignées,
/// **bande d'étapes** (durées dessinées : fondu hachuré, puis maintien), contenu de l'étape avec ses pastilles.
/// Tout s'enregistre dès la saisie (§4.2) : aucun bouton « Enregistrer ».
/// </summary>
internal static class PropertiesView
{
    public static Control Create(MockScenario scenario)
    {
        var modeColor = scenario.Mode == MockMode.Live ? Tokens.Accent : Tokens.ModeColor(scenario.Mode);
        var scene = MockShow.Find(scenario.EditScene);
        var steps = MockShow.Steps(scenario.EditScene);
        var stack = new StackPanel { Margin = new Thickness(12, 34, 12, 12), Spacing = 10 };

        stack.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children =
            {
                new Border { Width = 22, Height = 22, CornerRadius = new CornerRadius(4), Background = Tokens.Brush(scene.Color) },
                new TextBlock { Text = scene.Name, FontSize = 17, FontWeight = FontWeight.SemiBold, Foreground = Tokens.Brush(Tokens.Text), VerticalAlignment = VerticalAlignment.Center },
            },
        });
        stack.Children.Add(new TextBlock
        {
            Text = MockShow.Summary(scene.Name),
            FontSize = 12,
            Foreground = Tokens.Brush(Tokens.Secondary),
        });

        stack.Children.Add(Section("Lecture", Rows(
            ("Vitesse", "100 %  ·  tempo libre"),
            ("Enchaînement", "en boucle"),
            ("À la fin", "reste sur la dernière étape"),
            ("Fondu d'entrée", "0,5 s"))));

        stack.Children.Add(Section("Étapes", StepStrip(steps, scenario.EditStep, modeColor)));
        stack.Children.Add(Section($"Étape {scenario.EditStep + 1} · {steps[scenario.EditStep].Label}", StepContent(scenario, modeColor)));
        stack.Children.Add(Section("Notes", new TextBlock
        {
            Text = MockShow.Notes(scene.Name),
            TextWrapping = TextWrapping.Wrap,
            Foreground = Tokens.Brush(Tokens.Secondary),
            FontSize = 12,
        }));
        return new ScrollViewer { Content = stack };
    }

    private static Border Section(string title, Control content) =>
        new()
        {
            Background = Tokens.Brush(Tokens.Surface),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "▾  " + title, FontWeight = FontWeight.SemiBold, FontSize = 13, Foreground = Tokens.Brush(Tokens.Text) },
                    content,
                },
            },
        };

    private static Grid Rows(params (string Label, string Value)[] rows)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("110,*"), RowSpacing = 6 };
        for (var i = 0; i < rows.Length; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var label = new TextBlock { Text = rows[i].Label, FontSize = 12, Foreground = Tokens.Brush(Tokens.Secondary), VerticalAlignment = VerticalAlignment.Center };
            var value = new Border
            {
                Background = Tokens.Brush(Tokens.Background),
                BorderBrush = Tokens.Brush(Tokens.Border),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 3),
                Child = new TextBlock { Text = rows[i].Value, FontSize = 12, Foreground = Tokens.Brush(Tokens.Text) },
            };
            Grid.SetRow(label, i);
            Grid.SetRow(value, i);
            Grid.SetColumn(value, 1);
            grid.Children.Add(label);
            grid.Children.Add(value);
        }

        return grid;
    }

    // Une case par étape, largeur proportionnelle à sa durée ; partie hachurée = fondu, pleine = maintien.
    private static StackPanel StepStrip(IReadOnlyList<(string Label, Color Color, double Fade, double Hold)> steps, int selected, Color modeColor)
    {
        var total = steps.Sum(s => s.Fade + s.Hold);
        var strip = new Grid();
        var column = 0;
        foreach (var (label, color, fade, hold) in steps)
        {
            strip.ColumnDefinitions.Add(new ColumnDefinition((fade + hold) / total, GridUnitType.Star));
            var fadePart = new Border
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                    GradientStops = { new GradientStop(Color.FromArgb(40, color.R, color.G, color.B), 0), new GradientStop(color, 1) },
                },
            };
            var holdPart = new Border { Background = Tokens.Brush(color) };
            var bar = new Grid { ColumnDefinitions = new ColumnDefinitions($"{fade.ToString(CultureInfo.InvariantCulture)}*,{hold.ToString(CultureInfo.InvariantCulture)}*"), Height = 16 };
            Grid.SetColumn(holdPart, 1);
            bar.Children.Add(fadePart);
            bar.Children.Add(holdPart);
            var isSelected = column == selected;
            var cell = new Border
            {
                Margin = new Thickness(0, 0, 3, 0),
                Padding = new Thickness(4),
                CornerRadius = new CornerRadius(5),
                Background = Tokens.Brush(Tokens.Raised),
                BorderBrush = isSelected ? Tokens.Brush(modeColor) : Tokens.Brush(Tokens.Border),
                BorderThickness = new Thickness(isSelected ? 2 : 1),
                Child = new StackPanel
                {
                    Spacing = 3,
                    Children =
                    {
                        new TextBlock { Text = $"{column + 1} · {label}", FontSize = 11, FontWeight = isSelected ? FontWeight.SemiBold : FontWeight.Normal, Foreground = Tokens.Brush(Tokens.Text) },
                        bar,
                        new TextBlock { Text = string.Create(CultureInfo.CurrentCulture, $"{fade:0.#} + {hold:0.#} s"), FontSize = 10, Foreground = Tokens.Brush(Tokens.Secondary) },
                    },
                },
            };
            Grid.SetColumn(cell, column++);
            strip.Children.Add(cell);
        }

        return new StackPanel
        {
            Spacing = 6,
            Children =
            {
                strip,
                new TextBlock
                {
                    Text = "Glisser pour réordonner · Maj + clic : plusieurs · clic droit : Dupliquer, Insérer, Supprimer",
                    FontSize = 11,
                    Foreground = Tokens.Brush(Tokens.Secondary),
                },
            },
        };
    }

    private static StackPanel StepContent(MockScenario scenario, Color modeColor)
    {
        var dot = scenario.Mode == MockMode.Live ? Tokens.Edit : modeColor;
        var rows = new StackPanel { Spacing = 5 };
        foreach (var (who, what, used) in MockShow.StepContent(scenario.EditScene, scenario.EditStep))
        {
            rows.Children.Add(new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new Ellipse { Width = 10, Height = 10, Fill = used ? Tokens.Brush(dot) : null, Stroke = Tokens.Brush(used ? dot : Tokens.Secondary), StrokeThickness = 1.5, VerticalAlignment = VerticalAlignment.Center },
                    new TextBlock { Text = who, Width = 100, FontSize = 12, Foreground = Tokens.Brush(Tokens.Text) },
                    new TextBlock { Text = what, FontSize = 12, Foreground = Tokens.Brush(used ? Tokens.Text : Tokens.Secondary) },
                },
            });
        }

        rows.Children.Add(new TextBlock
        {
            Text = "● dans la scène   ◯ non utilisé (l'appareil garde ce que donnent les autres couches)",
            FontSize = 11,
            Foreground = Tokens.Brush(Tokens.Secondary),
            Margin = new Thickness(0, 4, 0, 0),
        });
        return rows;
    }
}
