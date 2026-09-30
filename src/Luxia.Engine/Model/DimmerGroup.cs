namespace Luxia.Engine.Model;

/// <summary>
/// Groupe d'appareils de l'arbre des dimmers (ERG-036, ERG-037), tel que le moteur le connaît. Un groupe peut avoir un
/// dimmer (un niveau réglable en direct) ou n'être qu'un nœud d'organisation (niveau fixe à 1).
/// </summary>
/// <param name="Id">Identifiant stable du groupe.</param>
/// <param name="Name">Nom affiché (journal, explication de la valeur).</param>
/// <param name="Parent">Rang du groupe parent dans <see cref="ShowModel.DimmerGroups"/>, ou -1 pour une racine ; toujours inférieur au rang du groupe.</param>
/// <param name="HasDimmer">Le groupe a un dimmer réglable.</param>
public sealed record DimmerGroup(Guid Id, string Name, int Parent, bool HasDimmer);
