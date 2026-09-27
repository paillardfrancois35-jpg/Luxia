using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Luxia.UI.Controls;

namespace Luxia.Tools.Prototype.Panels;

/// <summary>Panneau Couleur : sélecteur, aperçu et valeurs émises.</summary>
internal static class ColorPanel
{
    public static Control Create(DemoState state)
    {
        var picker = new ColorPicker { MinWidth = 220, MinHeight = 160 };
        picker.ColorRequested += (_, color) => state.SetColor(color);
        picker.FavoriteAddRequested += (_, _) => state.AddFavorite();
        picker.FavoriteRemoveRequested += (_, index) => state.RemoveFavorite(index);

        var preview = new Border { Width = 56, Height = 56, CornerRadius = new CornerRadius(28), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
        var values = new TextBlock { Classes = { "secondary" }, TextWrapping = TextWrapping.Wrap };
        var side = new StackPanel
        {
            Width = 150,
            Spacing = 8,
            Margin = new Thickness(12, 28, 0, 0),
            Children =
            {
                preview,
                values,
                new TextBlock
                {
                    Text = "Clic droit sur un favori : le retirer.",
                    Classes = { "secondary" },
                    TextWrapping = TextWrapping.Wrap,
                },
            },
        };
        var root = new DockPanel { Margin = new Thickness(8) };
        DockPanel.SetDock(side, Avalonia.Controls.Dock.Right);
        root.Children.Add(side);
        root.Children.Add(picker);

        PanelViews.Follow(root, state, () =>
        {
            picker.SelectedColor = state.Color;
            picker.Favorites = state.Favorites.ToList();
            preview.Background = new SolidColorBrush(state.Color.ToColor());
            var (r, g, b) = state.Color.ToRgb();
            values.Text = string.Create(
                CultureInfo.CurrentCulture,
                $"Rouge {r * 100 / 255} %\nVert {g * 100 / 255} %\nBleu {b * 100 / 255} %\n\nTeinte {state.Color.Hue:0}°\nSaturation {state.Color.Saturation * 100:0} %\nIntensité {state.Color.Brightness * 100:0} %");
        });
        return root;
    }
}
