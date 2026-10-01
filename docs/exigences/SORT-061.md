# SORT-061 – Enregistreur activable à chaud

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P0 |
| **Source** | [doc 10 – 7. Pilotes Simulateur, Enregistreur, Art-Net](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> L'Enregistreur peut être activé/désactivé à chaud, et utilisé en même temps que l'Arduino.

**Critère d'acceptation** : Test.

## Réalisation

- `src/Luxia.Hosting/LuxiaRuntime.cs`
- `src/Luxia.Output/Drivers/RecorderOutputDriver.cs`
- `src/Luxia.Output/OutputRouter.cs`
- `src/Luxia.UI.Modules.Outputs/OutputsView.axaml`
- `src/Luxia.UI.Modules.Outputs/OutputsViewModel.cs`

## Tests

- `RecordingTests.RecorderDriver_AttachedHot_WritesFramesToFile`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 7. Pilotes Simulateur, Enregistreur, Art-Net). |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
| 2026-09-24 | Claude | Développement | `e7ca543` feat(hote): journal technique, assemblage P0 et outil dmx-headless |
| 2026-09-24 | Claude | Développement | `d5fdbac` feat(app): écran Sorties (état des pilotes, Arduino, test, enregistreur, cadence) |
| 2026-09-27 | Claude | Développement | Défaut trouvé en cherchant un test instable (analyse d'une session parallèle) : à l'arrêt d'un pilote, la trame encore en attente n'était pas écrite (dernière trame d'un enregistrement perdue). `OutputDriver` l'écrit désormais avant de se déconnecter. Le test du journal attend la connexion de l'enregistreur au lieu de la supposer. |
| 2026-09-27 | Claude | Test | v1.004.078 : enregistrement arrêté peu après l'extinction de l'UV : la dernière transition (161-173 : 255→0) figure bien dans le journal. |
