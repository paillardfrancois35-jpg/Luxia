using Avalonia.Data.Converters;

namespace Luxia.UI.Modules.Control;

/// <summary>Petits convertisseurs des vues de l'écran Contrôle.</summary>
public static class ControlConverters
{
    /// <summary>Estompe (opacité 0,55) ce qui est vrai : scène masquée du Live.</summary>
    public static readonly IValueConverter DimIfTrue = new FuncValueConverter<bool, double>(value => value ? 0.55 : 1);

    /// <summary>Pourcentage entier lisible.</summary>
    public static readonly IValueConverter Percent = new FuncValueConverter<double, string>(value => $"{Math.Round(value)} %");
}
