namespace Luxia.Engine;

/// <summary>
/// D'où vient la valeur finale d'un paramètre (GEN-043, MOT-034) : défaut, scène (et sa couche), surcharge, blackout.
/// </summary>
/// <param name="Kind">Genre de source.</param>
/// <param name="SceneId">Scène qui fournit la valeur (si <see cref="SourceKind.Scene"/>).</param>
/// <param name="LayerId">Couche de cette scène.</param>
public readonly record struct ParameterSource(SourceKind Kind, Guid SceneId = default, Guid LayerId = default);
