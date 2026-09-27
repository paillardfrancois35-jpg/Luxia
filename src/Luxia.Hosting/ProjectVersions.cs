using System.Globalization;
using System.Security.Cryptography;

namespace Luxia.Hosting;

/// <summary>
/// Versions du projet (GEN-054, GEN-055) : copie des fichiers du projet dans <c>&lt;projet&gt;\Versions\AAAAMMJJ-HHMMSS</c>,
/// seulement si quelque chose a changé depuis la dernière version ; les <see cref="Keep"/> plus récentes sont gardées.
/// Toute modification étant déjà enregistrée au moment où elle est faite, ces versions servent à <b>revenir en arrière</b>.
/// </summary>
public static class ProjectVersions
{
    /// <summary>Dossier des versions, dans le projet.</summary>
    public const string FolderName = "Versions";

    /// <summary>Nombre de versions gardées (GEN-055).</summary>
    public const int Keep = 10;

    private const string ReasonFile = "motif.txt";

    /// <summary>Enregistre une version si le projet a changé depuis la dernière ; renvoie son nom, ou <c>null</c>.</summary>
    public static string? Save(string projectFolder, string reason, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(projectFolder);
        var files = ProjectFiles(projectFolder);
        if (files.Count == 0)
        {
            return null;
        }

        var root = Path.Combine(projectFolder, FolderName);
        var versions = List(projectFolder);
        var latest = versions.Count > 0 ? versions[0] : null;
        if (latest is not null && Hash(projectFolder, files) == Hash(Path.Combine(root, latest.Name), ProjectFiles(Path.Combine(root, latest.Name))))
        {
            return null;
        }

        var name = now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var target = Path.Combine(root, name);
        for (var suffix = 2; Directory.Exists(target); suffix++)
        {
            target = Path.Combine(root, string.Create(CultureInfo.InvariantCulture, $"{name}-{suffix}"));
        }

        foreach (var file in files)
        {
            var destination = Path.Combine(target, file);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(Path.Combine(projectFolder, file), destination);
        }

        File.WriteAllText(Path.Combine(target, ReasonFile), reason);
        foreach (var old in List(projectFolder).Skip(Keep))
        {
            Directory.Delete(Path.Combine(root, old.Name), recursive: true);
        }

        return Path.GetFileName(target);
    }

    /// <summary>Versions, de la plus récente à la plus ancienne.</summary>
    public static IReadOnlyList<ProjectVersion> List(string projectFolder)
    {
        ArgumentNullException.ThrowIfNull(projectFolder);
        var root = Path.Combine(projectFolder, FolderName);
        if (!Directory.Exists(root))
        {
            return [];
        }

        return [.. Directory.EnumerateDirectories(root)
            .Select(d =>
            {
                var reasonPath = Path.Combine(d, ReasonFile);
                var reason = File.Exists(reasonPath) ? File.ReadAllText(reasonPath).Trim() : string.Empty;
                return new ProjectVersion(Path.GetFileName(d), Directory.GetCreationTime(d), reason);
            })
            .OrderByDescending(v => v.Name, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Revient à une version (GEN-055) : l'état actuel est d'abord gardé comme version « avant restauration », puis les
    /// fichiers de la version remplacent ceux du projet (les parties absentes de la version sont retirées).
    /// </summary>
    public static void Restore(string projectFolder, string versionName, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(projectFolder);
        ArgumentNullException.ThrowIfNull(versionName);
        var source = Path.Combine(projectFolder, FolderName, versionName);
        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException($"Version introuvable : {versionName}");
        }

        Save(projectFolder, $"avant restauration de {versionName}", now);
        var wanted = ProjectFiles(source).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var file in ProjectFiles(projectFolder).Where(f => !wanted.Contains(f)))
        {
            File.Delete(Path.Combine(projectFolder, file));
        }

        foreach (var file in wanted)
        {
            var destination = Path.Combine(projectFolder, file);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(Path.Combine(source, file), destination, overwrite: true);
        }
    }

    /// <summary>Fichiers JSON du projet (chemins relatifs), hors versions et hors enregistrements.</summary>
    private static List<string> ProjectFiles(string folder) =>
        !Directory.Exists(folder)
            ? []
            : [.. Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(folder, f))
                .Where(f => !f.StartsWith(FolderName + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal)];

    private static string Hash(string folder, IReadOnlyList<string> files)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var file in files)
        {
            hash.AppendData(System.Text.Encoding.UTF8.GetBytes(file));
            hash.AppendData(File.ReadAllBytes(Path.Combine(folder, file)));
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
