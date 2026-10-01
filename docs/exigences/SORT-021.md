# SORT-021 – Nombre de canaux émis réglable

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P0 |
| **Source** | [doc 10 – 4.2 Émission](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Le nombre de canaux émis est réglable (par défaut : 512 ; option « jusqu'au dernier canal patché ») pour accélérer la ligne DMX si besoin.

**Critère d'acceptation** : Réglage à 120 → messages de 120 canaux.

## Réalisation

- `src/Luxia.Core/Settings/Preferences.cs`
- `src/Luxia.Output/Arduino/ArduinoOutputDriver.cs`
- `src/Luxia.UI.Modules.Outputs/OutputsView.axaml`
- `src/Luxia.UI.Modules.Outputs/OutputsViewModel.cs`

## Tests

- `ArduinoOutputDriverTests.Post_SendsFullFrameWithConfiguredChannelCount`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 4.2 Émission). |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
| 2026-09-24 | Claude | Développement | `d5fdbac` feat(app): écran Sorties (état des pilotes, Arduino, test, enregistreur, cadence) |
