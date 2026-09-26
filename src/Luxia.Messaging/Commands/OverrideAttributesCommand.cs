namespace Luxia.Messaging.Commands;

/// <summary>
/// CMD-021 <c>SurchargerAttribut</c> : impose des valeurs logiques d'attributs (console en mode appareils, programmeur).
/// Appliquées à l'étape 5 de la chaîne de rendu (doc 02 §9) : elles passent donc par le Grand Master, le blackout
/// et les limites de sûreté (CONS-022), jusqu'à libération (<see cref="ReleaseAttributesCommand"/>).
/// </summary>
/// <param name="Origin">Origine.</param>
/// <param name="Values">Attributs et valeurs.</param>
public sealed record OverrideAttributesCommand(CommandOrigin Origin, IReadOnlyList<AttributeValue> Values) : Command(Origin);
