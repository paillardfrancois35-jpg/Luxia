using System.Globalization;
using Luxia.Music.Base;
using Luxia.Music.Classification;
using Luxia.Music.Enrichment;
using Luxia.Persistence;

// Enrichissement de la base musicale (MUS-040 à MUS-042) : à lancer à la maison, jamais en soirée (GEN-121).
//   luxia-enrich "<dossier du projet>" [--max 100] [--lastfm <clé>] [--cache <dossier>] [--hors-ligne]
// Pour les artistes « Inconnu » de la base, interroge MusicBrainz (et Last.fm avec une clé
// personnelle), convertit les étiquettes de genre en famille et ÉCRIT DES PROPOSITIONS dans propositions.json : rien n'entre dans la
// base sans validation (LuXia → Base… → onglet « Propositions »).
Console.OutputEncoding = System.Text.Encoding.UTF8;
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
var options = ParseOptions(args);
if (options.Project is null || !Directory.Exists(options.Project))
{
    Console.Error.WriteLine("Usage : luxia-enrich \"<dossier du projet>\" [--max 100] [--lastfm <clé>] [--cache <dossier>] [--hors-ligne]");
    return 1;
}

var paths = DataPaths.Current;
var (musicBase, messages) = MusicStore.Load(options.Project);
var previous = MusicStore.LoadProposals(options.Project);
foreach (var message in messages)
{
    Console.WriteLine("Base musicale : " + message);
}

// Les artistes à classer sont ceux de la base au style « Inconnu » (morceaux joués d'artistes absents de la base, playlists importées).
var artists = musicBase.SearchArtists(null, int.MaxValue, Taxonomy.UnknownId)
    .Select(a => a.Name)
    .Where(a => !previous.Rejected.Contains(a, StringComparer.OrdinalIgnoreCase))
    .ToList();
Console.WriteLine($"{artists.Count} artiste(s) « Inconnu » à classer.");
if (artists.Count == 0)
{
    Console.WriteLine("Rien à proposer.");
    return 0;
}

var cache = options.Cache ?? Path.Combine(paths.AppDataRoot, "cache-enrichissement");
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
var sources = new List<ITagSource> { new CachedTagSource(new MusicBrainzSource(http), cache, options.Offline) };
if (options.LastFmKey is { Length: > 0 } key)
{
    sources.Add(new CachedTagSource(new LastFmSource(http, key), cache, options.Offline));
}

Console.WriteLine($"Sources : {string.Join(", ", sources.Select(s => s.Name))}{(options.Offline ? " (cache seul, hors ligne)" : string.Empty)}. Cache : {cache}");
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};
var enricher = new Enricher(sources, musicBase);
IReadOnlyList<Proposal> proposals;
try
{
    proposals = await enricher.ProposeAsync(artists, options.Max, new Progress<string>(Console.WriteLine), cancellation.Token).ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Interrompu : rien n'est enregistré.");
    return 2;
}

var existing = previous.Items.Where(p => !proposals.Any(n => string.Equals(n.Artist, p.Artist, StringComparison.OrdinalIgnoreCase))).ToList();
MusicStore.SaveProposals(options.Project, previous with { Items = [.. existing, .. proposals] });
if (enricher.Unanswered.Count > 0)
{
    Console.WriteLine($"⚠ {enricher.Unanswered.Count} requête(s) sans réponse (pas « aucune proposition » : réseau, limite de débit ou, hors ligne, absent du cache) : {string.Join(", ", enricher.Unanswered)}. Relancez la commande pour les retenter.");
}

Console.WriteLine($"{proposals.Count} proposition(s) enregistrée(s) dans {Path.Combine(options.Project, MusicStore.ProposalsFile)}.");
Console.WriteLine("À valider dans LuXia : bouton « Base… » de l'écran de jeu, onglet « Propositions » (accepter, modifier ou rejeter).");
return 0;

static Options ParseOptions(string[] args)
{
    string? project = null;
    string? cache = null;
    string? lastFm = null;
    var max = 100;
    var offline = false;
    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--max" when i + 1 < args.Length:
                _ = int.TryParse(args[++i], NumberStyles.Integer, CultureInfo.InvariantCulture, out max);
                break;
            case "--cache" when i + 1 < args.Length:
                cache = args[++i];
                break;
            case "--lastfm" when i + 1 < args.Length:
                lastFm = args[++i];
                break;
            case "--hors-ligne":
                offline = true;
                break;
            default:
                project ??= args[i];
                break;
        }
    }

    return new Options(project is null ? null : Path.GetFullPath(project), Math.Max(1, max), cache, lastFm, offline);
}

internal sealed record Options(string? Project, int Max, string? Cache, string? LastFmKey, bool Offline);
