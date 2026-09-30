namespace Luxia.Midi;

/// <summary>
/// Reprise douce d'un fader (MIDI-004) : le fader physique ne prend la main que lorsqu'il croise (ou rejoint) la valeur
/// courante ; il la perd si la valeur est changée ailleurs (écran, autre contrôleur), jusqu'au prochain croisement.
/// La valeur relue du moteur peut avoir du retard sur les messages du fader (des dizaines par seconde en mouvement
/// rapide) : elle n'est tenue pour « changée ailleurs » que si elle ne correspond à <b>aucune</b> des dernières valeurs
/// envoyées (essai P5 : un fader descendu vite perdait la main vers 93 %).
/// </summary>
public sealed class SoftTakeover
{
    private const double Tolerance = 0.02;

    /// <summary>Dernières valeurs envoyées retenues : largement plus que ce qu'un fader émet entre deux relectures.</summary>
    private const int History = 128;

    private readonly Queue<double> _recent = new(History);
    private double? _last;
    private bool _engaged;

    /// <summary>Le fader a la main.</summary>
    public bool Engaged => _engaged;

    /// <summary>
    /// Mouvement du fader physique vers <paramref name="physical"/> (0-1) alors que la valeur courante est
    /// <paramref name="target"/> : renvoie la valeur à appliquer, ou <c>null</c> tant que le fader n'a pas croisé.
    /// </summary>
    public double? Move(double physical, double target)
    {
        if (_engaged && !IsRecent(target))
        {
            // Valeur changée ailleurs depuis les derniers envois : le fader doit la recroiser.
            _engaged = false;
            _recent.Clear();
        }

        if (!_engaged)
        {
            var crossed = _last is { } last && (last - target) * (physical - target) <= 0;
            _engaged = Math.Abs(physical - target) <= Tolerance || crossed;
        }

        _last = physical;
        if (!_engaged)
        {
            return null;
        }

        if (_recent.Count == History)
        {
            _recent.Dequeue();
        }

        _recent.Enqueue(physical);
        return physical;
    }

    /// <summary>Le fader perd la main : il devra recroiser la valeur courante (remise à 100 % par un bouton, essai 1.007.080).</summary>
    public void Disengage()
    {
        _engaged = false;
        _last = null;
        _recent.Clear();
    }

    private bool IsRecent(double target)
    {
        foreach (var sent in _recent)
        {
            if (Math.Abs(target - sent) <= Tolerance + 0.01)
            {
                return true;
            }
        }

        return false;
    }
}
