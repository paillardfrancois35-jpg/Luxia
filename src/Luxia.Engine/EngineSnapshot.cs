using Luxia.Engine.Model;

namespace Luxia.Engine;

/// <summary>
/// État observable du moteur à la fin d'un tick (doc 02 §6.4, MOT-100) : copie faite pour l'interface, à son rythme.
/// Les tableaux sont alignés sur <see cref="ShowModel.Parameters"/> du modèle <see cref="Show"/>.
/// </summary>
public sealed record EngineSnapshot
{
    /// <summary>Modèle auquel correspondent les tableaux.</summary>
    public required ShowModel Show { get; init; }

    /// <summary>Valeur logique finale de chaque paramètre (après surcharges, Grand Master, blackout ; avant conversion DMX).</summary>
    public required double[] Values { get; init; }

    /// <summary>Origine de chaque valeur (GEN-043).</summary>
    public required ParameterSource[] Sources { get; init; }

    /// <summary>Surcharge d'attribut de chaque paramètre ; <see cref="double.NaN"/> = libre.</summary>
    public required double[] Overrides { get; init; }

    /// <summary>Lectures en cours, dans l'ordre de fusion (priorité de couche puis ordre d'activation).</summary>
    public required IReadOnlyList<PlaybackInfo> Playbacks { get; init; }

    /// <summary>Master de chaque couche, aligné sur <see cref="ShowModel.Layers"/>.</summary>
    public required double[] LayerMasters { get; init; }

    /// <summary>Niveau réglé du dimmer de chaque groupe (1 pour un groupe sans dimmer), aligné sur <see cref="ShowModel.DimmerGroups"/> (ERG-037).</summary>
    public double[] DimmerLevels { get; init; } = [];

    /// <summary>Niveau effectif de chaque groupe : son niveau × celui de ses parents (règle proportionnelle, ERG-037).</summary>
    public double[] DimmerEffective { get; init; } = [];

    /// <summary>Blackout actif.</summary>
    public bool Blackout { get; init; }

    /// <summary>Sortie figée (MOT-073).</summary>
    public bool Frozen { get; init; }

    /// <summary>Fumée manuelle en cours (maintien ou rafale, CMD-030).</summary>
    public bool Smoking { get; init; }

    /// <summary>Repos de fumée restant en secondes (0 = fumée disponible), MOT-081.</summary>
    public double SmokeRestSeconds { get; init; }

    /// <summary>Horloge musicale : tempo, source, position dans la mesure (doc 19 §3).</summary>
    public TempoInfo Tempo { get; init; } = TempoInfo.Default;

    /// <summary>Grand Master (0 à 1).</summary>
    public double GrandMaster { get; init; } = 1;

    /// <summary>Limites de sûreté en train d'agir (GEN-086, LIVE-008).</summary>
    public IReadOnlyList<ActiveLimit> ActiveLimits { get; init; } = [];

    /// <summary>Dernière lecture active d'une scène (ni en fondu de sortie, ni terminée), ou nulle.</summary>
    public PlaybackInfo? ActivePlayback(Guid sceneId)
    {
        PlaybackInfo? found = null;
        foreach (var playback in Playbacks)
        {
            if (playback.SceneId == sceneId && playback.State is not (Model.PlaybackState.FadingOut or Model.PlaybackState.Done))
            {
                found = playback;
            }
        }

        return found;
    }
}
