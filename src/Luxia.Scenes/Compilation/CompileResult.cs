using Luxia.Engine.Model;

namespace Luxia.Scenes.Compilation;

/// <summary>Résultat d'une compilation : le modèle du moteur et les problèmes rencontrés.</summary>
/// <param name="Model">Modèle prêt à être chargé par le moteur.</param>
/// <param name="Issues">Problèmes (valeurs ignorées, références introuvables).</param>
public sealed record CompileResult(ShowModel Model, IReadOnlyList<CompileIssue> Issues);
