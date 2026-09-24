namespace Dmx.Messaging.Commands;

/// <summary>
/// CMD-022 <c>LibérerSurcharges</c> : rend la main à la chaîne de rendu (CONS-003, CONS-004).
/// </summary>
/// <param name="Origin">Origine.</param>
/// <param name="Universe">Univers concerné ; <c>null</c> = tous les univers.</param>
/// <param name="Channels">Canaux à libérer ; <c>null</c> = tous les canaux de l'univers.</param>
public sealed record ReleaseOverridesCommand(CommandOrigin Origin, int? Universe = null, IReadOnlyList<int>? Channels = null) : Command(Origin)
{
    /// <summary>Tout libérer (bouton « Tout libérer »).</summary>
    public static ReleaseOverridesCommand All(CommandOrigin origin) => new(origin);
}
