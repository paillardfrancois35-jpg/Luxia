using Luxia.Engine.Model;

namespace Luxia.Scenes.Model;

/// <summary>Couche (doc 17 §1.2) : scènes mutuellement exclusives, priorité, master, mode d'intensité.</summary>
public sealed record Layer
{
    /// <summary>Identifiant stable (GEN-052).</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nom.</summary>
    public required string Name { get; init; }

    /// <summary>Couleur d'affichage « #RRGGBB » (GEN-106).</summary>
    public string Color { get; init; } = "#58A6FF";

    /// <summary>Icône facultative (GEN-106).</summary>
    public string? Icon { get; init; }

    /// <summary>Priorité (ordre d'empilement pour la fusion LTP).</summary>
    public int Priority { get; init; }

    /// <summary>Exclusive : lancer une scène coupe la précédente (fondu croisé).</summary>
    public bool Exclusive { get; init; } = true;

    /// <summary>Master initial (0-1).</summary>
    public double Master { get; init; } = 1;

    /// <summary>Mode d'intensité (doc 15 §5.2).</summary>
    public IntensityMode IntensityMode { get; init; } = IntensityMode.Htp;

    /// <summary>Le master agit aussi sur les autres attributs (MOT-033).</summary>
    public bool MasterOnAllAttributes { get; init; }

    /// <summary>Fondu croisé par défaut (0,5 s).</summary>
    public Duration CrossFade { get; init; } = Duration.FromSeconds(0.5);
}
