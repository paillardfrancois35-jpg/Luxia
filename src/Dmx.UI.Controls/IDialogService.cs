namespace Dmx.UI.Controls;

/// <summary>
/// Boîtes de dialogue de l'Atelier, fournies par l'application aux modèles de vue.
/// Jamais utilisées en Live (P9 : aucune question bloquante).
/// </summary>
public interface IDialogService
{
    /// <summary>Demande confirmation d'une action destructrice (GEN-103).</summary>
    Task<bool> ConfirmAsync(string title, string message);

    /// <summary>Demande un texte (nom d'un instantané, d'un projet…) ; null si annulé.</summary>
    Task<string?> AskTextAsync(string title, string prompt, string? initialValue = null);

    /// <summary>Choix d'un dossier ; null si annulé.</summary>
    Task<string?> PickFolderAsync(string title);

    /// <summary>Choix d'un ou plusieurs fichiers ; vide si annulé.</summary>
    Task<IReadOnlyList<string>> PickFilesAsync(string title, bool allowMultiple, params string[] extensions);
}
