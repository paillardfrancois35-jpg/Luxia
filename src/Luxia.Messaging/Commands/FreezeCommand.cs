namespace Luxia.Messaging.Commands;

/// <summary>
/// CMD-003 <c>Figer</c> : la sortie des étapes 1 à 7 est gelée ; blackout et sûreté restent actifs (MOT-073).
/// </summary>
/// <param name="Origin">Origine.</param>
/// <param name="Active">Figé.</param>
/// <param name="SuspendPlaybacks">
/// Suspendre aussi les lectures (elles reprennent là où elles étaient) ; par défaut elles continuent en arrière-plan
/// et le dégel reprend à leur état courant.
/// </param>
public sealed record FreezeCommand(CommandOrigin Origin, bool Active, bool SuspendPlaybacks = false) : Command(Origin);
