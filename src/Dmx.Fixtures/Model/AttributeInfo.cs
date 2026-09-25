using System.Collections.Frozen;

namespace Dmx.Fixtures.Model;

/// <summary>Description d'un attribut du catalogue.</summary>
/// <param name="Attribute">Attribut.</param>
/// <param name="Label">Libellé français (interface).</param>
/// <param name="Family">Famille.</param>
/// <param name="IsEmitter">Émetteur de couleur (mélange) : concerné par l'intensité virtuelle (doc 12 §2.7).</param>
/// <param name="DefaultTags">Étiquettes de sûreté déduites (BIB-007).</param>
public sealed record AttributeInfo(AttributeKind Attribute, string Label, AttributeFamily Family, bool IsEmitter, SafetyTags DefaultTags);
