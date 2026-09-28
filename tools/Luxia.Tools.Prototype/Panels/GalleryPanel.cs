using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Luxia.UI.Controls;

namespace Luxia.Tools.Prototype.Panels;

/// <summary>
/// Galerie des composants (ERG-005, doc 60 §5) : chaque composant commun dans chacun de ses états, avec son nom.
/// Chaque exemple répond à la souris (il retient ce qu'on lui demande), sans rien partager avec les autres panneaux.
/// C'est aussi la source des captures de référence (<c>LuXia-Prototype --captures</c>).
/// </summary>
internal static class GalleryPanel
{
    public static Control Create()
    {
        var content = new StackPanel { Margin = new Thickness(16, 8, 40, 16), Spacing = 4 };
        content.Children.Add(new TextBlock { Text = "Galerie des composants", FontSize = 20, FontWeight = FontWeight.SemiBold });
        content.Children.Add(new TextBlock
        {
            Text = "Chaque composant dans ses états. Tous répondent à la souris ; rien n'est envoyé ailleurs.",
            Classes = { "secondary" },
        });

        content.Children.Add(Section("Fader"));
        content.Children.Add(Row(
            Sample("Normal", FaderSample(128, selected: false, overridden: false, fill: null)),
            Sample("Sélectionné", FaderSample(200, selected: true, overridden: false, fill: null)),
            Sample("Surcharge live", FaderSample(90, selected: false, overridden: true, fill: null)),
            Sample("Couleur d'émetteur", FaderSample(255, selected: false, overridden: false, fill: Brushes.Red))));

        content.Children.Add(Section("Grille Pan / Tilt"));
        content.Children.Add(Row(
            Sample("Aucune sélection", GridSample([Marker("l1", "Lyre 1", 0.5, 0.5, false)], [], editing: false)),
            Sample("Un appareil", GridSample([Marker("l1", "Lyre 1", 0.3, 0.7, true)], [], editing: false)),
            Sample("Plusieurs, en relatif", GridSample(
                [Marker("l1", "Lyre 1", 0.35, 0.6, true), Marker("l2", "Lyre 2", 0.55, 0.6, true), Marker("l3", "Lyre 3", 0.8, 0.3, false)], [], editing: false))));
        content.Children.Add(Row(
            Sample("Zone interdite", GridSample(
                [Marker("l1", "Lyre 1", 0.3, 0.6, true)],
                [new PanTiltZoneMarker("z1", "Public", new PanTiltRect(0.4, 0.7, 0.05, 0.3), PanTiltZoneKind.Forbidden)], editing: false)),
            Sample("Zone permise (limites)", GridSample(
                [Marker("l1", "Lyre 1", 0.5, 0.5, true)],
                [new PanTiltZoneMarker("z1", "Limites", new PanTiltRect(0.15, 0.85, 0.2, 0.9), PanTiltZoneKind.Allowed)], editing: false)),
            Sample("Édition d'une zone", GridSample(
                [Marker("l1", "Lyre 1", 0.3, 0.6, true)],
                [new PanTiltZoneMarker("z1", "Public", new PanTiltRect(0.4, 0.7, 0.05, 0.3), PanTiltZoneKind.Forbidden)], editing: true, selectedZone: "z1"))));

        content.Children.Add(Section("Sélecteur de couleur"));
        content.Children.Add(Row(
            Sample("Blanc", ColorSample(LightColor.White, [])),
            Sample("Bleu pâle à 50 %", ColorSample(new LightColor(210, 0.45, 0.5), [])),
            Sample("Avec favoris", ColorSample(new LightColor(30, 1, 1), [new(0, 1, 1), new(30, 1, 1), new(120, 1, 1), new(240, 1, 1), new(285, 1, 1)]))));

        content.Children.Add(Section("Pastilles d'état (charte §4.3)"));
        content.Children.Add(Row(
            Sample("Dans la scène", Dot("#3FB950", filled: true)),
            Sample("Surcharge live", Dot("#D29922", filled: true)),
            Sample("Non utilisé", Dot("#6E7681", filled: false)),
            Sample("Sûreté", Dot("#F0883E", filled: true)),
            Sample("Enregistrement", Dot("#F85149", filled: true))));

        content.Children.Add(Section("À venir"));
        content.Children.Add(new TextBlock
        {
            Text = "Molette, bouton de scène, colonne de couche, plan des appareils, bande d'étapes, sélecteur de mode, panneau de propriétés.",
            Classes = { "secondary" },
            TextWrapping = TextWrapping.Wrap,
        });

        return new ScrollViewer { Content = content, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
    }

    private static TextBlock Section(string title) => new() { Text = title, Classes = { "section" } };

    private static WrapPanel Row(params Control[] samples)
    {
        var row = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var sample in samples)
        {
            row.Children.Add(sample);
        }

        return row;
    }

