using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Luxia.UI.Modules.Control.Sequencing;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>
/// Fenêtre d'édition d'une séquence (maquette 11). Le glisser-déposer de la bibliothèque vers la frise est mené par la fenêtre : un
/// appui sur un élément le prend, l'ombre suit la souris sur la frise, le relâcher l'y pose (aimanté à la grille).
/// </summary>
public partial class SequenceEditorWindow : SequencingWindow
{
    private readonly SequenceTimeline _timeline;
    private LibraryItem? _dragged;

    /// <summary>Crée la fenêtre.</summary>
    public SequenceEditorWindow()
    {
        AvaloniaXamlLoader.Load(this);
        _timeline = this.FindControl<SequenceTimeline>("Timeline")!;
        _timeline.BlockChosen += (_, block) => ViewModel?.Select(block);
        _timeline.BlockMoved += (_, e) => ViewModel?.Move(e.Block, e.Start);
        _timeline.BlockResized += (_, e) => ViewModel?.Resize(e.Block, e.Length);
        _timeline.DeleteRequested += (_, _) => ViewModel?.DeleteBlock();
        _timeline.Snap = bars => ViewModel?.SnapTo(bars) ?? bars;
        this.FindControl<Button>("ZoomIn")!.Click += (_, _) => _timeline.PixelsPerBar = Math.Min(_timeline.PixelsPerBar * 1.25, 240);
        this.FindControl<Button>("ZoomOut")!.Click += (_, _) => _timeline.PixelsPerBar = Math.Max(_timeline.PixelsPerBar / 1.25, 16);

        // Au niveau de la fenêtre (en tunnel, même déjà traités) : le pointeur est capturé par l'élément pris dans la bibliothèque.
        AddHandler(PointerMovedEvent, OnDragMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnDragReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private SequenceEditorViewModel? ViewModel => DataContext as SequenceEditorViewModel;

    private void OnLibraryPressed(object? sender, PointerPressedEventArgs e)
    {
        if ((sender as Avalonia.Controls.Control)?.DataContext is LibraryItem item && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _dragged = item;
            e.Pointer.Capture(sender as IInputElement);
            Cursor = new Cursor(StandardCursorType.DragCopy);
            e.Handled = true;
        }
    }

    private void OnDragMoved(object? sender, PointerEventArgs e)
    {
        if (_dragged is { } item)
        {
            _timeline.ShowGhost(e.GetPosition(_timeline), item.Label);
        }
    }

    private void OnDragReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragged is not { } item)
        {
            return;
        }

        _dragged = null;
        Cursor = Cursor.Default;
        e.Pointer.Capture(null);
        _timeline.ShowGhost(null, string.Empty);
        if (_timeline.HitTest(e.GetPosition(_timeline)) is { } hit && ViewModel is { } vm)
        {
            vm.Message = vm.Drop(item, hit.Row, hit.Start);
        }
    }
}
