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

- `src/Dmx.Hosting/DmxRuntime.cs`
- `src/Dmx.Output/Drivers/RecorderOutputDriver.cs`
- `src/Dmx.Output/OutputRouter.cs`
- `src/Dmx.UI.Modules.Outputs/OutputsView.axaml`
- `src/Dmx.UI.Modules.Outputs/OutputsViewModel.cs`

## Tests

- `RecordingTests.RecorderDriver_AttachedHot_WritesFramesToFile`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 7. Pilotes Simulateur, Enregistreur, Art-Net). |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
| 2026-09-24 | Claude | Développement | `e7ca543` feat(hote): journal technique, assemblage P0 et outil dmx-headless |
| 2026-09-24 | Claude | Développement | `d5fdbac` feat(app): écran Sorties (état des pilotes, Arduino, test, enregistreur, cadence) |
