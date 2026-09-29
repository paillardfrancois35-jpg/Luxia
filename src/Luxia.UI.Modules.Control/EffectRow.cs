namespace Luxia.UI.Modules.Control;

/// <summary>Un effet de l'étape éditée, dans la liste du panneau Effets.</summary>
/// <param name="Id">Identifiant de l'effet.</param>
/// <param name="Title">Icône de la forme et nom.</param>
/// <param name="Detail">Appareils et durée d'un cycle.</param>
/// <param name="Color">Couleur de la famille (intensité, mouvement, couleur).</param>
public sealed record EffectRow(Guid Id, string Title, string Detail, string Color);
