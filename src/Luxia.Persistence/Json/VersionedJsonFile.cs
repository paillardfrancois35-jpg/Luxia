using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Luxia.Persistence.Json;

/// <summary>
/// Lecture / écriture d'un fichier JSON versionné (GEN-050, GEN-051, GEN-056).
/// Le numéro de version est la propriété <c>formatVersion</c>, écrite en premier.
/// </summary>
public static class VersionedJsonFile
{
    /// <summary>Nom de la propriété de version.</summary>
    public const string VersionProperty = "formatVersion";

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Charge un fichier : migre les anciennes versions (copie de l'original en <c>.vN.bak</c>, fichier réécrit),
    /// met de côté un fichier illisible (renommé en <c>.illisible-horodatage</c>), refuse un format plus récent.
    /// </summary>
    public static LoadResult<T> Load<T>(string path, DocumentType<T> type, TimeProvider? time = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(type);
        if (!File.Exists(path))
        {
            return new LoadResult<T>(LoadStatus.Missing, null, $"Fichier {type.Name} absent : {path}", null);
        }

        JsonObject document;
        int version;
        try
        {
            var text = File.ReadAllText(path, Encoding.UTF8);
            document = JsonNode.Parse(text, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }) as JsonObject
                ?? throw new JsonException("Le fichier ne contient pas un objet JSON.");
            version = document[VersionProperty]?.GetValue<int>()
                ?? throw new JsonException($"Propriété « {VersionProperty} » absente.");
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or IOException or DecoderFallbackException)
        {
            return SetAside<T>(path, type, ex.Message, time);
        }

        if (version > type.CurrentVersion)
        {
            return new LoadResult<T>(
                LoadStatus.TooRecent,
                null,
                $"Le fichier {type.Name} « {Path.GetFileName(path)} » est au format {version}, plus récent que cette application (format {type.CurrentVersion}). Il n'a pas été modifié.",
                null);
        }

        string? backup = null;
        if (version < type.CurrentVersion)
        {
            try
            {
                for (var v = version; v < type.CurrentVersion; v++)
                {
                    var migration = type.Migrations.SingleOrDefault(m => m.FromVersion == v)
                        ?? throw new InvalidOperationException($"Aucune migration du format {v} vers {v + 1}.");
                    migration.Apply(document);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or JsonException or FormatException)
            {
                return SetAside<T>(path, type, $"migration impossible : {ex.Message}", time);
            }

            document[VersionProperty] = type.CurrentVersion;
            backup = $"{path}.v{version}.bak";
            File.Copy(path, backup, overwrite: true);
        }

        T? value;
        try
        {
            value = document.Deserialize<T>(LuxiaJson.Options);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or NotSupportedException)
        {
            return SetAside<T>(path, type, ex.Message, time);
        }

        if (value is null)
        {
            return SetAside<T>(path, type, "contenu vide", time);
        }

        if (backup is not null)
        {
            Save(path, value, type);
            return new LoadResult<T>(
                LoadStatus.Migrated,
                value,
                $"Fichier {type.Name} migré du format {version} au format {type.CurrentVersion} ; original conservé : {Path.GetFileName(backup)}",
                backup);
        }

        return new LoadResult<T>(LoadStatus.Loaded, value, null, null);
    }

    /// <summary>Enregistre (écriture dans un fichier temporaire puis remplacement : pas de fichier à moitié écrit).</summary>
    public static void Save<T>(string path, T value, DocumentType<T> type)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(type);
        var body = JsonSerializer.SerializeToNode(value, LuxiaJson.Options) as JsonObject
            ?? throw new InvalidOperationException("Les données ne forment pas un objet JSON.");

        var document = new JsonObject { [VersionProperty] = type.CurrentVersion };
        foreach (var (key, node) in body.ToList())
        {
            if (key == VersionProperty)
            {
                continue;
            }

            body.Remove(key);
            document[key] = node;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        var temp = path + ".tmp";
        File.WriteAllText(temp, document.ToJsonString(LuxiaJson.Options) + "\n", Utf8NoBom);
        File.Move(temp, path, overwrite: true);
    }

    private static LoadResult<T> SetAside<T>(string path, DocumentType<T> type, string reason, TimeProvider? time)
        where T : class
    {
        var stamp = (time ?? TimeProvider.System).GetLocalNow().ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var aside = $"{path}.illisible-{stamp}";
        try
        {
            File.Move(path, aside, overwrite: true);
        }
        catch (IOException)
        {
            aside = path;
        }

        return new LoadResult<T>(
            LoadStatus.Invalid,
            null,
            $"Fichier {type.Name} « {Path.GetFileName(path)} » illisible ({reason}). Il a été mis de côté : {Path.GetFileName(aside)}",
            aside);
    }
}
