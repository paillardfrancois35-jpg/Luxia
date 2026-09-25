namespace Dmx.Fixtures.Model;

/// <summary>
/// Définition de canal (doc 12 §2.4), réutilisable par plusieurs modes (BIB-002).
/// Un canal 16 bits reste <b>un seul</b> attribut ; ses octets grossier et fin sont placés séparément dans chaque mode (BIB-003).
/// </summary>
public sealed record ChannelDefinition
{
    /// <summary>Clé unique dans le modèle (référencée par les modes).</summary>
    public required string Key { get; init; }

    /// <summary>Libellé du constructeur (« CH6 Sélecteur de fonction »).</summary>
    public required string Name { get; init; }

    /// <summary>Attribut.</summary>
    public required AttributeKind Attribute { get; init; }

    /// <summary>Cellule (tête, segment) ; 0 = appareil entier (doc 12 §2.6).</summary>
    public int Cell { get; init; }

    /// <summary>Résolution.</summary>
    public ChannelResolution Resolution { get; init; } = ChannelResolution.Bit8;

    /// <summary>Valeur par défaut (étape 1 de la chaîne de rendu).</summary>
    public int Default { get; init; }

    /// <summary>Valeur « appareil éteint proprement ».</summary>
    public int? Rest { get; init; }

    /// <summary>Valeur utilisée par « Identifier ».</summary>
    public int? Identify { get; init; }

    /// <summary>Sens inversé (0 = max).</summary>
    public bool Inverted { get; init; }

    /// <summary>« Suit l'intensité » imposé ; null = déduit (doc 12 §2.7, BIB-006).</summary>
    public bool? FollowsIntensity { get; init; }

    /// <summary>Étiquettes de sûreté imposées ; null = déduites (BIB-007).</summary>
    public SafetyTags? Safety { get; init; }

    /// <summary>Clé de la roue utilisée (roue de couleur, gobos).</summary>
    public string? Wheel { get; init; }

    /// <summary>Plages (vide = canal continu 0-255 sans signification particulière).</summary>
    public IReadOnlyList<Capability> Capabilities { get; init; } = [];

    /// <summary>Remarques (à vérifier, particularités).</summary>
    public string? Notes { get; init; }

    /// <summary>Plage contenant une valeur (null si aucune).</summary>
    public Capability? CapabilityAt(int value) => Capabilities.FirstOrDefault(c => c.Contains(value));
}
