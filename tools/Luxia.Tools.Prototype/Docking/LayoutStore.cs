using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using Dock.Model.Controls;
using Dock.Serializer.SystemTextJson;
using Luxia.Persistence;
using Luxia.Persistence.Json;

namespace Luxia.Tools.Prototype.Docking;

/// <summary>
/// Enregistrement des dispositions (ERG-002) : un fichier par disposition prête, dans
/// <c>%AppData%\LuXia\prototype\</c>. Un fichier illisible est mis de côté et la disposition par défaut reprend
/// la main, avec un message : jamais d'échec au démarrage à cause de la disposition.
/// </summary>
internal sealed class LayoutStore(string folder)
{
    private readonly DockSerializer _serializer = new(typeof(ObservableCollection<>));

    /// <summary>Dossier des fichiers.</summary>
    public string Folder { get; } = folder;

    /// <summary>Emplacement de l'utilisateur (ou celui de <c>LUXIA_DOSSIER_DONNEES</c>).</summary>
    public static LayoutStore ForCurrentUser() => new(Path.Combine(DataPaths.Current.AppDataRoot, "prototype"));

    /// <summary>Fichier d'une disposition.</summary>
    public string PathFor(LayoutPreset preset) =>
        Path.Combine(Folder, preset == LayoutPreset.Show ? "disposition-spectacle.json" : "disposition-controle.json");

    /// <summary>Texte de la disposition, tel que Dock l'écrit (sert aussi à savoir si elle a changé).</summary>
    public string Serialize(IRootDock layout) => _serializer.Serialize(layout);

    /// <summary>Enregistre une disposition.</summary>
    public void Save(LayoutPreset preset, IRootDock layout) => SaveSerialized(preset, Serialize(layout));

    /// <summary>Enregistre une disposition déjà mise en texte par <see cref="Serialize"/>.</summary>
    public void SaveSerialized(LayoutPreset preset, string serialized)
    {
        var dock = JsonNode.Parse(serialized) as JsonObject
            ?? throw new InvalidOperationException("La disposition Dock n'est pas un objet JSON.");
        VersionedJsonFile.Save(PathFor(preset), new LayoutFile { Preset = preset.ToString(), Dock = dock }, LayoutFile.Type);
    }

    /// <summary>
    /// Relit une disposition. Nul si le fichier est absent (disposition par défaut, sans message) ou illisible
    /// (<paramref name="message"/> dit pourquoi et où il a été mis de côté).
    /// </summary>
    public IRootDock? Load(LayoutPreset preset, out string? message)
    {
        var path = PathFor(preset);
        var result = VersionedJsonFile.Load(path, LayoutFile.Type);
        message = result.Status == LoadStatus.Missing ? null : result.Message;
        if (result.Value is not { } file)
        {
            return null;
        }

        try
        {
            var layout = _serializer.Deserialize<IRootDock?>(file.Dock.ToJsonString());
            if (layout is null)
            {
                message = $"Disposition « {Path.GetFileName(path)} » vide : disposition par défaut.";
            }

            return layout;
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidOperationException or NotSupportedException)
        {
            message = $"Disposition « {Path.GetFileName(path)} » illisible ({ex.Message}) : disposition par défaut.";
            return null;
        }
    }

    /// <summary>Oublie une disposition enregistrée (retour à la disposition par défaut).</summary>
    public void Delete(LayoutPreset preset)
    {
        var path = PathFor(preset);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
