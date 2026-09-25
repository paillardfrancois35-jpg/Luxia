namespace Dmx.Persistence.Json;

/// <summary>Issue du chargement d'un fichier.</summary>
public enum LoadStatus
{
    /// <summary>Chargé, déjà à la version courante.</summary>
    Loaded,

    /// <summary>Chargé après migration ; l'original est sauvegardé en <c>.vN.bak</c>.</summary>
    Migrated,

    /// <summary>Fichier absent.</summary>
    Missing,

    /// <summary>Fichier illisible ou incohérent, mis de côté (GEN-056).</summary>
    Invalid,

    /// <summary>Format plus récent que l'application : non chargé, non modifié.</summary>
    TooRecent,
}

/// <summary>Résultat du chargement : jamais d'exception pour un fichier défectueux (GEN-056).</summary>
/// <typeparam name="T">Type des données.</typeparam>
/// <param name="Status">Issue.</param>
/// <param name="Value">Données (null si non chargées).</param>
/// <param name="Message">Explication en français, destinée à l'utilisateur.</param>
/// <param name="SetAsidePath">Chemin du fichier mis de côté ou de la sauvegarde avant migration.</param>
public sealed record LoadResult<T>(LoadStatus Status, T? Value, string? Message, string? SetAsidePath)
    where T : class
{
    /// <summary>Les données sont utilisables.</summary>
    public bool Succeeded => Status is LoadStatus.Loaded or LoadStatus.Migrated;
}
