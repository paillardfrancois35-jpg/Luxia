using System.Text.Json;
using System.Text.Json.Nodes;
using Luxia.Fixtures.Model;
using Luxia.Persistence.Json;

namespace Luxia.Fixtures.Import;

/// <summary>
/// Import de fichiers ou de dossiers entiers (BIB-083) : reconnaît le format de chaque fichier
/// (modèle de l'application, OFL, QLC+) et produit un rapport par fichier. Ne bloque jamais sur un fichier.
/// </summary>
public static class FixtureImporter
{
    /// <summary>Extensions reconnues.</summary>
    public static readonly IReadOnlyList<string> Extensions = [".json", ".qxf"];

    /// <summary>Fichiers importables d'un dossier (récursif).</summary>
    public static IReadOnlyList<string> FindFiles(string folder) =>
        [.. Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
            .Where(f => Extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Where(f => !Path.GetFileName(f).Equals("manufacturers.json", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase)];

    /// <summary>
    /// Importe une liste de fichiers ; à appeler hors du fil de l'interface (GEN-109).
    /// La progression est publiée à chaque fichier.
    /// </summary>
    public static IReadOnlyList<ImportResult> ImportFiles(IReadOnlyList<string> files, IProgress<(int Done, int Total)>? progress = null, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(files);
        var results = new List<ImportResult>(files.Count);
        for (var i = 0; i < files.Count; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            results.Add(ImportFile(files[i]));
            progress?.Report((i + 1, files.Count));
        }

        return results;
    }

    /// <summary>Importe un fichier, quel que soit son format reconnu.</summary>
    public static ImportResult ImportFile(string path)
    {
        if (Path.GetExtension(path).Equals(".qxf", StringComparison.OrdinalIgnoreCase))
        {
            return QlcImporter.ImportFile(path);
        }

        try
        {
            var document = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
            if (document is null)
            {
                return new ImportResult(path, null, [], "pas un objet JSON");
            }

            if (OflImporter.IsOfl(document))
            {
                return OflImporter.ImportFile(path);
            }

            if (document[VersionedJsonFile.VersionProperty] is not null && document["manufacturer"] is not null)
            {
                var fixture = document.Deserialize<FixtureType>(LuxiaJson.Options);
                return fixture is null
                    ? new ImportResult(path, null, [], "modèle vide")
                    : new ImportResult(path, fixture, []);
            }

            return new ImportResult(path, null, [], "format non reconnu (ni modèle LuXia, ni OFL)");
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidOperationException or NotSupportedException)
        {
            return new ImportResult(path, null, [], $"fichier illisible : {ex.Message}");
        }
    }
}
