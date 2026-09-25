using System.Text;
using Dmx.Fixtures.Model;
using Dmx.Persistence.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dmx.Fixtures;

/// <summary>Modèle chargé depuis la bibliothèque.</summary>
/// <param name="Fixture">Modèle.</param>
/// <param name="FilePath">Fichier (null pour un générique livré avec l'application).</param>
public sealed record LibraryEntry(FixtureType Fixture, string? FilePath)
{
    /// <summary>Générique en lecture seule (à dupliquer pour le modifier).</summary>
    public bool IsBuiltIn => FilePath is null;
}
