using System.Globalization;
using Luxia.Fixtures.Model;

namespace Luxia.UI.Modules.Console;

/// <summary>Bouton de plage : un clic émet la valeur médiane (BIB-061, CONS-023).</summary>
/// <param name="Capability">Plage.</param>
public sealed record RangeButton(Capability Capability)
{
    /// <summary>Libellé « 51-200 Strobe lent → rapide ».</summary>
    public string Text => string.Create(CultureInfo.CurrentCulture, $"{Capability.Min}-{Capability.Max}  {Capability.Label}");
}
