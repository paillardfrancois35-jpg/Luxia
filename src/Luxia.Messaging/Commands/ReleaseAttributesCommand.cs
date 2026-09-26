namespace Luxia.Messaging.Commands;

/// <summary>
/// CMD-022 <c>LibérerSurcharges</c>, pour les surcharges d'attributs (étape 5) : rend la main aux couches.
/// </summary>
/// <param name="Origin">Origine.</param>
/// <param name="FixtureId">Appareil ; <c>null</c> = tous les appareils.</param>
/// <param name="ChannelKeys">Attributs de cet appareil ; <c>null</c> = tous.</param>
public sealed record ReleaseAttributesCommand(CommandOrigin Origin, Guid? FixtureId = null, IReadOnlyList<string>? ChannelKeys = null) : Command(Origin);
