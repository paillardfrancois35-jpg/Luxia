namespace Luxia.Persistence;

/// <summary>
/// Emplacements des données (doc 02 §10.1). Les racines sont injectables (tests, poste de démonstration).
/// </summary>
/// <param name="DocumentsRoot">Racine des données utilisateur (<c>Documents\LuXia</c>).</param>
/// <param name="AppDataRoot">Racine des préférences du poste (<c>%AppData%\LuXia</c>).</param>
public sealed record DataPaths(string DocumentsRoot, string AppDataRoot)
{
    /// <summary>Emplacements par défaut de l'utilisateur Windows, avec migration depuis les anciens dossiers « DMX ».</summary>
    public static DataPaths Default { get; } = new(
        MigrateLegacyFolder(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DMX"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LuXia")),
        MigrateLegacyFolder(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DMX"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LuXia")));

    /// <summary>
    /// Si l'ancien dossier existe encore et que le nouveau n'existe pas, le déplace (renommage de l'application, 2026-09-26).
    /// Ne lève jamais : en cas d'échec, les données restent dans l'ancien dossier (GEN-056).
    /// </summary>
    internal static string MigrateLegacyFolder(string legacyPath, string newPath)
    {
        if (Directory.Exists(newPath) || !Directory.Exists(legacyPath))
        {
            return newPath;
        }

        try
        {
            Directory.Move(legacyPath, newPath);
            return newPath;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return legacyPath;
        }
    }

    /// <summary>Variable d'environnement qui redirige toutes les données (poste de démonstration, essais).</summary>
    public const string OverrideVariable = "LUXIA_DOSSIER_DONNEES";

    /// <summary>
    /// Emplacements effectifs : ceux de <see cref="Default"/>, ou <c>&lt;dossier&gt;\Documents</c> et <c>&lt;dossier&gt;\AppData</c>
    /// si la variable <see cref="OverrideVariable"/> est définie.
    /// </summary>
    public static DataPaths Current =>
        Environment.GetEnvironmentVariable(OverrideVariable) is { Length: > 0 } root
            ? new DataPaths(Path.Combine(root, "Documents"), Path.Combine(root, "AppData"))
            : Default;

    /// <summary>Bibliothèque d'appareils (partagée).</summary>
    public string Library => Path.Combine(DocumentsRoot, "Bibliothèque");

    /// <summary>Projets.</summary>
    public string Projects => Path.Combine(DocumentsRoot, "Projets");

    /// <summary>Base musicale.</summary>
    public string Music => Path.Combine(DocumentsRoot, "Musique");

    /// <summary>Journaux (technique, soirée).</summary>
    public string Logs => Path.Combine(DocumentsRoot, "Journaux");

    /// <summary>Enregistrements de trames.</summary>
    public string Recordings => Path.Combine(DocumentsRoot, "Enregistrements");

    /// <summary>Fichier des préférences du poste.</summary>
    public string PreferencesFile => Path.Combine(AppDataRoot, "preferences.json");
}
