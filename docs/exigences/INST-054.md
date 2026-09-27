# INST-054 – Les palettes de position sont stockées par lieu

| Champ | Valeur |
|---|---|
| **Statut** | Validé |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 13 – 5.2 Exigences](../13-installation-et-lieux.md) |
| **Remarque** | Dupliquer un lieu copie ses positions calibrées (valeurs effectives) et ses zones interdites. |
| **Liens** | PAL-004, INST-053 |

## Description

> Les palettes de **position** sont stockées **par lieu** (doc 17) ; dupliquer un lieu copie ses palettes.

**Critère d'acceptation** : —

## Réalisation

- `src/Luxia.UI.Modules.Installation/InstallationViewModel.cs` (`DuplicateVenue`)
- `src/Luxia.Scenes/Rules/VenuePalettes.cs` (`CopyVenue`)

## Tests

- `VenuePaletteTests.CopyVenue_CopiesTheEffectivePositions`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, 5.2 Exigences). |
| 2026-09-26 | Claude | Développement | `4070786` feat(palettes): positions par lieu avec repli sur Générique, zones interdites saisies en visant |
| 2026-09-27 | Utilisateur | Test | Exemple 11 : lieu « Garage » dupliqué du Générique et activé ; « Piste centre » recalée pour la lyre 1 au Garage (clic droit → Mettre à jour depuis le programmeur) ; retour au Générique = ancienne position, retour au Garage = nouveau point ; avertissement des positions non calées dans Problèmes du projet. Validé. |
