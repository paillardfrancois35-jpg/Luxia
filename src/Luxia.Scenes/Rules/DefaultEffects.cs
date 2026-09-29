using Luxia.Engine.Model;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Rules;

/// <summary>
/// Bibliothèque d'effets livrée avec tout projet (EFF-007). Identifiants fixes : un projet sans <c>effets.json</c>
/// retrouve toujours les mêmes modèles. Réglages choisis pour le parc réel (4 PAR, 2 lyres, 2 barres de 8 segments).
/// </summary>
public static class DefaultEffects
{
    /// <summary>Catégorie des effets d'intensité.</summary>
    public const string IntensityCategory = "Intensité";

    /// <summary>Catégorie des effets de mouvement.</summary>
    public const string MovementCategory = "Mouvement";

    /// <summary>Catégorie des effets de couleur.</summary>
    public const string ColorCategory = "Couleur";

    /// <summary>Modèles livrés.</summary>
    public static IReadOnlyList<EffectTemplate> Templates { get; } =
    [
        Template(1, "Vague douce", IntensityCategory, "L'intensité monte et descend (10 à 100 %) d'un appareil à l'autre, dans l'ordre de la sélection.", new SceneEffect
        {
            Shape = SceneEffectShape.Sine,
            Period = Duration.FromSeconds(3),

            // De 10 à 100 % : les appareils ne s'éteignent jamais complètement (essai P6, minimum choisi par l'utilisateur).
            Size = 0.9,
            Center = 0.55,
        }),
        Template(2, "Chenillard on/off", IntensityCategory, "Un seul appareil allumé à la fois, qui passe de l'un à l'autre.", new SceneEffect
        {
            Shape = SceneEffectShape.Square,
            Period = Duration.FromSeconds(1.2),
            DutyCycle = 0.25,
        }),
        Template(3, "Pairs / impairs", IntensityCategory, "Un appareil sur deux, en alternance.", new SceneEffect
        {
            Shape = SceneEffectShape.Square,
            Period = Duration.FromSeconds(1),
            PhaseMode = EffectPhaseMode.Groups,
            GroupSize = 2,
        }),
        Template(4, "Respiration", IntensityCategory, "Tous ensemble, montée et descente lentes.", new SceneEffect
        {
            Shape = SceneEffectShape.Sine,
            Period = Duration.FromSeconds(4),
            Spread = 0,
            Size = 0.9,
            Center = 0.55,
        }),
        Template(5, "Miroir", IntensityCategory, "La vague part du centre de la sélection vers les deux bords.", new SceneEffect
        {
            Shape = SceneEffectShape.Sine,
            Period = Duration.FromSeconds(2),
            PhaseMode = EffectPhaseMode.Mirror,
            Spread = 180,
            Size = 0.9,
            Center = 0.55,
        }),
        Template(6, "Scintillement", IntensityCategory, "Chaque appareil varie au hasard, vite.", new SceneEffect
        {
            Shape = SceneEffectShape.Random,
            Period = Duration.FromSeconds(0.15),
            Spread = 0,
            Size = 0.6,
            Center = 0.7,
        }),
        Template(7, "Pulsation", IntensityCategory, "Éclats brefs, tous ensemble.", new SceneEffect
        {
            Shape = SceneEffectShape.Pulse,
            Period = Duration.FromBeats(1),
            DutyCycle = 0.3,
            Spread = 0,
        }),
        Template(8, "Dent de scie", IntensityCategory, "Montée régulière puis extinction, en cascade.", new SceneEffect
        {
            Shape = SceneEffectShape.SawUp,
            Period = Duration.FromSeconds(1.5),
        }),
        Template(20, "Cercle lent", MovementCategory, "Les lyres tracent un cercle autour de leur position.", new SceneEffect
        {
            Shape = SceneEffectShape.Circle,
            Period = Duration.FromSeconds(8),
            Size = 60,
            Relative = true,
            Spread = 0,
        }),
        Template(21, "Cercle rapide", MovementCategory, "Petit cercle rapide.", new SceneEffect
        {
            Shape = SceneEffectShape.Circle,
            Period = Duration.FromSeconds(2),
            Size = 30,
            Relative = true,
            Spread = 0,
        }),
        Template(22, "Cercles opposés", MovementCategory, "Deux lyres en cercle, chacune à l'opposé de l'autre.", new SceneEffect
        {
            Shape = SceneEffectShape.Circle,
            Period = Duration.FromSeconds(6),
            Size = 60,
            Relative = true,
            Spread = 360,
        }),
        Template(23, "Huit", MovementCategory, "Un huit couché autour de la position.", new SceneEffect
        {
            Shape = SceneEffectShape.Eight,
            Period = Duration.FromSeconds(6),
            Size = 60,
            Relative = true,
            Spread = 0,
        }),
        Template(24, "Balayage", MovementCategory, "Va-et-vient horizontal.", new SceneEffect
        {
            Shape = SceneEffectShape.SweepPan,
            Period = Duration.FromSeconds(4),
            Size = 90,
            Relative = true,
            Spread = 0,
        }),
        Template(25, "Hochement", MovementCategory, "Va-et-vient vertical.", new SceneEffect
        {
            Shape = SceneEffectShape.SweepTilt,
            Period = Duration.FromSeconds(3),
            Size = 40,
            Relative = true,
            Spread = 0,
        }),
        Template(26, "Errance", MovementCategory, "Déplacement lent au hasard.", new SceneEffect
        {
            Shape = SceneEffectShape.RandomSlow,
            Period = Duration.FromSeconds(5),
            Size = 60,
            Relative = true,
            Spread = 0,
        }),
        Template(40, "Arc-en-ciel", ColorCategory, "Toutes les couleurs, tous ensemble.", new SceneEffect
        {
            Shape = SceneEffectShape.Rainbow,
            Period = Duration.FromSeconds(10),
            Spread = 0,
        }),
        Template(41, "Arc-en-ciel en vague", ColorCategory, "Les couleurs défilent d'un appareil à l'autre.", new SceneEffect
        {
            Shape = SceneEffectShape.Rainbow,
            Period = Duration.FromSeconds(6),
        }),
        Template(42, "Alternance Latino", ColorCategory, "Jaune, orange, rouge en alternance franche.", new SceneEffect
        {
            Shape = SceneEffectShape.Alternate,
            Period = Duration.FromSeconds(3),
            ThemeId = DefaultPalettes.Themes[0].Id,
        }),
        Template(43, "Dégradé froid", ColorCategory, "Bleu, cyan, blanc en passage lent.", new SceneEffect
        {
            Shape = SceneEffectShape.Gradient,
            Period = Duration.FromSeconds(8),
            ThemeId = DefaultPalettes.Themes[1].Id,
            Spread = 120,
        }),
    ];

    /// <summary>Nouvelle bibliothèque par défaut.</summary>
    public static EffectLibrary Create() => new() { Templates = Templates };

    /// <summary>Copie d'un modèle appliquée à une cible (EFF-007) : nouvel identifiant, nom du modèle.</summary>
    public static SceneEffect Apply(EffectTemplate template, IReadOnlyList<ValueTarget> targets, bool perCell = false)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(targets);
        return template.Effect with { Id = Guid.NewGuid(), Name = template.Name, Targets = targets, PerCell = perCell };
    }

    private static EffectTemplate Template(int number, string name, string category, string description, SceneEffect effect) => new()
    {
        Id = new Guid($"7e0f0001-0000-4000-8000-{number:D12}"),
        Name = name,
        Category = category,
        Description = description,
        Effect = effect with { Id = new Guid($"7e0f0002-0000-4000-8000-{number:D12}"), Name = name },
    };
}
