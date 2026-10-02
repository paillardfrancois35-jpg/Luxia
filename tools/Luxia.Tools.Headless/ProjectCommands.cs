using System.Globalization;
using Luxia.Fixtures.Rules;
using Luxia.Hosting.Tools;

namespace Luxia.Tools.Headless;

/// <summary>Commandes sur un projet : valider (GEN-131), jouer une scène (GEN-132), dérouler un scénario (MOT-103).</summary>
internal static class ProjectCommands
{
    public static int Validate(Arguments args)
    {
        if (Folder(args) is not { } folder)
        {
            return 2;
        }

        var issues = ProjectValidator.Validate(folder);
        foreach (var issue in issues)
        {
            Console.WriteLine(issue);
        }

        var errors = issues.Count(i => i.Severity == IssueSeverity.Error);
        Console.WriteLine(errors == 0 && issues.Count == 0
            ? "Projet valide : aucun problème."
            : string.Create(CultureInfo.CurrentCulture, $"{errors} erreur(s), {issues.Count - errors} avertissement(s)."));
        return errors == 0 ? 0 : 1;
    }

    public static int Play(Arguments args)
    {
        if (Folder(args) is not { } folder)
        {
            return 2;
        }

        var content = ProjectFiles.Load(folder);
        var (sequences, _) = Show.SequenceStore.Load(folder);
        var (shows, _) = Show.ShowStore.Load(folder);

        // GEN-132 : « jouer » une séquence ou un show (P8), au métronome (--tempo, 120 BPM par défaut), sans musique.
        if ((args.Get("show") ?? args.Get("sequence")) is { } key)
        {
            var isShow = args.Get("show") is not null;
            var items = isShow ? shows.Shows.Select(x => (x.Id, x.Name)) : sequences.Sequences.Select(x => (x.Id, x.Name));
            var found = items.FirstOrDefault(i => string.Equals(i.Name, key, StringComparison.CurrentCultureIgnoreCase) || i.Id.ToString() == key);
            if (found.Id == Guid.Empty)
            {
                Console.Error.WriteLine($"{(isShow ? "Show" : "Séquence")} introuvable : « {key} ». Disponibles : {string.Join(", ", items.Select(i => i.Name))}");
                return 2;
            }

            var tempo = args.Get("tempo") ?? "120";
            var text = $"0 tempo {tempo}\n0 {(isShow ? "show" : "sequence")} {found.Id}\n";
            var played = Hosting.Tools.Scenario.Parse(text, content.Scenes, content.Layers, out _, sequences, shows);
            return Report(ScenarioRunner.Run(content, played, Duration(args, 30), Recording(args), Sample(args), sequences: sequences, shows: shows));
        }

        var name = args.Get("scene");
        var scene = content.Scenes.Scenes.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.CurrentCultureIgnoreCase) || s.Id.ToString() == name);
        if (scene is null)
        {
            Console.Error.WriteLine($"Scène introuvable : « {name} ». Scènes du projet : {string.Join(", ", content.Scenes.Scenes.Select(s => s.Name))}");
            return 2;
        }

        return Report(ScenarioRunner.Run(content, Hosting.Tools.Scenario.ForScene(scene.Id), Duration(args, 10), Recording(args), Sample(args)));
    }

    public static int Scenario(Arguments args)
    {
        if (Folder(args) is not { } folder)
        {
            return 2;
        }

        if (args.Positional.Count < 2 || !File.Exists(args.Positional[1]))
        {
            Console.Error.WriteLine("Usage : luxia-headless scenario dossier fichier.txt [--duree 60]");
            return 2;
        }

        var content = ProjectFiles.Load(folder);
        var (sequences, _) = Show.SequenceStore.Load(folder);
        var (shows, _) = Show.ShowStore.Load(folder);
        var scenario = Hosting.Tools.Scenario.Parse(File.ReadAllText(args.Positional[1]), content.Scenes, content.Layers, out var errors, sequences, shows);
        foreach (var error in errors)
        {
            Console.Error.WriteLine(error);
        }

        return errors.Count > 0 ? 1 : Report(ScenarioRunner.Run(content, scenario, Duration(args, 60), Recording(args), Sample(args), sequences: sequences, shows: shows));
    }

    private static int Report(ScenarioReport report)
    {
        foreach (var line in report.Lines)
        {
            Console.WriteLine(line);
        }

        foreach (var rejection in report.Rejections)
        {
            Console.WriteLine(rejection);
        }

        Console.WriteLine(string.Create(CultureInfo.CurrentCulture, $"{report.Frames} trames calculées."));
        return report.Rejections.Count == 0 ? 0 : 1;
    }

    private static string? Folder(Arguments args)
    {
        var folder = args.Positional.Count > 0 ? Path.GetFullPath(args.Positional[0]) : null;
        if (folder is null || !Directory.Exists(folder))
        {
            Console.Error.WriteLine("Dossier du projet introuvable.");
            return null;
        }

        return folder;
    }

    private static TimeSpan Duration(Arguments args, double fallback) => TimeSpan.FromSeconds(args.GetDouble("duree", fallback));

    private static TimeSpan Sample(Arguments args) => TimeSpan.FromSeconds(args.GetDouble("pas", 0.25));

    private static string? Recording(Arguments args) => args.Get("enregistrer") is { } file ? Path.GetFullPath(file) : null;
}
