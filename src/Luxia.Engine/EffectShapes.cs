using Luxia.Engine.Model;

namespace Luxia.Engine;

/// <summary>
/// Formes des effets (EFF-002, EFF-003) : fonctions pures d'une position dans le cycle, sans allocation
/// (appelées à chaque tick, doc 03 §4.1). Aussi utilisées par l'interface pour dessiner une forme.
/// </summary>
public static class EffectShapes
{
    /// <summary>
    /// Écart par rapport au centre, entre -0,5 et +0,5, pour une position <paramref name="cycles"/> exprimée en cycles
    /// (partie entière = numéro du cycle, pour les formes aléatoires).
    /// </summary>
    /// <param name="shape">Forme.</param>
    /// <param name="cycles">Position en cycles (peut être négative).</param>
    /// <param name="axis">Composante lue (formes de position).</param>
    /// <param name="dutyCycle">Rapport cyclique (carré, impulsion).</param>
    /// <param name="seed">Graine des formes aléatoires (moteur, effet, membre).</param>
    public static double Offset(EffectShape shape, double cycles, EffectAxis axis = EffectAxis.X, double dutyCycle = 0.5, ulong seed = 0)
    {
        var u = cycles - Math.Floor(cycles);
        var duty = Math.Clamp(dutyCycle, 0.01, 1);
        switch (shape)
        {
            case EffectShape.Sine:
                // Départ au plus bas : un effet qui arrive ne commence pas par un éclat.
                return -0.5 * Math.Cos(2 * Math.PI * u);
            case EffectShape.Triangle:
                return (u < 0.5 ? 2 * u : 2 - (2 * u)) - 0.5;
            case EffectShape.Square:
                return u < duty ? 0.5 : -0.5;
            case EffectShape.SawUp:
                return u - 0.5;
            case EffectShape.SawDown:
                return 0.5 - u;
            case EffectShape.Pulse:
                return u < duty ? 0.5 - (u / duty) : -0.5;
            case EffectShape.Random:
                return Noise(seed, (long)Math.Floor(cycles)) - 0.5;
            case EffectShape.Circle:
                return axis == EffectAxis.X ? 0.5 * Math.Sin(2 * Math.PI * u) : 0.5 * Math.Cos(2 * Math.PI * u);
            case EffectShape.Eight:
                return axis == EffectAxis.X ? 0.5 * Math.Sin(2 * Math.PI * u) : 0.5 * Math.Sin(4 * Math.PI * u);
            case EffectShape.SweepPan:
                return axis == EffectAxis.X ? 0.5 * Math.Sin(2 * Math.PI * u) : 0;
            case EffectShape.SweepTilt:
                return axis == EffectAxis.Y ? 0.5 * Math.Sin(2 * Math.PI * u) : 0;
            case EffectShape.RandomSlow:
                {
                    // Un point par cycle, rejoint par une courbe en S : pas de saut (ménage les moteurs des lyres).
                    var n = (long)Math.Floor(cycles);
                    var salt = axis == EffectAxis.X ? 0x51UL : 0xA7UL;
                    var from = Noise(seed ^ salt, n);
                    var to = Noise(seed ^ salt, n + 1);
                    var s = u * u * (3 - (2 * u));
                    return from + ((to - from) * s) - 0.5;
                }

            default:
                return 0;
        }
    }

    /// <summary>La forme se dessine dans le plan Pan/Tilt (EFF-003), pas en courbe.</summary>
    public static bool IsPosition(EffectShape shape) =>
        shape is EffectShape.Circle or EffectShape.Eight or EffectShape.SweepPan or EffectShape.SweepTilt or EffectShape.RandomSlow;

    /// <summary>Valeur d'une table sur un cycle, interpolée ou en escalier.</summary>
    public static double Sample(IReadOnlyList<double> table, double cycles, bool stepped)
    {
        ArgumentNullException.ThrowIfNull(table);
        var count = table.Count;
        if (count == 0)
        {
            return 0;
        }

        var u = cycles - Math.Floor(cycles);
        var position = u * count;
        var index = Math.Min(count - 1, (int)position);
        if (stepped || count == 1)
        {
            return table[index];
        }

        // Interpolation vers la valeur suivante, en rebouclant sur la première : le cycle se referme sans saut.
        var next = table[(index + 1) % count];
        return table[index] + ((next - table[index]) * (position - index));
    }

    /// <summary>Tirage reproductible dans [0, 1) pour une graine et un numéro de cycle (SplitMix64, MOT-004).</summary>
    public static double Noise(ulong seed, long cycle)
    {
        var z = unchecked(seed + ((ulong)cycle * 0x9E3779B97F4A7C15UL) + 0x9E3779B97F4A7C15UL);
        z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
        z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
        z ^= z >> 31;
        return (z >> 11) * (1.0 / (1UL << 53));
    }

    /// <summary>
    /// Position dans le cycle selon le sens (aller-retour : le cycle est parcouru à l'endroit puis à l'envers,
    /// sans saut).
    /// </summary>
    public static double Directed(EffectDirection direction, double cycles) => direction switch
    {
        EffectDirection.Backward => -cycles,
        EffectDirection.PingPong => PingPong(cycles),
        _ => cycles,
    };

    private static double PingPong(double cycles)
    {
        var turn = Math.Floor(cycles / 2);
        var within = cycles - (turn * 2);
        return (turn * 2) + (within < 1 ? within : 2 - within);
    }
}
