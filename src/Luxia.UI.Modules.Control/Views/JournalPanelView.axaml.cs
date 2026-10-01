using Avalonia.Input.Platform;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>Vue du journal.</summary>
public partial class JournalPanelView : UserControl
{
    /// <summary>Crée la vue.</summary>
    public JournalPanelView()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<ListBox>("LinesList")!.ContextMenu!.Items.OfType<MenuItem>().First().Click += async (_, _) =>
        {
            if (DataContext is JournalPanelViewModel journal && TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                // Du plus ancien au plus récent : l'ordre de lecture d'un journal.
                await clipboard.SetTextAsync(string.Join(Environment.NewLine, journal.Lines.Reverse()));
            }
        };
    }
}
