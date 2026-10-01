# CONS-001 – Faders par pages

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P1 |
| **Source** | [doc 11 – 3. Exigences – mode canaux (P1)](../11-console.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Affichage de faders par pages (16, 32 ou 48 par page selon la largeur), numérotés 1 à 512, pour l'univers choisi.

**Critère d'acceptation** : Parcours des 16 pages de 32.

## Réalisation

- `src/Luxia.UI.Modules.Console/ConsoleView.axaml`
- `src/Luxia.UI.Modules.Console/ConsoleViewModel.cs`

## Tests

- `ConsoleViewModelTests.PageSize_AdaptsToWidth_AndPagesCoverAll512Channels`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 3. Exigences – mode canaux (P1)). |
| 2026-09-24 | Claude | Décision | 16, 32 ou 48 faders selon la largeur ; tranche réduite de 46 à 42 px après capture d'écran, pour obtenir 32 faders sur un écran 1920 px. |
| 2026-09-24 | Claude | Développement | `87237c9` feat(console): console en mode canaux, moniteur, instantanés, coquille de l'application |
