using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Dock.Model.Mvvm.Controls;
using Luxia.UI.Modules.Control.Views;

namespace Luxia.UI.Modules.Control.Docking;

/// <summary>
/// Contenu d'un panneau de l'écran Contrôle selon son identifiant, avec son bouton « ? » (F6). Enregistré au niveau de
/// l'application : un panneau détaché vit dans une autre fenêtre et doit y retrouver son contenu. Les vues n'ont pas
/// d'état propre (tout est dans les modèles de vue) : Dock peut les recréer à chaque déplacement.
/// </summary>
public sealed class ControlPanelTemplate : IDataTemplate
{
    /// <inheritdoc />
    public bool Match(object? data) => data is Tool { Id: { } id } && ControlPanels.All.Any(p => p.Id == id);

    /// <inheritdoc />
    public Avalonia.Controls.Control? Build(object? param)
    {
        if (param is not Tool tool)
        {
            return null;
        }

        Avalonia.Controls.Control view = tool.Id switch
        {
            ControlPanels.Columns => new ColumnsPanelView(),
            ControlPanels.Properties => new PropertiesPanelView(),
            ControlPanels.Plan => new PlanPanelView(),
            ControlPanels.Settings => new SettingsPanelView(),
            ControlPanels.Effects => new EffectsPanelView(),
            ControlPanels.Journal => new JournalPanelView(),
            ControlPanels.Looks => new LooksPanelView(),
            ControlPanels.Pilot => new PilotPanelView(),
            ControlPanels.Dimmers => new DimmersPanelView(),
            _ => new TextBlock { Text = ControlPanels.Get(tool.Id).Help, Margin = new Thickness(12, 36) },
        };
        view.DataContext = tool.Context;
        return WithHelp(view, ControlPanels.Get(tool.Id));
    }

    /// <summary>Le contenu d'un panneau avec son bouton « ? » (aide en trois lignes) : aussi dans la fenêtre d'édition.</summary>
    public static Panel WithHelp(Avalonia.Controls.Control content, ControlPanel info)
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
                    MaxWidth = 340,
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
}
