using System.Text.Json.Nodes;
using Luxia.Persistence.Json;

namespace Luxia.Tools.Prototype.Docking;

/// <summary>
/// Fichier d'une disposition (ERG-002) : l'enveloppe versionnée de LuXia (GEN-051) autour de la disposition écrite
/// par le sérialiseur de Dock, gardée telle quelle.
/// </summary>
internal sealed record LayoutFile
{
    /// <summary>Type de document (format 1).</summary>
    public static DocumentType<LayoutFile> Type { get; } = new("disposition", 1, []);

    /// <summary>Disposition concernée (<see cref="LayoutPreset"/>).</summary>
    public required string Preset { get; init; }

    /// <summary>Disposition Dock (arbre des panneaux, proportions, fenêtres détachées, panneaux masqués).</summary>
    public required JsonObject Dock { get; init; }
}
