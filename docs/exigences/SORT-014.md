# SORT-014 – Port imposé manuellement

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P0 |
| **Source** | [doc 10 – 4.1 Détection et connexion](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Un port choisi manuellement peut forcer la connexion (identification facultative, pour les firmwares anciens).

**Critère d'acceptation** : Connexion avec le firmware POC actuel.

## Réalisation

- `src/Luxia.Core/Settings/Preferences.cs`
- `src/Luxia.Output/Arduino/ArduinoOutputDriver.cs`
- `src/Luxia.Output/Arduino/EnttecProtocol.cs`
- `src/Luxia.UI.Modules.Outputs/OutputsViewModel.cs`

## Tests

- `ArduinoOutputDriverTests.ForcedPort_Legacy_SendsLabel0x11WithoutIdentification`
- `EnttecProtocolTests.EncodeLegacy_HasNoStartCode`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 4.1 Détection et connexion). |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
| 2026-09-24 | Claude | Développement | `d5fdbac` feat(app): écran Sorties (état des pilotes, Arduino, test, enregistreur, cadence) |
| 2026-09-24 | Claude | Note | Port imposé avec protocole « ancien firmware POC » (label 0x11, sans start code) pour la transition. |
