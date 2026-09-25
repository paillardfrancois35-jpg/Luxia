using CommunityToolkit.Mvvm.ComponentModel;
using Dmx.Patch.Model;

namespace Dmx.UI.Modules.Installation;

/// <summary>Une sélection manuelle affichée (INST-030, INST-032).</summary>
public sealed partial class SelectionRowViewModel : ObservableObject
{
    /// <summary>Crée la ligne.</summary>
    public SelectionRowViewModel(Selection selection, Func<Guid, string> nameOf)
    {
        Selection = selection;
        Members = string.Join(" → ", selection.Items.Select(i => nameOf(i.FixtureId) + (i.Cell > 0 ? $" #{i.Cell}" : string.Empty)));
    }

    /// <summary>Sélection.</summary>
    public Selection Selection { get; }

    /// <summary>Identifiant stable.</summary>
    public Guid Id => Selection.Id;

    /// <summary>Nom affiché.</summary>
    public string Name => Selection.Name;

    /// <summary>Couleur d'affichage.</summary>
    public string Color => Selection.Color;

    /// <summary>Nombre d'éléments.</summary>
    public int Count => Selection.Items.Count;

    /// <summary>Membres, dans l'ordre (« PAR 1 → PAR 2 → PAR 3 »).</summary>
    public string Members { get; }
}

/// <summary>Une sélection automatique affichée (INST-031), jamais enregistrée.</summary>
/// <param name="Title">Intitulé (« Tous », « Tous les PAR »…).</param>
/// <param name="Count">Nombre d'appareils.</param>
public sealed record AutoSelectionRowViewModel(string Title, int Count);
