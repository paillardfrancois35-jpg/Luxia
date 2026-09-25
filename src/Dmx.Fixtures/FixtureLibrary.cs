using System.Text;
using Dmx.Fixtures.Model;
using Dmx.Persistence.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dmx.Fixtures;

/// <summary>
/// Bibliothèque d'appareils : un fichier JSON par modèle, <c>&lt;fabricant&gt;/&lt;modèle&gt;.json</c> (doc 12 §8),
/// plus les génériques livrés (doc 12 §7). Un fichier illisible est mis de côté sans empêcher le reste (GEN-056).
/// </summary>
public sealed class FixtureLibrary
{
    /// <summary>Type de document « modèle d'appareil ».</summary>
    public static readonly DocumentType<FixtureType> DocumentType = new("modèle d'appareil", FixtureType.CurrentFormatVersion, []);

    private readonly ILogger _logger;
    private readonly List<LibraryEntry> _entries = [];

    /// <summary>Crée la bibliothèque sur un dossier (créé si besoin).</summary>
    public FixtureLibrary(string folder, ILogger<FixtureLibrary>? logger = null)
    {
        Folder = folder;
        _logger = logger ?? NullLogger<FixtureLibrary>.Instance;
    }

    /// <summary>Dossier de la bibliothèque.</summary>
    public string Folder { get; }

    /// <summary>Modèles : génériques puis fichiers, triés par fabricant et modèle.</summary>
    public IReadOnlyList<LibraryEntry> Entries => _entries;

    /// <summary>Messages du dernier chargement (fichiers mis de côté, migrations).</summary>
    public IReadOnlyList<string> Messages { get; private set; } = [];

    /// <summary>(Re)charge tous les modèles.</summary>
    public void Load()
    {
        _entries.Clear();
        var messages = new List<string>();
        _entries.AddRange(GenericFixtures.All.Select(f => new LibraryEntry(f, null)));

        if (Directory.Exists(Folder))
        {
            foreach (var file in Directory.EnumerateFiles(Folder, "*.json", SearchOption.AllDirectories))
            {
                var result = VersionedJsonFile.Load(file, DocumentType);
                if (result.Succeeded)
                {
                    _entries.Add(new LibraryEntry(result.Value!, file));
                }

                if (result.Message is not null)
                {
                    messages.Add(result.Message);
                    _logger.LogWarning("{Message}", result.Message);
                }
            }
        }

        Sort();
        Messages = messages;
    }

    /// <summary>
    /// Enregistre un modèle : version incrémentée (BIB-009), fichier <c>fabricant/modèle.json</c>.
    /// Renommer le fabricant ou le modèle déplace le fichier.
    /// </summary>
    public LibraryEntry Save(FixtureType fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        var existing = _entries.FirstOrDefault(e => e.Fixture.Id == fixture.Id && !e.IsBuiltIn);
        var saved = existing is null ? fixture : fixture with { Version = existing.Fixture.Version + 1 };
        var path = PathFor(saved);
        VersionedJsonFile.Save(path, saved, DocumentType);

        if (existing?.FilePath is { } oldPath && !string.Equals(Path.GetFullPath(oldPath), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(oldPath);
        }

        _entries.RemoveAll(e => e.Fixture.Id == saved.Id && !e.IsBuiltIn);
        var entry = new LibraryEntry(saved, path);
        _entries.Add(entry);
        Sort();
        _logger.LogInformation("Modèle enregistré : {Modele} v{Version}", saved.DisplayName, saved.Version);
        return entry;
    }

    /// <summary>Supprime le fichier d'un modèle (les génériques ne se suppriment pas).</summary>
    public void Delete(Guid id)
    {
        var entry = _entries.FirstOrDefault(e => e.Fixture.Id == id && !e.IsBuiltIn);
        if (entry?.FilePath is { } path)
        {
            File.Delete(path);
            _entries.Remove(entry);
            _logger.LogInformation("Modèle supprimé : {Modele}", entry.Fixture.DisplayName);
        }
    }

    /// <summary>Chemin du fichier d'un modèle.</summary>
    public string PathFor(FixtureType fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        return Path.Combine(Folder, SafeName(fixture.Manufacturer), SafeName(fixture.Model) + ".json");
    }

    /// <summary>Nom de fichier sans caractère interdit.</summary>
    public static string SafeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder();
        foreach (var c in (name ?? string.Empty).Trim())
        {
            builder.Append(invalid.Contains(c) ? '_' : c);
        }

        return builder.Length == 0 ? "_" : builder.ToString();
    }

    private void Sort() => _entries.Sort((a, b) =>
    {
        var byManufacturer = string.Compare(a.Fixture.Manufacturer, b.Fixture.Manufacturer, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.CompareOptions.IgnoreCase);
        return byManufacturer != 0 ? byManufacturer : string.Compare(a.Fixture.Model, b.Fixture.Model, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.CompareOptions.IgnoreCase);
    });
}
