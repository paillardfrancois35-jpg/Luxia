using Luxia.Fixtures;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Library;

/// <summary>Modèle dans la liste.</summary>
/// <param name="entry">Entrée de la bibliothèque.</param>
public sealed class LibraryItemViewModel(LibraryEntry entry) : ViewModelBase
{
    /// <summary>Entrée.</summary>
    public LibraryEntry Entry { get; } = entry;

    /// <summary>Nom du modèle.</summary>
    public string Name => Entry.Fixture.Model;

    /// <summary>Catégorie et modes.</summary>
    public string Detail => $"{Choices.Label(Entry.Fixture.Category)} · {string.Join(", ", Entry.Fixture.Modes.Select(m => m.ShortName ?? m.Name))}{(Entry.IsBuiltIn ? " · générique" : string.Empty)}";
}
