using Luxia.Patch;
using Luxia.Scenes;
using Luxia.Scenes.Compilation;

namespace Luxia.Hosting.Tools;

/// <summary>Lecture d'un projet sur disque pour les outils sans interface (GEN-131, GEN-132).</summary>
public static class ProjectFiles
{
    /// <summary>Contenu d'un projet (fichiers absents = valeurs par défaut, comme à l'ouverture dans l'application).</summary>
    public static ProjectContent Load(string folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        var library = new ProjectFixtureLibrary(folder);
        return new ProjectContent(
            InstallationStore.Load(folder).Value,
            VenueStore.Load(folder).Value,
            library.Find,
            LayerStore.Load(folder).Value,
            SceneStore.Load(folder).Value,
            PaletteStore.Load(folder).Value,
            SafetyStore.Load(folder).Value);
    }
}
