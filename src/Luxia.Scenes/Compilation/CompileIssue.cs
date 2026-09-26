using Luxia.Fixtures.Rules;

namespace Luxia.Scenes.Compilation;

/// <summary>
/// Problème trouvé en compilant ou en validant un projet (GEN-131) : où (fichier, objet, champ) et pourquoi.
/// Un problème n'empêche jamais le reste du projet de jouer (GEN-056) : la valeur fautive est ignorée.
/// </summary>
/// <param name="Severity">Gravité.</param>
/// <param name="File">Fichier du projet (« scènes.json »).</param>
/// <param name="Item">Objet concerné (« scène « Blanc chaud », étape 2 »).</param>
/// <param name="Field">Champ (« paletteId »).</param>
/// <param name="Message">Explication en français.</param>
public sealed record CompileIssue(IssueSeverity Severity, string File, string Item, string Field, string Message)
{
    /// <inheritdoc />
    public override string ToString() =>
        $"{(Severity == IssueSeverity.Error ? "Erreur" : "Avertissement")} – {File} – {Item} – {Field} : {Message}";
}
