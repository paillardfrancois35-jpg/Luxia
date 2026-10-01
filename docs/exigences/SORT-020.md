# SORT-020 – Trame complète à chaque tick

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P0 |
| **Source** | [doc 10 – 4.2 Émission](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> À chaque tick, le pilote envoie la **trame complète** de l'univers (et non les seules différences) : cela sert aussi de signal de vie au firmware.

**Critère d'acceptation** : Analyse du flux série : 40 messages/s.

## Réalisation

- `src/Luxia.Output/Arduino/ArduinoOutputDriver.cs`

## Tests

- `ArduinoOutputDriverTests.Post_SendsFullFrameWithConfiguredChannelCount`
- `EnttecProtocolTests.EncodeSendDmx_FullUniverse_Is518Bytes`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 4.2 Émission). |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
