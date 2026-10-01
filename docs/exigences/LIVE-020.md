# LIVE-020 – Affichage du tempo à l'écran de jeu

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P7 |
| **Source** | [doc 18 – 4. Exigences – musique et automatique](../18-live.md) |
| **Remarque** | Bloc BPM de l'écran de jeu : BPM (champ saisissable), interrupteur Audio (la source), confiance en couleur, voyants des quatre temps. L'énergie s'affiche à l'écran Audio, pas dans le bloc. Fiche créée à la revue de fin de P7. |
| **Liens** | — |

## Description

> Affichage du BPM, de la source (audio / tap / fixe), de la confiance, d'un indicateur de battement (1-2-3-4) et de l'énergie.

**Critère d'acceptation** : Revue.

## Réalisation

- `src/Luxia.UI.Modules.Control/TempoBarViewModel.cs`
- `src/Luxia.UI.Modules.Control/Views/GameView.axaml`
- `src/Luxia.UI.Controls/TempoGlance.cs`

## Tests

- `TempoBarTests.Bar_StartsAt120_Fixed_AndShowsTheFirstBeat`
- `AudioViewModelTests.GameSwitch_FollowsTheMusic_GreysTheManualControls_AndKeepsTheCorrections`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 4. Exigences – musique et automatique). |
| 2026-09-30 | Claude | Développement | P7 lot 5 : bloc BPM (source, valeur, compteur 1-2-3-4, confiance). |
| 2026-10-01 | Utilisateur | Test | Essai P7 puis re-vérifications : bloc BPM, interrupteur Audio, confiance, voyants conformes (E1, E2, E4, E5 ✅). |
| 2026-10-02 | Claude | Note | Fiche créée à la revue de fin de P7 (elle manquait). |
