namespace Luxia.Messaging.Commands;

/// <summary>Décalage de latence global des événements musicaux (GEN-035, AUD-027), de −0,5 à +0,5 s.</summary>
/// <param name="Origin">Origine.</param>
/// <param name="Seconds">Décalage en secondes (positif : les événements sont avancés).</param>
public sealed record SetTempoLatencyCommand(CommandOrigin Origin, double Seconds) : Command(Origin);
