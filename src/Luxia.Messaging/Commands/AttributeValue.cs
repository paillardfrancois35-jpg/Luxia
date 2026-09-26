namespace Luxia.Messaging.Commands;

/// <summary>Valeur logique d'un attribut d'appareil (CMD-021).</summary>
/// <param name="FixtureId">Appareil patché.</param>
/// <param name="ChannelKey">Clé de la définition de canal du modèle (l'attribut de cet appareil).</param>
/// <param name="Value">Valeur normalisée 0 à 1 (GEN-020).</param>
public readonly record struct AttributeValue(Guid FixtureId, string ChannelKey, double Value);
