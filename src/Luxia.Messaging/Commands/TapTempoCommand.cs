namespace Luxia.Messaging.Commands;

/// <summary>CMD-040 <c>TapTempo</c> : une frappe de tap tempo (AUD-025).</summary>
/// <param name="Origin">Origine.</param>
public sealed record TapTempoCommand(CommandOrigin Origin) : Command(Origin);
