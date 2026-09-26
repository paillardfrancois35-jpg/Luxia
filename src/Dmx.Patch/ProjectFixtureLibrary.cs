using Dmx.Fixtures;
using Dmx.Fixtures.Model;
using Dmx.Persistence.Json;

namespace Dmx.Patch;

/// <summary>
/// Copie des modèles d'appareils utilisés par le projet (GEN-053) : dossier <c>Bibliothèque</c> du projet,
/// même format que la bibliothèque partagée (<see cref="FixtureLibrary"/>), mais gérée séparément.
/// Une modification de la bibliothèque partagée n'est répercutée ici que sur action explicite
/// (<see cref="UpdateFrom"/>), jamais automatiquement.
/// </summary>
public sealed class ProjectFixtureLibrary
{
    /// <summary>Nom du sous-dossier, dans le dossier du projet.</summary>
    public const string FolderName = "Bibliothèque";

    private readonly FixtureLibrary _inner;

    /// <summary>Ouvre (ou crée) la copie du projet <paramref name="projectFolder"/>.</summary>
    public ProjectFixtureLibrary(string projectFolder)
    {
        ArgumentNullException.ThrowIfNull(projectFolder);
        _inner = new FixtureLibrary(Path.Combine(projectFolder, FolderName));
        _inner.Load();
    }

    /// <summary>Modèles copiés dans le projet.</summary>
    public IReadOnlyList<LibraryEntry> Entries => _inner.Entries;

    /// <summary>Modèle par identifiant.</summary>
    public FixtureType? Find(Guid id) => _inner.Entries.FirstOrDefault(e => e.Fixture.Id == id)?.Fixture;

    /// <summary>
    /// Copie <paramref name="fixture"/> dans le projet s'il n'y est pas déjà (GEN-053) : version conservée telle
    /// quelle (contrairement à <see cref="FixtureLibrary.Save"/>, qui incrémenterait la version d'une copie existante).
    /// Sans effet si un modèle de même identifiant est déjà présent : voir <see cref="UpdateFrom"/> pour le mettre à jour.
    /// </summary>
    public void EnsureCopied(FixtureType fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        if (Find(fixture.Id) is not null)
        {
            return;
        }

        VersionedJsonFile.Save(_inner.PathFor(fixture), fixture, FixtureLibrary.DocumentType);
        _inner.Load();
    }

    /// <summary>
    /// Remplace la copie du projet par <paramref name="fixture"/> (GEN-053, mise à jour explicite) : la version
    /// de <paramref name="fixture"/> est reprise telle quelle (pas d'incrément, à la différence d'un enregistrement
    /// depuis l'éditeur). Le rapport d'impact (canaux perdus / gagnés par mode) est du ressort de l'appelant
    /// (<see cref="Rules.FixtureUpdateImpact"/>), avant d'appeler cette méthode.
    /// </summary>
    public void UpdateFrom(FixtureType fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        VersionedJsonFile.Save(_inner.PathFor(fixture), fixture, FixtureLibrary.DocumentType);
        _inner.Load();
    }
}
