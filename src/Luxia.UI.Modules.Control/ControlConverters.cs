using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Luxia.UI.Modules.Control;

/// <summary>Petits convertisseurs des vues de l'écran Contrôle.</summary>
public static class ControlConverters
{
    /// <summary>Estompe (opacité 0,55) ce qui est vrai : scène masquée du Live.</summary>
    public static readonly IValueConverter DimIfTrue = new FuncValueConverter<bool, double>(value => value ? 0.55 : 1);

    /// <summary>Couleur d'une valeur retouchée (jaune, comme une surcharge LIVE) ou normale.</summary>
    public static readonly IValueConverter RetouchBrush = new FuncValueConverter<bool, IBrush?>(retouched => retouched ? RetouchedBrush : NormalBrush);

    private static readonly IBrush RetouchedBrush = new SolidColorBrush(Color.Parse("#D29922"));
    private static readonly IBrush NormalBrush = new SolidColorBrush(Color.Parse("#E6EDF3"));

    /// <summary>Pourcentage entier lisible.</summary>
    public static readonly IValueConverter Percent = new FuncValueConverter<double, string>(value => $"{Math.Round(value)} %");
}
