using Luxia.Messaging.Commands;

namespace Luxia.Hosting.Tools;

/// <summary>Commande d'un scénario, à son instant (MOT-103).</summary>
/// <param name="At">Instant depuis le début du déroulé.</param>
/// <param name="Command">Commande envoyée au moteur.</param>
/// <param name="Line">Numéro de ligne dans le fichier (0 = générée).</param>
/// <param name="Text">Texte de la ligne.</param>
public sealed record ScenarioStep(TimeSpan At, Command Command, int Line, string Text);
