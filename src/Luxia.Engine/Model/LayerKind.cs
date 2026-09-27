namespace Luxia.Engine.Model;

/// <summary>Type de couche (doc 17 §1.2).</summary>
public enum LayerKind
{
    /// <summary>Couche normale : une scène lancée joue jusqu'à son arrêt.</summary>
    Normal,

    /// <summary>Couche Flash : ses scènes sont actives tant que la commande est maintenue (COU-005, MOT-072).</summary>
    Flash,
}
