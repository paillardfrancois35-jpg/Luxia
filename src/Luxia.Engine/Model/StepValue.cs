namespace Luxia.Engine.Model;

/// <summary>
/// Valeur d'une étape pour un paramètre (doc 16 §2), déjà résolue : palette traduite, sélection développée,
/// couleur convertie selon les émetteurs de l'appareil (D26).
/// </summary>
/// <param name="Parameter">Indice du paramètre dans <see cref="ShowModel.Parameters"/>.</param>
/// <param name="Value">Valeur normalisée cible (0 à 1).</param>
/// <param name="Fade">Fondu propre à cette valeur (SCN-011) ; <c>null</c> = fondu de l'étape.</param>
/// <param name="Delay">Retard avant le début du fondu (« fan » temporel, SCN-010).</param>
public readonly record struct StepValue(int Parameter, double Value, Duration? Fade = null, Duration Delay = default);
