using Luxia.UI.Controls;

namespace Luxia.App.ViewModels;

/// <summary>Entrée de la navigation de l'Atelier.</summary>
/// <param name="Title">Libellé.</param>
/// <param name="Glyph">Symbole affiché devant le libellé.</param>
/// <param name="Page">Modèle de vue de l'écran.</param>
public sealed record NavigationItem(string Title, string Glyph, ViewModelBase Page);
