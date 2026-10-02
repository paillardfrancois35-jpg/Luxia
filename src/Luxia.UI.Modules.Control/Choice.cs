namespace Luxia.UI.Modules.Control;

/// <summary>Élément de liste déroulante : une valeur et son libellé français.</summary>
/// <typeparam name="T">Type de valeur.</typeparam>
/// <param name="Value">Valeur.</param>
/// <param name="Label">Libellé.</param>
public sealed record Choice<T>(T Value, string Label)
{
    /// <inheritdoc />
    public override string ToString() => Label;
}
