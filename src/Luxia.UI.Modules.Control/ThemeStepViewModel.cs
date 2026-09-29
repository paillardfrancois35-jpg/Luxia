using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.Scenes.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>Un cran d'un thème de couleurs en cours d'édition (PAL-010).</summary>
public sealed partial class ThemeStepViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _hex;

    [ObservableProperty]
    private string _label = string.Empty;

    /// <summary>Crée le cran.</summary>
    public ThemeStepViewModel(LogicalColor color)
    {
        ArgumentNullException.ThrowIfNull(color);
        _hex = color.Hex;
    }

    /// <summary>Couleur logique du cran.</summary>
    public LogicalColor Color => LogicalColor.FromHex(Hex);

    /// <summary>Couleur du sélecteur (teinte, saturation, luminosité).</summary>
    public LightColor Light
    {
        get
        {
            var value = int.Parse(Hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return LightColor.FromRgb((byte)((value >> 16) & 0xFF), (byte)((value >> 8) & 0xFF), (byte)(value & 0xFF));
        }
    }

    /// <summary>Change la couleur ; renvoie faux si le texte n'est pas une couleur « #RRGGBB ».</summary>
    public bool TrySet(string? text)
    {
        var candidate = (text ?? string.Empty).Trim();
        if (!candidate.StartsWith('#'))
        {
            candidate = "#" + candidate;
        }

        if (candidate.Length != 7 || !int.TryParse(candidate.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            return false;
        }

        Hex = candidate.ToUpperInvariant();
        return true;
    }
}
