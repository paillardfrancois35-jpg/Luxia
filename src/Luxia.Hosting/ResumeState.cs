namespace Luxia.Hosting;

/// <summary>
/// Instantané de reprise (MOT-102, GEN-095), écrit toutes les 5 s dans <c>%AppData%\LuXia\reprise.json</c> : de quoi
/// rejouer les mêmes scènes après un arrêt brutal. Un arrêt propre le marque « terminé ».
/// </summary>
public sealed record ResumeState
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Dossier du projet ouvert.</summary>
    public string? ProjectFolder { get; init; }

    /// <summary>Instant de l'instantané.</summary>
    public DateTime SavedAt { get; init; }

    /// <summary>L'application s'est arrêtée proprement après cet instantané.</summary>
    public bool CleanExit { get; init; }

    /// <summary>Scènes qui jouaient (hors flashs), dans l'ordre de fusion.</summary>
    public IReadOnlyList<Guid> Scenes { get; init; } = [];

    /// <summary>Masters des couches (couche → 0-1).</summary>
    public IReadOnlyDictionary<Guid, double> LayerMasters { get; init; } = new Dictionary<Guid, double>();

    /// <summary>Grand Master (0-1).</summary>
    public double GrandMaster { get; init; } = 1;

    /// <summary>Blackout actif.</summary>
    public bool Blackout { get; init; }

    /// <summary>Sortie figée.</summary>
    public bool Frozen { get; init; }
}
