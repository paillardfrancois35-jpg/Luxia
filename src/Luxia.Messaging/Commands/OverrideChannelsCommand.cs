namespace Luxia.Messaging.Commands;

/// <summary>
/// CMD-020 <c>SurchargerCanal</c> : impose des valeurs brutes sur des canaux (console en mode canaux, CONS-003).
/// Appliquées à l'étape 11 de la chaîne de rendu (doc 02 §9), jusqu'à libération (CMD-022).
/// Plusieurs canaux par commande : un déplacement de faders sélectionnés (CONS-006) ou un instantané (CONS-010)
/// s'applique ainsi au même tick.
/// </summary>
/// <param name="Origin">Origine.</param>
/// <param name="Universe">Univers.</param>
/// <param name="Values">Canaux et valeurs.</param>
public sealed record OverrideChannelsCommand(CommandOrigin Origin, int Universe, IReadOnlyList<ChannelValue> Values) : Command(Origin);
