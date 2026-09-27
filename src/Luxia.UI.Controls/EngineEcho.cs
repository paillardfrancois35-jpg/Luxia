namespace Luxia.UI.Controls;

/// <summary>
/// Valeur réglée à l'écran en attente de confirmation par le moteur (essai P5). L'écran relit l'état du moteur à son
/// rythme ; relu avant que le moteur ait traité la commande, l'ancien état ferait « sauter » l'affichage (20 → 10 → 20).
/// Tant que la valeur demandée n'est pas revenue du moteur, l'état relu est ignoré, au plus quelques relectures
/// (commande refusée ou changée ailleurs : l'écran se réaligne quand même).
/// </summary>
/// <param name="tolerance">Écart en dessous duquel la valeur du moteur confirme la demande.</param>
/// <param name="maxWaits">Relectures ignorées au plus.</param>
public sealed class EngineEcho(double tolerance = 0.5, int maxWaits = 10)
{
    private double? _expected;
    private int _waitsLeft;

    /// <summary>Une valeur vient d'être envoyée au moteur.</summary>
    public void Sent(double value)
    {
        _expected = value;
        _waitsLeft = maxWaits;
    }

    /// <summary>L'état relu du moteur peut-il être affiché ?</summary>
    public bool Accept(double engineValue)
    {
        if (_expected is not { } expected)
        {
            return true;
        }

        if (Math.Abs(engineValue - expected) < tolerance || --_waitsLeft <= 0)
        {
            _expected = null;
            return true;
        }

        return false;
    }
}
