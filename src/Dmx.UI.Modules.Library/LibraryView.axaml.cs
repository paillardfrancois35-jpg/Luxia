using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Dmx.UI.Modules.Library;

/// <summary>Écran « Bibliothèque ».</summary>
public partial class LibraryView : UserControl
{
    private SlotViewModel? _dragged;

    /// <summary>Crée la vue.</summary>
    public LibraryView()
    {
        InitializeComponent();

        // Glisser-déposer des positions d'un mode (BIB-021) : appui sur une ligne, relâche sur une autre.
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
    }

    private static SlotViewModel? SlotUnder(object? source) =>
        (source as Control)?.GetSelfAndVisualAncestors().OfType<ListBoxItem>().FirstOrDefault()?.DataContext as SlotViewModel;

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e) => _dragged = SlotUnder(e.Source);

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var target = SlotUnder(this.InputHitTest(e.GetPosition(this)) as Control);
        if (_dragged is { } from && target is { } to && from != to
            && DataContext is LibraryViewModel { Editor: { } editor })
        {
            editor.MoveSlot(from.Position - 1, to.Position - 1);
        }

        _dragged = null;
    }

    private void OnBoundaryMoved(object? sender, (int Index, int NewMax) e)
    {
        if (DataContext is LibraryViewModel { Editor.SelectedChannel: { } channel })
        {
            channel.MoveBoundary(e.Index, e.NewMax);
        }
    }
}
