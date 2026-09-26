namespace Luxia.Midi;

/// <summary>
/// Reprise douce d'un fader (MIDI-004) : le fader physique ne prend la main que lorsqu'il croise (ou rejoint) la valeur
/// courante ; il la perd si la valeur est changée ailleurs (écran, autre contrôleur), jusqu'au prochain croisement.
/// </summary>
public sealed class SoftTakeover
{
    private const double Tolerance = 0.02;

    private double? _last;
    private double? _sent;
    private bool _engaged;

    /// <summary>Le fader a la main.</summary>
    public bool Engaged => _engaged;

    /// <summary>
    /// Mouvement du fader physique vers <paramref name="physical"/> (0-1) alors que la valeur courante est
    /// <paramref name="target"/> : renvoie la valeur à appliquer, ou <c>null</c> tant que le fader n'a pas croisé.
    /// </summary>
    public double? Move(double physical, double target)
    {
        if (_engaged && _sent is { } sent && Math.Abs(target - sent) > Tolerance + 0.01)
        {
            // Valeur changée ailleurs depuis le dernier envoi : le fader doit la recroiser.
            _engaged = false;
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

        _sent = physical;
        return physical;
    }
}
