namespace Luxia.Fixtures.Rules;

/// <summary>Problème détecté dans un modèle.</summary>
/// <param name="Severity">Gravité.</param>
/// <param name="Location">Où (« Mode 7 canaux, position 3 », « Canal Strobe »).</param>
/// <param name="Message">Explication en français.</param>
public sealed record ValidationIssue(IssueSeverity Severity, string Location, string Message)
{
    /// <inheritdoc />
    public override string ToString() => $"{(Severity == IssueSeverity.Error ? "Erreur" : "Avertissement")} – {Location} : {Message}";
}
