namespace Luxia.UI.Controls;

/// <summary>Une étape dans la bande d'étapes.</summary>
/// <param name="Label">Nom court (« 2 · Bleu »).</param>
/// <param name="FadeSeconds">Fondu d'entrée de l'étape, en secondes.</param>
/// <param name="HoldSeconds">Maintien, en secondes.</param>
/// <param name="Color">Couleur « #RRGGBB » représentative (la couleur réglée dans l'étape), ou nulle.</param>
/// <param name="IsPlaying">L'étape joue en ce moment.</param>
/// <param name="TimesText">Durées écrites dans leur unité (« 0 + 2 temps ») ; nul : les secondes.</param>
public sealed record StepStripItem(string Label, double FadeSeconds, double HoldSeconds, string? Color, bool IsPlaying, string? TimesText = null);
