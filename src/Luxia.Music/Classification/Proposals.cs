using Luxia.Music.Base;
using Luxia.Music.Normalization;

namespace Luxia.Music.Classification;

/// <summary>Une étiquette de genre d'une source en ligne (« house », 87).</summary>
/// <param name="Name">Étiquette.</param>
/// <param name="Count">Poids donné par la source (nombre de votes ou score de 0 à 100).</param>
public sealed record TagCount(string Name, int Count);

/// <summary>Une proposition de famille pour un artiste, trouvée en ligne par l'outil d'enrichissement (MUS-040) ; à valider (MUS-041).</summary>
public sealed record Proposal
{
    /// <summary>Artiste.</summary>
    public string Artist { get; init; } = string.Empty;

    /// <summary>Famille proposée.</summary>
    public string Style { get; init; } = string.Empty;

    /// <summary>Confiance de 0 à 1 (part de la famille parmi les étiquettes reconnues, et nombre d'étiquettes).</summary>
    public double Confidence { get; init; }

    /// <summary>Source (MusicBrainz, Last.fm).</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>Étiquettes brutes de la source, pour juger.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>Propositions en attente de validation, enregistrées dans <c>propositions.json</c> du projet.</summary>
public sealed record ProposalSet
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Propositions.</summary>
    public IReadOnlyList<Proposal> Items { get; init; } = [];

    /// <summary>Artistes dont la proposition a été rejetée : l'outil ne les repropose pas.</summary>
    public IReadOnlyList<string> Rejected { get; init; } = [];
}

/// <summary>Passage des étiquettes de genre d'une source en ligne à une famille de la taxonomie.</summary>
public static class TagMapper
{
    /// <summary>
    /// Choisit la famille dont les étiquettes pèsent le plus. Confiance = part de cette famille parmi les poids reconnus, réduite quand
    /// les étiquettes sont peu nombreuses ou peu votées (au plus 0,9 : une source en ligne ne vaut pas une correction).
    /// </summary>
    /// <param name="tags">Étiquettes de la source.</param>
    /// <param name="musicBase">Base (taxonomie et étiquettes de genre par famille).</param>
    /// <returns>La famille et la confiance, ou <c>null</c> si aucune étiquette n'est reconnue.</returns>
    public static (MusicFamily Family, double Confidence)? Map(IEnumerable<TagCount> tags, MusicBase musicBase)
    {
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(musicBase);
        var weights = new Dictionary<string, (MusicFamily Family, double Weight)>(StringComparer.Ordinal);
        double total = 0;
        foreach (var tag in tags)
        {
            var weight = Math.Max(1, tag.Count);
            total += weight;
            if (musicBase.FindFamily(TextKey.Of(tag.Name)) is { } family)
            {
                weights[family.Id] = weights.TryGetValue(family.Id, out var old) ? (family, old.Weight + weight) : (family, weight);
            }
        }

        if (weights.Count == 0 || total <= 0)
        {
            return null;
        }

        var best = weights.Values.OrderByDescending(w => w.Weight).ThenBy(w => w.Family.Id, StringComparer.Ordinal).First();
        var recognized = weights.Values.Sum(w => w.Weight);
        var share = best.Weight / recognized;
        var coverage = Math.Min(1.0, recognized / 5.0);
        return (best.Family, Math.Round(Math.Min(0.9, share * (0.5 + (0.5 * coverage))), 2));
    }
}
