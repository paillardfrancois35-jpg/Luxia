using Luxia.Fixtures.Model;

namespace Luxia.Fixtures.Import;

/// <summary>
/// Résultat de l'import d'un fichier (BIB-082) : le modèle converti et la liste de ce qui a été approximé
/// ou ignoré. Un élément inconnu ne bloque jamais l'import : il devient « Générique » et figure au rapport.
/// </summary>
/// <param name="SourceFile">Fichier importé.</param>
/// <param name="Fixture">Modèle converti (null si le fichier est illisible).</param>
/// <param name="Notes">Éléments non convertis ou approximés.</param>
/// <param name="Error">Erreur bloquante (fichier illisible, format inconnu).</param>
public sealed record ImportResult(string SourceFile, FixtureType? Fixture, IReadOnlyList<string> Notes, string? Error = null)
{
    /// <summary>L'import a produit un modèle.</summary>
    public bool Succeeded => Fixture is not null;

    /// <summary>Résumé d'une ligne pour le rapport.</summary>
    public string Summary => Fixture is { } f
        ? $"{f.DisplayName} : {f.Modes.Count} mode(s), {f.Channels.Count} canaux{(Notes.Count > 0 ? $", {Notes.Count} remarque(s)" : string.Empty)}"
        : $"{Path.GetFileName(SourceFile)} : {Error}";
}
