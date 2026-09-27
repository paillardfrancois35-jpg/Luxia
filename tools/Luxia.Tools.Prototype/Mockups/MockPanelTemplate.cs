using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Luxia.Tools.Prototype.Docking;
using Luxia.Tools.Prototype.Panels;

namespace Luxia.Tools.Prototype.Mockups;

/// <summary>Contenu des panneaux de maquette, selon leur identifiant et la maquette montrée.</summary>
internal sealed class MockPanelTemplate : IDataTemplate
{
    /// <inheritdoc />
    public bool Match(object? data) => data is MockPanel;

    /// <inheritdoc />
    public Control? Build(object? param)
    {
        if (param is not MockPanel panel)
        {
            return null;
        }

        Control content = panel.Id switch
        {
            PanelCatalog.Columns => ColumnsView.Create(panel.Scenario),
            PanelCatalog.Properties => PropertiesView.Create(panel.Scenario),
            PanelCatalog.FixturePlan => FixturePlanView.Create(panel.Scenario),
            PanelCatalog.Settings => SettingsView.Create(panel.Scenario),
            PanelCatalog.Log => new ListBox
            {
                ItemsSource = MockShow.Log,
                Margin = new Thickness(4, 32, 4, 4),
                FontFamily = new FontFamily("Consolas, Cascadia Mono, monospace"),
                FontSize = 12,
                Background = Brushes.Transparent,
            },
            _ => new TextBlock { Text = panel.Title ?? "" },
        };
        return PanelViews.WithHelp(content, PanelCatalog.Get(panel.Id));
    }
}
