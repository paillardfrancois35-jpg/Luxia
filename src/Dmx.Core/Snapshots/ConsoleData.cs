namespace Dmx.Core.Snapshots;

/// <summary>Contenu du fichier <c>console.json</c> d'un projet.</summary>
public sealed record ConsoleData
{
    /// <summary>Version courante du format.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Instantanés.</summary>
    public IReadOnlyList<ConsoleSnapshot> Snapshots { get; init; } = [];
}
