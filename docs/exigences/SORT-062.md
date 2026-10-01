# SORT-062 – Pilote Simulateur

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 10 – 7. Pilotes Simulateur, Enregistreur, Art-Net](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | Pas de pilote `IOutputDriver` dédié : le simulateur lit directement `RenderEngine.CopyLastFrame`, comme la console. |
| **Liens** | SIM-002 |

## Description

> **Simulateur** : transmet la trame en mémoire au visualiseur, sans copie bloquante.

**Critère d'acceptation** : Simulateur à jour à 40 Hz.

## Réalisation

- `src/Luxia.UI.Modules.Simulator/SimulatorViewModel.cs` : `FrameOf`, lit `RenderEngine.CopyLastFrame` par univers utilisé.

## Tests

- Couvert indirectement par `SimulatorViewModelTests.Refresh_DecodesEmittedFrameIntoFixtureColor`.

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, §7). |
| 2026-09-26 | Claude | Décision | Pas de pilote `IOutputDriver` séparé (Arduino/Nul/Enregistreur) : le simulateur est **dans le même processus** que le moteur, donc « transmettre la trame en mémoire sans copie bloquante » est exactement ce que fait déjà `CopyLastFrame` (verrou court, déjà utilisé par le moniteur de la console depuis P1). Créer un pilote au sens des autres sorties (avec état de connexion, etc.) n'aurait rien apporté ici. |
| 2026-09-26 | Claude | Développement | `7b86422` feat(simulateur): écran Simulateur 2D (doc 14) |
