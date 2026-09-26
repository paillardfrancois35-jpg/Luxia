using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Luxia.UI.Controls;

namespace Luxia.App.Services;

/// <summary>Boîtes de dialogue de l'Atelier, construites simplement (thème sombre de l'application).</summary>
internal sealed class DialogService(Func<Window?> owner) : IDialogService
{
    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var window = Owner();
        if (window is null)
        {
            return false;
        }

        var dialog = CreateDialog(title);
        var yes = new Button { Content = "Oui", IsDefault = true, MinWidth = 80 };
        var no = new Button { Content = "Non", IsCancel = true, MinWidth = 80 };
        yes.Click += (_, _) => dialog.Close(true);
        no.Click += (_, _) => dialog.Close(false);
        dialog.Content = Layout(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap }, yes, no);
        return await dialog.ShowDialog<bool>(window).ConfigureAwait(true);
    }

    public async Task<string?> AskTextAsync(string title, string prompt, string? initialValue = null)
    {
        var window = Owner();
        if (window is null)
        {
            return null;
        }

        var dialog = CreateDialog(title);
        var box = new TextBox { Text = initialValue ?? string.Empty, MinWidth = 320 };
        var ok = new Button { Content = "Valider", IsDefault = true, MinWidth = 80 };
        var cancel = new Button { Content = "Annuler", IsCancel = true, MinWidth = 80 };
        ok.Click += (_, _) => dialog.Close(box.Text);
        cancel.Click += (_, _) => dialog.Close(null);
        dialog.Content = Layout(new StackPanel { Spacing = 8, Children = { new TextBlock { Text = prompt }, box } }, ok, cancel);
        dialog.Opened += (_, _) => box.Focus();
        return await dialog.ShowDialog<string?>(window).ConfigureAwait(true);
    }

    public async Task<string?> PickFolderAsync(string title)
    {
        var window = Owner();
        if (window is null)
        {
            return null;
        }

        var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = title }).ConfigureAwait(true);
        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    public async Task<IReadOnlyList<string>> PickFilesAsync(string title, bool allowMultiple, params string[] extensions)
    {
        var window = Owner();
        if (window is null)
        {
            return [];
        }

        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = allowMultiple,
            FileTypeFilter = extensions.Length == 0
                ? null
                : [new FilePickerFileType(string.Join(", ", extensions)) { Patterns = [.. extensions.Select(e => "*" + e)] }],
        };
        var files = await window.StorageProvider.OpenFilePickerAsync(options).ConfigureAwait(true);
        return [.. files.Select(f => f.TryGetLocalPath()).OfType<string>()];
    }

    private static Window CreateDialog(string title) => new()
    {
        Title = title,
        SizeToContent = SizeToContent.WidthAndHeight,
        CanResize = false,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        ShowInTaskbar = false,
        MinWidth = 360,
    };

    private static StackPanel Layout(Control body, params Button[] buttons) => new StackPanel
    {
        Margin = new Thickness(20),
        Spacing = 16,
        MaxWidth = 520,
        Children =
        {
            body,
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { buttons[0], buttons[1] } },
        },
    };

    private Window? Owner() => owner();
}
