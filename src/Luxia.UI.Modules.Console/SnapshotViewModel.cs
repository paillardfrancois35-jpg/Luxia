using System.Globalization;
using Luxia.Core.Snapshots;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Console;

/// <summary>Instantané de console dans la liste (CONS-010).</summary>
public sealed class SnapshotViewModel(ConsoleSnapshot snapshot) : ViewModelBase
{
    /// <summary>Instantané.</summary>
    public ConsoleSnapshot Snapshot { get; } = snapshot;

    /// <summary>Nom.</summary>
    public string Name => Snapshot.Name;

    /// <summary>Détail : catégorie, univers, nombre de canaux.</summary>
    public string Detail => string.Create(
        CultureInfo.CurrentCulture,
        $"{(Snapshot.Category is { } c ? c + " – " : string.Empty)}univers {Snapshot.Universe}, {Snapshot.Channels.Count} canaux");

    /// <summary>Description (ce qu'on doit observer).</summary>
    public string? Description => Snapshot.Description;
}
