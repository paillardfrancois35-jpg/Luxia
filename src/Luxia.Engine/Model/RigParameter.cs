namespace Luxia.Engine.Model;

/// <summary>
/// Paramètre du moteur : un attribut d'un appareil patché, c'est-à-dire une définition de canal de son modèle
/// (D26). C'est l'unité de calcul du moteur : scènes, surcharges et masters agissent sur des paramètres,
/// la conversion en octets n'a lieu qu'en fin de chaîne (P4, D13).
/// </summary>
/// <remarks>
/// Les appareils jumeaux (INST-014) partagent les mêmes paramètres : ils reçoivent donc les mêmes valeurs (MOT-092).
/// </remarks>
public sealed record RigParameter
{
    /// <summary>Appareil patché (le premier du groupe pour des jumeaux).</summary>
    public required Guid FixtureId { get; init; }

    /// <summary>Clé de la définition de canal dans le modèle ; <see cref="VirtualIntensityKey"/> pour l'intensité virtuelle.</summary>
    public required string ChannelKey { get; init; }

    /// <summary>Cellule (0 = appareil entier).</summary>
    public int Cell { get; init; }

    /// <summary>Libellé lisible (« PAR 1 – Rouge »), pour le journal et l'explication de la valeur (GEN-043).</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Rôle dans la chaîne de rendu.</summary>
    public ParameterRole Role { get; init; } = ParameterRole.Other;

    /// <summary>Valeur par défaut normalisée (étape 1 de la chaîne, MOT-032).</summary>
    public double Default { get; init; }

    /// <summary>Attribut discret (emplacement de roue, programme…) : jamais interpolé (MOT-012).</summary>
    public bool Discrete { get; init; }

    /// <summary>Sens inversé (définition du modèle ou option de montage, MOT-090).</summary>
    public bool Inverted { get; init; }

    /// <summary>
    /// Indice du paramètre d'intensité qui multiplie celui-ci en fin de chaîne (« Suit l'intensité », MOT-040) ;
    /// -1 = aucun.
    /// </summary>
    public int IntensitySource { get; init; } = -1;

    /// <summary>Appareil absent du lieu actif : ses canaux sont émis à 0 (MOT-091, INST-052).</summary>
    public bool Absent { get; init; }

    /// <summary>Emplacements DMX (vide pour l'intensité virtuelle ; plusieurs pour des jumeaux à des adresses différentes).</summary>
    public IReadOnlyList<ChannelAddress> Outputs { get; init; } = [];

    /// <summary>Clé réservée de l'intensité virtuelle d'un appareil sans gradateur (BIB-006).</summary>
    public const string VirtualIntensityKey = "@intensite";

    /// <summary>Intensité calculée par le logiciel, sans canal DMX propre.</summary>
    public bool IsVirtual => Outputs.Count == 0;
}
