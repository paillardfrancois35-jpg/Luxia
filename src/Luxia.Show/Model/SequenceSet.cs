namespace Luxia.Show.Model;

/// <summary>Séquences d'un projet, enregistrées dans <c>séquences.json</c> (doc 50).</summary>
public sealed record SequenceSet
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Séquences, dans l'ordre d'affichage.</summary>
    public IReadOnlyList<Sequence> Sequences { get; init; } = [];
}
