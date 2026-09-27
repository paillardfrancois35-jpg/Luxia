using Luxia.Engine.Model;

namespace Luxia.Engine;

/// <summary>État observable d'une lecture de scène (doc 02 §6.4, MOT-100).</summary>
/// <param name="SceneId">Scène.</param>
/// <param name="LayerId">Couche.</param>
/// <param name="State">État.</param>
/// <param name="StepIndex">Étape courante (0 = première).</param>
/// <param name="StepCount">Nombre d'étapes.</param>
/// <param name="StepProgress">Progression 0-1 dans l'étape.</param>
/// <param name="Speed">Vitesse.</param>
/// <param name="Solo">Jouée seule (SCN-034).</param>
/// <param name="Flash">Flash maintenu (MOT-072).</param>
public readonly record struct PlaybackInfo(
    Guid SceneId,
    Guid LayerId,
    PlaybackState State,
    int StepIndex,
    int StepCount,
    double StepProgress,
    double Speed,
    bool Solo,
    bool Flash = false);
