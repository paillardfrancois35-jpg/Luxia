using System.Globalization;
using Luxia.Tools.Headless;

// Outil sans interface (doc 00 §7.2) : pilotage de la boucle et des sorties, validation et déroulé d'un projet (GEN-131, GEN-132).
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
Console.OutputEncoding = System.Text.Encoding.UTF8;

var arguments = new Arguments(args);
return arguments.Command switch
{
    "ports" => Commands.Ports(),
    "midi" => Commands.Midi(arguments),
    "audio" => AudioCommands.Analyse(arguments),
    "audio-ecoute" => AudioCommands.Listen(arguments),
    "audio-diag" => AudioCommands.Diagnose(arguments),
    "media" => MediaCommands.Probe(arguments),
    "lancer" => await Commands.RunAsync(arguments).ConfigureAwait(false),
    "endurance" => await Commands.EnduranceAsync(arguments).ConfigureAwait(false),
    "gigue" => await Commands.JitterAsync(arguments).ConfigureAwait(false),
    "projet" => Commands.Project(arguments),
    "relire" => Commands.Replay(arguments),
    "valider" => ProjectCommands.Validate(arguments),
    "jouer" => ProjectCommands.Play(arguments),
    "scenario" => ProjectCommands.Scenario(arguments),
    _ => Commands.Help(),
};
