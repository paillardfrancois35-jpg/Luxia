namespace Luxia.Messaging.Commands;

/// <summary>
/// CMD-030 <c>Fumée</c> : émission manuelle sur toutes les machines à fumée, tant que la commande est maintenue
/// (<paramref name="Pressed"/>) ou pendant une rafale (<paramref name="Burst"/>). Toujours soumise au limiteur de
/// fumée (GEN-084, MOT-081).
/// </summary>
/// <param name="Origin">Origine.</param>
/// <param name="Pressed">Appui (<c>true</c>) ou relâche (<c>false</c>) ; ignoré pour une rafale.</param>
/// <param name="Burst">Rafale d'une durée donnée ; <c>null</c> = maintien.</param>
/// <param name="Level">Niveau d'émission (0-1, 1 par défaut).</param>
public sealed record SmokeCommand(CommandOrigin Origin, bool Pressed, TimeSpan? Burst = null, double Level = 1) : Command(Origin);
