using System.Text.Json.Serialization;
using Luxia.Engine.Model;
using Luxia.Fixtures.Model;

namespace Luxia.Scenes.Model;

/// <summary>
/// Effet généré d'une étape (doc 16 §6, EFF-001) : une forme parcourue en boucle sur les membres d'une cible,
/// décalée d'un membre à l'autre. Exécuté par le moteur (doc 15 §7) après compilation.
/// </summary>
/// <remarks>
/// Unités : <see cref="Size"/> et <see cref="Center"/> en fraction 0-1 de l'attribut pour les formes d'intensité,
/// <see cref="Size"/> en degrés pour les formes de position (converti selon la course Pan / Tilt de chaque modèle).
/// </remarks>
public sealed record SceneEffect
{
    /// <summary>Identifiant stable (GEN-052) : le même effet dans deux étapes successives continue sans à-coup.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nom affiché (« Vague douce »…).</summary>
    public string? Name { get; init; }

    /// <summary>
    /// Cibles : appareils, cellules, sélections manuelles ou automatiques ; leurs membres mis bout à bout, dans l'ordre,
    /// forment les membres de l'effet (EFF-005). Un membre présent deux fois ne compte qu'une fois.
    /// </summary>
    public IReadOnlyList<ValueTarget> Targets { get; init; } = [];

    /// <summary>Chaque cellule de chaque membre devient un membre (segments des barres, têtes, EFF-008).</summary>
    public bool PerCell { get; init; }

    /// <summary>Forme.</summary>
    public SceneEffectShape Shape { get; init; } = SceneEffectShape.Sine;

    /// <summary>Attribut animé par une forme d'intensité (intensité par défaut, ou tout attribut continu).</summary>
    public AttributeKind Attribute { get; init; } = AttributeKind.Intensity;

    /// <summary>Durée d'un cycle (secondes, ou temps musicaux : un cycle = N temps, GEN-023).</summary>
    public Duration Period { get; init; } = Duration.FromSeconds(2);

    /// <summary>Amplitude crête à crête : 0-1 (intensité), degrés (position).</summary>
    public double Size { get; init; } = 1;

    /// <summary>Centre en mode absolu, 0-1 (position : milieu de la course par défaut).</summary>
    public double Center { get; init; } = 0.5;

    /// <summary>Palette de position autour de laquelle tourne un effet de position (EFF-003) ; prioritaire sur <see cref="Center"/>.</summary>
    public Guid? PositionPaletteId { get; init; }

    /// <summary>
    /// Mode relatif (MOT-061) : la forme s'ajoute à la valeur de l'étape, sinon à la valeur sous-jacente ; absolu : elle la
    /// remplace. Les formes de couleur sont toujours absolues.
    /// </summary>
    public bool Relative { get; init; }

    /// <summary>Décalage total entre le premier et le dernier membre, en degrés (360 = un cycle réparti sur les membres).</summary>
    public double Spread { get; init; } = 360;

    /// <summary>Répartition du décalage (EFF-005).</summary>
    public EffectPhaseMode PhaseMode { get; init; } = EffectPhaseMode.Linear;

    /// <summary>Taille des groupes pour <see cref="EffectPhaseMode.Groups"/>.</summary>
    public int GroupSize { get; init; } = 2;

    /// <summary>Sens.</summary>
    public EffectDirection Direction { get; init; } = EffectDirection.Forward;

    /// <summary>Rapport cyclique 0-1 (carré, impulsion).</summary>
    public double DutyCycle { get; init; } = 0.5;

    /// <summary>Couleurs d'une alternance ou d'un dégradé (EFF-004).</summary>
    public IReadOnlyList<EffectColor> Colors { get; init; } = [];

    /// <summary>Thème de couleurs (PAL-010) utilisé à la place de <see cref="Colors"/>.</summary>
    public Guid? ThemeId { get; init; }

    /// <summary>La forme se joue dans le plan Pan / Tilt.</summary>
    [JsonIgnore]
    public bool IsPosition => Shape is SceneEffectShape.Circle or SceneEffectShape.Eight or SceneEffectShape.SweepPan
        or SceneEffectShape.SweepTilt or SceneEffectShape.RandomSlow;

    /// <summary>La forme anime la couleur.</summary>
    [JsonIgnore]
    public bool IsColor => Shape is SceneEffectShape.Rainbow or SceneEffectShape.Alternate or SceneEffectShape.Gradient;
}
