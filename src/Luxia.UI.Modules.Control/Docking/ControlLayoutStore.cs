using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using Dock.Model.Controls;
using Dock.Serializer.SystemTextJson;
using Luxia.Persistence.Json;

namespace Luxia.UI.Modules.Control.Docking;

/// <summary>Fichier d'une disposition de panneaux : enveloppe versionnée LuXia autour du texte de Dock (GEN-051).</summary>
public sealed record ControlLayoutFile
{
    /// <summary>Type de document (format 1).</summary>
    public static DocumentType<ControlLayoutFile> Type { get; } = new("disposition", 1, []);

    /// <summary>Disposition Dock (arbre des panneaux, proportions, fenêtres détachées, panneaux fermés).</summary>
    public required JsonObject Dock { get; init; }
}

/// <summary>
/// Enregistrement de la disposition de l'écran Contrôle (ERG-002, C10 : sur le poste, <c>%AppData%\LuXia\dispositions</c>).
/// Un fichier illisible est mis de côté et la disposition livrée reprend, avec un message : jamais d'échec au
/// démarrage à cause de la disposition.
/// </summary>
public sealed class ControlLayoutStore(string folder)
{
    private readonly DockSerializer _serializer = new(typeof(ObservableCollection<>));

    /// <summary>Fichier de la disposition.</summary>
    public string Path => System.IO.Path.Combine(folder, "controle.json");

    /// <summary>Texte de la disposition tel que Dock l'écrit (sert aussi à savoir si elle a changé).</summary>
    public string Serialize(IRootDock layout) => _serializer.Serialize(layout);

    /// <summary>Enregistre une disposition déjà mise en texte.</summary>
    public void Save(string serialized)
    {
        var dock = JsonNode.Parse(serialized) as JsonObject ?? throw new InvalidOperationException("La disposition Dock n'est pas un objet JSON.");
        VersionedJsonFile.Save(Path, new ControlLayoutFile { Dock = dock }, ControlLayoutFile.Type);
    }

    /// <summary>Relit la disposition : nulle si absente (sans message) ou illisible (avec message).</summary>
    public IRootDock? Load(out string? message)
    {
        var result = VersionedJsonFile.Load(Path, ControlLayoutFile.Type);
        message = result.Status == LoadStatus.Missing ? null : result.Message;
        if (result.Value is not { } file)
        {
            return null;
        }

        try
        {
            return _serializer.Deserialize<IRootDock?>(file.Dock.ToJsonString());
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidOperationException or NotSupportedException)
        {
            message = $"Disposition des panneaux illisible ({ex.Message}) : disposition livrée.";
            return null;
        }
    }

    /// <summary>Oublie la disposition enregistrée.</summary>
    public void Delete()
    {
        if (File.Exists(Path))
        {
            File.Delete(Path);
        }
    }
}
