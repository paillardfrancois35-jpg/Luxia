namespace Luxia.Engine.Model;

/// <summary>
/// Paramètre piloté par un effet, pour un membre de sa sélection (MOT-060) : déjà résolu par la compilation
/// (sélection développée, retard de phase du membre, taille convertie dans l'unité du paramètre).
/// </summary>
/// <param name="Parameter">Indice du paramètre dans <see cref="ShowModel.Parameters"/>.</param>
/// <param name="Member">Rang du membre dans la sélection (tirage aléatoire propre à chaque membre).</param>
/// <param name="Lag">Retard de phase du membre, en fraction de cycle (EFF-005 : 0 = en tête).</param>
/// <param name="Center">Centre (valeur normalisée) en mode absolu.</param>
/// <param name="Size">Amplitude crête à crête (valeur normalisée).</param>
/// <param name="Axis">Composante de la forme (Pan = X, Tilt = Y).</param>
/// <param name="Table">Valeurs d'un cycle pour la forme <see cref="EffectShape.Table"/> (au moins une).</param>
public readonly record struct EffectChannel(
    int Parameter,
    int Member,
    double Lag,
    double Center,
    double Size,
    EffectAxis Axis = EffectAxis.X,
    IReadOnlyList<double>? Table = null);
