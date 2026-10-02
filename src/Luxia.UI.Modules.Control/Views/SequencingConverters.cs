using Avalonia;
using Avalonia.Data.Converters;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>Convertisseurs des fenêtres d'édition d'une séquence et d'un show.</summary>
public static class SequencingConverters
{
    /// <summary>Un cadre clair autour de la section du bloc choisi, aucun sinon.</summary>
    public static IValueConverter SelectionBorder { get; } = new FuncValueConverter<bool, Thickness>(selected => new Thickness(selected ? 1 : 0));

    /// <summary>Bord épais (2) pour une étape active pendant l'essai, fin (1) sinon.</summary>
    public static IValueConverter ActiveBorder { get; } = new FuncValueConverter<bool, Thickness>(active => new Thickness(active ? 2 : 1));

    /// <summary>Couleur du bord d'une carte : bleu-vert si l'étape est active, gris sinon.</summary>
    public static IValueConverter ActiveColor { get; } = new FuncValueConverter<bool, string>(active => active ? "#39C5CF" : "#30363D");
}
