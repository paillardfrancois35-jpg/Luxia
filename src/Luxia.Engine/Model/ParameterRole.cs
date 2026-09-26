namespace Luxia.Engine.Model;

/// <summary>Rôle d'un paramètre dans la chaîne de rendu (doc 02 §9).</summary>
public enum ParameterRole
{
    /// <summary>
    /// Intensité (gradateur maître, gradateur de cellule ou intensité virtuelle) : fusion HTP entre couches,
    /// soumise au Grand Master et au blackout (GEN-041).
    /// </summary>
    Intensity,

    /// <summary>Émetteur de couleur (R, V, B, blanc, ambre, UV…) : LTP ; suit l'intensité si l'appareil le demande (MOT-040).</summary>
    Emitter,

    /// <summary>Tout autre attribut (position, faisceau, roue, programme…) : LTP par priorité, jamais touché par le blackout.</summary>
    Other,
}
