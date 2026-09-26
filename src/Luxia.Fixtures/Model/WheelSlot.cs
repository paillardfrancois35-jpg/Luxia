namespace Luxia.Fixtures.Model;

/// <summary>Emplacement d'une roue.</summary>
/// <param name="Name">Nom (« Rouge », « Étoile »).</param>
/// <param name="Colors">Couleurs #RRGGBB (vide pour un gobo ou un emplacement ouvert).</param>
/// <param name="Image">Image du motif (gobo), chemin relatif au modèle.</param>
public sealed record WheelSlot(string Name, IReadOnlyList<string> Colors, string? Image = null);
