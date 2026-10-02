using Luxia.Persistence;
using Luxia.Persistence.Json;
using Luxia.Show.Model;

namespace Luxia.Show;

/// <summary>Lecture / écriture de <c>séquences.json</c> (doc 50, SHOW-001).</summary>
public static class SequenceStore
{
    /// <summary>Nom du fichier.</summary>
    public const string FileName = "séquences.json";

    /// <summary>Type de document « séquences ».</summary>
    public static readonly DocumentType<SequenceSet> DocumentType = new("séquences", SequenceSet.CurrentFormatVersion, []);

    /// <summary>Charge les séquences ; absent = aucune.</summary>
    public static (SequenceSet Value, string? Message) Load(string projectFolder) =>
        ProjectPartStore.Load(projectFolder, FileName, DocumentType, () => new SequenceSet());

    /// <summary>Enregistre les séquences.</summary>
    public static void Save(string projectFolder, SequenceSet sequences) =>
        ProjectPartStore.Save(projectFolder, FileName, sequences, DocumentType);
}
