using Luxia.Scenes.Model;

namespace Luxia.UI.Modules.Live;

/// <summary>Palette rapide en Live (LIVE-005).</summary>
/// <param name="Palette">Palette.</param>
public sealed record QuickPaletteViewModel(Palette Palette)
{
    /// <summary>Nom.</summary>
    public string Name => Palette.Name;

    /// <summary>Couleur du bouton.</summary>
    public string Color => Palette.DisplayColor();
}
