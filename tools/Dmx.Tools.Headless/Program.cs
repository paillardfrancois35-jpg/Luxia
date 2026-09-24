using System.Globalization;
using Dmx.Tools.Headless;

// Outil sans interface (doc 00 §7.2, GEN-131 à venir) : en P0, pilotage de la boucle et des sorties en ligne de commande.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
Console.OutputEncoding = System.Text.Encoding.UTF8;

var arguments = new Arguments(args);
return arguments.Command switch
{
    "ports" => Commands.Ports(),
    "lancer" => await Commands.RunAsync(arguments).ConfigureAwait(false),
    "endurance" => await Commands.EnduranceAsync(arguments).ConfigureAwait(false),
    "gigue" => await Commands.JitterAsync(arguments).ConfigureAwait(false),
    "relire" => Commands.Replay(arguments),
    _ => Commands.Help(),
};
