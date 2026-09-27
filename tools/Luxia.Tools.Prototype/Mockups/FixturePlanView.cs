using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Luxia.Tools.Prototype.Mockups;

/// <summary>
/// Panneau Plan des appareils (doc 60 §5, E5) : le lieu vu de dessus, qui sert de sélection pour tous les panneaux
/// (rectangle, clic, Ctrl + clic), avec les sélections rapides et enregistrées au-dessus.
/// </summary>
internal static class FixturePlanView
{
    public static Control Create(MockScenario scenario)
    {
        var tools = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 34, 36, 4) };
        foreach (var (label, active) in new[]
        {
            ("Tous", false), ("PAR", scenario.Selected.All(s => s.StartsWith("par", StringComparison.Ordinal))), ("Lyres", scenario.Selected.All(s => s.StartsWith("lyre", StringComparison.Ordinal)) && scenario.Selected.Count > 1),
            ("Barres", false), ("½", false), ("⅓", false), ("¼", false), ("Inverser", false), ("Aucun", false),
        })
        {
            tools.Children.Add(Chip(label, active));
        }

        tools.Children.Add(new Border { Width = 10 });
        tools.Children.Add(new TextBlock { Text = "Sélections :", FontSize = 12, Foreground = Tokens.Brush(Tokens.Secondary), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
        tools.Children.Add(Chip("PAR gauche → droite", false));
        tools.Children.Add(Chip("+ enregistrer", false));

        var plan = new FixturePlan
        {
            Selected = scenario.Selected,
            Preview = scenario.Mode == MockMode.Blind,
            Margin = new Thickness(8, 0, 8, 8),
        };
        var root = new DockPanel();
        DockPanel.SetDock(tools, Avalonia.Controls.Dock.Top);
        root.Children.Add(tools);
        root.Children.Add(plan);
        return root;
    }

    private static Border Chip(string text, bool active) =>
        new()
        {
            Margin = new Thickness(0, 0, 4, 4),
            Padding = new Thickness(8, 3),
            CornerRadius = new CornerRadius(4),
            Background = active ? Tokens.Brush(Tokens.Accent, 0.25) : Tokens.Brush(Tokens.Raised),
            BorderBrush = active ? Tokens.Brush(Tokens.Accent) : Tokens.Brush(Tokens.Border),
            BorderThickness = new Thickness(1),
            Child = new TextBlock { Text = text, FontSize = 12, Foreground = Tokens.Brush(Tokens.Text) },
        };

    /// <summary>Dessin du plan (maquette : pas d'interaction).</summary>
    private sealed class FixturePlan : Control
    {
        public IReadOnlyList<string> Selected { get; init; } = [];

        public bool Preview { get; init; }

        public override void Render(DrawingContext context)
        {
            var bounds = new Rect(Bounds.Size);
            context.FillRectangle(Tokens.Brush(Tokens.Background), bounds, 6);

            // Le lieu à l'échelle, centré.
            var scale = Math.Min((bounds.Width - 40) / MockShow.VenueWidth, (bounds.Height - 62) / MockShow.VenueDepth);
            var venue = new Rect(
                (bounds.Width - (MockShow.VenueWidth * scale)) / 2,
                22,
                MockShow.VenueWidth * scale,
                MockShow.VenueDepth * scale);
            context.FillRectangle(Tokens.Brush(Tokens.Surface), venue, 4);
            context.DrawRectangle(new Pen(Tokens.Brush(Tokens.Border), 1), venue, 4);
            Label(context, "SCÈNE", new Point(venue.Center.X, venue.Top - 16), Tokens.Secondary, 11, center: true);
            Label(context, "PUBLIC", new Point(venue.Center.X, venue.Bottom + 3), Tokens.Secondary, 11, center: true);

            Point At(MockFixture f) => new(venue.X + (f.X * scale), venue.Y + (f.Y * scale));

            // Sélection au rectangle, telle qu'on la tracerait à la souris.
            var chosen = MockShow.Fixtures.Where(f => Selected.Contains(f.Id)).Select(At).ToList();
            if (chosen.Count > 1)
            {
                var r = new Rect(
                    new Point(chosen.Min(p => p.X) - 22, chosen.Min(p => p.Y) - 22),
                    new Point(chosen.Max(p => p.X) + 60, chosen.Max(p => p.Y) + 22));
                context.FillRectangle(Tokens.Brush(Tokens.Accent, 0.08), r, 4);
                context.DrawRectangle(new Pen(Tokens.Brush(Tokens.Accent), 1, new DashStyle([4, 3], 0)), r, 4);
            }

            foreach (var fixture in MockShow.Fixtures)
            {
                var p = At(fixture);
                var selected = Selected.Contains(fixture.Id);
                var color = Preview && fixture.Id.StartsWith("lyre", StringComparison.Ordinal) ? Color.Parse("#58A6FF") : fixture.Output;
                var fill = Tokens.Brush(color, color == Color.Parse("#30363D") ? 1 : 0.9);
                var pen = new Pen(Tokens.Brush(selected ? Tokens.Accent : Tokens.Border), selected ? 3 : 1);
                switch (fixture.Kind)
                {
                    case "Barre":
                        context.DrawRectangle(fill, pen, new Rect(p.X - 26, p.Y - 6, 52, 12), 3, 3);
                        break;
                    case "Lyre":
                        context.DrawEllipse(Tokens.Brush(Tokens.Raised), pen, p, 13, 13);
                        context.DrawEllipse(fill, null, p, 7, 7);
                        break;
                    case "Effet":
                    case "Fumée":
                        context.DrawRectangle(fill, pen, new Rect(p.X - 11, p.Y - 8, 22, 16), 5, 5);
                        break;
                    default:
                        context.DrawEllipse(fill, pen, p, 10, 10);
                        break;
                }

                // Nom à droite du symbole (sous la barre) : les appareils alignés en colonne ne se chevauchent pas.
                if (fixture.Kind == "Barre")
                {
                    Label(context, fixture.Name, new Point(p.X, p.Y + 8), selected ? Tokens.Text : Tokens.Secondary, 11, center: true, bold: selected);
                }
                else
                {
                    Label(context, fixture.Name, new Point(p.X + 16, p.Y - 7), selected ? Tokens.Text : Tokens.Secondary, 11, bold: selected);
                }
            }

            if (Preview)
            {
                var text = Text("APERÇU 👁  la sortie ne change pas", 11, Tokens.Background, bold: true);
                var badge = new Rect(bounds.Right - text.Width - 22, 4, text.Width + 14, text.Height + 6);
                context.FillRectangle(Tokens.Brush(Tokens.Blind), badge, 4);
                context.DrawText(text, new Point(badge.X + 7, badge.Y + 3));
            }

            var hint = string.Create(CultureInfo.CurrentCulture, $"{Selected.Count} sélectionné(s) · glisser : rectangle · Ctrl + clic : ajouter / retirer");
            Label(context, hint, new Point(8, bounds.Bottom - 16), Tokens.Secondary, 11);
        }

        private static void Label(DrawingContext context, string value, Point at, Color color, double size, bool center = false, bool bold = false)
        {
            var text = Text(value, size, color, bold);
            context.DrawText(text, center ? new Point(at.X - (text.Width / 2), at.Y) : at);
        }

        private static FormattedText Text(string value, double size, Color color, bool bold = false) =>
            new(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(FontFamily.Default, FontStyle.Normal, bold ? FontWeight.SemiBold : FontWeight.Normal), size, Tokens.Brush(color));
    }
}
