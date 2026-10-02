# LIVE-023 – Zone Show

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 18 – 4. Exigences – musique et automatique](../18-live.md) |
| **Remarque** | Repris par l'écran de jeu (Q44 solution C) : colonne « Shows » (lancer / arrêter) et bandeau « Show en cours » (étape active, transitions possibles, forcer). |
| **Liens** | — |

## Description

> Zone **Show** : show en cours, étape active, prochaines transitions possibles ; forcer une transition ; lancer / arrêter un show.

**Critère d'acceptation** : —

## Réalisation

- src/Luxia.UI.Modules.Control/Sequencing/ShowBandViewModel.cs
- src/Luxia.UI.Modules.Control/Sequencing/ShowsColumnViewModel.cs

## Tests

- SequencingScreensTests.ShowsColumn_LaunchesAShow_AndTheBandSuperviseIt
- SequencingScreensTests.Band_ForcesATransition_AtTheNextBar

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 4. Exigences – musique et automatique). |
| 2026-10-02 | Claude | Développement | P8 lot 5 (`4ceb412`) : l'écran Live sera retiré à la fin de P8 (Q47) ; sa « zone Show » est dans l'écran de jeu. |
