using System.Globalization;
using Luxia.Fixtures.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Library;

/// <summary>Position d'un mode dans la liste des canaux.</summary>
/// <param name="position">Position (1 = adresse de l'appareil).</param>
/// <param name="slot">Position dans le modèle.</param>
/// <param name="definition">Définition du canal (null si la clé est inconnue).</param>
public sealed class SlotViewModel(int position, ModeChannel slot, ChannelDefinition? definition) : ViewModelBase
{
    /// <summary>Position (1 = adresse).</summary>
    public int Position { get; } = position;

    /// <summary>Position dans le modèle.</summary>
    public ModeChannel Slot { get; } = slot;

    /// <summary>Définition.</summary>
    public ChannelDefinition? Definition { get; } = definition;

    /// <summary>Numéro affiché.</summary>
    public string PositionText => Position.ToString(CultureInfo.CurrentCulture);

    /// <summary>Nom affiché.</summary>
    public string Title => Definition is null
        ? $"? {Slot.Channel}"
        : Slot.Part == ChannelPart.Fine ? $"{Definition.Name} (fin)" : Definition.Name;

    /// <summary>Attribut, cellule et résolution.</summary>
    public string Detail => Definition is null
        ? "canal inconnu"
        : string.Create(
            CultureInfo.CurrentCulture,
            $"{AttributeCatalog.Label(Definition.Attribute)}{(Definition.Cell > 0 ? $" · cellule {Definition.Cell}" : string.Empty)}{(Definition.Resolution == ChannelResolution.Bit16 ? " · 16 bits" : string.Empty)}");
}
