namespace Dmx.Persistence;

/// <summary>
/// Emplacements des données (doc 02 §10.1). Les racines sont injectables (tests, poste de démonstration).
/// </summary>
/// <param name="DocumentsRoot">Racine des données utilisateur (<c>Documents\DMX</c>).</param>
/// <param name="AppDataRoot">Racine des préférences du poste (<c>%AppData%\DMX</c>).</param>
public sealed record DataPaths(string DocumentsRoot, string AppDataRoot)
{
    /// <summary>Emplacements par défaut de l'utilisateur Windows.</summary>
    public static DataPaths Default { get; } = new(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DMX"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DMX"));

    /// <summary>Variable d'environnement qui redirige toutes les données (poste de démonstration, essais).</summary>
    public const string OverrideVariable = "DMX_DOSSIER_DONNEES";

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
