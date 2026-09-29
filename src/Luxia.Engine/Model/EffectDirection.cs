namespace Luxia.Engine.Model;

/// <summary>Sens de parcours d'un effet (doc 16 §6.1).</summary>
public enum EffectDirection
{
    /// <summary>Vers l'avant.</summary>
    Forward,

    /// <summary>Vers l'arrière (cercle dans l'autre sens, vague de droite à gauche).</summary>
    Backward,

    /// <summary>Aller-retour : le cycle est parcouru dans un sens puis dans l'autre.</summary>
    PingPong,
}
