using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>Vue des réglages : les demandes des composants (couleur, visée, zones) partent au modèle de vue.</summary>
public partial class SettingsPanelView : UserControl
{
    /// <summary>Crée la vue.</summary>
    public SettingsPanelView()
    {
        AvaloniaXamlLoader.Load(this);
        var picker = this.FindControl<ColorPicker>("Picker")!;
        picker.ColorRequested += (_, color) => ViewModel?.RequestColor(color);
        picker.FavoriteAddRequested += async (_, _) =>
        {
            if (ViewModel is { } vm)
            {
                await vm.AddColorPaletteAsync().ConfigureAwait(true);
            }
        };
        var grid = this.FindControl<PanTiltGrid>("Grid")!;
        grid.MoveRequested += (_, targets) => ViewModel?.RequestAim(targets);
        grid.ZoneRequested += (_, request) => ViewModel?.RequestZone(request);
        grid.ZoneSelected += (_, id) => ViewModel?.SelectZone(id);
        grid.ZoneDeleteRequested += (_, id) => ViewModel?.DeleteZone(id);
    }

    private SettingsPanelViewModel? ViewModel => DataContext as SettingsPanelViewModel;
}
