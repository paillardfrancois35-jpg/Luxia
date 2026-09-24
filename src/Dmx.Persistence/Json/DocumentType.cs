using System.Text.Json.Nodes;

namespace Dmx.Persistence.Json;

/// <summary>Migration d'un document d'une version de format à la suivante (GEN-051).</summary>
public interface IJsonMigration
{
    /// <summary>Version lue ; le document produit est en version <c>FromVersion + 1</c>.</summary>
    int FromVersion { get; }

    /// <summary>Transforme le document en place.</summary>
    void Apply(JsonObject document);
}

/// <summary>Migration écrite sous forme de fonction.</summary>
/// <param name="FromVersion">Version lue.</param>
/// <param name="Transform">Transformation en place.</param>
public sealed record JsonMigration(int FromVersion, Action<JsonObject> Transform) : IJsonMigration
{
    /// <inheritdoc />
    public void Apply(JsonObject document) => Transform(document);
}

/// <summary>
/// Description d'un type de fichier versionné : nom lisible, version courante, migrations successives.
/// </summary>
/// <typeparam name="T">Type des données.</typeparam>
/// <param name="Name">Nom affiché dans les messages (« préférences », « projet »…).</param>
/// <param name="CurrentVersion">Version écrite par cette version de l'application.</param>
/// <param name="Migrations">Migrations de la version 1 à la version courante.</param>
public sealed record DocumentType<T>(string Name, int CurrentVersion, IReadOnlyList<IJsonMigration> Migrations)
    where T : class
{
    /// <summary>Crée un type sans migration (version 1).</summary>
    public DocumentType(string name)
        : this(name, 1, [])
    {
    }
}
