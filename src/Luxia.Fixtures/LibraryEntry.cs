using Luxia.Fixtures.Model;

namespace Luxia.Fixtures;

/// <summary>Modèle chargé depuis la bibliothèque.</summary>
/// <param name="Fixture">Modèle.</param>
/// <param name="FilePath">Fichier (null pour un générique livré avec l'application).</param>
public sealed record LibraryEntry(FixtureType Fixture, string? FilePath)
{
    /// <summary>Générique en lecture seule (à dupliquer pour le modifier).</summary>
    public bool IsBuiltIn => FilePath is null;
}
