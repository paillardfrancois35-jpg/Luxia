using Luxia.Fixtures.Model;

namespace Luxia.Scenes.Rules;

/// <summary>
/// Palette automatique (PAL-003) : une plage marquée « palette automatique » dans la bibliothèque, regroupée par
/// attribut et libellé sur tous les modèles de la sélection (« Strobe lent » du PAR et de la lyre = un seul bouton).
/// </summary>
/// <param name="Attribute">Attribut du canal.</param>
/// <param name="Label">Libellé de la plage.</param>
/// <param name="Color">Couleur de la plage (emplacement de roue), sinon <c>null</c>.</param>
/// <param name="Ranges">Pour chaque modèle concerné : canal et bornes de la plage.</param>
public sealed record AutoPalette(
    AttributeKind Attribute,
    string Label,
    string? Color,
    IReadOnlyList<(Guid FixtureTypeId, string ChannelKey, int Min, int Max)> Ranges);
