using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>
/// Fenêtre « Base musicale » (MUS-027 à MUS-029), ouverte depuis le bloc « Morceau en cours » : onglets Base (liste + fiche), À classer et
/// Propositions, import et export JSON. Fermer la fenêtre avec une fiche modifiée pose la question Oui / Non / Annuler (doc 60).
/// </summary>
public partial class MusicBaseWindow : Window
{
    private static readonly FilePickerFileType JsonFiles = new("Fichier JSON") { Patterns = ["*.json"] };

    private bool _closeConfirmed;

    /// <summary>Crée la fenêtre.</summary>
    public MusicBaseWindow()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<Button>("CloseButton")!.Click += (_, _) => Close();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is not MusicBaseViewModel vm)
            {
                return;
            }

            vm.ImportRequested += async (_, _) => await ImportAsync(vm).ConfigureAwait(true);
            vm.ExportRequested += async (_, _) => await ExportAsync(vm).ConfigureAwait(true);
            vm.FocusRequested += (_, row) => FocusFirstCell(vm, row);
        };
    }

    /// <inheritdoc />
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_closeConfirmed && DataContext is MusicBaseViewModel { IsModified: true } vm)
        {
            // La question est asynchrone : on retient la fermeture, puis on la rejoue si l'utilisateur répond Oui ou Non.
            e.Cancel = true;
            _ = AskThenCloseAsync(vm);
        }

        base.OnClosing(e);
    }

    private async Task AskThenCloseAsync(MusicBaseViewModel vm)
    {
        if (await vm.CanLeaveAsync().ConfigureAwait(true))
        {
            _closeConfirmed = true;
            Close();
        }
    }

    // Le curseur va dans la première cellule de la ligne ajoutée (ou de la ligne vide qui existait déjà) ; Tab passe ensuite d'une cellule à l'autre.
    private void FocusFirstCell(MusicBaseViewModel vm, object row)
    {
        var list = this.FindControl<ItemsControl>(row is EditableText ? "AliasList" : "TitleList");
        var index = row is EditableText alias ? vm.Aliases.IndexOf(alias) : row is TitleRow title ? vm.TitleRows.IndexOf(title) : -1;
        if (list is null || index < 0)
        {
            return;
        }

        // La ligne vient d'être ajoutée : son conteneur est créé au prochain passage de mise en page.
        Dispatcher.UIThread.Post(
            () =>
            {
                var box = list.ContainerFromIndex(index)?.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
                box?.Focus();
            },
            DispatcherPriority.Loaded);
    }

    private async Task ImportAsync(MusicBaseViewModel vm)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Importer un fichier JSON (styles, artistes, alias)",
            AllowMultiple = false,
            FileTypeFilter = [JsonFiles],
        }).ConfigureAwait(true);
        if (files.Count > 0 && files[0].TryGetLocalPath() is { } path)
        {
            vm.ImportFile(path);
        }
    }

    private async Task ExportAsync(MusicBaseViewModel vm)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Exporter la base musicale en JSON",
            SuggestedFileName = "base-musicale.json",
            DefaultExtension = "json",
            FileTypeChoices = [JsonFiles],
        }).ConfigureAwait(true);
        if (file?.TryGetLocalPath() is { } path)
        {
            vm.ExportFile(path);
        }
    }
}
