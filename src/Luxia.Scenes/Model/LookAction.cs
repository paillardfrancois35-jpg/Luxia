namespace Luxia.Scenes.Model;

/// <summary>Une action d'un look, jouée dans l'ordre de la liste.</summary>
public sealed record LookAction
{
    /// <summary>Genre d'action.</summary>
    public required LookActionKind Kind { get; init; }

    /// <summary>Scène visée (lancer, arrêter).</summary>
    public Guid? SceneId { get; init; }

    /// <summary>Couche visée (arrêter, master).</summary>
    public Guid? LayerId { get; init; }

    /// <summary>Niveau (0-1) pour un master.</summary>
    public double? Level { get; init; }
}
