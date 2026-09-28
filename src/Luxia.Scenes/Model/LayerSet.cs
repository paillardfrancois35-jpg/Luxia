using Luxia.Engine.Model;
using Luxia.Fixtures.Model;

namespace Luxia.Scenes.Model;

/// <summary>
/// Couches d'un projet, enregistrées dans <c>couches.json</c> (doc 50). En P4, le modèle par défaut du doc 17 §1.3
/// est fourni tel quel (l'éditeur de couches vient en P5, COU-001) ; ses identifiants sont fixes pour que les
/// scènes d'un projet sans fichier de couches y retrouvent toujours leur couche.
/// </summary>
public sealed record LayerSet
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Couche « Intensité » du modèle par défaut.</summary>
    public static readonly Guid IntensityLayerId = new("7c1a0001-0000-4000-8000-000000000001");

    /// <summary>Couche « Couleurs » du modèle par défaut.</summary>
    public static readonly Guid ColorsLayerId = new("7c1a0001-0000-4000-8000-000000000002");

    /// <summary>Couche « Mouvements » du modèle par défaut.</summary>
    public static readonly Guid MovementsLayerId = new("7c1a0001-0000-4000-8000-000000000003");

    /// <summary>Couche « Faisceau » du modèle par défaut.</summary>
    public static readonly Guid BeamLayerId = new("7c1a0001-0000-4000-8000-000000000004");

    /// <summary>Couche « Effets » du modèle par défaut.</summary>
    public static readonly Guid EffectsLayerId = new("7c1a0001-0000-4000-8000-000000000005");

    /// <summary>Couche « Ambiance » du modèle par défaut.</summary>
    public static readonly Guid AtmosphereLayerId = new("7c1a0001-0000-4000-8000-000000000006");

    /// <summary>
    /// Couche « Libre » du modèle par défaut (ERG-008) : sans famille attendue, pour ce qui n'entre dans aucune autre
    /// couche (un appareil piloté à part, un essai) ; elle donne aussi un rôle au 8e fader de l'APC mini.
    /// </summary>
    public static readonly Guid FreeLayerId = new("7c1a0001-0000-4000-8000-000000000007");

    /// <summary>Couche « Flashs » du modèle par défaut.</summary>
    public static readonly Guid FlashLayerId = new("7c1a0001-0000-4000-8000-000000000099");

    /// <summary>Couches.</summary>
    public IReadOnlyList<Layer> Layers { get; init; } = DefaultLayers();

    /// <summary>Modèle de couches par défaut (doc 17 §1.3).</summary>
    public static LayerSet Default() => new() { Layers = DefaultLayers() };

    private static IReadOnlyList<Layer> DefaultLayers() =>
    [
        new Layer { Id = IntensityLayerId, Name = "Intensité", Priority = 1, Color = "#E3B341", Icon = "☀", Families = [AttributeFamily.Intensity] },
        new Layer { Id = ColorsLayerId, Name = "Couleurs", Priority = 2, Color = "#DB61A2", Icon = "◐", Families = [AttributeFamily.Color, AttributeFamily.Intensity] },
        new Layer { Id = MovementsLayerId, Name = "Mouvements", Priority = 3, Color = "#58A6FF", Icon = "↻", Families = [AttributeFamily.Position, AttributeFamily.EffectMotion] },
        new Layer { Id = BeamLayerId, Name = "Faisceau", Priority = 4, Color = "#A371F7", Icon = "◎", Families = [AttributeFamily.Beam] },
        new Layer
        {
            Id = EffectsLayerId,
            Name = "Effets",
            Priority = 5,
            Color = "#F0883E",
            Icon = "✦",
            Families = [AttributeFamily.Beam, AttributeFamily.Programs, AttributeFamily.EffectMotion, AttributeFamily.Intensity, AttributeFamily.Color],
        },
        new Layer
        {
            Id = AtmosphereLayerId,
            Name = "Ambiance",
            Priority = 6,
            Color = "#8957E5",
            Icon = "☁",
            KeepOnStopAll = true,
            Families = [AttributeFamily.Atmosphere, AttributeFamily.Color, AttributeFamily.Intensity],
        },
        new Layer { Id = FreeLayerId, Name = "Libre", Priority = 7, Color = "#3FB950", Icon = "★" },
        new Layer
        {
            Id = FlashLayerId,
            Name = "Flashs",
            Priority = 99,
            IntensityMode = IntensityMode.Priority,
            Kind = LayerKind.Flash,
            CrossFade = Duration.Zero,
            Color = "#F85149",
            Icon = "⚡",
        },
    ];
}
