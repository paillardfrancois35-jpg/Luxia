namespace Luxia.Engine.Model;

/// <summary>Mode d'intensité d'une couche (doc 15 §5.2, MOT-031).</summary>
public enum IntensityMode
{
    /// <summary>Participe au maximum (défaut).</summary>
    Htp,

    /// <summary>Remplace le résultat des couches de priorité inférieure (LTP).</summary>
    Priority,

    /// <summary>Ajoute sa contribution (plafonnée à 100 %).</summary>
    Additive,

    /// <summary>Multiplie le résultat des couches inférieures.</summary>
    Multiplicative,
}
