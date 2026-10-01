namespace Luxia.Messaging.Commands;

/// <summary>CMD-042 <c>AjusterTempo</c> : ×2, ÷2, ± BPM ou recalage de la mesure.</summary>
/// <param name="Origin">Origine.</param>
/// <param name="Adjustment">Réglage.</param>
/// <param name="Value">BPM à ajouter (réglage <see cref="TempoAdjustment.AddBpm"/>).</param>
public sealed record AdjustTempoCommand(CommandOrigin Origin, TempoAdjustment Adjustment, double Value = 0) : Command(Origin);
