using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Luxia.UI.Controls;

namespace Luxia.Tools.Prototype.Panels;

/// <summary>Panneau Position : sélection des lyres (en attendant le plan), grille Pan / Tilt, zones.</summary>
internal static class PositionPanel
{
    public static Control Create(DemoState state)
    {
        var grid = new PanTiltGrid { MinHeight = 180, Margin = new Thickness(0, 4, 0, 0) };
        grid.MoveRequested += (_, targets) => state.Aim(targets);
        grid.ZoneRequested += (_, request) => state.ChangeZone(request);
        grid.ZoneSelected += (_, id) => state.SelectZone(id);
        grid.ZoneDeleteRequested += (_, id) => state.DeleteZone(id);

        var fixtures = new WrapPanel { Orientation = Orientation.Horizontal };
        var checks = state.Fixtures.Select(f => PanelViews.Check(f.Label, f.IsSelected, v => state.SetSelected(f.Id, v))).ToList();
        foreach (var check in checks)
        {
            fixtures.Children.Add(check);
        }

        var kind = new ComboBox
        {
            ItemsSource = new[] { "Zone interdite", "Zone permise (limites)" },
            SelectedIndex = 0,
            Width = 190,
            Margin = new Thickness(0, 0, 8, 0),
        };
        kind.SelectionChanged += (_, _) => state.NewZoneKind = kind.SelectedIndex == 1 ? PanTiltZoneKind.Allowed : PanTiltZoneKind.Forbidden;
        var editing = PanelViews.Toggle("Éditer les zones", state.IsZoneEditing, state.SetZoneEditing);
        var hint = new TextBlock { Classes = { "secondary" }, VerticalAlignment = VerticalAlignment.Center, TextWrapping = Avalonia.Media.TextWrapping.Wrap };

        var tools = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 4, 32, 0),
            Children = { fixtures, editing, kind },
        };
        var root = new DockPanel { Margin = new Thickness(8) };
        DockPanel.SetDock(tools, Avalonia.Controls.Dock.Top);
        DockPanel.SetDock(hint, Avalonia.Controls.Dock.Bottom);
        root.Children.Add(tools);
        root.Children.Add(hint);
        root.Children.Add(grid);

        PanelViews.Follow(root, state, () =>
        {
            grid.Markers = state.Markers();
            grid.Zones = state.Zones;
            grid.IsZoneEditing = state.IsZoneEditing;
            grid.SelectedZoneId = state.SelectedZoneId;
            editing.IsChecked = state.IsZoneEditing;
            kind.IsEnabled = state.IsZoneEditing;
            for (var i = 0; i < checks.Count; i++)
            {
                checks[i].IsChecked = state.Fixtures[i].IsSelected;
            }

            hint.Text = state.IsZoneEditing
                ? "Glisser dans le vide : nouvelle zone · glisser une zone : la déplacer · poignées : la redimensionner · Suppr : la retirer."
                : "Clic : la sélection vient sous le curseur · Maj + glisser : réglage fin · molette : Tilt (Maj : Pan, Ctrl : ×10) · flèches : pas fin.";
        });
        return root;
    }
}
