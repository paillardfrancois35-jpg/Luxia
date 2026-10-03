using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>
/// Fenêtre « Base musicale » (MUS-027 à MUS-029), ouverte depuis le bloc « Morceau en cours » : onglets Base et À classer, import et export
/// CSV, raccourcis clavier de l'onglet À classer (1 à 9, 0, Q, W, E, R : une famille ; Entrée : passer).
/// </summary>
public partial class MusicBaseWindow : Window
{
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
            this.FindControl<TextBlock>("ShortcutsText")!.Text = "Raccourcis : " + string.Join("   ", vm.Families.Select(f => $"{vm.KeyOf(f)} = {f.Name}"));
        };
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        // Les raccourcis ne valent que dans l'onglet « À classer » et hors d'un champ de texte.
        if (DataContext is MusicBaseViewModel { SelectedTab: 1 } vm && FocusManager?.GetFocusedElement() is not TextBox)
        {
            if (e.Key == Key.Enter)
            {
                vm.SkipCommand.Execute(null);
                e.Handled = true;
                return;
            }

            var text = e.Key switch
            {
                >= Key.D0 and <= Key.D9 => ((char)('0' + (e.Key - Key.D0))).ToString(),
                >= Key.NumPad0 and <= Key.NumPad9 => ((char)('0' + (e.Key - Key.NumPad0))).ToString(),
                Key.Q or Key.W or Key.E or Key.R => e.Key.ToString(),
                _ => null,
            };
            if (text is not null && e.KeyModifiers == KeyModifiers.None && vm.FamilyOfKey(text) is { } family)
            {
                vm.ClassifyCommand.Execute(family.Id);
                e.Handled = true;
                return;
            }
        }

        base.OnKeyDown(e);
    }

    private async Task ImportAsync(MusicBaseViewModel vm)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Importer un fichier CSV (artistes, titres ou playlist)",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Fichier CSV") { Patterns = ["*.csv", "*.txt"] }],
        }).ConfigureAwait(true);
        if (files.Count == 0)
        {
            return;
        }

        await using var stream = await files[0].OpenReadAsync().ConfigureAwait(true);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        vm.ImportFile(await reader.ReadToEndAsync().ConfigureAwait(true));
    }

    private async Task ExportAsync(MusicBaseViewModel vm)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Exporter les artistes en CSV",
            SuggestedFileName = "artistes.csv",
            DefaultExtension = "csv",
            FileTypeChoices = [new FilePickerFileType("Fichier CSV") { Patterns = ["*.csv"] }],
        }).ConfigureAwait(true);
        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenWriteAsync().ConfigureAwait(true);
        stream.SetLength(0);
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetPreamble().Concat(Encoding.UTF8.GetBytes(vm.ExportText())).ToArray();
        await stream.WriteAsync(bytes).ConfigureAwait(true);
    }
}
