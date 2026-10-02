using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Luxia.UI.Modules.Control.Sequencing;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>
/// Base des fenêtres d'édition d'une séquence et d'un show (P8) : comme la fenêtre d'édition des scènes (ERG-033), non bloquante,
/// une seule par modèle de vue, cachée (et non détruite) à la fermeture ; Ctrl+Z / Ctrl+Y hors des champs de texte ; la croix ne perd
/// jamais un brouillon par mégarde ; rafraîchie vingt fois par seconde (essai en cours).
/// </summary>
public class SequencingWindow : Window
{
    private static readonly ConditionalWeakTable<IDraftEditor, SequencingWindow> Windows = [];
    private readonly DispatcherTimer _timer;
    private IDraftEditor? _editor;

    /// <summary>Crée la fenêtre.</summary>
    public SequencingWindow()
    {
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background, (_, _) => _editor?.Refresh());
        Closing += OnClosing;
    }

    /// <summary>Montre la fenêtre de ce modèle de vue (créée au premier appel), au premier plan.</summary>
    public static void Present(IDraftEditor editor, Func<SequencingWindow> create, Window? owner)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(create);
        if (!Windows.TryGetValue(editor, out var window))
        {
            window = create();
            window.Attach(editor);
            Windows.Add(editor, window);
        }

        window.PresentCore(owner);
    }

    /// <summary>Fenêtre déjà créée pour ce modèle de vue, ou nulle (outil de captures).</summary>
    public static SequencingWindow? Of(IDraftEditor editor) => Windows.TryGetValue(editor, out var window) ? window : null;

    /// <summary>Lie la fenêtre à son modèle de vue.</summary>
    protected virtual void Attach(IDraftEditor editor)
    {
        _editor = editor;
        DataContext = editor;
        editor.Closed += (_, _) =>
        {
            _timer.Stop();
            Hide();
        };
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (FocusManager?.GetFocusedElement() is TextBox)
        {
            base.OnKeyDown(e);
            return;
        }

        if ((e.KeyModifiers & KeyModifiers.Control) != 0 && e.Key is Key.Z or Key.Y)
        {
            if (e.Key == Key.Z)
            {
                _editor?.Undo();
            }
            else
            {
                _editor?.Redo();
            }

            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void PresentCore(Window? owner)
    {
        _timer.Start();
        if (!IsVisible)
        {
            // Jamais plus grande que la fenêtre principale ni que l'écran (leçon de la fenêtre d'édition des scènes, essai 1.007.092).
            var screen = (owner is not null ? Screens.ScreenFromWindow(owner) : null) ?? Screens.Primary;
            if (screen is not null)
            {
                var scaling = screen.Scaling <= 0 ? 1 : screen.Scaling;
                Width = Math.Min(Width, (screen.WorkingArea.Width / scaling) - 40);
                Height = Math.Min(Height, (screen.WorkingArea.Height / scaling) - 40);
                MinWidth = Math.Min(MinWidth, Width);
                MinHeight = Math.Min(MinHeight, Height);
            }

            if (owner is not null)
            {
                Show(owner);
            }
            else
            {
                Show();
            }
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown or WindowCloseReason.OwnerWindowClosing)
        {
            _editor?.Abandon();
            _timer.Stop();
            return;
        }

        e.Cancel = true;
        _ = CloseWithConfirmationAsync();
    }

    private async Task CloseWithConfirmationAsync()
    {
        if (_editor is null || await _editor.ConfirmCloseAsync().ConfigureAwait(true))
        {
            _timer.Stop();
            Hide();
        }
    }
}