    private static StackPanel Sample(string label, Control control) =>
        new()
        {
            Margin = new Thickness(0, 4, 20, 8),
            Spacing = 4,
            Children = { control, new TextBlock { Text = label, Classes = { "state" } } },
        };

    private static PanTiltMarker Marker(string id, string label, double pan, double tilt, bool selected) =>
        new(id, label, pan, tilt, selected ? Color.Parse("#FFB347") : Colors.Gray, selected);

    private static Fader FaderSample(int value, bool selected, bool overridden, IBrush? fill)
    {
        var fader = new Fader { Width = 36, Height = 150, Value = value, IsSelected = selected, IsOverridden = overridden, Fill = fill, HorizontalAlignment = HorizontalAlignment.Center };
        fader.ValueRequested += (_, request) => fader.Value = Math.Clamp(request.NewValue, 0, 255);
        return fader;
    }

    private static PanTiltGrid GridSample(IReadOnlyList<PanTiltMarker> markers, IReadOnlyList<PanTiltZoneMarker> zones, bool editing, string? selectedZone = null)
    {
        var grid = new PanTiltGrid { Width = 300, Height = 200, Markers = markers, Zones = zones, IsZoneEditing = editing, SelectedZoneId = selectedZone };
        grid.MoveRequested += (_, targets) =>
            grid.Markers = grid.Markers?.Select(m => targets.FirstOrDefault(t => t.Id == m.Id) is { } t ? m with { Pan = t.Pan, Tilt = t.Tilt } : m).ToList();
        grid.ZoneRequested += (_, request) =>
        {
            var list = (grid.Zones ?? []).ToList();
            if (request.Id is null)
            {
                list.Add(new PanTiltZoneMarker("n" + list.Count, "Nouvelle", request.Area, PanTiltZoneKind.Forbidden));
            }
            else
            {
                list = list.Select(z => z.Id == request.Id ? z with { Area = request.Area } : z).ToList();
            }

            grid.Zones = list;
        };
        grid.ZoneSelected += (_, id) => grid.SelectedZoneId = id;
        grid.ZoneDeleteRequested += (_, id) => grid.Zones = (grid.Zones ?? []).Where(z => z.Id != id).ToList();
        return grid;
    }

    private static ColorPicker ColorSample(LightColor color, List<LightColor> favorites)
    {
        var picker = new ColorPicker { Width = 260, Height = 190, SelectedColor = color, Favorites = favorites };
        picker.ColorRequested += (_, requested) => picker.SelectedColor = requested;
        picker.FavoriteAddRequested += (_, _) => picker.Favorites = [.. picker.Favorites ?? [], picker.SelectedColor];
        picker.FavoriteRemoveRequested += (_, index) => picker.Favorites = (picker.Favorites ?? []).Where((_, i) => i != index).ToList();
        return picker;
    }

    private static Ellipse Dot(string color, bool filled)
    {
        var brush = new SolidColorBrush(Color.Parse(color));
        return new Ellipse
        {
            Width = 14,
            Height = 14,
            Fill = filled ? brush : null,
            Stroke = brush,
            StrokeThickness = 2,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(24, 8),
        };
    }
}
