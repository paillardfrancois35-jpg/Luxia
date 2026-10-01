# CONS-043 – Délimitation des appareils et surcharges dans le moniteur

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P1 |
| **Source** | [doc 11 – 5. Moniteur de sortie](../11-console.md) |
| **Remarque** | — |
| **Liens** | CONS-092 |

## Description

> Les canaux appartenant à un même appareil sont délimités visuellement ; les canaux surchargés sont marqués.

**Critère d'acceptation** : Revue.

## Réalisation

- `src/Luxia.UI.Controls/OutputMonitor.cs` : propriété `FixtureBoundaries`, trait entre deux canaux voisins d'appareils différents (haut/bas/gauche/droite selon le voisin réellement différent, gère le passage à la ligne des 32 colonnes).
- `src/Luxia.Patch/Rules/PatchLookup.cs` : `FixtureRanges`.
- `src/Luxia.UI.Modules.Console/ConsoleViewModel.cs`, `ConsoleView.axaml.cs` : `FixtureBoundaries` transmise au moniteur à chaque rafraîchissement.

## Tests

- `PatchLookupTests.FixtureRanges_ReturnsOneRangePerFixture`
- `ConsoleViewModelTests.FixtureBoundaries_ReflectsPatch`
- Rendu visuel : vérification par le guide de démonstration ou sur le matériel.

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 5. Moniteur de sortie). |
| 2026-09-24 | Claude | Développement | `87237c9` feat(console): console en mode canaux, moniteur, instantanés, coquille de l'application |
| 2026-09-25 | Utilisateur | Écart | Info-bulle absente ou trop lente au survol ; numéro de canal immédiat + cadre demandés. Nouvelle exigence CONS-092 (P3, avec la délimitation par appareil). |
| 2026-09-26 | Claude | Développement | `5d60230` feat(console): mode appareils (CONS-020 à 024), CONS-007, CONS-043, CONS-092 |
