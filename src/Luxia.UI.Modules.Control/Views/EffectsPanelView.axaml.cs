using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>Vue du panneau Effets.</summary>
public partial class EffectsPanelView : UserControl
{
    /// <summary>Crée la vue.</summary>
    public EffectsPanelView() => AvaloniaXamlLoader.Load(this);

    private EffectsPanelViewModel? _subscribed;

    // Seule la vue affichée répond (Dock peut recréer les vues : pas de fenêtre ouverte en double).
    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Subscribe(DataContext as EffectsPanelViewModel);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Subscribe(null);
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (TopLevel.GetTopLevel(this) is not null)
        {
            Subscribe(DataContext as EffectsPanelViewModel);
        }
    }

    private void Subscribe(EffectsPanelViewModel? vm)
    {
        if (_subscribed is not null)
        {
            _subscribed.ThemeEditorRequested -= OnThemeEditorRequested;
        }

        _subscribed = vm;
        if (vm is not null)
        {
            vm.ThemeEditorRequested += OnThemeEditorRequested;
        }
    }

    // Fenêtre modale « Thème de couleurs » (PAL-010) : le résultat est rendu au modèle de vue à la fermeture.
    private async void OnThemeEditorRequested(object? sender, ThemeEditorViewModel editor)
    {
        if (sender is not EffectsPanelViewModel vm || TopLevel.GetTopLevel(this) is not Window owner)
        {
            return;
        }

        await new ThemeEditorWindow { DataContext = editor }.ShowDialog(owner).ConfigureAwait(true);
        vm.CompleteTheme(editor);
    }
}
