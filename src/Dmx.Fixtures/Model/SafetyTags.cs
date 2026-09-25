namespace Dmx.Fixtures.Model;

/// <summary>Étiquettes de sûreté d'un canal (doc 12 §2.4, BIB-007) : utilisées par les limites de sûreté (P5).</summary>
[Flags]
public enum SafetyTags
{
    /// <summary>Aucune.</summary>
    None = 0,

    /// <summary>Strobe : durée et fréquence plafonnées (GEN-083).</summary>
    Strobe = 1,

    /// <summary>Fumée : durée et repos plafonnés (GEN-084).</summary>
    Smoke = 2,

    /// <summary>Mouvement : zones interdites (GEN-085).</summary>
    Movement = 4,
}
