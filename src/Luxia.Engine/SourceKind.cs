namespace Luxia.Engine;

/// <summary>Origine de la valeur finale d'un paramètre (GEN-043, MOT-034).</summary>
public enum SourceKind
{
    /// <summary>Valeur par défaut du canal (aucune scène, aucune surcharge).</summary>
    Default,

    /// <summary>Une scène jouée dans une couche.</summary>
    Scene,

    /// <summary>Surcharge d'attribut (console en mode appareils, programmeur).</summary>
    Override,

    /// <summary>Intensité coupée par le blackout.</summary>
    Blackout,
}
