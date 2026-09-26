using Luxia.Fixtures.Model;

namespace Luxia.Fixtures.Rules;

/// <summary>
/// Déductions automatiques (doc 12 §2.7) : intensité virtuelle, « Suit l'intensité », étiquettes de sûreté.
/// Une valeur imposée dans la définition l'emporte toujours sur la déduction.
/// </summary>
public static class FixtureRules
{
    /// <summary>
    /// Le mode n'a pas de canal d'intensité mais a des émetteurs de couleur : le moteur multipliera
    /// les émetteurs par l'intensité logique (BIB-006).
    /// </summary>
    public static bool HasVirtualIntensity(FixtureType fixture, FixtureMode mode)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(mode);
        var channels = ChannelsOf(fixture, mode).ToList();
        return !channels.Any(c => c.Attribute == AttributeKind.Intensity)
            && channels.Any(c => AttributeCatalog.Get(c.Attribute).IsEmitter);
    }

    /// <summary>Valeur déduite de « Suit l'intensité » pour un canal dans un mode (tableau du doc 12 §2.7).</summary>
    public static bool DeducedFollowsIntensity(FixtureType fixture, FixtureMode mode, ChannelDefinition channel)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(mode);
        ArgumentNullException.ThrowIfNull(channel);
        if (!AttributeCatalog.Get(channel.Attribute).IsEmitter)
        {
            return false;
        }

        // Émetteur d'une cellule : il suit l'intensité de la cellule seulement si elle n'a pas son propre gradateur.
        var channels = ChannelsOf(fixture, mode).ToList();
        var hasOwnDimmer = channels.Any(c =>
            c.Attribute == AttributeKind.Intensity
            || (c.Attribute == AttributeKind.CellIntensity && c.Cell == channel.Cell));
        return !hasOwnDimmer;
    }

    /// <summary>« Suit l'intensité » effectif : valeur imposée, sinon déduite.</summary>
    public static bool FollowsIntensity(FixtureType fixture, FixtureMode mode, ChannelDefinition channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        return channel.FollowsIntensity ?? DeducedFollowsIntensity(fixture, mode, channel);
    }

    /// <summary>
    /// Étiquettes de sûreté déduites : celles de l'attribut, plus « strobe » si une plage produit un strobe
    /// (ex. canal Programme dont une plage est un strobe, BIB-007).
    /// </summary>
    public static SafetyTags DeducedSafety(ChannelDefinition channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        var tags = AttributeCatalog.Get(channel.Attribute).DefaultTags;
        if (channel.Capabilities.Any(c => c.Strobe is StrobeEffect.Strobe or StrobeEffect.Random or StrobeEffect.Pulse))
        {
            tags |= SafetyTags.Strobe;
        }

        return tags;
    }

    /// <summary>Étiquettes de sûreté effectives : imposées, sinon déduites.</summary>
    public static SafetyTags Safety(ChannelDefinition channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        return channel.Safety ?? DeducedSafety(channel);
    }

    /// <summary>Nombre de cellules d'un mode (plus grand numéro de cellule utilisé).</summary>
    public static int CellCount(FixtureType fixture, FixtureMode mode) =>
        ChannelsOf(fixture, mode).Select(c => c.Cell).DefaultIfEmpty(0).Max();

    /// <summary>Texte « réglage sur l'appareil » d'un mode (BIB-005, BIB-026).</summary>
    public static string SettingSheet(FixtureType fixture, FixtureMode mode, int? address = null)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(mode);
        var setting = string.IsNullOrWhiteSpace(mode.DeviceSetting) ? "(réglage non renseigné)" : mode.DeviceSetting;
        var addressText = address is { } a ? $" – adresse {a} à {a + mode.ChannelCount - 1}" : string.Empty;
        return $"{fixture.DisplayName} : mode « {mode.Name} » ({mode.ChannelCount} canaux) – régler l'appareil sur {setting}{addressText}";
    }

    /// <summary>Définitions de canaux utilisées par un mode (sans doublon).</summary>
    public static IEnumerable<ChannelDefinition> ChannelsOf(FixtureType fixture, FixtureMode mode)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(mode);
        return mode.Channels
            .Select(m => m.Channel)
            .Distinct(StringComparer.Ordinal)
            .Select(fixture.Channel)
            .OfType<ChannelDefinition>();
    }
}
